# RimAI.Framework v4.2.1 - API 调用指南

欢迎使用 RimAI.Framework v4.2.1 API！本次更新带来了强大的**端到端流式聊天 API**。本指南将帮助您快速上手，并将框架的强大 AI 功能集成到您自己的 Mod 中。

## 1. 快速上手指南 (Quick Start)

本节将引导您完成一次**流式**聊天请求，让您在几分钟内看到 AI 逐字生成回复的酷炫效果。

### 前提条件

1.  **添加引用**: 在您的 C# 项目中，添加对 `RimAI.Framework.dll` 的引用。
2.  **框架配置**: 确保最终用户已经在 RimWorld 的 Mod 设置菜单中，至少配置好了一个聊天服务提供商（例如 OpenAI），并填入了有效的 API Key。
3.  **Embedding 总开关**: 设置页面第一排提供“Embed:OFF/ON”按钮（红/绿）。默认 OFF；OFF 时不会发出任何 Embedding 请求，也不会触发任何 Embedding 测试发送，但仍可编辑并保存所有 Embedding 相关配置。

### 示例：实时获取 AI 回复（需提供会话ID）

```csharp
using RimAI.Framework.API;
using RimAI.Framework.Contracts;
using System.Collections.Generic;
using System.Text;
using Verse;

public class MyModFeature
{
    public async void StreamAiResponse(string question)
    {
        // 1. 构建聊天消息列表
        var messages = new List<ChatMessage>
        {
            new ChatMessage { Role = "system", Content = "You are a helpful assistant." },
            new ChatMessage { Role = "user", Content = question }
        };

        // 2. 构建统一请求对象，并传入唯一会话ID
        var request = new UnifiedChatRequest {
            ConversationId = "my-mod-conv-12345-unique", // 上游必须提供一个稳定且唯一的ID
            Messages = messages
        };

        // 3. 【新】使用 await foreach 消费流式 API
        var responseBuilder = new StringBuilder();
        await foreach (var result in RimAIApi.StreamCompletionAsync(request))
        {
            if (result.IsSuccess)
            {
                var chunk = result.Value;

                // 实时拼接收到的文本块
                if (chunk.ContentDelta != null)
                {
                    responseBuilder.Append(chunk.ContentDelta);
                    // 在这里，您可以将拼接中的文本实时更新到您的 UI 控件上
                    // Log.Message($"实时内容: {responseBuilder.ToString()}"); 
                }

                // 当流结束时
                if (chunk.FinishReason != null)
                {
                    Log.Message($"[MyMod] 流结束，原因: {chunk.FinishReason}");
                    if (chunk.ToolCalls != null)
                    {
                        Log.Message($"[MyMod] 模型请求调用工具: {chunk.ToolCalls.First().Function.Name}");
                    }
                }
            }
            else
            {
                // 如果在流的任何阶段发生错误，都会在这里捕获
                Log.Error($"[MyMod] AI Stream Failed: {result.Error}");
                break; // 出错后中断循环
            }
        }
        
        Log.Message($"[MyMod] 最终完整回复: {responseBuilder.ToString()}");
    }
}
```
调用 `StreamAiResponse("给我讲一个关于机器人的短笑话")`，您将可以在日志中看到 AI 的回复被一个词一个词地构建起来，而不是等待漫长的几秒后才看到完整答案。

---

## 2. 全面调用指南 (Comprehensive Guide)

### Contracts 程序集概览

`RimAI.Framework.Contracts` 作为框架的 **数据契约 (Contracts) 稳定层**，提供以下内容：

* **统一模型定义**：`UnifiedChatRequest`、`UnifiedEmbeddingRequest`、`ChatMessage` 等；
* **错误处理基类**：`Result<T>`，确保调用方显式处理失败；
* **Function Calling 数据模型**：`ToolDefinition`, `ToolCall` 等；

位置：`Rimworld_AI_Framework/RimAI.Framework.Contracts`

引用方式：
```csharp
using RimAI.Framework.Contracts;
```

> 阅读本指南后续示例前，建议先打开 Contracts/Models 目录，了解这些核心 DTO。这样便于理解所有示例代码中的类型。

### 核心设计哲学

*   **静态 API 入口**: 所有功能都通过静态类 `RimAI.Framework.API.RimAIApi` 调用。
*   **统一数据模型**: 无论后端是哪个服务商，您都只与我们统一的请求/响应模型打交道。
*   **健壮的 `Result<T>` 模式**: 所有方法都返回 `Result<T>` 或 `IAsyncEnumerable<Result<T>>`。您**必须**通过检查 `IsSuccess` 来判断操作是否成功。

