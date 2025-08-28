using Newtonsoft.Json.Linq;
using RimAI.Framework.Configuration.Models;
using System.Collections.Generic;

namespace RimAI.Framework.Configuration
{
    /// <summary>
    /// 包含所有内置的、硬编码的 AI 服务模板。
    /// v4.2.1: 模板保持兼容，补充Embedding/Chat键策略说明。
    /// </summary>
    public static class BuiltInTemplates
    {
        private static readonly HttpConfig OpenAiHttpConfig = new HttpConfig { AuthHeader = "Authorization", AuthScheme = "Bearer", Headers = new Dictionary<string, string> { { "Content-Type", "application/json" } } };
        private static readonly ChatRequestPaths OpenAiChatRequestPaths = new ChatRequestPaths {
            Model = "model",
            Messages = "messages",
            Temperature = "temperature",
            TopP = "top_p",
            TypicalP = "typical_p",
            MaxTokens = "max_tokens",
            Stream = "stream",
            Tools = "tools",
            ToolChoice = "tool_choice"
        };
        private static readonly ChatResponsePaths OpenAiChatResponsePaths = new ChatResponsePaths { Choices = "choices", Content = "content", ToolCalls = "tool_calls", FinishReason = "finish_reason" };
        private static readonly ToolPaths OpenAiToolPaths = new ToolPaths { Root = "tools", Type = "type", FunctionRoot = "function", FunctionName = "name", FunctionDescription = "description", FunctionParameters = "parameters" };
        private static readonly JsonModeConfig OpenAiJsonMode = new JsonModeConfig { Path = "response_format", Value = JObject.FromObject(new { type = "json_object" }) };

        
        private static readonly EmbeddingRequestPaths OpenAiEmbeddingRequestPaths = new EmbeddingRequestPaths { Model = "model", Input = "input" };
        private static readonly EmbeddingResponsePaths OpenAiEmbeddingResponsePaths = new EmbeddingResponsePaths { DataList = "data", Embedding = "embedding", Index = "index" };


