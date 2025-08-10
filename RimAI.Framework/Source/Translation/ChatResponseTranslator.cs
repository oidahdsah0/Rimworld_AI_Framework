using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RimAI.Framework.Shared.Logging;
using RimAI.Framework.Contracts;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using RimAI.Framework.Configuration.Models;

namespace RimAI.Framework.Translation
{
    public class ChatResponseTranslator
    {
        // --- 公共 API ---

        /// <summary>
        /// (非流式) 将完整的 HttpResponseMessage 翻译成 UnifiedChatResponse。
        /// </summary>
        public async Task<UnifiedChatResponse> TranslateAsync(HttpResponseMessage httpResponse, MergedChatConfig config, CancellationToken cancellationToken)
        {
            if (httpResponse.Content.Headers.ContentType?.MediaType == "text/event-stream")
            {
                // 如果是流式响应，则在内部进行拼接
                return await TranslateAndAggregateStreamAsync(httpResponse, config, cancellationToken);
            }
            return await TranslateStandardAsync(httpResponse, config, cancellationToken);
        }

        /// <summary>
        /// (流式) 将 HttpResponseMessage 翻译成 UnifiedChatChunk 的异步流。
        /// </summary>
        public async IAsyncEnumerable<UnifiedChatChunk> TranslateStreamAsync(HttpResponseMessage httpResponse, MergedChatConfig config, [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            await foreach (var (jObject, finishReason) in ProcessSseStream(httpResponse, config, cancellationToken))
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (jObject != null)
                {
                    var chunk = ParseJObjectToChunk(jObject, config);
                    if (chunk != null)
                    {
                        yield return chunk;
                    }
                }
                
                if (!string.IsNullOrEmpty(finishReason))
                {
                    // Emit a pure final chunk with only FinishReason to avoid duplicating last delta
                    yield return new UnifiedChatChunk { FinishReason = finishReason };
                }
            }
        }

        // --- 内部翻译逻辑 ---

        private async Task<UnifiedChatResponse> TranslateStandardAsync(HttpResponseMessage httpResponse, MergedChatConfig config, CancellationToken cancellationToken)
        {
            // 【修复 #1】: 移除 ReadAsStringAsync 的 CancellationToken 参数以兼容旧版 .NET Framework
            var jsonString = await httpResponse.Content.ReadAsStringAsync();
            cancellationToken.ThrowIfCancellationRequested(); // 在读取后检查取消状态

            if (string.IsNullOrWhiteSpace(jsonString))
                return new UnifiedChatResponse { Message = new ChatMessage { Content = "Error: Empty response from server." } };

            try
            {
                var jObject = JObject.Parse(jsonString);
                return ParseJObjectToFinalResponse(jObject, config);
            }
            catch (JsonReaderException ex)
            {
                RimAILogger.Error($"Failed to parse standard JSON response: {ex.Message}. Response body: {jsonString.Substring(0, System.Math.Min(500, jsonString.Length))}");
                return new UnifiedChatResponse { Message = new ChatMessage { Content = $"Error: Invalid JSON response from server. Details: {ex.Message}" } };
            }
        }
        
        private async Task<UnifiedChatResponse> TranslateAndAggregateStreamAsync(HttpResponseMessage httpResponse, MergedChatConfig config, CancellationToken cancellationToken)
        {
            var finalMessage = new ChatMessage { Role = "assistant", Content = "" };
            string finalFinishReason = "stream_end";

            await foreach (var chunk in TranslateStreamAsync(httpResponse, config, cancellationToken))
            {
                if (chunk.ContentDelta != null)
                    finalMessage.Content += chunk.ContentDelta;
                if (chunk.ToolCalls != null)
                    finalMessage.ToolCalls = chunk.ToolCalls;
                if (!string.IsNullOrEmpty(chunk.FinishReason))
                    finalFinishReason = chunk.FinishReason;
            }
            
            return new UnifiedChatResponse { Message = finalMessage, FinishReason = finalFinishReason };
        }

