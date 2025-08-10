# RimAI.Framework v4.2.1 - API Usage Guide

Welcome to the RimAI.Framework v4.2.1 API! This version introduces a powerful, end-to-end streaming chat API. This guide will get you up and running quickly, allowing you to integrate the framework's advanced AI capabilities into your own mods.

## 1. Quick Start Guide

This section will walk you through a **streaming chat request**, demonstrating the impressive effect of an AI generating a response word by word in real-time.

### Prerequisites

1.  **Add Reference**: In your C# project, add a reference to `RimAI.Framework.dll`.
2.  **Framework Configuration**: Ensure the end-user has configured at least one chat service provider (e.g., OpenAI) with a valid API Key in the RimWorld Mod Settings menu.

### Example: Get a Real-Time AI Response (Conversation ID required)

```csharp
using RimAI.Framework.API;
using RimAI.Framework.Contracts;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using Verse;

public class MyModFeature
{
    public async void StreamAiResponse(string question, CancellationToken cancellationToken = default)
    {
        // 1. Build the list of chat messages
        var messages = new List<ChatMessage>
        {
            new ChatMessage { Role = "system", Content = "You are a helpful assistant." },
            new ChatMessage { Role = "user", Content = question }
        };

        // 2. Construct the unified request object with a unique conversation ID
        var request = new UnifiedChatRequest {
            ConversationId = "my-mod-conv-12345-unique", // upstream must provide a stable unique ID
            Messages = messages
        };

        // 3. [New] Consume the streaming API with await foreach
        var responseBuilder = new StringBuilder();
        await foreach (var result in RimAIApi.StreamCompletionAsync(request, cancellationToken))
        {
            if (result.IsSuccess)
            {
                var chunk = result.Value;

                // Append the received text chunk in real-time
                if (chunk.ContentDelta != null)
                {
                    responseBuilder.Append(chunk.ContentDelta);
                    // Here, you can update your UI control with the in-progress text
                    // Log.Message($"Live content: {responseBuilder.ToString()}"); 
                }

                // When the stream ends
                if (chunk.FinishReason != null)
                {
                    Log.Message($"[MyMod] Stream finished. Reason: {chunk.FinishReason}");
                    if (chunk.ToolCalls != null)
                    {
                        Log.Message($"[MyMod] Model requested tool call: {chunk.ToolCalls.First().Function.Name}");
                    }
                }
            }
            else
            {
                // Errors at any stage of the stream are caught here
                Log.Error($"[MyMod] AI Stream Failed: {result.Error}");
                break; // Stop the loop on error
            }
        }
        
        Log.Message($"[MyMod] Final complete response: {responseBuilder.ToString()}");
    }
}
```
By calling `StreamAiResponse("Tell me a short joke about a robot")`, you will see the AI's reply being built word by word in the logs, instead of waiting several seconds for the full answer.

---

## 2. Comprehensive Guide

### The `Contracts` Assembly

The `RimAI.Framework.Contracts` project serves as the **Stable Data Contract Layer** for the framework, providing:

*   **Unified Model Definitions**: `UnifiedChatRequest`, `UnifiedEmbeddingRequest`, `ChatMessage`, etc.
*   **Error Handling Base**: The `Result<T>` class, ensuring callers explicitly handle failures.
*   **Function Calling Data Models**: `ToolDefinition`, `ToolCall`, etc.

**Location**: `Rimworld_AI_Framework/RimAI.Framework.Contracts`

**Usage**:
```csharp
using RimAI.Framework.Contracts;
```

> Before reading the examples below, it is recommended to explore the `Contracts/Models` directory to understand the core DTOs. This will make the example code much clearer.

### Core Design Philosophy

*   **Static API Entry Point**: All functionality is accessed through the static class `RimAI.Framework.API.RimAIApi`.
*   **Unified Data Models**: You will only ever interact with our unified request/response models, regardless of the backend provider.
*   **Robust `Result<T>` Pattern**: All methods return `Result<T>` or `IAsyncEnumerable<Result<T>>`. You **must** check the `IsSuccess` property to determine if an operation succeeded.

### Public API (`RimAIApi`)

#### Chat Completions

**1. [New/Required] Core Streaming Method: `StreamCompletionAsync` (requires `ConversationId`)**
This is the recommended way to get real-time, token-by-token responses.

