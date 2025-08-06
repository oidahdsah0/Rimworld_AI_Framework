using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace RimAI.Framework.Contracts
{
    /// <summary>
    /// 定义一个可供大语言模型调用的外部工具（函数）。
    /// 这是框架对外公开的统一工具描述格式。
    /// </summary>
    public class ToolDefinition
    {
        /// <summary>
        /// 工具类型，绝大多数情况下为 "function"。
        /// </summary>
        [JsonProperty("type")]
        public string Type { get; set; } = "function";

        /// <summary>
        /// 函数主体，包含 name / description / parameters 等字段。
        /// 使用 JObject 以保持灵活性。
        /// </summary>
        [JsonProperty("function")]
        public JObject Function { get; set; }
    }

    /// <summary>
    /// 表示一次由大语言模型发起的工具调用。
    /// </summary>
    public class ToolCall
    {
        /// <summary>
        /// 模型生成的唯一 ID，用于回传结果时的关联。
        /// </summary>
        [JsonProperty("id")]
        public string Id { get; set; }

        /// <summary>
        /// 调用类型，通常为 "function"。
        /// </summary>
        [JsonProperty("type")]
        public string Type { get; set; }

        /// <summary>
        /// 嵌套的 function 对象，包含函数名和参数。
        /// </summary>
        [JsonProperty("function")]
        public ToolFunction Function { get; set; }
    }

    /// <summary>
    /// 描述一次调用中关于 function 的具体信息。
    /// </summary>
    public class ToolFunction
    {
        /// <summary>
        /// 函数名称。
        /// </summary>
        [JsonProperty("name")]
        public string Name { get; set; }

        /// <summary>
        /// 函数描述。供 LLM 理解工具用途。
        /// </summary>
        [JsonProperty("description")]
        public string Description { get; set; }

        /// <summary>
        /// 参数 JSON 字符串。
        /// </summary>
        [JsonProperty("arguments")]
        public string Arguments { get; set; }
    }
}