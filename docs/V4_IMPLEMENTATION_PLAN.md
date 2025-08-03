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

### ✅ 阶段一：配置与基础 (Configuration & Foundation)

-   **设计文档**
    - [✅] `docs/TEMPLATE_DESIGN.md`: 经深度讨论，创建了V4模板设计的最终版权威文档。
-   **配置模型 (根据TEMPLATE_DESIGN.md重新实现)**
    - [✅] `Configuration/Models/ProviderTemplate.cs`: **重做完毕**。已根据 `TEMPLATE_DESIGN.md` 的最终版规范，完整实现了包含所有嵌套子类的C#模型。
    - [✅] `Configuration/Models/UserConfig.cs`: **重做完毕**。已根据 `TEMPLATE_DESIGN.md`，完整实现了包含所有可选覆盖项和可空类型的C#模型。
    - [✅] `Configuration/Models/MergedConfig.cs`: **重做完毕**。已实现为一个“智能”对象，其只读属性通过 `?.` 和 `??` 运算符封装了所有“用户优先”的合并逻辑，并加固了对`null`值的处理。
-   **配置服务**
    - [✅] `Configuration/SettingsManager.cs`: 实现加载所有 `provider_template_*.json` 和 `user_config_*.json` 的逻辑。
    - [✅] `Configuration/SettingsManager.cs`: 实现 `GetMergedConfig(string providerId)` 方法。（注：该方法目前仅做装配，合并逻辑由`MergedConfig`内部实现）。
    - [  ] `Configuration/SettingsManager.cs`: 实现模板验证逻辑，确保加载的模板符合规范，并在出错时提供清晰的错误信息。
-   **共享组件**
    - [✅] `Shared/Models/Result.cs`: 创建通用的、用于封装操作结果（成功或失败）的 `Result<T>` 类。
    - [✅] `Shared/Exceptions/`: 创建 `FrameworkException.cs`, `ConfigurationException.cs`, `LLMException.cs`。
    - [✅] `Shared/Logging/RimAILogger.cs`: 创建一个简单的静态日志类。

### 🚧 阶段二：执行与翻译 - Chat (Execution & Translation - Chat)

-   **执行层 (通用)**
    - [✅] `Execution/HttpClientFactory.cs`: 实现一个静态工厂来管理 `HttpClient` 实例。
    - [✅] `Execution/Models/RetryPolicy.cs`: 定义重试策略的数据模型。
    - [✅] `Execution/HttpExecutor.cs`: 实现 `ExecuteAsync` 方法，负责发送 `HttpRequestMessage` 并接收 `HttpResponseMessage`，应用 `RetryPolicy`。
-   **翻译模型 - Chat**
    - [✅] `Translation/Models/ToolingModels.cs`: 定义 `ToolDefinition` 和 `ToolCall`。
    - [✅] `Translation/Models/UnifiedChatModels.cs`: 定义 `UnifiedChatRequest` 和 `UnifiedChatResponse`。
-   **翻译服务 - Chat**
    - [✅] `Translation/ChatRequestTranslator.cs`: 实现 `Translate(UnifiedChatRequest, MergedConfig)` 方法。**必须严格根据`MergedConfig`中的`requestPaths`, `toolPaths`等进行数据驱动的翻译。**
    - [✅] `Translation/ChatResponseTranslator.cs`: 实现 `TranslateAsync(HttpResponseMessage, MergedConfig)` 方法，需要支持流式解析。**必须严格根据`MergedConfig`中的`responsePaths`进行数据驱动的解析。**

### 🚧 阶段三：执行与翻译 - Embedding (Execution & Translation - Embedding)

-   **翻译模型 - Embedding**
    - [✅] `Translation/Models/UnifiedEmbeddingModels.cs`: 定义 `UnifiedEmbeddingRequest` 和 `UnifiedEmbeddingResponse`。
