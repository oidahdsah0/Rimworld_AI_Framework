![GitHub 预览](docs/preview/GithubPreview.png)

# 🤖 RimAI 框架 🏛️

[🇺🇸 English](README.md) | [🇨🇳 简体中文](README_zh-CN.md) | [📚 文档](docs/)

[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)
[![RimWorld](https://img.shields.io/badge/RimWorld-1.6-brightgreen.svg)](https://rimworldgame.com/)
[![.NET Framework](https://img.shields.io/badge/.NET%20Framework-4.7.2-blue.svg)](https://dotnet.microsoft.com/download/dotnet-framework)
[![Steam Workshop](https://img.shields.io/badge/Steam-创意工坊-blue.svg)](https://steamcommunity.com/sharedfiles/filedetails/?id=3529263357)
[![Status](https://img.shields.io/badge/状态-v4.0%20测试版-orange.svg)](https://steamcommunity.com/sharedfiles/filedetails/?id=3529186453)

> **🚀 一个革命性的AI驱动的RimWorld框架，将大语言模型直接集成到您的殖民地管理体验中，实现智能化、上下文感知的行政决策！🎮✨**

**🎉 现已在Steam创意工坊发布！** 🎉  
**[📥 下载RimAI框架](https://steamcommunity.com/sharedfiles/filedetails/?id=3529263357)**

**👨‍💻 作者**: [@oidahdsah0](https://github.com/oidahdsah0)  
**📅 创建时间**: 2025年07月15日  
**🚀 更新时间**: 2025年08月03日  
**🔄 最新版本**: v4.0 测试版 - 统一架构

---

## 🚀 **愿景：智能殖民地管理** 🧠

RimAI框架通过**"无摩擦交互"** 🌊 引入了殖民地管理的范式转变——无需殖民者聚集到特定地点或中断他们的工作来处理行政任务。管理通过集中化的**"AI行政终端"** 🖥️，采用异步、智能化的命令处理来实现。

您的决策成为"智能指令" ⚡，在不干扰殖民地生产力的情况下无缝影响世界。后果通过殖民者的想法 💭、对话气泡 💬、社交日志 📋 和全局消息 📢 呈现，创造出由先进AI分析驱动的智能化且极其高效的治理体验！🎯

## 📐 **三层架构** 🏗️

为了创建清晰且可扩展的生态系统，RimAI项目被组织成三个不同的层次：

### 1. **🔧 框架层**（本仓库）✅
- **🎯 目的**：一个与具体服务商无关的、用于所有 AI 通信的技术后端。
- **📋 职责**：
  - 🔌 **v4.0 新增**: **数据驱动的提供商系统**：通过外部 JSON 模板连接到任何 LLM/Embedding API（OpenAI、Ollama、Groq 等）。无需修改代码。
  - 🌐 统一的网络通信、请求构建、响应解析和错误处理。✅
  - ⚡ 具有强大并发控制的异步处理。✅
  - ✨ **v4.0 新增**: **一流的 Embedding 支持**：为文本嵌入提供完全集成的高性能 API。✅
  - 📊 **v4.0 新增**: **高级批量处理**：为 Embedding 提供原生批量处理，为聊天提供并发请求，以最大化吞吐量。✅
  - 🔄 用于实时交互的流式响应。✅
  - 🧠 用于提升性能和节省成本的、可配置的智能缓存。✅
  - 📚 RAG（检索增强生成）知识库集成 🚧
- **🎮 目标**：绝对中立、稳定且高效。不包含任何游戏逻辑。✅

### 2. **⚔️ 核心游戏模块**（未来仓库）🚧
- **🎯 目的**：定义核心游戏体验的官方内容包
- **📋 职责**：
  - 具体的游戏系统，如"司法系统" ⚖️ 和"殖民地编年史" 📖
  - 通过XML和C#定义特定的案例类型、AI工具和游戏事件监听器
  - 为玩家交互界面填充UI 🖱️
- **🎮 类比**：类似于RimWorld的"Core"内容，具有独立"DLC"模块的潜力

### 3. **🎭 AI叙事者**（未来集成）🚧
- **🎯 目的**：由先进AI分析驱动的智能叙事指导者
- **📋 职责**：
  - 带有AI增强的标准RimWorld `StorytellerDef`实现 🤖
  - 对殖民地状态和玩家行为进行持续AI驱动分析 📊
  - 基于智能模式识别的动态事件生成 🎲
- **🎮 目标**：真正自适应的、AI驱动的叙事体验，与您的殖民地共同发展！🌟

## 🎯 **核心功能** 🌟

### 🖥️ AI行政终端
- **🏛️ 单一智能中心**：一个可建造的核心结构解锁整个RimAI系统
- **🎛️ 集成UI**：用于不同管理任务的多标签终端界面：
  - **📁 案件档案**：以档案格式处理刑事案件和民事纠纷
  - **📜 行政法典**：发布全球行政法令和永久法律
  - **👥 AI顾问内阁**：任命和管理您的AI驱动官员
  - **📚 殖民地档案**：访问历史记录和统计数据
  - **🤖 W.I.F.E. 系统**：监护人集成预见引擎——您的AI顾问委员会

### ⚖️ 智能化治理
- **📋 案件登记**：犯罪和纠纷自动创建带有AI分析期限的时间戳案件 ⏰
- **🔍 智能档案审查**：通过AI增强的界面随时处理案件
- **🧠 智能判决**：通过AI驱动的推理和上下文分析发布决定
- **🤖 AI法官代理**：未处理的案件由AI指定官员自动处理
- **⚡ 智能后果**：结果通过AI分析的思想、社交变化和环境反应传播

### 👨‍💼 AI官员系统
- **⚖️ AI法官**：通过智能分析处理逾期案件并通过信件报告 📝
- **🏛️ AI管家**：基于智能殖民地状态分析提供行政建议
- **🎤 AI议长**：基于AI驱动的事件分析提出立法改进

## 🛠️ **技术实现** ⚙️

### 🔧 核心技术与设计
- **🪶 轻量级**: 除游戏本体和 Newtonsoft.Json 外无外部依赖。**不需要 Harmony**。🚀
- **🔌 数据驱动**: API 行为由外部 `provider_template_*.json` 文件定义，而非硬编码。
- **🧱 解耦架构**: 在 API 门面、协调器（聊天/嵌入）、翻译器和执行器之间明确分离关注点。
- **⚙️ 模组设置**: 一个强大的 UI，用于管理多个提供商配置文件及其设置。
- **🛡️ `Result<T>` 模式**: 用于在整个框架中实现健壮、可预测且异常安全的错误处理。

### 🗂️ V4 关键组件
- 🤖 `RimAIApi`: 为所有外部 Mod 提供的简洁、静态的入口点。
- ⚙️ `SettingsManager`: 加载、验证并合并提供商模板与用户配置。
- 🧠 `ChatManager` / `EmbeddingManager`: 各自功能的核心协调器。
- 🔄 `请求/响应翻译器`: 基于模板规则，在统一内部模型和特定提供商的 JSON 结构之间进行转换。
- 📡 `HttpExecutor`: 处理所有出站 HTTP 请求的单点，内置重试逻辑。

### ⚡ **v4.0 核心特性** 🌟
- **🔌 数据驱动**：通过 JSON 模板连接到任何 API。
- **✨ 嵌入 API**：对文本嵌入提供一流的支持。
- **📊 高级批量处理**：为聊天和嵌入优化。
- **🔄 流式响应**：用于实时交互。
- **🧠 智能缓存**：降低成本和延迟。
- **🛡️ 健壮与安全**：使用 `Result<T>` 模式确保类型安全。

## 🔧 **安装和设置** 📦

### 📋 前置要求
- 🎮 RimWorld 1.6+

### 💾 安装

#### 🎮 **对于玩家（推荐）**
1. **📥 Steam创意工坊**：[订阅RimAI框架](https://steamcommunity.com/sharedfiles/filedetails/?id=3529263357)
2. **🔧 启用模组**：启动RimWorld并在模组列表中启用"RimAI Framework"
3. **⚙️ 配置**：在模组选项中设置您的API凭证

#### 👨‍💻 **对于开发者**
1. **📂 手动安装**：从[GitHub发布页面](https://github.com/oidahdsah0/Rim_AI_Framework/releases)下载
2. **🔨 从源代码构建**：克隆并本地构建（见下方开发设置）
3. **⚙️ 配置**：设置您的开发环境和API设置

### ⚙️ 配置
1. 🎮 打开 RimWorld > 选项 > 模组设置 > RimAI Framework。
2. **🤖 提供商选择**：使用下拉菜单选择一个服务提供商（如 OpenAI, Ollama）。下方的设置项将根据所选提供商动态调整。
3. **🔑 API 凭证**：
   - **API 密钥**：您所选服务的 API 密钥。（对于像 Ollama 这样的本地提供商，可以留空）。
   - **端点 URL**：API 的基础 URL。我们已提供默认值。
   - **模型**：您希望使用的具体模型（如 `gpt-4o-mini`, `llama3.2`）。
4. **⚙️ 高级设置（可选）**：
    - 微调如 `温度` 和 `并发上限` 等参数。
    - 通过 JSON 字段添加自定义 HTTP 头或覆盖静态请求参数。
5. **✅ 测试并保存**：使用“测试”按钮验证您的连接，然后点击“保存”。

## 📚 **v4.0 API 使用示例** 💻

v4 API 经过精简，功能更强大。所有配置都在模组设置中处理，而非代码中。

### 简单的聊天补全
```csharp
using RimAI.Framework.API;
using RimAI.Framework.Shared.Models; // 用于 Result<T>
using System.Threading;

CancellationToken cancellationToken = default;
Result<string> response = await RimAIApi.GetCompletionAsync(
    "分析殖民地当前状态并提供简要总结。",
    cancellationToken
);

if (response.IsSuccess)
{
    Log.Message($"AI 回复: {response.Value}");
}
else
{
    Log.Error($"AI 错误: {response.Error}");
}
```

### 流式聊天响应
```csharp
// 获取响应块流，用于实时更新 UI
var stream = RimAIApi.GetCompletionStreamAsync("生成一段详细的事件描述。", cancellationToken);

await foreach (var chunkResult in stream)
{
    if (chunkResult.IsSuccess)
    {
        UpdateMyUI(chunkResult.Value);
    }
    else
    {
        Log.Error($"流错误: {chunkResult.Error}");
        break;
    }
}
```

### 文本嵌入（批量）
```csharp
using System.Collections.Generic;

// 高效地将多个文本转换为向量嵌入
// 框架会根据提供商的限制自动处理批量大小
var textsToEmbed = new List<string>
{
    "殖民者无所事事。",
    "一支袭击队正从北面接近。",
    "食物供应严重不足。"
};

Result<List<float[]>> embeddingsResult = await RimAIApi.GetEmbeddingsAsync(textsToEmbed, cancellationToken);

if (embeddingsResult.IsSuccess)
{
    foreach (var vector in embeddingsResult.Value)
    {
        // 使用向量进行语义搜索等
        Log.Message($"获得维度为 {vector.Length} 的嵌入向量");
    }
}
```

### 强制 JSON 输出
```csharp
// 当所选提供商的模板支持时，可以强制输出 JSON
// 提示词应指示模型返回 JSON
string jsonPrompt = "以 JSON 对象格式返回殖民地的资源水平（食物、药品、零部件）。";

// 只需将 `forceJson` 标志设置为 true
Result<string> jsonResponse = await RimAIApi.GetCompletionAsync(jsonPrompt, cancellationToken, forceJson: true);

if (jsonResponse.IsSuccess)
{
    // jsonResponse.Value 将是一个 JSON 字符串
    var stats = Newtonsoft.Json.JsonConvert.DeserializeObject<ColonyStats>(jsonResponse.Value);
}
```

## 🌍 **支持的语言** 🗣️

框架包含完整的本地化支持：
- 🇺🇸 English（英语）
- 🇨🇳 **简体中文**
- 🇯🇵 日本語（日语）
- 🇰🇷 한국어（韩语）
- 🇫🇷 Français（法语）
- 🇩🇪 Deutsch（德语）
- 🇷🇺 Русский（俄语）

## 🤝 **贡献** 👥

这是一个开源项目，欢迎贡献！🎉 请查看我们的[贡献指南](CONTRIBUTING.md)了解详情。

### 👨‍💻 开发设置
1. **📂 克隆仓库**
   ```bash
   git clone https://github.com/oidahdsah0/Rim_AI_Framework.git
   cd Rim_AI_Framework
   ```

2. **🔨 构建项目**
   ```bash
   # 进入框架目录
   cd RimAI.Framework
   
   # 使用dotnet CLI构建（跨平台）
   dotnet build
   
   # 或在Windows上使用MSBuild
   msbuild Rim_AI_Framework.sln /p:Configuration=Debug
   ```

3. **📋 开发要求**
   - 🛠️ .NET Framework 4.7.2 SDK
   - 💻 Visual Studio 2019+ 或带有C# Dev Kit的VS Code
   - 🎮 RimWorld 1.6+（用于测试）

4. **🍎 macOS构建注意事项**
   - 使用`dotnet build`命令（macOS上不可用MSBuild）
   - 项目自动检测macOS RimWorld安装路径 🎯
   - 需要Mono运行时（通常随.NET SDK一起安装）
   - PostBuild事件直接部署到RimWorld Mods文件夹 📂

### 🏗️ 仓库结构
- **📝 仅源代码**：此仓库仅包含源代码
- **🔨 本地构建**：开发者应从源代码构建
- **✨ 干净的Git**：不提交编译后的二进制文件到仓库
- **📦 发布版本**：预编译的模组在GitHub发布页面提供

### 📚 架构文档
- 🏛️ **[V4 架构设计](docs/ARCHITECTURE_V4.md)**: 深入了解新的数据驱动架构。
- 📋 **[V4 实施计划](docs/V4_IMPLEMENTATION_PLAN.md)**: 分步开发清单。
- 📄 **[V4 模板设计](docs/TEMPLATE_DESIGN.md)**: 创建您自己的提供商模板的规范。

## 📄 **许可证** ⚖️

此项目采用MIT许可证 - 查看[LICENSE](LICENSE)文件了解详情。

## 🙏 **致谢** ❤️

- 🎮 RimWorld社区的灵感和支持
- 👥 所有贡献者和早期采用者

---

## ⚙️ **基础设置指南** 🔧

**⚠️ 重要：在使用任何RimAI模块前，您必须配置Mod设置！**

### 📋 **详细配置步骤**

1. **安装并启用**
   - 在Steam创意工坊订阅RimAI Framework
   - 在Mod列表中启用该模组并重启边缘世界

2. **进入Mod设置**
   - 前往 **设置 → Mod设置 → RimAI Framework**
   - 您将看到包含多个需要填写字段的配置面板

3. **配置必填字段**

   **🔐 API Key**（云服务必需）：
   - **OpenAI**：从 https://platform.openai.com/api-keys 获取
   - **Ollama（本地）、vLLM**：留空 - 无需密钥
   - 完全按照服务商提供的密钥复制粘贴

   **🌐 Endpoint URL**（必填）：
   ```
   OpenAI用户：  https://api.openai.com/v1（Deepseek、Siliconflow设置类似）
   本地Ollama：  http://localhost:11434/v1
   其他服务商：  请查看提供商文档
   ```

   **🤖 模型名称**（必填）：
   ```
   OpenAI：     gpt-4o-mini, gpt-4o, gpt-3.5-turbo
   Ollama：     llama3.2:3b, qwen2.5:7b, mistral:7b（你已安装的模型）
   ```

   **🔄 启用流式传输**（可选）：
   - ✅ **推荐**：勾选以获得实时响应
   - ❌ 取消勾选获得单次完整响应

4. **测试并保存**
   - 使用 **测试连接** 按钮验证您的设置
   - 点击 **保存** 应用您的配置
   - 现在您可以使用RimAI模块了！

### 💡 **新手推荐设置**

**🆓 免费方案（本地AI）**：
- 在您的电脑上安装Ollama
- 下载 `llama3.2:3b` 模型
- URL：`http://localhost:11434/v1`
- API Key：（留空）
- 模型：`llama3.2:3b`

**💰 经济方案（云端AI）**：
- 注册OpenAI账户
- URL：`https://api.openai.com/v1`
- 从OpenAI仪表板获取API密钥
- 模型：`gpt-4o-mini`（非常实惠：每100万token约￥1）

**⭐ 高级方案**：
- 使用 `gpt-4o` 获得最佳效果，或尝试Deepseek、Siliconflow等经济替代方案

---

**⚠️ 免责声明**：这是一个框架级模组，需要额外的内容模块才能实现完整功能。核心游戏功能（司法系统、殖民地编年史等）将作为单独的模块发布。🚧

**🔗 链接**：
- 🎮 **[Steam创意工坊 - 现已发布！](https://steamcommunity.com/sharedfiles/filedetails/?id=3529263357)** ⭐
- 💬 [Discord服务器](https://discord.gg/TBD)（即将推出）
- 🐛 [错误报告与问题](https://github.com/oidahdsah0/Rim_AI_Framework/issues)
- 📖 [GitHub仓库](https://github.com/oidahdsah0/Rim_AI_Framework)
- 📋 [更新日志](https://github.com/oidahdsah0/Rim_AI_Framework/releases)
