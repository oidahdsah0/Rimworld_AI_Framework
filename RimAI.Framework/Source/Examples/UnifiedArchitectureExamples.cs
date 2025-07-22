using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using RimAI.Framework.API;
using RimAI.Framework.LLM.Models;
using Verse;

namespace RimAI.Framework.Examples
{
    /// <summary>
    /// 统一架构使用示例 - 展示RimAI Framework v3.0的统一API使用方法
    /// 包含基础请求、流式处理、批量处理等核心功能的完整示例
    /// </summary>
    /// <example>
    /// 在您的Mod中使用这些示例：
    /// <code>
    /// // 在您的Mod类或静态方法中调用
    /// var result = await UnifiedArchitectureExamples.BasicUsageExample();
    /// await UnifiedArchitectureExamples.StreamingExample();
    /// </code>
    /// </example>
    public static class UnifiedArchitectureExamples
    {
        #region 基础使用示例

        /// <summary>
        /// 示例1：基础消息发送
        /// 展示最简单的AI请求方式
        /// </summary>
        /// <returns>AI生成的回复文本</returns>
        public static async Task<string> BasicUsageExample()
        {
            try
            {
                // 检查框架状态
                if (!RimAIAPI.IsInitialized)
                {
                    Log.Warning("[RimAI Examples] Framework is not initialized");
                    return "Framework not available";
                }

                // 发送简单消息
                var response = await RimAIAPI.SendMessageAsync(
                    "Describe a typical day in a RimWorld colony"
                );

                Log.Message($"[RimAI Examples] Basic response: {response?.Substring(0, Math.Min(100, response?.Length ?? 0))}...");
                return response;
            }
            catch (Exception ex)
            {
                Log.Error($"[RimAI Examples] Basic usage failed: {ex.Message}");
                return $"Error: {ex.Message}";
            }
        }

        /// <summary>
        /// 示例2：带温度控制的创意生成
        /// 展示如何控制AI回复的创造性程度
        /// </summary>
        /// <returns>创意回复文本</returns>
        public static async Task<string> TemperatureControlExample()
        {
            try
            {
                // 低温度 - 更保守、一致的回复
                var conservativeResponse = await RimAIAPI.SendMessageWithTemperatureAsync(
                    "What are the best defensive strategies for a RimWorld colony?",
                    0.2  // 低温度，更准确的信息
                );

                // 高温度 - 更有创意、变化多样的回复
                var creativeResponse = await RimAIAPI.SendMessageWithTemperatureAsync(
                    "Create a dramatic story about a RimWorld colonist",
                    1.2  // 高温度，更有创意
                );

                Log.Message($"[RimAI Examples] Conservative: {conservativeResponse?.Substring(0, 50)}...");
                Log.Message($"[RimAI Examples] Creative: {creativeResponse?.Substring(0, 50)}...");

                return $"Conservative: {conservativeResponse}\n\nCreative: {creativeResponse}";
            }
            catch (Exception ex)
            {
                Log.Error($"[RimAI Examples] Temperature control failed: {ex.Message}");
                return $"Error: {ex.Message}";
            }
        }

        #endregion

        #region 预设选项示例

        /// <summary>
        /// 示例3：使用预设选项
        /// 展示如何使用内置的选项预设来快速配置不同类型的请求
        /// </summary>
        /// <returns>不同风格的回复集合</returns>
        public static async Task<Dictionary<string, string>> PresetOptionsExample()
        {
            var results = new Dictionary<string, string>();
            var prompt = "Explain RimWorld's research system";

            try
            {
                // 事实性预设 - 用于准确信息
                var factualResponse = await RimAIAPI.SendMessageAsync(
                    prompt,
                    RimAIAPI.Options.Factual()
                );
                results["Factual"] = factualResponse;

                // 创造性预设 - 用于创意内容
                var creativeResponse = await RimAIAPI.SendMessageAsync(
                    "Write a story about " + prompt.ToLower(),
                    RimAIAPI.Options.Creative()
                );
                results["Creative"] = creativeResponse;

                // 结构化预设 - 用于需要格式化输出
                var structuredResponse = await RimAIAPI.SendMessageAsync(
                    prompt + " in a structured format with bullet points",
                    RimAIAPI.Options.Structured()
                );
                results["Structured"] = structuredResponse;

                Log.Message($"[RimAI Examples] Generated {results.Count} different response styles");
                return results;
            }
            catch (Exception ex)
            {
                Log.Error($"[RimAI Examples] Preset options failed: {ex.Message}");
                results["Error"] = ex.Message;
                return results;
            }
        }

