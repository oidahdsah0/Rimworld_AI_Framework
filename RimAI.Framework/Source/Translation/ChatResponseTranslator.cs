// 引入必要的命名空间
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Runtime.CompilerServices; // 用于 IAsyncEnumerable
using System.Text.Json; // 我们将使用 System.Text.Json 来高效处理流式JSON
using System.Threading.Tasks;
using Newtonsoft.Json.Linq; // 用于非流式解析
using RimAI.Framework.Configuration.Models;
using RimAI.Framework.Translation.Models;

namespace RimAI.Framework.Translation
{
    /// <summary>
    /// 聊天响应翻译器。
    /// 负责将来自提供商的原始 HttpResponseMessage 翻译回我们统一的内部模型 (UnifiedChatResponse)。
    /// 支持流式 (streaming) 和非流式 (non-streaming) 两种模式。
    /// </summary>
    public class ChatResponseTranslator
    {
        /// <summary>
        /// 【非流式】将一个完整的HTTP响应翻译成统一聊天响应。
        /// </summary>
        public async Task<UnifiedChatResponse> TranslateAsync(HttpResponseMessage httpResponse, MergedConfig config)
        {
            var contentStr = await httpResponse.Content.ReadAsStringAsync();
            var contentJson = JObject.Parse(contentStr);
            var paths = config.ChatResponsePaths;

            // 根据 responsePaths 从JSON中提取数据
            // .SelectToken() 支持点符号嵌套路径，非常强大
            var choice = contentJson.SelectToken(paths?.Choices ?? "choices")?[0];
            if (choice == null)
                return null; // or throw exception

            var finishReason = choice.SelectToken(paths?.FinishReason ?? "finish_reason")?.ToString();
            var messageContent = choice.SelectToken(paths?.Content ?? "message.content")?.ToString();
            var toolCallsToken = choice.SelectToken(paths?.ToolCalls ?? "message.tool_calls");
            
            var toolCalls = new List<ToolCall>();
            if (toolCallsToken is JArray toolCallsArray)
            {
                foreach (var token in toolCallsArray)
                {
                    toolCalls.Add(new ToolCall
                    {
                        Id = token["id"]?.ToString(),
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
                    ToolCalls = toolCalls.Count > 0 ? toolCalls : null
                }
            };
        }

        /// <summary>
        /// 【流式】将一个流式HTTP响应翻译成一个异步的统一聊天响应序列。
        /// </summary>
        public async IAsyncEnumerable<UnifiedChatResponse> TranslateStreamAsync(HttpResponseMessage httpResponse, MergedConfig config)
        {
            using var stream = await httpResponse.Content.ReadAsStreamAsync();
            using var reader = new StreamReader(stream);

            string line;
            while ((line = await reader.ReadLineAsync()) != null)
            {
                if (string.IsNullOrWhiteSpace(line))
                    continue;

                // 流式响应通常以 "data: " 开头
                if (!line.StartsWith("data: "))
                    continue;

                var jsonData = line.Substring("data: ".Length);

                // [DONE] 是许多流式API表示结束的标记
                if (jsonData.Trim() == "[DONE]")
                    yield break; // 结束迭代

                try
                {
                    var contentJson = JObject.Parse(jsonData);
                    var paths = config.ChatResponsePaths;

                    var choice = contentJson.SelectToken(paths?.Choices ?? "choices")?[0];
                    if (choice == null)
                        continue;
                    
                    // 在流式响应中，我们主要关心增量的文本内容
                    var delta = choice["delta"];
                    var contentChunk = delta?.SelectToken(paths?.Content ?? "content")?.ToString();
                    
                    if (!string.IsNullOrEmpty(contentChunk))
                    {
                        // 每解析出一个文本块，就 yield return 一个响应对象
                        yield return new UnifiedChatResponse
                        {
                            Message = new ChatMessage { Role = "assistant", Content = contentChunk }
                        };
                    }

                    // TODO: 在流的末尾，也需要解析 finish_reason 和 tool_calls
                }
                catch (JsonReaderException)
                {
                    // 忽略无法解析的行
                }
            }
        }
    }
}