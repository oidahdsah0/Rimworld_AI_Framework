// =====================================================================================================================
// 文件: EmbeddingRequestTranslator.cs
//
// 作用:
//  这是一个“数据驱动”的翻译器，其核心职责是将框架内部的 UnifiedEmbeddingRequest 模型，
//  翻译成一个可以被任何外部 AI 服务商理解的 HttpRequestMessage。
//
//  它本身不包含任何针对特定服务商的逻辑，所有的翻译规则都来自于传入的 MergedConfig 对象。
//  这使得它具有极高的灵活性和可扩展性。
// =====================================================================================================================

// 引入必要的库
using Newtonsoft.Json.Linq; // 引入强大的 JSON.NET 库中的 JObject，用于动态构建 JSON
using RimAI.Framework.Configuration.Models; // 引入我们的配置模型，特别是 MergedConfig
using RimAI.Framework.Translation.Models;  // 引入我们的统一模型，特别是 UnifiedEmbeddingRequest
using System; // 引入基础库，例如 Uri
using System.Net.Http; // 引入用于创建 HTTP 请求的类
using System.Net.Http.Headers; // 引入用于处理 HTTP 头的类
using System.Text; // 引入用于处理文本编码的类 (例如 UTF-8)

namespace RimAI.Framework.Translation
{
    /// <summary>
    /// 将统一的 Embedding 请求翻译成特定于提供商的 HTTP 请求。
    /// </summary>
    public class EmbeddingRequestTranslator
    {
        /// <summary>
        /// 执行翻译操作。
        /// </summary>
        /// <param name="request">框架内部的统一 Embedding 请求。</param>
        /// <param name="config">包含了所有模板和用户设置的合并后配置，是本次翻译的“规则手册”。</param>
        /// <returns>一个配置完成，随时可以被 HttpExecutor 发送的 HttpRequestMessage 对象。</returns>
        public HttpRequestMessage Translate(UnifiedEmbeddingRequest request, MergedConfig config)
        {
            // [步骤 1: 动态构建 JSON 请求体]
            // 我们不使用固定的 C# 类去序列化，因为不同 API 的 JSON 结构千差万别。
            // 使用 JObject 可以让我们像操作字典一样，动态地添加字段，非常灵活。
            var requestBody = new JObject();

            // 如果模板中定义了 staticParameters，先把它们作为基础添加入请求体。
            // 这是“逃生舱口”，用于支持非标准字段。
            if (config.StaticParameters != null)
            {
                requestBody.Merge(config.StaticParameters);
            }

            // 从配置中获取 Embedding API 的请求路径映射规则。
            var paths = config.EmbeddingApi.RequestPaths;

            // [步骤 2: 根据路径规则，将统一请求的数据填入 JObject]
            // JObject[key] = value; 这种语法会自动创建对应的 JSON 字段。
            // 例如，如果 paths.Model 是 "model"，那么这里就会生成 "model": "text-embedding-3-small"
            requestBody[paths.Model] = config.EmbeddingModel;
            requestBody[paths.Input] = JArray.FromObject(request.Inputs); // 将输入的 List<string> 转换成 JSON 数组

            // [步骤 3: 创建并配置 HttpRequestMessage 对象]
            // 这就是我们要发出的“网络数据包”。
            var httpRequest = new HttpRequestMessage
            {
                // 使用 POST 方法，因为我们要在请求体中发送数据。
                Method = HttpMethod.Post, 
                
                // 设置请求的目标地址，从配置中读取。
                RequestUri = new Uri(config.EmbeddingApi.Endpoint), 
                
                // 设置请求体的内容。
                // 1. requestBody.ToString() 将 JObject 转换成 JSON 字符串。
                // 2. new StringContent(...) 将字符串封装成 HTTP 内容。
                // 3. 必须指定编码 (UTF-8) 和媒体类型 (application/json)。
                Content = new StringContent(requestBody.ToString(), Encoding.UTF8, "application/json")
            };

            // [步骤 4: 添加必要的 HTTP 头 (Headers)]
            // HTTP 头包含了认证、内容类型等元数据。
            
            // 添加模板中定义的通用头，例如 "Content-Type": "application/json"
            if (config.Http.Headers != null)
            {
                foreach (var header in config.Http.Headers)
                {
                    httpRequest.Headers.TryAddWithoutValidation(header.Key, header.Value);
                }
            }

            // 添加用户自定义的头，如果与模板中的键冲突，用户的会覆盖。
            if (config.CustomHeaders != null)
            {
                foreach (var header in config.CustomHeaders)
                {
                    // 先移除可能存在的同名头，再添加，确保用户优先。
                    httpRequest.Headers.Remove(header.Key);
                    httpRequest.Headers.TryAddWithoutValidation(header.Key, header.Value);
                }
            }

            // 添加最重要的“认证”头。
            // 例如，生成 "Authorization: Bearer sk-..."
            // AuthenticationHeaderValue 会正确处理 scheme 和 key 之间的空格。
            httpRequest.Headers.Authorization = new AuthenticationHeaderValue(config.Http.AuthScheme, config.ApiKey);
            
            // [步骤 5: 返回配置好的请求]
            return httpRequest;
        }
    }
}