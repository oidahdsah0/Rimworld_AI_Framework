using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Threading;
using System.Linq;
using RimAI.Framework.API;
using RimAI.Framework.Core;
using RimAI.Framework.LLM;
using RimAI.Framework.LLM.Models;
using RimAI.Framework.Cache;
using RimAI.Framework.Configuration;
using RimAI.Framework.Diagnostics;
using RimAI.Framework.Exceptions;
using Verse;

namespace RimAI.Framework.Examples
{
    /// <summary>
    /// 展示RimAI框架v3.0高级用法和最佳实践的完整示例
    /// 包含错误处理、性能优化、监控集成等最佳实践
    /// </summary>
    public static class EnhancedArchitectureExamples
    {
        #region 1. 生产环境最佳实践示例

        /// <summary>
        /// 生产环境推荐的AI请求处理模式
        /// </summary>
        public static async Task<string> ProductionReadyAIRequest(string prompt, CancellationToken cancellationToken = default)
        {
            // 1. 输入验证
            if (string.IsNullOrWhiteSpace(prompt))
            {
                throw new ArgumentException("Prompt cannot be null or empty", nameof(prompt));
            }

            // 2. 超时控制
            using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeoutCts.CancelAfter(TimeSpan.FromSeconds(30)); // 30秒超时

            try
            {
                // 3. 健康检查
                var healthResult = FrameworkDiagnostics.PerformHealthCheck();
                if (!healthResult.IsHealthy)
                {
                    RimAILogger.Warning($"Framework health issues detected: {healthResult.Status}");
                    // 在生产环境中可能需要降级处理或延迟请求
                }

                // 4. 创建请求选项（推荐的生产环境配置）
                var options = new LLMRequestOptions
                {
                    Model = "gpt-3.5-turbo", // 生产环境推荐稳定模型
                    MaxTokens = 500,
                    Temperature = 0.3 // 更低的随机性以获得更一致的结果
                };

                // 5. 执行请求
                var response = await RimAIAPI.SendMessageAsync(prompt, options, timeoutCts.Token);

                // 6. 结果验证
                if (string.IsNullOrWhiteSpace(response))
                {
                    throw LLMException.ServiceUnavailable("AI返回空响应");
                }

                // 7. 日志记录（生产环境的最佳实践）
                RimAILogger.Info($"AI请求成功完成 - 提示长度: {prompt.Length}, 响应长度: {response.Length}");

                return response;
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                RimAILogger.Info("AI请求被用户取消");
                throw;
            }
            catch (OperationCanceledException) when (timeoutCts.Token.IsCancellationRequested)
            {
                RimAILogger.Warning("AI请求超时");
                throw new TimeoutException("AI请求在30秒内未完成");
            }
            catch (RimAIException aiEx)
            {
                RimAILogger.Error($"AI框架错误: {aiEx.Message}");
                throw;
            }
            catch (Exception ex)
            {
                RimAILogger.Error($"意外错误: {ex.Message}");
                throw LLMException.ServiceUnavailable("AI请求处理失败", ex);
            }
        }

        #endregion

        #region 2. 高性能批处理模式

        /// <summary>
        /// 高效的批量AI请求处理
        /// </summary>
        public static async Task<Dictionary<string, string>> ProcessBatchRequestsOptimized(
            Dictionary<string, string> prompts, 
            CancellationToken cancellationToken = default)
        {
            var results = new Dictionary<string, string>();
            
            if (prompts == null || prompts.Count == 0)
            {
                return results;
            }

            try
            {
                // 由于RequestBatcher需要泛型类型参数且较为复杂，
                // 这里展示如何直接使用API进行批处理优化
                var tasks = new List<Task<KeyValuePair<string, string>>>();
                
                // 使用信号量控制并发数
                using var semaphore = new SemaphoreSlim(5, 5); // 最多5个并发请求
                
                foreach (var kvp in prompts)
                {
                    var task = ProcessSingleOptimizedRequest(semaphore, kvp.Key, kvp.Value, cancellationToken);
                    tasks.Add(task);
                }

                // 等待所有请求完成
                var completedResults = await Task.WhenAll(tasks);

                // 整理结果
                foreach (var result in completedResults)
                {
                    if (!string.IsNullOrEmpty(result.Value))
                    {
                        results[result.Key] = result.Value;
                    }
                }

                RimAILogger.Info($"批处理完成 - 处理了 {results.Count}/{prompts.Count} 个请求");
                return results;
            }
            catch (Exception ex)
            {
                RimAILogger.Error($"批处理失败: {ex.Message}");
                throw;
            }
        }

