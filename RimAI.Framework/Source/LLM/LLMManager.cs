using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using RimAI.Framework.Cache;
using RimAI.Framework.Configuration;
using RimAI.Framework.Core;
using RimAI.Framework.LLM.Configuration;
using RimAI.Framework.LLM.Http;
using RimAI.Framework.LLM.Models;
using RimAI.Framework.LLM.Services;
using Verse;
using static RimAI.Framework.Core.RimAILogger;

namespace RimAI.Framework.LLM
{
    /// <summary>
    /// Unified LLM Manager that provides a simplified API for LLM interactions.
    /// Enhanced with cache, configuration management, and resource lifecycle control.
    /// </summary>
    public class LLMManager : IDisposable
    {
        #region Singleton Pattern
        private static LLMManager _instance;
        private static readonly object _lock = new object();

        /// <summary>
        /// Gets the singleton instance of LLMManager.
        /// </summary>
        public static LLMManager Instance
        {
            get
            {
                if (_instance == null || _instance._disposed)
                {
                    lock (_lock)
                    {
                        if (_instance == null || _instance._disposed)
                        {
                            if (_instance?._disposed == true)
                            {
                                Warning("LLMManager was disposed, creating new instance");
                            }
                            _instance = new LLMManager();
                        }
                    }
                }
                return _instance;
            }
        }
        #endregion

        #region Dependencies
        private readonly HttpClient _httpClient;
        private readonly SettingsManager _settingsManager;
        private readonly LLMExecutor _executor;
        private readonly RimAIConfiguration _configuration;
        private readonly ResponseCache _responseCache;
        private bool _disposed;
        
        // 统计信息
        private int _totalRequests;
        private int _successfulRequests;
        private int _failedRequests;
        private DateTime _lastRequestTime = DateTime.MinValue;
        
        // 连接信息
        private string _connectionId;
        #endregion

        #region Constructor
        /// <summary>
        /// Private constructor for singleton pattern.
        /// </summary>
        private LLMManager()
        {
            try
            {
                // Initialize configuration system first
                _configuration = RimAIConfiguration.Instance;
                Info("LLMManager initializing with configuration system");
                
                // Initialize cache system
                var cacheEnabled = _configuration.Get<bool>("cache.enabled", true);
                if (cacheEnabled)
                {
                    _responseCache = ResponseCache.Instance;
                    Info("LLMManager cache system enabled");
                }
                else
                {
                    Info("LLMManager cache system disabled by configuration");
                }
                
                // Initialize dependencies with improved resource management
                var httpTimeoutSeconds = _configuration.Get<int>("performance.timeoutSeconds", 30); 
                // 为测试连接使用更长的超时时间，确保有足够时间完成网络请求
                var httpClientTimeoutSeconds = Math.Max(httpTimeoutSeconds, 45); // 至少45秒
                _httpClient = HttpClientFactory.GetClient(httpClientTimeoutSeconds);
                _settingsManager = new SettingsManager();
                
                Info("LLMManager HttpClient timeout set to {0} seconds", httpClientTimeoutSeconds);
                
                // 生成连接ID并注册到连接池
                _connectionId = $"LLMManager-{Guid.NewGuid():N}";
                var connectionTimeout = TimeSpan.FromMinutes(_configuration.Get<int>("performance.connectionTimeoutMinutes", 30)); // 30分钟默认
                ConnectionPoolManager.Instance.RegisterConnection(
                    _connectionId, 
                    "LLMManager", 
                    "LLM API Connection",
                    connectionTimeout
                );
                
                // Create unified executor with settings from the actual mod settings
                // 优先使用用户在游戏中设置的实际设置，而不是配置系统中的值
                var actualSettings = _settingsManager.GetSettings();
                _executor = new LLMExecutor(_httpClient, actualSettings);
                
                // 初始化统计信息
                _totalRequests = 0;
                _successfulRequests = 0;
                _failedRequests = 0;

                Info("LLMManager initialized successfully with unified architecture");
            }
            catch (Exception ex)
            {
                Error("Failed to initialize LLMManager: {0}", ex.Message);
                throw;
            }
        }
        #endregion

