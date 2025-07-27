# RimAI.Framework v4.0 - 施工计划与清单

本文档旨在作为 v4.0 架构重构工作的核心指导蓝图，确保开发过程清晰、有序、可追踪。

---

## 1. 最终目录结构规划 (Target Directory Structure)

V4 架构的核心是数据驱动和职责分离。所有与特定 AI 服务商相关的适配逻辑都将从核心代码中剥离，转移到外部 JSON 模板中。

```
RimAI.Framework/
└── Source/
    ├── API/
    │   └── RimAIApi.cs          # [公共API] 静态门面。
    │
    ├── Core/
    │   ├── Lifecycle/
    │   │   └── FrameworkDI.cs   # [核心-生命周期] 内部DI容器。
    │   ├── ChatManager.cs       # [核心-协调] 聊天功能总协调器。
    │   └── EmbeddingManager.cs  # [核心-协调] Embedding功能总协调器。
    │
    ├── Configuration/
    │   ├── Models/
    │   │   ├── ProviderTemplate.cs # [配置-模型] 对应 provider_template_*.json
    │   │   ├── UserConfig.cs    # [配置-模型] 对应 user_config_*.json
    │   │   └── MergedConfig.cs  # [配置-模型] 合并上述两者。
    │   └── SettingsManager.cs   # [配置-服务] 加载、解析、合并所有配置文件。
    │
    ├── Translation/
    │   ├── Models/
    │   │   ├── UnifiedChatModels.cs      # [翻译-模型] 聊天相关模型。
    │   │   ├── UnifiedEmbeddingModels.cs # [翻译-模型] Embedding相关模型。
    │   │   └── ToolingModels.cs          # [翻译-模型] 工具调用相关模型。
    │   ├── ChatRequestTranslator.cs    # [翻译-服务] 聊天请求翻译器
    │   ├── ChatResponseTranslator.cs   # [翻译-服务] 聊天响应翻译器
    │   ├── EmbeddingRequestTranslator.cs  # [翻译-服务] Embedding请求翻译器
    │   └── EmbeddingResponseTranslator.cs # [翻译-服务] Embedding响应翻译器
    │
    ├── Execution/
    │   ├── Models/
    │   │   └── RetryPolicy.cs   # [执行-模型] 重试策略。
    │   ├── HttpClientFactory.cs # [执行-基础设施] 创建和管理 HttpClient。
    │   └── HttpExecutor.cs      # [执行-服务] 发送 HTTP 请求并应用重试策略。
    │
    ├── Caching/
    │   └── ResponseCache.cs     # [缓存-服务] 为非流式请求提供响应缓存。
    │
    └── Shared/
        ├── Models/
        │   └── Result.cs        # [共享-模型] [新增] 封装操作结果的通用Result<T>类。
        ├── Exceptions/
        │   ├── FrameworkException.cs
        │   ├── ConfigurationException.cs
        │   └── LLMException.cs
        └── Logging/
            └── RimAILogger.cs     # [共享-日志] 统一的日志记录工具。
```

### 核心目录职责：
*   **`Core/`**: 包含DI容器和负责协调所有其他服务的 **`ChatManager`** 和 **`EmbeddingManager`**。
*   **`Translation/`**: 负责在我们的内部统一模型 (`UnifiedChatModels`, `UnifiedEmbeddingModels`) 和外部提供商特定的数据格式之间进行双向翻译。
*   **`Execution/`**: 负责所有网络通信的底层细节，包括 `HttpClient` 管理和重试逻辑。

---

## 2. 施工计划：五阶段实施策略 (Phased Implementation)

我们将采用“由内而外，先基础后应用”的策略，分五个阶段完成重构。