### 公共 API 接口 (`RimAIApi`)

#### 聊天 (Chat Completions)

**1. 【新增/强制】流式核心方法：`StreamCompletionAsync`（要求 `ConversationId`）**
这是实现实时、逐字响应的最佳方式。

```csharp
public static IAsyncEnumerable<Result<UnifiedChatChunk>> StreamCompletionAsync(
    UnifiedChatRequest request, 
    CancellationToken cancellationToken = default
);
```
*   **返回**: 一个 `UnifiedChatChunk` 的异步流。您必须使用 `await foreach` 来消费它。流中的每个元素都是一个 `Result` 对象，您需要检查其 `IsSuccess` 状态。

**2. (非流式) 核心方法：`GetCompletionAsync`（要求 `ConversationId`）**
用于一次性获取完整的 AI 回复。

```csharp
public static Task<Result<UnifiedChatResponse>> GetCompletionAsync(
    UnifiedChatRequest request, 
    CancellationToken cancellationToken = default
);
```

**3. (非流式) 工具调用辅助方法：`GetCompletionWithToolsAsync`（要求 `ConversationId`）**
这是一个为了简化（非流式）工具调用而设计的便捷方法。新增 `conversationId` 参数。

```csharp
public static Task<Result<UnifiedChatResponse>> GetCompletionWithToolsAsync(
    List<ChatMessage> messages,
    List<ToolDefinition> tools,
    string conversationId,
    CancellationToken cancellationToken = default
);
```

**4. (非流式) 批量处理方法：`GetCompletionsAsync`**
用于并发发送多个独立的聊天请求，并等待所有请求完成后返回。

```csharp
public static Task<List<Result<UnifiedChatResponse>>> GetCompletionsAsync(
    List<UnifiedChatRequest> requests, 
    CancellationToken cancellationToken = default
);
```

**5. 会话缓存失效：`InvalidateConversationCacheAsync`**
用于按会话ID精准失效当前活动 Provider/Model 下的聊天缓存。

```csharp
public static Task<Result<bool>> InvalidateConversationCacheAsync(
    string conversationId,
    CancellationToken cancellationToken = default
);
```

#### 文本嵌入 (Embeddings)

**`GetEmbeddingsAsync`** (非流式)
```csharp
public static Task<Result<UnifiedEmbeddingResponse>> GetEmbeddingsAsync(
    UnifiedEmbeddingRequest request, 
    CancellationToken cancellationToken = default
);
```

开关与语义：
- 当设置中的 Embedding 总开关为 OFF 时，本方法直接返回 `Result.Failure("Embedding is disabled by settings.")`，不会触发缓存、翻译或 HTTP。
- 上游可通过 `RimAIApi.IsEmbeddingEnabled()` 读取当前开关状态，以决定是否展示或启用相关功能。

### 缓存与伪流式行为说明（会话隔离）

- 统一缓存（Framework 层）：
  - Chat 与 Embedding 默认启用短 TTL 缓存（默认 120 秒），失败不缓存。
  - Chat 的 Key 增加“会话维度”，形式：`chat:{provider}:{model}:conv:{sha256(conversationId)[0..15]}:{payloadHash}`。其中 payloadHash 基于规范化请求摘要（忽略 `Stream` 标记）。
  - Embedding 按“单输入文本”粒度缓存（Key = provider + model + sha256(text)）。
- 命中伪流式：
  - 若流式请求命中缓存，Framework 会将缓存中的完整回复切片为 `UnifiedChatChunk` 并“伪流式”立刻吐出，末块包含 `FinishReason` 和可能的 `ToolCalls`。
  - 未命中则执行真实流式；流式过程中不缓存中间块，结束聚合成功后写入整段缓存。
- 配置项：TTL 与开关后续版本将暴露为可配置；当前版本默认启用短 TTL。

### 关键数据模型 (Request Models)

（此部分与之前版本相同，此处省略以保持简洁）
*   `UnifiedChatRequest`（新增字段：`ConversationId` 必填）
*   `ChatMessage`
*   `ToolDefinition` & `ToolCall`
*   ...

### 关键响应模型 (Response Models)

#### 【新增】`UnifiedChatChunk` (流式)
这是 `StreamCompletionAsync` 返回流中的基本数据单元。

