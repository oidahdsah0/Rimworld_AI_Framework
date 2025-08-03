using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;

namespace RimAI.Framework.Configuration.Models
{
    // --- Template Model ---
    public class ChatTemplate
    {
        public string ProviderName { get; set; }
        public string ProviderUrl { get; set; }
        public HttpConfig Http { get; set; }
        public ChatApiConfig ChatApi { get; set; }
        public JObject StaticParameters { get; set; }
    }

    // --- User Config Model ---
    public class ChatUserConfig
    {
        [JsonProperty("apiKey")] public string ApiKey { get; set; }
        [JsonProperty("modelOverride")] public string ModelOverride { get; set; }
        [JsonProperty("endpointOverride")] public string EndpointOverride { get; set; }
        [JsonProperty("temperature")] public float? Temperature { get; set; }
        [JsonProperty("topP")] public float? TopP { get; set; }
        [JsonProperty("concurrencyLimit")] public int? ConcurrencyLimit { get; set; }
        [JsonProperty("customHeaders")] public Dictionary<string, string> CustomHeaders { get; set; }
        [JsonProperty("staticParametersOverride")] public JObject StaticParametersOverride { get; set; }
    }

    // --- Merged Config Model ---
    public class MergedChatConfig
    {
        public ChatTemplate Template { get; set; }
        public ChatUserConfig User { get; set; }
        public string ProviderName => Template.ProviderName;
        public string ApiKey => User.ApiKey;
        public int ConcurrencyLimit => User.ConcurrencyLimit ?? 5;
        public string Endpoint => User.EndpointOverride ?? Template.ChatApi.Endpoint;
        public string Model => User.ModelOverride ?? Template.ChatApi.DefaultModel;
    }

    // --- Sub-Models ---
    public class HttpConfig
    {
        [JsonProperty("authHeader")] public string AuthHeader { get; set; }
        [JsonProperty("authScheme")] public string AuthScheme { get; set; }
        [JsonProperty("headers")] public Dictionary<string, string> Headers { get; set; }
    }

    public class ChatApiConfig
    {
        [JsonProperty("endpoint")] public string Endpoint { get; set; }
        [JsonProperty("defaultModel")] public string DefaultModel { get; set; }
        [JsonProperty("defaultParameters")] public JObject DefaultParameters { get; set; }
        [JsonProperty("requestPaths")] public ChatRequestPaths RequestPaths { get; set; }
        [JsonProperty("responsePaths")] public ChatResponsePaths ResponsePaths { get; set; }
        [JsonProperty("toolPaths")] public ToolPaths ToolPaths { get; set; }
        [JsonProperty("jsonMode")] public JsonModeConfig JsonMode { get; set; }
    }
    
    // --- Path Models ---
    public class ChatRequestPaths
    {
        [JsonProperty("model")] public string Model { get; set; }
        [JsonProperty("messages")] public string Messages { get; set; }
        [JsonProperty("temperature")] public string Temperature { get; set; }
        [JsonProperty("top_p")] public string TopP { get; set; }
        [JsonProperty("stream")] public string Stream { get; set; }
        [JsonProperty("tools")] public string Tools { get; set; }
        [JsonProperty("toolChoice")] public string ToolChoice { get; set; }
    }

    public class ChatResponsePaths
    {
        [JsonProperty("choices")] public string Choices { get; set; }
        [JsonProperty("content")] public string Content { get; set; }
        [JsonProperty("toolCalls")] public string ToolCalls { get; set; }
        [JsonProperty("finishReason")] public string FinishReason { get; set; }
    }

    public class ToolPaths
    {
        [JsonProperty("root")] public string Root { get; set; }
        [JsonProperty("type")] public string Type { get; set; }
        [JsonProperty("functionRoot")] public string FunctionRoot { get; set; }
        [JsonProperty("functionName")] public string FunctionName { get; set; }
        [JsonProperty("functionDescription")] public string FunctionDescription { get; set; }
        [JsonProperty("functionParameters")] public string FunctionParameters { get; set; }
    }

    public class JsonModeConfig
    {
        [JsonProperty("path")] public string Path { get; set; }
        [JsonProperty("value")] public JToken Value { get; set; }
    }
}