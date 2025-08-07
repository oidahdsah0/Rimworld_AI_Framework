![GitHub 预览](docs/preview/GithubPreview.png)

# 🤖 RimAI 框架 🏛️

[🇺🇸 English](README.md) | [🇨🇳 简体中文](README_zh-CN.md) | [📚 文档](docs/)

[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)
[![RimWorld](https://img.shields.io/badge/RimWorld-1.5%20%7C%201.6-brightgreen.svg)](https://rimworldgame.com/)
[![.NET Framework](https://img.shields.io/badge/.NET%20Framework-4.7.2-blue.svg)](https://dotnet.microsoft.com/download/dotnet-framework)
[![Steam Workshop](https://img.shields.io/badge/Steam-创意工坊-blue.svg)](https://steamcommunity.com/sharedfiles/filedetails/?id=3529263357)
[![Status](https://img.shields.io/badge/状态-v4.1.2-orange.svg)](https://steamcommunity.com/sharedfiles/filedetails/?id=3529186453)

> **🚀 一个革命性的、由 AI 驱动的 RimWorld 框架，它将大语言模型（LLM）的强大能力直接集成到您的殖民地管理体验中，实现智能化、高情境感知的行政决策！🎮✨**

**🎉 现已在 Steam 创意工坊发布！** 🎉  
**[📥 下载 RimAI 框架](https://steamcommunity.com/sharedfiles/filedetails/?id=3529263357)**

**👨‍💻 Author**: [@oidahdsah0](https://github.com/oidahdsah0)  
**📅 Created**: 15 July 2025  
**🚀 Updated**: 06 Aug 2025  
**🔄 Latest**: v4.1.2 Beta - Unified Architecture

---

## 🎯 **核心设计哲学**

**RimAI.Framework** 是一个与服务商无关、数据驱动的后端基础设施，用于同各类大语言模型（LLM）及 Embedding API 交互。其设计目标是实现高度的灵活性、可扩展性和卓越性能。

*   **提供商模板系统**: 通过外部的 `provider_template_*.json` 文件，连接到任何 AI 服务（OpenAI、Ollama、Groq 等）。这些模板定义了完整的 API 契约，使添加新服务商无需修改任何代码。
*   **统一内部模型**: 所有外部请求和响应都会被翻译为统一的内部对象（`UnifiedChatRequest`、`UnifiedEmbeddingResponse` 等），从而将上层逻辑与底层 API 实现解耦。
*   **清晰的分层架构**: 严格分离 API 门面、核心协调器（区分聊天与嵌入）、配置管理、请求/响应翻译器以及 HTTP 执行器等组件的职责。
*   **全面的功能支持**: 原生支持流式/非流式聊天、JSON 模式、函数调用（Function Calling）以及文本嵌入（Text Embedding）。
*   **智能批量处理**: 为 Embedding 提供基于服务商限制的自动分块处理，为聊天提供基于并发限制的批量处理，以最大化吞吐量。

## ⚡ **v4 核心特性** 🌟
- **🔌 数据驱动**：通过 JSON 模板连接到任何 API。
- **🔄 端到端流式传输**：**v4.0 新增！** 功能完备的流式 API，用于实现实时的、逐字响应的交互体验。
- **✨ 嵌入 API**：对文本嵌入提供一流的支持。
- **📊 高级批量处理**：为聊天和嵌入进行优化。
- **🛡️ 健壮与安全**：使用 `Result<T>` 模式确保类型安全的结果处理。
- **🪶 轻量级**: 除游戏本体和 Newtonsoft.Json 外无外部依赖。**不需要 Harmony**。🚀

## 🔧 **安装与设置** 📦

### 📋 前置要求
- 🎮 RimWorld 1.5+

### 💾 安装

#### 🎮 **对于玩家（推荐）**
1. **📥 Steam 创意工坊**：[订阅 RimAI 框架](https://steamcommunity.com/sharedfiles/filedetails/?id=3529263357)
2. **🔧 启用模组**：启动 RimWorld 并在模组列表中启用 "RimAI Framework"。
3. **⚙️ 配置**：遵循下方的配置步骤，在“模组选项”中设置您的 API 凭证。

#### 👨‍💻 **对于开发者**
1. **📂 手动安装**：从 [GitHub Releases](https://github.com/oidahdsah0/Rimworld_AI_Framework/releases) 下载。
2. **🔨 从源码构建**：克隆本仓库并本地构建。

### ⚙️ **配置（关键步骤）**
1. 🎮 打开 RimWorld > 选项 > 模组设置 > RimAI Framework。
2. **🤖 提供商选择**：使用下拉菜单选择一个服务提供商（如 OpenAI, Ollama）。
3. **🔑 API 凭证**：
   - **API 密钥**：您的 API 密钥。（本地服务商如 Ollama 可留空）。
   - **端点 URL**：API 的基础 URL，我们已提供默认值。
   - **模型**：您希望使用的具体模型（如 `gpt-4o-mini`, `llama3`）。
4. **✅ 测试并保存**：使用“测试”按钮验证您的连接，然后点击“保存”。

## 📚 **v4.0 API 使用指南** 💻

v4.0 API 经过精简，功能强大，并引入了一流的流式编程体验。

### 1. 【新增】流式聊天响应
使用 `await foreach` 来消费实时的文本块流。这是实现交互式体验的推荐方式。

```csharp
using RimAI.Framework.API;
using RimAI.Framework.Contracts;
using System.Collections.Generic;
using System.Text;
using Verse;

// 1. 构建请求
var request = new UnifiedChatRequest
{
    Messages = new List<ChatMessage>
    {
        new ChatMessage { Role = "system", Content = "你是一个乐于助人的助手。" },
        new ChatMessage { Role = "user", Content = "给我讲一个关于机器人的短笑话。" }
    }
};

// 2. 使用 await foreach 消费流
var responseBuilder = new StringBuilder();
await foreach (var result in RimAIApi.StreamCompletionAsync(request))
{
    if (result.IsSuccess)
    {
        var chunk = result.Value;
        if (chunk.ContentDelta != null)
        {
            // 实时拼接收到的文本块
            responseBuilder.Append(chunk.ContentDelta);
            // 在这里更新你的 UI
        }
        if (chunk.FinishReason != null)
        {
            Log.Message($"流结束。原因: {chunk.FinishReason}");
        }
    }
    else
    {
        Log.Error($"[MyMod] AI Stream Failed: {result.Error}");
        break; // 出错后中断
    }
}

Log.Message($"[MyMod] 最终完整回复: {responseBuilder.ToString()}");
```

### 2. 非流式聊天补全
用于需要一次性获取完整回复的后台任务。

```csharp
using RimAI.Framework.API;
using RimAI.Framework.Contracts;
using System.Threading.Tasks;

var request = new UnifiedChatRequest { /* ... */ };
Result<UnifiedChatResponse> response = await RimAIApi.GetCompletionAsync(request);

if (response.IsSuccess)
{
    Log.Message($"AI 回复: {response.Value.Message.Content}");
}
else
{
    Log.Error($"AI 错误: {response.Error}");
}
```

### 3. 文本嵌入（批量）
高效地将多个文本转换为向量嵌入。框架会根据服务商的限制自动处理批量。

```csharp
using RimAI.Framework.API;
using RimAI.Framework.Contracts;
using System.Collections.Generic;
using System.Threading.Tasks;

var request = new UnifiedEmbeddingRequest
{
    Input = new List<string>
    {
        "殖民者无所事事。",
        "一支袭击队正从北面接近。",
        "食物供应严重不足。"
    }
};

Result<UnifiedEmbeddingResponse> embeddingsResult = await RimAIApi.GetEmbeddingsAsync(request);

if (embeddingsResult.IsSuccess)
{
    foreach (var embedding in embeddingsResult.Value.Data)
    {
        // 使用向量进行语义搜索等
        Log.Message($"在索引 {embedding.Index} 获得维度为 {embedding.Embedding.Count} 的嵌入向量");
    }
}
```

## 🤝 **贡献** 👥

这是一个开源项目，我们欢迎各种形式的贡献！🎉 请查看我们的[贡献指南](CONTRIBUTING.md)以获取详细信息。

### 📚 架构文档
- 🏛️ **[V4 架构设计 (英文)](docs/ARCHITECTURE_V4.md)**: 深入了解数据驱动架构。
- 🇨🇳 **[v4.3 API 调用指南 (中文)](docs/CN_v4.0_API调用指南.md)**: 最新 API 的详细指南。

## 📄 **许可证** ⚖️

此项目采用 MIT 许可证 - 详细信息请参阅 [LICENSE](LICENSE) 文件。