        /// <summary>
        /// 获取所有内置的聊天服务模板。
        /// </summary>
        public static IEnumerable<ChatTemplate> GetChatTemplates()
        {
            // --- Local & Compatible Providers ---
            yield return new ChatTemplate
            {
                ProviderName = "OpenAI-compatible",
                ProviderUrl = "https://example.com",
                Http = OpenAiHttpConfig,
                ChatApi = new ChatApiConfig
                {
                    Endpoint = "http://localhost:8080/v1/chat/completions",
                    DefaultModel = "default-model",
                    DefaultParameters = JObject.FromObject(new { temperature = 0.7, top_p = 1.0, typical_p = 1.0, max_tokens = 300 }),
                    RequestPaths = OpenAiChatRequestPaths,
                    ResponsePaths = OpenAiChatResponsePaths,
                    ToolPaths = OpenAiToolPaths,
                    JsonMode = OpenAiJsonMode
                }
            };
            yield return new ChatTemplate
            {
                ProviderName = "Ollama",
                ProviderUrl = "https://ollama.com/",
                Http = OpenAiHttpConfig,
                ChatApi = new ChatApiConfig
                {
                    Endpoint = "http://localhost:11434/v1/chat/completions",
                    DefaultModel = "llama3",
                    DefaultParameters = JObject.FromObject(new { temperature = 0.7, top_p = 1.0, typical_p = 1.0, max_tokens = 300 }),
                    RequestPaths = OpenAiChatRequestPaths,
                    ResponsePaths = OpenAiChatResponsePaths,
                    ToolPaths = OpenAiToolPaths,
                    JsonMode = OpenAiJsonMode
                }
            };
            yield return new ChatTemplate
            {
                ProviderName = "LM Studio",
                ProviderUrl = "https://lmstudio.ai/",
                Http = OpenAiHttpConfig,
                ChatApi = new ChatApiConfig
                {
                    Endpoint = "http://localhost:1234/v1/chat/completions",
                    DefaultModel = "loaded-model",
                    DefaultParameters = JObject.FromObject(new { temperature = 0.7, top_p = 1.0, typical_p = 1.0, max_tokens = 300 }),
                    RequestPaths = OpenAiChatRequestPaths,
                    ResponsePaths = OpenAiChatResponsePaths,
                    ToolPaths = OpenAiToolPaths,
                    JsonMode = OpenAiJsonMode
                }
            };
            yield return new ChatTemplate
            {
                ProviderName = "vLLM",
                ProviderUrl = "https://github.com/vllm-project/vllm",
                Http = OpenAiHttpConfig,
                ChatApi = new ChatApiConfig
                {
                    Endpoint = "http://localhost:8000/v1/chat/completions",
                    DefaultModel = "loaded-model",
                    DefaultParameters = JObject.FromObject(new { temperature = 0.7, top_p = 1.0, typical_p = 1.0, max_tokens = 300 }),
                    RequestPaths = OpenAiChatRequestPaths,
                    ResponsePaths = OpenAiChatResponsePaths,
                    ToolPaths = OpenAiToolPaths,
                    JsonMode = OpenAiJsonMode
                }
            };
            yield return new ChatTemplate
            {
                ProviderName = "SGLang",
                ProviderUrl = "https://github.com/sgl-project/sglang",
                Http = OpenAiHttpConfig,
                ChatApi = new ChatApiConfig
                {
                    Endpoint = "http://localhost:30000/v1/chat/completions",
                    DefaultModel = "loaded-model",
                    DefaultParameters = JObject.FromObject(new { temperature = 0.7, top_p = 1.0, typical_p = 1.0, max_tokens = 300 }),
                    RequestPaths = OpenAiChatRequestPaths,
                    ResponsePaths = OpenAiChatResponsePaths,
                    ToolPaths = OpenAiToolPaths,
                    JsonMode = OpenAiJsonMode
                }
            };
            
            // --- Cloud Providers ---
            yield return new ChatTemplate
            {
                ProviderName = "Groq",
                ProviderUrl = "https://groq.com/",
                Http = OpenAiHttpConfig,
                ChatApi = new ChatApiConfig
                {
                    Endpoint = "https://api.groq.com/openai/v1/chat/completions",
                    DefaultModel = "llama3-70b-8192",
                    DefaultParameters = JObject.FromObject(new { temperature = 0.7, top_p = 1.0, typical_p = 1.0, max_tokens = 8192 }),
                    RequestPaths = OpenAiChatRequestPaths,
                    ResponsePaths = OpenAiChatResponsePaths,
                    ToolPaths = OpenAiToolPaths,
                    JsonMode = OpenAiJsonMode
                }
            };
            yield return new ChatTemplate
            {
                ProviderName = "Anthropic (OpenAI-compatible)",
                ProviderUrl = "https://docs.anthropic.com/en/api/openai-sdk",
                Http = OpenAiHttpConfig,
                ChatApi = new ChatApiConfig
                {
                    Endpoint = "https://api.anthropic.com/v1/chat/completions",
                    DefaultModel = "claude-3-5-sonnet-latest",
                    DefaultParameters = JObject.FromObject(new { temperature = 0.7, top_p = 1.0, typical_p = 1.0, max_tokens = 300 }),
                    RequestPaths = OpenAiChatRequestPaths,
                    ResponsePaths = OpenAiChatResponsePaths,
                    ToolPaths = OpenAiToolPaths,
                    JsonMode = OpenAiJsonMode
                }
            };
            yield return new ChatTemplate
            {
                ProviderName = "Google Gemini (OpenAI-compatible)",
                ProviderUrl = "https://ai.google.dev/",
                Http = OpenAiHttpConfig,
                ChatApi = new ChatApiConfig
                {
                    Endpoint = "https://generativelanguage.googleapis.com/v1beta/openai/chat/completions",
                    DefaultModel = "gemini-2.5-flash",
                    DefaultParameters = JObject.FromObject(new { temperature = 0.7, top_p = 1.0, typical_p = 1.0, max_tokens = 300 }),
                    RequestPaths = OpenAiChatRequestPaths,
                    ResponsePaths = OpenAiChatResponsePaths,
                    ToolPaths = OpenAiToolPaths,
                    JsonMode = OpenAiJsonMode
                },
                StaticParameters = JObject.FromObject(new { reasoning_effort = "none" })
            };
            yield return new ChatTemplate
            {
                ProviderName = "DeepSeek",
                ProviderUrl = "https://platform.deepseek.com/",
                Http = OpenAiHttpConfig,
                ChatApi = new ChatApiConfig
                {
                    Endpoint = "https://api.deepseek.com/v1/chat/completions",
                    DefaultModel = "deepseek-chat",
                    DefaultParameters = JObject.FromObject(new { temperature = 0.7, top_p = 1.0, typical_p = 1.0, max_tokens = 300 }),
                    RequestPaths = OpenAiChatRequestPaths,
                    ResponsePaths = OpenAiChatResponsePaths,
                    ToolPaths = OpenAiToolPaths,
                    JsonMode = OpenAiJsonMode
                }
            };
            yield return new ChatTemplate
            {
                ProviderName = "Siliconflow",
                ProviderUrl = "https://siliconflow.cn/",
                Http = OpenAiHttpConfig,
                ChatApi = new ChatApiConfig
                {
                    Endpoint = "https://api.siliconflow.cn/v1/chat/completions",
                    DefaultModel = "Qwen/Qwen2.5-72B-Instruct",
                    DefaultParameters = JObject.FromObject(new { temperature = 0.7, top_p = 1.0, typical_p = 1.0, max_tokens = 300 }),
                    RequestPaths = OpenAiChatRequestPaths,
                    ResponsePaths = OpenAiChatResponsePaths,
                    ToolPaths = OpenAiToolPaths,
                    JsonMode = OpenAiJsonMode
                }
            };
            yield return new ChatTemplate
            {
                ProviderName = "OpenAI",
                ProviderUrl = "https://platform.openai.com/",
                Http = OpenAiHttpConfig,
                ChatApi = new ChatApiConfig
                {
                    Endpoint = "https://api.openai.com/v1/chat/completions",
                    DefaultModel = "gpt-4o",
                    DefaultParameters = JObject.FromObject(new { temperature = 0.7, top_p = 1.0, typical_p = 1.0, max_tokens = 300 }),
                    RequestPaths = OpenAiChatRequestPaths,
                    ResponsePaths = OpenAiChatResponsePaths,
                    ToolPaths = OpenAiToolPaths,
                    JsonMode = OpenAiJsonMode
                }
            };
        }

