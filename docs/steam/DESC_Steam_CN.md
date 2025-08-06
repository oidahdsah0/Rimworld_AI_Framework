🤖 **RimAI Framework** - 人工智能驱动的边缘世界体验

🔧 **核心框架模块**
RimAI Framework 是整个 RimAI 生态系统的基础核心。它是一个前置模组，专门负责处理与大语言模型（LLM）的所有通信，并为其他内容模组提供一个全面的 API。

⚡ **V4.1 主要特性**
*   🔌 **数据驱动**：通过简单的 JSON 模板连接到任何 AI 服务商（OpenAI, Ollama, Groq 等）。
*   🔄 **端到端流式传输**：功能完备的流式 API，用于实现实时的、逐字响应的交互体验。
*   ✨ **一流的 Embedding 支持**：为实现复杂的语义理解和记忆功能提供了高性能的 API。
*   📊 **高级批量处理**：为聊天和 Embedding 提供优化的并发请求，以最大化吞吐量。
*   🏠 **完全支持本地 OpenAI 兼容 API** (Ollama, vLLM 等)。

🔑 **重要：使用前必须进行设置**

⚠️ **使用前必须配置 Mod 设置！**

**详细设置步骤：**
1.  **启用 Mod** 并重启边缘世界。
2.  进入 **设置 → Mod 设置 → RimAI Framework**。
3.  **填写必填字段：**
    *   **API 密钥 (API Key)**：您从 OpenAI 等服务商获取的密钥。（本地服务如 Ollama 可留空）。
    *   **端点 URL (Endpoint URL)**：API 的基础地址。**通常我们会为您预设好**，除非您有特殊代理需求或官方地址变更，否则无需手动修改。（例如，OpenAI 为 `https://api.openai.com/v1`，本地 Ollama 为 `http://localhost:11434/v1`）。
    *   **模型名称 (Model Name)**：具体的模型名称（例如 `gpt-4o-mini`, `llama3`）。
4.  使用 **“测试连接”** 按钮验证您的设置。
5.  **保存** 配置，即可开始使用！

💡 **快速入门推荐：**
*   **免费方案**：在本地安装 Ollama，并运行如 `llama3` 这样的模型。
*   **经济方案**：使用 OpenAI 的 `gpt-4o-mini` 模型（非常实惠，每百万 token 约 1.5 元人民币）。

💰 **重要费用说明**
⚠️ **Token 费用是直接支付给您的 AI 服务提供商的，而非 Mod 作者！** Mod 作者不会从您的 API 使用中获得任何收益。像 Ollama 这样的本地模型在初次设置后即可免费运行。

📋 **重要说明**
本框架本身不添加任何游戏内容，但它是所有其他 RimAI 模块的**必需前置**。

🎯 **适用版本**
✅ RimWorld 1.5
✅ RimWorld 1.6

🛡️ **开源与安全**
本项目完全开源。您可以在我们的 GitHub 仓库中查看完整的源代码、贡献代码或报告问题：[github.com/oidahdsah0/Rimworld_AI_Framework](https://github.com/oidahdsah0/Rimworld_AI_Framework)

🔥 如果您喜欢这个项目，请点赞👍并关注➕以获取更多 RimAI 模块的更新！