        private static async Task<KeyValuePair<string, string>> ProcessSingleOptimizedRequest(
            SemaphoreSlim semaphore,
            string key, 
            string prompt, 
            CancellationToken cancellationToken)
        {
            await semaphore.WaitAsync(cancellationToken);
            
            try
            {
                var options = new LLMRequestOptions
                {
                    Model = "gpt-3.5-turbo",
                    MaxTokens = 200, // 批处理时使用较小的token限制
                    Temperature = 0.2
                };

                var response = await RimAIAPI.SendMessageAsync(prompt, options, cancellationToken);
                return new KeyValuePair<string, string>(key, response ?? "");
            }
            catch (Exception ex)
            {
                RimAILogger.Warning($"批处理单项失败 [{key}]: {ex.Message}");
                return new KeyValuePair<string, string>(key, "");
            }
            finally
            {
                semaphore.Release();
            }
        }

        #endregion

        #region 3. 智能缓存管理

        /// <summary>
        /// 展示智能缓存策略
        /// </summary>
        public static async Task<string> SmartCachedRequest(string prompt, string category = "general")
        {
            try
            {
                var cache = ResponseCache.Instance;
                
                // 1. 检查缓存统计，决定是否需要清理
                var stats = cache.GetStats();
                if (stats.MemoryUsageEstimate > 50 * 1024 * 1024) // 50MB阈值
                {
                    RimAILogger.Info($"缓存内存使用过高 ({stats.MemoryUsageEstimate / (1024 * 1024):F1}MB)，执行清理");
                    cache.Clear(); // 清理缓存
                }

                // 2. 创建请求选项
                var options = new LLMRequestOptions
                {
                    Model = "gpt-3.5-turbo",
                    MaxTokens = 300,
                    Temperature = 0.3
                };

                // 3. 执行请求
                var response = await RimAIAPI.SendMessageAsync(prompt, options);

                // 4. 日志缓存效果
                var newStats = cache.GetStats();
                RimAILogger.Info($"缓存命中率: {newStats.HitRate:P2}, 条目数: {newStats.EntryCount}");

                return response;
            }
            catch (Exception ex)
            {
                RimAILogger.Error($"智能缓存请求失败: {ex.Message}");
                throw;
            }
        }

        #endregion

        #region 4. 综合应用示例

        /// <summary>
        /// 综合展示所有最佳实践的完整应用示例
        /// </summary>
        public static async Task<ProcessingResult> ComprehensiveAIProcessing(
            string prompt, 
            ProcessingConfig config = null,
            CancellationToken cancellationToken = default)
        {
            config = config ?? new ProcessingConfig();
            var result = new ProcessingResult { StartTime = DateTime.UtcNow };

            try
            {
                // 1. 输入验证和预处理
                if (string.IsNullOrWhiteSpace(prompt))
                {
                    throw new ArgumentException("Prompt cannot be null or empty", nameof(prompt));
                }

                // 2. 系统健康检查
                var healthCheck = FrameworkDiagnostics.PerformHealthCheck();
                result.HealthStatus = healthCheck.Status;
                
                if (!healthCheck.IsHealthy && config.RequireHealthySystem)
                {
                    throw LLMException.ServiceUnavailable($"System unhealthy: {healthCheck.Status}");
                }

                // 3. 性能基线测量
                var initialPerformance = FrameworkDiagnostics.GeneratePerformanceReport();
                result.InitialMemoryMB = Convert.ToDouble(initialPerformance.Metrics.GetValueOrDefault("System.MemoryUsageMB", 0));

                // 4. 智能模型选择
                var selectedModel = SelectOptimalModel(prompt, config, initialPerformance);
                result.SelectedModel = selectedModel;

                // 5. 请求执行（带完整错误处理）
                var response = await ExecuteRobustRequest(prompt, selectedModel, config, cancellationToken);
                result.Response = response;
                result.IsSuccessful = !string.IsNullOrEmpty(response);

                // 6. 后处理性能分析
                var finalPerformance = FrameworkDiagnostics.GeneratePerformanceReport();
                result.FinalMemoryMB = Convert.ToDouble(finalPerformance.Metrics.GetValueOrDefault("System.MemoryUsageMB", 0));
                result.MemoryDeltaMB = result.FinalMemoryMB - result.InitialMemoryMB;

                // 7. 性能建议生成
                result.PerformanceRecommendations = GeneratePerformanceRecommendations(result, finalPerformance);

                return result;
            }
            catch (Exception ex)
            {
                result.IsSuccessful = false;
                result.ErrorMessage = ex.Message;
                result.Exception = ex;
                
                RimAILogger.Error($"综合AI处理失败: {ex.Message}");
                return result;
            }
            finally
            {
                result.EndTime = DateTime.UtcNow;
                result.TotalDuration = result.EndTime - result.StartTime;
                
                // 记录完整的处理统计
                RimAILogger.Info($"综合处理完成 - 成功: {result.IsSuccessful}, " +
                               $"耗时: {result.TotalDuration.TotalMilliseconds:F0}ms, " +
                               $"内存变化: {result.MemoryDeltaMB:F1}MB");
            }
        }

