// RimAI.Framework/Source/Configuration/SettingsManager.cs

// --- 命名空间引用 ---
// 这里我们引入了完成工作所必需的工具箱。
// System.IO: 用于文件和目录操作，比如查找文件、读取文件内容。
// System.Collections.Generic: 提供了我们核心的数据结构 Dictionary<TKey, TValue>。
// System.Linq: 提供了很多方便的数据操作方法，比如 .LastOrDefault()，我们用它来从分割后的文件名中安全地获取最后一部分。
// Newtonsoft.Json: 这是RimWorld自带的强大库，用于在JSON字符串和C#对象之间进行转换。
// RimAI.Framework.Configuration.Models: 我们自己定义的配置模型，比如 ProviderTemplate 和 UserConfig。
// RimAI.Framework.Shared.Logging: 我们自己创建的日志工具。
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using RimAI.Framework.Configuration.Models;
using RimAI.Framework.Shared.Models; // 【新增】引入 Result<T>，用于更健壮的错误处理。
using RimAI.Framework.Shared.Logging;

namespace RimAI.Framework.Configuration
{
    /// <summary>
    /// 负责加载、解析、验证、合并和缓存所有 `provider` 和 `user` 配置文件。
    /// 它是整个框架配置信息的大管家。
    /// </summary>
    public class SettingsManager
    {
        // --- 成员变量 ---

        // 定义一个常量来存储配置文件夹的名称，避免在代码中多次硬编码字符串"RimAI"。
        // 这是一个好习惯，便于未来统一修改。
        private const string ConfigFolderName = "RimAI";

        // 用于存储所有提供商模板的字典。
        // readonly 确保它在构造函数执行完毕后不能被再次赋值。
        // new ...() 确保它被创建时是一个空字典，而不是 null。
        private readonly Dictionary<string, ProviderTemplate> _providerTemplates = new Dictionary<string, ProviderTemplate>();

        // 用于存储所有用户配置的字典。
        // 它的结构和作用与 _providerTemplates 非常相似。
        private readonly Dictionary<string, UserConfig> _userConfigs = new Dictionary<string, UserConfig>();
        
        // 【新增字段】用于存储从全局配置中读取的默认提供商ID。
        private string _defaultChatProviderId;
        private string _defaultEmbeddingProviderId;

        // --- 构造函数 ---

        /// <summary>
        /// 当一个 SettingsManager 对象被创建时，这个构造函数会自动执行。
        /// 它的职责是调用所有必要的初始化方法，确保对象处于随时可用的状态。
        /// </summary>
        public SettingsManager()
        {
            // 【新增】首先加载全局设置，确定默认的提供商。
            LoadGlobalSettings();
            
            // 接着加载所有的提供商模板文件。
            LoadProviderTemplates();

            // 然后加载所有的用户配置文件。
            LoadUserConfigs();

            // 最后，记录一下我们成功加载了所有配置。
            RimAILogger.Log("SettingsManager: Initialized successfully. All configurations loaded.");
        }
        
        // --- 新增公共方法 ---

        /// <summary>
        /// 获取用户在 Framework 设置中配置的默认聊天提供商 ID。
        /// </summary>
        /// <returns>默认提供商的 ID 字符串。</returns>
        public string GetDefaultChatProviderId()
        {
            return _defaultChatProviderId;
        }

        /// <summary>
        /// 获取用户在 Framework 设置中配置的默认 Embedding 提供商 ID。
        /// </summary>
        /// <returns>默认提供商的 ID 字符串。</returns>
        public string GetDefaultEmbeddingProviderId()
        {
            return _defaultEmbeddingProviderId;
        }

        // --- 私有方法 ---

        // 【新增方法】
        /// <summary>
        /// 加载全局设置，例如默认的提供商。
        /// 【注意】为了简化当前开发，我们暂时在这里“硬编码”默认值。
        /// 未来这里会替换为读取 "global_settings.json" 文件的逻辑。
        /// </summary>
        private void LoadGlobalSettings()
        {
            // --- 硬编码占位符 ---
            // 假设我们从配置文件中读到，用户将 "openai" 设置为了默认提供商。
            _defaultChatProviderId = "openai";
            _defaultEmbeddingProviderId = "openai";
            // --------------------

            RimAILogger.Log($"SettingsManager: Default chat provider set to '{_defaultChatProviderId}'.");
            RimAILogger.Log($"SettingsManager: Default embedding provider set to '{_defaultEmbeddingProviderId}'.");
        }

