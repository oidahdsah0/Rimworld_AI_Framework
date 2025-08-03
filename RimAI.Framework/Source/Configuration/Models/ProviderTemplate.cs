// 引入 Newtonsoft.Json 来使用 [JsonProperty] 特性。
// 引入 System.Collections.Generic 来使用 Dictionary。
using System.Collections.Generic;
using Newtonsoft.Json;

namespace RimAI.Framework.Configuration.Models
{
    /// <summary>
    /// C# 类，精确映射 provider_template_*.json 文件的复杂结构。
    /// 这个类及其所有子类共同定义了一个提供商的“API说明书”。
    /// </summary>
    public class ProviderTemplate
    {
        [JsonProperty("providerName")]
        public string ProviderName { get; set; }

        [JsonProperty("providerUrl")]
        public string ProviderUrl { get; set; }

        [JsonProperty("http")]
        public HttpConfig Http { get; set; }

        [JsonProperty("chatApi")]
        public ChatApiConfig ChatApi { get; set; }

        [JsonProperty("embeddingApi")]
        public EmbeddingApiConfig EmbeddingApi { get; set; }

        // 使用 Dictionary<string, object> 来接收灵活的、任意嵌套的JSON对象。
        // 这对于 staticParameters 这种“逃生舱口”性质的字段至关重要。
        [JsonProperty("staticParameters")]
        public Dictionary<string, object> StaticParameters { get; set; }
    }

    #region 子配置模型 (Nested Configuration Models)

    /// <summary>
    /// 对应 JSON 中的 "http" 对象，包含所有HTTP协议级别的配置。
    /// </summary>
    public class HttpConfig
    {
        [JsonProperty("authHeader")]
        public string AuthHeader { get; set; }

        [JsonProperty("authScheme")]
        public string AuthScheme { get; set; }

        [JsonProperty("headers")]
        public Dictionary<string, string> Headers { get; set; }
    }

    /// <summary>
    /// 对应 JSON 中的 "chatApi" 对象，包含所有与聊天功能相关的适配规则。
    /// </summary>
    public class ChatApiConfig
    {
        [JsonProperty("endpoint")]
        public string Endpoint { get; set; }

        [JsonProperty("defaultModel")]
        public string DefaultModel { get; set; }

        // 对应 JSON 中的 "defaultParameters" 对象，用于存放如 temperature, top_p 等参数的默认值。
        [JsonProperty("defaultParameters")]
        public Dictionary<string, object> DefaultParameters { get; set; }

        [JsonProperty("requestPaths")]
        public ChatRequestPaths RequestPaths { get; set; }

        [JsonProperty("responsePaths")]
        public ChatResponsePaths ResponsePaths { get; set; }

        [JsonProperty("toolPaths")]
        public ToolPaths ToolPaths { get; set; }

        [JsonProperty("jsonMode")]
        public JsonModeConfig JsonMode { get; set; }
    }

    /// <summary>
    /// 对应 JSON 中的 "embeddingApi" 对象。
    /// </summary>
    public class EmbeddingApiConfig
    {
        [JsonProperty("endpoint")]
        public string Endpoint { get; set; }

        [JsonProperty("defaultModel")]
        public string DefaultModel { get; set; }

        [JsonProperty("maxBatchSize")]
        public int MaxBatchSize { get; set; }

        [JsonProperty("requestPaths")]
        public EmbeddingRequestPaths RequestPaths { get; set; }

        [JsonProperty("responsePaths")]
        public EmbeddingResponsePaths ResponsePaths { get; set; }
    }

    // --- Chat API 的路径定义 ---

    public class ChatRequestPaths
    {
        [JsonProperty("model")]
        public string Model { get; set; }

        [JsonProperty("messages")]
        public string Messages { get; set; }

        [JsonProperty("temperature")]
        public string Temperature { get; set; }

        // [JsonProperty("top_p")] 告诉序列化器：
        // 当看到JSON里有 "top_p" 字段时，请把它赋值给 C# 的 "TopP" 属性。
        [JsonProperty("top_p")]
        public string TopP { get; set; }

        [JsonProperty("stream")]
        public string Stream { get; set; }

        [JsonProperty("tools")]
        public string Tools { get; set; }

        [JsonProperty("toolChoice")]
        public string ToolChoice { get; set; }
    }

    public class ChatResponsePaths
    {
        [JsonProperty("choices")]
        public string Choices { get; set; }

        [JsonProperty("content")]
        public string Content { get; set; }

        [JsonProperty("toolCalls")]
        public string ToolCalls { get; set; }

        [JsonProperty("finishReason")]
        public string FinishReason { get; set; }
    }

    public class ToolPaths
    {
        [JsonProperty("root")]
        public string Root { get; set; }

        [JsonProperty("type")]
        public string Type { get; set; }

        [JsonProperty("functionName")]
        public string FunctionName { get; set; }

        [JsonProperty("functionDescription")]
        public string FunctionDescription { get; set; }

        [JsonProperty("functionParameters")]
        public string FunctionParameters { get; set; }
    }

    public class JsonModeConfig
    {
        [JsonProperty("path")]
        public string Path { get; set; }

        // 使用 object 类型来接收任意类型的JSON值，无论是简单字符串还是复杂对象。
        [JsonProperty("value")]
        public object Value { get; set; }
    }

    // --- Embedding API 的路径定义 ---

    public class EmbeddingRequestPaths
    {
        [JsonProperty("model")]
        public string Model { get; set; }

        [JsonProperty("input")]
        public string Input { get; set; }
    }

    public class EmbeddingResponsePaths
    {
        [JsonProperty("dataList")]
        public string DataList { get; set; }

        [JsonProperty("embedding")]
        public string Embedding { get; set; }

        [JsonProperty("index")]
        public string Index { get; set; }
    }

    #endregion
}