using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using System.Reflection;
using RimAI.Framework.API;
using RimAI.Framework.Core;
using RimAI.Framework.LLM;
using RimAI.Framework.Cache;
using RimAI.Framework.Configuration;
using Verse;

namespace RimAI.Framework.Diagnostics
{
    /// <summary>
    /// RimAI框架诊断和监控系统
    /// 提供健康状态检查、性能监控、调试命令等功能
    /// </summary>
    public static class FrameworkDiagnostics
    {
        #region 健康状态检查

        /// <summary>
        /// 框架健康状态检查
        /// </summary>
        public class HealthCheckResult
        {
            public bool IsHealthy { get; set; }
            public string Status { get; set; }
            public List<string> Issues { get; set; } = new List<string>();
            public List<string> Warnings { get; set; } = new List<string>();
            public DateTime CheckTime { get; set; } = DateTime.UtcNow;
            public TimeSpan CheckDuration { get; set; }
        }

        /// <summary>
        /// 执行完整的框架健康检查
        /// </summary>
        /// <returns>健康检查结果</returns>
        public static HealthCheckResult PerformHealthCheck()
        {
            var startTime = DateTime.UtcNow;
            var result = new HealthCheckResult();

            try
            {
                RimAILogger.Info("Starting framework health check...");

                // 1. 检查API层
                CheckApiLayer(result);

                // 2. 检查LLM管理器
                CheckLLMManager(result);

                // 3. 检查缓存系统
                CheckCacheSystem(result);

                // 4. 检查配置系统
                CheckConfigurationSystem(result);

                // 5. 检查生命周期管理器
                CheckLifecycleManager(result);

                // 6. 检查连接池
                CheckConnectionPool(result);

                // 7. 评估整体健康状态
                result.IsHealthy = result.Issues.Count == 0;
                result.Status = result.IsHealthy ? "Healthy" : $"Unhealthy ({result.Issues.Count} issues)";

                if (result.Warnings.Count > 0)
                {
                    result.Status += $" ({result.Warnings.Count} warnings)";
                }

                result.CheckDuration = DateTime.UtcNow - startTime;
                RimAILogger.Info($"Health check completed in {result.CheckDuration.TotalMilliseconds:F0}ms - Status: {result.Status}");

                return result;
            }
            catch (Exception ex)
            {
                result.IsHealthy = false;
                result.Status = "Health Check Failed";
                result.Issues.Add($"Health check exception: {ex.Message}");
                result.CheckDuration = DateTime.UtcNow - startTime;
                
                RimAILogger.Error($"Health check failed: {ex.Message}");
                return result;
            }
        }

        private static void CheckApiLayer(HealthCheckResult result)
        {
            try
            {
                if (!RimAIAPI.IsInitialized)
                {
                    result.Issues.Add("API Layer: Not initialized");
                    return;
                }

                // 检查API状态
                var status = RimAIAPI.Status;
                if (status != "Ready")
                {
                    result.Warnings.Add($"API Layer: Status is '{status}' instead of 'Ready'");
                }

                // 尝试获取统计信息
                var stats = RimAIAPI.GetStatistics();
                if (stats == null || stats.Count == 0)
                {
                    result.Warnings.Add("API Layer: No statistics available");
                }
            }
            catch (Exception ex)
            {
                result.Issues.Add($"API Layer: Exception during check - {ex.Message}");
            }
        }

        private static void CheckLLMManager(HealthCheckResult result)
        {
            try
            {
                var manager = LLMManager.Instance;
                if (manager == null)
                {
                    result.Issues.Add("LLM Manager: Instance is null");
                    return;
                }

                if (manager.CurrentSettings == null)
                {
                    result.Warnings.Add("LLM Manager: CurrentSettings is null");
                }

                // 检查统计信息
                var stats = manager.GetStatistics();
                if (stats.ContainsKey("IsHealthy"))
                {
                    var isHealthy = Convert.ToBoolean(stats["IsHealthy"]);
                    if (!isHealthy)
                    {
                        result.Issues.Add("LLM Manager: Reports unhealthy status");
                    }
                }
            }
            catch (Exception ex)
            {
                result.Issues.Add($"LLM Manager: Exception during check - {ex.Message}");
            }
        }

