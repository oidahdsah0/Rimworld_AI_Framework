using RimAI.Framework.Configuration;
using RimAI.Framework.Execution;
using RimAI.Framework.Contracts;
using RimAI.Framework.Translation;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using RimAI.Framework.Configuration.Models;
using RimAI.Framework.Execution.Cache;
using RimAI.Framework.Shared.Logging;

namespace RimAI.Framework.Core
{
    public class EmbeddingManager
    {
        private readonly SettingsManager _settingsManager;
        private readonly EmbeddingRequestTranslator _requestTranslator;
        private readonly HttpExecutor _httpExecutor;
        private readonly EmbeddingResponseTranslator _responseTranslator;
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

        public EmbeddingManager(SettingsManager settingsManager, EmbeddingRequestTranslator requestTranslator, HttpExecutor httpExecutor, EmbeddingResponseTranslator responseTranslator, ICacheService cache, IInFlightCoordinator inFlight)
        {
            _settingsManager = settingsManager;
            _requestTranslator = requestTranslator;
            _httpExecutor = httpExecutor;
            _responseTranslator = responseTranslator;
            _cache = cache;
            _inFlight = inFlight;
        }

        public async Task<Result<UnifiedEmbeddingResponse>> ProcessRequestAsync(UnifiedEmbeddingRequest request, string providerId, CancellationToken cancellationToken)
        {
            // 二次闸门：Embedding 总开关关闭则立即失败，避免绕过门面时触发网络
            if (!_settingsManager.IsEmbeddingEnabled())
                return Result<UnifiedEmbeddingResponse>.Failure("Embedding is disabled by settings.");

            var configResult = _settingsManager.GetMergedEmbeddingConfig(providerId);
            if (!configResult.IsSuccess)
                return Result<UnifiedEmbeddingResponse>.Failure(configResult.Error);
            
            var config = configResult.Value;
            cancellationToken.ThrowIfCancellationRequested();

            // Embedding cache: per-input granularity
            var inputs = request.Inputs ?? new List<string>();
            var uiSettings = Verse.LoadedModManager.GetMod<RimAI.Framework.UI.RimAIFrameworkMod>()?.GetSettings<RimAI.Framework.UI.RimAIFrameworkSettings>();
            bool cacheEnabled = uiSettings?.CacheEnabled ?? true;
            var uniqueInputs = inputs.Distinct().ToList();

            // Lookup cache for each input
            var indexMap = new Dictionary<string, List<int>>();
            for (int i = 0; i < inputs.Count; i++)
            {
                var s = inputs[i] ?? string.Empty;
                if (!indexMap.TryGetValue(s, out var list)) { list = new List<int>(); indexMap[s] = list; }
                list.Add(i);
            }

            var cacheHits = new Dictionary<string, EmbeddingResult>();
            var misses = new List<string>();
            foreach (var s in uniqueInputs)
            {
                var key = CacheKeyBuilder.BuildEmbeddingKey(s ?? string.Empty, config);
                var (hit, value) = cacheEnabled ? await _cache.TryGetAsync<UnifiedEmbeddingResponse>(key, cancellationToken) : (false, null);
                if (cacheEnabled && hit && value?.Data != null && value.Data.Count > 0)
                {
                    // We store per-input as a single-entry response; take index 0
                    cacheHits[s] = new EmbeddingResult { Index = 0, Embedding = value.Data[0].Embedding };
                }
                else
                {
                    misses.Add(s);
                }
            }

            var newResults = new Dictionary<string, EmbeddingResult>();
            if (misses.Count > 0)
            {
                // Batch the misses using provider max batch size, with in-flight de-dup per batch key
                var batches = new List<List<string>>();
                for (int i = 0; i < misses.Count; i += config.MaxBatchSize)
                    batches.Add(misses.GetRange(i, System.Math.Min(config.MaxBatchSize, misses.Count - i)));

                foreach (var batch in batches)
                {
                    // Build a synthetic key for the batch by joining individual keys
                    var batchKey = string.Join("|", batch.Select(s => CacheKeyBuilder.BuildEmbeddingKey(s, config)));
                    var batchResult = await _inFlight.GetOrJoinAsync(batchKey, async () =>
                    {
                        var httpReq = _requestTranslator.Translate(new UnifiedEmbeddingRequest { Inputs = batch }, config);
                        try
                        {
                            var body = await (httpReq.Content?.ReadAsStringAsync() ?? System.Threading.Tasks.Task.FromResult<string>(null));
                            RimAILogger.Log(RequestLogFormatter.FormatProviderDispatch(
                                apiName: "Embedding",
                                providerId: providerId,
                                httpRequest: httpReq,
                                requestBodyJson: body
                            ));
                        }
                        catch { }
                        var httpRes = await _httpExecutor.ExecuteAsync(httpReq, cancellationToken);
                        if (httpRes.IsFailure)
                            return Result<UnifiedEmbeddingResponse>.Failure(httpRes.Error);

                        var httpResponse = httpRes.Value;
                        try
                        {
                            if (!httpResponse.IsSuccessStatusCode)
                            {
                                var errorContent = await httpResponse.Content.ReadAsStringAsync();
                                return Result<UnifiedEmbeddingResponse>.Failure($"Request failed: {httpResponse.StatusCode}: {errorContent}");
                            }
                            return await _responseTranslator.TranslateAsync(httpResponse, config, cancellationToken);
                        }
                        finally { httpResponse.Dispose(); }
                    });

                    if (batchResult.IsFailure)
                        return batchResult; // propagate error

                    // Map results to inputs (provider should return embeddings aligned to input order)
                    var data = batchResult.Value.Data;
                    for (int i = 0; i < batch.Count; i++)
                    {
                        var s = batch[i];
                        var e = data.ElementAtOrDefault(i);
                        if (e != null)
                        {
                            var perInput = new EmbeddingResult { Index = 0, Embedding = e.Embedding };
                            newResults[s] = perInput;
                            // write per-input cache as single-entry response
                            var singleResponse = new UnifiedEmbeddingResponse { Data = new List<EmbeddingResult> { new EmbeddingResult { Index = 0, Embedding = e.Embedding } } };
                            var key = CacheKeyBuilder.BuildEmbeddingKey(s, config);
                            if (cacheEnabled)
                                await _cache.SetAsync(key, singleResponse, GetCacheTtl(), cancellationToken);
                        }
                    }
                }
            }

            // Merge cache hits and new results, then rebuild final response in original order
            var finalData = new List<EmbeddingResult>(inputs.Count);
            for (int idx = 0; idx < inputs.Count; idx++)
            {
                var s = inputs[idx];
                EmbeddingResult r = null;
                if (cacheHits.TryGetValue(s, out var hit)) r = hit;
                else if (newResults.TryGetValue(s, out var nr)) r = nr;
                if (r == null) return Result<UnifiedEmbeddingResponse>.Failure("Embedding cache coordination failed for one or more inputs.");
                finalData.Add(new EmbeddingResult { Index = idx, Embedding = r.Embedding });
            }

            return Result<UnifiedEmbeddingResponse>.Success(new UnifiedEmbeddingResponse { Data = finalData });
        }
    }
}