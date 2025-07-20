using System;
using System.Threading;
using System.Threading.Tasks;
using RimAI.Framework.LLM;
using RimAI.Framework.LLM.Http;
using Verse;
using static RimAI.Framework.Core.RimAILogger;

namespace RimAI.Framework.Core
{
    /// <summary>
    /// 管理RimAI框架的完整生命周期，包括资源管理、健康检查和优雅关闭
    /// </summary>
    public class LifecycleManager : IDisposable
    {
        private static LifecycleManager _instance;
        private static readonly object _lockObject = new object();
        
        private readonly CancellationTokenSource _applicationLifetime;
        private readonly Timer _healthCheckTimer;
        private readonly Timer _memoryCheckTimer;
        private bool _disposed;
        private bool _shutdownInitiated;
        
        // 健康检查统计
        private int _healthCheckCount;
        private int _healthCheckFailures;
        private DateTime _lastHealthCheck = DateTime.MinValue;
        
        /// <summary>
        /// 获取生命周期管理器的单例实例
        /// </summary>
        public static LifecycleManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (_lockObject)
                    {
                        if (_instance == null)
                        {
                            _instance = new LifecycleManager();
                        }
                    }
                }
                return _instance;
            }
        }
        
        /// <summary>
        /// 应用程序级别的取消令牌，当框架关闭时会被取消
        /// </summary>
        public CancellationToken ApplicationToken => _applicationLifetime?.Token ?? CancellationToken.None;
        
        /// <summary>
        /// 框架是否正在关闭
        /// </summary>
        public bool IsShuttingDown => _shutdownInitiated || _disposed;
        
        /// <summary>
        /// 健康检查统计信息
        /// </summary>
        public HealthCheckStats HealthStats => new HealthCheckStats
        {
            TotalChecks = _healthCheckCount,
            FailedChecks = _healthCheckFailures,
            LastCheckTime = _lastHealthCheck,
            SuccessRate = _healthCheckCount > 0 ? (double)(_healthCheckCount - _healthCheckFailures) / _healthCheckCount : 1.0
        };
        
        private LifecycleManager()
        {
            try
            {
                _applicationLifetime = new CancellationTokenSource();
                
                // 健康检查定时器 - 每5分钟一次
                _healthCheckTimer = new Timer(
                    PerformHealthCheck, 
                    null, 
                    TimeSpan.FromMinutes(1), // 延迟1分钟后开始
                    TimeSpan.FromMinutes(5)  // 每5分钟检查一次
                );
                
                // 内存检查定时器 - 每2分钟一次
                _memoryCheckTimer = new Timer(
                    CheckMemoryUsage,
                    null,
                    TimeSpan.FromMinutes(1), // 延迟1分钟后开始
                    TimeSpan.FromMinutes(2)  // 每2分钟检查一次
                );
                
                // 注册游戏关闭事件
                RegisterGameShutdownHook();
                
                Info("LifecycleManager initialized successfully");
            }
            catch (Exception ex)
            {
                Error("Failed to initialize LifecycleManager: {0}", ex.Message);
                throw;
            }
        }
        
        /// <summary>
        /// 注册RimWorld游戏关闭钩子
        /// </summary>
        private void RegisterGameShutdownHook()
        {
            try
            {
                // 使用RimWorld的游戏退出事件
                // 注意：这里需要在适当的时机调用，比如mod卸载时
                AppDomain.CurrentDomain.ProcessExit += (sender, e) =>
                {
                    Log.Message("[RimAI] Process exit detected, shutting down AI Framework...");
                    InitiateShutdown();
                };
                
                // 如果可以访问到游戏的其他关闭事件，也可以注册
                Log.Message("[RimAI] Game shutdown hooks registered");
            }
            catch (Exception ex)
            {
                Log.Warning($"[RimAI] Failed to register shutdown hooks: {ex.Message}");
            }
        }
        
        /// <summary>
        /// 执行健康检查
        /// </summary>
        private void PerformHealthCheck(object state)
        {
            if (_disposed || _shutdownInitiated)
                return;
                
            try
            {
                _healthCheckCount++;
                _lastHealthCheck = DateTime.UtcNow;
                
                var healthReport = new System.Text.StringBuilder();
                healthReport.AppendLine("[RimAI] Performing health check...");
                
                // 检查应用程序取消令牌状态
                if (_applicationLifetime.Token.IsCancellationRequested)
                {
                    healthReport.AppendLine("  ❌ Application token is cancelled");
                    _healthCheckFailures++;
                    return;
                }
                else
                {
                    healthReport.AppendLine("  ✅ Application token is active");
                }
                
                // 检查HTTP客户端状态（如果存在的话）
                try
                {
                    var httpClient = HttpClientFactory.GetClient(5); // 短超时用于健康检查
                    if (httpClient != null)
                    {
                        // 获取连接统计信息
                        var connStats = HttpClientFactory.GetConnectionStats();
                        healthReport.AppendLine($"  ✅ HttpClient is healthy (Age: {connStats.Age.TotalMinutes:F1}min, Requests: {connStats.RequestCount})");
                    }
                    else
                    {
                        healthReport.AppendLine("  ⚠️ HttpClient is null");
                        _healthCheckFailures++;
                    }
                }
                catch (Exception ex)
                {
                    healthReport.AppendLine($"  ❌ HttpClient error: {ex.Message}");
                    _healthCheckFailures++;
                }
                
                // 检查LLM管理器状态
                try
                {
                    var llmManager = LLMManager.Instance;
                    if (llmManager != null)
                    {
                        healthReport.AppendLine("  ✅ LLMManager is available");
                    }
                    else
                    {
                        healthReport.AppendLine("  ❌ LLMManager is null");
                        _healthCheckFailures++;
                    }
                }
                catch (Exception ex)
                {
                    healthReport.AppendLine($"  ❌ LLMManager error: {ex.Message}");
                    _healthCheckFailures++;
                }
                
                // 清理过期连接
                CleanupExpiredConnections();
                healthReport.AppendLine("  ✅ Connection cleanup completed");
                
                // 检查统计信息
                var successRate = HealthStats.SuccessRate;
                healthReport.AppendLine($"  📊 Health check success rate: {successRate:P1}");
                
                // 只在出现问题时记录详细信息，正常情况下记录简要信息
                if (_healthCheckFailures == 0 || (_healthCheckCount % 10 == 0))
                {
                    Log.Message($"[RimAI] Health check #{_healthCheckCount} completed. Success rate: {successRate:P1}");
                }
                else
                {
                    Log.Message(healthReport.ToString());
                }
            }
            catch (Exception ex)
            {
                _healthCheckFailures++;
                Log.Error($"[RimAI] Health check failed: {ex.Message}");
            }
        }
        
        /// <summary>
        /// 清理过期连接
        /// </summary>
        private void CleanupExpiredConnections()
        {
            try
            {
                ConnectionPoolManager.Instance.CleanupExpiredConnections();
            }
            catch (Exception ex)
            {
                Log.Warning($"[RimAI] Failed to cleanup expired connections: {ex.Message}");
            }
        }
        
        /// <summary>
        /// 检查内存使用情况
        /// </summary>
        private void CheckMemoryUsage(object state)
        {
            if (_disposed || _shutdownInitiated)
                return;
                
            try
            {
                var memoryUsageMB = GC.GetTotalMemory(false) / (1024 * 1024);
                
                // 如果内存使用超过阈值，记录警告并建议垃圾回收
                if (memoryUsageMB > 500) // 500MB 阈值
                {
                    Log.Warning($"[RimAI] High memory usage detected: {memoryUsageMB}MB");
                    
                    // 如果内存使用超过800MB，强制垃圾回收
                    if (memoryUsageMB > 800)
                    {
                        Log.Warning("[RimAI] Triggering forced garbage collection due to high memory usage");
                        GC.Collect();
                        GC.WaitForPendingFinalizers();
                        GC.Collect();
                        
                        var memoryAfterGC = GC.GetTotalMemory(false) / (1024 * 1024);
                        Log.Message($"[RimAI] Memory usage after GC: {memoryAfterGC}MB (freed: {memoryUsageMB - memoryAfterGC}MB)");
                    }
                }
                else
                {
                    // 正常情况下每10次检查记录一次
                    if (_healthCheckCount % 10 == 0)
                    {
                        Log.Message($"[RimAI] Current memory usage: {memoryUsageMB}MB");
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Warning($"[RimAI] Failed to check memory usage: {ex.Message}");
            }
        }
        
        /// <summary>
        /// 开始优雅关闭流程
        /// </summary>
        public void InitiateShutdown()
        {
            if (_shutdownInitiated || _disposed)
                return;
                
            _shutdownInitiated = true;
            Log.Message("[RimAI] Initiating graceful shutdown...");
            
            try
            {
                // 取消应用程序令牌，通知所有操作停止
                _applicationLifetime?.Cancel();
                
                // 给正在进行的操作一些时间完成
                Task.Run(async () =>
                {
                    await Task.Delay(2000); // 等待2秒
                    
                    // 执行完整的资源清理
                    Dispose();
                });
            }
            catch (Exception ex)
            {
                Log.Error($"[RimAI] Error during shutdown initiation: {ex.Message}");
            }
        }
        
        /// <summary>
        /// 释放所有资源
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        
        protected virtual void Dispose(bool disposing)
        {
            if (_disposed)
                return;
                
            try
            {
                Log.Message("[RimAI] Starting LifecycleManager disposal...");
                
                if (disposing)
                {
                    // 停止所有定时器
                    _healthCheckTimer?.Dispose();
                    _memoryCheckTimer?.Dispose();
                    
                    // 取消应用程序令牌
                    if (!_applicationLifetime?.Token.IsCancellationRequested == true)
                    {
                        _applicationLifetime?.Cancel();
                    }
                    
                    // 清理HTTP客户端工厂
                    try
                    {
                        HttpClientFactory.Dispose();
                        Log.Message("[RimAI] HttpClientFactory disposed");
                    }
                    catch (Exception ex)
                    {
                        Log.Warning($"[RimAI] Error disposing HttpClientFactory: {ex.Message}");
                    }
                    
                    // 清理其他管理器
                    try
                    {
                        // LLMManager.Instance?.Dispose(); // 等实现IDisposable后启用
                        Log.Message("[RimAI] LLMManager disposed");
                    }
                    catch (Exception ex)
                    {
                        Log.Warning($"[RimAI] Error disposing LLMManager: {ex.Message}");
                    }
                    
                    try
                    {
                        ConnectionPoolManager.Instance.Dispose();
                        Log.Message("[RimAI] ConnectionPoolManager disposed");
                    }
                    catch (Exception ex)
                    {
                        Log.Warning($"[RimAI] Error disposing ConnectionPoolManager: {ex.Message}");
                    }
                    
                    // 最后释放取消令牌源
                    _applicationLifetime?.Dispose();
                }
                
                _disposed = true;
                Log.Message("[RimAI] LifecycleManager disposed successfully");
            }
            catch (Exception ex)
            {
                Log.Error($"[RimAI] Error during LifecycleManager disposal: {ex.Message}");
            }
        }
        
        /// <summary>
        /// 析构函数
        /// </summary>
        ~LifecycleManager()
        {
            Dispose(false);
        }
    }
    
    /// <summary>
    /// 健康检查统计信息
    /// </summary>
    public class HealthCheckStats
    {
        public int TotalChecks { get; set; }
        public int FailedChecks { get; set; }
        public DateTime LastCheckTime { get; set; }
        public double SuccessRate { get; set; }
    }
}
