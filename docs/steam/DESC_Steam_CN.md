🤖 **RimAI Framework** - 人工智能驱动的边缘世界体验

🔧 **核心框架模块**
RimAI Framework是整个RimAI生态系统的基础核心，为边缘世界带来革命性的人工智能体验。本框架专门处理与大型语言模型(LLMs)的所有通信，并为其他内容模块提供完整的API接口。

⚡ **主要特性**
• 🌐 与多种大型语言模型的无缝集成
• 🔌 为其他RimAI模块提供统一的API接口
• ⚙️ 高度优化的性能和稳定性
• 🛡️ 安全可靠的AI通信协议
• 🔄 支持异步处理和实时响应
• 🏠 **完全支持本地OpenAI兼容API** (Ollama, vLLM等)

🔑 **重要：使用前必须进行设置**

⚠️ **使用前必须配置Mod设置！**

**详细设置步骤：**
1. **启用Mod** 并重启边缘世界
2. **进入 设置 → Mod设置 → RimAI Framework**
3. **填写以下字段：**

    🔑 **API Key**（大多数服务需要）：
   • OpenAI：从 https://platform.openai.com/api-keys 获取
   • 本地Ollama、vllm：留空（无需密钥）
   • 完全按照提供商给出的密钥复制粘贴

   📍 **Endpoint URL**（必填）：
   • OpenAI用户：https://api.openai.com/v1
     (Deepseek、Siliconflow的设置类似)
   • 本地Ollama用户：http://localhost:11434/v1
   • 其他服务商：请查看提供商文档

   🤖 **模型名称**（必填）：
   • OpenAI：`gpt-4o-mini`、`gpt-4o`、`gpt-3.5-turbo`
   • Ollama：`llama3.2:3b`、`qwen2.5:7b`、`mistral:7b`（你已安装的模型）
   • 输入提供商指定的准确模型名称

   🔄 **启用流式传输**（可选）：
   • ✅ 勾选此项获得实时响应（推荐）
   • ❌ 取消勾选获得单次完整响应

4. **使用设置中的测试按钮**测试连接
5. **保存设置**，即可开始使用！

💡 **快速入门推荐：**
• **免费方案**：本地安装Ollama配合`llama3.2:3b`模型
• **付费方案**：OpenAI GPT-4o-mini（非常实惠，约每100万token ￥1）
• **经济方案**：使用Anthropic Claude Haiku处理基本任务

💰 **重要费用说明**
⚠️ **Token费用直接支付给您的AI服务提供商，而非Mod作者！**
• 对于云服务（OpenAI、Anthropic等）：按Token使用量付费
• 对于本地部署（Ollama、vLLM等）：安装后无额外费用
• Mod作者不从您的API使用中获得任何收益

🏠 **本地AI支持**
✅ **完全支持本地OpenAI兼容API：**
• Ollama（推荐用于本地部署）
• vLLM
• Text-generation-webui
• LocalAI
• 任何OpenAI兼容端点

📋 **重要说明**
⚠️ 本框架本身不添加任何游戏内容或功能，但是所有其他RimAI模块的必需依赖项。请确保在安装任何RimAI内容模块之前先安装此框架。

🔗 **RimAI生态系统**
此框架是RimAI系列模块的基石，支持：
• 智能对话系统
• AI驱动的故事生成
• 动态事件响应
• 个性化游戏体验
• 更多模块持续开发中...

🎯 **适用版本**
✅ RimWorld 1.5
✅ RimWorld 1.6

🛡️ **开源与安全**
本项目完全开源且安全可用。您可以在我们的GitHub仓库中查看完整源代码、贡献代码和报告问题：github.com/oidahdsah0/Rimworld_AI_Framework

💡 **安装指南**
1. 首先安装此框架模块
2. 在Mod选项中配置AI API设置
3. 安装您需要的RimAI功能模块
4. 享受AI增强的游戏体验！

👨‍💻 **作者**: Kilokio
📦 **模块ID**: kilokio.RimAI.Framework

🔥 如果您喜欢这个项目，请点赞👍并关注➕更多RimAI模块的更新！

⭐ **加入社区**: 在GitHub上报告错误、建议功能或贡献代码！