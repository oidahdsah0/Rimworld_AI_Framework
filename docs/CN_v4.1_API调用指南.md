# RimAI.Framework v4.2.1 - API 调用指南

欢迎使用 RimAI.Framework v4.2.1 API！本次更新带来了强大的**端到端流式聊天 API**。本指南将帮助您快速上手，并将框架的强大 AI 功能集成到您自己的 Mod 中。

## 1. 快速上手指南 (Quick Start)

本节将引导您完成一次**流式**聊天请求，让您在几分钟内看到 AI 逐字生成回复的酷炫效果。

### 前提条件

1.  **添加引用**: 在您的 C# 项目中，添加对 `RimAI.Framework.dll` 的引用。
2.  **框架配置**: 确保最终用户已经在 RimWorld 的 Mod 设置菜单中，至少配置好了一个聊天服务提供商（例如 OpenAI），并填入了有效的 API Key。

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