        #region Public Properties
        /// <summary>
        /// Gets the current streaming status. For backward compatibility.
        /// </summary>
        public bool IsStreamingEnabled => _settingsManager.GetSettings()?.enableStreaming ?? false;

        /// <summary>
        /// Gets the current API settings. Read-only access for downstream mods.
        /// </summary>
        public RimAISettings CurrentSettings => _settingsManager.GetSettings();
        
        /// <summary>
        /// 获取统计信息
        /// </summary>
        public double SuccessRate => _totalRequests > 0 ? (double)_successfulRequests / _totalRequests : 0.0;
        
        /// <summary>
        /// 是否已被释放
        /// </summary>
        public bool IsDisposed => _disposed;
        #endregion

        #region Configuration Integration
        /// <summary>
        /// Creates a RimAISettings object from the current configuration system
        /// </summary>
        private RimAISettings CreateSettingsFromConfiguration()
        {
            var settings = new RimAISettings
            {
                // API配置从配置系统读取 - API key不提供默认值！
                apiKey = _configuration.Get<string>("api.key"), // 不提供默认值，如果配置中没有就是null
                apiEndpoint = _configuration.Get<string>("api.endpoint", "https://api.openai.com/v1"),
                modelName = _configuration.Get<string>("api.model", "gpt-4o"),
                temperature = _configuration.Get<float>("api.temperature", 0.7f),
                maxTokens = _configuration.Get<int>("api.maxTokens", 1000),
                enableStreaming = _configuration.Get<bool>("api.enableStreaming", false),
                
                // 性能配置
                timeoutSeconds = _configuration.Get<int>("performance.timeoutSeconds", 30),
                retryCount = _configuration.Get<int>("performance.retryCount", 3),
                maxConcurrentRequests = _configuration.Get<int>("performance.maxConcurrentRequests", 5),
                
                // 缓存配置
                enableCaching = _configuration.Get<bool>("cache.enabled", true),
                cacheSize = _configuration.Get<int>("cache.size", 500), // 与配置系统保持一致
                cacheTtlMinutes = _configuration.Get<int>("cache.ttlMinutes", 30), // 与配置系统保持一致
                
                // 批处理配置
                batchSize = _configuration.Get<int>("batch.size", 5),
                batchTimeoutSeconds = _configuration.Get<int>("batch.timeoutSeconds", 2),
                
                // 日志配置
                enableDetailedLogging = _configuration.Get<bool>("logging.enableDetailed", false),
                logLevel = _configuration.Get<int>("logging.level", 1),
                
                // 健康检查配置
                enableHealthCheck = _configuration.Get<bool>("health.enableChecks", true),
                healthCheckIntervalMinutes = _configuration.Get<int>("health.intervalMinutes", 5),
                enableMemoryMonitoring = _configuration.Get<bool>("health.enableMemoryMonitoring", true),
                memoryThresholdMB = _configuration.Get<int>("health.memoryThresholdMB", 100),
                
                // 嵌入配置 - embedding key也不提供默认值
                enableEmbeddings = _configuration.Get<bool>("embedding.enabled", false),
                embeddingApiKey = _configuration.Get<string>("embedding.key"), // 不提供默认值
                embeddingEndpoint = _configuration.Get<string>("embedding.endpoint", "https://api.openai.com/v1"),
                embeddingModelName = _configuration.Get<string>("embedding.model", "text-embedding-3-small")
            };
            
            Debug("Created settings from configuration: apiKey={0}, model={1}, temperature={2}", 
                  string.IsNullOrEmpty(settings.apiKey) ? "NOT_SET" : "SET", 
                  settings.modelName, 
                  settings.temperature);
                  
            return settings;
        }
        #endregion

        #region Core API Methods