        #endregion

        #region 流式处理示例

        /// <summary>
        /// 示例4：流式响应处理
        /// 展示如何处理实时流式AI回复，适合长文本生成
        /// </summary>
        /// <param name="onProgress">进度回调函数，接收每个文本块</param>
        /// <returns>完成标记</returns>
        public static async Task<bool> StreamingExample(Action<string> onProgress = null)
        {
            try
            {
                var receivedChunks = new List<string>();
                var totalLength = 0;

                await RimAIAPI.SendStreamingMessageAsync(
                    "Write a detailed guide on how to survive the first year in RimWorld",
                    chunk =>
                    {
                        // 处理每个接收到的文本块
                        if (!string.IsNullOrEmpty(chunk))
                        {
                            receivedChunks.Add(chunk);
                            totalLength += chunk.Length;
                            
                            // 通知进度
                            onProgress?.Invoke(chunk);
                            
                            // 显示进度（每10个块显示一次）
                            if (receivedChunks.Count % 10 == 0)
                            {
                                Log.Message($"[RimAI Examples] Streaming progress: {receivedChunks.Count} chunks, {totalLength} characters");
                            }
                        }
                    },
                    RimAIAPI.Options.Streaming(temperature: 0.7, maxTokens: 800)
                );

                Log.Message($"[RimAI Examples] Streaming completed: {receivedChunks.Count} chunks, {totalLength} total characters");
                return true;
            }
            catch (Exception ex)
            {
                Log.Error($"[RimAI Examples] Streaming failed: {ex.Message}");
                return false;
            }
        }

        #endregion

        #region 批量处理示例

        /// <summary>
        /// 示例5：批量请求处理
        /// 展示如何高效处理多个相关的AI请求
        /// </summary>
        /// <returns>所有回复的列表</returns>
        public static async Task<List<string>> BatchProcessingExample()
        {
            try
            {
                // 准备批量问题
                var questions = new List<string>
                {
                    "What are the basic needs of RimWorld colonists?",
                    "How do you manage food production effectively?",
                    "What are the most important early-game research priorities?",
                    "How do you defend against raids?",
                    "What's the best way to manage colonist mood?"
                };

                Log.Message($"[RimAI Examples] Starting batch processing of {questions.Count} questions");

                // 使用批量处理API
                var responses = await RimAIAPI.SendBatchRequestAsync(
                    questions,
                    RimAIAPI.Options.Factual(temperature: 0.3, maxTokens: 200)
                );

                // 记录结果
                for (int i = 0; i < Math.Min(questions.Count, responses.Count); i++)
                {
                    Log.Message($"[RimAI Examples] Q{i + 1}: {questions[i].Substring(0, Math.Min(50, questions[i].Length))}...");
                    Log.Message($"[RimAI Examples] A{i + 1}: {responses[i]?.Substring(0, Math.Min(100, responses[i]?.Length ?? 0))}...");
                }

                Log.Message($"[RimAI Examples] Batch processing completed: {responses.Count} responses generated");
                return responses;
            }
            catch (Exception ex)
            {
                Log.Error($"[RimAI Examples] Batch processing failed: {ex.Message}");
                return new List<string> { $"Batch processing error: {ex.Message}" };
            }
        }

        #endregion

        #region 系统监控示例

        /// <summary>
        /// 示例6：系统统计和监控
        /// 展示如何获取框架运行状态和性能指标
        /// </summary>
        public static void SystemMonitoringExample()
        {
            try
            {
                // 获取框架状态
                Log.Message($"[RimAI Examples] Framework Status: {RimAIAPI.Status}");
                Log.Message($"[RimAI Examples] Framework Initialized: {RimAIAPI.IsInitialized}");

                if (RimAIAPI.IsInitialized)
                {
                    // 获取详细统计信息
                    var stats = RimAIAPI.GetStatistics();
                    
                    Log.Message($"[RimAI Examples] === Framework Statistics ===");
                    foreach (var stat in stats)
                    {
                        Log.Message($"[RimAI Examples] {stat.Key}: {stat.Value}");
                    }

                    // 显示关键指标
                    if (stats.ContainsKey("TotalRequests"))
                    {
                        Log.Message($"[RimAI Examples] Performance Summary:");
                        Log.Message($"[RimAI Examples] - Total Requests: {stats["TotalRequests"]}");
                        
                        if (stats.ContainsKey("SuccessRate"))
                            Log.Message($"[RimAI Examples] - Success Rate: {stats["SuccessRate"]:P2}");
                        
                        if (stats.ContainsKey("CacheHitRate"))
                            Log.Message($"[RimAI Examples] - Cache Hit Rate: {stats["CacheHitRate"]:P2}");
                    }
                }
                else
                {
                    Log.Warning("[RimAI Examples] Framework is not initialized - cannot retrieve statistics");
                }
            }
            catch (Exception ex)
            {
                Log.Error($"[RimAI Examples] System monitoring failed: {ex.Message}");
            }
        }

