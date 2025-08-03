// 引入必要的命名空间
using System;
using System.Net.Http;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RimAI.Framework.Configuration.Models;
using RimAI.Framework.Translation.Models;
using System.Net.Http.Headers; // [新增] 为 AuthenticationHeaderValue

namespace RimAI.Framework.Translation
{
    public class ChatRequestTranslator
    {
        public HttpRequestMessage Translate(UnifiedChatRequest unifiedRequest, MergedConfig config)
        {
            // 1. 创建 JSON body 对象，并首先合并静态参数
            var body = new JObject();
            if (config.StaticParameters != null)
            {
                body.Merge(config.StaticParameters);
            }

            // 2. 根据 requestPaths 映射标准参数
            var paths = config.ChatApi.RequestPaths;
            body[paths.Model] = config.ChatModel;
            body[paths.Stream] = unifiedRequest.Stream;

            if (config.Temperature.HasValue)
                body[paths.Temperature] = config.Temperature.Value;
            if (config.TopP.HasValue)
                body[paths.TopP] = config.TopP;

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
            body[paths.Messages] = messagesArray;

            // 4. 构建 tools 数组
            if (unifiedRequest.Tools != null && unifiedRequest.Tools.Count > 0)
            {
                var toolPaths = config.ChatApi.ToolPaths;
                var toolsArray = new JArray();
                foreach (var toolDef in unifiedRequest.Tools)
                {
                    // 【修正】根据新的 ToolDefinition 结构来构建
                    var toolObject = new JObject
                    {
                        [toolPaths.Type] = toolDef.Type,
                        [toolPaths.FunctionRoot] = toolDef.Function
                    };
                    toolsArray.Add(toolObject);
                }
                body[toolPaths.Root] = toolsArray;
            }

            // 5. 如果要求强制JSON输出，则添加相应字段
            var jsonMode = config.ChatApi.JsonMode;
            if (unifiedRequest.ForceJsonOutput && jsonMode != null)
            {
                body[jsonMode.Path] = jsonMode.Value;
            }

            // 6. 创建 HttpRequestMessage
            var request = new HttpRequestMessage(HttpMethod.Post, config.ChatEndpoint)
            {
                Content = new StringContent(body.ToString(Formatting.None), Encoding.UTF8, "application/json")
            };

            // 7. 添加 Headers
            if (config.Http?.Headers != null)
            {
                foreach (var header in config.Http.Headers)
                {
                    request.Headers.TryAddWithoutValidation(header.Key, header.Value);
                }
            }
            if (config.CustomHeaders != null)
            {
                foreach (var header in config.CustomHeaders)
                {
                    request.Headers.Remove(header.Key);
                    request.Headers.TryAddWithoutValidation(header.Key, header.Value);
                }
            }

            // 8. 添加认证头
            if (!string.IsNullOrEmpty(config.ApiKey))
            {
                request.Headers.Authorization = new AuthenticationHeaderValue(config.Http.AuthScheme, config.ApiKey);
            }

            return request;
        }
    }
}