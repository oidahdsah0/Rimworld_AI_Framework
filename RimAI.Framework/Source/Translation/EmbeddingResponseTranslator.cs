using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RimAI.Framework.Configuration.Models;
using RimAI.Framework.Shared.Logging;
using RimAI.Framework.Shared.Models;
using RimAI.Framework.Translation.Models;
using System.Threading;

namespace RimAI.Framework.Translation
{
    public class EmbeddingResponseTranslator
    {
        public async Task<Result<UnifiedEmbeddingResponse>> TranslateAsync(HttpResponseMessage httpResponse, MergedEmbeddingConfig config, CancellationToken cancellationToken)
        {
            var jsonString = await httpResponse.Content.ReadAsStringAsync();
            cancellationToken.ThrowIfCancellationRequested();

            if (string.IsNullOrWhiteSpace(jsonString))
            {
                return Result<UnifiedEmbeddingResponse>.Failure("Empty response from embedding server.");
            }

            try
            {
                var jObject = JObject.Parse(jsonString);

                // 【修复】通过 config.Template.EmbeddingApi 访问路径
                var dataListPath = config.Template.EmbeddingApi.ResponsePaths.DataList;
                var embeddingPath = config.Template.EmbeddingApi.ResponsePaths.Embedding;
                var indexPath = config.Template.EmbeddingApi.ResponsePaths.Index;

                var dataArray = jObject.SelectToken(dataListPath) as JArray;
                if (dataArray == null)
                {
                    return Result<UnifiedEmbeddingResponse>.Failure($"Could not find data list at path: '{dataListPath}' in response.");
                }

                var results = dataArray.Select(item => new EmbeddingResult
                {
                    // 【修复】使用 .ToList() 将 float[] 转换为 List<float>
                    Embedding = item.SelectToken(embeddingPath)?.ToObject<float[]>()?.ToList(),
                    Index = item.SelectToken(indexPath)?.Value<int>() ?? 0
                }).ToList();

                return Result<UnifiedEmbeddingResponse>.Success(new UnifiedEmbeddingResponse { Data = results });
            }
            catch (JsonReaderException ex)
            {
                RimAILogger.Error($"Failed to parse embedding JSON response: {ex.Message}. Response body: {jsonString.Substring(0, 500)}");
                return Result<UnifiedEmbeddingResponse>.Failure($"Invalid JSON response from server. Details: {ex.Message}");
            }
        }
    }
}