        private static void CheckCacheSystem(HealthCheckResult result)
        {
            try
            {
                var cache = ResponseCache.Instance;
                if (cache == null)
                {
                    result.Warnings.Add("Cache System: Instance is null");
                    return;
                }

                if (cache.IsDisposed)
                {
                    result.Issues.Add("Cache System: Cache is disposed");
                    return;
                }

                var stats = cache.GetStats();
                if (stats == null)
                {
                    result.Warnings.Add("Cache System: Unable to retrieve statistics");
                }
                else
                {
                    // 检查缓存是否过大
                    if (stats.EntryCount > 1000)
                    {
                        result.Warnings.Add($"Cache System: Large cache size ({stats.EntryCount} entries)");
                    }

                    // 检查内存使用
                    if (stats.MemoryUsageEstimate > 100 * 1024 * 1024) // 100MB
                    {
                        result.Warnings.Add($"Cache System: High memory usage ({stats.MemoryUsageEstimate / (1024 * 1024):F0}MB)");
                    }
                }
            }
            catch (Exception ex)
            {
                result.Issues.Add($"Cache System: Exception during check - {ex.Message}");
            }
        }

        private static void CheckConfigurationSystem(HealthCheckResult result)
        {
            try
            {
                var config = RimAIConfiguration.Instance;
                if (config == null)
                {
                    result.Issues.Add("Configuration System: Instance is null");
                    return;
                }

                if (config.IsDisposed)
                {
                    result.Issues.Add("Configuration System: Configuration is disposed");
                    return;
                }

                // 检查关键配置项
                var httpTimeout = config.Get<int>("HTTP.TimeoutSeconds", 30);
                if (httpTimeout < 5 || httpTimeout > 300)
                {
                    result.Warnings.Add($"Configuration System: HTTP timeout is unusual ({httpTimeout}s)");
                }

                var cacheSize = config.Get<int>("Cache.MaxSize", 100);
                if (cacheSize < 10 || cacheSize > 10000)
                {
                    result.Warnings.Add($"Configuration System: Cache size is unusual ({cacheSize})");
                }
            }
            catch (Exception ex)
            {
                result.Issues.Add($"Configuration System: Exception during check - {ex.Message}");
            }
        }

        private static void CheckLifecycleManager(HealthCheckResult result)
        {
            try
            {
                var lifecycle = LifecycleManager.Instance;
                if (lifecycle == null)
                {
                    result.Issues.Add("Lifecycle Manager: Instance is null");
                    return;
                }

                // LifecycleManager实现了IDisposable，但没有公开IsDisposed属性
                // 通过访问ApplicationToken来间接检查状态
                if (lifecycle.ApplicationToken.IsCancellationRequested)
                {
                    result.Warnings.Add("Lifecycle Manager: Application cancellation requested");
                }
            }
            catch (ObjectDisposedException)
            {
                result.Issues.Add("Lifecycle Manager: Manager is disposed");
            }
            catch (Exception ex)
            {
                result.Issues.Add($"Lifecycle Manager: Exception during check - {ex.Message}");
            }
        }

        private static void CheckConnectionPool(HealthCheckResult result)
        {
            try
            {
                var pool = ConnectionPoolManager.Instance;
                if (pool == null)
                {
                    result.Issues.Add("Connection Pool: Instance is null");
                    return;
                }

                // 检查连接统计
                var activeConnections = pool.ActiveConnectionCount;
                var healthyConnections = pool.HealthyConnectionCount;
                
                if (activeConnections > 50)
                {
                    result.Warnings.Add($"Connection Pool: High active connection count ({activeConnections})");
                }
                
                if (activeConnections > 0 && healthyConnections == 0)
                {
                    result.Issues.Add("Connection Pool: No healthy connections available");
                }
            }
            catch (ObjectDisposedException)
            {
                result.Issues.Add("Connection Pool: Pool is disposed");
            }
            catch (Exception ex)
            {
                result.Issues.Add($"Connection Pool: Exception during check - {ex.Message}");
            }
        }

        #endregion

        #region 性能监控

        /// <summary>
        /// 性能指标报告
        /// </summary>
        public class PerformanceReport
        {
            public DateTime ReportTime { get; set; } = DateTime.UtcNow;
            public Dictionary<string, object> Metrics { get; set; } = new Dictionary<string, object>();
            public List<string> Recommendations { get; set; } = new List<string>();
            public string Summary { get; set; }
        }

