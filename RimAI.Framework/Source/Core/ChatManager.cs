using System;
using System.Collections.Generic;
using System.Linq;
using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using RimAI.Framework.Configuration;
using RimAI.Framework.Execution;
using RimAI.Framework.Contracts;
using RimAI.Framework.Translation;
using RimAI.Framework.Configuration.Models;
using RimAI.Framework.Execution.Cache;
using System.Text;
using RimAI.Framework.Shared.Logging;

namespace RimAI.Framework.Core
{
    public partial class ChatManager
    {
        private static readonly ConcurrentDictionary<string, SemaphoreSlim> _streamGates = new ConcurrentDictionary<string, SemaphoreSlim>(StringComparer.Ordinal);
        private readonly SettingsManager _settingsManager;
        private readonly ChatRequestTranslator _requestTranslator;
        private readonly HttpExecutor _httpExecutor;
        private readonly ChatResponseTranslator _responseTranslator;
        private readonly ICacheService _cache;
        private readonly IInFlightCoordinator _inFlight;
        private static System.TimeSpan GetCacheTtl()
        {
            var s = Verse.LoadedModManager.GetMod<RimAI.Framework.UI.RimAIFrameworkMod>()?.GetSettings<RimAI.Framework.UI.RimAIFrameworkSettings>();
            var secs = s?.CacheTtlSeconds ?? 120;
            if (secs < 10) secs = 10;
            if (secs > 3600) secs = 3600;
            return System.TimeSpan.FromSeconds(secs);
        }

        public ChatManager(SettingsManager settingsManager, ChatRequestTranslator requestTranslator, HttpExecutor httpExecutor, ChatResponseTranslator responseTranslator, ICacheService cache, IInFlightCoordinator inFlight)
        {
            _settingsManager = settingsManager;
            _requestTranslator = requestTranslator;
            _httpExecutor = httpExecutor;
            _responseTranslator = responseTranslator;
            _cache = cache;
            _inFlight = inFlight;
        }

        public async Task<Result<UnifiedChatResponse>> ProcessRequestAsync(UnifiedChatRequest request, string providerId, CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(request?.ConversationId))
                return Result<UnifiedChatResponse>.Failure("ConversationId is required for chat requests.");
            var configResult = _settingsManager.GetMergedChatConfig(providerId);
            if (!configResult.IsSuccess)
                return Result<UnifiedChatResponse>.Failure(configResult.Error);
            
            var config = configResult.Value;
            cancellationToken.ThrowIfCancellationRequested();

            // Cache lookup (non-streaming)
            var cacheKey = CacheKeyBuilder.BuildChatKey(request, config);
            var settings = Verse.LoadedModManager.GetMod<RimAI.Framework.UI.RimAIFrameworkMod>()?.GetSettings<RimAI.Framework.UI.RimAIFrameworkSettings>();
            bool cacheEnabled = settings?.CacheEnabled ?? true;
            var cached = cacheEnabled ? await _cache.TryGetAsync<UnifiedChatResponse>(cacheKey, cancellationToken) : (false, null);
            if (cached.hit && cacheEnabled)
                return Result<UnifiedChatResponse>.Success(cached.value);

            // In-flight de-duplication for identical requests
            var result = await _inFlight.GetOrJoinAsync(cacheKey, async () =>
            {
                var httpRequest = _requestTranslator.Translate(request, config);
                try
                {
                    var body = await (httpRequest.Content?.ReadAsStringAsync() ?? System.Threading.Tasks.Task.FromResult<string>(null));
                    RimAILogger.Log(RequestLogFormatter.FormatProviderDispatch(
                        apiName: "Chat:NonStream",
                        providerId: providerId,
                        httpRequest: httpRequest,
                        requestBodyJson: body
                    ));
                }
                catch { }
                var httpResult = await _httpExecutor.ExecuteAsync(httpRequest, cancellationToken, isStreaming: false);
                if (httpResult.IsFailure)
                    return Result<UnifiedChatResponse>.Failure(httpResult.Error);

                var httpResponse = httpResult.Value;
                try
                {
                    var finalResponse = await _responseTranslator.TranslateAsync(httpResponse, config, cancellationToken);
                    if (!httpResponse.IsSuccessStatusCode)
                        return Result<UnifiedChatResponse>.Failure(finalResponse?.Message?.Content ?? $"Request failed: {httpResponse.StatusCode}", finalResponse);

                    return Result<UnifiedChatResponse>.Success(finalResponse);
                }
                finally
                {
                    httpResponse.Dispose();
                }
            });