        /// <summary>
        /// 加载所有 provider_template_*.json 文件，解析它们，并存入 _providerTemplates 字典。
        /// </summary>
        private void LoadProviderTemplates()
        {
            // RimWorld的配置文件夹路径，GenFilePaths.ConfigFolderPath 是游戏API提供的一个标准位置。
            string configPath = Path.Combine(GenFilePaths.ConfigFolderPath, ConfigFolderName);

            // 如果配置文件夹不存在，则记录一条警告信息并直接返回，不做任何操作。
            if (!Directory.Exists(configPath))
            {
                RimAILogger.Warning($"SettingsManager: Configuration folder not found: {configPath}");
                return;
            }

            RimAILogger.Log($"SettingsManager: Loading provider templates from: {configPath}");

            // 查找所有符合 "provider_template_*.json" 模式的文件。
            foreach (string filePath in Directory.GetFiles(configPath, "provider_template_*.json"))
            {
                try
                {
                    // 从完整文件路径中提取不带后缀的文件名，例如 "provider_template_openai"。
                    string fileName = Path.GetFileNameWithoutExtension(filePath);
                    // 将文件名按下划线分割成数组 ["provider", "template", "openai"]，并取最后一个元素作为ID。
                    var providerId = fileName.Split('_').LastOrDefault();

                    // 如果提取的ID为空，则记录一条警告信息并跳过当前文件。
                    if (string.IsNullOrEmpty(providerId))
                    {
                        RimAILogger.Warning($"SettingsManager: Skipping file with invalid ID: {fileName}");
                        continue;
                    }

                    var jsonContent = File.ReadAllText(filePath);
                    var template = JsonConvert.DeserializeObject<ProviderTemplate>(jsonContent);

                    // 以 providerId 为键，template 对象为值，存入字典。
                    _providerTemplates[providerId] = template;
                    RimAILogger.Log($"SettingsManager: Successfully loaded template for '{providerId}' from: {filePath}");
                }
                catch (Exception ex)
                {
                    RimAILogger.Error($"SettingsManager: Error loading provider template from {filePath}: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// 加载所有 user_config_*.json 文件，解析它们，并存入 _userConfigs 字典。
        /// 这个方法的逻辑与 LoadProviderTemplates 高度相似。
        /// </summary>
        private void LoadUserConfigs()
        {
            var configPath = Path.Combine(GenFilePaths.ConfigFolderPath, ConfigFolderName);

            if (!Directory.Exists(configPath))
            {
                // 注意：这里我们不再重复打印目录不存在的警告，因为 LoadProviderTemplates 已经做过了。
                return;
            }

            RimAILogger.Log($"SettingsManager: Loading user configs from: {configPath}");

            foreach (string filePath in Directory.GetFiles(configPath, "user_config_*.json"))
            {
                try
                {
                    var fileName = Path.GetFileNameWithoutExtension(filePath);  // -> "user_config_openai"
                    var userId = fileName.Split('_').LastOrDefault();  // -> "openai"

                    if (string.IsNullOrEmpty(userId))
                    {
                        RimAILogger.Warning($"SettingsManager: Skipping file with invalid ID: {fileName}");
                        continue;
                    }

                    var jsonContent = File.ReadAllText(filePath);
                    var userConfig = JsonConvert.DeserializeObject<UserConfig>(jsonContent);

                    // 以 userId 为键，userConfig 对象为值，存入字典。
                    _userConfigs[userId] = userConfig;
                    RimAILogger.Log($"SettingsManager: Successfully loaded user config for '{userId}' from: {filePath}");
                }
                catch (Exception ex)
                {
                    RimAILogger.Error($"SettingsManager: Error loading user config from {filePath}: {ex.Message}");
                }
            }
        }
        
        // --- 公共方法 ---

        /// <summary>
        /// 【修改】根据提供商ID，查找对应的模板和用户配置，并将它们合并成一个 MergedConfig 对象。
        /// </summary>
        /// <param name="providerId">提供商的唯一标识符，例如 "openai"。</param>
        /// <returns>一个封装了 MergedConfig 或错误的 Result 对象，提供了更清晰的错误处理方式。</returns>
        public Result<MergedConfig> GetMergedConfig(string providerId)
        {
            // 安全检查：验证传入的 providerId 是否有效。
            // string.IsNullOrWhiteSpace 是一个健壮的检查，能同时处理 null、空字符串 "" 和只包含空格的字符串 " "。
            if (string.IsNullOrWhiteSpace(providerId))
            {
                return Result<MergedConfig>.Failure("SettingsManager: Invalid provider ID provided (null or empty).");
            }

            // 查找 ProviderTemplate：
            // 使用 TryGetValue 是最安全的做法。如果键存在，它返回 true 并通过 out 参数赋值；
            // 如果键不存在，它返回 false 且不会抛出异常。
            if (!_providerTemplates.TryGetValue(providerId, out var providerTemplate))
            {
                return Result<MergedConfig>.Failure($"SettingsManager: Provider template not found for ID: {providerId}");
            }

            // 查找 UserConfig：
            // 同样，安全地查找用户配置。
            if (!_userConfigs.TryGetValue(providerId, out var userConfig))
            {
                return Result<MergedConfig>.Failure($"SettingsManager: User config not found for ID: {providerId}");
            }

            // 合并与返回：
            // 当 template 和 userConfig 都成功找到后，我们才进行合并。
            // 这里使用了 "对象初始化器" 语法，非常简洁。
            var mergedConfig = new MergedConfig
            {
                Provider = providerTemplate,
                User = userConfig
            };

            RimAILogger.Log($"SettingsManager: Successfully created merged config for '{providerId}'.");
            
            // 使用 Result.Success 包装成功的结果，这是最佳实践。
            return Result<MergedConfig>.Success(mergedConfig);
        }
    }
}