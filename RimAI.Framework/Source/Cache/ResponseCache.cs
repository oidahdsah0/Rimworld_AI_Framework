using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using RimAI.Framework.Core;
using Verse;
using static RimAI.Framework.Core.RimAILogger;

namespace RimAI.Framework.Cache
{
    /// <summary>
    /// 响应缓存系统，使用LRU算法实现线程安全的缓存机制
    /// </summary>
    public class ResponseCache : IDisposable
    {
        private static ResponseCache _instance;
        private static readonly object _lockObject = new object();
        
        private readonly ConcurrentDictionary<string, CacheEntry> _cache;
        private readonly LinkedList<string> _accessOrder;
        private readonly object _accessOrderLock = new object();
        private readonly Timer _cleanupTimer;
        private readonly Timer _statsTimer;
        
        private readonly int _maxSize;
        private bool _disposed;
        
        // 统计信息
        private long _totalRequests;
        private long _cacheHits;
        private long _cacheMisses;
        private long _evictions;
        private long _expirations;
        
        /// <summary>
        /// 获取响应缓存的单例实例
        /// </summary>
        public static ResponseCache Instance
        {
            get
            {
                if (_instance == null || _instance._disposed)
                {
                    lock (_lockObject)
                    {
                        if (_instance == null || _instance._disposed)
                        {
                            if (_instance?._disposed == true)
                            {
                                Warning("ResponseCache was disposed, creating new instance");
                            }
                            _instance = new ResponseCache();
                        }
                    }
                }
                return _instance;
            }
        }
        
        /// <summary>
        /// 当前缓存条目数量
        /// </summary>
        public int Count => _cache.Count;
        
        /// <summary>
        /// 最大缓存大小
        /// </summary>
        public int MaxSize => _maxSize;
        
        /// <summary>
        /// 缓存命中率
        /// </summary>
        public double HitRate => _totalRequests > 0 ? (double)_cacheHits / _totalRequests : 0.0;
        
        /// <summary>
        /// 总请求数
        /// </summary>
        public long TotalRequests => _totalRequests;
        
        /// <summary>
        /// 缓存命中数
        /// </summary>
        public long CacheHits => _cacheHits;
        
        /// <summary>
        /// 缓存未命中数
        /// </summary>
        public long CacheMisses => _cacheMisses;
        
        /// <summary>
        /// 是否已被释放
        /// </summary>
        public bool IsDisposed => _disposed;
        
        private ResponseCache()
        {
            // 从配置系统读取缓存设置 - CRITICAL FIX
            var configuration = RimAI.Framework.Configuration.RimAIConfiguration.Instance;
            _maxSize = Math.Max(1, configuration.Get<int>("cache.size", 200)); // 降低默认值
            var cleanupIntervalMinutes = Math.Max(1, configuration.Get<int>("cache.cleanupIntervalMinutes", 1)); // 更频繁的清理
            
            _cache = new ConcurrentDictionary<string, CacheEntry>();
            _accessOrder = new LinkedList<string>();
            
            // 使用配置的清理间隔
            _cleanupTimer = new Timer(
                CleanupExpiredEntries,
                null,
                TimeSpan.FromMinutes(cleanupIntervalMinutes),
                TimeSpan.FromMinutes(cleanupIntervalMinutes)
            );
            
            // 统计信息定时器 - 每5分钟记录一次统计（更频繁）
            _statsTimer = new Timer(
                LogStatistics,
                null,
                TimeSpan.FromMinutes(5),
                TimeSpan.FromMinutes(5)
            );
            
            Info("ResponseCache initialized from configuration: maxSize={0}, cleanupInterval={1}min", 
                 _maxSize, cleanupIntervalMinutes);
        }
        
