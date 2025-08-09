using RimAI.Framework.Configuration;
using RimAI.Framework.Execution;
using RimAI.Framework.Translation;
using RimAI.Framework.Core;
using RimAI.Framework.Execution.Cache;

namespace RimAI.Framework.Core.Lifecycle
{
    /// <summary>
    /// 框架的轻量级依赖注入容器。
    /// 负责在启动时“一次性”创建和连接所有服务。
    /// </summary>
    public static class FrameworkDI
    {
        public static SettingsManager SettingsManager { get; private set; }
        public static ChatManager ChatManager { get; private set; }
        public static EmbeddingManager EmbeddingManager { get; private set; }

        private static bool _isAssembled = false;

        public static void Assemble()
        {
            if (_isAssembled) return;

            // --- 实例化顺序 ---
            // 1. 基础设施 (Infrastructure)
            // 【修复】HttpClientFactory 是静态类，不应被实例化，HttpExecutor 也不再需要它作为构造参数。
            var httpExecutor = new HttpExecutor();

            // 2. 配置服务 (Configuration Services)
            var settingsManager = new SettingsManager();

            // 3. 翻译服务 (Translation Services)
            var chatRequestTranslator = new ChatRequestTranslator();
            var chatResponseTranslator = new ChatResponseTranslator();
            var embeddingRequestTranslator = new EmbeddingRequestTranslator();
            var embeddingResponseTranslator = new EmbeddingResponseTranslator();

            // 4. 缓存与合流 (Execution/Cache)
            var cache = new MemoryCacheService();
            var inFlight = new InFlightCoordinator();

            // 5. 核心协调器 (Core Managers)
            var chatManager = new ChatManager(settingsManager, chatRequestTranslator, httpExecutor, chatResponseTranslator, cache, inFlight);
            var embeddingManager = new EmbeddingManager(settingsManager, embeddingRequestTranslator, httpExecutor, embeddingResponseTranslator, cache, inFlight);

            // --- 赋值给公共静态属性 ---
            SettingsManager = settingsManager;
            ChatManager = chatManager;
            EmbeddingManager = embeddingManager;

            _isAssembled = true;
        }
    }
}