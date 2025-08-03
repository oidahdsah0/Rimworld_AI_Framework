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
//  - 【新增】可取消 (Cancellable): 所有操作，包括并发批量处理，都可以通过 CancellationToken 被安全地中断。
// =====================================================================================================================

using RimAI.Framework.Configuration;
using RimAI.Framework.Execution;
using RimAI.Framework.Shared.Models;
using RimAI.Framework.Translation;
using RimAI.Framework.Translation.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading; // 【新增】引入 CancellationToken
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
        /// <param name="cancellationToken">【新增】用于中断操作的令牌。</param>
        public async Task<Result<UnifiedEmbeddingResponse>> ProcessRequestAsync(UnifiedEmbeddingRequest request, string providerId, CancellationToken cancellationToken)
        {
            var configResult = _settingsManager.GetMergedConfig(providerId);
            if (!configResult.IsSuccess)
            {
                return Result<UnifiedEmbeddingResponse>.Failure(configResult.Error);
            }
            var config = configResult.Value;

            // 在进入任何流程前，先检查一次。
            cancellationToken.ThrowIfCancellationRequested();

            int maxBatchSize = config.EmbeddingApi.MaxBatchSize;

            if (request.Inputs.Count <= maxBatchSize)
            {
                // 【修改】将 cancellationToken 传递给简单路径。
                return await ProcessSingleBatchAsync(request, config, cancellationToken);
            }
            else
            {
                // 【修改】将 cancellationToken 传递给并发路径。
                return await ProcessBatchesConcurrentlyAsync(request, config, cancellationToken);
            }
        }
        
        /// <summary>
        /// 私有方法：处理单个批次的请求（简单路径）。
        /// </summary>
        /// <param name="cancellationToken">【新增】用于中断操作的令牌。</param>
        private async Task<Result<UnifiedEmbeddingResponse>> ProcessSingleBatchAsync(UnifiedEmbeddingRequest request, Configuration.Models.MergedConfig config, CancellationToken cancellationToken)
        {
            // 在翻译前检查，减少不必要工作。
            cancellationToken.ThrowIfCancellationRequested();

            var httpRequest = _requestTranslator.Translate(request, config);
            
            // 【修改】将 cancellationToken 传递给 HttpExecutor。
            var httpResponseResult = await _httpExecutor.ExecuteAsync(httpRequest, cancellationToken);
            if (!httpResponseResult.IsSuccess)
            {
                return Result<UnifiedEmbeddingResponse>.Failure(httpResponseResult.Error);
            }

            // 【修改】将 cancellationToken 传递给 EmbeddingResponseTranslator。
            // 虽然 Embedding 响应通常不是流式的，但保持 API 一致性是个好习惯。
            var finalResponse = await _responseTranslator.TranslateAsync(httpResponseResult.Value, config, cancellationToken);
            return Result<UnifiedEmbeddingResponse>.Success(finalResponse);
        }

        /// <summary>
        /// 私有方法：并发处理多个批次的请求（复杂路径）。
        /// </summary>
        /// <param name="cancellationToken">【新增】用于中断操作的令牌。</param>
        private async Task<Result<UnifiedEmbeddingResponse>> ProcessBatchesConcurrentlyAsync(UnifiedEmbeddingRequest request, Configuration.Models.MergedConfig config, CancellationToken cancellationToken)
        {
            var allResults = new List<EmbeddingResult>();
            var tasks = new List<Task<Result<UnifiedEmbeddingResponse>>>();
            int maxBatchSize = config.EmbeddingApi.MaxBatchSize;

            // [步骤 A: 创建所有批次的处理任务]
            for (int i = 0; i < request.Inputs.Count; i += maxBatchSize)
            {
                // 在创建每个任务前都检查一下，如果已经取消，就没必要创建新任务了。
                cancellationToken.ThrowIfCancellationRequested();

                var batchInputs = request.Inputs.GetRange(i, System.Math.Min(maxBatchSize, request.Inputs.Count - i));
                var batchRequest = new UnifiedEmbeddingRequest { Inputs = batchInputs };

                // 【修改】将 cancellationToken 传递给每个并发任务。
                tasks.Add(ProcessSingleBatchAsync(batchRequest, config, cancellationToken));
            }

            // [步骤 B: 并发执行所有任务]
            // Task.WhenAll 天生就支持 CancellationToken。如果 token 被取消，
            // 它会立即停止等待，并抛出 OperationCanceledException，我们将在最外层捕获它。
            var taskResults = await Task.WhenAll(tasks);

            // [步骤 C: 聚合所有结果]
            foreach (var result in taskResults)
            {
                if (!result.IsSuccess)
                {
                    return Result<UnifiedEmbeddingResponse>.Failure($"A batch failed: {result.Error}");
                }
                
                allResults.AddRange(result.Value.Data);
            }
            
            allResults = allResults.OrderBy(r => r.Index).ToList();

            // [步骤 D: 返回最终的聚合响应]
            return Result<UnifiedEmbeddingResponse>.Success(new UnifiedEmbeddingResponse { Data = allResults });
        }
    }
}