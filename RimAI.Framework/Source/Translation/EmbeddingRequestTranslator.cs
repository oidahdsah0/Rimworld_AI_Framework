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
    public class EmbeddingRequestTranslator
    {
        public HttpRequestMessage Translate(UnifiedEmbeddingRequest unifiedRequest, MergedEmbeddingConfig config)
        {
            var requestBody = new JObject();
            
            // 【修复】从 Template 对象中获取路径
            var modelPath = config.Template.EmbeddingApi.RequestPaths.Model;
            var inputPath = config.Template.EmbeddingApi.RequestPaths.Input;

            // 【修复】使用 MergedEmbeddingConfig 的便捷属性
            requestBody[modelPath] = config.Model;

            // Handle potentially complex input paths
            if (inputPath.Contains("[]"))
            {
                // This is a simplified handler for paths like "requests[].content.parts[].text"
                // A full implementation would require a more robust path resolver.
                var parts = inputPath.Split(new[] { "[]" }, System.StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length == 2)
                {
                    var outerArray = new JArray();
                    foreach (var input in unifiedRequest.Inputs)
                    {
                        var innerObject = new JObject();
                        var currentLevel = innerObject;
                        var pathSegments = parts[1].TrimStart('.').Split('.');
                        for(int i = 0; i < pathSegments.Length - 1; i++)
                        {
                            var newLevel = new JObject();
                            currentLevel[pathSegments[i]] = newLevel;
                            currentLevel = newLevel;
                        }
                        currentLevel[pathSegments.Last()] = input;
                        
                        var outerWrapper = new JObject();
                        outerWrapper[parts[0]] = outerArray;
                        outerArray.Add(innerObject);
                    }
                    requestBody.Merge(JToken.FromObject(new { requests = outerArray })); // Simplified assumption
                }
            }
            else
            {
                 requestBody[inputPath] = JArray.FromObject(unifiedRequest.Inputs);
            }

            // 【修复】使用 MergedEmbeddingConfig 的便捷属性
            // Build endpoint safely even if ApiKey is null or provider doesn't use placeholder
            var finalEndpoint = config.Endpoint ?? string.Empty;
            if (finalEndpoint.Contains("{apiKey}"))
            {
                var replacement = config.ApiKey ?? string.Empty;
                finalEndpoint = finalEndpoint.Replace("{apiKey}", replacement);
            }
            var request = new HttpRequestMessage(HttpMethod.Post, finalEndpoint)
            {
                Content = new StringContent(requestBody.ToString(Formatting.None), Encoding.UTF8, "application/json")
            };

            // 【修复】从 Template 和 User 对象中分别获取 Headers
            if (config.Template.Http?.Headers != null)
            {
                foreach (var header in config.Template.Http.Headers)
                    request.Headers.TryAddWithoutValidation(header.Key, header.Value);
            }
            if (config.User.CustomHeaders != null)
            {
                foreach (var header in config.User.CustomHeaders)
                    request.Headers.TryAddWithoutValidation(header.Key, header.Value);
            }

            // 【修复】从 Template 对象中获取认证信息
            if (!string.IsNullOrEmpty(config.Template.Http?.AuthHeader) && !string.IsNullOrEmpty(config.ApiKey))
            {
                string authValue = $"{config.Template.Http.AuthScheme} {config.ApiKey}".Trim();
                request.Headers.TryAddWithoutValidation(config.Template.Http.AuthHeader, authValue);
            }

            return request;
        }
    }
}