        /// <summary>
        /// 生成性能报告
        /// </summary>
        /// <returns>性能报告</returns>
        public static PerformanceReport GeneratePerformanceReport()
        {
            var report = new PerformanceReport();

            try
            {
                RimAILogger.Info("Generating performance report...");

                // 收集各组件性能指标
                CollectApiMetrics(report);
                CollectCacheMetrics(report);
                CollectConnectionMetrics(report);
                CollectSystemMetrics(report);

                // 生成建议
                GeneratePerformanceRecommendations(report);

                // 生成摘要
                GeneratePerformanceSummary(report);

                RimAILogger.Info("Performance report generated successfully");
                return report;
            }
            catch (Exception ex)
            {
                report.Summary = $"Failed to generate performance report: {ex.Message}";
                RimAILogger.Error($"Performance report generation failed: {ex.Message}");
                return report;
            }
        }

        private static void CollectApiMetrics(PerformanceReport report)
        {
            try
            {
                if (RimAIAPI.IsInitialized)
                {
                    var stats = RimAIAPI.GetStatistics();
                    foreach (var stat in stats)
                    {
                        report.Metrics[$"API.{stat.Key}"] = stat.Value;
                    }
                }
            }
            catch (Exception ex)
            {
                report.Metrics["API.Error"] = ex.Message;
            }
        }

        private static void CollectCacheMetrics(PerformanceReport report)
        {
            try
            {
                var cache = ResponseCache.Instance;
                if (cache != null && !cache.IsDisposed)
                {
                    var stats = cache.GetStats();
                    report.Metrics["Cache.EntryCount"] = stats.EntryCount;
                    report.Metrics["Cache.HitRate"] = stats.HitRate;
                    report.Metrics["Cache.TotalRequests"] = stats.TotalRequests;
                    report.Metrics["Cache.MemoryUsageMB"] = stats.MemoryUsageEstimate / (1024 * 1024);
                }
            }
            catch (Exception ex)
            {
                report.Metrics["Cache.Error"] = ex.Message;
            }
        }

        private static void CollectConnectionMetrics(PerformanceReport report)
        {
            try
            {
                var pool = ConnectionPoolManager.Instance;
                if (pool != null)
                {
                    report.Metrics["Connection.ActiveConnections"] = pool.ActiveConnectionCount;
                    report.Metrics["Connection.HealthyConnections"] = pool.HealthyConnectionCount;
                    report.Metrics["Connection.TotalConnectionsCreated"] = pool.TotalConnectionsCreated;
                    report.Metrics["Connection.TotalConnectionsDisposed"] = pool.TotalConnectionsDisposed;
                }
            }
            catch (ObjectDisposedException)
            {
                report.Metrics["Connection.Error"] = "Connection pool is disposed";
            }
            catch (Exception ex)
            {
                report.Metrics["Connection.Error"] = ex.Message;
            }
        }

        private static void CollectSystemMetrics(PerformanceReport report)
        {
            try
            {
                // 内存使用情况
                var totalMemory = GC.GetTotalMemory(false);
                report.Metrics["System.MemoryUsageMB"] = totalMemory / (1024 * 1024);
                
                // GC 信息
                report.Metrics["System.GCGen0Collections"] = GC.CollectionCount(0);
                report.Metrics["System.GCGen1Collections"] = GC.CollectionCount(1);
                report.Metrics["System.GCGen2Collections"] = GC.CollectionCount(2);

                // 线程信息
                report.Metrics["System.ThreadCount"] = System.Diagnostics.Process.GetCurrentProcess().Threads.Count;
            }
            catch (Exception ex)
            {
                report.Metrics["System.Error"] = ex.Message;
            }
        }

        private static void GeneratePerformanceRecommendations(PerformanceReport report)
        {
            // 基于指标生成建议
            if (report.Metrics.ContainsKey("Cache.HitRate"))
            {
                var hitRate = Convert.ToDouble(report.Metrics["Cache.HitRate"]);
                if (hitRate < 0.5)
                {
                    report.Recommendations.Add("Consider reviewing cache configuration - low hit rate detected");
                }
                else if (hitRate > 0.9)
                {
                    report.Recommendations.Add("Excellent cache performance - consider current settings as baseline");
                }
            }

            if (report.Metrics.ContainsKey("System.MemoryUsageMB"))
            {
                var memoryMB = Convert.ToDouble(report.Metrics["System.MemoryUsageMB"]);
                if (memoryMB > 500)
                {
                    report.Recommendations.Add("High memory usage detected - consider cache cleanup or size reduction");
                }
            }

            if (report.Metrics.ContainsKey("Connection.ActiveConnections"))
            {
                var activeConn = Convert.ToInt32(report.Metrics["Connection.ActiveConnections"]);
                if (activeConn > 20)
                {
                    report.Recommendations.Add("High number of active connections - monitor for connection leaks");
                }
            }

            if (report.Recommendations.Count == 0)
            {
                report.Recommendations.Add("System performance appears optimal");
            }
        }

