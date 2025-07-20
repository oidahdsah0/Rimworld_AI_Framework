using System;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using Verse;
using RimWorld;
using RimAI.Framework.LLM;
using RimAI.Framework.API;

namespace RimAI.Framework.Core
{
    /// <summary>
    /// The main Mod class for the RimAI Framework.
    /// This class handles the settings window and is the primary entry point for RimWorld.
    /// </summary>
    public class RimAIMod : Mod
    {
        /// <summary>
        /// A reference to our settings instance.
        /// </summary>
        public readonly RimAISettings settings;

        private bool isTesting = false;
        private string testResult = "";
        private Color testResultColor = Color.white;
        private string testingStatus = "";
        private CancellationTokenSource testCancellationTokenSource;

        /// <summary>
        /// The constructor for the Mod class. It's called once when the mod is loaded.
        /// </summary>
        /// <param name="content">The ModContentPack which contains info about this mod.</param>
        public RimAIMod(ModContentPack content) : base(content)
        {
            // Get a reference to our settings.
            settings = GetSettings<RimAISettings>();
            
            // Initialize the LifecycleManager early in the mod lifecycle
            try
            {
                _ = LifecycleManager.Instance;
                RimAILogger.Info("LifecycleManager initialized successfully");
            }
            catch (Exception ex)
            {
                RimAILogger.Error("Failed to initialize LifecycleManager: {0}", ex.Message);
            }
        }

        /// <summary>
        /// The name of the mod in the settings list.
        /// </summary>
        /// <returns>The display name for the settings category.</returns>
        public override string SettingsCategory()
        {
            return "RimAI.Framework.Settings.Category".Translate();
        }

        /// <summary>
        /// This method is called when the user opens the settings window for this mod.
        /// We use it to draw our custom settings UI.
        /// </summary>
        /// <param name="inRect">The rectangle area to draw the settings within.</param>
        public override void DoSettingsWindowContents(Rect inRect)
        {
            Listing_Standard listingStandard = new Listing_Standard();
            listingStandard.Begin(inRect);

            // Header with framework info
            Text.Font = GameFont.Medium;
            listingStandard.Label("RimAI Framework v3.0");
            Text.Font = GameFont.Small;
            listingStandard.GapLine();

            // Quick status check
            var stats = RimAIAPI.GetStatistics();
            var isHealthy = stats.ContainsKey("IsHealthy") ? Convert.ToBoolean(stats["IsHealthy"]) : false;
            
            GUI.color = isHealthy ? Color.green : Color.red;
            listingStandard.Label($"Framework Status: {(isHealthy ? "Healthy" : "Issues Detected")}");
            GUI.color = Color.white;
            
            listingStandard.Gap(12f);

            // Enhanced Settings Button
            if (listingStandard.ButtonText("Open Complete Settings Window"))
            {
                Find.WindowStack.Add(new RimAISettingsWindow(settings, this));
            }
            
            listingStandard.Gap(6f);
            
            // Quick access settings
            listingStandard.Label("Quick Settings:");
            
            listingStandard.Label("API Key:");
            settings.apiKey = listingStandard.TextEntry(settings.apiKey);

            listingStandard.Label("API Endpoint:");
            settings.apiEndpoint = listingStandard.TextEntry(settings.apiEndpoint);

            listingStandard.Label("Model Name:");
            settings.modelName = listingStandard.TextEntry(settings.modelName);

            listingStandard.CheckboxLabeled("Enable Streaming", ref settings.enableStreaming);
            listingStandard.CheckboxLabeled("Enable Caching", ref settings.enableCaching);

            listingStandard.Gap(6f);
            listingStandard.Label($"Temperature: {settings.temperature:F1}");
            settings.temperature = (float)Math.Round(listingStandard.Slider(settings.temperature, 0.0f, 2.0f), 1);

            listingStandard.Gap(12f);

            // Apply Settings Button
            if (listingStandard.ButtonText("Apply Settings"))
            {
                Log.Message("[RimAI] RimAIMod: Apply Settings button clicked");
                ApplySettings();
            }
            
            listingStandard.Gap(6f);

            // Test Connection Button
            if (isTesting)
            {
                listingStandard.Label("Testing Connection...");
                if (!string.IsNullOrEmpty(testingStatus))
                {
                    GUI.color = Color.yellow;
                    listingStandard.Label($"  {testingStatus}");
                    GUI.color = Color.white;
                }
                
                // Cancel button during testing
                if (listingStandard.ButtonText("Cancel Test"))
                {
                    CancelTest();
                }
            }
            else
            {
                if (listingStandard.ButtonText("Test Connection"))
                {
                    Log.Message("[RimAI] RimAIMod: Test Connection button clicked");
                    isTesting = true;
                    testResult = "";
                    testingStatus = "Initializing test...";
                    _ = TestConnection();
                }
            }

            if (!string.IsNullOrEmpty(testResult))
            {
                GUI.color = testResultColor;
                listingStandard.Label(testResult);
                GUI.color = Color.white;
            }

            // Apply changes notification
            if (GUI.changed)
            {
                // Refresh LLMManager settings when any setting changes
                LLMManager.Instance.RefreshSettings();
            }

            listingStandard.End();
            base.DoSettingsWindowContents(inRect);
        }

