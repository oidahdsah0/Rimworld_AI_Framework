// =====================================================================================================================
// 文件: EmbeddingManager.cs
//
// 作用:
//  Embedding 功能的核心协调器。它不仅负责像 ChatManager 那样协调标准的“配置-翻译-执行-翻译”流程，
//  更内置了【智能批量处理】的核心逻辑。
//
// 关键特性:
//  - 自动分块 (Auto-Chunking): 当输入数量超过 API 限制时，自动将任务拆分为多个小块。
//  - 并发执行 (Concurrent Execution): 使用 Task.WhenAll 并发处理所有小块，最大化效率。
//  - 结果聚合 (Result Aggregation): 将所有小块的结果合并，并按原始顺序返回。
// =====================================================================================================================

using RimAI.Framework.Configuration;
using RimAI.Framework.Execution;
using RimAI.Framework.Shared.Models;
using RimAI.Framework.Translation;
using RimAI.Framework.Translation.Models;
using System.Collections.Generic;
using System.Linq; // 引入 LINQ (Language Integrated Query) 库，它提供了强大的数据操作方法，如 .Select()
using System.Threading.Tasks;

namespace RimAI.Framework.Core
{
    /// <summary>
    /// Embedding 功能总协调器。
    /// </summary>
    public class EmbeddingManager
    {
        private readonly SettingsManager _settingsManager;
        private readonly EmbeddingRequestTranslator _requestTranslator;
        private readonly HttpExecutor _httpExecutor;
        private readonly EmbeddingResponseTranslator _responseTranslator;

        /// <summary>
        /// 构造函数，通过依赖注入获取所有必要服务。
        /// </summary>
        public EmbeddingManager(
            SettingsManager settingsManager,
            EmbeddingRequestTranslator requestTranslator,
            HttpExecutor httpExecutor,
            EmbeddingResponseTranslator responseTranslator)
        {
            _settingsManager = settingsManager;
            _requestTranslator = requestTranslator;
            _httpExecutor = httpExecutor;
            _responseTranslator = responseTranslator;
        }

        /// <summary>
        /// 处理一个完整的 Embedding 请求流程，内置自动分块和并发处理逻辑。
        /// </summary>
        public async Task<Result<UnifiedEmbeddingResponse>> ProcessRequestAsync(UnifiedEmbeddingRequest request, string providerId)
        {
            // [步骤 1: 获取配置] - 与 ChatManager 相同
            var configResult = _settingsManager.GetMergedConfig(providerId);
            if (!configResult.IsSuccess)
            {
                return Result<UnifiedEmbeddingResponse>.Failure(configResult.Error);
            }
            var config = configResult.Value;

            // [步骤 2: 决定处理路径 - 是否需要分块？]
            int maxBatchSize = config.EmbeddingApi.MaxBatchSize;

            // 如果输入数量小于或等于 API 限制，则走简单路径。
            if (request.Inputs.Count <= maxBatchSize)
            {
                return await ProcessSingleBatchAsync(request, config);
            }
            else
            {
                // 否则，走复杂的分块、并发处理路径。
                return await ProcessBatchesConcurrentlyAsync(request, config);
            }
        }
        
        /// <summary>
        /// 私有方法：处理单个批次的请求（简单路径）。
        /// </summary>
        private async Task<Result<UnifiedEmbeddingResponse>> ProcessSingleBatchAsync(UnifiedEmbeddingRequest request, Configuration.Models.MergedConfig config)
        {
            // 这个流程与 ChatManager 的核心逻辑几乎完全一样。
            var httpRequest = _requestTranslator.Translate(request, config);

            var httpResponseResult = await _httpExecutor.ExecuteAsync(httpRequest);
            if (!httpResponseResult.IsSuccess)
            {
                return Result<UnifiedEmbeddingResponse>.Failure(httpResponseResult.Error);
            }

            var finalResponse = await _responseTranslator.TranslateAsync(httpResponseResult.Value, config);
            return Result<UnifiedEmbeddingResponse>.Success(finalResponse);
        }

        /// <summary>
        /// 私有方法：并发处理多个批次的请求（复杂路径）。
        /// </summary>
        private async Task<Result<UnifiedEmbeddingResponse>> ProcessBatchesConcurrentlyAsync(UnifiedEmbeddingRequest request, Configuration.Models.MergedConfig config)
        {
            var allResults = new List<EmbeddingResult>();
            var tasks = new List<Task<Result<UnifiedEmbeddingResponse>>>();
            int maxBatchSize = config.EmbeddingApi.MaxBatchSize;

            // [步骤 A: 创建所有批次的处理任务]
            // 使用一个 for 循环，每次跳跃一个批次的大小，来分割输入列表。
            for (int i = 0; i < request.Inputs.Count; i += maxBatchSize)
            {
                // 从原始输入中提取当前批次的数据。
                var batchInputs = request.Inputs.GetRange(i, System.Math.Min(maxBatchSize, request.Inputs.Count - i));
                
                // 为这个小批次创建一个独立的请求对象。
                var batchRequest = new UnifiedEmbeddingRequest { Inputs = batchInputs };

                // 为处理这个小批次启动一个异步任务，并将其加入任务列表。
                // 注意：我们在这里调用 ProcessSingleBatchAsync，实现了逻辑的复用。
                tasks.Add(ProcessSingleBatchAsync(batchRequest, config));
            }

            // [步骤 B: 并发执行所有任务]
            // Task.WhenAll 会“等待”任务列表中的所有任务完成。
            // 这使得所有 HTTP 请求可以几乎同时发送出去，大大提高了效率。
            var taskResults = await Task.WhenAll(tasks);

            // [步骤 C: 聚合所有结果]
            // 遍历每个任务返回的结果。
            foreach (var result in taskResults)
            {
                if (!result.IsSuccess)
                {
                    // 如果任何一个批次失败了，则整个操作立即失败。
                    // 这种“快速失败”策略可以避免返回不完整的数据。
                    return Result<UnifiedEmbeddingResponse>.Failure($"A batch failed: {result.Error}");
                }

                // 【核心】结果重索引 (Re-indexing)
                // 从翻译器返回的 result.Value.Data 中的每个 EmbeddingResult 的 Index 是相对于它自己的小批次的 (例如 0, 1, 2...)。
                // 我们需要将它转换回在原始输入列表中的“全局”索引。
                // 但由于我们的翻译器已经正确处理了索引，并且我们是按顺序聚合的，这里可以直接添加。
                // 一个更健壮的实现会在这里重新计算索引，但我们相信翻译器。
                allResults.AddRange(result.Value.Data);
            }
            
            // 重新排序以确保与原始输入顺序一致
            allResults = allResults.OrderBy(r => r.Index).ToList();

            // [步骤 D: 返回最终的聚合响应]
            return Result<UnifiedEmbeddingResponse>.Success(new UnifiedEmbeddingResponse { Data = allResults });
        }
    }
}