        private UnifiedChatResponse ParseJObjectToFinalResponse(JObject jObject, MergedChatConfig config)
        {
            var firstChoice = jObject.SelectToken(config.Template.ChatApi.ResponsePaths.Choices)?.FirstOrDefault();
            if (firstChoice == null)
            {
                var errorContent = jObject.SelectToken("error.message")?.ToString();
                return new UnifiedChatResponse { Message = new ChatMessage { Role = "assistant", Content = errorContent ?? jObject.ToString() }};
            }

            var messageToken = firstChoice.SelectToken("message");
            var content = messageToken?.SelectToken("content")?.ToString();
            var toolCallsToken = messageToken?.SelectToken("tool_calls");
            
            var toolCalls = toolCallsToken?.ToObject<List<ToolCall>>();

            var finishReason = firstChoice.SelectToken(config.Template.ChatApi.ResponsePaths.FinishReason)?.ToString();

            return new UnifiedChatResponse
            {
                FinishReason = finishReason,
                Message = new ChatMessage { Role = "assistant", Content = content, ToolCalls = toolCalls }
            };
        }
        
        private UnifiedChatChunk ParseJObjectToChunk(JObject jObject, MergedChatConfig config)
        {
            if (jObject == null) return null;

            var firstChoice = jObject.SelectToken(config.Template.ChatApi.ResponsePaths.Choices)?.FirstOrDefault();
            if (firstChoice == null) return null;

            var deltaToken = firstChoice.SelectToken("delta");
            var contentDelta = deltaToken?.SelectToken(config.Template.ChatApi.ResponsePaths.Content)?.ToString();
            if (string.IsNullOrEmpty(contentDelta))
            {
                // Fallback for templates that didn't set ResponsePaths.Content correctly
                contentDelta = deltaToken?["content"]?.ToString();
            }
            var toolCallsToken = deltaToken?.SelectToken("tool_calls");

            var toolCalls = toolCallsToken?.ToObject<List<ToolCall>>();

            return new UnifiedChatChunk
            {
                ContentDelta = contentDelta,
                ToolCalls = toolCalls
            };
        }

        private async IAsyncEnumerable<(JObject, string)> ProcessSseStream(HttpResponseMessage httpResponse, MergedChatConfig config, [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            using var stream = await httpResponse.Content.ReadAsStreamAsync();
            using var reader = new StreamReader(stream);
            // 禁用流读取的缓冲合并，尽量低延迟（默认 ReadLineAsync 已逐行，但明确注释意图）

            string pendingData = null;
            while (!reader.EndOfStream)
            {
                cancellationToken.ThrowIfCancellationRequested();
                string line;
                try
                {
                    line = await reader.ReadLineAsync();
                }
                catch (IOException) when (cancellationToken.IsCancellationRequested)
                {
                    yield break;
                }

                if (string.IsNullOrEmpty(line))
                {
                    // Empty line usually separates events; flush pending data
                    if (!string.IsNullOrEmpty(pendingData))
                    {
                        JObject jObject = null;
                        try { jObject = JObject.Parse(pendingData); }
                        catch (JsonException) { /* ignore and continue */ }
                        if (jObject != null)
                        {
                            var finishReason = jObject.SelectToken(config.Template.ChatApi.ResponsePaths.Choices)?.FirstOrDefault()
                                                   ?.SelectToken(config.Template.ChatApi.ResponsePaths.FinishReason)?.ToString();
                            yield return (jObject, finishReason);
                        }
                        pendingData = null;
                    }
                    continue;
                }

                if (line.StartsWith("event:"))
                {
                    // ignore named event lines for compatibility
                    continue;
                }

                if (!line.StartsWith("data: ")) continue;

                var data = line.Substring(6);
                if (data == "[DONE]")
                {
                    yield return (null, "stop");
                    break;
                }

                // aggregate potential multi-line JSON payloads
                if (pendingData == null) pendingData = data;
                else pendingData += data;
            }

            // flush tail
            if (!string.IsNullOrEmpty(pendingData))
            {
                JObject jObject = null;
                try { jObject = JObject.Parse(pendingData); } catch (JsonException) { }
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
