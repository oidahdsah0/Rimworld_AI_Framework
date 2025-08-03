using Newtonsoft.Json;
using RimAI.Framework.Configuration.Models;
using RimAI.Framework.Shared.Logging;
using RimAI.Framework.Shared.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Verse;

namespace RimAI.Framework.Configuration
{
    public class SettingsManager
    {
        private const string ConfigFolderName = "RimAI_Framework"; // 【修正】使用正确的文件夹名
        private readonly string _userConfigDirectory;
        private readonly Dictionary<string, ProviderTemplate> _providerTemplates = new Dictionary<string, ProviderTemplate>();
        private readonly Dictionary<string, UserConfig> _userConfigs = new Dictionary<string, UserConfig>();

        public bool IsActive { get; private set; } = false;

        public SettingsManager()
        {
            _userConfigDirectory = Path.Combine(GenFilePaths.ConfigFolderPath, ConfigFolderName);
            Directory.CreateDirectory(_userConfigDirectory);
            ReloadConfigs();
            RimAILogger.Log("SettingsManager: Initialized successfully.");
        }
        
        public Result<MergedConfig> GetMergedConfig(string providerId)
        {
            if (string.IsNullOrWhiteSpace(providerId))
                return Result<MergedConfig>.Failure("SettingsManager: Invalid provider ID provided (null or empty).");

            if (!_providerTemplates.TryGetValue(providerId, out var providerTemplate))
                return Result<MergedConfig>.Failure($"SettingsManager: Provider template not found for ID: {providerId}");

            _userConfigs.TryGetValue(providerId, out var userConfig);
            var mergedConfig = new MergedConfig { Provider = providerTemplate, User = userConfig ?? new UserConfig() };
            return Result<MergedConfig>.Success(mergedConfig);
        }

        public IEnumerable<string> GetAllProviderIds() => _providerTemplates.Keys;
        
        public UserConfig GetUserConfig(string providerId)
        {
            _userConfigs.TryGetValue(providerId, out var config);
            return config;
        }

        // --- 【新增】方法 ---
        /// <summary>
        /// 根据提供商ID获取其内置的、只读的模板。
        /// </summary>
        /// <returns>如果找到则返回 ProviderTemplate 对象，否则返回 null。</returns>
        public ProviderTemplate GetProviderTemplate(string providerId)
        {
            _providerTemplates.TryGetValue(providerId, out var template);
            return template;
        }

        public void WriteUserConfig(string providerId, UserConfig config)
        {
            if (string.IsNullOrEmpty(providerId)) { /* ... */ return; }
            try {
                string filePath = Path.Combine(_userConfigDirectory, $"user_config_{providerId}.json");
                string jsonContent = JsonConvert.SerializeObject(config, Formatting.Indented);
                File.WriteAllText(filePath, jsonContent);
            }
            catch (Exception ex) { RimAILogger.Error($"Failed to write user config for '{providerId}'. Error: {ex.Message}"); }
        }
        
        public void ReloadConfigs()
        {
            _providerTemplates.Clear(); _userConfigs.Clear();
            LoadProviderTemplatesFromBuiltIn();
            LoadUserConfigsFromFileSystem();
            UpdateActiveStatus();
        }

        private void LoadProviderTemplatesFromBuiltIn()
        {
            try {
                foreach (var template in BuiltInTemplates.GetAll())
                {
                    string providerId = template.ProviderName.ToLowerInvariant();
                    if (string.IsNullOrEmpty(providerId)) continue;
                    _providerTemplates[providerId] = template;
                }
            }
            catch (Exception ex) { RimAILogger.Error($"A critical error occurred while loading built-in templates. Error: {ex.Message}"); }
        }

        private void LoadUserConfigsFromFileSystem()
        {
            foreach (string filePath in Directory.GetFiles(_userConfigDirectory, "user_config_*.json"))
            {
                try {
                    string fileName = Path.GetFileNameWithoutExtension(filePath);
                    string providerId = fileName.Split('_').LastOrDefault();
                    if (string.IsNullOrEmpty(providerId)) continue;
                    var userConfig = JsonConvert.DeserializeObject<UserConfig>(File.ReadAllText(filePath));
                    _userConfigs[providerId] = userConfig;
                }
                catch (Exception ex) { RimAILogger.Error($"Error loading user config from {filePath}. Error: {ex.Message}"); }
            }
        }

        private void UpdateActiveStatus()
        {
            IsActive = _userConfigs.Values.Any(config => config != null && !string.IsNullOrWhiteSpace(config.ApiKey));
            RimAILogger.Log($"SettingsManager: Framework active status updated to: {IsActive}");
        }
    }
}