            if (result.IsSuccess && cacheEnabled)
            {
                await _cache.SetAsync(cacheKey, result.Value, GetCacheTtl(), cancellationToken);
            }

            return result;
        }

        public async IAsyncEnumerable<Result<UnifiedChatChunk>> ProcessStreamRequestAsync(UnifiedChatRequest request, string providerId, [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(request?.ConversationId))
            {
                yield return Result<UnifiedChatChunk>.Failure("ConversationId is required for chat requests.");
                yield break;
            }
            var configResult = _settingsManager.GetMergedChatConfig(providerId);
            if (!configResult.IsSuccess)
            {
                yield return Result<UnifiedChatChunk>.Failure(configResult.Error);
                yield break;
            }

            var config = configResult.Value;
            cancellationToken.ThrowIfCancellationRequested();

            // Cache check: if hit, emit pseudo-stream
            var cacheKey = CacheKeyBuilder.BuildChatKey(request, config);
            var settings2 = Verse.LoadedModManager.GetMod<RimAI.Framework.UI.RimAIFrameworkMod>()?.GetSettings<RimAI.Framework.UI.RimAIFrameworkSettings>();
            bool cacheEnabled2 = settings2?.CacheEnabled ?? true;
            var cached = cacheEnabled2 ? await _cache.TryGetAsync<UnifiedChatResponse>(cacheKey, cancellationToken) : (false, null);
            if (cacheEnabled2 && cached.hit && cached.value?.Message != null)
            {
                foreach (var chunk in SliceIntoChunks(cached.value))
                {
                    yield return Result<UnifiedChatChunk>.Success(chunk);
                }
                yield break;
            }

            // Concurrency gate for real streaming to avoid exhausting underlying connection limits
            var limit = config.ConcurrencyLimit;
            // 流式并发限制为 3：即使配置更大也按 3 封顶；未配置或非法时使用 3
            if (limit <= 0) limit = 3;
            if (limit > 3) limit = 3;
            var gate = _streamGates.GetOrAdd(providerId, _ => new SemaphoreSlim(limit));
            await gate.WaitAsync(cancellationToken);

            var httpRequest = _requestTranslator.Translate(request, config);
            try
            {
                var body = await (httpRequest.Content?.ReadAsStringAsync() ?? System.Threading.Tasks.Task.FromResult<string>(null));
                RimAILogger.Log(RequestLogFormatter.FormatProviderDispatch(
                    apiName: "Chat:Stream",
                    providerId: providerId,
                    httpRequest: httpRequest,
                    requestBodyJson: body
                ));
            }
            catch { }
            var httpResult = await _httpExecutor.ExecuteAsync(httpRequest, cancellationToken, isStreaming: true);
            if (httpResult.IsFailure)
            {
                yield return Result<UnifiedChatChunk>.Failure(httpResult.Error);
                gate.Release();
                yield break;
            }

            var httpResponse = httpResult.Value;
            if (!httpResponse.IsSuccessStatusCode)
            {
                try
                {
                    var errorResponse = await _responseTranslator.TranslateAsync(httpResponse, config, cancellationToken);
                    yield return Result<UnifiedChatChunk>.Failure(errorResponse?.Message?.Content ?? $"Request failed: {httpResponse.StatusCode}");
                }
                finally
                {
                    httpResponse.Dispose();
                    gate.Release();
                }
                yield break;
            }

            var builder = new StringBuilder();
            List<ToolCall> lastToolCalls = null;
            string finalFinishReason = null;
            try
            {
                await foreach (var chunk in _responseTranslator.TranslateStreamAsync(httpResponse, config, cancellationToken))
                {
                    if (chunk.ContentDelta != null)
                        builder.Append(chunk.ContentDelta);
                    if (chunk.ToolCalls != null)
                        lastToolCalls = chunk.ToolCalls;
                    if (!string.IsNullOrEmpty(chunk.FinishReason))
                        finalFinishReason = chunk.FinishReason;
                    yield return Result<UnifiedChatChunk>.Success(chunk);
                }
            }
            finally
            {
                httpResponse.Dispose();
                gate.Release();
            }

            // After streaming, write to cache only if it appears complete
            bool shouldCache = !string.IsNullOrEmpty(finalFinishReason)
                               && (finalFinishReason == "stop" || finalFinishReason == "tool_calls")
                               && (builder.Length > 0 || (lastToolCalls != null && lastToolCalls.Count > 0));
            var settings3 = Verse.LoadedModManager.GetMod<RimAI.Framework.UI.RimAIFrameworkMod>()?.GetSettings<RimAI.Framework.UI.RimAIFrameworkSettings>();
            bool cacheEnabled3 = settings3?.CacheEnabled ?? true;
            if (shouldCache && cacheEnabled3)
            {
                var cachedResponse = new UnifiedChatResponse
                {
                    FinishReason = finalFinishReason,
                    Message = new ChatMessage { Role = "assistant", Content = builder.ToString(), ToolCalls = lastToolCalls }
                };
                await _cache.SetAsync(cacheKey, cachedResponse, GetCacheTtl(), cancellationToken);
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
                if (string.IsNullOrEmpty(req?.ConversationId))
                {
                    return Result<UnifiedChatResponse>.Failure("ConversationId is required for chat requests.");
                }
                await semaphore.WaitAsync(cancellationToken);
                try { return await ProcessRequestAsync(req, providerId, cancellationToken); }
                finally { semaphore.Release(); }
            });

