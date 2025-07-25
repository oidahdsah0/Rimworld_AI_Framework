using System;
using System.Collections.Generic;
using RimAI.Framework.Configuration;
using Verse;

namespace RimAI.Framework.Core
{
    /// <summary>
    /// Helper class to manage RimAI Framework settings and configuration synchronization.
    /// Bridges between RimWorld's mod settings system and the internal configuration system.
    /// </summary>
    public static class RimAISettingsHelper
    {
        /// <summary>
        /// Synchronizes RimAISettings with the internal configuration system.
        /// </summary>
        public static void SyncSettingsToConfiguration(RimAISettings settings)
        {
            try
            {
                var config = RimAIConfiguration.Instance;
                if (config == null) return;

                // API Settings - 只有在API key不为空时才同步，避免覆盖为空值
                if (!string.IsNullOrEmpty(settings.apiKey))
                {
                    config.Set("api.key", settings.apiKey);
                }
                config.Set("api.endpoint", settings.apiEndpoint ?? "https://api.openai.com/v1");
                config.Set("api.model", settings.modelName ?? "gpt-4o");
                config.Set("api.temperature", settings.temperature);
                config.Set("api.maxTokens", settings.maxTokens);
                config.Set("api.enableStreaming", settings.enableStreaming);

                // Performance Settings
                config.Set("performance.timeoutSeconds", settings.timeoutSeconds);
                config.Set("performance.retryCount", settings.retryCount);
                config.Set("performance.maxConcurrentRequests", settings.maxConcurrentRequests);
                config.Set("performance.connectionTimeoutMinutes", 30); // 添加连接超时配置

                // Cache Settings
                config.Set("cache.enabled", settings.enableCaching);
                config.Set("cache.size", settings.cacheSize);
                config.Set("cache.ttlMinutes", settings.cacheTtlMinutes);
                config.Set("cache.maxMemoryMB", settings.cacheMaxMemoryMB);
                config.Set("cache.cleanupIntervalMinutes", settings.cacheCleanupIntervalMinutes);

                // Batch Settings
                config.Set("batch.size", settings.batchSize);
                config.Set("batch.timeoutSeconds", settings.batchTimeoutSeconds);

                // Logging Settings
                config.Set("logging.enableDetailed", settings.enableDetailedLogging);
                config.Set("logging.level", settings.logLevel);

                // Health Check Settings
                config.Set("health.enableChecks", settings.enableHealthCheck);
                config.Set("health.intervalMinutes", settings.healthCheckIntervalMinutes);
                config.Set("health.enableMemoryMonitoring", settings.enableMemoryMonitoring);
                config.Set("health.memoryThresholdMB", settings.memoryThresholdMB);

                // Embedding Settings - 只有在embedding API key不为空时才同步
                config.Set("embedding.enabled", settings.enableEmbeddings);
                if (!string.IsNullOrEmpty(settings.embeddingApiKey))
                {
                    config.Set("embedding.key", settings.embeddingApiKey);
                }
                config.Set("embedding.endpoint", settings.embeddingEndpoint ?? "https://api.openai.com/v1");
                config.Set("embedding.model", settings.embeddingModelName ?? "text-embedding-3-small");

                // 立即保存配置 - CRITICAL FIX
                config.SaveConfiguration();
                
                // 通知相关系统更新设置 - CRITICAL FIX
                NotifySystemsOfConfigurationChange();

                Log.Message("[RimAI Settings] Configuration synchronized and saved");
            }
            catch (Exception ex)
            {
                Log.Error($"[RimAI Settings] Failed to sync settings to configuration: {ex.Message}");
            }
        }
        