        /// <summary>
        /// Enhanced API: Sends a message with cache integration, timeout control, and lifecycle management
        /// </summary>
        public async Task<string> SendMessageAsync(string prompt, LLMRequestOptions options = null, CancellationToken cancellationToken = default)
        {
            if (_disposed)
            {
                Debug("SendMessage called on disposed LLMManager");
                return null;
            }
            
            if (!ValidateRequest(prompt)) 
            {
                // 不增加失败计数，因为这是验证失败，不是请求失败
                return null;
            }
            
            // 只有真正发起请求时才增加计数
            Interlocked.Increment(ref _totalRequests);
            
            // 从配置获取超时时间
            var timeoutMs = _configuration.Get<int>("performance.timeoutSeconds", 30) * 1000; // 转换为毫秒
            Debug("Base timeout from config: {0}ms ({1}s)", timeoutMs, timeoutMs / 1000);
            
            if (options?.AdditionalParameters?.ContainsKey("timeout_seconds") == true)
            {
                if (options.AdditionalParameters["timeout_seconds"] is int customTimeout)
                {
                    timeoutMs = customTimeout * 1000;
                    Debug("Custom timeout override: {0}ms ({1}s)", timeoutMs, customTimeout);
                }
            }

            // 创建与生命周期管理器集成的取消令牌，但不使用using语句
            var cts = CancellationTokenSource.CreateLinkedTokenSource(
                cancellationToken,
                LifecycleManager.Instance.ApplicationToken
            );
            
            try
            {
                Debug("Setting CancellationToken timeout to {0}ms", timeoutMs);
                cts.CancelAfter(TimeSpan.FromMilliseconds(timeoutMs));

                // 检查是否应该使用缓存
                var cacheEnabled = _configuration.Get<bool>("cache.enabled", true);
                var shouldCache = cacheEnabled && _responseCache != null && ShouldCacheRequest(options);
                
                if (shouldCache)
                {
                    // 生成缓存键
                    var cacheKey = GenerateCacheKey(prompt, options);
                    var cacheExpiration = TimeSpan.FromMinutes(_configuration.Get<int>("cache.ttlMinutes", 15)); // 使用统一的配置键
                    
                    Debug("Attempting to use cache for request");
                    
                    // 使用缓存的异步工厂方法
                    return await _responseCache.GetOrAddAsync(
                        cacheKey,
                        async () => await ExecuteRequestInternal(prompt, options, cts.Token),
                        cacheExpiration,
                        cts.Token
                    );
                }
                else
                {
                    // 直接执行请求
                    return await ExecuteRequestInternal(prompt, options, cts.Token);
                }
            }
            catch (Exception ex)
            {
                Error("HTTP request exception: {0}", ex);
                Interlocked.Increment(ref _failedRequests);
                return null;
            }
            finally
            {
                // 确保 CancellationTokenSource 被正确释放
                cts?.Dispose();
            }
        }

        /// <summary>
        /// Send a request using the unified request model
        /// </summary>
        public async Task<LLMResponse> SendRequestAsync(UnifiedLLMRequest request)
        {
            if (request == null)
            {
                return LLMResponse.Failed("Request cannot be null");
            }

            return await _executor.ExecuteAsync(request);
        }

        /// <summary>
        /// Convenience method for creative requests
        /// </summary>
        public async Task<string> SendCreativeMessageAsync(string prompt, double temperature = 1.2, CancellationToken cancellationToken = default)
        {
            var options = LLMRequestOptions.Creative(temperature);
            return await SendMessageAsync(prompt, options, cancellationToken);
        }

        /// <summary>
        /// Convenience method for factual requests
        /// </summary>
        public async Task<string> SendFactualMessageAsync(string prompt, double temperature = 0.3, CancellationToken cancellationToken = default)
        {
            var options = LLMRequestOptions.Factual(temperature);
            return await SendMessageAsync(prompt, options, cancellationToken);
        }

