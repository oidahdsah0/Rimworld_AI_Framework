using System.Collections.Generic;

namespace RimAI.Framework.Translation.Models
{
    #region 统一聊天请求 (Unified Chat Request)

    /// <summary>
    /// 我们框架内部统一的聊天请求模型。
    /// 它聚合了所有可能的聊天请求参数，用于在框架内部各组件之间传递信息。
    /// </summary>
    public class UnifiedChatRequest
    {
        /// <summary>
        /// 本次对话的上下文消息列表。
        /// </summary>
        public List<ChatMessage> Messages { get; set; } = new List<ChatMessage>();

        /// <summary>
        /// 本次请求中可供模型选择使用的工具列表。
        /// 如果不提供或列表为空，则表示不使用工具调用。
        /// </summary>
        public List<ToolDefinition> Tools { get; set; }

        /// <summary>
        /// 是否强制要求模型以JSON格式输出。
        /// </summary>
        public bool ForceJsonOutput { get; set; } = false;

        /// <summary>
        /// 是否以流式方式接收响应。
        /// </summary>
        public bool Stream { get; set; } = false;
    }

    /// <summary>
    /// 表示一次对话中的单条消息。
    /// </summary>
    public class ChatMessage
    {
        /// <summary>
        /// 消息的角色。 "user", "assistant", "system", 或 "tool"。
        /// </summary>
        public string Role { get; set; }

        /// <summary>
        /// 消息的具体文本内容。
        /// </summary>
        public string Content { get; set; }

        // --- 以下属性仅在特定角色时使用 ---

        /// <summary>
        /// 当角色为 "assistant" 且该消息包含工具调用时，此列表不为空。
        /// </summary>
        public List<ToolCall> ToolCalls { get; set; }

        /// <summary>
        /// 当角色为 "tool" 时，此属性不为空，用于标识此工具响应对应的是哪个ToolCall。
        /// </summary>
        public string ToolCallId { get; set; }
    }

    #endregion

    #region 统一聊天响应 (Unified Chat Response)

    /// <summary>
    /// 我们框架内部统一的聊天响应模型。
    /// 所有来自外部API的响应都会被翻译成这个标准格式。
    /// </summary>
    public class UnifiedChatResponse
    {
        /// <summary>
        /// 本次响应的完成原因。
        /// 例如 "stop", "length", "tool_calls"。
        /// </summary>
        public string FinishReason { get; set; }

        /// <summary>
        /// 模型生成的回复消息。
        /// </summary>
        public ChatMessage Message { get; set; }
    }

    #endregion
}