        /// <summary>
        /// 获取缓存的值，如果不存在则使用工厂方法创建
        /// </summary>
        /// <typeparam name="T">返回值类型</typeparam>
        /// <param name="key">缓存键</param>
        /// <param name="factory">值工厂方法</param>
        /// <param name="expiration">过期时间</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>缓存的值或新创建的值</returns>
        public async Task<T> GetOrAddAsync<T>(
            string key, 
            Func<Task<T>> factory, 
            TimeSpan? expiration = null,
            CancellationToken cancellationToken = default)
        {
            if (_disposed)
            {
                Warning("Cache is disposed, executing factory directly");
                return await factory();
            }
            
            if (string.IsNullOrEmpty(key))
                throw new ArgumentException("Cache key cannot be null or empty", nameof(key));
                
            if (factory == null)
                throw new ArgumentNullException(nameof(factory));
            
            Interlocked.Increment(ref _totalRequests);
            
            // 生成安全的缓存键
            var safeKey = GenerateCacheKey(key);
            
            // 尝试从缓存获取
            if (_cache.TryGetValue(safeKey, out var existingEntry) && !existingEntry.IsExpired)
            {
                UpdateAccessOrder(safeKey);
                Interlocked.Increment(ref _cacheHits);
                
                Debug("Cache hit for key: {0}", key);
                
                if (existingEntry.Value is T cachedValue)
                {
                    return cachedValue;
                }
            }
            else
            {
                // 如果条目已过期，移除它
                if (existingEntry?.IsExpired == true)
                {
                    RemoveEntry(safeKey);
                    Interlocked.Increment(ref _expirations);
                }
            }
            
            Interlocked.Increment(ref _cacheMisses);
            Debug("Cache miss for key: {0}", key);
            
            // 缓存未命中，调用工厂方法
            var value = await factory();
            
            // 判断是否应该缓存请求
            if (ShouldCacheRequest(safeKey, value))
            {
                // 从配置系统获取默认TTL - CRITICAL FIX
                var configuration = RimAI.Framework.Configuration.RimAIConfiguration.Instance;
                var defaultTtlMinutes = configuration.Get<int>("cache.ttlMinutes", 30);
                var expirationTime = expiration ?? TimeSpan.FromMinutes(defaultTtlMinutes);
                AddToCache(safeKey, value, expirationTime);
            }
            
            return value;
        }
        
        /// <summary>
        /// 直接获取缓存值
        /// </summary>
        /// <typeparam name="T">返回值类型</typeparam>
        /// <param name="key">缓存键</param>
        /// <returns>缓存的值，如果不存在或已过期则返回default(T)</returns>
        public T Get<T>(string key)
        {
            if (_disposed || string.IsNullOrEmpty(key))
                return default(T);
                
            var safeKey = GenerateCacheKey(key);
            
            if (_cache.TryGetValue(safeKey, out var entry) && !entry.IsExpired)
            {
                UpdateAccessOrder(safeKey);
                return entry.Value is T value ? value : default(T);
            }
            
            return default(T);
        }
        
        /// <summary>
        /// 设置缓存值
        /// </summary>
        /// <param name="key">缓存键</param>
        /// <param name="value">缓存值</param>
        /// <param name="expiration">过期时间</param>
        public void Set(string key, object value, TimeSpan? expiration = null)
        {
            if (_disposed || string.IsNullOrEmpty(key))
                return;
                
            var safeKey = GenerateCacheKey(key);
            // 从配置系统获取默认TTL - CRITICAL FIX
            var configuration = RimAI.Framework.Configuration.RimAIConfiguration.Instance;
            var defaultTtlMinutes = configuration.Get<int>("cache.ttlMinutes", 30);
            var expirationTime = expiration ?? TimeSpan.FromMinutes(defaultTtlMinutes);
            
            AddToCache(safeKey, value, expirationTime);
        }
        
        /// <summary>
        /// 移除缓存项
        /// </summary>
        /// <param name="key">缓存键</param>
        /// <returns>是否成功移除</returns>
        public bool Remove(string key)
        {
            if (_disposed || string.IsNullOrEmpty(key))
                return false;
                
            var safeKey = GenerateCacheKey(key);
            return RemoveEntry(safeKey);
        }
        
        /// <summary>
        /// 清空所有缓存
        /// </summary>
        public void Clear()
        {
            if (_disposed)
                return;
                
            lock (_accessOrderLock)
            {
                _cache.Clear();
                _accessOrder.Clear();
            }
            
            Info("Cache cleared");
        }
        