        /// <summary>
        /// Convenience method for JSON requests
        /// </summary>
        public async Task<string> SendJsonMessageAsync(string prompt, object schema = null, double? temperature = null, CancellationToken cancellationToken = default)
        {
            var options = LLMRequestOptions.Json(schema, temperature);
            return await SendMessageAsync(prompt, options, cancellationToken);
        }

        /// <summary>
        /// Stream response method with real-time chunk processing
        /// </summary>
        public async Task<string> SendStreamingMessageAsync(string prompt, Action<string> onChunkReceived = null, LLMRequestOptions options = null, CancellationToken cancellationToken = default)
        {
            if (_disposed)
            {
                Debug("SendStreamingMessage called on disposed LLMManager");
                return null;
            }
            
            if (!ValidateRequest(prompt))
            {
                // 不增加失败计数，因为这是验证失败，不是请求失败
                return null;
            }

            // 只有真正发起请求时才增加计数
            Interlocked.Increment(ref _totalRequests);

            // 流式请求不使用缓存
            var streamingOptions = options ?? new LLMRequestOptions();
            streamingOptions.EnableStreaming = true;

            using var cts = CancellationTokenSource.CreateLinkedTokenSource(
                cancellationToken,
                LifecycleManager.Instance.ApplicationToken
            );

            var timeoutMs = _configuration.Get<int>("performance.timeoutSeconds", 30) * 1000; // 转换为毫秒
            cts.CancelAfter(TimeSpan.FromMilliseconds(timeoutMs));

            return await ExecuteRequestInternal(prompt, streamingOptions, cts.Token, onChunkReceived);
        }

        /// <summary>
        /// Stream response method with real-time chunk processing (alternative name for compatibility)
        /// </summary>
        public async Task<string> SendMessageStreamAsync(string prompt, Action<string> onChunkReceived = null, LLMRequestOptions options = null, CancellationToken cancellationToken = default)
        {
            return await SendStreamingMessageAsync(prompt, onChunkReceived, options, cancellationToken);
        }

        /// <summary>
        /// Get chat completion (alternative interface for compatibility)
        /// </summary>
        public async Task<string> GetChatCompletionAsync(string prompt, CancellationToken cancellationToken = default)
        {
            return await SendMessageAsync(prompt, null, cancellationToken);
        }

        /// <summary>
        /// Get streaming chat completion (alternative interface for compatibility)
        /// </summary>
        public async Task<string> GetChatCompletionStreamAsync(string prompt, Action<string> onChunkReceived = null, CancellationToken cancellationToken = default)
        {
            return await SendStreamingMessageAsync(prompt, onChunkReceived, null, cancellationToken);
        }

        /// <summary>
        /// Test API connection with enhanced diagnostics
        /// </summary>
        public async Task<(bool success, string message)> TestConnectionAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                if (_disposed)
                {
                    return (false, "LLMManager is disposed");
                }

                // UI层检查：优先检查用户实际设置的API key
                var userSettings = _settingsManager.GetSettings();
                if (string.IsNullOrEmpty(userSettings?.apiKey))
                {
                    // UI层友好提示，不记录错误
                    Debug("TestConnection: User has not configured API key");
                    return (false, "Please enter your API key in the settings before testing the connection.");
                }

                // 详细诊断信息（只在Debug模式下记录）
                Debug("=== Connection Test Diagnostics ===");
                Debug("User API key configured (length: {0})", userSettings.apiKey.Length);
                Debug("User API endpoint: {0}", userSettings.apiEndpoint ?? "default");
                Debug("User model: {0}", userSettings.modelName ?? "default");
                
