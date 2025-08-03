using Verse;

namespace RimAI.Framework.UI
{
    /// <summary>
    /// RimAI.Framework Mod 的设置数据存储类。
    /// 这个类继承自 RimWorld 的 ModSettings，专门用于存储需要被游戏持久化保存的 Mod 配置。
    /// RimWorld 会在游戏启动时加载这些设置，并在玩家修改后自动保存。
    ///
    /// 在我们的设计中，这个类非常轻量，只存储 UI 级别的状态，例如“当前选中的 AI 提供商是谁”。
    /// 具体的、敏感的提供商配置（如 API Key）则由 SettingsManager 动态加载和保存到各自的 user_config_*.json 文件中，
    /// 这种分离使得配置管理更加灵活和安全。
    /// </summary>
    public class RimAIFrameworkSettings : ModSettings
    {
        /// <summary>
        /// 当前在设置界面中被选中的 AI 提供商的唯一标识符 (ID)。
        /// 例如 "openai", "gemini" 等。
        /// 这个值将决定 UI 加载和显示哪个提供商的配置信息。
        /// 我们将其初始化为一个默认值或空字符串，以便在首次加载时进行处理。
        /// </summary>
        public string ActiveProviderId = "";

        /// <summary>
        /// 这是 RimWorld Mod 开发中的核心方法，用于实现数据的保存和加载。
        /// 当游戏需要保存 Mod 设置时（例如关闭设置窗口或退出游戏），它会调用这个方法。
        /// 当游戏需要加载 Mod 设置时（例如启动游戏或打开设置窗口），它也会调用这个方法。
        /// 'Scribe' 是 RimWorld 的数据序列化系统，可以理解为一个智能的读写器。
        /// </summary>
        public override void ExposeData()
        {
            // 调用基类的方法，这是一个好习惯，可以确保父类的任何持久化逻辑也能被执行。
            base.ExposeData();

            // Scribe_Values.Look() 是一个非常方便的方法，它能同时处理加载和保存。
            // 工作原理：
            // - 在保存时，它会读取 ActiveProviderId 变量的当前值，并将其以 "ActiveProviderId" 为标签写入 XML 配置文件。
            // - 在加载时，它会从 XML 配置文件中寻找 "ActiveProviderId" 标签，并将读取到的值赋给 ActiveProviderId 变量。
            //
            // 参数解释：
            // 1. ref ActiveProviderId:  要进行读/写操作的变量。必须使用 ref 关键字。
            // 2. "ActiveProviderId":     在配置文件中用于识别这个数据的唯一标签（Key）。通常和变量名保持一致。
            // 3. "":                     这是默认值。如果在加载时，配置文件中找不到 "ActiveProviderId" 这个标签（比如 Mod 第一次运行），
            //                           那么 ActiveProviderId 变量就会被赋予这个默认值。
            Scribe_Values.Look(ref ActiveProviderId, "ActiveProviderId", "");
        }
    }
}