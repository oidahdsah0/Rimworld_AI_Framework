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
using System.Threading; // [新增]
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
        /// <param name="cancellationToken">【新增】令牌，用于在读取内容时响应取消信号。</param>
        /// <returns>一个包含所有结果的、统一格式的 UnifiedEmbeddingResponse 对象。</returns>
        // [C# 知识点] "async Task<T>" 是异步方法的标志。
        //  - async: 告诉编译器这个方法包含 await 关键字，需要进行异步处理。
        //  - Task<T>: 表示这个方法最终会返回一个 T 类型的结果，但可能不会立即返回。
        //    调用者可以使用 await 来“等待”这个结果，而不会阻塞当前线程。
        public async Task<UnifiedEmbeddingResponse> TranslateAsync(HttpResponseMessage response, MergedConfig config, CancellationToken cancellationToken)
        {
            // 【修改】将 cancellationToken 传递给 ReadAsStringAsync
            var responseBody = await response.Content.ReadAsStringAsync();
            cancellationToken.ThrowIfCancellationRequested(); // 在解析前再次检查

            var jsonResponse = JObject.Parse(responseBody);
            
            var paths = config.EmbeddingApi.ResponsePaths;
            
            var dataListToken = jsonResponse.SelectToken(paths.DataList);

            var results = new List<EmbeddingResult>();
            if (dataListToken is JArray dataArray)
            {
                foreach (var item in dataArray)
                {
                    // 在循环内部也检查取消，对于非常大的 embedding 列表有好处
                    cancellationToken.ThrowIfCancellationRequested();

                    var embeddingToken = item.SelectToken(paths.Embedding);
                    var indexToken = item.SelectToken(paths.Index);

                    var embedding = embeddingToken?.ToObject<List<float>>();
                    var index = indexToken?.ToObject<int>() ?? 0;

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
            
            return new UnifiedEmbeddingResponse
            {
                Data = results
            };
        }
    }
}