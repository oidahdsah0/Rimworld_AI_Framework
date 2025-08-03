using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RimAI.Framework.Shared.Logging;
using RimAI.Framework.Translation.Models;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using RimAI.Framework.Configuration.Models;

namespace RimAI.Framework.Translation
{
    public class ChatResponseTranslator
    {
        public async Task<UnifiedChatResponse> TranslateAsync(HttpResponseMessage httpResponse, MergedChatConfig config, CancellationToken cancellationToken)
        {
            if (httpResponse.Content.Headers.ContentType?.MediaType == "text/event-stream")
            {
                return await TranslateStreamAsync(httpResponse, config, cancellationToken);
            }
            return await TranslateStandardAsync(httpResponse, config, cancellationToken);
        }

        private async Task<UnifiedChatResponse> TranslateStandardAsync(HttpResponseMessage httpResponse, MergedChatConfig config, CancellationToken cancellationToken)
        {
            var jsonString = await httpResponse.Content.ReadAsStringAsync();
            cancellationToken.ThrowIfCancellationRequested();

            if (string.IsNullOrWhiteSpace(jsonString))
                return new UnifiedChatResponse { Message = new ChatMessage { Content = "Error: Empty response from server." } };

            try
            {
                var jObject = JObject.Parse(jsonString);
                return ParseSingleJObject(jObject, config);
            }
            catch (JsonReaderException ex)
            {
                RimAILogger.Error($"Failed to parse standard JSON response: {ex.Message}. Response body: {jsonString.Substring(0, 500)}");
                return new UnifiedChatResponse { Message = new ChatMessage { Content = $"Error: Invalid JSON response from server. Details: {ex.Message}" } };
            }
        }

        private async Task<UnifiedChatResponse> TranslateStreamAsync(HttpResponseMessage httpResponse, MergedChatConfig config, CancellationToken cancellationToken)
        {
            var finalMessage = new ChatMessage { Role = "assistant", Content = "" };
            string finalFinishReason = "stream_end";

            await foreach (var (jObject, finishReason) in ProcessSseStream(httpResponse, config, cancellationToken))
            {
                if (jObject != null)
                {
                    var partialResponse = ParseSingleJObject(jObject, config);
                    if (partialResponse?.Message?.Content != null)
                        finalMessage.Content += partialResponse.Message.Content;
                    if (partialResponse?.Message?.ToolCalls != null)
                        finalMessage.ToolCalls = partialResponse.Message.ToolCalls;
                }
                if (!string.IsNullOrEmpty(finishReason))
                    finalFinishReason = finishReason;
            }
            
            return new UnifiedChatResponse { Message = finalMessage, FinishReason = finalFinishReason };
        }

        private UnifiedChatResponse ParseSingleJObject(JObject jObject, MergedChatConfig config)
        {
            // 【修复】通过 config.Template.ChatApi 访问路径
            var choiceToken = jObject.SelectToken(config.Template.ChatApi.ResponsePaths.Choices);
            var firstChoice = choiceToken?.FirstOrDefault();
            if (firstChoice == null)
            {
                // Handle cases where response is not in a 'choices' array (e.g. error messages)
                var errorContent = jObject.SelectToken("error.message")?.ToString();
                if (errorContent != null)
                    return new UnifiedChatResponse { Message = new ChatMessage { Content = errorContent } };
                return null;
            }

            var content = firstChoice.SelectToken(config.Template.ChatApi.ResponsePaths.Content)?.ToString();
            var finishReason = firstChoice.SelectToken(config.Template.ChatApi.ResponsePaths.FinishReason)?.ToString();
            
            return new UnifiedChatResponse
            {
                FinishReason = finishReason,
                Message = new ChatMessage { Role = "assistant", Content = content }
            };
        }

        private async IAsyncEnumerable<(JObject, string)> ProcessSseStream(HttpResponseMessage httpResponse, MergedChatConfig config, [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            using var stream = await httpResponse.Content.ReadAsStreamAsync();
            using var reader = new StreamReader(stream);
            
            while (!reader.EndOfStream)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var line = await reader.ReadLineAsync();

                if (line != null && line.StartsWith("data: "))
                {
                    var data = line.Substring(6);
                    if (data == "[DONE]")
                    {
                        yield return (null, "stop");
                        break;
                    }

                    // 【修复】将 try-catch 和 jObject 的声明都放在循环内部，确保作用域正确
                    JObject jObject = null;
                    try
                    {
                        jObject = JObject.Parse(data);
                    }
                    catch (JsonException) 
                    {
                        // 忽略非JSON数据行，例如心跳包或注释
                        continue; 
                    }

                    if (jObject != null)
                    {
                        var finishReason = jObject.SelectToken(config.Template.ChatApi.ResponsePaths.Choices)?.FirstOrDefault()
                                           ?.SelectToken(config.Template.ChatApi.ResponsePaths.FinishReason)?.ToString();
                        yield return (jObject, finishReason);
                    }
                }
            }
        }
    }
}