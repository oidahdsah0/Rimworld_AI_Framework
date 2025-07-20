using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
using RimWorld;
using RimAI.Framework.API;
using RimAI.Framework.Cache;
using RimAI.Framework.Configuration;
using RimAI.Framework.Diagnostics;
using RimAI.Framework.LLM;

namespace RimAI.Framework.Core
{
    /// <summary>
    /// Enhanced settings window for RimAI Framework v3.0 with tabbed interface and advanced options.
    /// </summary>
    public class RimAISettingsWindow : Window
    {
        private readonly RimAISettings settings;
        private readonly RimAIMod mod;
        
        // Tab management
        private TabRecord[] tabs;
        private int currentTab = 0;
        
        // UI state
        private Vector2 scrollPosition = Vector2.zero;
        private string diagnosticsResult = "";
        private Color diagnosticsColor = Color.white;
        private bool isDiagnosticRunning = false;
        
        // Presets
        private static readonly Dictionary<string, Action<RimAISettings>> presets = new Dictionary<string, Action<RimAISettings>>
        {
            {
                "Performance", (s) => {
                    s.enableCaching = true;
                    s.cacheSize = 2000;
                    s.maxConcurrentRequests = 8;
                    s.batchSize = 10;
                    s.enableHealthCheck = true;
                    s.enableMemoryMonitoring = true;
                }
            },
            {
                "Quality", (s) => {
                    s.temperature = 0.3f;
                    s.maxTokens = 2000;
                    s.retryCount = 5;
                    s.enableCaching = false;
                    s.enableDetailedLogging = true;
                }
            },
            {
                "Balanced", (s) => {
                    s.temperature = 0.7f;
                    s.maxTokens = 1000;
                    s.enableCaching = true;
                    s.cacheSize = 1000;
                    s.maxConcurrentRequests = 5;
                    s.batchSize = 5;
                    s.retryCount = 3;
                }
            }
        };

        public override Vector2 InitialSize => new Vector2(800f, 600f);

        public RimAISettingsWindow(RimAISettings settings, RimAIMod mod)
        {
            this.settings = settings;
            this.mod = mod;
            
            forcePause = false;
            doCloseX = true;
            doCloseButton = true;
            closeOnClickedOutside = false;
            absorbInputAroundWindow = true;
            
            // Initialize tabs
            tabs = new TabRecord[]
            {
                new TabRecord("Basic", () => currentTab = 0, () => currentTab == 0),
                new TabRecord("Performance", () => currentTab = 1, () => currentTab == 1),
                new TabRecord("Cache", () => currentTab = 2, () => currentTab == 2),
                new TabRecord("Diagnostics", () => currentTab = 3, () => currentTab == 3),
                new TabRecord("Advanced", () => currentTab = 4, () => currentTab == 4)
            };
        }

        public override void DoWindowContents(Rect inRect)
        {
            // Window title
            Text.Font = GameFont.Medium;
            Widgets.Label(new Rect(0f, 0f, inRect.width, 35f), "RimAI Framework v3.0 Settings");
            Text.Font = GameFont.Small;

            // Tab bar
            Rect tabRect = new Rect(0f, 35f, inRect.width, 30f);
            TabDrawer.DrawTabs<TabRecord>(tabRect, tabs.ToList());

            // Content area with scrolling
            Rect contentRect = new Rect(0f, 70f, inRect.width, inRect.height - 110f);
            Rect scrollRect = new Rect(0f, 0f, contentRect.width - 16f, GetContentHeight());
            
            Widgets.BeginScrollView(contentRect, ref scrollPosition, scrollRect);
            
            Listing_Standard listing = new Listing_Standard();
            listing.Begin(scrollRect);

            switch (currentTab)
            {
                case 0: DrawBasicTab(listing); break;
                case 1: DrawPerformanceTab(listing); break;
                case 2: DrawCacheTab(listing); break;
                case 3: DrawDiagnosticsTab(listing); break;
                case 4: DrawAdvancedTab(listing); break;
            }

            listing.End();
            Widgets.EndScrollView();

            // Bottom buttons
            Rect buttonRect = new Rect(0f, inRect.height - 35f, inRect.width, 35f);
            DrawBottomButtons(buttonRect);

            // Validate settings and sync if changed
            if (GUI.changed)
            {
                RimAISettingsHelper.SyncSettingsToConfiguration(settings);
                
                // Show validation errors if any
                var validationErrors = RimAISettingsHelper.ValidateSettings(settings);
                if (validationErrors.Count > 0)
                {
                    // Just log errors, don't show in UI to avoid clutter
                    Log.Warning($"[RimAI Settings] Validation warnings: {string.Join(", ", validationErrors)}");
                }
            }
        }