*   **阶段一：配置与基础 (Configuration & Foundation)** - **目标：** 搭建数据驱动的核心。
*   **阶段二：执行与翻译 - Chat (Execution & Translation - Chat)** - **目标：** 构建聊天请求的“翻译”和“执行”管道。
*   **阶段三：执行与翻译 - Embedding (Execution & Translation - Embedding)** - **目标：** 构建 Embedding 请求的“翻译”和“执行”管道。
*   **阶段四：核心协调与整合 (Coordination & Integration)** - **目标：** 实现 `ChatManager` 和 `EmbeddingManager`，将所有独立的服务串联起来，并完成DI容器的构建。
*   **阶段五：API门面与完善 (Facade & Polish)** - **目标：** 封装内部逻辑，提供简洁、稳定的公共 API，并添加缓存、批量处理等高级功能。

---

## 3. 详细施工清单 (Implementation Checklist)

将上述计划分解为可追踪的具体任务。

注意：在第一轮完成后，必然有第二轮编码、完善、补充。

注解：✅代表已完成；🚧代表施工中；

### ✅ 阶段零：项目初始化

- [✅] 清理 `Source/` 目录下的旧文件（或将其移动到 `Source/Old/` 备份）。
- [✅] 根据规划创建新的空目录结构。

### 🚧 阶段一：配置与基础 (Configuration & Foundation)

-   **配置模型**
    - [✅] `Configuration/Models/ProviderTemplate.cs`: 定义提供商模板的数据结构。**需同时包含 `chatApi` 和 `embeddingApi` 的结构。**
    - [✅] `Configuration/Models/UserConfig.cs`: 定义用户配置的数据结构。**需包含 `concurrencyLimit` 等批量设置。**
    - [✅] `Configuration/Models/MergedConfig.cs`: 定义合并后的内部配置对象。
-   **配置服务**
    - [🚧] `Configuration/SettingsManager.cs`: 实现加载所有 `provider_template_*.json` 和 `user_config_*.json` 的逻辑。
    - [🚧] `Configuration/SettingsManager.cs`: 实现模板验证逻辑，确保加载的模板符合规范，并在出错时提供清晰的错误信息。
    - [🚧] `Configuration/SettingsManager.cs`: 实现 `GetMergedConfig(string providerId)` 方法。
-   **共享组件**
    - [✅] `Shared/Models/Result.cs`: 创建通用的、用于封装操作结果（成功或失败）的 `Result<T>` 类。
    - [✅] `Shared/Exceptions/`: 创建 `FrameworkException.cs`, `ConfigurationException.cs`, `LLMException.cs`。
    - [🚧] `Shared/Logging/RimAILogger.cs`: 创建一个简单的静态日志类。

### 🚧 阶段二：执行与翻译 - Chat (Execution & Translation - Chat)

-   **执行层 (通用)**
    - [  ] `Execution/HttpClientFactory.cs`: 实现一个静态工厂来管理 `HttpClient` 实例。
    - [  ] `Execution/Models/RetryPolicy.cs`: 定义重试策略的数据模型。
    - [  ] `Execution/HttpExecutor.cs`: 实现 `ExecuteAsync` 方法，负责发送 `HttpRequestMessage` 并接收 `HttpResponseMessage`，应用 `RetryPolicy`。
-   **翻译模型 - Chat**
    - [  ] `Translation/Models/ToolingModels.cs`: 定义 `ToolDefinition` 和 `ToolCall`。
    - [  ] `Translation/Models/UnifiedChatModels.cs`: 定义 `UnifiedChatRequest` 和 `UnifiedChatResponse`。
-   **翻译服务 - Chat**
    - [  ] `Translation/ChatRequestTranslator.cs`: 实现 `Translate(UnifiedChatRequest, MergedConfig)` 方法。
    - [  ] `Translation/ChatResponseTranslator.cs`: 实现 `TranslateAsync(HttpResponseMessage, MergedConfig)` 方法，需要支持流式解析。

### 🚧 阶段三：执行与翻译 - Embedding (Execution & Translation - Embedding)

-   **翻译模型 - Embedding**
    - [  ] `Translation/Models/UnifiedEmbeddingModels.cs`: 定义 `UnifiedEmbeddingRequest` 和 `UnifiedEmbeddingResponse`。
