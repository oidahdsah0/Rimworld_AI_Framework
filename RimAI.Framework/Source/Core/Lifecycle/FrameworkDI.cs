// =====================================================================================================================
// 文件: FrameworkDI.cs
//
// 作用:
//  这是 RimAI.Framework 的轻量级依赖注入 (Dependency Injection) 容器。
//  它扮演着“总装配线”的角色，负责在 Mod 启动时，“一次性”地创建和连接框架所需的所有服务实例。
//
// 设计模式:
//  - 服务定位器 (Service Locator): 通过静态属性提供对全局单例服务的访问。
//  - 单例模式 (Singleton): 所有创建的服务都只存在一个实例，并在整个应用生命周期内共享。
//
// 工作流程:
//  1. 在 Mod 加载的最初期，外部代码（如 RimAIApi 的静态构造函数）调用 Assemble() 方法。
//  2. Assemble() 方法按照正确的依赖顺序，逐一创建所有服务。
//  3. 在创建 Manager 时，将它所依赖的服务作为参数“注入”到其构造函数中。
//  4. 将最终组装好的 Manager 实例存储在静态属性中，供外部使用。
// =====================================================================================================================

// 引入所有需要被“组装”的组件的命名空间
using RimAI.Framework.Configuration;
using RimAI.Framework.Execution;
using RimAI.Framework.Translation;

namespace RimAI.Framework.Core.Lifecycle
{
    /// <summary>
    /// 负责创建和组装所有框架服务的静态 DI 容器。
    /// "static" 类意味着不能创建它的实例，它的所有成员都直接通过类名访问。
    /// </summary>
    public static class FrameworkDI
    {
        // [C# 知识点] "public static" 属性：
        //  - public: 允许从项目中的任何地方访问。
        //  - static: 表示这个属性属于 FrameworkDI 类本身，而不是它的某个实例。
        //  - { get; private set; }: 表示这个属性可以被外部读取 (get)，但只能被 FrameworkDI 类内部设置 (private set)。
        //    这确保了只有 Assemble() 方法能改变它们的值。

        /// <summary>
        /// 获取已组装好的、全局唯一的 ChatManager 实例。
        /// </summary>
        public static ChatManager ChatManager { get; private set; }

        /// <summary>
        /// 获取已组装好的、全局唯一的 EmbeddingManager 实例。
        /// </summary>
        public static EmbeddingManager EmbeddingManager { get; private set; }
        
        private static bool _isAssembled = false;
        private static readonly object _lock = new object();

        /// <summary>
        /// “一次性”组装方法。它会创建所有服务实例并解决它们之间的依赖关系。
        /// </summary>
        public static void Assemble()
        {
            // 增加一个锁和布尔标记，确保即使在多线程环境下，组装过程也只执行一次。
            lock (_lock)
            {
                if (_isAssembled)
                {
                    return;
                }

                // --- 开始组装 ---

                // [阶段 1: 创建无依赖或只有外部依赖的基础服务]
                // 这些是“零件”，它们不依赖于框架中的其他服务。
                var settingsManager = new SettingsManager();
                var httpExecutor = new HttpExecutor();
                var chatRequestTranslator = new ChatRequestTranslator();
                var chatResponseTranslator = new ChatResponseTranslator();
                var embeddingRequestTranslator = new EmbeddingRequestTranslator();
                var embeddingResponseTranslator = new EmbeddingResponseTranslator();

                // [阶段 2: 创建协调器 (Manager)，并注入它们所需的依赖]
                // 这是“组装”步骤。我们将上面创建的“零件”作为构造函数参数“递”给 Manager。
                var chatManager = new ChatManager(
                    settingsManager,
                    chatRequestTranslator,
                    httpExecutor,
                    chatResponseTranslator
                );

                var embeddingManager = new EmbeddingManager(
                    settingsManager,
                    embeddingRequestTranslator,
                    httpExecutor,
                    embeddingResponseTranslator
                );

                // [阶段 3: 将组装好的最终产品，放入公共静态属性中，供外部访问]
                ChatManager = chatManager;
                EmbeddingManager = embeddingManager;
                
                _isAssembled = true;
                
                // --- 组装完成 ---
            }
        }
    }
}