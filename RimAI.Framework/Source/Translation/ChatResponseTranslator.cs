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
    /// <summary>
    /// 聊天响应翻译器。
    /// 【重构】现在只有一个公共入口点，能智能处理流式和非流式响应。
    /// 【新增】完全支持通过 CancellationToken 中断操作。
    /// </summary>
    public class ChatResponseTranslator
    {
        /// <summary>
        /// 将 HTTP 响应翻译成统一聊天响应，能自动处理流式或非流式。
        /// </summary>
        /// <param name="httpResponse">从 HttpExecutor 收到的原始 HTTP 响应。</param>
        /// <param name="config">包含了所有翻译规则的合并后配置。</param>
        /// <param name="cancellationToken">用于中断操作的令牌。</param>
        public async Task<UnifiedChatResponse> TranslateAsync(HttpResponseMessage httpResponse, MergedConfig config, CancellationToken cancellationToken)
        {
            // 通过检查响应头来判断是否为流式响应。
            // "text/event-stream" 是 SSE (Server-Sent Events) 的标准MIME类型。
            bool isStreaming = httpResponse.Content.Headers.ContentType?.MediaType == "text/event-stream";

            if (!isStreaming)
            {
                // --- 非流式路径 ---
                var contentStr = await httpResponse.Content.ReadAsStringAsync();
                return ParseCompleteResponse(contentStr, config);
            }
            else
            {
                // --- 流式路径 ---
                var finalResponse = new UnifiedChatResponse
                {
                    Message = new ChatMessage { Role = "assistant", Content = "", ToolCalls = new List<ToolCall>() }
                };
                var contentBuilder = new StringBuilder();

                await foreach (var chunk in ProcessStreamAsync(httpResponse, config, cancellationToken))
                {
                    cancellationToken.ThrowIfCancellationRequested();

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
        
        /// <summary>
        /// 私有辅助方法，用于解析一个完整的、非流式的JSON字符串。
        /// </summary>
        private UnifiedChatResponse ParseCompleteResponse(string jsonContent, MergedConfig config)
        {
            if (string.IsNullOrWhiteSpace(jsonContent))
            {
                return new UnifiedChatResponse { FinishReason = "error", Message = new ChatMessage { Content = "Empty response body." } };
            }
            
            var contentJson = JObject.Parse(jsonContent);
            var paths = config.ChatResponsePaths;

            // 【修正】使用正确的 C# null 条件运算符 ?.
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

        /// <summary>
        /// 私有方法，核心的异步流处理器。
        /// </summary>
        private async IAsyncEnumerable<UnifiedChatResponse> ProcessStreamAsync(HttpResponseMessage httpResponse, MergedConfig config, [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            using var stream = await httpResponse.Content.ReadAsStreamAsync();
            using var reader = new StreamReader(stream);

            string line;
            while ((line = await reader.ReadLineAsync()) != null)
            {
                if (string.IsNullOrWhiteSpace(line)) continue;

                var jsonData = line.StartsWith("data: ") ? line.Substring("data: ".Length) : line;
                if (jsonData.Trim() == "[DONE]") yield break;

                try
                {
                    var partialResponse = ParseCompleteResponse(jsonData, config);
                    if (partialResponse != null)
                    {
                        yield return partialResponse;
                    }
                }
                catch (JsonException) { /* 忽略无法解析的行 */ }
            }
        }
    }
}