// 引入必要的命名空间
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RimAI.Framework.Configuration.Models; // 我们的配置模型
using RimAI.Framework.Translation.Models;   // 我们的统一模型

namespace RimAI.Framework.Translation
{
    /// <summary>
    /// 聊天请求翻译器。
    /// 负责将统一的内部模型 (UnifiedChatRequest) 翻译成特定于提供商的 HttpRequestMessage。
    /// 这是一个完全由 MergedConfig 驱动的“数据驱动”翻译器。
    /// </summary>
    public class ChatRequestTranslator
    {
        /// <summary>
        /// 将统一聊天请求翻译成一个HTTP请求。
        /// </summary>
        /// <param name="unifiedRequest">统一的内部聊天请求。</param>
        /// <param name="config">包含了所有适配规则的合并后配置。</param>
        /// <returns>一个可以被 HttpExecutor 直接发送的 HttpRequestMessage。</returns>
        public HttpRequestMessage Translate(UnifiedChatRequest unifiedRequest, MergedConfig config)
        {
            // 1. 创建一个最终要构建的JSON body对象
            var body = new JObject();

            // 2. 根据 requestPaths 映射标准参数
            var paths = config.ChatRequestPaths;
            body[paths?.Model ?? "model"] = config.ChatModel;
            body[paths?.Stream ?? "stream"] = unifiedRequest.Stream;

            if (config.Temperature.HasValue)
                body[paths?.Temperature ?? "temperature"] = config.Temperature.Value;
            if (config.TopP.HasValue)
                body[paths?.TopP ?? "top_p"] = config.TopP.Value;

            // 3. 构建 messages 数组
            var messagesArray = new JArray();
            foreach (var msg in unifiedRequest.Messages)
            {
                var messageObject = new JObject
                {
                    ["role"] = msg.Role,
                    ["content"] = msg.Content
                };
                messagesArray.Add(messageObject);
            }
            body[paths?.Messages ?? "messages"] = messagesArray;

            // 4. 如果有工具(Tools)，则构建工具部分
            if (unifiedRequest.Tools != null && unifiedRequest.Tools.Count > 0)
            {
                var toolPaths = config.ToolPaths;
                var toolsArray = new JArray();
                foreach (var toolDef in unifiedRequest.Tools)
                {
                    // 【优化】从 toolPaths 获取 function 的根键名，如果模板没提供，则默认使用 "function"
                    var functionRootKey = toolPaths?.FunctionRoot ?? "function";

                    // 根据 toolPaths 动态构建工具定义JSON
                    var toolObject = new JObject
                    {
                        [toolPaths?.Type ?? "type"] = "function",
                        [functionRootKey] = new JObject // 【优化】使用变量代替硬编码
                        {
                            [toolPaths?.FunctionName ?? "name"] = toolDef.Name,
                            [toolPaths?.FunctionDescription ?? "description"] = toolDef.Description,
                            [toolPaths?.FunctionParameters ?? "parameters"] = toolDef.Parameters
                        }
                    };
                    toolsArray.Add(toolObject);
                }
                body[toolPaths?.Root ?? "tools"] = toolsArray;
            }

            // 5. 如果要求强制JSON输出，则添加相应字段
            if (unifiedRequest.ForceJsonOutput && config.JsonMode != null)
            {
                body[config.JsonMode.Path] = JToken.FromObject(config.JsonMode.Value);
            }

            // 6. 合并静态参数 (逃生舱口)
            var staticParams = config.GetMergedStaticParameters();
            if (staticParams != null)
            {
                body.Merge(JObject.FromObject(staticParams), new JsonMergeSettings
                {
                    MergeArrayHandling = MergeArrayHandling.Replace
                });
            }

            // 7. 创建 HttpRequestMessage
            var request = new HttpRequestMessage(HttpMethod.Post, config.ChatEndpoint)
            {
                Content = new StringContent(body.ToString(Formatting.None), Encoding.UTF8, "application/json")
            };

            // 8. 添加 Headers
            var headers = config.GetMergedHeaders();
            foreach (var header in headers)
            {
                request.Headers.TryAddWithoutValidation(header.Key, header.Value);
            }

            // 添加认证头 (API Key)
            if (!string.IsNullOrEmpty(config.ApiKey))
            {
                var authScheme = config.Provider?.Http?.AuthScheme ?? "Bearer";
                var authHeader = config.Provider?.Http?.AuthHeader ?? "Authorization";
                request.Headers.TryAddWithoutValidation(authHeader, $"{authScheme} {config.ApiKey}".Trim());
            }

            return request;
        }
    }
}