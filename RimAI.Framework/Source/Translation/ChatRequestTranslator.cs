using System.Linq;
using System.Net.Http;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RimAI.Framework.Configuration.Models;
using RimAI.Framework.Shared.Logging;
using RimAI.Framework.Contracts;

namespace RimAI.Framework.Translation
{
    public class ChatRequestTranslator
    {
        public HttpRequestMessage Translate(UnifiedChatRequest unifiedRequest, MergedChatConfig config)
        {
            var requestBody = new JObject();

            // 【修复】现在可以正确地从 Template 访问 StaticParameters
            if (config.Template.StaticParameters != null)
                requestBody.Merge(config.Template.StaticParameters);
            if (config.User.StaticParametersOverride != null)
                requestBody.Merge(config.User.StaticParametersOverride);

            // 【修复】使用 MergedChatConfig 的便捷属性
            // 2. Dynamic Parameters (Model, Temperature, etc.)
            requestBody[config.Template.ChatApi.RequestPaths.Model] = config.Model;
            
            var temperature = config.User.Temperature ?? config.Template.ChatApi.DefaultParameters?["temperature"]?.Value<float>();
            if (temperature.HasValue)
                requestBody[config.Template.ChatApi.RequestPaths.Temperature] = temperature.Value;

            var topP = config.User.TopP ?? config.Template.ChatApi.DefaultParameters?["top_p"]?.Value<float>();
            if (topP.HasValue && config.Template.ChatApi.RequestPaths.TopP != null)
                requestBody[config.Template.ChatApi.RequestPaths.TopP] = topP.Value;

            var typicalP = config.User.TypicalP ?? config.Template.ChatApi.DefaultParameters?["typical_p"]?.Value<float>();
            if (typicalP.HasValue && config.Template.ChatApi.RequestPaths.TypicalP != null)
                requestBody[config.Template.ChatApi.RequestPaths.TypicalP] = typicalP.Value;

            int? maxTokens = config.User.MaxTokens 
                ?? config.Template.ChatApi.DefaultParameters?["max_tokens"]?.Value<int>()
                ?? (int?)300;
            if (maxTokens != null && config.Template.ChatApi.RequestPaths.MaxTokens != null)
                requestBody[config.Template.ChatApi.RequestPaths.MaxTokens] = maxTokens.Value;

            // 3. Messages
            var messagesArray = new JArray();
            foreach (var msg in unifiedRequest.Messages)
            {
                var jMsg = new JObject { ["role"] = msg.Role, ["content"] = msg.Content };
                if (msg.ToolCalls != null && msg.ToolCalls.Any())
                {
                    jMsg["tool_calls"] = JArray.FromObject(msg.ToolCalls);
                }
                // 当角色为 tool 时，需要携带 tool_call_id 字段
                if (!string.IsNullOrEmpty(msg.ToolCallId))
                {
                    jMsg["tool_call_id"] = msg.ToolCallId;
                }
                messagesArray.Add(jMsg);
            }
            requestBody[config.Template.ChatApi.RequestPaths.Messages] = messagesArray;

            // 4. Stream
            if (unifiedRequest.Stream)
            {
                requestBody[config.Template.ChatApi.RequestPaths.Stream] = true;
            }

            // 5. Tools (Function Calling)
            if (unifiedRequest.Tools != null && unifiedRequest.Tools.Any() && config.Template.ChatApi.RequestPaths.Tools != null)
            {
                requestBody[config.Template.ChatApi.RequestPaths.Tools] = JArray.FromObject(unifiedRequest.Tools);
                if (config.Template.ChatApi.RequestPaths.ToolChoice != null)
                    requestBody[config.Template.ChatApi.RequestPaths.ToolChoice] = "auto";
            }

            // 6. JSON Mode
            if (unifiedRequest.ForceJsonOutput && config.Template.ChatApi.JsonMode != null)
            {
                requestBody[config.Template.ChatApi.JsonMode.Path] = config.Template.ChatApi.JsonMode.Value;
            }
            
            // 【修复】使用 MergedChatConfig 的便捷属性
            var finalEndpoint = config.Endpoint.Replace("{apiKey}", config.ApiKey);
            var request = new HttpRequestMessage(HttpMethod.Post, finalEndpoint)
            {
                Content = new StringContent(requestBody.ToString(Formatting.None), Encoding.UTF8, "application/json")
            };

            // 【修复】从 Template 和 User 对象中分别获取 Headers
            // 6. Headers
            if (config.Template.Http?.Headers != null)
            {
                foreach (var header in config.Template.Http.Headers)
                    request.Headers.TryAddWithoutValidation(header.Key, header.Value);
            }
            if (config.User.CustomHeaders != null)
            {
                foreach (var header in config.User.CustomHeaders)
                {
                    // 用户自定义同名 Header 覆盖模板 Header
                    if (request.Headers.Contains(header.Key)) request.Headers.Remove(header.Key);
                    request.Headers.TryAddWithoutValidation(header.Key, header.Value);
                }
            }

            // 7. Authentication
            if (!string.IsNullOrEmpty(config.Template.Http?.AuthHeader) && !string.IsNullOrEmpty(config.ApiKey))
            {
                string authValue = $"{config.Template.Http.AuthScheme} {config.ApiKey}".Trim();
                request.Headers.TryAddWithoutValidation(config.Template.Http.AuthHeader, authValue);
            }

            return request;
        }
    }
}