        private void CancelTest()
        {
            Log.Message("[RimAI] RimAIMod: Test Connection cancelled by user");
            testCancellationTokenSource?.Cancel();
            testCancellationTokenSource?.Dispose();
            testCancellationTokenSource = null;
            
            isTesting = false;
            testingStatus = "";
            testResult = "Test cancelled";
            testResultColor = Color.gray;
            
            Messages.Message("Connection test cancelled", MessageTypeDefOf.NeutralEvent);
        }

        private void ApplySettings()
        {
            Log.Message("[RimAI] RimAIMod: Applying settings changes");
            
            try
            {
                // 刷新 LLMManager 设置
                LLMManager.Instance.RefreshSettings();
                
                // 保存设置到游戏
                settings.Write();
                
                // 显示成功消息
                Messages.Message("RimAI settings applied successfully", MessageTypeDefOf.PositiveEvent);
                
                Log.Message("[RimAI] RimAIMod: Settings applied successfully");
            }
            catch (Exception ex)
            {
                Log.Error($"[RimAI] RimAIMod: Error applying settings: {ex}");
                Messages.Message($"Error applying RimAI settings: {ex.Message}", MessageTypeDefOf.RejectInput);
            }
        }

        private async Task TestConnection()
        {
            Log.Message("[RimAI] RimAIMod: TestConnection started");
            Messages.Message("RimAI.Framework.Messages.TestStarting".Translate(), MessageTypeDefOf.NeutralEvent);
            testingStatus = "RimAI.Framework.Settings.TestConnectionStatus.Validating".Translate();
            
            // 创建取消令牌
            testCancellationTokenSource = new CancellationTokenSource();
            
            (bool success, string message) result = (false, "Unknown error");
            Exception testException = null;

            try
            {
                testingStatus = "RimAI.Framework.Settings.TestConnectionStatus.Connecting".Translate();
                result = await LLMManager.Instance.TestConnectionAsync(testCancellationTokenSource.Token);
            }
            catch (OperationCanceledException)
            {
                // 测试被取消，不需要特别处理，UI已经在CancelTest中处理了
                return;
            }
            catch (Exception ex)
            {
                testException = ex;
            }

            // Schedule UI updates to run on the main thread
            LongEventHandler.ExecuteWhenFinished(() =>
            {
                try
                {
                    Log.Message($"[RimAI] RimAIMod: TestConnection completed. Success: {result.success}");
                    
                    if (testException != null)
                    {
                        Log.Error($"[RimAI] RimAIMod: TestConnection exception: {testException}");
                        testResultColor = Color.red;
                        testResult = $"Exception: {testException.Message}";
                        Messages.Message($"{"RimAI.Framework.Messages.TestError".Translate()} {testException.Message}", MessageTypeDefOf.RejectInput);
                    }
                    else if (result.success)
                    {
                        testResultColor = Color.green;
                        testResult = "Success";
                        Messages.Message("RimAI.Framework.Messages.TestSuccess".Translate(), MessageTypeDefOf.PositiveEvent);
                    }
                    else
                    {
                        testResultColor = Color.red;
                        testResult = $"Failure: {result.message}";
                        Messages.Message($"{"RimAI.Framework.Messages.TestFailed".Translate()} {result.message}", MessageTypeDefOf.NegativeEvent);
                    }
                }
                catch (Exception ex)
                {
                    Log.Error($"[RimAI] RimAIMod: Error in main thread callback: {ex}");
                    testResultColor = Color.red;
                    testResult = $"Callback error: {ex.Message}";
                }
                finally
                {
                    Log.Message("[RimAI] RimAIMod: Setting isTesting to false");
                    testingStatus = "";
                    isTesting = false;
                    
                    // 清理取消令牌
                    testCancellationTokenSource?.Dispose();
                    testCancellationTokenSource = null;
                }
            });
        }
    }
}
