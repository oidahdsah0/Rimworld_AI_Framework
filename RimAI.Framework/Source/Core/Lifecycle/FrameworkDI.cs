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
    public static class FrameworkDI
    {
        // ... (ChatManager 和 EmbeddingManager 属性保持不变) ...
        public static ChatManager ChatManager { get; private set; }
        public static EmbeddingManager EmbeddingManager { get; private set; }

        // [新增属性]
        /// <summary>
        /// 获取已组装好的、全局唯一的 SettingsManager 实例。
        /// 我们将它暴露出来，以便 API 门面可以查询默认配置。
        /// </summary>
        public static SettingsManager SettingsManager { get; private set; }
        
        private static bool _isAssembled = false;
        private static readonly object _lock = new object();

        public static void Assemble()
        {
            lock (_lock)
            {
                if (_isAssembled)
                {
                    return;
                }

                // --- 开始组装 ---

                // [阶段 1: 创建基础服务]
                // 将 settingsManager 的变量名从局部变量提升，以便后续可以赋值给静态属性
                var settingsManager = new SettingsManager(); 
                var httpExecutor = new HttpExecutor();
                var chatRequestTranslator = new ChatRequestTranslator();
                var chatResponseTranslator = new ChatResponseTranslator();
                var embeddingRequestTranslator = new EmbeddingRequestTranslator();
                var embeddingResponseTranslator = new EmbeddingResponseTranslator();

                // [阶段 2: 创建协调器]
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

                // [阶段 3: 将组装好的产品放入公共静态属性]
                // [修改] 同时存储 SettingsManager 的实例
                SettingsManager = settingsManager; 
                ChatManager = chatManager;
                EmbeddingManager = embeddingManager;
                
                _isAssembled = true;
                
                // --- 组装完成 ---
            }
        }
    }
}