                // 记录完整的诊断信息用于调试
                var diagnosticInfo = new StringBuilder();
                diagnosticInfo.AppendLine("Connection test starting...");
                diagnosticInfo.AppendLine($"✅ API key configured (length: {userSettings.apiKey.Length})");
                diagnosticInfo.AppendLine($"✅ API endpoint: {userSettings.apiEndpoint ?? "https://api.openai.com/v1"}");
                diagnosticInfo.AppendLine($"✅ Model: {userSettings.modelName ?? "gpt-4o"}");
                
                // 检查HttpClient状态
                if (_httpClient == null)
                {
                    diagnosticInfo.AppendLine("❌ HttpClient is null");
                    return (false, "HttpClient not initialized. Please restart the game.");
                }
                diagnosticInfo.AppendLine("✅ HttpClient initialized");
                
                Debug("Connection test diagnostics:\n{0}", diagnosticInfo.ToString());

                // 发送测试请求 - 直接调用内部方法绕过缓存
                Info("Starting connection test...");
                // 使用带时间戳的唯一测试提示，确保不会命中缓存
                var testPrompt = $"Say 'test' - {DateTime.UtcNow.Ticks}";
                var testOptions = new LLMRequestOptions
                {
                    Temperature = 0.1,
                    MaxTokens = 5,
                    AdditionalParameters = new Dictionary<string, object>
                    {
                        ["timeout_seconds"] = 30,
                        ["test_connection"] = true // 标记为测试连接，确保不使用缓存
                    }
                };
                
                var startTime = DateTime.UtcNow;
                // 重要：直接调用 ExecuteRequestInternal 绕过所有缓存
                var result = await ExecuteRequestInternal(testPrompt, testOptions, cancellationToken);
                var elapsed = DateTime.UtcNow - startTime;
                
                if (!string.IsNullOrEmpty(result))
                {
                    Info("Connection test successful in {0}ms", elapsed.TotalMilliseconds);
                    return (true, "Connection test successful");
                }
                else
                {
                    // 这不是错误，只是测试失败
                    Warning("Connection test failed: No response received");
                    return (false, "No response received from API. Please check your API key and endpoint.");
                }
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                Debug("Connection test was cancelled");
                return (false, "Connection test was cancelled");
            }
            catch (OperationCanceledException)
            {
                Warning("Connection test timed out");
                return (false, "Connection test timed out. Please check your network connection.");
            }
            catch (HttpRequestException ex)
            {
                // 网络错误才使用Error级别
                Error("Connection test network error: {0}", ex.Message);
                return (false, $"Network error: {ex.Message}");
            }
            catch (Exception ex)
            {
                // 未预期的错误使用Error级别
                Error("Connection test unexpected error: {0}", ex);
                return (false, $"Unexpected error: {ex.Message}");
            }
        }

        /// <summary>
        /// Cancels all pending requests (for backward compatibility)
        /// </summary>
        public void CancelAllRequests()
        {
            Info("Cancel all requests called - using unified architecture, cancellation is per-request");
        }

        #endregion

        #region Statistics and Monitoring

        /// <summary>
        /// 获取LLMManager的统计信息
        /// </summary>
        public LLMManagerStats GetStats()
        {
            return new LLMManagerStats
            {
                TotalRequests = _totalRequests,
                SuccessfulRequests = _successfulRequests,
                FailedRequests = _failedRequests,
                SuccessRate = SuccessRate,
                LastRequestTime = _lastRequestTime,
                IsHealthy = !_disposed && _executor != null,
                ConnectionId = _connectionId,
                CacheEnabled = _responseCache != null,
                CacheStats = _responseCache?.GetStats()
            };
        }

        /// <summary>
        /// Refreshes the settings from the mod configuration.
        /// Call this when settings are changed.
        /// </summary>
        public void RefreshSettings()
        {
            _settingsManager.RefreshSettings();
            Info("RimAI Framework: Settings refreshed successfully. Some services may require restart to apply new settings.");
        }

        #endregion

        #region Private Helper Methods