        /// <summary>
        /// 示例7：缓存管理
        /// 展示如何管理响应缓存以优化性能和内存使用
        /// </summary>
        public static async Task CacheManagementExample()
        {
            try
            {
                var testPrompt = "What is the best way to start a RimWorld colony?";

                // 第一次请求 - 会缓存结果
                Log.Message("[RimAI Examples] Making first request (will be cached)...");
                var response1 = await RimAIAPI.SendMessageAsync(testPrompt);
                
                var stats1 = RimAIAPI.GetStatistics();
                if (stats1.ContainsKey("CacheHits") && stats1.ContainsKey("CacheMisses"))
                {
                    Log.Message($"[RimAI Examples] After first request - Hits: {stats1["CacheHits"]}, Misses: {stats1["CacheMisses"]}");
                }

                // 第二次相同请求 - 应该从缓存获取
                Log.Message("[RimAI Examples] Making second identical request (should use cache)...");
                var response2 = await RimAIAPI.SendMessageAsync(testPrompt);

                var stats2 = RimAIAPI.GetStatistics();
                if (stats2.ContainsKey("CacheHits") && stats2.ContainsKey("CacheMisses"))
                {
                    Log.Message($"[RimAI Examples] After second request - Hits: {stats2["CacheHits"]}, Misses: {stats2["CacheMisses"]}");
                }

                // 清理缓存
                Log.Message("[RimAI Examples] Clearing cache...");
                RimAIAPI.ClearCache();
                
                var stats3 = RimAIAPI.GetStatistics();
                if (stats3.ContainsKey("CacheEntryCount"))
                {
                    Log.Message($"[RimAI Examples] After cache clear - Entry count: {stats3["CacheEntryCount"]}");
                }

                Log.Message("[RimAI Examples] Cache management example completed");
            }
            catch (Exception ex)
            {
                Log.Error($"[RimAI Examples] Cache management failed: {ex.Message}");
            }
        }

        #endregion

        #region 综合使用示例

        /// <summary>
        /// 示例8：综合使用场景
        /// 展示一个完整的使用场景，结合多种API功能
        /// </summary>
        /// <returns>场景执行结果</returns>
        public static async Task<string> ComprehensiveUsageExample()
        {
            var results = new List<string>();

            try
            {
                Log.Message("[RimAI Examples] Starting comprehensive usage scenario...");

                // 1. 检查系统状态
                if (!RimAIAPI.IsInitialized)
                {
                    results.Add("❌ Framework not initialized");
                    return string.Join("\n", results);
                }
                results.Add("✅ Framework initialized");

                // 2. 获取基础信息
                var basicInfo = await RimAIAPI.SendMessageAsync(
                    "Provide a brief overview of RimWorld gameplay mechanics",
                    RimAIAPI.Options.Factual(maxTokens: 300)
                );
                results.Add($"✅ Basic info retrieved ({basicInfo?.Length ?? 0} chars)");

                // 3. 生成创意内容
                var storyPrompt = "Create a short story about a RimWorld colonist's adventure";
                var hasStory = false;
                
                await RimAIAPI.SendStreamingMessageAsync(
                    storyPrompt,
                    chunk => {
                        if (!string.IsNullOrEmpty(chunk) && !hasStory)
                        {
                            hasStory = true;
                        }
                    },
                    RimAIAPI.Options.Creative(maxTokens: 400)
                );
                results.Add($"✅ Creative story generated via streaming");

                // 4. 批量处理技巧问题
                var tipQuestions = new List<string>
                {
                    "Quick tip for food management?",
                    "Quick tip for defense setup?",
                    "Quick tip for research priority?"
                };

                var tips = await RimAIAPI.SendBatchRequestAsync(
                    tipQuestions,
                    RimAIAPI.Options.Factual(maxTokens: 100)
                );
                results.Add($"✅ Generated {tips.Count} tips via batch processing");

                // 5. 检查性能统计
                var finalStats = RimAIAPI.GetStatistics();
                var totalRequests = finalStats.ContainsKey("TotalRequests") ? finalStats["TotalRequests"] : "N/A";
                results.Add($"✅ Performance check: {totalRequests} total requests");

                results.Add("🎉 Comprehensive scenario completed successfully!");
                
                var finalResult = string.Join("\n", results);
                Log.Message($"[RimAI Examples] Comprehensive scenario results:\n{finalResult}");
                
                return finalResult;
            }
            catch (Exception ex)
            {
                Log.Error($"[RimAI Examples] Comprehensive scenario failed: {ex.Message}");
                results.Add($"❌ Scenario failed: {ex.Message}");
                return string.Join("\n", results);
            }
        }

