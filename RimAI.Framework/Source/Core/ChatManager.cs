using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using RimAI.Framework.Configuration;
using RimAI.Framework.Execution;
using RimAI.Framework.Contracts;
using RimAI.Framework.Translation;
using RimAI.Framework.Configuration.Models;

namespace RimAI.Framework.Core
{
    public class ChatManager
    {
        private readonly SettingsManager _settingsManager;
        private readonly ChatRequestTranslator _requestTranslator;
        private readonly HttpExecutor _httpExecutor;
        private readonly ChatResponseTranslator _responseTranslator;

        public ChatManager(SettingsManager settingsManager, ChatRequestTranslator requestTranslator, HttpExecutor httpExecutor, ChatResponseTranslator responseTranslator)
        {
            _settingsManager = settingsManager;
            _requestTranslator = requestTranslator;
            _httpExecutor = httpExecutor;
            _responseTranslator = responseTranslator;
        }

        public async Task<Result<UnifiedChatResponse>> ProcessRequestAsync(UnifiedChatRequest request, string providerId, CancellationToken cancellationToken)
        {
            var configResult = _settingsManager.GetMergedChatConfig(providerId);
            if (!configResult.IsSuccess)
                return Result<UnifiedChatResponse>.Failure(configResult.Error);
            
            var config = configResult.Value;
            cancellationToken.ThrowIfCancellationRequested();
            
            var httpRequest = _requestTranslator.Translate(request, config);
            var httpResult = await _httpExecutor.ExecuteAsync(httpRequest, cancellationToken);
            if (httpResult.IsFailure)
                return Result<UnifiedChatResponse>.Failure(httpResult.Error);

            var httpResponse = httpResult.Value;
            var finalResponse = await _responseTranslator.TranslateAsync(httpResponse, config, cancellationToken);
            if (!httpResponse.IsSuccessStatusCode)
                return Result<UnifiedChatResponse>.Failure(finalResponse?.Message?.Content ?? $"Request failed: {httpResponse.StatusCode}", finalResponse);

            return Result<UnifiedChatResponse>.Success(finalResponse);
        }

        public async IAsyncEnumerable<Result<UnifiedChatChunk>> ProcessStreamRequestAsync(UnifiedChatRequest request, string providerId, [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            var configResult = _settingsManager.GetMergedChatConfig(providerId);
            if (!configResult.IsSuccess)
            {
                yield return Result<UnifiedChatChunk>.Failure(configResult.Error);
                yield break;
            }

            var config = configResult.Value;
            cancellationToken.ThrowIfCancellationRequested();

            var httpRequest = _requestTranslator.Translate(request, config);
            var httpResult = await _httpExecutor.ExecuteAsync(httpRequest, cancellationToken);
            if (httpResult.IsFailure)
            {
                yield return Result<UnifiedChatChunk>.Failure(httpResult.Error);
                yield break;
            }

            var httpResponse = httpResult.Value;
            if (!httpResponse.IsSuccessStatusCode)
            {
                var errorResponse = await _responseTranslator.TranslateAsync(httpResponse, config, cancellationToken);
                yield return Result<UnifiedChatChunk>.Failure(errorResponse?.Message?.Content ?? $"Request failed: {httpResponse.StatusCode}");
                yield break;
            }
            
            await foreach (var chunk in _responseTranslator.TranslateStreamAsync(httpResponse, config, cancellationToken))
            {
                yield return Result<UnifiedChatChunk>.Success(chunk);
            }
        }

        public async Task<List<Result<UnifiedChatResponse>>> ProcessBatchRequestAsync(List<UnifiedChatRequest> requests, string providerId, CancellationToken cancellationToken)
        {
            var configResult = _settingsManager.GetMergedChatConfig(providerId);
            if (!configResult.IsSuccess)
                return requests.Select(r => Result<UnifiedChatResponse>.Failure(configResult.Error)).ToList();
            
            var config = configResult.Value;
            var semaphore = new SemaphoreSlim(config.ConcurrencyLimit);
            var tasks = requests.Select(async req => {
                await semaphore.WaitAsync(cancellationToken);
                try { return await ProcessRequestAsync(req, providerId, cancellationToken); }
                finally { semaphore.Release(); }
            });

            return (await Task.WhenAll(tasks)).ToList();
        }
    }
}