        /// <summary>
        /// 获取缓存统计信息
        /// </summary>
        public CacheStats GetStats()
        {
            var expiredCount = _cache.Values.Count(e => e.IsExpired);
            
            return new CacheStats
            {
                EntryCount = _cache.Count,
                MaxSize = _maxSize,
                TotalRequests = _totalRequests,
                CacheHits = _cacheHits,
                CacheMisses = _cacheMisses,
                HitRate = HitRate,
                Evictions = _evictions,
                Expirations = _expirations,
                ExpiredEntries = expiredCount,
                MemoryUsageEstimate = EstimateMemoryUsage()
            };
        }
        
        /// <summary>
        /// 生成安全的缓存键
        /// </summary>
        private string GenerateCacheKey(string originalKey)
        {
            // 优化：对于长键或包含特殊字符的键，使用更高效的哈希
            if (originalKey.Length > 200 || originalKey.IndexOfAny(new[] { '\n', '\r', '\t', '\0' }) >= 0)
            {
                // 使用更高效的哈希算法
                var hash = 0;
                foreach (char c in originalKey)
                {
                    hash = ((hash << 5) - hash) + c;
                    hash = hash & hash; // 转换为32位整数
                }
                return $"H{Math.Abs(hash):X8}";
            }
            
            return originalKey;
        }
        
        /// <summary>
        /// 添加条目到缓存
        /// </summary>
        private void AddToCache(string key, object value, TimeSpan expiration)
        {
            var entry = new CacheEntry
            {
                Value = value,
                CreatedAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.Add(expiration),
                AccessCount = 1
            };
            
            _cache.AddOrUpdate(key, entry, (k, oldEntry) =>
            {
                // 如果键已存在，更新值和过期时间
                oldEntry.Value = value;
                oldEntry.ExpiresAt = DateTime.UtcNow.Add(expiration);
                oldEntry.AccessCount++;
                return oldEntry;
            });
            
            UpdateAccessOrder(key);
            
            // 如果缓存大小超过限制，执行LRU清理
            if (_cache.Count > _maxSize)
            {
                EvictLeastRecentlyUsed();
            }
        }
        
        /// <summary>
        /// 更新访问顺序（LRU）
        /// </summary>
        private void UpdateAccessOrder(string key)
        {
            lock (_accessOrderLock)
            {
                // 如果键已经在链表中，先移除
                var node = _accessOrder.Find(key);
                if (node != null)
                {
                    _accessOrder.Remove(node);
                }
                
                // 添加到链表头部（最近访问）
                _accessOrder.AddFirst(key);
            }
        }
        
        /// <summary>
        /// 移除条目
        /// </summary>
        private bool RemoveEntry(string key)
        {
            var removed = _cache.TryRemove(key, out _);
            
            if (removed)
            {
                lock (_accessOrderLock)
                {
                    var node = _accessOrder.Find(key);
                    if (node != null)
                    {
                        _accessOrder.Remove(node);
                    }
                }
            }
            
            return removed;
        }
        
        /// <summary>
        /// 清理最少使用的条目（LRU）
        /// </summary>
        private bool EvictLeastRecentlyUsed()
        {
            string keyToRemove = null;
            
            lock (_accessOrderLock)
            {
                // 从链表尾部开始移除（最少使用）
                while (_cache.Count >= _maxSize && _accessOrder.Count > 0)
                {
                    keyToRemove = _accessOrder.Last.Value;
                    _accessOrder.RemoveLast();
                    
                    if (_cache.TryRemove(keyToRemove, out _))
                    {
                        Interlocked.Increment(ref _evictions);
                        Debug("Evicted LRU entry: {0}", keyToRemove);
                        return true; // 成功移除
                    }
                }
            }
            return false; // 没有成功移除
        }
        
