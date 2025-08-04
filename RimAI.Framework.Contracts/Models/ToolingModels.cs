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
        public string Type { get; set; } = "function";

        /// <summary>
        /// 函数主体，包含 name / description / parameters 等字段。
        /// 使用 JObject 以保持灵活性。
        /// </summary>
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
        public string Id { get; set; }

        /// <summary>
        /// 调用类型，通常为 "function"。
        /// </summary>
        public string Type { get; set; }

        /// <summary>
        /// 要调用的函数名称，对应 <see cref="ToolDefinition"/> 中的 name 字段。
        /// </summary>
        public string FunctionName { get; set; }

        /// <summary>
        /// 模型推断出的参数 JSON 字符串，如 "{\"location\":\"Boston\"}"。
        /// </summary>
        public string Arguments { get; set; }
    }
}