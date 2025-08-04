using System.Collections.Generic;

namespace RimAI.Framework.Contracts
{
    #region 统一聊天请求 (Unified Chat Request)

    /// <summary>
    /// 对外统一的聊天请求模型。
    /// </summary>
    public class UnifiedChatRequest
    {
        /// <summary>
        /// 对话上下文消息列表。
        /// </summary>
        public List<ChatMessage> Messages { get; set; } = new List<ChatMessage>();

        /// <summary>
        /// 可供模型调用的工具列表。
        /// </summary>
        public List<ToolDefinition> Tools { get; set; }

        /// <summary>
        /// 是否强制要求 JSON 输出。
        /// </summary>
        public bool ForceJsonOutput { get; set; } = false;

        /// <summary>
        /// 是否采用流式响应。
        /// </summary>
        public bool Stream { get; set; } = false;
    }

    /// <summary>
    /// 聊天消息。
    /// </summary>
    public class ChatMessage
    {
        /// <summary>
        /// 角色: "user" | "assistant" | "system" | "tool"。
        /// </summary>
        public string Role { get; set; }

        /// <summary>
        /// 文本内容。
        /// </summary>
        public string Content { get; set; }

        /// <summary>
        /// 当角色为 assistant 且包含工具调用时。
        /// </summary>
        public List<ToolCall> ToolCalls { get; set; }

        /// <summary>
        /// 当角色为 tool 时，标识对应的调用 ID。
        /// </summary>
        public string ToolCallId { get; set; }
    }

    #endregion

    #region 统一聊天响应 (Unified Chat Response)

    /// <summary>
    /// 非流式聊天完整响应。
    /// </summary>
    public class UnifiedChatResponse
    {
        /// <summary>
        /// 完成原因: "stop" | "length" | "tool_calls"。
        /// </summary>
        public string FinishReason { get; set; }

        /// <summary>
        /// AI 生成的回复消息。
        /// </summary>
        public ChatMessage Message { get; set; }
    }

    /// <summary>
    /// 流式响应片段 (Chunk)。
    /// </summary>
    public class UnifiedChatChunk
    {
        public string ContentDelta { get; set; }
        public string FinishReason { get; set; }
        public List<ToolCall> ToolCalls { get; set; }
    }

    #endregion
}