        /// <summary>
        /// 清理过期条目
        /// </summary>
        private void CleanupExpiredEntries(object state)
        {
            if (_disposed)
                return;
                
            try
            {
                var expiredKeys = _cache
                    .Where(kvp => kvp.Value.IsExpired)
                    .Select(kvp => kvp.Key)
                    .ToList();
                
                int cleanedCount = 0;
                foreach (var key in expiredKeys)
                {
                    if (RemoveEntry(key))
                    {
                        cleanedCount++;
                        Interlocked.Increment(ref _expirations);
                    }
                }
                
                // 新增：内存压力检测和主动清理
                var stats = GetStats();
                var memoryUsageMB = stats.MemoryUsageEstimate / (1024 * 1024);
                var maxMemoryMB = RimAI.Framework.Configuration.RimAIConfiguration.Instance.Get<int>("cache.maxMemoryMB", 50);
                
                // 如果内存使用超过限制，进行更积极的清理
                if (memoryUsageMB > maxMemoryMB)
                {
                    Warning("Cache memory usage ({0:F1}MB) exceeds limit ({1}MB), performing aggressive cleanup", 
                           memoryUsageMB, maxMemoryMB);
                    
                    // 清理最少使用的条目，直到内存使用降低
                    var aggressiveCleanupCount = 0;
                    while (memoryUsageMB > maxMemoryMB * 0.8 && _cache.Count > _maxSize * 0.5)
                    {
                        if (EvictLeastRecentlyUsed())
                        {
                            aggressiveCleanupCount++;
                            stats = GetStats();
                            memoryUsageMB = stats.MemoryUsageEstimate / (1024 * 1024);
                        }
                        else
                        {
                            break;
                        }
                    }
                    
                    if (aggressiveCleanupCount > 0)
                    {
                        Info("Aggressive cleanup completed: removed {0} entries, memory usage: {1:F1}MB", 
                             aggressiveCleanupCount, memoryUsageMB);
                    }
                }
                
                // 新增：低命中率时的清理
                var minHitRate = RimAI.Framework.Configuration.RimAIConfiguration.Instance.Get<double>("cache.minHitRate", 0.1);
                if (stats.TotalRequests > 50 && stats.HitRate < minHitRate)
                {
                    Warning("Cache hit rate ({0:P2}) below minimum ({1:P2}), clearing cache", 
                           stats.HitRate, minHitRate);
                    Clear();
                }
                
                if (cleanedCount > 0)
                {
                    Debug("Cleaned up {0} expired cache entries", cleanedCount);
                }
            }
            catch (Exception ex)
            {
                Error("Error during cache cleanup: {0}", ex.Message);
            }
        }
        
        /// <summary>
        /// 记录统计信息
        /// </summary>
        private void LogStatistics(object state)
        {
            if (_disposed)
                return;
                
            try
            {
                var stats = GetStats();
                var memoryUsageMB = stats.MemoryUsageEstimate / (1024 * 1024);
                var maxMemoryMB = RimAI.Framework.Configuration.RimAIConfiguration.Instance.Get<int>("cache.maxMemoryMB", 50);
                var minHitRate = RimAI.Framework.Configuration.RimAIConfiguration.Instance.Get<double>("cache.minHitRate", 0.1);
                
                // 详细的缓存健康状态报告
                Info("Cache Statistics - Entries: {0}/{1}, Hit Rate: {2:P1}, Memory: ~{3:F1}MB/{4}MB", 
                     stats.EntryCount, stats.MaxSize, stats.HitRate, memoryUsageMB, maxMemoryMB);
                
                // 健康状态警告
                if (stats.EntryCount > stats.MaxSize * 0.8)
                {
                    Warning("Cache approaching size limit: {0}/{1}", stats.EntryCount, stats.MaxSize);
                }
                
                if (memoryUsageMB > maxMemoryMB * 0.8)
                {
                    Warning("Cache approaching memory limit: {0:F1}MB/{1}MB", memoryUsageMB, maxMemoryMB);
                }
                
                if (stats.TotalRequests > 50 && stats.HitRate < minHitRate)
                {
                    Warning("Cache hit rate below minimum: {0:P2} < {1:P2}", stats.HitRate, minHitRate);
                }
                
                // 性能指标
                if (stats.TotalRequests > 0)
                {
                    var avgResponseTime = stats.TotalRequests > 0 ? 
                        (double)stats.TotalRequests / stats.TotalRequests : 0;
                    Debug("Cache performance - Avg response time: {0:F0}ms, Evictions: {1}, Expirations: {2}", 
                          avgResponseTime, stats.Evictions, stats.Expirations);
                }
            }
            catch (Exception ex)
            {
                Error("Error logging cache statistics: {0}", ex.Message);
            }
        }
        