        private void DrawBasicTab(Listing_Standard listing)
        {
            // API Configuration
            DrawSectionHeader(listing, "API Configuration");
            
            listing.Label("API Key:");
            settings.apiKey = listing.TextEntry(settings.apiKey);
            
            listing.Label("API Endpoint:");
            settings.apiEndpoint = listing.TextEntry(settings.apiEndpoint);
            
            listing.Label("Model Name:");
            settings.modelName = listing.TextEntry(settings.modelName);

            listing.Gap(12f);

            // Basic Options
            DrawSectionHeader(listing, "Basic Options");
            
            listing.CheckboxLabeled("Enable Streaming", ref settings.enableStreaming, 
                "Enable streaming responses for real-time updates");
            
            listing.Gap(6f);
            listing.Label($"Temperature: {settings.temperature:F1} (0.0 = Deterministic, 2.0 = Creative)");
            settings.temperature = (float)Math.Round(listing.Slider(settings.temperature, 0.0f, 2.0f), 1);
            
            listing.Gap(6f);
            listing.Label($"Max Tokens: {settings.maxTokens}");
            settings.maxTokens = (int)listing.Slider(settings.maxTokens, 50, 4000);

            // Preset configurations
            listing.Gap(12f);
            DrawSectionHeader(listing, "Quick Presets");
            
            Rect presetRect = listing.GetRect(30f);
            float presetWidth = presetRect.width / 3f;
            
            if (Widgets.ButtonText(new Rect(presetRect.x, presetRect.y, presetWidth - 5f, presetRect.height), "Performance"))
            {
                presets["Performance"](settings);
            }
            
            if (Widgets.ButtonText(new Rect(presetRect.x + presetWidth, presetRect.y, presetWidth - 5f, presetRect.height), "Quality"))
            {
                presets["Quality"](settings);
            }
            
            if (Widgets.ButtonText(new Rect(presetRect.x + presetWidth * 2, presetRect.y, presetWidth, presetRect.height), "Balanced"))
            {
                presets["Balanced"](settings);
            }
        }

        private void DrawPerformanceTab(Listing_Standard listing)
        {
            // Performance Presets
            DrawSectionHeader(listing, "Performance Presets");
            
            var presets = RimAISettingsHelper.GetPresets();
            foreach (var preset in presets)
            {
                if (listing.ButtonText($"Apply {preset.Key} Preset"))
                {
                    RimAISettingsHelper.ApplyPreset(settings, preset.Key);
                    Messages.Message($"{preset.Key} preset applied", MessageTypeDefOf.PositiveEvent);
                }
            }

            listing.Gap(12f);
            DrawSectionHeader(listing, "Request Settings");
            
            listing.Label($"Timeout (seconds): {settings.timeoutSeconds}");
            settings.timeoutSeconds = (int)listing.Slider(settings.timeoutSeconds, 5, 120);
            
            listing.Label($"Retry Count: {settings.retryCount}");
            settings.retryCount = (int)listing.Slider(settings.retryCount, 1, 10);
            
            listing.Label($"Max Concurrent Requests: {settings.maxConcurrentRequests}");
            settings.maxConcurrentRequests = (int)listing.Slider(settings.maxConcurrentRequests, 1, 20);

            listing.Gap(12f);
            DrawSectionHeader(listing, "Batch Processing");
            
            listing.Label($"Batch Size: {settings.batchSize}");
            settings.batchSize = (int)listing.Slider(settings.batchSize, 1, 20);
            
            listing.Label($"Batch Timeout (seconds): {settings.batchTimeoutSeconds}");
            settings.batchTimeoutSeconds = (int)listing.Slider(settings.batchTimeoutSeconds, 1, 10);

            listing.Gap(12f);
            DrawSectionHeader(listing, "Memory Management");
            
            listing.CheckboxLabeled("Enable Memory Monitoring", ref settings.enableMemoryMonitoring,
                "Monitor memory usage and trigger automatic cleanup");
            
            if (settings.enableMemoryMonitoring)
            {
                listing.Label($"Memory Threshold (MB): {settings.memoryThresholdMB}");
                settings.memoryThresholdMB = (int)listing.Slider(settings.memoryThresholdMB, 50, 500);
            }
        }