-   **翻译服务 - Embedding**
    - [✅] `Translation/EmbeddingRequestTranslator.cs`: 实现 `Translate(UnifiedEmbeddingRequest, MergedConfig)` 方法。
    - [✅] `Translation/EmbeddingResponseTranslator.cs`: 实现 `TranslateAsync(HttpResponseMessage, MergedConfig)` 方法。

### 🚧 阶段四：核心协调与整合 (Coordination & Integration)

-   **核心协调器**
    - [  ] `Core/ChatManager.cs`: 注入所需服务，实现 `ProcessRequestAsync` 方法，按顺序调用 Chat 相关服务。
    - [  ] `Core/EmbeddingManager.cs`: 注入所需服务，实现 `ProcessRequestAsync` 方法，按顺序调用 Embedding 相关服务。
-   **依赖注入**
    - [  ] `Core/Lifecycle/FrameworkDI.cs`: 创建一个静态类，包含一个“一次性”的 `Assemble()` 方法。
    - [  ] `Core/Lifecycle/FrameworkDI.cs`: 在 `Assemble()` 方法中，实例化并连接所有服务。
    - [  ] `Core/Lifecycle/FrameworkDI.cs`: 提供静态属性来访问已组装好的 `ChatManager` 和 `EmbeddingManager` 实例。

### 🚧 阶段五：API门面与完善 (Facade & Polish)

-   **公共 API**
    - [  ] `API/RimAIApi.cs`: 创建一个静态类作为公共门面，并在静态构造函数中调用 `FrameworkDI.Assemble()`。
    - [  ] `API/RimAIApi.cs`: 创建 Chat 和 Embedding 相关公共方法。
    - [  ] `API/RimAIApi.cs`: 为 Chat 和 Embedding 创建批量处理的公共方法。
-   **批量处理逻辑**
    - [  ] `Core/ChatManager.cs`: 实现**并发控制**逻辑 (如使用 `SemaphoreSlim`)。
    - [  ] `Core/EmbeddingManager.cs`: 实现**原生批量分块**逻辑。
-   **缓存**
    - [  ] `Caching/ResponseCache.cs`: 实现一个简单的、线程安全的内存缓存服务。
    - [  ] `Core/ChatManager.cs` & `EmbeddingManager.cs`: 注入 `ResponseCache` 服务，并在处理非流式请求时检查和更新缓存。
-   **最终审查**
    - [  ] 结合整体架构，对所有文件进行第2次遍历。如有必要，为前面的文件整合后加入内容、功能，使代码成为强壮的整体。
    - [  ] 审查所有公共API，确保没有内部类型泄露。
    - [  ] 添加 XML 注释到所有公共类和方法。

---

## 施工日志

*(在此处记录每日开发进度、遇到的问题和决策。)*