        /// <summary>
        /// 通知各个系统配置已更改 - CRITICAL FIX
        /// </summary>
        private static void NotifySystemsOfConfigurationChange()
        {
            try
            {
                // 更新日志系统设置
                RimAILogger.UpdateFromConfiguration();
                
                // 强制刷新HttpClient以应用新的超时设置
                RimAI.Framework.LLM.Http.HttpClientFactory.RefreshHttpClient();
                
                Log.Message("[RimAI Settings] Notified systems of configuration changes");
            }
            catch (Exception ex)
            {
                Log.Warning($"[RimAI Settings] Some systems failed to update: {ex.Message}");
            }
        }

        /// <summary>
        /// Validates settings and returns any validation errors.
        /// </summary>
        public static List<string> ValidateSettings(RimAISettings settings)
        {
            var errors = new List<string>();

            // API Key validation
            if (string.IsNullOrWhiteSpace(settings.apiKey))
            {
                errors.Add("API Key is required");
            }

            // Endpoint validation
            if (string.IsNullOrWhiteSpace(settings.apiEndpoint))
            {
                errors.Add("API Endpoint is required");
            }
            else if (!Uri.IsWellFormedUriString(settings.apiEndpoint, UriKind.Absolute))
            {
                errors.Add("API Endpoint must be a valid URL");
            }

            // Model validation
            if (string.IsNullOrWhiteSpace(settings.modelName))
            {
                errors.Add("Model Name is required");
            }

            // Temperature validation
            if (settings.temperature < 0.0f || settings.temperature > 2.0f)
            {
                errors.Add("Temperature must be between 0.0 and 2.0");
            }

            // Max tokens validation
            if (settings.maxTokens < 1 || settings.maxTokens > 8000)
            {
                errors.Add("Max Tokens must be between 1 and 8000");
            }

            // Timeout validation
            if (settings.timeoutSeconds < 5 || settings.timeoutSeconds > 300)
            {
                errors.Add("Timeout must be between 5 and 300 seconds");
            }

            // Cache size validation
            if (settings.cacheSize < 100 || settings.cacheSize > 10000)
            {
                errors.Add("Cache size must be between 100 and 10000");
            }

            // Cache memory validation
            if (settings.cacheMaxMemoryMB < 50 || settings.cacheMaxMemoryMB > 2048)
            {
                errors.Add("Cache memory limit must be between 50 MB and 2048 MB");
            }

            // Cache cleanup interval validation
            if (settings.cacheCleanupIntervalMinutes < 1 || settings.cacheCleanupIntervalMinutes > 60)
            {
                errors.Add("Cache cleanup interval must be between 1 and 60 minutes");
            }

            // Batch settings validation
            if (settings.batchSize < 1 || settings.batchSize > 50)
            {
                errors.Add("Batch size must be between 1 and 50");
            }

            // Memory threshold validation
            if (settings.enableMemoryMonitoring)
            {
                if (settings.memoryThresholdMB < 10 || settings.memoryThresholdMB > 2048)
                {
                    errors.Add("Memory threshold must be between 10 and 2048 MB");
                }
            }

            return errors;
        }

        /// <summary>
        /// 应用性能预设
        /// </summary>
        public static void ApplyPreset(RimAISettings settings, string presetName)
        {
            if (RimAISettingsWindow.PresetConfigurations.ContainsKey(presetName))
            {
                RimAISettingsWindow.PresetConfigurations[presetName](settings);
                Log.Message($"[RimAI Settings] Applied preset: {presetName}");
            }
            else
            {
                Log.Warning($"[RimAI Settings] Unknown preset: {presetName}");
            }
        }

        /// <summary>
        /// 获取所有可用的预设
        /// </summary>
        public static Dictionary<string, Action<RimAISettings>> GetPresets()
        {
            return RimAISettingsWindow.PresetConfigurations;
        }