        /// <summary>
        /// 内部执行请求的方法，处理实际的API调用
        /// </summary>
        private async Task<string> ExecuteRequestInternal(string prompt, LLMRequestOptions options, CancellationToken cancellationToken, Action<string> onChunkReceived = null)
        {
            var request = new UnifiedLLMRequest
            {
                Prompt = prompt,
                Options = options ?? new LLMRequestOptions(),
                IsStreaming = options?.EnableStreaming ?? false,
                CancellationToken = cancellationToken,
                RequestId = Guid.NewGuid().ToString()
            };

            try
            {
                // 更新统计信息
                Interlocked.Increment(ref _totalRequests);
                _lastRequestTime = DateTime.UtcNow;
                
                // 更新连接池活动时间
                ConnectionPoolManager.Instance.UpdateConnectionActivity(_connectionId);

                LLMResponse response;
                
                if (onChunkReceived != null && request.IsStreaming)
                {
                    // 流式处理
                    var fullResponse = new StringBuilder();
                    request.OnChunkReceived = (chunk) =>
                    {
                        fullResponse.Append(chunk);
                        onChunkReceived(chunk);
                    };
                    
                    response = await _executor.ExecuteAsync(request);
                    
                    if (response.IsSuccess)
                    {
                        Interlocked.Increment(ref _successfulRequests);
                        return fullResponse.ToString();
                    }
                }
                else
                {
                    // 普通处理
                    response = await _executor.ExecuteAsync(request);
                    
                    if (response.IsSuccess)
                    {
                        Interlocked.Increment(ref _successfulRequests);
                        return response.Content;
                    }
                }
                
                Interlocked.Increment(ref _failedRequests);
                Warning("Request failed: {0}", response.Error);
                return null;
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                Interlocked.Increment(ref _failedRequests);
                Debug("Request was cancelled");
                throw;
            }
            catch (Exception ex)
            {
                Interlocked.Increment(ref _failedRequests);
                Error("Unexpected error in request execution: {0}", ex.Message);
                return null;
            }
        }
        
        /// <summary>
        /// 判断是否应该缓存请求
        /// </summary>
        private bool ShouldCacheRequest(LLMRequestOptions options)
        {
            // 游戏启动时的优化：前75000个tick（约30秒）不缓存
            if (Find.TickManager != null && Find.TickManager.TicksGame < 75000)
            {
                Debug("Skipping cache during game startup (tick {0})", Find.TickManager.TicksGame);
                return false;
            }
            
            // 流式请求不缓存
            if (options?.EnableStreaming == true)
                return false;
                
            // 高温度的创意请求不缓存（温度 > 1.0）
            if (options?.Temperature > 1.0)
                return false;
                
            // 包含随机性的请求不缓存
            if (options?.AdditionalParameters?.ContainsKey("seed") == true)
                return false;
            
            // 内存压力检查
            if (_responseCache != null)
            {
                var stats = _responseCache.GetStats();
                if (stats.EntryCount >= stats.MaxSize * 0.95)
                {
                    Debug("Skipping cache due to size limit: {0}/{1}", stats.EntryCount, stats.MaxSize);
                    return false;
                }
            }
            
            return true;
        }
        
        /// <summary>
        /// 生成缓存键
        /// </summary>
        private string GenerateCacheKey(string prompt, LLMRequestOptions options)
        {
            var keyBuilder = new StringBuilder();
            keyBuilder.Append("LLM:");
            keyBuilder.Append(prompt?.GetHashCode().ToString() ?? "null");
            
            if (options != null)
            {
                keyBuilder.Append($":temp={options.Temperature}");
                keyBuilder.Append($":maxtok={options.MaxTokens}");
                keyBuilder.Append($":model={options.Model ?? "default"}");
                keyBuilder.Append($":json={options.ForceJsonMode}");
                
                if (options.JsonSchema != null)
                {
                    keyBuilder.Append($":schema={options.JsonSchema.GetHashCode()}");
                }
                
                if (options.TopP.HasValue)
                {
                    keyBuilder.Append($":topp={options.TopP}");
                }
            }
            
            return keyBuilder.ToString();
        }

