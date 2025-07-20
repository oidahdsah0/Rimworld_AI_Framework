using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using RimAI.Framework.Configuration;
using Verse;

namespace RimAI.Framework.LLM.Http
{
    /// <summary>
    /// 改进的HTTP客户端工厂，支持DNS刷新、连接管理和性能优化
    /// </summary>
    public static class HttpClientFactory
    {
        private static readonly object _lock = new object();
        private static ManagedHttpClient _managedClient;
        private static Timer _dnsRefreshTimer;
        
        static HttpClientFactory()
        {
            // 定期刷新 DNS 和 HttpClient（解决 DNS 缓存问题）
            _dnsRefreshTimer = new Timer(
                _ => RefreshHttpClient(), 
                null, 
                TimeSpan.FromHours(1),  // 1小时后开始
                TimeSpan.FromHours(1)   // 每小时刷新一次
            );
            
            Log.Message("[RimAI] HttpClientFactory initialized with DNS refresh timer");
        }
        
        /// <summary>
        /// 从配置系统获取超时设置
        /// </summary>
        /// <returns>超时时间（秒）</returns>
        private static int GetTimeoutFromConfiguration()
        {
            try
            {
                var config = RimAIConfiguration.Instance;
                return config.Get<int>("performance.timeoutSeconds", 30);
            }
            catch (Exception ex)
            {
                Log.Warning($"[RimAI] Failed to read timeout from configuration: {ex.Message}");
                return 30; // 回退到默认值
            }
        }
        
        /// <summary>
        /// 获取管理的HttpClient实例（推荐使用这个方法）
        /// </summary>
        /// <param name="timeoutSeconds">请求超时时间（秒），如果为null则从配置读取</param>
        /// <returns>管理的HttpClient实例</returns>
        public static HttpClient GetClient(int? timeoutSeconds = null)
        {
            // 从配置系统获取默认超时 - CRITICAL FIX
            var actualTimeout = timeoutSeconds ?? GetTimeoutFromConfiguration();
            
            if (_managedClient == null || _managedClient.ShouldRefresh)
            {
                lock (_lock)
                {
                    if (_managedClient == null || _managedClient.ShouldRefresh)
                    {
                        var oldClient = _managedClient;
                        _managedClient = new ManagedHttpClient(actualTimeout);
                        
                        if (oldClient != null)
                        {
                            // 延迟销毁旧客户端，确保正在进行的请求完成
                            Task.Delay(TimeSpan.FromMinutes(2)).ContinueWith(_ => 
                            {
                                try
                                {
                                    oldClient.Dispose();
                                    Log.Message("[RimAI] Old HttpClient disposed after grace period");
                                }
                                catch (Exception ex)
                                {
                                    Log.Warning($"[RimAI] Error disposing old HttpClient: {ex.Message}");
                                }
                            });
                        }
                        
                        Log.Message("[RimAI] New managed HttpClient created");
                    }
                }
            }
            
            return _managedClient.Client;
        }
        
        /// <summary>
        /// 创建新的HttpClient实例（兼容旧版本API）
        /// </summary>
        /// <param name="timeoutSeconds">Request timeout in seconds</param>
        /// <returns>Configured HttpClient instance</returns>
        [Obsolete("Use GetClient() instead for better connection management")]
        public static HttpClient CreateClient(int timeoutSeconds = 60)
        {
            return GetClient(timeoutSeconds);
        }
        
