using Newtonsoft.Json.Linq; // 【新增】
using RimAI.Framework.API;
using RimAI.Framework.Configuration;
using RimAI.Framework.Configuration.Models;
using RimAI.Framework.Core.Lifecycle;
using RimAI.Framework.Translation.Models;
using RimAI.Framework.Shared.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using UnityEngine;
using Verse;
using RimWorld;

namespace RimAI.Framework.UI
{
    public class RimAIFrameworkMod : Mod
    {
        // ... (其他字段保持不变) ...
        private readonly RimAIFrameworkSettings settings;
        private string _apiKeyBuffer = "";
        private float _temperatureBuffer = 0.7f;
        private int _concurrencyLimitBuffer = 5;
        private string _testStatusMessage = "Test the currently saved and active provider.";
        private Color _testStatusColor = Color.gray;
        private bool _isTesting = false;
        private string _lastSelectedProviderId = null;


        public RimAIFrameworkMod(ModContentPack content) : base(content)
        {
            settings = GetSettings<RimAIFrameworkSettings>();
            _lastSelectedProviderId = settings.ActiveProviderId;
        }

        public override string SettingsCategory() => "RimAI Framework";

        public override void DoSettingsWindowContents(Rect inRect)
        {
            // ... (大部分UI绘制代码保持不变) ...
            Listing_Standard listing = new Listing_Standard();
            listing.Begin(inRect);

            var availableProviders = FrameworkDI.SettingsManager.GetAllProviderIds().ToList();
            if (!availableProviders.Any()) { listing.Label("No AI provider templates found."); listing.End(); return; }

            Widgets.Dropdown(
                listing.GetRect(30f), this, (mod) => settings.ActiveProviderId,
                (mod) => GenerateProviderDropdownOptions(availableProviders),
                string.IsNullOrEmpty(settings.ActiveProviderId) ? "Select Provider..." : settings.ActiveProviderId.CapitalizeFirst()
            );
            listing.Gap(12f);

            if (settings.ActiveProviderId != _lastSelectedProviderId)
            {
                LoadSettingsForProvider(settings.ActiveProviderId);
                _lastSelectedProviderId = settings.ActiveProviderId;
            }

            if (string.IsNullOrEmpty(settings.ActiveProviderId)) { listing.Label("Please select a provider from the dropdown menu."); listing.End(); return; }

            listing.Label("API Key:");
            _apiKeyBuffer = Widgets.TextField(listing.GetRect(30f), _apiKeyBuffer);
            listing.Gap(6f);
            
            listing.Label($"Temperature: {_temperatureBuffer:F2}");
            _temperatureBuffer = listing.Slider(_temperatureBuffer, 0f, 2.0f);
            listing.Gap(6f);
            
            listing.Label($"Concurrency Limit: {_concurrencyLimitBuffer}");
            _concurrencyLimitBuffer = (int)listing.Slider(_concurrencyLimitBuffer, 1, 20);
            listing.Gap(24f);

            Rect buttonRect = listing.GetRect(30f);
            float buttonWidth = buttonRect.width / 3f - 4f;
            
            if (Widgets.ButtonText(new Rect(buttonRect.x, buttonRect.y, buttonWidth, 30f), "Save Settings"))
            {
                var newConfig = new UserConfig {
                    ApiKey = _apiKeyBuffer,
                    Temperature = _temperatureBuffer,
                    ConcurrencyLimit = _concurrencyLimitBuffer
                };
                FrameworkDI.SettingsManager.WriteUserConfig(settings.ActiveProviderId, newConfig);
                FrameworkDI.SettingsManager.ReloadConfigs();
                settings.Write();
                Messages.Message("RimAI Framework settings saved.", MessageTypeDefOf.PositiveEvent);
            }

            if (Widgets.ButtonText(new Rect(buttonRect.x + buttonWidth + 6f, buttonRect.y, buttonWidth, 30f), "Reset to Defaults"))
            {
                LoadSettingsForProvider(settings.ActiveProviderId);
            }
            
            if (Widgets.ButtonText(new Rect(buttonRect.x + (buttonWidth + 6f) * 2, buttonRect.y, buttonWidth, 30f), "Test Connection", active: !_isTesting))
            {
                HandleTestConnectionClick();
            }
            listing.Gap(6f);
            
            var defaultColor = GUI.color;
            GUI.color = _testStatusColor;
            listing.Label(_testStatusMessage);
            GUI.color = defaultColor;

            listing.End();
            base.DoSettingsWindowContents(inRect);
        }

        private IEnumerable<Widgets.DropdownMenuElement<string>> GenerateProviderDropdownOptions(IEnumerable<string> providerIds)
        {
            foreach (var providerId in providerIds) {
                yield return new Widgets.DropdownMenuElement<string> {
                    option = new FloatMenuOption(providerId.CapitalizeFirst(), () => settings.ActiveProviderId = providerId),
                    payload = providerId
                };
            }
        }
        
        // --- 【核心重构】---
        private void LoadSettingsForProvider(string providerId)
        {
            if (string.IsNullOrEmpty(providerId)) return;

            // 1. 获取用户配置和模板
            var userConfig = FrameworkDI.SettingsManager.GetUserConfig(providerId);
            var template = FrameworkDI.SettingsManager.GetProviderTemplate(providerId);

            if (template == null) // 安全检查
            {
                RimAILogger.Error($"Could not find a template for providerId: {providerId}");
                return;
            }

            // 2. 实现优先级加载逻辑
            
            // API Key: 只能来自用户配置
            _apiKeyBuffer = userConfig?.ApiKey ?? "";

            // Temperature: 优先用用户配置，其次用模板默认值，最后用代码后备值
            _temperatureBuffer = userConfig?.Temperature 
                ?? template.ChatApi?.DefaultParameters?["temperature"]?.Value<float>() 
                ?? 0.7f;
            
            // Concurrency Limit: 只能来自用户配置，因为它不是模板的一部分
            _concurrencyLimitBuffer = userConfig?.ConcurrencyLimit ?? 5;


            // 3. 重置测试状态
            _isTesting = false;
            _testStatusMessage = "Test the currently saved and active provider.";
            _testStatusColor = Color.gray;
        }

        private async void HandleTestConnectionClick()
        {
            _isTesting = true;
            _testStatusMessage = "Testing, please wait...";
            _testStatusColor = Color.white;

            var cts = new CancellationTokenSource(TimeSpan.FromSeconds(15));
            var request = new UnifiedChatRequest { Messages = new List<ChatMessage> { new ChatMessage { Role = "user", Content = "Hi" } } };

            try
            {
                var result = await RimAIApi.GetCompletionAsync(request, cts.Token);

                if (result.IsSuccess) {
                    _testStatusMessage = $"Success! Response: {result.Value.Message.Content.Truncate(50)}";
                    _testStatusColor = Color.green;
                    Messages.Message("Connection successful!", MessageTypeDefOf.PositiveEvent);
                } else {
                    _testStatusMessage = $"Failed. Reason: {result.Error}";
                    _testStatusColor = Color.red;
                    Messages.Message($"Connection failed: {result.Error}", MessageTypeDefOf.NegativeEvent);
                }
            }
            catch (Exception ex) {
                _testStatusMessage = $"An error occurred: {ex.Message}";
                _testStatusColor = Color.red;
                Messages.Message($"Connection error: {ex.Message}", MessageTypeDefOf.NegativeEvent);
            }
            finally
            {
                _isTesting = false;
            }
        }
    }
}