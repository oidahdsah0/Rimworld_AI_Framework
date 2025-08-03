![GitHub Preview](docs/preview/GithubPreview.png)

# 🤖 RimAI Framework 🏛️

[🇺🇸 English](README.md) | [🇨🇳 简体中文](README_zh-CN.md) | [📚 文档](docs/)

[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)
[![RimWorld](https://img.shields.io/badge/RimWorld-1.6-brightgreen.svg)](https://rimworldgame.com/)
[![.NET Framework](https://img.shields.io/badge/.NET%20Framework-4.7.2-blue.svg)](https://dotnet.microsoft.com/download/dotnet-framework)
[![Steam Workshop](https://img.shields.io/badge/Steam-Workshop-blue.svg)](https://steamcommunity.com/sharedfiles/filedetails/?id=3529263357)
[![Status](https://img.shields.io/badge/Status-v4.0%20Beta-orange.svg)](https://steamcommunity.com/sharedfiles/filedetails/?id=3529186453)

> **🚀 A revolutionary AI-powered framework for RimWorld that brings Large Language Models directly into your colony management experience, enabling intelligent, context-aware administrative decisions! 🎮✨**

**🎉 NOW AVAILABLE ON STEAM WORKSHOP!** 🎉  
**[📥 Download RimAI Framework](https://steamcommunity.com/sharedfiles/filedetails/?id=3529263357)**

**👨‍💻 Author**: [@oidahdsah0](https://github.com/oidahdsah0)  
**📅 Created**: 15 July 2025  
**🚀 Released**: 19 July 2025  
**🔄 Latest**: v4.0 Beta - Unified Architecture

---

## 🚀 **Vision: Intelligent Colony Management** 🧠

RimAI Framework introduces a paradigm shift in colony management through **"frictionless interaction"** 🌊 - eliminating the need for colonists to gather at specific locations or interrupt their work for administrative tasks. Management is exercised through a centralized **"AI Administrative Terminal"** 🖥️ using asynchronous, intelligent command processing.

Your decisions become "smart directives" ⚡ that seamlessly influence the world without disrupting colony productivity. Consequences manifest through colonist thoughts 💭, dialogue bubbles 💬, social logs 📋, and global messages 📢, creating an intelligent and supremely efficient governance experience powered by advanced AI analysis! 🎯

## 📐 **Three-Tier Architecture** 🏗️

To create a clear and extensible ecosystem, the RimAI project is organized into three distinct layers:

### 1. **🔧 Framework Layer** (This Repository) ✅
- **🎯 Purpose**: A provider-agnostic technical backend for all AI communication.
- **📋 Responsibilities**:
  - 🔌 **v4.0 NEW**: **Data-Driven Provider System**: Connect to any LLM/Embedding API (OpenAI, Ollama, Groq, etc.) via external JSON templates. No code changes needed.
  - 🌐 Unified network communication, request building, response parsing, and error handling. ✅
  - ⚡ Asynchronous processing with robust concurrency control. ✅
  - ✨ **v4.0 NEW**: **First-Class Embedding Support**: Fully integrated, high-performance API for text embeddings. ✅
  - 📊 **v4.0 NEW**: **Advanced Batching**: Native batching for embeddings and concurrent requests for chat to maximize throughput. ✅
  - 🔄 Streaming responses for real-time interaction. ✅
  - 🧠 Intelligent, configurable caching for performance and cost-saving. ✅
  - 📚 RAG (Retrieval-Augmented Generation) knowledge base integration 🚧
- **🎮 Goal**: Absolutely neutral, stable, and efficient. Contains no gameplay logic. ✅

### 2. **⚔️ Core Gameplay Modules** (Future Repositories) 🚧
- **🎯 Purpose**: Official content packages that define core game experiences
- **📋 Responsibilities**:
  - Concrete game systems like "Judicial System" ⚖️ and "Colony Chronicles" 📖
  - Specific case types, AI tools, and game event listeners via XML and C#
  - UI population for player interaction interfaces 🖱️
- **🎮 Analogy**: Like RimWorld's "Core" content, with potential for independent "DLC" modules

### 3. **🎭 AI Storyteller** (Future Integration) 🚧
- **🎯 Purpose**: Intelligent narrative director powered by advanced AI analysis
- **📋 Responsibilities**:
  - Standard RimWorld `StorytellerDef` implementation with AI enhancements 🤖
  - Continuous AI-driven analysis of colony state and player behavior 📊
  - Dynamic event generation based on intelligent pattern recognition 🎲
- **🎮 Goal**: Truly adaptive, AI-powered narrative experiences that evolve with your colony! 🌟

## 🎯 **Core Features** 🌟

### 🖥️ AI Administrative Terminal
- **🏛️ Single Point of Intelligence**: One buildable core structure unlocks the entire RimAI system
- **🎛️ Integrated UI**: Multi-tabbed terminal interface for different administrative tasks:
  - **📁 Case Files**: Handle criminal cases and civil disputes in dossier format
  - **📜 Administrative Codex**: Issue global administrative decrees and permanent laws
  - **👥 Advisory Cabinet**: Appoint and manage your AI-powered officials
  - **📚 Colony Archives**: Access historical records and statistics
  - **🤖 W.I.F.E. System**: Warden's Integrated Foresight Engine - your AI advisory council

### ⚖️ Intelligent Governance
- **📋 Case Logging**: Crimes and disputes automatically create timestamped cases with AI-analyzed deadlines ⏰
- **🔍 Smart Dossier Review**: Handle cases at your convenience through AI-enhanced interfaces
- **🧠 Intelligent Judgment**: Issue decisions with AI-powered reasoning and context analysis
- **🤖 AI Magistrate Delegation**: Unhandled cases are automatically processed by AI-appointed officials
- **⚡ Intelligent Consequences**: Results propagate through AI-analyzed thoughts, social changes, and ambient reactions

### 👨‍💼 AI Officer System
- **⚖️ AI Magistrate**: Handles overdue cases with intelligent analysis and reports back through letters 📝
- **🏛️ AI Steward**: Provides administrative suggestions based on intelligent colony state analysis
- **🎤 AI Speaker**: Proposes legislative improvements based on AI-driven event analysis

## 🛠️ **Technical Implementation** ⚙️

### 🔧 Core Technologies & Design
- **🪶 Lightweight**: No external dependencies beyond the base game and Newtonsoft.Json. **Does not require Harmony**. 🚀
- **🔌 Data-Driven**: API behavior is defined by external `provider_template_*.json` files, not hard-coded.
- **🧱 Decoupled Architecture**: Clear separation of concerns between API Facade, Coordinators (Chat/Embedding), Translators, and Execution.
- **⚙️ ModSettings**: A robust UI for managing multiple provider profiles and their settings.
- **🛡️ `Result<T>` Pattern**: For robust, predictable, and exception-safe error handling across the framework.

### 🗂️ Key V4 Components
- 🤖 `RimAIApi`: The clean, static entry point for all external mods.
- ⚙️ `SettingsManager`: Loads, validates, and merges provider templates with user configurations.
- 🧠 `ChatManager` / `EmbeddingManager`: Central coordinators for their respective functionalities.
- 🔄 `Request/Response Translators`: Translate between the unified internal models and provider-specific JSON structures based on template rules.
- 📡 `HttpExecutor`: The single point for handling all outgoing HTTP requests, with built-in retry logic.

### ⚡ **v4.0 Key Features** 🌟
- **🔌 Data-Driven**: Connect to any API via JSON templates.
- **✨ Embedding API**: First-class support for text embeddings.
- **📊 Advanced Batching**: Optimized for chat and embeddings.
- **🔄 Streaming Responses**: For real-time interaction.
- **🧠 Smart Caching**: Reduces cost and latency.
- **🛡️ Robust & Safe**: Type-safe results with the `Result<T>` pattern.

## 🔧 **Installation & Setup** 📦

### 📋 Prerequisites
- 🎮 RimWorld 1.6+

### 💾 Installation

#### 🎮 **For Players (Recommended)**
1. **📥 Steam Workshop**: [Subscribe to RimAI Framework](https://steamcommunity.com/sharedfiles/filedetails/?id=3529263357)
2. **🔧 Enable Mod**: Launch RimWorld and enable "RimAI Framework" in the mod list
3. **⚙️ Configure**: Set up your API credentials in Mod Options

#### 👨‍💻 **For Developers**
1. **📂 Manual Install**: Download from [GitHub Releases](https://github.com/oidahdsah0/Rim_AI_Framework/releases)
2. **🔨 Build from Source**: Clone and build locally (see Development Setup below)
3. **⚙️ Configure**: Set up your development environment and API settings

### ⚙️ Configuration
1. 🎮 Open RimWorld > Options > Mod Settings > RimAI Framework.
2. **🤖 Provider Selection**: Use the dropdown to select a service provider (e.g., OpenAI, Ollama). The settings below will adapt to the selected provider.
3. **🔑 API Credentials**:
   - **API Key**: Your API key for the selected service. (May be left blank for local providers like Ollama).
   - **Endpoint URL**: The base URL for the API. Defaults are provided.
   - **Model**: The specific model you wish to use (e.g., `gpt-4o-mini`, `llama3.2`).
4. **⚙️ Advanced Settings (Optional)**:
    - Fine-tune parameters like `Temperature` and `Concurrency Limit`.
    - Add custom HTTP headers or override static request parameters via JSON fields.
5. **✅ Test & Save**: Use the "Test" button to verify your connection, then "Save".

## 📚 **v4.0 API Usage Examples** 💻

The v4 API is streamlined and powerful. Configuration is handled in the Mod Settings, not in the code.

### Simple Chat Completion
```csharp
using RimAI.Framework.API;
using RimAI.Framework.Shared.Models; // For Result<T>
using System.Threading;

CancellationToken cancellationToken = default;
Result<string> response = await RimAIApi.GetCompletionAsync(
    "Analyze the current state of the colony and provide a brief summary.",
    cancellationToken
);

if (response.IsSuccess)
{
    Log.Message($"AI Response: {response.Value}");
}
else
{
    Log.Error($"AI Error: {response.Error}");
}
```

### Streaming Chat Response
```csharp
// Get a stream of response chunks for real-time UI updates
var stream = RimAIApi.GetCompletionStreamAsync("Generate a detailed event description.", cancellationToken);

await foreach (var chunkResult in stream)
{
    if (chunkResult.IsSuccess)
    {
        UpdateMyUI(chunkResult.Value);
    }
    else
    {
        Log.Error($"Stream Error: {chunkResult.Error}");
        break;
    }
}
```

### Text Embedding (Batch)
```csharp
using System.Collections.Generic;

// Convert multiple texts into vector embeddings efficiently
// The framework handles batching automatically based on provider limits.
var textsToEmbed = new List<string>
{
    "Colonist idle.",
    "A raid is approaching from the north.",
    "The food supply is critically low."
};

Result<List<float[]>> embeddingsResult = await RimAIApi.GetEmbeddingsAsync(textsToEmbed, cancellationToken);

if (embeddingsResult.IsSuccess)
{
    foreach (var vector in embeddingsResult.Value)
    {
        // Use the vector for semantic search, etc.
        Log.Message($"Got embedding of dimension: {vector.Length}");
    }
}
```

### Forced JSON Output
```csharp
// When the selected provider's template supports it, you can force JSON output.
// The prompt should instruct the model to return JSON.
string jsonPrompt = "Return the colony's resource levels (food, medicine, components) as a JSON object.";

// Simply set the `forceJson` flag to true.
Result<string> jsonResponse = await RimAIApi.GetCompletionAsync(jsonPrompt, cancellationToken, forceJson: true);

if (jsonResponse.IsSuccess)
{
    // jsonResponse.Value will be a JSON string
    var stats = Newtonsoft.Json.JsonConvert.DeserializeObject<ColonyStats>(jsonResponse.Value);
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

### 👨‍💻 Development Setup
1. **📂 Clone the repository**
   ```bash
   git clone https://github.com/oidahdsah0/Rim_AI_Framework.git
   cd Rim_AI_Framework
   ```

2. **🔨 Build the project**
   ```bash
   # Navigate to the framework directory
   cd RimAI.Framework
   
   # Build using dotnet CLI (cross-platform)
   dotnet build
   
   # Or using MSBuild on Windows
   msbuild Rim_AI_Framework.sln /p:Configuration=Debug
   ```

3. **📋 Development Requirements**
   - 🛠️ .NET Framework 4.7.2 SDK
   - 💻 Visual Studio 2019+ or VS Code with C# Dev Kit
   - 🎮 RimWorld 1.6+ (for testing)

4. **🍎 macOS Build Notes**
   - Use `dotnet build` command (MSBuild not available on macOS)
   - Project automatically detects macOS RimWorld installation path 🎯
   - Requires Mono runtime (usually installed with .NET SDK)
   - PostBuild event deploys directly to RimWorld Mods folder 📂

### 🏗️ Repository Structure
- **📝 Source Code Only**: This repository contains only source code
- **🔨 Build Locally**: Developers should build from source
- **✨ Clean Git**: No compiled binaries are committed to the repository
- **📦 Releases**: Pre-compiled mods are available in GitHub Releases

### 📚 Architecture Documentation
- 🏛️ **[V4 Architecture Design](docs/ARCHITECTURE_V4.md)**: A deep dive into the new data-driven architecture.
- 📋 **[V4 Implementation Plan](docs/V4_IMPLEMENTATION_PLAN.md)**: The step-by-step development checklist.
- 📄 **[V4 Template Design](docs/TEMPLATE_DESIGN.md)**: The specification for creating your own provider templates.

## 📄 **License** ⚖️

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## 🙏 **Acknowledgments** ❤️

- 🎮 RimWorld community for inspiration and support
- 👥 All contributors and early adopters

---

## ⚙️ **Essential Setup Guide** 🔧

**⚠️ CRITICAL: You MUST configure the mod settings before any RimAI module will work!**

### 📋 **Step-by-Step Configuration**

1. **Install and Enable**
   - Subscribe to RimAI Framework on Steam Workshop
   - Enable the mod in your mod list and restart RimWorld

2. **Access Mod Settings**
   - Go to **Settings → Mod Settings → RimAI Framework**
   - You'll see the configuration panel with several fields to fill

3. **Configure Required Fields**

   **🔐 API Key** (Required for cloud services):
   - **OpenAI**: Get from https://platform.openai.com/api-keys
   - **Ollama (Local), vLLM**: Leave empty - no key needed
   - Copy the key exactly as provided by your service

   **🌐 Endpoint URL** (Required):
   ```
   OpenAI users: https://api.openai.com/v1 (Deepseek, Siliconflow settings are similar)
   Local Ollama: http://localhost:11434/v1
   Other services: Check your provider's documentation
   ```

   **🤖 Model Name** (Required):
   ```
   OpenAI:     gpt-4o-mini, gpt-4o, gpt-3.5-turbo
   Ollama:     llama3.2:3b, qwen2.5:7b, mistral:7b (your installed model)
   ```

   **🔄 Enable Streaming** (Optional):
   - ✅ **Recommended**: Check for real-time responses
   - ❌ Uncheck for single complete responses

4. **Test and Save**
   - Use the **Test Connection** button to verify your settings
   - Click **Save** to apply your configuration
   - You're ready to use RimAI modules!

### 💡 **Recommended Setups for Beginners**

**🆓 Free Option (Local AI)**:
- Install Ollama on your computer
- Download `llama3.2:3b` model
- URL: `http://localhost:11434/v1`
- API Key: (leave empty)
- Model: `llama3.2:3b`

**💰 Budget Option (Cloud AI)**:
- Sign up for OpenAI
- URL: `https://api.openai.com/v1`
- Get API key from OpenAI dashboard
- Model: `gpt-4o-mini` (very affordable: ~$0.15 per 1M tokens)

**⭐ Premium Option**:
- Use `gpt-4o` for best results, or try Deepseek, Siliconflow for cost-effective alternatives

---

**⚠️ Disclaimer**: This is a framework-level mod that requires additional content modules for full functionality. The core gameplay features (Judicial System, Colony Chronicles, etc.) will be released as separate modules. 🚧

**🔗 Links**:
- 🎮 **[Steam Workshop - LIVE NOW!](https://steamcommunity.com/sharedfiles/filedetails/?id=3529263357)** ⭐
- 💬 [Discord Server](https://discord.gg/TBD) (coming soon)
- 🐛 [Bug Reports & Issues](https://github.com/oidahdsah0/Rim_AI_Framework/issues)
- 📖 [GitHub Repository](https://github.com/oidahdsah0/Rim_AI_Framework)
- 📋 [Changelog](https://github.com/oidahdsah0/Rim_AI_Framework/releases)