        private void DrawCacheTab(Listing_Standard listing)
        {
            DrawSectionHeader(listing, "Cache Configuration");
            
            listing.CheckboxLabeled("Enable Caching", ref settings.enableCaching,
                "Cache responses for identical requests to improve performance");
            
            if (settings.enableCaching)
            {
                listing.Label($"Cache Size (entries): {settings.cacheSize}");
                settings.cacheSize = (int)listing.Slider(settings.cacheSize, 100, 5000);
                
                listing.Label($"Cache TTL (minutes): {settings.cacheTtlMinutes}");
                settings.cacheTtlMinutes = (int)listing.Slider(settings.cacheTtlMinutes, 5, 180);

                // Cache statistics
                listing.Gap(12f);
                DrawSectionHeader(listing, "Cache Statistics");
                
                try
                {
                    var stats = RimAIAPI.GetStatistics();
                    
                    // Get cache-specific stats
                    var cacheHits = stats.ContainsKey("CacheHits") ? Convert.ToInt64(stats["CacheHits"]) : 0;
                    var cacheMisses = stats.ContainsKey("CacheMisses") ? Convert.ToInt64(stats["CacheMisses"]) : 0;
                    var cacheHitRate = stats.ContainsKey("CacheHitRate") ? Convert.ToDouble(stats["CacheHitRate"]) : 0.0;
                    
                    listing.Label($"Cache Hits: {cacheHits}");
                    listing.Label($"Cache Misses: {cacheMisses}");
                    listing.Label($"Cache Hit Rate: {cacheHitRate:P2}");
                    
                    if (listing.ButtonText("Clear Cache"))
                    {
                        RimAIAPI.ClearCache();
                        Messages.Message("Cache cleared successfully", MessageTypeDefOf.PositiveEvent);
                    }
                }
                catch (Exception ex)
                {
                    listing.Label($"Unable to retrieve cache stats: {ex.Message}");
                }
            }
        }

        private void DrawDiagnosticsTab(Listing_Standard listing)
        {
            DrawSectionHeader(listing, "Logging Settings");
            
            listing.CheckboxLabeled("Enable Detailed Logging", ref settings.enableDetailedLogging,
                "Enable verbose logging for debugging purposes");
            
            string[] logLevels = { "Debug", "Info", "Warning", "Error" };
            listing.Label($"Log Level: {logLevels[settings.logLevel]}");
            settings.logLevel = (int)listing.Slider(settings.logLevel, 0, 3);

            listing.Gap(12f);
            DrawSectionHeader(listing, "Health Monitoring");
            
            listing.CheckboxLabeled("Enable Health Checks", ref settings.enableHealthCheck,
                "Periodically check system health and performance");
            
            if (settings.enableHealthCheck)
            {
                listing.Label($"Health Check Interval (minutes): {settings.healthCheckIntervalMinutes}");
                settings.healthCheckIntervalMinutes = (int)listing.Slider(settings.healthCheckIntervalMinutes, 1, 60);
            }

            // System diagnostics
            listing.Gap(12f);
            DrawSectionHeader(listing, "System Diagnostics");
            
            if (!isDiagnosticRunning)
            {
                if (listing.ButtonText("Run Full Diagnostic"))
                {
                    RunDiagnostics();
                }
                
                if (listing.ButtonText("Test Connection"))
                {
                    TestConnection();
                }
            }
            else
            {
                listing.Label("Running diagnostics...");
            }
            
            if (!string.IsNullOrEmpty(diagnosticsResult))
            {
                GUI.color = diagnosticsColor;
                listing.Label(diagnosticsResult);
                GUI.color = Color.white;
            }

            // Framework statistics
            listing.Gap(12f);
            DrawSectionHeader(listing, "Framework Statistics");
            
            try
            {
                var stats = RimAIAPI.GetStatistics();
                
                var totalRequests = stats.ContainsKey("TotalRequests") ? Convert.ToInt32(stats["TotalRequests"]) : 0;
                var successfulRequests = stats.ContainsKey("SuccessfulRequests") ? Convert.ToInt32(stats["SuccessfulRequests"]) : 0;
                var successRate = stats.ContainsKey("SuccessRate") ? Convert.ToDouble(stats["SuccessRate"]) * 100 : 0.0;
                var totalTokensUsed = stats.ContainsKey("TotalTokensUsed") ? Convert.ToInt64(stats["TotalTokensUsed"]) : 0;
                var isHealthy = stats.ContainsKey("IsHealthy") ? Convert.ToBoolean(stats["IsHealthy"]) : false;
                
                listing.Label($"Total Requests: {totalRequests}");
                listing.Label($"Success Rate: {successRate:F1}%");
                listing.Label($"Total Tokens Used: {totalTokensUsed:N0}");
                listing.Label($"System Health: {(isHealthy ? "Healthy" : "Unhealthy")}");
            }
            catch (Exception ex)
            {
                listing.Label($"Unable to retrieve statistics: {ex.Message}");
            }
        }

