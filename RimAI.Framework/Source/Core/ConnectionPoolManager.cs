using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Verse;

namespace RimAI.Framework.Core
{
    /// <summary>
    /// 连接池管理器，负责跟踪和管理所有活跃连接的生命周期
    /// </summary>
    public class ConnectionPoolManager : IDisposable
    {
        private static ConnectionPoolManager _instance;
        private static readonly object _lockObject = new object();
        
        private readonly ConcurrentDictionary<string, ConnectionInfo> _activeConnections;
        private readonly Timer _cleanupTimer;
        private bool _disposed;
        
        // 统计信息
        private int _totalConnectionsCreated;
        private int _totalConnectionsDisposed;
        private int _totalCleanupOperations;
        
        /// <summary>
        /// 获取连接池管理器的单例实例
        /// </summary>
        public static ConnectionPoolManager Instance
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
                                Log.Warning("[RimAI] ConnectionPoolManager was disposed, creating new instance");
                            }
                            _instance = new ConnectionPoolManager();
                        }
                    }
                }
                return _instance;
            }
        }
        
        /// <summary>
        /// 当前活跃连接数
        /// </summary>
        public int ActiveConnectionCount => _activeConnections.Count;
        
        /// <summary>
        /// 健康连接数
        /// </summary>
        public int HealthyConnectionCount => _activeConnections.Count(kvp => kvp.Value.IsHealthy);
        
        /// <summary>
        /// 总计创建的连接数
        /// </summary>
        public int TotalConnectionsCreated => _totalConnectionsCreated;
        
        /// <summary>
        /// 总计销毁的连接数
        /// </summary>
        public int TotalConnectionsDisposed => _totalConnectionsDisposed;
        
        private ConnectionPoolManager()
        {
            _activeConnections = new ConcurrentDictionary<string, ConnectionInfo>();
            
            // 定期清理过期连接 - 每分钟检查一次
            _cleanupTimer = new Timer(
                CleanupExpiredConnectionsInternal,
                null,
                TimeSpan.FromMinutes(1), // 延迟1分钟后开始
                TimeSpan.FromMinutes(1)  // 每分钟清理一次
            );
            
            Log.Message("[RimAI] ConnectionPoolManager initialized");
        }
        
        /// <summary>
        /// 注册一个连接到连接池
        /// </summary>
        /// <param name="connectionId">连接的唯一标识符</param>
        /// <param name="connectionType">连接类型（如HTTP、WebSocket等）</param>
        /// <param name="endpoint">连接的端点信息</param>
        /// <param name="maxIdleTime">最大空闲时间，超过后会被清理</param>
        /// <returns>连接信息对象</returns>
        public ConnectionInfo RegisterConnection(
            string connectionId, 
            string connectionType = "HTTP", 
            string endpoint = "", 
            TimeSpan? maxIdleTime = null)
        {
            if (string.IsNullOrEmpty(connectionId))
                throw new ArgumentException("Connection ID cannot be null or empty", nameof(connectionId));
                
            var connectionInfo = new ConnectionInfo
            {
                Id = connectionId,
                ConnectionType = connectionType,
                Endpoint = endpoint,
                CreatedAt = DateTime.UtcNow,
                LastUsedAt = DateTime.UtcNow,
                MaxIdleTime = maxIdleTime ?? TimeSpan.FromMinutes(10),
                IsHealthy = true
                // RequestCount 是只读属性，不需要初始化
            };
            
            if (_activeConnections.TryAdd(connectionId, connectionInfo))
            {
                Interlocked.Increment(ref _totalConnectionsCreated);
                Log.Message($"[RimAI] Connection registered: {connectionId} ({connectionType}) - Total active: {_activeConnections.Count}");
                return connectionInfo;
            }
            else
            {
                // 如果连接已存在，更新其信息
                if (_activeConnections.TryGetValue(connectionId, out var existing))
                {
                    existing.UpdateLastUsed();
                    existing.IsHealthy = true;
                    Log.Message($"[RimAI] Connection updated: {connectionId}");
                    return existing;
                }
                
                Log.Warning($"[RimAI] Failed to register connection: {connectionId}");
                return connectionInfo;
            }
        }
        
        /// <summary>
        /// 从连接池中注销连接
        /// </summary>
        /// <param name="connectionId">连接ID</param>
        public void UnregisterConnection(string connectionId)
        {
            if (string.IsNullOrEmpty(connectionId))
                return;
                
            if (_activeConnections.TryRemove(connectionId, out var connectionInfo))
            {
                connectionInfo.Dispose();
                Interlocked.Increment(ref _totalConnectionsDisposed);
                Log.Message($"[RimAI] Connection unregistered: {connectionId} - Total active: {_activeConnections.Count}");
            }
        }
        
        /// <summary>
        /// 更新连接的最后使用时间
        /// </summary>
        /// <param name="connectionId">连接ID</param>
        public void UpdateConnectionActivity(string connectionId)
        {
            if (_activeConnections.TryGetValue(connectionId, out var connectionInfo))
            {
                connectionInfo.UpdateLastUsed();
            }
        }
        
        /// <summary>
        /// 标记连接为不健康状态
        /// </summary>
        /// <param name="connectionId">连接ID</param>
        /// <param name="reason">不健康的原因</param>
        public void MarkConnectionUnhealthy(string connectionId, string reason = "")
        {
            if (_activeConnections.TryGetValue(connectionId, out var connectionInfo))
            {
                connectionInfo.IsHealthy = false;
                connectionInfo.UnhealthyReason = reason;
                Log.Warning($"[RimAI] Connection marked unhealthy: {connectionId} - Reason: {reason}");
            }
        }
        
        /// <summary>
        /// 获取连接信息
        /// </summary>
        /// <param name="connectionId">连接ID</param>
        /// <returns>连接信息，如果不存在则返回null</returns>
        public ConnectionInfo GetConnectionInfo(string connectionId)
        {
            _activeConnections.TryGetValue(connectionId, out var connectionInfo);
            return connectionInfo;
        }
        
        /// <summary>
        /// 清理过期连接（公共方法）
        /// </summary>
        public void CleanupExpiredConnections()
        {
            CleanupExpiredConnectionsInternal(null);
        }
        
        /// <summary>
        /// 内部清理方法
        /// </summary>
        private void CleanupExpiredConnectionsInternal(object state)
        {
            if (_disposed)
                return;
                
            try
            {
                Interlocked.Increment(ref _totalCleanupOperations);
                
                var expiredConnections = _activeConnections
                    .Where(kvp => kvp.Value.IsExpired || !kvp.Value.IsHealthy)
                    .Select(kvp => kvp.Key)
                    .ToList();
                
                int cleanedCount = 0;
                foreach (var connectionId in expiredConnections)
                {
                    if (_activeConnections.TryRemove(connectionId, out var connectionInfo))
                    {
                        try
                        {
                            connectionInfo.Dispose();
                            cleanedCount++;
                            Interlocked.Increment(ref _totalConnectionsDisposed);
                        }
                        catch (Exception ex)
                        {
                            Log.Warning($"[RimAI] Error disposing connection {connectionId}: {ex.Message}");
                        }
                    }
                }
                
                if (cleanedCount > 0)
                {
                    Log.Message($"[RimAI] Cleaned up {cleanedCount} expired/unhealthy connections. Active: {_activeConnections.Count}");
                }
                
                // 每10次清理操作记录一次详细统计
                if (_totalCleanupOperations % 10 == 0)
                {
                    var stats = GetDetailedStats();
                    Log.Message($"[RimAI] Connection pool stats - Active: {stats.ActiveConnections}, Created: {stats.TotalCreated}, Disposed: {stats.TotalDisposed}");
                }
            }
            catch (Exception ex)
            {
                Log.Error($"[RimAI] Error during connection cleanup: {ex.Message}");
            }
        }
        
        /// <summary>
        /// 获取连接池统计信息
        /// </summary>
        public ConnectionPoolStats GetStats()
        {
            var connections = _activeConnections.Values.ToList();
            
            return new ConnectionPoolStats
            {
                ActiveConnections = connections.Count,
                HealthyConnections = connections.Count(c => c.IsHealthy),
                TotalRequests = connections.Sum(c => c.RequestCount),
                AverageAge = connections.Any() 
                    ? TimeSpan.FromTicks((long)connections.Average(c => c.Age.Ticks))
                    : TimeSpan.Zero,
                OldestConnection = connections.Any() 
                    ? connections.Max(c => c.Age) 
                    : TimeSpan.Zero
            };
        }
        
        /// <summary>
        /// 获取详细统计信息
        /// </summary>
        public DetailedConnectionStats GetDetailedStats()
        {
            var connections = _activeConnections.Values.ToList();
            var connectionsByType = connections
                .GroupBy(c => c.ConnectionType)
                .ToDictionary(g => g.Key, g => g.Count());
            
            return new DetailedConnectionStats
            {
                ActiveConnections = connections.Count,
                HealthyConnections = connections.Count(c => c.IsHealthy),
                UnhealthyConnections = connections.Count(c => !c.IsHealthy),
                TotalCreated = _totalConnectionsCreated,
                TotalDisposed = _totalConnectionsDisposed,
                TotalRequests = connections.Sum(c => c.RequestCount),
                CleanupOperations = _totalCleanupOperations,
                ConnectionsByType = connectionsByType,
                AverageAge = connections.Any() 
                    ? TimeSpan.FromTicks((long)connections.Average(c => c.Age.Ticks))
                    : TimeSpan.Zero,
                OldestConnection = connections.Any() 
                    ? connections.Max(c => c.Age) 
                    : TimeSpan.Zero,
                ExpiredConnections = connections.Count(c => c.IsExpired)
            };
        }
        
        /// <summary>
        /// 获取所有连接的详细列表（用于调试）
        /// </summary>
        public ConnectionInfo[] GetAllConnections()
        {
            return _activeConnections.Values.ToArray();
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
                // 停止清理定时器
                _cleanupTimer?.Dispose();
                
                // 清理所有连接
                var connections = _activeConnections.Values.ToArray();
                _activeConnections.Clear();
                
                foreach (var connection in connections)
                {
                    try
                    {
                        connection.Dispose();
                    }
                    catch (Exception ex)
                    {
                        Log.Warning($"[RimAI] Error disposing connection {connection.Id}: {ex.Message}");
                    }
                }
                
                Log.Message($"[RimAI] ConnectionPoolManager disposed. Final stats - Created: {_totalConnectionsCreated}, Disposed: {_totalConnectionsDisposed + connections.Length}");
            }
            catch (Exception ex)
            {
                Log.Error($"[RimAI] Error disposing ConnectionPoolManager: {ex.Message}");
            }
        }
    }
    
    /// <summary>
    /// 连接信息类
    /// </summary>
    public class ConnectionInfo : IDisposable
    {
        private int _requestCount;
        private bool _disposed;
        
        /// <summary>
        /// 连接唯一标识符
        /// </summary>
        public string Id { get; set; }
        
        /// <summary>
        /// 连接类型
        /// </summary>
        public string ConnectionType { get; set; }
        
        /// <summary>
        /// 连接端点
        /// </summary>
        public string Endpoint { get; set; }
        
        /// <summary>
        /// 创建时间
        /// </summary>
        public DateTime CreatedAt { get; set; }
        
        /// <summary>
        /// 最后使用时间
        /// </summary>
        public DateTime LastUsedAt { get; set; }
        
        /// <summary>
        /// 最大空闲时间
        /// </summary>
        public TimeSpan MaxIdleTime { get; set; }
        
        /// <summary>
        /// 是否健康
        /// </summary>
        public bool IsHealthy { get; set; }
        
        /// <summary>
        /// 不健康的原因
        /// </summary>
        public string UnhealthyReason { get; set; }
        
        /// <summary>
        /// 请求计数
        /// </summary>
        public int RequestCount => _requestCount;
        
        /// <summary>
        /// 连接年龄
        /// </summary>
        public TimeSpan Age => DateTime.UtcNow - CreatedAt;
        
        /// <summary>
        /// 空闲时间
        /// </summary>
        public TimeSpan IdleTime => DateTime.UtcNow - LastUsedAt;
        
        /// <summary>
        /// 是否已过期
        /// </summary>
        public bool IsExpired => IdleTime > MaxIdleTime;
        
        /// <summary>
        /// 更新最后使用时间并增加请求计数
        /// </summary>
        public void UpdateLastUsed()
        {
            LastUsedAt = DateTime.UtcNow;
            Interlocked.Increment(ref _requestCount);
        }
        
        /// <summary>
        /// 释放连接相关资源
        /// </summary>
        public void Dispose()
        {
            if (_disposed)
                return;
                
            _disposed = true;
            
            // 这里可以添加具体的资源清理逻辑
            // 比如关闭网络连接、释放文件句柄等
        }
        
        public override string ToString()
        {
            return $"Connection[{Id}] Type:{ConnectionType} Age:{Age.TotalMinutes:F1}min Requests:{RequestCount} Healthy:{IsHealthy}";
        }
    }
    
    /// <summary>
    /// 连接池基本统计信息
    /// </summary>
    public class ConnectionPoolStats
    {
        public int ActiveConnections { get; set; }
        public int HealthyConnections { get; set; }
        public int TotalRequests { get; set; }
        public TimeSpan AverageAge { get; set; }
        public TimeSpan OldestConnection { get; set; }
    }
    
    /// <summary>
    /// 连接池详细统计信息
    /// </summary>
    public class DetailedConnectionStats : ConnectionPoolStats
    {
        public int UnhealthyConnections { get; set; }
        public int TotalCreated { get; set; }
        public int TotalDisposed { get; set; }
        public int CleanupOperations { get; set; }
        public int ExpiredConnections { get; set; }
        public Dictionary<string, int> ConnectionsByType { get; set; } = new Dictionary<string, int>();
    }
}
