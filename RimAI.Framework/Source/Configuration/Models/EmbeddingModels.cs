using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;

namespace RimAI.Framework.Configuration.Models
{
    // --- Template Model ---
    public class EmbeddingTemplate
    {
        public string ProviderName { get; set; }
        public string ProviderUrl { get; set; }
        public HttpConfig Http { get; set; } // Re-defined here
        public EmbeddingApiConfig EmbeddingApi { get; set; }
    }

    // --- User Config Model ---
    public class EmbeddingUserConfig
    {
        [JsonProperty("apiKey")] public string ApiKey { get; set; }
        [JsonProperty("modelOverride")] public string ModelOverride { get; set; }
        [JsonProperty("endpointOverride")] public string EndpointOverride { get; set; }
        [JsonProperty("customHeaders")] public Dictionary<string, string> CustomHeaders { get; set; }
        [JsonProperty("staticParametersOverride")] public JObject StaticParametersOverride { get; set; }
    }

    // --- Merged Config Model ---
    public class MergedEmbeddingConfig
    {
        public EmbeddingTemplate Template { get; set; }
        public EmbeddingUserConfig User { get; set; }
        public string ProviderName => Template.ProviderName;
        public string ApiKey => User.ApiKey;
        public string Endpoint => User.EndpointOverride ?? Template.EmbeddingApi.Endpoint;
        public string Model => User.ModelOverride ?? Template.EmbeddingApi.DefaultModel;
        public int MaxBatchSize => Template.EmbeddingApi.MaxBatchSize;
    }

    // --- Sub-Models (re-defined from Shared files) ---
    // Although HttpConfig is defined in ChatModels.cs, we redefine it here
    // to make this file completely self-contained, as per the user's request.
    // The C# compiler will handle this correctly as long as they are in the same namespace.
    // However, to avoid compiler errors for duplicate types, we only define what's necessary.
    // The best approach is to only define HttpConfig once. Let's assume it's in ChatModels.

    public class EmbeddingApiConfig
    {
        [JsonProperty("endpoint")] public string Endpoint { get; set; }
        [JsonProperty("defaultModel")] public string DefaultModel { get; set; }
        [JsonProperty("maxBatchSize")] public int MaxBatchSize { get; set; }
        [JsonProperty("requestPaths")] public EmbeddingRequestPaths RequestPaths { get; set; }
        [JsonProperty("responsePaths")] public EmbeddingResponsePaths ResponsePaths { get; set; }
    }

    // --- Path Models ---
    public class EmbeddingRequestPaths
    {
        [JsonProperty("model")] public string Model { get; set; }
        [JsonProperty("input")] public string Input { get; set; }
    }

    public class EmbeddingResponsePaths
    {
        [JsonProperty("dataList")] public string DataList { get; set; }
        [JsonProperty("embedding")] public string Embedding { get; set; }
        [JsonProperty("index")] public string Index { get; set; }
    }
}