        /// <summary>
        /// 获取所有内置的 Embedding 服务模板。
        /// </summary>
        public static IEnumerable<EmbeddingTemplate> GetEmbeddingTemplates()
        {
            // --- Local & Compatible Providers ---
            yield return new EmbeddingTemplate
            {
                ProviderName = "OpenAI-compatible",
                ProviderUrl = "https://example.com",
                Http = OpenAiHttpConfig,
                EmbeddingApi = new EmbeddingApiConfig
                {
                    Endpoint = "http://localhost:8080/v1/embeddings",
                    DefaultModel = "default-embedding-model",
                    MaxBatchSize = 512,
                    RequestPaths = OpenAiEmbeddingRequestPaths,
                    ResponsePaths = OpenAiEmbeddingResponsePaths
                }
            };
            yield return new EmbeddingTemplate
            {
                ProviderName = "Ollama",
                ProviderUrl = "https://ollama.com/",
                Http = OpenAiHttpConfig,
                EmbeddingApi = new EmbeddingApiConfig
                {
                    Endpoint = "http://localhost:11434/v1/embeddings",
                    DefaultModel = "nomic-embed-text",
                    MaxBatchSize = 2048,
                    RequestPaths = OpenAiEmbeddingRequestPaths,
                    ResponsePaths = OpenAiEmbeddingResponsePaths
                }
            };
            yield return new EmbeddingTemplate
            {
                ProviderName = "LM Studio",
                ProviderUrl = "https://lmstudio.ai/",
                Http = OpenAiHttpConfig,
                EmbeddingApi = new EmbeddingApiConfig
                {
                    Endpoint = "http://localhost:1234/v1/embeddings",
                    DefaultModel = "loaded-model",
                    MaxBatchSize = 2048,
                    RequestPaths = OpenAiEmbeddingRequestPaths,
                    ResponsePaths = OpenAiEmbeddingResponsePaths
                }
            };
            yield return new EmbeddingTemplate
            {
                ProviderName = "vLLM",
                ProviderUrl = "https://github.com/vllm-project/vllm",
                Http = OpenAiHttpConfig,
                EmbeddingApi = new EmbeddingApiConfig
                {
                    Endpoint = "http://localhost:8000/v1/embeddings",
                    DefaultModel = "loaded-model",
                    MaxBatchSize = 2048,
                    RequestPaths = OpenAiEmbeddingRequestPaths,
                    ResponsePaths = OpenAiEmbeddingResponsePaths
                }
            };
            yield return new EmbeddingTemplate
            {
                ProviderName = "SGLang",
                ProviderUrl = "https://github.com/sgl-project/sglang",
                Http = OpenAiHttpConfig,
                EmbeddingApi = new EmbeddingApiConfig
                {
                    Endpoint = "http://localhost:30000/v1/embeddings",
                    DefaultModel = "loaded-model",
                    MaxBatchSize = 2048,
                    RequestPaths = OpenAiEmbeddingRequestPaths,
                    ResponsePaths = OpenAiEmbeddingResponsePaths
                }
            };

            // --- Cloud Providers ---
            yield return new EmbeddingTemplate
            {
                ProviderName = "Anthropic (OpenAI-compatible)",
                ProviderUrl = "",
                Http = OpenAiHttpConfig,
                EmbeddingApi = new EmbeddingApiConfig
                {
                    Endpoint = "",
                    DefaultModel = "",
                    MaxBatchSize = 2048,
                    RequestPaths = OpenAiEmbeddingRequestPaths,
                    ResponsePaths = OpenAiEmbeddingResponsePaths
                }
            };
            yield return new EmbeddingTemplate
            {
                ProviderName = "Groq",
                ProviderUrl = "https://groq.com/",
                Http = OpenAiHttpConfig,
                EmbeddingApi = new EmbeddingApiConfig
                {
                    Endpoint = "https://api.groq.com/openai/v1/embeddings",
                    DefaultModel = "nomic-embed-text", // Groq does not have its own, uses others
                    MaxBatchSize = 2048,
                    RequestPaths = OpenAiEmbeddingRequestPaths,
                    ResponsePaths = OpenAiEmbeddingResponsePaths
                }
            };
            yield return new EmbeddingTemplate
            {
                ProviderName = "Google Gemini (OpenAI-compatible)",
                ProviderUrl = "https://ai.google.dev/",
                Http = OpenAiHttpConfig,
                EmbeddingApi = new EmbeddingApiConfig
                {
                    Endpoint = "https://generativelanguage.googleapis.com/v1beta/openai/embeddings",
                    DefaultModel = "gemini-embedding-001",
                    MaxBatchSize = 2048,
                    RequestPaths = OpenAiEmbeddingRequestPaths,
                    ResponsePaths = OpenAiEmbeddingResponsePaths
                }
            };
            yield return new EmbeddingTemplate
            {
                ProviderName = "DeepSeek",
                ProviderUrl = "https://platform.deepseek.com/",
                Http = OpenAiHttpConfig,
                EmbeddingApi = new EmbeddingApiConfig
                {
                    Endpoint = "https://api.deepseek.com/v1/embeddings",
                    DefaultModel = "deepseek-embedder",
                    MaxBatchSize = 32,
                    RequestPaths = OpenAiEmbeddingRequestPaths,
                    ResponsePaths = OpenAiEmbeddingResponsePaths
                }
            };
            yield return new EmbeddingTemplate
            {
                ProviderName = "Siliconflow",
                ProviderUrl = "https://siliconflow.cn/",
                Http = OpenAiHttpConfig,
                EmbeddingApi = new EmbeddingApiConfig
                {
                    Endpoint = "https://api.siliconflow.cn/v1/embeddings",
                    DefaultModel = "BAAI/bge-m3",
                    MaxBatchSize = 512,
                    RequestPaths = OpenAiEmbeddingRequestPaths,
                    ResponsePaths = OpenAiEmbeddingResponsePaths
                }
            };
            yield return new EmbeddingTemplate
            {
                ProviderName = "OpenAI",
                ProviderUrl = "https://platform.openai.com/",
                Http = OpenAiHttpConfig,
                EmbeddingApi = new EmbeddingApiConfig
                {
                    Endpoint = "https://api.openai.com/v1/embeddings",
                    DefaultModel = "text-embedding-3-small",
                    MaxBatchSize = 2048,
                    RequestPaths = OpenAiEmbeddingRequestPaths,
                    ResponsePaths = OpenAiEmbeddingResponsePaths
                }
            };
        }
    }
}
