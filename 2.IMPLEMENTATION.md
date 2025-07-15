# RimAI 框架

欢迎，监督者。本文件概述了 RimAI 框架的开发路线图，该项目旨在通过集成先进的人工智能，彻底改变《环世界》的叙事体验。

我们的核心使命是建立一个强大、可扩展的框架，不仅实现“无摩擦的神权”概念，还赋能其他 Modder 在我们的基础上进行构建。

## 核心架构原则：为扩展而生

1.  **API优先 (API-First)**: 所有的核心系统（编年史、司法、AI交互）都必须提供清晰、稳定且文档化的公共API。Modder 将通过这些API进行交互，而不是修改内部代码。
2.  **事件驱动 (Event-Driven)**: 框架将在关键节点（如：创建新案件、下达判决、记录新编年史事件）广播全局事件。这允许其他 Mod 可以轻松地“挂载”到框架的行为上。
3.  **数据可扩展 (Extensible Data)**: 核心数据结构（如 `ChronicleEvent`, `CaseFile`）将被设计为基类或实现接口，允许 Modder 创建他们自己的自定义事件和案件类型。
4.  **文档至上 (Documentation is King)**: 我们将编写专门的开发者文档，解释框架的理念、API用法和扩展指南。

---

## 第一阶段：可扩展的基石 (The Extensible Foundation)

**目标：从一开始就以可扩展性为核心，搭建核心交互界面。**

1.  **项目与API结构**:
    *   初始化项目，将代码明确划分为 `Internal`（内部实现）和 `API`（公共接口）两个命名空间。
2.  **可扩展的终端建筑**:
    *   创建“帝国精神思维传输终端”的 `ThingDef`。
    *   其 `Comp` 组件将包含一个可供外部访问的 `List<ITab>`。其他 Modder 可以通过 API 向这个列表中添加他们自己的标签页，从而将他们的功能无缝集成到我们的终端中。
3.  **主UI框架**:
    *   实现主UI窗口和标签页系统。
    *   **框架化体现**: 定义一个 `ITab` 接口。我们自己的“案件卷宗”、“法典”等都将是这个接口的实现，为第三方扩展提供标准。

## 第二阶段：开放的殖民地编年史 (The Open Chronicle)

**目标：构建一个允许任何 Mod 记录和查询事件的中央叙事数据库。**

1.  **可扩展的事件定义**:
    *   创建 `ChronicleEvent` 基类，包含所有事件共有的属性（ID, 日期等）。
    *   我们自己的事件，如 `SocialFightEvent` 或 `CrimeEvent`，将继承自这个基类。
    *   **框架化体现**: Modder 可以创建他们自己的 `MyModEvent` 类，继承自 `ChronicleEvent`，并添加到编年史中。
2.  **编年史服务总线 (Chronicle Service Bus)**:
    *   创建 `ColonyChronicleAPI` 静态类。
    *   提供 `public static void LogEvent(ChronicleEvent newEvent)` 方法，允许任何 Mod 向编年史记录新事件。
    *   提供 `public static List<ChronicleEvent> GetEvents(EventFilter filter)` 方法，允许任何 Mod 根据条件查询历史事件。
    *   **框架化体现**: 当 `LogEvent` 被调用时，系统会广播一个 `OnChronicleEventLogged(ChronicleEvent loggedEvent)` 全局事件，供其他 Mod 监听。

## 第三阶段：模块化的司法系统 (The Modular Judiciary)

**目标：实现一个可插拔的司法流程，允许自定义案件类型和判决后果。**

1.  **司法系统API**:
    *   创建 `JudiciaryAPI` 静态类。
    *   提供 `public static void RegisterCaseType(CaseDef caseDef)`，允许其他 Mod 注册他们自己的案件类型（例如“宗教审判”、“商业仲裁”）。
    *   提供 `public static void FileNewCase(CaseFile newCase)`，用于立案。
2.  **可定制的案件与后果**:
    *   创建 `CaseFile` 基类。
    *   **框架化体现**: 当一个 `CaseFile` 被“盖章并下达”后，系统会广播 `OnJudgmentIssued(CaseFile resolvedCase)` 事件。其他 Mod 可以监听此事件，如果 `resolvedCase` 是他们自己的案件类型，他们就可以执行自定义的后果逻辑。
