using System.Collections.Generic;
using Newtonsoft.Json.Linq;

namespace RimAI.Framework.Translation.Models
{
    /// <summary>
    /// 定义一个可供大语言模型调用的外部工具（函数）。
    /// 这是我们框架内部描述一个工具的统一标准。
    /// </summary>
    public class ToolDefinition
    {
        /// <summary>
        /// 工具的类型，几乎总是 "function"。
        /// </summary>
        public string Type { get; set; } = "function";

        /// <summary>
        /// 描述工具具体内容的 JObject，对于函数来说，它包含 name, description, parameters。
        /// </summary>
        public JObject Function { get; set; }
    }

    /// <summary>
    /// 表示大语言模型请求进行的一次工具调用。
    /// 这是框架内部表示一次工具调用的统一标准。
    /// </summary>
    public class ToolCall
    {
        /// <summary>
        /// 模型生成的唯一ID，用于标识这次特定的工具调用。
        /// 当我们返回工具执行结果时，需要将这个ID一并返回，以便模型知道是哪个调用的结果。
        /// </summary>
        public string Id { get; set; }
        
        /// <summary>
        /// 【新增】工具调用的类型，几乎总是 "function"。
        /// </summary>
        public string Type { get; set; }

        /// <summary>
        /// 要调用的工具的名称，与 ToolDefinition 中的 Name 相对应。
        /// </summary>
        public string FunctionName { get; set; }

        /// <summary>
        /// 模型为函数参数生成的参数值，表现为一个JSON格式的字符串。
        /// 例如: "{\"location\":\"Boston\"}"
        /// </summary>
        public string Arguments { get; set; }
    }
}