        /// <summary>
        /// 估算内存使用量（字节）
        /// </summary>
        private long EstimateMemoryUsage()
        {
            // 改进的内存估算算法
            long totalSize = 0;
            
            foreach (var entry in _cache.Values)
            {
                // 基础开销：对象头 + 字段 + 引用
                totalSize += 64; // 基础开销
                
                if (entry.Value is string str)
                {
                    // 字符串：长度 * 2 (Unicode) + 对象开销
                    totalSize += str.Length * 2 + 24;
                }
                else if (entry.Value is LLMResponse response)
                {
                    // LLMResponse对象估算
                    totalSize += 200; // 基础对象大小
                    if (response.Content != null)
                    {
                        totalSize += response.Content.Length * 2;
                    }
                }
                else
                {
                    // 其他对象：保守估算
                    totalSize += 128;
                }
                
                // 缓存键的开销
                totalSize += 32;
            }
            
            return totalSize;
        }
        
        /// <summary>
        /// 判断是否应该缓存请求
        /// </summary>
        private bool ShouldCacheRequest(string key, object value)
        {
            // 游戏启动时的优化：前1000个tick不缓存
            if (Find.TickManager != null && Find.TickManager.TicksGame < 1000)
            {
                Debug("Skipping cache during game startup (tick {0})", Find.TickManager.TicksGame);
                return false;
            }
            
            // 内存压力检查
            var stats = GetStats();
            var memoryUsageMB = stats.MemoryUsageEstimate / (1024 * 1024);
            var maxMemoryMB = RimAI.Framework.Configuration.RimAIConfiguration.Instance.Get<int>("cache.maxMemoryMB", 50);
            
            if (memoryUsageMB > maxMemoryMB * 0.9)
            {
                Debug("Skipping cache due to memory pressure: {0:F1}MB/{1}MB", memoryUsageMB, maxMemoryMB);
                return false;
            }
            
            // 缓存大小检查
            if (_cache.Count >= _maxSize * 0.95)
            {
                Debug("Skipping cache due to size limit: {0}/{1}", _cache.Count, _maxSize);
                return false;
            }
            
            return true;
        }
        
        /// <summary>
        /// 释放资源
        /// </summary>
        public void Dispose()
        {
            if (_disposed)
                return;
                
            _disposed = true;
            
            try
            {
                _cleanupTimer?.Dispose();
                _statsTimer?.Dispose();
                
                var stats = GetStats();
                Info("ResponseCache disposed. Final stats - Entries: {0}, Hit Rate: {1:P1}, Total Requests: {2}", 
                     stats.EntryCount, stats.HitRate, stats.TotalRequests);
                
                Clear();
            }
            catch (Exception ex)
            {
                Error("Error disposing ResponseCache: {0}", ex.Message);
            }
        }
    }
    
    /// <summary>
    /// 缓存条目
    /// </summary>
    internal class CacheEntry
    {
        public object Value { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime ExpiresAt { get; set; }
        public int AccessCount { get; set; }
        
        /// <summary>
        /// 是否已过期
        /// </summary>
        public bool IsExpired => DateTime.UtcNow > ExpiresAt;
        
        /// <summary>
        /// 缓存条目年龄
        /// </summary>
        public TimeSpan Age => DateTime.UtcNow - CreatedAt;
    }
    
    /// <summary>
    /// 缓存统计信息
    /// </summary>
    public class CacheStats
    {
        public int EntryCount { get; set; }
        public int MaxSize { get; set; }
        public long TotalRequests { get; set; }
        public long CacheHits { get; set; }
        public long CacheMisses { get; set; }
        public double HitRate { get; set; }
        public long Evictions { get; set; }
        public long Expirations { get; set; }
        public int ExpiredEntries { get; set; }
        public long MemoryUsageEstimate { get; set; }
    }
}
