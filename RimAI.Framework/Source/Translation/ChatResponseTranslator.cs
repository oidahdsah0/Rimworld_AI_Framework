// 引入必要的命名空间
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RimAI.Framework.Configuration.Models;
using RimAI.Framework.Translation.Models;

namespace RimAI.Framework.Translation
{
    public class ChatResponseTranslator
    {
        public async Task<UnifiedChatResponse> TranslateAsync(HttpResponseMessage httpResponse, MergedConfig config, CancellationToken cancellationToken)
        {
            // 通过检查响应头来判断是否为流式响应。
            bool isStreaming = httpResponse.Content.Headers.ContentType?.MediaType == "text/event-stream";

            if (!isStreaming)
            {
                var contentStr = await httpResponse.Content.ReadAsStringAsync();
                return ParseCompleteResponse(contentStr, config);
            }
            else
            {
                var finalResponse = new UnifiedChatResponse
                {
                    Message = new ChatMessage { Role = "assistant", Content = "", ToolCalls = new List<ToolCall>() }
                };
                var contentBuilder = new StringBuilder();

                // 使用 .WithCancellation(cancellationToken) 来确保取消信号能被异步流正确处理
                await foreach (var chunk in ProcessStreamAsync(httpResponse, config, cancellationToken).WithCancellation(cancellationToken))
                {
                    if (!string.IsNullOrEmpty(chunk.Message?.Content))
                    {
                        contentBuilder.Append(chunk.Message.Content);
                    }
                    if (chunk.Message?.ToolCalls != null && chunk.Message.ToolCalls.Any())
                    {
                        finalResponse.Message.ToolCalls.AddRange(chunk.Message.ToolCalls);
                    }
                    if (!string.IsNullOrEmpty(chunk.FinishReason))
                    {
                        finalResponse.FinishReason = chunk.FinishReason;
                    }
                }

                finalResponse.Message.Content = contentBuilder.ToString();
                if (finalResponse.Message.ToolCalls.Count == 0)
                {
                    finalResponse.Message.ToolCalls = null;
                }
                
                return finalResponse;
            }
        }
        
        private UnifiedChatResponse ParseCompleteResponse(string jsonContent, MergedConfig config)
        {
            if (string.IsNullOrWhiteSpace(jsonContent))
            {
                return new UnifiedChatResponse { FinishReason = "error", Message = new ChatMessage { Content = "Empty response body." } };
            }
            
            JObject contentJson;
            try
            {
                contentJson = JObject.Parse(jsonContent);
            }
            catch (JsonReaderException)
            {
                return new UnifiedChatResponse { FinishReason = "error", Message = new ChatMessage { Content = $"Invalid JSON received: {jsonContent}" } };
            }
            
            var paths = config.ChatApi.ResponsePaths;

            // 【最终修正】使用正确的 C# null 条件运算符 ?. 和索引器 [0]
            var choice = contentJson.SelectToken(paths.Choices)?[0];
            
            if (choice == null) return new UnifiedChatResponse { FinishReason = "error", Message = new ChatMessage { Content = "Invalid response structure: 'choices' array not found or is empty." }};

            var finishReason = choice.SelectToken(paths.FinishReason)?.ToString();
            var messageContent = choice.SelectToken(paths.Content)?.ToString();
            var toolCallsToken = choice.SelectToken(paths.ToolCalls);
            
            var toolCalls = new List<ToolCall>();
            if (toolCallsToken is JArray toolCallsArray)
            {
                foreach (var token in toolCallsArray)
                {
                    toolCalls.Add(new ToolCall
                    {
                        Id = token["id"]?.ToString(),
                        Type = token["type"]?.ToString(),
                        FunctionName = token["function"]?["name"]?.ToString(),
                        Arguments = token["function"]?["arguments"]?.ToString()
                    });
                }
            }

            return new UnifiedChatResponse
            {
                FinishReason = finishReason,
                Message = new ChatMessage
                {
                    Role = "assistant",
                    Content = messageContent,
                    ToolCalls = toolCalls.Any() ? toolCalls : null
                }
            };
        }

        private async IAsyncEnumerable<UnifiedChatResponse> ProcessStreamAsync(HttpResponseMessage httpResponse, MergedConfig config, [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            using var stream = await httpResponse.Content.ReadAsStreamAsync();
            using var reader = new StreamReader(stream);

            string line;
            while ((line = await reader.ReadLineAsync()) != null)
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (string.IsNullOrWhiteSpace(line)) continue;

                var jsonData = line.StartsWith("data: ") ? line.Substring("data: ".Length) : line;
                if (jsonData.Trim() == "[DONE]") yield break;
                
                UnifiedChatResponse partialResponse = null;
                try
                {
                    partialResponse = ParseCompleteResponse(jsonData, config);
                }
                catch (JsonException) { /* 忽略无法解析的行 */ }

                if (partialResponse != null)
                {
                    yield return partialResponse;
                }
            }
        }
    }
}