        /// <summary>
        /// 手动刷新HttpClient（强制创建新的连接）
        /// </summary>
        public static void RefreshHttpClient()
        {
            try
            {
                lock (_lock)
                {
                    if (_managedClient != null)
                    {
                        var oldClient = _managedClient;
                        _managedClient = null; // 这会触发下次GetClient时创建新实例
                        
                        // 延迟清理旧客户端
                        Task.Delay(TimeSpan.FromMinutes(1)).ContinueWith(_ =>
                        {
                            try
                            {
                                oldClient.Dispose();
                                Log.Message("[RimAI] HttpClient refreshed and old instance disposed");
                            }
                            catch (Exception ex)
                            {
                                Log.Warning($"[RimAI] Error during HttpClient refresh: {ex.Message}");
                            }
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error($"[RimAI] Failed to refresh HttpClient: {ex.Message}");
            }
        }
        
        /// <summary>
        /// 获取连接统计信息
        /// </summary>
        public static ConnectionStats GetConnectionStats()
        {
            lock (_lock)
            {
                if (_managedClient != null)
                {
                    return new ConnectionStats
                    {
                        CreatedAt = _managedClient.CreatedAt,
                        RequestCount = _managedClient.RequestCount,
                        IsHealthy = _managedClient.IsHealthy,
                        ShouldRefresh = _managedClient.ShouldRefresh,
                        Age = DateTime.UtcNow - _managedClient.CreatedAt
                    };
                }
                else
                {
                    return new ConnectionStats
                    {
                        CreatedAt = DateTime.MinValue,
                        RequestCount = 0,
                        IsHealthy = false,
                        ShouldRefresh = true,
                        Age = TimeSpan.Zero
                    };
                }
            }
        }
        
        /// <summary>
        /// 清理资源（在应用关闭时调用）
        /// </summary>
        public static void Dispose()
        {
            try
            {
                _dnsRefreshTimer?.Dispose();
                
                lock (_lock)
                {
                    _managedClient?.Dispose();
                    _managedClient = null;
                }
                
                Log.Message("[RimAI] HttpClientFactory disposed");
            }
            catch (Exception ex)
            {
                Log.Error($"[RimAI] Error disposing HttpClientFactory: {ex.Message}");
            }
        }
        
        /// <summary>
        /// 使用内置重试机制执行HTTP请求的帮助方法
        /// </summary>
        /// <param name="httpClient">HTTP客户端</param>
        /// <param name="requestFactory">请求工厂函数</param>
        /// <param name="maxRetries">最大重试次数</param>
        /// <param name="baseDelayMs">基础延迟毫秒数</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>HTTP响应</returns>
        public static async Task<HttpResponseMessage> ExecuteWithRetryAsync(
            HttpClient httpClient,
            Func<Task<HttpResponseMessage>> requestFactory,
            int maxRetries = 3,
            int baseDelayMs = 1000,
            CancellationToken cancellationToken = default)
        {
            Exception lastException = null;
            
            for (int attempt = 0; attempt <= maxRetries; attempt++)
            {
                try
                {
                    var response = await requestFactory();
                    
                    // 如果响应成功或者是客户端错误（不值得重试），直接返回
                    if (response.IsSuccessStatusCode || 
                        ((int)response.StatusCode >= 400 && (int)response.StatusCode < 500))
                    {
                        return response;
                    }
                    
                    // 服务器错误，记录并考虑重试
                    lastException = new HttpRequestException($"Server error: {response.StatusCode}");
                    response.Dispose();
                    
                    if (attempt == maxRetries)
                        break;
                        
                    Log.Warning($"[RimAI] HTTP request failed with {response.StatusCode}, attempt {attempt + 1}/{maxRetries + 1}");
                }
                catch (TaskCanceledException ex) when (ex.CancellationToken.IsCancellationRequested)
                {
                    // 请求被取消，不重试
                    throw;
                }
                catch (Exception ex)
                {
                    lastException = ex;
                    
                    if (attempt == maxRetries)
                        break;
                        
                    Log.Warning($"[RimAI] HTTP request failed: {ex.Message}, attempt {attempt + 1}/{maxRetries + 1}");
                }
                
                // 指数退避延迟
                if (attempt < maxRetries)
                {
                    var delay = TimeSpan.FromMilliseconds(baseDelayMs * Math.Pow(2, attempt));
                    await Task.Delay(delay, cancellationToken);
                }
            }
            
            // 所有重试都失败了，抛出最后的异常
            throw lastException ?? new HttpRequestException("Request failed after all retries");
        }
    }
    
    /// <summary>
    /// 管理的HttpClient包装器
    /// </summary>
    internal class ManagedHttpClient : IDisposable
    {
        private readonly HttpClientHandler _handler;
        private int _requestCount;
        private bool _disposed;
        
        public HttpClient Client { get; private set; }
        public DateTime CreatedAt { get; private set; }
        public bool IsHealthy => !_disposed && Client != null;
        public int RequestCount => _requestCount;
        
        /// <summary>
        /// 是否应该刷新（24小时后或请求次数过多）
        /// </summary>
        public bool ShouldRefresh => 
            DateTime.UtcNow - CreatedAt > TimeSpan.FromHours(24) ||
            _requestCount > 10000; // 超过1万次请求后刷新
        
        public ManagedHttpClient(int timeoutSeconds = 30)
        {
            CreatedAt = DateTime.UtcNow;
            _requestCount = 0;
            
            try
            {
                // 尝试使用自定义HttpClientHandler - 但要安全地处理不支持的功能
                try
                {
                    _handler = new HttpClientHandler();
                    
                    // 安全地尝试配置各项功能，捕获NotImplementedException
                    try
                    {
                        _handler.ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) => true;
                    }
                    catch (NotImplementedException)
                    {
                        Log.Message("[RimAI] SSL validation bypass not supported on this platform");
                    }
                    catch (Exception ex)
                    {
                        Log.Warning($"[RimAI] Could not configure SSL validation bypass: {ex.Message}");
                    }
                    
                    try
                    {
                        _handler.MaxConnectionsPerServer = 10;
                    }
                    catch (NotImplementedException)
                    {
                        Log.Message("[RimAI] MaxConnectionsPerServer not supported on this platform");
                    }
                    catch (Exception ex)
                    {
                        Log.Warning($"[RimAI] Could not configure MaxConnectionsPerServer: {ex.Message}");
                    }
                    
                    try
                    {
                        _handler.UseProxy = false;
                    }
                    catch (NotImplementedException)
                    {
                        Log.Message("[RimAI] Proxy configuration not supported on this platform");
                    }
                    catch (Exception ex)
                    {
                        Log.Warning($"[RimAI] Could not configure proxy settings: {ex.Message}");
                    }
                    
                    Client = new HttpClient(_handler);
                }
                catch (NotImplementedException ex)
                {
                    Log.Warning($"[RimAI] HttpClientHandler not fully supported on this platform: {ex.Message}");
                    // 回退到基本HttpClient
                    _handler?.Dispose();
                    _handler = null;
                    Client = new HttpClient();
                }
                
                ConfigureClient(timeoutSeconds);
                
                Log.Message($"[RimAI] ManagedHttpClient created successfully with {timeoutSeconds}s timeout");
            }
            catch (Exception ex)
            {
                Log.Error($"[RimAI] Failed to create ManagedHttpClient with custom handler: {ex.Message}");
                
                // 完全回退到基本配置 - 最后的安全措施
                try
                {
                    _handler?.Dispose();
                    _handler = null;
                    Client?.Dispose();
                    Client = new HttpClient();
                    ConfigureClient(timeoutSeconds);
                    Log.Message("[RimAI] Successfully created basic HttpClient as fallback");
                }
                catch (Exception fallbackEx)
                {
                    Log.Error($"[RimAI] Even fallback HttpClient creation failed: {fallbackEx.Message}");
                    throw; // 这时候真的有大问题了
                }
            }
        }
        
        private void ConfigureClient(int timeoutSeconds)
        {
            try
            {
                Client.Timeout = TimeSpan.FromSeconds(timeoutSeconds);
                Client.DefaultRequestHeaders.Clear();
                Client.DefaultRequestHeaders.Add("User-Agent", "RimAI-Framework/3.0");
                Client.DefaultRequestHeaders.Add("Accept", "application/json");
                Client.DefaultRequestHeaders.Add("Accept-Encoding", "gzip, deflate");
                
                // 禁用连接复用的keep-alive以避免某些网络问题
                Client.DefaultRequestHeaders.ConnectionClose = false;
                
                Log.Message($"[RimAI] HttpClient configured successfully with timeout: {timeoutSeconds} seconds ({timeoutSeconds * 1000}ms)");
            }
            catch (Exception ex)
            {
                Log.Warning($"[RimAI] Error configuring HttpClient: {ex.Message}");
            }
        }
        
        /// <summary>
        /// 更新请求计数（应该在发送请求时调用）
        /// </summary>
        public void IncrementRequestCount()
        {
            Interlocked.Increment(ref _requestCount);
        }
        
        public void Dispose()
        {
            if (_disposed) return;
            
            try
            {
                Client?.Dispose();
                _handler?.Dispose();
                _disposed = true;
                
                Log.Message($"[RimAI] ManagedHttpClient disposed (served {_requestCount} requests)");
            }
            catch (Exception ex)
            {
                Log.Warning($"[RimAI] Error disposing ManagedHttpClient: {ex.Message}");
            }
        }
    }
    
    /// <summary>
    /// 连接统计信息
    /// </summary>
    public class ConnectionStats
    {
        public DateTime CreatedAt { get; set; }
        public int RequestCount { get; set; }
        public bool IsHealthy { get; set; }
        public bool ShouldRefresh { get; set; }
        public TimeSpan Age { get; set; }
    }
}