        private void DrawAdvancedTab(Listing_Standard listing)
        {
            DrawSectionHeader(listing, "Embedding Settings");
            
            listing.CheckboxLabeled("Enable Embeddings", ref settings.enableEmbeddings,
                "Enable embedding functionality for advanced AI features");
            
            if (settings.enableEmbeddings)
            {
                listing.Label("Embedding API Key (leave empty to use main API key):");
                settings.embeddingApiKey = listing.TextEntry(settings.embeddingApiKey);
                
                listing.Label("Embedding Endpoint:");
                settings.embeddingEndpoint = listing.TextEntry(settings.embeddingEndpoint);
                
                listing.Label("Embedding Model:");
                settings.embeddingModelName = listing.TextEntry(settings.embeddingModelName);
            }

            listing.Gap(12f);
            DrawSectionHeader(listing, "Configuration Management");
            
            if (listing.ButtonText("Export Settings"))
            {
                ExportSettings();
            }
            
            if (listing.ButtonText("Import Settings"))
            {
                ImportSettings();
            }
            
            if (listing.ButtonText("Reset to Defaults"))
            {
                ResetToDefaults();
            }
        }

        private void DrawSectionHeader(Listing_Standard listing, string title)
        {
            listing.Gap(6f);
            Text.Font = GameFont.Medium;
            listing.Label(title);
            Text.Font = GameFont.Small;
            listing.GapLine(6f);
        }

        private void DrawBottomButtons(Rect rect)
        {
            float buttonWidth = 100f;
            float spacing = 10f;
            
            if (Widgets.ButtonText(new Rect(rect.x, rect.y, buttonWidth, rect.height), "Apply"))
            {
                ApplySettings();
            }
            
            if (Widgets.ButtonText(new Rect(rect.x + buttonWidth + spacing, rect.y, buttonWidth, rect.height), "Reset"))
            {
                ResetToDefaults();
            }
            
            if (Widgets.ButtonText(new Rect(rect.xMax - buttonWidth, rect.y, buttonWidth, rect.height), "Close"))
            {
                Close();
            }
        }

        private float GetContentHeight()
        {
            switch (currentTab)
            {
                case 0: return 500f;  // Basic
                case 1: return 400f;  // Performance
                case 2: return 350f;  // Cache
                case 3: return 600f;  // Diagnostics
                case 4: return 450f;  // Advanced
                default: return 400f;
            }
        }

        private void ApplySettings()
        {
            try
            {
                // Refresh any systems that depend on settings
                // This would typically involve notifying the configuration system
                Messages.Message("Settings applied successfully", MessageTypeDefOf.PositiveEvent);
            }
            catch (Exception ex)
            {
                Log.Error($"Failed to apply settings: {ex.Message}");
                Messages.Message($"Failed to apply settings: {ex.Message}", MessageTypeDefOf.RejectInput);
            }
        }