        /// <summary>
        /// Validates request parameters
        /// </summary>
        private bool ValidateRequest(string prompt)
        {
            if (string.IsNullOrWhiteSpace(prompt))
            {
                Warning("Empty prompt provided to SendMessageAsync.");
                return false;
            }

            if (_executor == null)
            {
                Error("Executor not initialized. LLMManager may be in an invalid state.");
                return false;
            }

            var settings = _settingsManager.GetSettings();
            if (string.IsNullOrEmpty(settings?.apiKey))
            {
                Debug("API key is not configured. Request will be rejected.");
                return false;
            }

            return true;
        }

        #endregion
        
        #region Statistics and Cache Management

        /// <summary>
        /// 获取LLM Manager的统计信息
        /// </summary>
        /// <returns>包含统计信息的字典</returns>
        public Dictionary<string, object> GetStatistics()
        {
            var isHealthy = !_disposed && _executor != null;
            
            var stats = new Dictionary<string, object>
            {
                ["TotalRequests"] = _totalRequests,
                ["SuccessfulRequests"] = _successfulRequests,
                ["FailedRequests"] = _failedRequests,
                ["SuccessRate"] = _totalRequests > 0 ? (double)_successfulRequests / _totalRequests : 0.0,
                ["LastRequestTime"] = _lastRequestTime,
                ["IsHealthy"] = isHealthy,
                ["ConnectionId"] = _connectionId ?? "Unknown"
            };

            // 添加缓存统计信息
            if (_responseCache != null)
            {
                var cacheStats = _responseCache.GetStats();
                stats["CacheHitRate"] = cacheStats.HitRate;
                stats["CacheEntryCount"] = cacheStats.EntryCount;
                stats["CacheHits"] = cacheStats.CacheHits;
                stats["CacheMisses"] = cacheStats.CacheMisses;
            }

            return stats;
        }

        /// <summary>
        /// 清空响应缓存
        /// </summary>
        public void ClearCache()
        {
            try
            {
                _responseCache?.Clear();
                Info("Response cache cleared successfully");
            }
            catch (Exception ex)
            {
                Error("Failed to clear response cache: {0}", ex.Message);
            }
        }

        #endregion

        #region IDisposable Implementation

        /// <summary>
        /// Releases all resources used by the LLMManager.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Protected implementation of Dispose pattern.
        /// </summary>
        /// <param name="disposing">true if disposing managed resources.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (_disposed)
                return;
                
            if (disposing)
            {
                try
                {
                    // 从连接池中注销连接
                    if (!string.IsNullOrEmpty(_connectionId))
                    {
                        ConnectionPoolManager.Instance.UnregisterConnection(_connectionId);
                    }
                    
                    _executor?.Dispose();
                    
                    // 注意：不要释放 _httpClient，因为它是由 HttpClientFactory 管理的共享实例
                    // _httpClient?.Dispose(); 
                    
                    // 不要释放配置和缓存系统，它们是全局单例
                    // _configuration?.Dispose();
                    // _responseCache?.Dispose();
                    
                    Info("LLMManager disposed. Final stats - Total: {0}, Success: {1}, Failed: {2}",
                        _totalRequests, _successfulRequests, _failedRequests);
                }
                catch (Exception ex)
                {
                    Error("Error during LLMManager disposal: {0}", ex.Message);
                }
            }
            
            _disposed = true;
        }

        #endregion
    }
    
    /// <summary>
    /// LLMManager统计信息
    /// </summary>
    public class LLMManagerStats
    {
        public int TotalRequests { get; set; }
        public int SuccessfulRequests { get; set; }
        public int FailedRequests { get; set; }
        public double SuccessRate { get; set; }
        public DateTime LastRequestTime { get; set; }
        public bool IsHealthy { get; set; }
        public string ConnectionId { get; set; }
        public bool CacheEnabled { get; set; }
        public Cache.CacheStats CacheStats { get; set; }
    }
}
