// =====================================================================================================================
// 文件: EmbeddingResponseTranslator.cs
//
// 作用:
//  这是一个“数据驱动”的响应翻译器。它的职责是将从外部 AI 服务收到的原始 HttpResponseMessage，
//  解析并翻译成框架内部统一的 UnifiedEmbeddingResponse 模型。
//
//  与请求翻译器一样，它完全依赖于 MergedConfig 提供的“翻译规则”（特别是 ResponsePaths），
//  从而实现了对不同 API 响应格式的灵活适配。
// =====================================================================================================================

using Newtonsoft.Json.Linq; // 引入 JSON.NET 库，用于解析和查询 JSON
using RimAI.Framework.Configuration.Models; // 引入配置模型
using RimAI.Framework.Translation.Models;  // 引入统一模型
using System.Collections.Generic; // 引入 List<T>
using System.Net.Http; // 引入 HttpResponseMessage
using System.Threading.Tasks; // 引入 Task，用于异步编程

namespace RimAI.Framework.Translation
{
    /// <summary>
    /// 将特定于提供商的 HTTP 响应翻译回统一的 Embedding 响应。
    /// </summary>
    public class EmbeddingResponseTranslator
    {
        /// <summary>
        /// 执行异步翻译操作。
        /// </summary>
        /// <param name="response">从 HttpExecutor 收到的原始 HTTP 响应。</param>
        /// <param name="config">包含了所有翻译规则的合并后配置。</param>
        /// <returns>一个包含所有结果的、统一格式的 UnifiedEmbeddingResponse 对象。</returns>
        // [C# 知识点] "async Task<T>" 是异步方法的标志。
        //  - async: 告诉编译器这个方法包含 await 关键字，需要进行异步处理。
        //  - Task<T>: 表示这个方法最终会返回一个 T 类型的结果，但可能不会立即返回。
        //    调用者可以使用 await 来“等待”这个结果，而不会阻塞当前线程。
        public async Task<UnifiedEmbeddingResponse> TranslateAsync(HttpResponseMessage response, MergedConfig config)
        {
            // [步骤 1: 异步读取响应内容]
            // response.Content.ReadAsStringAsync() 会异步地将响应体读取为一个字符串。
            // 使用 await 关键字，可以在不阻塞线程的情况下等待这个操作完成。
            var responseBody = await response.Content.ReadAsStringAsync();

            // [步骤 2: 将响应字符串解析为 JObject]
            // JObject.Parse() 将 JSON 字符串转换成一个可以被查询和操作的动态对象。
            var jsonResponse = JObject.Parse(responseBody);

            // [步骤 3: 根据路径规则，提取核心数据]
            var paths = config.EmbeddingApi.ResponsePaths;
            
            // 使用 JObject.SelectToken() 方法，这是一个非常强大的功能。
            // 它允许我们使用点 (.) 分隔的路径字符串来查询 JSON 中的任意节点。
            // 例如，如果 paths.DataList 是 "data"，它就会找到 "data" 字段对应的 JSON 数组。
            var dataListToken = jsonResponse.SelectToken(paths.DataList);

            // [步骤 4: 遍历数据列表，并逐个解析成 EmbeddingResult]
            var results = new List<EmbeddingResult>();
            if (dataListToken is JArray dataArray) // 确保我们找到的是一个数组
            {
                foreach (var item in dataArray)
                {
                    // 在每个子对象中，再次使用 SelectToken 来提取具体的值。
                    var embeddingToken = item.SelectToken(paths.Embedding);
                    var indexToken = item.SelectToken(paths.Index);

                    // 将提取出的 JToken 转换成我们需要的 C# 类型。
                    // JToken.ToObject<T>() 是一个非常方便的转换方法。
                    var embedding = embeddingToken?.ToObject<List<float>>();
                    var index = indexToken?.ToObject<int>() ?? 0; // 如果索引不存在，默认给 0

                    if (embedding != null)
                    {
                        results.Add(new EmbeddingResult
                        {
                            Index = index,
                            Embedding = embedding
                        });
                    }
                }
            }
            
            // [步骤 5: 封装成最终的统一响应对象并返回]
            return new UnifiedEmbeddingResponse
            {
                Data = results
            };
        }
    }
}