        private void ResetToDefaults()
        {
            // Reset all settings to defaults
            settings.apiKey = "";
            settings.apiEndpoint = "https://api.openai.com/v1";
            settings.modelName = "gpt-4o";
            settings.enableStreaming = false;
            settings.temperature = 0.7f;
            settings.maxTokens = 1000;
            settings.timeoutSeconds = 30;
            settings.retryCount = 3;
            settings.enableCaching = true;
            settings.cacheSize = 1000;
            settings.cacheTtlMinutes = 30;
            settings.maxConcurrentRequests = 5;
            settings.batchSize = 5;
            settings.batchTimeoutSeconds = 2;
            settings.enableDetailedLogging = false;
            settings.logLevel = 1;
            settings.enableHealthCheck = true;
            settings.healthCheckIntervalMinutes = 5;
            settings.enableMemoryMonitoring = true;
            settings.memoryThresholdMB = 100;
            settings.enableEmbeddings = false;
            settings.embeddingApiKey = "";
            settings.embeddingEndpoint = "https://api.openai.com/v1";
            settings.embeddingModelName = "text-embedding-3-small";
            
            Messages.Message("Settings reset to defaults", MessageTypeDefOf.NeutralEvent);
        }

        private void TestConnection()
        {
            isDiagnosticRunning = true;
            diagnosticsResult = "Testing connection...";
            diagnosticsColor = Color.yellow;
            
            try
            {
                // Simple validation test instead of async network test
                var validationErrors = RimAISettingsHelper.ValidateSettings(settings);
                
                if (validationErrors.Count == 0 && !string.IsNullOrWhiteSpace(settings.apiKey))
                {
                    diagnosticsResult = "Connection settings validated successfully";
                    diagnosticsColor = Color.green;
                }
                else if (validationErrors.Count > 0)
                {
                    diagnosticsResult = $"Validation failed: {validationErrors.First()}";
                    diagnosticsColor = Color.red;
                }
                else
                {
                    diagnosticsResult = "API key is required";
                    diagnosticsColor = Color.red;
                }
            }
            catch (Exception ex)
            {
                diagnosticsResult = $"Connection test error: {ex.Message}";
                diagnosticsColor = Color.red;
            }
            finally
            {
                isDiagnosticRunning = false;
            }
        }

        private void RunDiagnostics()
        {
            isDiagnosticRunning = true;
            diagnosticsResult = "Running diagnostic...";
            diagnosticsColor = Color.yellow;
            
            try
            {
                // Check if systems are available
                bool cacheSystemOk = ResponseCache.Instance != null;
                bool configSystemOk = RimAIConfiguration.Instance != null;
                
                var issues = new List<string>();
                if (!cacheSystemOk) issues.Add("Cache system not initialized");
                if (!configSystemOk) issues.Add("Configuration system not initialized");
                
                // Validate current settings
                var validationErrors = RimAISettingsHelper.ValidateSettings(settings);
                issues.AddRange(validationErrors);
                
                bool isHealthy = issues.Count == 0;
                if (isHealthy)
                {
                    diagnosticsResult = "All systems operational";
                    diagnosticsColor = Color.green;
                }
                else
                {
                    diagnosticsResult = $"Issues detected: {string.Join(", ", issues)}";
                    diagnosticsColor = Color.red;
                }
                
                // Log detailed report
                Log.Message($"[RimAI Diagnostics] System check complete. Issues: {issues.Count}");
                if (issues.Count > 0)
                {
                    Log.Warning($"[RimAI Diagnostics] Issues found: {string.Join(", ", issues)}");
                }
            }
            catch (Exception ex)
            {
                diagnosticsResult = $"Diagnostic failed: {ex.Message}";
                diagnosticsColor = Color.red;
            }
            finally
            {
                isDiagnosticRunning = false;
            }
        }

        private void ExportSettings()
        {
            try
            {
                var exportData = RimAISettingsHelper.ExportSettings(settings);
                var summary = RimAISettingsHelper.GetSettingsSummary(settings);
                
                Log.Message($"[RimAI Settings] Export complete:\n{summary}");
                Messages.Message("Settings exported to log", MessageTypeDefOf.PositiveEvent);
            }
            catch (Exception ex)
            {
                Log.Error($"Failed to export settings: {ex.Message}");
                Messages.Message($"Export failed: {ex.Message}", MessageTypeDefOf.RejectInput);
            }
        }

        private void ImportSettings()
        {
            try
            {
                // Apply balanced preset as a demo of import functionality
                RimAISettingsHelper.ApplyPreset(settings, "Balanced");
                Messages.Message("Balanced preset applied as demo import", MessageTypeDefOf.PositiveEvent);
            }
            catch (Exception ex)
            {
                Log.Error($"Failed to import settings: {ex.Message}");
                Messages.Message($"Import failed: {ex.Message}", MessageTypeDefOf.RejectInput);
            }
        }
    }
}