            return (await Task.WhenAll(tasks)).ToList();
        }

        public async Task<Result<bool>> InvalidateConversationCacheAsync(string providerId, string conversationId, CancellationToken cancellationToken)
        {
            var configResult = _settingsManager.GetMergedChatConfig(providerId);
            if (!configResult.IsSuccess)
                return Result<bool>.Failure(configResult.Error);
            if (string.IsNullOrEmpty(conversationId))
                return Result<bool>.Failure("ConversationId is required for cache invalidation.");

            var cfg = configResult.Value;
            var prefix = CacheKeyBuilder.BuildChatConversationPrefix(cfg, conversationId);
            await _cache.InvalidateByPrefixAsync(prefix, cancellationToken);
            return Result<bool>.Success(true);
        }
    }

    // --- Private helpers ---
    partial class ChatManager
    {
        private static IEnumerable<UnifiedChatChunk> SliceIntoChunks(UnifiedChatResponse response)
        {
            if (response?.Message?.Content == null)
            {
                yield return new UnifiedChatChunk { FinishReason = response?.FinishReason ?? "stop", ToolCalls = response?.Message?.ToolCalls };
                yield break;
            }

            var text = response.Message.Content;
            const int chunkSize = 48; // small slices for pseudo-stream
            for (int i = 0; i < text.Length; i += chunkSize)
            {
                var len = System.Math.Min(chunkSize, text.Length - i);
                var piece = text.Substring(i, len);
                yield return new UnifiedChatChunk { ContentDelta = piece };
            }
            yield return new UnifiedChatChunk { FinishReason = response.FinishReason ?? "stop", ToolCalls = response.Message.ToolCalls };
        }
    }
}
