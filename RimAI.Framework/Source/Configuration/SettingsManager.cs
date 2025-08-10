using Newtonsoft.Json;
using RimAI.Framework.Configuration.Models;
using RimAI.Framework.Shared.Logging;
using RimAI.Framework.Contracts;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Verse;

namespace RimAI.Framework.Configuration
{
    /// <summary>
    /// v4.2.1: 继续完善，确保与会话级缓存与键策略兼容。
    /// </summary>
    public class SettingsManager
    {
        private const string ConfigFolderName = "RimAI_Framework";
        private readonly string _configDirectory;

        // --- 分离的缓存 ---
        private readonly Dictionary<string, ChatTemplate> _chatTemplates = new Dictionary<string, ChatTemplate>();
        private readonly Dictionary<string, EmbeddingTemplate> _embeddingTemplates = new Dictionary<string, EmbeddingTemplate>();
        private readonly Dictionary<string, ChatUserConfig> _chatUserConfigs = new Dictionary<string, ChatUserConfig>();
        private readonly Dictionary<string, EmbeddingUserConfig> _embeddingUserConfigs = new Dictionary<string, EmbeddingUserConfig>();

        // --- 分离的状态 ---
        public bool IsChatActive { get; private set; } = false;
        public bool IsEmbeddingActive { get; private set; } = false;

        public SettingsManager()
        {
            _configDirectory = Path.Combine(GenFilePaths.ConfigFolderPath, ConfigFolderName);
            Directory.CreateDirectory(_configDirectory);
            ReloadConfigs();
            RimAILogger.Log("SettingsManager: Initialized successfully with separated configurations.");
        }

        // --- 热重载 ---
        public void ReloadConfigs()
        {
            RimAILogger.Log("SettingsManager: Reloading all configurations...");
            _chatTemplates.Clear();
            _embeddingTemplates.Clear();
            _chatUserConfigs.Clear();
            _embeddingUserConfigs.Clear();

            LoadChatTemplates();
            LoadEmbeddingTemplates();
            LoadChatUserConfigs();
            LoadEmbeddingUserConfigs();
            
            UpdateActiveStatus();
            RimAILogger.Log("SettingsManager: All configurations reloaded.");
        }

        // --- Chat 配置 API ---
        public IEnumerable<string> GetAllChatProviderIds() => _chatTemplates.Keys;
        public ChatUserConfig GetChatUserConfig(string providerId) => _chatUserConfigs.TryGetValue(providerId, out var config) ? config : null;
        public Result<MergedChatConfig> GetMergedChatConfig(string providerId)
        {
            if (!_chatTemplates.TryGetValue(providerId, out var template))
                return Result<MergedChatConfig>.Failure($"Chat template not found for ID: {providerId}");
            _chatUserConfigs.TryGetValue(providerId, out var userConfig);
            return Result<MergedChatConfig>.Success(new MergedChatConfig { Template = template, User = userConfig ?? new ChatUserConfig() });
        }
        public void WriteChatUserConfig(string providerId, ChatUserConfig config) => 
            WriteConfigToFile($"chat_config_{providerId}.json", config);

        // --- Embedding 配置 API ---
        public IEnumerable<string> GetAllEmbeddingProviderIds() => _embeddingTemplates.Keys;
        public EmbeddingUserConfig GetEmbeddingUserConfig(string providerId) => _embeddingUserConfigs.TryGetValue(providerId, out var config) ? config : null;
        public Result<MergedEmbeddingConfig> GetMergedEmbeddingConfig(string providerId)
        {
            if (!_embeddingTemplates.TryGetValue(providerId, out var template))
                return Result<MergedEmbeddingConfig>.Failure($"Embedding template not found for ID: {providerId}");
            _embeddingUserConfigs.TryGetValue(providerId, out var userConfig);
            return Result<MergedEmbeddingConfig>.Success(new MergedEmbeddingConfig { Template = template, User = userConfig ?? new EmbeddingUserConfig() });
        }
        public void WriteEmbeddingUserConfig(string providerId, EmbeddingUserConfig config) => 
            WriteConfigToFile($"embedding_config_{providerId}.json", config);

        // --- 私有加载逻辑 ---
        private void LoadChatTemplates()
        {
            foreach (var template in BuiltInTemplates.GetChatTemplates())
                _chatTemplates[template.ProviderName.ToLowerInvariant()] = template;
        }

        private void LoadEmbeddingTemplates()
        {
            foreach (var template in BuiltInTemplates.GetEmbeddingTemplates())
                _embeddingTemplates[template.ProviderName.ToLowerInvariant()] = template;
        }

        private void LoadChatUserConfigs() => 
            LoadConfigsFromFileSystem("chat_config_*.json", _chatUserConfigs);

        private void LoadEmbeddingUserConfigs() =>
            LoadConfigsFromFileSystem("embedding_config_*.json", _embeddingUserConfigs);
        
        // --- 通用文件读写帮助方法 ---
        private void LoadConfigsFromFileSystem<T>(string pattern, Dictionary<string, T> targetDictionary)
        {
            foreach (string filePath in Directory.GetFiles(_configDirectory, pattern))
            {
                try
                {
                    string fileName = Path.GetFileNameWithoutExtension(filePath); // e.g., "chat_config_openai"
                    string providerId = fileName.Split('_').LastOrDefault();
                    if (string.IsNullOrEmpty(providerId)) continue;
                    
                    targetDictionary[providerId] = JsonConvert.DeserializeObject<T>(File.ReadAllText(filePath));
                }
                catch (Exception ex) { RimAILogger.Error($"Error loading config from {filePath}: {ex.Message}"); }
            }
        }

        private void WriteConfigToFile<T>(string fileName, T configObject)
        {
            try
            {
                string filePath = Path.Combine(_configDirectory, fileName);
                File.WriteAllText(filePath, JsonConvert.SerializeObject(configObject, Formatting.Indented));
            }
            catch (Exception ex) { RimAILogger.Error($"Failed to write config to {fileName}: {ex.Message}"); }
        }

        private void UpdateActiveStatus()
        {
            // A provider is considered active if either:
            // - It requires auth (has AuthHeader) and has a non-empty ApiKey, or
            // - It does NOT require auth (no AuthHeader), regardless of ApiKey value.
            IsChatActive = _chatTemplates.Any(kv =>
            {
                var providerId = kv.Key;
                var template = kv.Value;
                _chatUserConfigs.TryGetValue(providerId, out var userCfg);
                bool requiresAuth = !string.IsNullOrWhiteSpace(template?.Http?.AuthHeader);
                return requiresAuth ? !string.IsNullOrWhiteSpace(userCfg?.ApiKey) : true;
            });

            IsEmbeddingActive = _embeddingTemplates.Any(kv =>
            {
                var providerId = kv.Key;
                var template = kv.Value;
                _embeddingUserConfigs.TryGetValue(providerId, out var userCfg);
                bool requiresAuth = !string.IsNullOrWhiteSpace(template?.Http?.AuthHeader);
                return requiresAuth ? !string.IsNullOrWhiteSpace(userCfg?.ApiKey) : true;
            });
            RimAILogger.Log($"SettingsManager: Active status updated. Chat={IsChatActive}, Embedding={IsEmbeddingActive}");
        }
    }
}