        #endregion

        #region 工具方法

        /// <summary>
        /// 工具方法：运行所有示例
        /// 方便一次性测试所有功能
        /// </summary>
        /// <returns>执行摘要</returns>
        public static async Task<string> RunAllExamples()
        {
            var summary = new List<string>();
            
            Log.Message("[RimAI Examples] === Running All Examples ===");

            try
            {
                // 运行各个示例
                await BasicUsageExample();
                summary.Add("✅ Basic Usage");

                await TemperatureControlExample();
                summary.Add("✅ Temperature Control");

                await PresetOptionsExample();
                summary.Add("✅ Preset Options");

                await StreamingExample();
                summary.Add("✅ Streaming");

                await BatchProcessingExample();
                summary.Add("✅ Batch Processing");

                SystemMonitoringExample();
                summary.Add("✅ System Monitoring");

                await CacheManagementExample();
                summary.Add("✅ Cache Management");

                await ComprehensiveUsageExample();
                summary.Add("✅ Comprehensive Usage");

                summary.Add("🎉 All examples completed!");
            }
            catch (Exception ex)
            {
                summary.Add($"❌ Examples failed: {ex.Message}");
                Log.Error($"[RimAI Examples] RunAllExamples failed: {ex.Message}");
            }

            var result = string.Join("\n", summary);
            Log.Message($"[RimAI Examples] === Example Summary ===\n{result}");
            
            return result;
        }

        #endregion

        /// <summary>
        /// 测试缓存优化效果
        /// </summary>
        public static async Task TestCacheOptimization()
        {
            try
            {
                Log.Message("=== RimAI Cache Optimization Test ===");
                
                // 1. 游戏启动时的缓存优化测试
                if (Find.TickManager != null && Find.TickManager.TicksGame < 1000)
                {
                    Log.Message($"Game startup detected (tick {Find.TickManager.TicksGame}), cache optimization active");
                    
                    // 在游戏启动时发送多个请求，应该不会缓存
                    for (int i = 0; i < 5; i++)
                    {
                        var response = await RimAIAPI.SendMessageAsync($"Test request {i} during startup");
                        Log.Message($"Startup request {i}: {(response != null ? "Success" : "Failed")}");
                    }
                    
                    var startupStats = RimAIAPI.GetStatistics();
                    Log.Message($"Cache entries after startup: {startupStats.GetValueOrDefault("CacheEntryCount", 0)}");
                }
                
                // 2. 正常游戏时的缓存测试
                else
                {
                    Log.Message("Normal game mode, testing cache functionality");
                    
                    // 发送相同请求测试缓存命中
                    var testPrompt = "What is the best way to start a RimWorld colony?";
                    
                    var response1 = await RimAIAPI.SendMessageAsync(testPrompt);
                    Log.Message("First request completed");
                    
                    var response2 = await RimAIAPI.SendMessageAsync(testPrompt);
                    Log.Message("Second request completed");
                    
                    var stats = RimAIAPI.GetStatistics();
                    Log.Message($"Cache hits: {stats.GetValueOrDefault("CacheHits", 0)}");
                    Log.Message($"Cache misses: {stats.GetValueOrDefault("CacheMisses", 0)}");
                }
                
                // 3. 监控缓存健康状态
                RimAIAPI.MonitorCacheHealth();
                
                Log.Message("Cache optimization test completed");
            }
            catch (Exception ex)
            {
                Log.Error($"Cache optimization test failed: {ex.Message}");
            }
        }
    }
}