```csharp
public class UnifiedChatChunk
{
    // 本数据块中包含的增量文本内容。通常为 null 或单个 token。
    public string ContentDelta { get; set; }

    // 如果流结束，则包含最终的完成原因。仅在最后一个数据块中有效。
    public string FinishReason { get; set; }

    // 如果模型请求调用工具，这里会包含完整的工具调用信息。
    // 通常在流的末尾、`FinishReason` 为 "tool_calls" 时一次性返回。
    public List<ToolCall> ToolCalls { get; set; }
}
```

#### `UnifiedChatResponse` (非流式)
这是 `GetCompletionAsync` 等非流式方法的返回对象。

```csharp
public class UnifiedChatResponse
{
    // 完成原因: "stop", "length", "tool_calls"
    public string FinishReason { get; set; }

    // 模型生成的完整回复消息。
    public ChatMessage Message { get; set; }
}
```

#### `UnifiedEmbeddingResponse` (非流式)
这是 `GetEmbeddingsAsync` 的返回对象。
```csharp
public class UnifiedEmbeddingResponse
{
    // 包含所有向量化结果的列表。
    public List<EmbeddingResult> Data { get; set; }
}
```
（`EmbeddingResult` 结构省略）

---
这份经过升级的指南现在完整地展示了 v4.2.1 API 的全部功能。祝您开发愉快！
 
---

## 3. 对话历史 Payload 规范（强烈建议阅读）

本节详细说明“上游 Mod 如何携带用户与 AI 的历史对话”才算规范，确保跨供应商一致行为、正确的缓存命中与工具调用链路。

### 3.1 必填项与基本约定

- **ConversationId（必填）**
  - 对话唯一标识，必须是稳定且唯一的字符串；同一轮/同一窗口/同一会话内的所有请求均应复用同一个 `ConversationId`。
  - 缓存按会话维度隔离：缓存键包含 `conv:{sha256(conversationId)[0..15]}`，可避免不同会话间误命中。
  - 建议命名：`<存档或世界ID>:<会话主题或双方ID>:<时间片或序号>`，例如 `save42:player-123-npc-456:2025-08-17`。

- **Messages（必填）**
  - 类型：`List<ChatMessage>`。
  - 顺序：必须按时间顺序从旧到新排列（system → 历史 user/assistant 交替 → 最新 user）。
  - 允许角色：`"system" | "user" | "assistant" | "tool"`。
  - 文本：`Content` 为字符串；当 `assistant` 触发工具调用时，`Content` 可为 `null`（或空字符串）。

> 注意：`Stream` 仅影响是否流式返回，不影响缓存键；同一语义请求的流式/非流式会命中相同缓存条目。

### 3.2 ChatMessage 字段要求

- **通用字段**
  - `Role`：上述四种之一。
  - `Content`：消息文本内容（`assistant` 发起工具调用时可留空）。

- **工具相关字段**
  - `ToolCalls`（仅当 `Role == "assistant"` 且模型要发起工具调用时使用）：
    - 列表元素为 `ToolCall`，其中包含 `Id`、`Type`（通常为 `function`）与 `Function`（含 `name` 与 `arguments` JSON 字符串）。
  - `ToolCallId`（仅当 `Role == "tool"` 回传工具执行结果时使用）：
    - 必须与上一条 `assistant.tool_calls[*].id` 对应，以完成**调用-回传**配对。

### 3.3 工具定义（可选）

- 若要启用 Function Calling，请在请求体的 `Tools` 中提供可被调用的函数定义：
  - 类型为 `List<ToolDefinition>`；通常 `Type = "function"`。
  - `Function` 字段使用 JSON 对象，包含 `name`、`description`、`parameters`（JSON Schema）。
  - 框架会根据模板自动映射到供应商侧的 `tools` 字段，并在可用时默认设置 `tool_choice = "auto"`。

### 3.4 端到端生成的 Provider Payload 摘要

上游仅需构造 `UnifiedChatRequest` 与 `ChatMessage`。框架会自动翻译为供应商请求 JSON：

- 每条消息至少包含 `role` 与 `content`。
- 当 `assistant` 携带工具调用：生成 `tool_calls` 数组。
- 当 `tool` 回传结果：生成 `tool_call_id` 字段。
- 其他动态参数（model、temperature、top_p、max_tokens 等）由配置系统合并注入。

### 3.5 C# 示例：普通历史对话