        /// <summary>
        /// Exports settings to a readable format for backup.
        /// </summary>
        public static string ExportSettings(RimAISettings settings)
        {
            try
            {
                var lines = new List<string>
                {
                    "# RimAI Framework v3.0 Settings Export",
                    $"# Exported on: {DateTime.Now:yyyy-MM-dd HH:mm:ss}",
                    "",
                    "# API Configuration",
                    $"apiKey={settings.apiKey}",
                    $"apiEndpoint={settings.apiEndpoint}",
                    $"modelName={settings.modelName}",
                    $"temperature={settings.temperature}",
                    $"maxTokens={settings.maxTokens}",
                    $"enableStreaming={settings.enableStreaming}",
                    "",
                    "# Performance Settings",
                    $"timeoutSeconds={settings.timeoutSeconds}",
                    $"retryCount={settings.retryCount}",
                    $"maxConcurrentRequests={settings.maxConcurrentRequests}",
                    "",
                    "# Cache Settings",
                    $"enableCaching={settings.enableCaching}",
                    $"cacheSize={settings.cacheSize}",
                    $"cacheTtlMinutes={settings.cacheTtlMinutes}",
                    $"cacheMaxMemoryMB={settings.cacheMaxMemoryMB}",
                    $"cacheCleanupIntervalMinutes={settings.cacheCleanupIntervalMinutes}",
                    "",
                    "# Batch Settings",
                    $"batchSize={settings.batchSize}",
                    $"batchTimeoutSeconds={settings.batchTimeoutSeconds}",
                    "",
                    "# Logging Settings",
                    $"enableDetailedLogging={settings.enableDetailedLogging}",
                    $"logLevel={settings.logLevel}",
                    "",
                    "# Health Check Settings",
                    $"enableHealthCheck={settings.enableHealthCheck}",
                    $"healthCheckIntervalMinutes={settings.healthCheckIntervalMinutes}",
                    $"enableMemoryMonitoring={settings.enableMemoryMonitoring}",
                    $"memoryThresholdMB={settings.memoryThresholdMB}",
                    "",
                    "# Embedding Settings",
                    $"enableEmbeddings={settings.enableEmbeddings}",
                    $"embeddingApiKey={settings.embeddingApiKey}",
                    $"embeddingEndpoint={settings.embeddingEndpoint}",
                    $"embeddingModelName={settings.embeddingModelName}"
                };

                return string.Join("\n", lines);
            }
            catch (Exception ex)
            {
                Log.Error($"[RimAI Settings] Failed to export settings: {ex.Message}");
                return $"Export failed: {ex.Message}";
            }
        }

        /// <summary>
        /// Gets a summary of current settings for display.
        /// </summary>
        public static string GetSettingsSummary(RimAISettings settings)
        {
            try
            {
                var summary = new List<string>
                {
                    "=== RimAI Framework Settings Summary ===",
                    $"Model: {settings.modelName}",
                    $"Temperature: {settings.temperature:F1}",
                    $"Max Tokens: {settings.maxTokens}",
                    $"Streaming: {(settings.enableStreaming ? "Enabled" : "Disabled")}",
                    $"Caching: {(settings.enableCaching ? $"Enabled ({settings.cacheSize} entries, {settings.cacheTtlMinutes}min TTL, {settings.cacheMaxMemoryMB}MB limit)" : "Disabled")}",
                    $"Concurrent Requests: {settings.maxConcurrentRequests}",
                    $"Batch Size: {settings.batchSize}",
                    $"Health Checks: {(settings.enableHealthCheck ? $"Enabled ({settings.healthCheckIntervalMinutes}min interval)" : "Disabled")}",
                    $"Memory Monitoring: {(settings.enableMemoryMonitoring ? $"Enabled ({settings.memoryThresholdMB}MB threshold)" : "Disabled")}",
                    $"Detailed Logging: {(settings.enableDetailedLogging ? "Enabled" : "Disabled")}",
                    $"Embeddings: {(settings.enableEmbeddings ? "Enabled" : "Disabled")}"
                };

                return string.Join("\n", summary);
            }
            catch (Exception ex)
            {
                return $"Summary generation failed: {ex.Message}";
            }
        }
    }
}