- **2025-08-02 (基础建设):** 完成了 `Shared/Logging/RimAILogger.cs` 的创建。
- **2025-08-02 (项目配置修正):** 修正了项目对 `Newtonsoft.Json` 的依赖问题。
- **2025-08-02 (配置加载):** 在 `SettingsManager.cs` 中实现了 `LoadProviderTemplates` 和 `LoadUserConfigs` 方法。
- **2025-08-03 (设计迭代):** 针对通用性（非标准字段、Function Call差异、本地模型`extra_body`）发起了深度质询。经过多轮迭代，最终敲定了V4版本的模板设计，该设计引入了`requestPaths`, `toolPaths`, `staticParameters`等关键概念，极大地增强了框架的灵活性和可扩展性。
- **2025-08-03 (文档固化):** 将V4模板设计最终版方案，正式写入了 `docs/TEMPLATE_DESIGN.md` 文档，作为后续所有配置和翻译相关开发的权威依据。
- **2025-08-03 (模型重做):** 根据 `TEMPLATE_DESIGN.md`，依次完成了 `ProviderTemplate.cs`, `UserConfig.cs` 的重构。最终，通过实现一个包含复杂合并逻辑的“智能” `MergedConfig.cs`，完成了整个数据模型层的构建。**至此，“阶段一：配置与基础”核心任务已全部完成。**
- **2025-08-03 (进入阶段二 - 执行层):** 正式启动第二阶段的开发。首先完成了执行层的奠基工作：创建了 `Execution/HttpClientFactory.cs`，通过静态构造函数和静态只读实例，确保了全局共享 `HttpClient` 的最佳实践，为整个框架提供了稳定高效的网络通信基础。
- **2025-08-03 (执行层 - 应急预案):** 在 `Execution/Models/` 目录下创建了 `RetryPolicy.cs`。该数据模型通过属性初始化器定义了清晰的默认重试规则（次数、延迟、指数退避），为后续的 `HttpExecutor` 提供了健壮的“应急预案”。
- **2025-08-03 (执行层 - 执行官):** 完成了核心网络服务 `Execution/HttpExecutor.cs` 的编写。该类封装了 `async/await` 异步请求、响应码判断、以及基于 `RetryPolicy` 的完整重试逻辑，成为了框架所有出站HTTP通信的唯一执行者。
- **2025-08-03 (翻译模型 - 工具):** 开始构建阶段二的翻译模型。创建了 `Translation/Models/ToolingModels.cs`，定义了框架内部统一的 `ToolDefinition` 和 `ToolCall` 模型。通过使用 `Newtonsoft.Json.Linq.JObject`，确保了工具参数定义的灵活性，为适配不同厂商的Tool Calling标准打下了基础。
- **2025-08-03 (翻译模型 - 聊天):** 创建了 `Translation/Models/UnifiedChatModels.cs` 文件，定义了作为框架内部“通用语言”的 `UnifiedChatRequest` 和 `UnifiedChatResponse` 模型。通过聚合 `ChatMessage` 和 `ToolCall`，为所有类型的聊天交互提供了统一的数据结构。
- **2025-08-03 (翻译服务 - 请求):** 完成了阶段二最核心的组件之一 `Translation/ChatRequestTranslator.cs`。通过精巧地运用 `Newtonsoft.Json.Linq`，实现了一个完全由 `MergedConfig` 驱动的翻译器。它能够将内部的 `UnifiedChatRequest` 动态地、无硬编码地翻译成任何厂商要求的 `HttpRequestMessage`，完美体现了数据驱动的设计哲学。
- **2025-08-03 (翻译服务 - 响应):** 攻克了阶段二的最后一个堡垒：`Translation/ChatResponseTranslator.cs`。该类不仅实现了对标准JSON响应的“数据驱动”解析，更通过 `IAsyncEnumerable<T>` 和 `yield return` 实现了对流式响应的高效异步处理。**至此，“阶段二：执行与翻译 - Chat”核心任务已全部完成。**
- **2025-08-03 (进入阶段三 - Embedding基础):** 正式启动第三阶段的开发。首先，创建了 `Translation/Models/UnifiedEmbeddingModels.cs` 文件，在其中定义了作为 Embedding 功能“通用语言”的 `UnifiedEmbeddingRequest`、`EmbeddingResult` 和 `UnifiedEmbeddingResponse` 模型，为所有后续 Embedding 相关开发奠定了统一的数据结构基础。
- **2025-08-03 (Embedding Translation - Request):** 完成了阶段三的关键组件 `Translation/EmbeddingRequestTranslator.cs`。该类完美地实践了数据驱动的设计哲学，通过动态读取 `MergedConfig` 中的 `RequestPaths`，可以将统一的内部请求模型 `UnifiedEmbeddingRequest` 翻译成任何厂商所需的 `HttpRequestMessage`，而无需任何硬编码。
- **2025-08-03 (Embedding Translation - Response):** 攻克了阶段三的最后一个堡垒：`Translation/EmbeddingResponseTranslator.cs`。该类通过异步方式，并利用 `JObject.SelectToken()` 和 `MergedConfig` 中的 `ResponsePaths` 规则，实现了对任意厂商响应的“入境翻译”。**至此，“阶段三：执行与翻译 - Embedding”核心任务已全部完成，整个 Embedding 数据管道已成功打通。**