```csharp
using RimAI.Framework.Contracts;

var conversationId = "save42:player-123-npc-456:2025-08-17"; // 稳定且唯一

var messages = new List<ChatMessage>
{
    new ChatMessage { Role = "system", Content = "You are a helpful assistant for RimWorld." },
    new ChatMessage { Role = "user", Content = "你好，今天有什么建议？" },
    new ChatMessage { Role = "assistant", Content = "建议先检查粮食和医疗物资储备。" },
    new ChatMessage { Role = "user", Content = "库存够吗？顺便看看电力负载。" } // 最新用户消息
};

var request = new UnifiedChatRequest {
    ConversationId = conversationId,
    Messages = messages,
    // 可选：Stream = true, ForceJsonOutput = false
};
```

### 3.6 C# 示例：带工具调用的历史

```csharp
using Newtonsoft.Json.Linq;
using RimAI.Framework.Contracts;

var tools = new List<ToolDefinition>
{
    new ToolDefinition {
        Type = "function",
        Function = JObject.FromObject(new {
            name = "check_stock",
            description = "检查指定物资的库存数量",
            parameters = new {
                type = "object",
                properties = new {
                    item = new { type = "string" }
                },
                required = new [] { "item" }
            }
        })
    }
};

var messages = new List<ChatMessage>
{
    new ChatMessage { Role = "user", Content = "请检查药草库存" },
    new ChatMessage {
        Role = "assistant",
        Content = null,
        ToolCalls = new List<ToolCall> {
            new ToolCall {
                Id = "call_1",
                Type = "function",
                Function = new ToolFunction {
                    Name = "check_stock",
                    Arguments = "{\"item\":\"herbal_medicine\"}"
                }
            }
        }
    },
    new ChatMessage {
        Role = "tool",
        ToolCallId = "call_1",
        Content = "{\"item\":\"herbal_medicine\",\"count\":87}"
    },
    new ChatMessage { Role = "user", Content = "那食物和钢铁呢？" }
};

var request = new UnifiedChatRequest {
    ConversationId = "colony-A:thread-42",
    Messages = messages,
    Tools = tools
};
```

### 3.7 最小 JSON 形态（由框架自动生成并发送）

> 以下仅用于理解框架在供应商侧构造的近似 JSON 形态；上游无需手工拼 JSON。

```json
{
  "messages": [
    { "role": "system", "content": "..." },
    { "role": "user", "content": "..." },
    { "role": "assistant", "content": "..." }
    // 如有工具：可出现 "tool_calls" 或 "tool_call_id"
  ],
  "stream": true/false,
  "tools": [ /* 可选：function 定义 */ ]
}
```

### 3.8 常见错误与修正

- **缺少 ConversationId** → 请求将被拒绝。修正：提供稳定且唯一的 `ConversationId`。
- **消息顺序倒置** → 模型上下文错乱。修正：按时间从旧到新排列。
- **工具结果缺少 `tool_call_id`** → 无法与调用配对。修正：`tool` 消息设置正确的 `ToolCallId`。
- **在非 `assistant` 消息中放置 `tool_calls`** → 无效结构。修正：仅在 `assistant` 发起工具调用时设置 `ToolCalls`。
- **多会话混用同一 `ConversationId`** → 缓存/上下文相互污染。修正：为不同会话生成不同 ID。

### 3.9 与缓存和流式的关系

- 缓存键包含会话维度与规范化消息摘要：`messages[role, content, tool_call_id, tool_calls]`、`tools`、JSON 模式标记及温度/采样/长度参数等。
- 忽略 `Stream` 标记（同一语义请求，流式/非流式命中同一条目）。
- 命中缓存时，流式请求将进行“伪流式”切片并秒回；未命中则走真实流式，聚合完成后整体写入缓存。

---

## 4. 推荐实践与注意事项

- **控制上下文长度**：长历史会消耗大量 Token。建议仅保留必要上下文，或在上游自行做裁剪。
- **工具调用回传为字符串**：`tool` 消息的 `Content` 建议回传 JSON 字符串，便于后续解析与追踪。
- **一致的 system 提示词**：在同一会话内保持 `system` 一致，有助于模型风格稳定。
- **错误处理**：所有公共方法返回 `Result<T>` 或 `IAsyncEnumerable<Result<T>>`，请务必检查 `IsSuccess` 并处理 `Error` 信息。
- **Embedding 开关建议**：
  - UI 默认 OFF，面向非向量用户降低复杂度；仅当用户显式开启且配置可用时，上游再启用 Embedding 相关路径。
  - 上游在入口处优先检查 `RimAIApi.IsEmbeddingEnabled()`，OFF 时可直接短路或隐藏入口。

---

以上规范与示例与框架源码保持一致，可作为上游 Mod 构造携带历史的标准做法。