```csharp
public static IAsyncEnumerable<Result<UnifiedChatChunk>> StreamCompletionAsync(
    UnifiedChatRequest request, 
    CancellationToken cancellationToken = default
);
```
*   **Returns**: An asynchronous stream of `UnifiedChatChunk`. You must consume it using `await foreach`. Each element in the stream is a `Result` object, and you need to check its `IsSuccess` status.

**2. Core Non-Streaming Method: `GetCompletionAsync` (requires `ConversationId`)**
Use this to get the complete AI response in a single call.

```csharp
public static Task<Result<UnifiedChatResponse>> GetCompletionAsync(
    UnifiedChatRequest request, 
    CancellationToken cancellationToken = default
);
```

**3. Tool-Calling Helper Method: `GetCompletionWithToolsAsync` (requires `ConversationId`)**
A convenience method designed to simplify non-streaming tool calls. A new `conversationId` parameter is introduced.

```csharp
public static Task<Result<UnifiedChatResponse>> GetCompletionWithToolsAsync(
    List<ChatMessage> messages,
    List<ToolDefinition> tools,
    string conversationId,
    CancellationToken cancellationToken = default
);
```

**4. Batch Processing Method: `GetCompletionsAsync`**
Use this to send multiple independent chat requests concurrently and wait for all of them to complete.

```csharp
public static Task<List<Result<UnifiedChatResponse>>> GetCompletionsAsync(
    List<UnifiedChatRequest> requests, 
    CancellationToken cancellationToken = default
);
```

#### Text Embeddings

**`GetEmbeddingsAsync`** (Non-streaming)
```csharp
public static Task<Result<UnifiedEmbeddingResponse>> GetEmbeddingsAsync(
    UnifiedEmbeddingRequest request, 
    CancellationToken cancellationToken = default
);
```

### Caching and Pseudo-Streaming Behavior (Conversation-scoped)

- Unified cache (Framework layer):
  - Chat and Embedding use a short default TTL (120s by default); failures are not cached.
  - Chat key adds a “conversation scope”: `chat:{provider}:{model}:conv:{sha256(conversationId)[0..15]}:{payloadHash}`. The `payloadHash` is based on the canonical request summary (ignores the `Stream` flag).
  - Embedding caches per input text (key = provider + model + sha256(text)).
- Cache-hit pseudo-stream:
  - If a streaming request hits the cache, the Framework slices the cached full reply into `UnifiedChatChunk`s and immediately emits them as a pseudo-stream. The last chunk carries `FinishReason` and possibly `ToolCalls`.
  - On miss, true streaming occurs; mid-stream chunks are not cached. After successful final aggregation, the full response is cached.
- Configuration: TTL and a cache toggle will be exposed in a future version; the current version enables a short TTL by default.

### Key Request Models

(This section is unchanged from previous versions and is omitted for brevity)
*   `UnifiedChatRequest` (new required field: `ConversationId`)

### Cache Invalidation

To clear cache entries for a single dialogue under the current active Provider/Model:

```csharp
public static Task<Result<bool>> InvalidateConversationCacheAsync(
    string conversationId,
    CancellationToken cancellationToken = default
);
```
*   `ChatMessage`
*   `ToolDefinition` & `ToolCall`
*   ...

### Key Response Models

#### [New] `UnifiedChatChunk` (Streaming)
This is the fundamental data unit in the stream returned by `StreamCompletionAsync`.

```csharp
public class UnifiedChatChunk
{
    // The incremental text content in this chunk. Usually null or a single token.
    public string ContentDelta { get; set; }

    // If the stream is ending, this contains the final finish reason.
    // Only valid in the last chunk of the stream.
    public string FinishReason { get; set; }

    // If the model requests a tool call, this will contain the complete
    // tool call information. Typically returned in a single chunk at the end
    // of the stream when FinishReason is "tool_calls".
    public List<ToolCall> ToolCalls { get; set; }
}
```

#### `UnifiedChatResponse` (Non-streaming)
The return object for non-streaming methods like `GetCompletionAsync`.

```csharp
public class UnifiedChatResponse
{
    // Finish reason: "stop", "length", "tool_calls"
    public string FinishReason { get; set; }

    // The complete response message generated by the model.
    public ChatMessage Message { get; set; }
}
```

#### `UnifiedEmbeddingResponse` (Non-streaming)
The return object for `GetEmbeddingsAsync`.
```csharp
public class UnifiedEmbeddingResponse
{
    // A list containing all the embedding vector results.
    public List<EmbeddingResult> Data { get; set; }
}
```
(`EmbeddingResult` structure omitted)

---
This updated guide now fully covers the capabilities of the v4.2.1 API. Happy coding!