-   **翻译服务 - Embedding**
    - [  ] `Translation/EmbeddingRequestTranslator.cs`: 实现 `Translate(UnifiedEmbeddingRequest, MergedConfig)` 方法。**需处理原生批量逻辑，将输入列表打包。**
    - [  ] `Translation/EmbeddingResponseTranslator.cs`: 实现 `TranslateAsync(HttpResponseMessage, MergedConfig)` 方法。**需处理批量响应，将结果列表正确解析。**

### 🚧 阶段四：核心协调与整合 (Coordination & Integration)

-   **核心协调器**
    - [  ] `Core/ChatManager.cs`: 注入所需服务，实现 `ProcessRequestAsync` 方法，按顺序调用 Chat 相关服务。
    - [  ] `Core/EmbeddingManager.cs`: 注入所需服务，实现 `ProcessRequestAsync` 方法，按顺序调用 Embedding 相关服务。
-   **依赖注入**
    - [  ] `Core/Lifecycle/FrameworkDI.cs`: 创建一个静态类，包含一个“一次性”的 `Assemble()` 方法。
    - [  ] `Core/Lifecycle/FrameworkDI.cs`: 在 `Assemble()` 方法中，实例化并连接所有服务 (`SettingsManager`, 所有`Translators`, `HttpExecutor`, `ChatManager`, `EmbeddingManager` 等)。
    - [  ] `Core/Lifecycle/FrameworkDI.cs`: 提供静态属性来访问已组装好的 `ChatManager` 和 `EmbeddingManager` 实例。

### 🚧 阶段五：API门面与完善 (Facade & Polish)

-   **公共 API**
    - [  ] `API/RimAIApi.cs`: 创建一个静态类作为公共门面，并在静态构造函数中调用 `FrameworkDI.Assemble()`。
    - [  ] `API/RimAIApi.cs`: 创建 Chat 相关公共方法 (`GetCompletionAsync`, `StreamCompletionAsync` 等)。
    - [  ] `API/RimAIApi.cs`: **[新增]** 创建 Embedding 相关公共方法 (`GetEmbeddingsAsync`)。
    - [  ] `API/RimAIApi.cs`: **[新增]** 为 Chat 和 Embedding 创建批量处理的公共方法 (`GetCompletionsAsync`, `GetEmbeddingsAsync` 的重载)。
-   **批量处理逻辑**
    - [  ] `Core/ChatManager.cs`: 在 `ProcessBatchAsync` 方法中实现**并发控制**逻辑 (如使用 `SemaphoreSlim`)。
    - [  ] `Core/EmbeddingManager.cs`: 在 `ProcessBatchAsync` 方法中实现**原生批量分块**逻辑。
-   **缓存**
    - [  ] `Caching/ResponseCache.cs`: 实现一个简单的、线程安全的内存缓存服务。
    - [  ] `Core/ChatManager.cs` & `EmbeddingManager.cs`: 注入 `ResponseCache` 服务，并在处理非流式请求时检查和更新缓存。
-   **最终审查**
    - [  ] 审查所有公共API，确保没有内部类型泄露。
    - [  ] 添加 XML 注释到所有公共类和方法。

---

## 施工日志

*(在此处记录每日开发进度、遇到的问题和决策。)*

- **2025-07-27 (初始设定):** 与AI助手讨论后，决定在正式开始编码前，将 `Result<T>` 模式确立为框架的基础错误处理机制。该决策已同步更新到 `ARCHITECTURE_V4.md` 和 `V4_IMPLEMENTATION_PLAN.md` 中，作为所有后续开发的第一步。
- **2025-07-27 (配置模型):** 根据V4架构和进一步讨论，完成了所有核心配置模型的定义，包括 `ProviderTemplate.cs` (V2版，支持动态字段), `UserConfig.cs`, 和 `MergedConfig.cs`。为下一步构建 `SettingsManager` 服务打下了数据基础。
