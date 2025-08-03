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
using System.Threading;
using System.Threading.Tasks;

namespace RimAI.Framework.Core
{
    public class EmbeddingManager
    {
        // ... (成员变量和构造函数保持不变) ...
        private readonly SettingsManager _settingsManager;
        private readonly EmbeddingRequestTranslator _requestTranslator;
        private readonly HttpExecutor _httpExecutor;
        private readonly EmbeddingResponseTranslator _responseTranslator;

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

        public async Task<Result<UnifiedEmbeddingResponse>> ProcessRequestAsync(UnifiedEmbeddingRequest request, string providerId, CancellationToken cancellationToken)
        {
            var configResult = _settingsManager.GetMergedConfig(providerId);
            if (!configResult.IsSuccess)
            {
                return Result<UnifiedEmbeddingResponse>.Failure(configResult.Error);
            }
            var config = configResult.Value;

            cancellationToken.ThrowIfCancellationRequested();

            // 【访问修正】从 MergedConfig.EmbeddingApi 获取 MaxBatchSize
            int maxBatchSize = config.EmbeddingApi?.MaxBatchSize ?? 1;

            if (request.Inputs.Count <= maxBatchSize)
            {
                return await ProcessSingleBatchAsync(request, config, cancellationToken);
            }
            else
            {
                return await ProcessBatchesConcurrentlyAsync(request, config, cancellationToken);
            }
        }
        
        private async Task<Result<UnifiedEmbeddingResponse>> ProcessSingleBatchAsync(UnifiedEmbeddingRequest request, Configuration.Models.MergedConfig config, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var httpRequest = _requestTranslator.Translate(request, config);
            
            var httpResult = await _httpExecutor.ExecuteAsync(httpRequest, cancellationToken);
            if (httpResult.IsFailure)
            {
                return Result<UnifiedEmbeddingResponse>.Failure(httpResult.Error);
            }

            var httpResponse = httpResult.Value;
            if (!httpResponse.IsSuccessStatusCode)
            {
                 // Embedding API 失败时通常也会返回包含错误信息的JSON体
                var errorContent = await httpResponse.Content.ReadAsStringAsync();
                return Result<UnifiedEmbeddingResponse>.Failure($"Request failed with status code {httpResponse.StatusCode}: {errorContent}");
            }

            // 【调用修正】确保 TranslateAsync 调用了我们即将修复的、有3个参数的版本
            // (我们目前假设 EmbeddingResponseTranslator.TranslateAsync 也需要一个 token)
            var finalResponse = await _responseTranslator.TranslateAsync(httpResponse, config, cancellationToken);
            return Result<UnifiedEmbeddingResponse>.Success(finalResponse);
        }

        private async Task<Result<UnifiedEmbeddingResponse>> ProcessBatchesConcurrentlyAsync(UnifiedEmbeddingRequest request, Configuration.Models.MergedConfig config, CancellationToken cancellationToken)
        {
            var allResults = new List<EmbeddingResult>();
            // 【访问修正】从 MergedConfig.EmbeddingApi 获取 MaxBatchSize
            int maxBatchSize = config.EmbeddingApi?.MaxBatchSize ?? 1;
            
            // 【逻辑修正】创建一个任务列表来存储每个批次的处理任务
            var tasks = new List<Task<Result<UnifiedEmbeddingResponse>>>();

            for (int i = 0; i < request.Inputs.Count; i += maxBatchSize)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var batchInputs = request.Inputs.GetRange(i, System.Math.Min(maxBatchSize, request.Inputs.Count - i));
                var batchRequest = new UnifiedEmbeddingRequest { Inputs = batchInputs };

                tasks.Add(ProcessSingleBatchAsync(batchRequest, config, cancellationToken));
            }

            var taskResults = await Task.WhenAll(tasks);

            foreach (var result in taskResults)
            {
                if (result.IsFailure)
                {
                    // 如果任何一个批次失败了，则让整个操作失败，并返回第一个遇到的错误信息。
                    return Result<UnifiedEmbeddingResponse>.Failure($"A batch failed: {result.Error}");
                }
                
                allResults.AddRange(result.Value.Data);
            }
            
            allResults = allResults.OrderBy(r => r.Index).ToList();

            return Result<UnifiedEmbeddingResponse>.Success(new UnifiedEmbeddingResponse { Data = allResults });
        }
    }
}