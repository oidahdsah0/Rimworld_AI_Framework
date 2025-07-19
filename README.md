# 🤖 RimAI Framework 🏛️

[🇺🇸 English](README.md) | [🇨🇳 简体中文](README_zh-CN.md) | [📚 文档](docs/)

[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)
[![RimWorld](https://img.shields.io/badge/RimWorld-1.6-brightgreen.svg)](https://rimworldgame.com/)
[![.NET Framework](https://img.shields.io/badge/.NET%20Framework-4.7.2-blue.svg)](https://dotnet.microsoft.com/download/dotnet-framework)
[![Steam Workshop](https://img.shields.io/badge/Steam-Workshop-blue.svg)](https://steamcommunity.com/sharedfiles/filedetails/?id=3529186453)
[![Status](https://img.shields.io/badge/Status-Released-success.svg)](https://steamcommunity.com/sharedfiles/filedetails/?id=3529186453)

> **🚀 A revolutionary AI-powered framework for RimWorld that brings Large Language Models directly into your colony management experience, enabling intelligent, context-aware administrative decisions! 🎮✨**

**🎉 NOW AVAILABLE ON STEAM WORKSHOP!** 🎉  
**[� Download RimAI Framework](https://steamcommunity.com/sharedfiles/filedetails/?id=3529238337)**

**�👨‍💻 Author**: [@oidahdsah0](https://github.com/oidahdsah0)  
**📅 Created**: 15 July 2025  
**🚀 Released**: 19 July 2025

---

## 🚀 **Vision: Intelligent Colony Management** 🧠

RimAI Framework introduces a paradigm shift in colony management through **"frictionless interaction"** 🌊 - eliminating the need for colonists to gather at specific locations or interrupt their work for administrative tasks. Management is exercised through a centralized **"AI Administrative Terminal"** 🖥️ using asynchronous, intelligent command processing.

Your decisions become "smart directives" ⚡ that seamlessly influence the world without disrupting colony productivity. Consequences manifest through colonist thoughts 💭, dialogue bubbles 💬, social logs 📋, and global messages 📢, creating an intelligent and supremely efficient governance experience powered by advanced AI analysis! 🎯

## 📐 **Three-Tier Architecture** 🏗️

To create a clear and extensible ecosystem, the RimAI project is organized into three distinct layers:

### 1. **🔧 Framework Layer** (This Repository) ✅
- **🎯 Purpose**: Pure technical backend and communication layer
- **📋 Responsibilities**:
  - All Large Language Model (LLM) network communication ✅
  - API key management, request building, response parsing, and error handling ✅
  - ⚡ Asynchronous processing and concurrency control for API requests ✅
  - 🔍 Embedding system for semantic search and context understanding 🚧
  - 📚 RAG (Retrieval-Augmented Generation) knowledge base integration 🚧
  - 🌳 JSON tree hierarchical structure RAG library support 🚧
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

### 🔧 Core Technologies
- **🪶 Lightweight**: No external dependencies beyond the base game and Newtonsoft.Json. **Does not require Harmony**. 🚀
- **🧩 ThingComp**: Component system for object-specific data and behavior
- **🌐 GameComponent**: Global data management and persistent storage
- **📝 Custom Defs**: New XML-definable concepts (`ToolDef`, `CaseDef`)
- **⚙️ ModSettings**: Player-configurable options and API management

### 🗂️ Key Classes
- 🤖 `LLMManager`: Singleton for all AI communication and intelligent response processing
- ⚙️ `RimAISettings`: Configuration management and AI model persistence
- 🧠 `ContextManager`: Intelligent game state analysis and context building for AI
- 📚 `CoreDefs`: Framework-level definitions and AI-powered data structures

## 🔧 **Installation & Setup** 📦

### 📋 Prerequisites
- 🎮 RimWorld 1.6+

### 💾 Installation

#### 🎮 **For Players (Recommended)**
1. **� Steam Workshop**: [Subscribe to RimAI Framework](https://steamcommunity.com/sharedfiles/filedetails/?id=3529238337)
2. **🔧 Enable Mod**: Launch RimWorld and enable "RimAI Framework" in the mod list
3. **⚙️ Configure**: Set up your API credentials in Mod Options

#### 👨‍💻 **For Developers**
1. **📂 Manual Install**: Download from [GitHub Releases](https://github.com/oidahdsah0/Rim_AI_Framework/releases)
2. **� Build from Source**: Clone and build locally (see Development Setup below)
3. **⚙️ Configure**: Set up your development environment and API settings

### ⚙️ Configuration
1. 🎮 Open RimWorld > Options > Mod Settings > RimAI Framework
2. 🔑 Enter your LLM API credentials:
   - **🔐 API Key**: Your OpenAI/Claude/local model API key
   - **🌐 Endpoint URL**: Service endpoint (defaults to OpenAI)
   - **🤖 Model Name**: Specific model to use (e.g., `gpt-4o`)
3. 🔍 Configure optional embedding settings for enhanced context

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
- 🏗️ [Technical Design](docs/TECHNICAL_DESIGN.md)
- 📖 [API Reference](docs/API_REFERENCE.md)
- 📋 [Implementation Guide](docs/IMPLEMENTATION_GUIDE.md)

## 📄 **License** ⚖️

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## 🙏 **Acknowledgments** ❤️

- 🎮 RimWorld community for inspiration and support
- 👥 All contributors and early adopters

---

**⚠️ Disclaimer**: This is a framework-level mod that requires additional content modules for full functionality. The core gameplay features (Judicial System, Colony Chronicles, etc.) will be released as separate modules. 🚧

**🔗 Links**:
- 🎮 **[Steam Workshop - LIVE NOW!](https://steamcommunity.com/sharedfiles/filedetails/?id=3529238337)** ⭐
- 💬 [Discord Server](https://discord.gg/TBD) (coming soon)
- 🐛 [Bug Reports & Issues](https://github.com/oidahdsah0/Rim_AI_Framework/issues)
- 📖 [GitHub Repository](https://github.com/oidahdsah0/Rim_AI_Framework)
- 📋 [Changelog](https://github.com/oidahdsah0/Rim_AI_Framework/releases)
