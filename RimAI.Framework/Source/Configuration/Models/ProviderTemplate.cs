using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace RimAI.Framework.Configuration.Models
{
    public class ProviderTemplate
    {
        [JsonPropertyName("providerName")]
        public string ProviderName { get; set; }

        [JsonPropertyName("chatApi")]
        public ChatApiConfig ChatApi { get; set; }

        [JsonPropertyName("embeddingApi")]
        public EmbeddingApiConfig EmbeddingApi { get; set; }
    }

    // --- 通用配置 ---

    public abstract class ApiConfigBase
    {
        [JsonPropertyName("endpoint")]
        public string Endpoint { get; set; }

        [JsonPropertyName("authHeader")]
        public string AuthHeader { get; set; }

        [JsonPropertyName("authScheme")]
        public string AuthScheme { get; set; }

        // 关键改动 : 使用字典来表示请求模板
        [JsonPropertyName("requestTemplate")]
        public Dictionary<string, string> RequestTemplate { get; set; }
    }

    // --- Chat API 配置 ---

    public class ChatApiConfig : ApiConfigBase
    {
        [JsonPropertyName("messagesPath")]
        public string MessagesPath { get; set; }

        [JsonPropertyName("streamFlag")]
        public KeyValuePair<string, object> StreamFlag { get; set; }

        [JsonPropertyName("response")]
        public ChatResponseConfig ResponsePaths { get; set; }
    }

    public class ChatResponsePaths
    {
        [JsonPropertyName("contentPath")]
        public string ContentPath { get; set; }

        [JsonPropertyName("streamingContentPath")]
        public string StreamingContentPath { get; set; }
    }

    // --- Embedding API 配置 ---

    public class EmbeddingApiConfig : ApiConfigBase
    {
        [JsonPropertyName("maxBatchSize")]
        public int MaxBatchSize { get; set; }

        [JsonPropertyName("inputPath")]
        public string InputPath { get; set; }

        [JsonPropertyName("response")]
        public EmbeddingResponseConfig ResponsePaths { get; set; }
    }

    public class EmbeddingResponsePaths
    {
        [JsonPropertyName("dataListPath")]
        public string DataListPath { get; set; }

        [JsonPropertyName("embeddingPath")]
        public string EmbeddingPath { get; set; }

        [JsonPropertyName("indexPath")]
        public string IndexPath { get; set; }
    }
}