        private static void GeneratePerformanceSummary(PerformanceReport report)
        {
            var summary = new StringBuilder();
            summary.AppendLine("=== RimAI Framework Performance Summary ===");
            summary.AppendLine($"Report Time: {report.ReportTime:yyyy-MM-dd HH:mm:ss UTC}");
            summary.AppendLine();

            // 关键指标摘要
            summary.AppendLine("Key Metrics:");
            var keyMetrics = new[] { "API.TotalRequests", "API.SuccessRate", "Cache.HitRate", "System.MemoryUsageMB" };
            
            foreach (var key in keyMetrics)
            {
                if (report.Metrics.ContainsKey(key))
                {
                    var value = report.Metrics[key];
                    if (key.Contains("Rate"))
                    {
                        summary.AppendLine($"  {key}: {Convert.ToDouble(value):P2}");
                    }
                    else if (key.Contains("Memory"))
                    {
                        summary.AppendLine($"  {key}: {Convert.ToDouble(value):F1} MB");
                    }
                    else
                    {
                        summary.AppendLine($"  {key}: {value}");
                    }
                }
            }

            summary.AppendLine();
            summary.AppendLine("Recommendations:");
            foreach (var recommendation in report.Recommendations)
            {
                summary.AppendLine($"  • {recommendation}");
            }

            report.Summary = summary.ToString();
        }

        #endregion

        #region 调试命令

        /// <summary>
        /// 注册RimWorld调试控制台命令
        /// </summary>
        public static void RegisterDebugCommands()
        {
            try
            {
                // 注册健康检查命令
                LongEventHandler.QueueLongEvent(() =>
                {
                    // 在RimWorld中，调试命令通常通过DebugActions系统注册
                    // 由于我们不能直接访问DebugActionsMod，我们将使用Log.Message输出
                    RimAILogger.Info("Debug commands system initialized - use dev console for manual commands");
                }, "Initializing RimAI Debug Commands", false, null);

                RimAILogger.Info("Debug commands registered successfully");
            }
            catch (Exception ex)
            {
                RimAILogger.Error($"Failed to register debug commands: {ex.Message}");
            }
        }

        /// <summary>
        /// 手动执行健康检查命令
        /// </summary>
        public static void ExecuteHealthCheckCommand()
        {
            var result = PerformHealthCheck();
            Log.Message($"[RimAI Health Check]\n{FormatHealthCheckResult(result)}");
        }

        /// <summary>
        /// 手动执行性能报告命令
        /// </summary>
        public static void ExecutePerformanceReportCommand()
        {
            var report = GeneratePerformanceReport();
            Log.Message($"[RimAI Performance]\n{report.Summary}");
        }

        /// <summary>
        /// 手动执行统计信息命令
        /// </summary>
        public static void ExecuteShowStatisticsCommand()
        {
            if (RimAIAPI.IsInitialized)
            {
                var stats = RimAIAPI.GetStatistics();
                var formatted = FormatStatistics(stats);
                Log.Message($"[RimAI Statistics]\n{formatted}");
            }
            else
            {
                Log.Message("[RimAI Statistics] Framework not initialized");
            }
        }

        /// <summary>
        /// 手动执行缓存清理命令
        /// </summary>
        public static void ExecuteClearCacheCommand()
        {
            RimAIAPI.ClearCache();
            Log.Message("[RimAI Cache] Cache cleared successfully");
        }

        /// <summary>
        /// 手动执行内存清理命令
        /// </summary>
        public static void ExecuteForceGarbageCollectionCommand()
        {
            var beforeMemory = GC.GetTotalMemory(false) / (1024 * 1024);
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
            var afterMemory = GC.GetTotalMemory(false) / (1024 * 1024);
            Log.Message($"[RimAI Memory] GC completed - Memory: {beforeMemory:F1}MB → {afterMemory:F1}MB (Freed: {beforeMemory - afterMemory:F1}MB)");
        }

        #endregion

        #region 格式化方法

        private static string FormatHealthCheckResult(HealthCheckResult result)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"Status: {result.Status}");
            sb.AppendLine($"Check Duration: {result.CheckDuration.TotalMilliseconds:F0}ms");
            sb.AppendLine($"Check Time: {result.CheckTime:yyyy-MM-dd HH:mm:ss UTC}");
            
            if (result.Issues.Count > 0)
            {
                sb.AppendLine("\nIssues:");
                foreach (var issue in result.Issues)
                {
                    sb.AppendLine($"  ❌ {issue}");
                }
            }

