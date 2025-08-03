using RimAI.Framework.Configuration;
using RimAI.Framework.Execution;
using RimAI.Framework.Shared.Models;
using RimAI.Framework.Translation;
using RimAI.Framework.Translation.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using RimAI.Framework.Configuration.Models;

namespace RimAI.Framework.Core
{
    public class EmbeddingManager
    {
        private readonly SettingsManager _settingsManager;
        private readonly EmbeddingRequestTranslator _requestTranslator;
        private readonly HttpExecutor _httpExecutor;
        private readonly EmbeddingResponseTranslator _responseTranslator;

        public EmbeddingManager(SettingsManager settingsManager, EmbeddingRequestTranslator requestTranslator, HttpExecutor httpExecutor, EmbeddingResponseTranslator responseTranslator)
        {
            _settingsManager = settingsManager;
            _requestTranslator = requestTranslator;
            _httpExecutor = httpExecutor;
            _responseTranslator = responseTranslator;
        }

        public async Task<Result<UnifiedEmbeddingResponse>> ProcessRequestAsync(UnifiedEmbeddingRequest request, string providerId, CancellationToken cancellationToken)
        {
            var configResult = _settingsManager.GetMergedEmbeddingConfig(providerId);
            if (!configResult.IsSuccess)
                return Result<UnifiedEmbeddingResponse>.Failure(configResult.Error);
            
            var config = configResult.Value;
            cancellationToken.ThrowIfCancellationRequested();

            if (request.Inputs.Count <= config.MaxBatchSize)
                return await ProcessSingleBatchAsync(request, config, cancellationToken);
            else
                return await ProcessBatchesConcurrentlyAsync(request, config, cancellationToken);
        }
        
        private async Task<Result<UnifiedEmbeddingResponse>> ProcessSingleBatchAsync(UnifiedEmbeddingRequest request, MergedEmbeddingConfig config, CancellationToken cancellationToken)
        {
            var httpRequest = _requestTranslator.Translate(request, config);
            var httpResult = await _httpExecutor.ExecuteAsync(httpRequest, cancellationToken);
            if (httpResult.IsFailure)
                return Result<UnifiedEmbeddingResponse>.Failure(httpResult.Error);

            var httpResponse = httpResult.Value;
            if (!httpResponse.IsSuccessStatusCode) {
                var errorContent = await httpResponse.Content.ReadAsStringAsync();
                return Result<UnifiedEmbeddingResponse>.Failure($"Request failed: {httpResponse.StatusCode}: {errorContent}");
            }

            return await _responseTranslator.TranslateAsync(httpResponse, config, cancellationToken);
        }

        private async Task<Result<UnifiedEmbeddingResponse>> ProcessBatchesConcurrentlyAsync(UnifiedEmbeddingRequest request, MergedEmbeddingConfig config, CancellationToken cancellationToken)
        {
            var tasks = new List<Task<Result<UnifiedEmbeddingResponse>>>();
            for (int i = 0; i < request.Inputs.Count; i += config.MaxBatchSize) {
                var batchInputs = request.Inputs.GetRange(i, System.Math.Min(config.MaxBatchSize, request.Inputs.Count - i));
                tasks.Add(ProcessSingleBatchAsync(new UnifiedEmbeddingRequest { Inputs = batchInputs }, config, cancellationToken));
            }

            var taskResults = await Task.WhenAll(tasks);
            if (taskResults.Any(r => r.IsFailure))
                return taskResults.First(r => r.IsFailure);

            var allEmbeddings = taskResults.SelectMany(r => r.Value.Data).OrderBy(e => e.Index).ToList();
            return Result<UnifiedEmbeddingResponse>.Success(new UnifiedEmbeddingResponse { Data = allEmbeddings });
        }
    }
}