3.  **UI与API的结合**:
    *   “案件卷宗”UI将通过API查询所有待处理的案件。
    *   当渲染一个案件的详情时，它会检查案件的类型，并可能调用该案件类型注册的特定UI渲染方法，允许其他 Mod 自定义其案件的显示方式。

## 第四阶段：可插拔的AI心智 (The Pluggable AI Mind)

**目标：将AI交互层抽象化，使其不依赖于任何特定的模型，并允许外部工具的注册。**

1.  **定义AI服务接口**:
    *   创建 `IAiService` 接口，定义如 `GenerateTextAsync(string prompt)` 和 `AnalyzeTextAsync(string text, List<ToolDef> tools)` 等方法。
    *   我们的默认实现将使用您选择的LLM，但其他 Modder 可以提供自己的 `IAiService` 实现（例如，使用本地模型或不同的云服务）。
2.  **工具注册API (Tool Registration API)**:
    *   创建 `AiToolRegistryAPI`。
    *   **框架化体现**: 其他 Modder 可以定义自己的“工具”（例如，一个能改变天气或生成物品的函数），并通过 `RegisterTool(ToolDef toolDef)` 将其注册到AI可用的工具列表中。当玩家在终端中下达自然语言指令时，AI将能够决定是否调用这些由其他 Mod 提供的工具。
3.  **上下文提供者 (Context Providers)**:
    *   定义 `IContextProvider` 接口。当向AI发送请求时，框架会遍历所有注册的上下文提供者，收集信息。
    *   **框架化体现**: 其他 Mod（例如一个魔法Mod）可以注册一个 `MagicContextProvider`，在AI请求中自动注入当前世界的“魔力水平”等信息，让AI的回答和决策能感知到其他 Mod 的存在。

## 第五阶段：框架发布与生态建设 (Release & Ecosystem)

**目标：正式将框架交付给社区，并提供支持。**

1.  **开发者文档**:
    *   撰写详细的Wiki，解释框架的架构、API用法，并提供代码示例。
2.  **创建示例Mod**:
    *   创建一个小型的、独立的示例Mod，演示如何使用RimAI框架API来添加一个新的案件类型和一个新的终端标签页。
3.  **API版本管理**:
    *   确立清晰的API版本控制策略，确保在未来的更新中，旧的Mod不会轻易失效。

---

## 第六阶段：沉浸式体验增强 (Immersive Experience Enhancements)

**目标：将终端从一个功能性UI提升为一个有质感、有灵魂的交互体验核心。**

设计：所有的加载和等待，我想播放一个声音，就是老机器读取磁盘的声音，另外用户的键盘输入我想加入按键音，回车后，我想播放效果音（有点类似飞船跃迁，代表思维传输。设定里，监督者在打字输入指令的同时，脑袋上还带着一个思维读取头盔。它需要用文字描述这段思维的意图，并让帝国快速审核并传递，虽然这并不存在。）

1.  **复古终端UI设计**:
    *   将主交互窗口设计为老式IBM双屏计算机风格。
    *   左侧小屏幕作为导航，显示案件列表和历史政令。
    *   右侧大屏幕作为主显示区，展示详细的对话和文本内容。
    *   UI采用黑底绿字或琥珀色字的经典终端配色，并使用等宽字体，营造复古科技感。

2.  **音效氛围设计**:
    *   **加载音效**: 在与AI通信、等待回应时，播放持续的“老式磁盘读取”循环音效。
    *   **输入音效**: 实时响应玩家的键盘输入，加入清脆的机械键盘按键音。
    *   **指令发送音效**: 在玩家按下回车发送指令时，播放一个具有科技感的“思维上传/跃迁”音效，强化“指令已发送至帝国网络”的设定。

3.  **对话历史外部化存储**:
    *   **问题**: 为保留玩家与AI的完整交互记忆，所有对话都需要保存。若直接存入主存档文件，会导致存档极速膨胀，影响游戏性能。
    *   **解决方案**: 将对话历史、已归档案件等大数据量内容，保存到与主存档文件关联的外部独立文件（如 `.rimai_log.xml`）中。
    *   **优势**: 保证主存档轻量化，实现无限的、可随时回顾的历史记录，同时不牺牲游戏性能。
