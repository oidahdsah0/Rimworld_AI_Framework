// 同样，我们需要这两个命名空间
using System.Collections.Generic;
using Newtonsoft.Json;

namespace RimAI.Framework.Configuration.Models
{
    /// <summary>
    /// C# 类，精确映射 user_config_*.json 文件的结构。
    /// 这个类包含所有用户特定的、私有的配置信息。
    /// </summary>
    public class UserConfig
    {
        // --- 核心私有信息 ---

        [JsonProperty("apiKey")]
        public string ApiKey { get; set; }

        // --- 可选的覆盖设置 ---
        // 用户可以通过这些字段来覆盖 ProviderTemplate 中的默认值。

        [JsonProperty("chatModelOverride")]
        public string ChatModelOverride { get; set; }

        [JsonProperty("embeddingModelOverride")]
        public string EmbeddingModelOverride { get; set; }

        [JsonProperty("chatEndpointOverride")]
        public string ChatEndpointOverride { get; set; }

        [JsonProperty("embeddingEndpointOverride")]
        public string EmbeddingEndpointOverride { get; set; }

        // --- 用户个人参数偏好 ---
        // 注意：这些是可空类型 (float?), 因为用户可能不设置它们。
        // 如果用户不设置（JSON中该字段为null或不存在），这些属性在反序列化后也将是 null。
        // 这对于我们后续在 MergedConfig 中实现“用户优先”逻辑至关重要。

        [JsonProperty("temperature")]
        public float? Temperature { get; set; }

        [JsonProperty("topP")]
        public float? TopP { get; set; }

        // --- 框架级别的设置 ---

        [JsonProperty("concurrencyLimit")]
        public int? ConcurrencyLimit { get; set; } // 同样设为可空，以便我们能提供一个框架级的默认值

        // --- 扩展性设置 ---

        // 用户的自定义HTTP头
        [JsonProperty("customHeaders")]
        public Dictionary<string, string> CustomHeaders { get; set; }

        // 用户的自定义静态参数，将与模板中的 staticParameters 深度合并。
        [JsonProperty("staticParametersOverride")]
        public Dictionary<string, object> StaticParametersOverride { get; set; }
    }
}