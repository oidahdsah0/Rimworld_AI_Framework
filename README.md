![GitHub Preview](docs/preview/GithubPreview.png)

# 🤖 RimAI Framework 🏛️

[🇺🇸 English](README.md) | [🇨🇳 简体中文](README_zh-CN.md) | [📚 Docs](docs/)

[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)
[![RimWorld](https://img.shields.io/badge/RimWorld-1.5%20%7C%201.6-brightgreen.svg)](https://rimworldgame.com/)
[![.NET Framework](https://img.shields.io/badge/.NET%20Framework-4.7.2-blue.svg)](https://dotnet.microsoft.com/download/dotnet-framework)
[![Steam Workshop](https://img.shields.io/badge/Steam-Workshop-blue.svg)](https://steamcommunity.com/sharedfiles/filedetails/?id=3529263357)
[![Status](https://img.shields.io/badge/Status-v4.2.1-orange.svg)](https://steamcommunity.com/sharedfiles/filedetails/?id=3529186453)

> **🚀 A revolutionary AI-powered framework for RimWorld that brings Large Language Models directly into your colony management experience, enabling intelligent, context-aware administrative decisions! 🎮✨**

**🎉 NOW AVAILABLE ON STEAM WORKSHOP!** 🎉  
**[📥 Download RimAI Framework](https://steamcommunity.com/sharedfiles/filedetails/?id=3529263357)**

**👨‍💻 Author**: [@oidahdsah0](https://github.com/oidahdsah0)  
**📅 Created**: 15 July 2025  
**🚀 Updated**: 06 Aug 2025  
**🔄 Latest**: v4.2.1 - Unified Architecture (Conversation-Scoped Cache)
**🧠 Build with**: This project was built entirely using prompts.

---

## 🎯 **Core Philosophy**

**RimAI.Framework** is a provider-agnostic, data-driven backend infrastructure for interacting with Large Language Models (LLMs) and Embedding APIs. It is designed to be highly flexible, extensible, and performant.

*   **Provider Template System**: Connect to any AI service (OpenAI, Ollama, Groq, etc.) using external `provider_template_*.json` files. These templates define the complete API contract, enabling zero-code adaptation for new providers.
*   **Unified Internal Models**: All external requests and responses are translated into unified internal objects (`UnifiedChatRequest`, `UnifiedEmbeddingResponse`, etc.), decoupling high-level logic from underlying API specifics.
*   **Clear, Layered Architecture**: A strict separation of concerns into an API facade, core coordinators (for Chat and Embedding), configuration management, request/response translation, and HTTP execution.
*   **Comprehensive Feature Support**: Native support for streaming/non-streaming chat, JSON mode, function calling, and text embedding.
*   **Intelligent Batching**: Automatic, provider-aware batching for embeddings and concurrency-limited batching for chat to maximize throughput.

## ⚡ **v4.2.1 Key Features** 🌟
- **🔌 Data-Driven**: Connect to any API via JSON templates.
- **🔄 End-to-End Streaming**: **Enhanced in v4.2.1!** A fully-featured streaming API for real-time, word-by-word responses.
- **✨ Embedding API**: First-class support for text embeddings.
- **📊 Advanced Batching**: Optimized for chat and embeddings.
- **🛡️ Robust & Safe**: Type-safe results with the `Result<T>` pattern.
- **🪶 Lightweight**: No external dependencies beyond the base game and Newtonsoft.Json. **Does not require Harmony**. 🚀

## 🔧 **Installation & Setup** 📦

### 📋 Prerequisites
- 🎮 RimWorld 1.5+

### 💾 Installation

#### 🎮 **For Players (Recommended)**
1. **📥 Steam Workshop**: [Subscribe to RimAI Framework](https://steamcommunity.com/sharedfiles/filedetails/?id=3529263357)
2. **🔧 Enable Mod**: Launch RimWorld and enable "RimAI Framework" in the mod list.
3. **⚙️ Configure**: Follow the configuration steps below to set up your API credentials in Mod Options.

#### 👨‍💻 **For Developers**
1. **📂 Manual Install**: Download from [GitHub Releases](https://github.com/oidahdsah0/Rimworld_AI_Framework/releases).
2. **🔨 Build from Source**: Clone the repository and build it locally.

### ⚙️ **Configuration (CRITICAL STEP)**
1. 🎮 Open RimWorld > Options > Mod Settings > RimAI Framework.
2. **🤖 Provider Selection**: Use the dropdown to select a service provider (e.g., OpenAI, Ollama).
3. **🔑 API Credentials**:
   - **API Key**: Your API key. (Leave blank for local providers like Ollama).
   - **Endpoint URL**: The base URL for the API. Defaults are provided.
   - **Model**: The specific model to use (e.g., `gpt-4o-mini`, `llama3`).
4. **✅ Test & Save**: Use the "Test" button to verify your connection, then click "Save".

## 📚 **v4.2.1 API Usage Guide** 💻

The v4.1.2 API is streamlined, powerful, and introduces a first-class streaming experience.

### 1. (NEW) Streaming Chat Response
Use `await foreach` to consume the real-time stream of text chunks. This is the recommended approach for interactive experiences.

```csharp
using RimAI.Framework.API;
using RimAI.Framework.Contracts;
using System.Collections.Generic;
using System.Text;
using Verse;

// 1. Build your request
var request = new UnifiedChatRequest
{
    Messages = new List<ChatMessage>
    {
        new ChatMessage { Role = "system", Content = "You are a helpful assistant." },
        new ChatMessage { Role = "user", Content = "Tell me a short joke about robots." }
    }
};

// 2. Consume the stream with await foreach
var responseBuilder = new StringBuilder();
await foreach (var result in RimAIApi.StreamCompletionAsync(request))
{
    if (result.IsSuccess)
    {
        var chunk = result.Value;
        if (chunk.ContentDelta != null)
        {
            // Append the text chunk in real-time
            responseBuilder.Append(chunk.ContentDelta);
            // Update your UI here
        }
        if (chunk.FinishReason != null)
        {
            Log.Message($"Stream finished. Reason: {chunk.FinishReason}");
        }
    }
    else
    {
        Log.Error($"AI Stream Failed: {result.Error}");
        break; // Stop on error
    }
}

Log.Message($"Final assembled response: {responseBuilder.ToString()}");
```

### 2. Non-Streaming Chat Completion
For background tasks where you need the full response at once.

```csharp
using RimAI.Framework.API;
using RimAI.Framework.Contracts;
using System.Threading.Tasks;

var request = new UnifiedChatRequest { /* ... */ };
Result<UnifiedChatResponse> response = await RimAIApi.GetCompletionAsync(request);

if (response.IsSuccess)
{
    Log.Message($"AI Response: {response.Value.Message.Content}");
}
else
{
    Log.Error($"AI Error: {response.Error}");
}
```

### 3. Text Embedding (Batch)
Convert multiple texts into vector embeddings efficiently. The framework handles batching automatically based on provider limits.

```csharp
using RimAI.Framework.API;
using RimAI.Framework.Contracts;
using System.Collections.Generic;
using System.Threading.Tasks;

var request = new UnifiedEmbeddingRequest
{
    Input = new List<string>
    {
        "Colonist idle.",
        "A raid is approaching from the north.",
        "The food supply is critically low."
    }
};

Result<UnifiedEmbeddingResponse> embeddingsResult = await RimAIApi.GetEmbeddingsAsync(request);

if (embeddingsResult.IsSuccess)
{
    foreach (var embedding in embeddingsResult.Value.Data)
    {
        // Use the vector for semantic search, etc.
        Log.Message($"Got embedding with {embedding.Embedding.Count} dimensions at index {embedding.Index}");
    }
}
```

## 🌍 **Supported Languages** 🗣️

The framework includes full localization support for:
- 🇺🇸 English
- 🇨🇳 简体中文 (Simplified Chinese)
- 🇯🇵 日本語 (Japanese)
- 🇰🇷 한국어 (Korean)
- 🇫🇷 Français (French)
- 🇩🇪 Deutsch (German)
- 🇷🇺 Русский (Russian)

## 🤝 **Contributing** 👥

This is an open-source project and contributions are welcome! 🎉 Please see our [Contributing Guide](CONTRIBUTING.md) for details.

### 📚 Architecture Documentation
- 🏛️ **[V4.2.1 Architecture Design](docs/EN_ARCHITECTURE_V4.md)**: A deep dive into the data-driven architecture.
- 🇨🇳 **[v4.2.1 API Guide (Chinese)](docs/CN_v4.1_API调用指南.md)**: Detailed guide for the latest API.

## 📄 **License** ⚖️

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.
