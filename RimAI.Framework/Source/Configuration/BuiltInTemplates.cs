using Newtonsoft.Json.Linq;
using RimAI.Framework.Configuration.Models;
using System.Collections.Generic;

namespace RimAI.Framework.Configuration
{
    public static class BuiltInTemplates
    {
        public static IEnumerable<ProviderTemplate> GetAll()
        {
            // --- OpenAI Template ---
            yield return new ProviderTemplate
            {
                ProviderName = "OpenAI",
                ProviderUrl = "https://platform.openai.com/",
                Http = new HttpConfig
                {
                    AuthHeader = "Authorization",
                    AuthScheme = "Bearer",
                    Headers = new Dictionary<string, string> { { "Content-Type", "application/json" } }
                },
                ChatApi = new ChatApiConfig
                {
                    Endpoint = "https://api.openai.com/v1/chat/completions",
                    DefaultModel = "gpt-4o",
                    DefaultParameters = JObject.FromObject(new { temperature = 0.7, top_p = 1.0 }),
                    RequestPaths = new ChatRequestPaths { Model = "model", Messages = "messages", Temperature = "temperature", TopP = "top_p", Stream = "stream", Tools = "tools", ToolChoice = "tool_choice" },
                    ResponsePaths = new ChatResponsePaths { Choices = "choices", Content = "message.content", ToolCalls = "message.tool_calls", FinishReason = "finish_reason" },
                    ToolPaths = new ToolPaths { Root = "tools", Type = "type", FunctionRoot = "function", FunctionName = "name", FunctionDescription = "description", FunctionParameters = "parameters" },
                    JsonMode = new JsonModeConfig { Path = "response_format", Value = JObject.FromObject(new { type = "json_object" }) }
                },
                EmbeddingApi = new EmbeddingApiConfig
                {
                    Endpoint = "https://api.openai.com/v1/embeddings",
                    DefaultModel = "text-embedding-3-small",
                    MaxBatchSize = 2048,
                    RequestPaths = new EmbeddingRequestPaths { Model = "model", Input = "input" },
                    ResponsePaths = new EmbeddingResponsePaths { DataList = "data", Embedding = "embedding", Index = "index" }
                },
                StaticParameters = new JObject()
            };

            // --- 【新增】Google Gemini Template ---
            yield return new ProviderTemplate
            {
                ProviderName = "Gemini",
                ProviderUrl = "https://ai.google.dev/",
                Http = new HttpConfig
                {
                    // Gemini API Key in URL, not in header
                    AuthHeader = null, 
                    AuthScheme = null,
                    Headers = new Dictionary<string, string> { { "Content-Type", "application/json" } }
                },
                ChatApi = new ChatApiConfig
                {
                    // Note: Gemini's endpoint structure is different, API Key is a query parameter
                    Endpoint = "https://generativelanguage.googleapis.com/v1beta/models/{model}:generateContent?key={apiKey}",
                    DefaultModel = "gemini-1.5-flash",
                    DefaultParameters = JObject.FromObject(new { temperature = 0.8 }),
                    // Gemini uses a different request/response structure
                    RequestPaths = new ChatRequestPaths { /* ... requires custom translator logic ... */ Messages = "contents" },
                    ResponsePaths = new ChatResponsePaths { /* ... requires custom translator logic ... */ Content = "candidates[0].content.parts[0].text" },
                    ToolPaths = null, // Gemini tool calling is different, handle in translator
                    JsonMode = null   // Gemini JSON mode is different, handle in translator
                },
                EmbeddingApi = new EmbeddingApiConfig
                {
                    Endpoint = "https://generativelanguage.googleapis.com/v1beta/models/{model}:embedContent?key={apiKey}",
                    DefaultModel = "text-embedding-004",
                    MaxBatchSize = 100,
                    RequestPaths = new EmbeddingRequestPaths { Model = "model", Input = "requests[].content.parts[].text" }, // Example, needs complex translation
                    ResponsePaths = new EmbeddingResponsePaths { DataList = "embeddings", Embedding = "values" }
                },
                StaticParameters = new JObject()
            };
        }
    }
}