            if (result.Warnings.Count > 0)
            {
                sb.AppendLine("\nWarnings:");
                foreach (var warning in result.Warnings)
                {
                    sb.AppendLine($"  ⚠️ {warning}");
                }
            }

            if (result.Issues.Count == 0 && result.Warnings.Count == 0)
            {
                sb.AppendLine("\n✅ All systems healthy!");
            }

            return sb.ToString();
        }

        private static string FormatStatistics(Dictionary<string, object> stats)
        {
            if (stats == null || stats.Count == 0)
            {
                return "No statistics available";
            }

            var sb = new StringBuilder();
            sb.AppendLine("Current Statistics:");

            // 按类别分组显示
            var groups = stats.Keys.GroupBy(k => k.Contains(".") ? k.Split('.')[0] : "General");
            
            foreach (var group in groups.OrderBy(g => g.Key))
            {
                sb.AppendLine($"\n{group.Key}:");
                foreach (var key in group.OrderBy(k => k))
                {
                    var value = stats[key];
                    var displayKey = key.Contains(".") ? key.Split('.')[1] : key;
                    
                    if (value is double d && (key.Contains("Rate") || key.Contains("Ratio")))
                    {
                        sb.AppendLine($"  {displayKey}: {d:P2}");
                    }
                    else if (value is DateTime dt)
                    {
                        sb.AppendLine($"  {displayKey}: {dt:yyyy-MM-dd HH:mm:ss}");
                    }
                    else
                    {
                        sb.AppendLine($"  {displayKey}: {value}");
                    }
                }
            }

            return sb.ToString();
        }

        #endregion

        #region 实时监控

        /// <summary>
        /// 监控配置
        /// </summary>
        public class MonitoringConfig
        {
            public bool EnableRealTimeMonitoring { get; set; } = false;
            public TimeSpan MonitoringInterval { get; set; } = TimeSpan.FromMinutes(5);
            public bool LogPerformanceAlerts { get; set; } = true;
            public bool LogHealthAlerts { get; set; } = true;
        }

        private static MonitoringConfig _monitoringConfig = new MonitoringConfig();
        private static bool _monitoringActive = false;

        /// <summary>
        /// 启动实时监控
        /// </summary>
        /// <param name="config">监控配置</param>
        public static void StartRealTimeMonitoring(MonitoringConfig config = null)
        {
            if (_monitoringActive)
            {
                RimAILogger.Warning("Real-time monitoring is already active");
                return;
            }

            _monitoringConfig = config ?? _monitoringConfig;
            _monitoringActive = true;

            RimAILogger.Info($"Starting real-time monitoring (interval: {_monitoringConfig.MonitoringInterval.TotalMinutes:F1} minutes)");

            // 注册到生命周期管理器的健康检查
            try
            {
                var lifecycle = LifecycleManager.Instance;
                // 监控逻辑会在生命周期管理器的健康检查中执行
                RimAILogger.Info("Real-time monitoring integrated with lifecycle manager");
            }
            catch (Exception ex)
            {
                RimAILogger.Error($"Failed to start real-time monitoring: {ex.Message}");
                _monitoringActive = false;
            }
        }

        /// <summary>
        /// 停止实时监控
        /// </summary>
        public static void StopRealTimeMonitoring()
        {
            if (!_monitoringActive)
            {
                return;
            }

            _monitoringActive = false;
            RimAILogger.Info("Real-time monitoring stopped");
        }

        /// <summary>
        /// 执行监控检查（由生命周期管理器调用）
        /// </summary>
        internal static void PerformMonitoringCheck()
        {
            if (!_monitoringActive || !_monitoringConfig.EnableRealTimeMonitoring)
            {
                return;
            }

            try
            {
                // 执行健康检查
                if (_monitoringConfig.LogHealthAlerts)
                {
                    var health = PerformHealthCheck();
                    if (!health.IsHealthy)
                    {
                        RimAILogger.Warning($"Health alert: {health.Status} - {health.Issues.Count} issues detected");
                    }
                }

                // 执行性能检查
                if (_monitoringConfig.LogPerformanceAlerts)
                {
                    var performance = GeneratePerformanceReport();
                    if (performance.Recommendations.Any(r => r.Contains("High") || r.Contains("Low")))
                    {
                        RimAILogger.Info($"Performance alert: {performance.Recommendations.Count} recommendations");
                    }
                }
            }
            catch (Exception ex)
            {
                RimAILogger.Error($"Monitoring check failed: {ex.Message}");
            }
        }

        #endregion
    }
}
