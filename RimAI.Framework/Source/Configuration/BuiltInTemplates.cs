using Newtonsoft.Json.Linq;
using RimAI.Framework.Configuration.Models;
using System.Collections.Generic;

namespace RimAI.Framework.Configuration
{
    /// <summary>
    /// 包含所有内置的、硬编码的 AI 服务模板。
    /// V4.2 重构: 已将统一的 ProviderTemplate 拆分为独立的 ChatTemplate 和 EmbeddingTemplate。
    /// </summary>
    public static class BuiltInTemplates
    {
        /// <summary>
        /// 获取所有内置的聊天服务模板。
        /// </summary>
        public static IEnumerable<ChatTemplate> GetChatTemplates()
        {
            // --- OpenAI Chat Template ---
            yield return new ChatTemplate
            {
                ProviderName = "OpenAI",
                ProviderUrl = "https://platform.openai.com/",
                Http = new HttpConfig { /* ... */ AuthHeader = "Authorization", AuthScheme = "Bearer", Headers = new Dictionary<string, string> { { "Content-Type", "application/json" } } },
                ChatApi = new ChatApiConfig
                {
                    Endpoint = "https://api.openai.com/v1/chat/completions",
                    DefaultModel = "gpt-4o",
                    DefaultParameters = JObject.FromObject(new { temperature = 0.7, top_p = 1.0, typical_p = 1.0, max_tokens = 1024 }),
                    RequestPaths = new ChatRequestPaths {
                        Model = "model",
                        Messages = "messages",
                        Temperature = "temperature",
                        TopP = "top_p",
                        TypicalP = "typical_p",
                        MaxTokens = "max_tokens",
                        Stream = "stream",
                        Tools = "tools",
                        ToolChoice = "tool_choice"
                    },
                    ResponsePaths = new ChatResponsePaths { Choices = "choices", Content = "message.content", ToolCalls = "message.tool_calls", FinishReason = "finish_reason" },
                    ToolPaths = new ToolPaths { Root = "tools", Type = "type", FunctionRoot = "function", FunctionName = "name", FunctionDescription = "description", FunctionParameters = "parameters" },
                    JsonMode = new JsonModeConfig { Path = "response_format", Value = JObject.FromObject(new { type = "json_object" }) }
                }
            };

            // --- Claude Chat Template ---
            yield return new ChatTemplate
            {
                ProviderName = "Claude",
                ProviderUrl = "https://www.anthropic.com/",
                Http = new HttpConfig { AuthHeader = "x-api-key", AuthScheme = null, Headers = new Dictionary<string, string> { { "Content-Type", "application/json" }, { "anthropic-version", "2023-06-01" } } },
                ChatApi = new ChatApiConfig
                {
                    Endpoint = "https://api.anthropic.com/v1/messages",
                    DefaultModel = "claude-3-opus-20240229",
                    DefaultParameters = JObject.FromObject(new { temperature = 0.7, top_p = 1.0, typical_p = 1.0, max_tokens = 4096 }),
                    RequestPaths = new ChatRequestPaths {
                        Model = "model",
                        Messages = "messages",
                        Temperature = "temperature",
                        TopP = "top_p", // Claude 可能支持
                        TypicalP = "typical_p",
                        MaxTokens = "max_tokens",
                        Stream = "stream",
                        Tools = "tools"
                    },
                    ResponsePaths = new ChatResponsePaths { Content = "content[0].text", FinishReason = "stop_reason", ToolCalls = "content" },
                    ToolPaths = new ToolPaths { Root = "tools", Type = "type", FunctionName = "name", FunctionDescription = "description", FunctionParameters = "input_schema" },
                    JsonMode = null
                }
            };
        }

        /// <summary>
        /// 获取所有内置的 Embedding 服务模板。
        /// </summary>
        public static IEnumerable<EmbeddingTemplate> GetEmbeddingTemplates()
        {
            // --- OpenAI Embedding Template ---
            yield return new EmbeddingTemplate
            {
                ProviderName = "OpenAI",
                ProviderUrl = "https://platform.openai.com/",
                Http = new HttpConfig { /* ... */ AuthHeader = "Authorization", AuthScheme = "Bearer", Headers = new Dictionary<string, string> { { "Content-Type", "application/json" } } },
                EmbeddingApi = new EmbeddingApiConfig
                {
                    Endpoint = "https://api.openai.com/v1/embeddings",
                    DefaultModel = "text-embedding-3-small",
                    MaxBatchSize = 2048,
                    RequestPaths = new EmbeddingRequestPaths { Model = "model", Input = "input" },
                    ResponsePaths = new EmbeddingResponsePaths { DataList = "data", Embedding = "embedding", Index = "index" }
                }
            };

            // 未来可以在这里添加其他厂商的 Embedding 模板，例如 Cohere
            // yield return new EmbeddingTemplate { ProviderName = "Cohere", ... };
        }
    }
}