using Verse;

namespace RimAI.Framework.UI
{
    /// <summary>
    /// V4.2: 重构为存储独立的 Chat 和 Embedding 服务提供商，并增加一个控制开关。
    /// </summary>
    public class RimAIFrameworkSettings : ModSettings
    {
        /// <summary>
        /// 当前在设置界面中被选中的【聊天】服务提供商的唯一标识符 (ID)。
        /// </summary>
        public string ActiveChatProviderId = "";

        /// <summary>
        /// 当前在设置界面中被选中的【Embedding】服务提供商的唯一标识符 (ID)。
        /// </summary>
        public string ActiveEmbeddingProviderId = "";

        // --- Cache Settings ---
        public bool CacheEnabled = true;
        public int CacheTtlSeconds = 120;


        /// <summary>
        /// 这是 RimWorld Mod 开发中的核心方法，用于实现数据的保存和加载。
        /// 'Scribe' 是 RimWorld 的数据序列化系统，可以理解为一个智能的读写器。
        /// </summary>
        public override void ExposeData()
        {
            base.ExposeData();

            // Scribe_Values.Look() 能同时处理加载和保存。
            // 当游戏加载设置时，它会从配置文件中读取值赋给变量。
            // 当游戏保存设置时，它会将变量的当前值写入配置文件。
            
            // 保存/加载当前选中的聊天提供商ID
            Scribe_Values.Look(ref ActiveChatProviderId, "ActiveChatProviderId", "");

            // 保存/加载当前选中的Embedding提供商ID
            Scribe_Values.Look(ref ActiveEmbeddingProviderId, "ActiveEmbeddingProviderId", "");
            // 缓存开关与 TTL
            Scribe_Values.Look(ref CacheEnabled, "CacheEnabled", true);
            Scribe_Values.Look(ref CacheTtlSeconds, "CacheTtlSeconds", 120);
        }
    }
}