        private static string SelectOptimalModel(
            string prompt, 
            ProcessingConfig config, 
            FrameworkDiagnostics.PerformanceReport performance)
        {
            // 基于多种因素智能选择模型
            var memoryMB = Convert.ToDouble(performance.Metrics.GetValueOrDefault("System.MemoryUsageMB", 0));
            
            if (config.PrioritizeCost || memoryMB > 300)
            {
                return "gpt-3.5-turbo";
            }
            
            if (prompt.Length > 1000 || config.RequireHighQuality)
            {
                return "gpt-4";
            }
            
            return "gpt-3.5-turbo";
        }

        private static async Task<string> ExecuteRobustRequest(
            string prompt, 
            string model, 
            ProcessingConfig config, 
            CancellationToken cancellationToken)
        {
            var options = new LLMRequestOptions
            {
                Model = model,
                MaxTokens = config.MaxTokens,
                Temperature = config.Temperature
            };

            return await RimAIAPI.SendMessageAsync(prompt, options, cancellationToken);
        }

        private static List<string> GeneratePerformanceRecommendations(
            ProcessingResult result, 
            FrameworkDiagnostics.PerformanceReport performance)
        {
            var recommendations = new List<string>();

            if (result.TotalDuration.TotalSeconds > 15)
            {
                recommendations.Add("Consider reducing request complexity or switching to a faster model");
            }

            if (result.MemoryDeltaMB > 10)
            {
                recommendations.Add("High memory usage detected - consider enabling more aggressive caching");
            }

            if (performance.Metrics.ContainsKey("Cache.HitRate"))
            {
                var hitRate = Convert.ToDouble(performance.Metrics["Cache.HitRate"]);
                if (hitRate < 0.3)
                {
                    recommendations.Add("Low cache hit rate - review caching strategy");
                }
            }

            return recommendations;
        }

        #endregion

        #region 支持类型定义

        /// <summary>
        /// 处理配置
        /// </summary>
        public class ProcessingConfig
        {
            public int MaxTokens { get; set; } = 500;
            public double Temperature { get; set; } = 0.3;
            public bool EnableCaching { get; set; } = true;
            public int RetryCount { get; set; } = 2;
            public int TimeoutSeconds { get; set; } = 30;
            public bool RequireHealthySystem { get; set; } = false;
            public bool PrioritizeCost { get; set; } = false;
            public bool RequireHighQuality { get; set; } = false;
        }

        /// <summary>
        /// 处理结果
        /// </summary>
        public class ProcessingResult
        {
            public DateTime StartTime { get; set; }
            public DateTime EndTime { get; set; }
            public TimeSpan TotalDuration { get; set; }
            public bool IsSuccessful { get; set; }
            public string Response { get; set; }
            public string SelectedModel { get; set; }
            public string HealthStatus { get; set; }
            public double InitialMemoryMB { get; set; }
            public double FinalMemoryMB { get; set; }
            public double MemoryDeltaMB { get; set; }
            public List<string> PerformanceRecommendations { get; set; } = new List<string>();
            public string ErrorMessage { get; set; }
            public Exception Exception { get; set; }
        }

        #endregion
    }
}