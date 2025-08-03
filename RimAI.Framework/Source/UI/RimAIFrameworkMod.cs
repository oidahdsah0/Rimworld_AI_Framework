using Newtonsoft.Json.Linq;
using RimAI.Framework.API;
using RimAI.Framework.Configuration;
using RimAI.Framework.Configuration.Models;
using RimAI.Framework.Core.Lifecycle;
using RimAI.Framework.Shared.Logging;
using RimAI.Framework.Translation.Models;
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
        private readonly RimAIFrameworkSettings settings;

        // Chat
        private string _chatApiKeyBuffer = "";
        private string _chatModelBuffer = "";
        private string _chatEndpointBuffer = "";
        private float _chatTemperatureBuffer = 0.7f;
        private int _chatConcurrencyLimitBuffer = 5;
        private string _lastChatProviderId = null;
        private bool _isChatTesting = false;
        private string _chatTestStatusMessage = "Test the currently saved chat provider.";

        // Embedding
        private string _embeddingApiKeyBuffer = "";
        private string _embeddingModelBuffer = "";
        private string _embeddingEndpointBuffer = "";
        private string _lastEmbeddingProviderId = null;
        private bool _isEmbeddingTesting = false;
        private string _embeddingTestStatusMessage = "Test the currently saved embedding provider.";

        public RimAIFrameworkMod(ModContentPack content) : base(content)
        {
            FrameworkDI.Assemble();
            settings = GetSettings<RimAIFrameworkSettings>();
        }

        public override string SettingsCategory() => "RimAI Framework";

        public override void DoSettingsWindowContents(Rect inRect)
        {
            var listing = new Listing_Standard();
            listing.Begin(inRect);

            DrawSection(listing, "Chat Service", settings.ActiveChatProviderId, ref _lastChatProviderId,
                (newId) => settings.ActiveChatProviderId = newId, // Pass setter action
                FrameworkDI.SettingsManager.GetAllChatProviderIds(),
                LoadChatSettings, DrawChatFields, HandleChatSave, HandleChatTest,
                _isChatTesting, ref _chatTestStatusMessage);

            listing.GapLine(24f);

            listing.CheckboxLabeled("Enable Separate Embedding Configuration", ref settings.IsEmbeddingConfigEnabled);
            if (settings.IsEmbeddingConfigEnabled)
            {
                DrawSection(listing, "Embedding Service", settings.ActiveEmbeddingProviderId, ref _lastEmbeddingProviderId,
                    (newId) => settings.ActiveEmbeddingProviderId = newId, // Pass setter action
                    FrameworkDI.SettingsManager.GetAllEmbeddingProviderIds(),
                    LoadEmbeddingSettings, DrawEmbeddingFields, HandleEmbeddingSave, HandleEmbeddingTest,
                    _isEmbeddingTesting, ref _embeddingTestStatusMessage);
            }

            listing.End();
        }

        private void DrawSection(
            Listing_Standard listing, 
            string title, 
            string activeProviderId, 
            ref string lastProviderId, 
            Action<string> setActiveProviderId, // 【修复】新增参数
            IEnumerable<string> providerIds, 
            Action<string> loadAction, 
            Action<Listing_Standard> drawFieldsAction, 
            Action saveAction, 
            Action testAction, 
            bool isTesting, 
            ref string testStatusMessage)
        {
            listing.Label(title);
            listing.Gap(4f);
            
            var providerIdList = providerIds.ToList();
            if (!providerIdList.Any()) { listing.Label("No providers available."); return; }
            string currentLabel = string.IsNullOrEmpty(activeProviderId) ? "Select Provider..." : activeProviderId.CapitalizeFirst();
            if (Widgets.ButtonText(listing.GetRect(30f), currentLabel))
            {
                var options = new List<FloatMenuOption>();
                foreach (var id in providerIdList)
                {
                    // 【修复】将 id 赋值给一个局部变量，以解决 lambda 捕获 ref 参数的问题
                    string newId = id;
                    options.Add(new FloatMenuOption(id.CapitalizeFirst(), () => setActiveProviderId(newId)));
                }
                Find.WindowStack.Add(new FloatMenu(options));
            }
            if (activeProviderId != lastProviderId) { loadAction(activeProviderId); lastProviderId = activeProviderId; }
            if (string.IsNullOrEmpty(activeProviderId)) { listing.Label("Please select a provider."); return; }
            listing.Gap(12f);
            drawFieldsAction(listing);
            listing.Gap(24f);
            Rect buttonRect = listing.GetRect(30f);
            float buttonWidth = (testAction != null) ? (buttonRect.width / 2f - 4f) : buttonRect.width;
            if (Widgets.ButtonText(new Rect(buttonRect.x, buttonRect.y, buttonWidth, 30f), "Save")) { saveAction(); }
            if (testAction != null) {
                if (Widgets.ButtonText(new Rect(buttonRect.x + buttonWidth + 8f, buttonRect.y, buttonWidth, 30f), "Test", active: !isTesting)) { testAction(); }
                listing.Label(testStatusMessage);
            }
        }

        // --- Chat Specific Logic ---
        private void DrawChatFields(Listing_Standard listing) {
            listing.Label("API Key:");
            _chatApiKeyBuffer = Widgets.TextField(listing.GetRect(30f), _chatApiKeyBuffer);
            listing.Label("Endpoint URL:");
            _chatEndpointBuffer = Widgets.TextField(listing.GetRect(30f), _chatEndpointBuffer);
            listing.Label("Model:");
            _chatModelBuffer = Widgets.TextField(listing.GetRect(30f), _chatModelBuffer);
            listing.Label($"Temperature: {_chatTemperatureBuffer:F2}");
            _chatTemperatureBuffer = listing.Slider(_chatTemperatureBuffer, 0f, 2.0f);
            listing.Label($"Concurrency Limit: {_chatConcurrencyLimitBuffer}");
            _chatConcurrencyLimitBuffer = (int)listing.Slider(_chatConcurrencyLimitBuffer, 1, 20);
        }
        
        private void LoadChatSettings(string providerId) {
            var userConfig = FrameworkDI.SettingsManager.GetChatUserConfig(providerId);
            var templateResult = FrameworkDI.SettingsManager.GetMergedChatConfig(providerId);
            if (!templateResult.IsSuccess) return;
            var template = templateResult.Value.Template;
            _chatApiKeyBuffer = userConfig?.ApiKey ?? "";
            _chatEndpointBuffer = userConfig?.EndpointOverride ?? template?.ChatApi?.Endpoint ?? "";
            _chatModelBuffer = userConfig?.ModelOverride ?? template?.ChatApi?.DefaultModel ?? "";
            _chatTemperatureBuffer = userConfig?.Temperature ?? template?.ChatApi?.DefaultParameters?["temperature"]?.Value<float>() ?? 0.7f;
            _chatConcurrencyLimitBuffer = userConfig?.ConcurrencyLimit ?? 5;
            _chatTestStatusMessage = "Test the currently saved chat provider.";
        }
        
        private void HandleChatSave() {
            var config = new ChatUserConfig {
                ApiKey = _chatApiKeyBuffer,
                EndpointOverride = _chatEndpointBuffer,
                ModelOverride = _chatModelBuffer,
                Temperature = _chatTemperatureBuffer,
                ConcurrencyLimit = _chatConcurrencyLimitBuffer
            };
            FrameworkDI.SettingsManager.WriteChatUserConfig(settings.ActiveChatProviderId, config);
            FrameworkDI.SettingsManager.ReloadConfigs();
            settings.Write();
            Messages.Message("Chat settings saved.", MessageTypeDefOf.PositiveEvent);
        }

        // --- 【修复】恢复完整的 Chat 测试方法 ---
        private async void HandleChatTest()
        {
            if (string.IsNullOrWhiteSpace(_chatApiKeyBuffer))
            {
                _chatTestStatusMessage = "API Key is missing.";
                Messages.Message("Cannot test with an empty API Key.", MessageTypeDefOf.CautionInput);
                return;
            }

            _isChatTesting = true;
            _chatTestStatusMessage = "Testing, please wait...";

            try
            {
                var request = new UnifiedChatRequest { Messages = new List<ChatMessage> { new ChatMessage { Role = "user", Content = "Hi" } } };
                using (var cts = new CancellationTokenSource(TimeSpan.FromSeconds(15)))
                {
                    var result = await RimAIApi.GetCompletionAsync(request, cts.Token);
                    if (result.IsSuccess) {
                        _chatTestStatusMessage = $"Success! Response: {result.Value.Message.Content.Truncate(50)}";
                        Messages.Message("Chat connection successful!", MessageTypeDefOf.PositiveEvent);
                    } else {
                        _chatTestStatusMessage = $"Failed: {result.Error}";
                        Messages.Message($"Chat connection failed: {result.Error}", MessageTypeDefOf.NegativeEvent);
                    }
                }
            }
            catch (Exception ex)
            {
                _chatTestStatusMessage = $"Error: {ex.Message}";
                RimAILogger.Error($"Chat test connection failed with exception: {ex}");
                Messages.Message($"An error occurred during chat test: {ex.Message}", MessageTypeDefOf.NegativeEvent);
            }
            finally
            {
                _isChatTesting = false;
            }
        }

        // --- Embedding Specific Logic ---
        private void DrawEmbeddingFields(Listing_Standard listing) {
            listing.Label("API Key:");
            _embeddingApiKeyBuffer = Widgets.TextField(listing.GetRect(30f), _embeddingApiKeyBuffer);
            listing.Label("Endpoint URL:");
            _embeddingEndpointBuffer = Widgets.TextField(listing.GetRect(30f), _embeddingEndpointBuffer);
            listing.Label("Model:");
            _embeddingModelBuffer = Widgets.TextField(listing.GetRect(30f), _embeddingModelBuffer);
        }
        
        private void LoadEmbeddingSettings(string providerId) {
            var userConfig = FrameworkDI.SettingsManager.GetEmbeddingUserConfig(providerId);
            var templateResult = FrameworkDI.SettingsManager.GetMergedEmbeddingConfig(providerId);
            if (!templateResult.IsSuccess) return;
            var template = templateResult.Value.Template;
            _embeddingApiKeyBuffer = userConfig?.ApiKey ?? "";
            _embeddingEndpointBuffer = userConfig?.EndpointOverride ?? template?.EmbeddingApi?.Endpoint ?? "";
            _embeddingModelBuffer = userConfig?.ModelOverride ?? template?.EmbeddingApi?.DefaultModel ?? "";
            _embeddingTestStatusMessage = "Test the currently saved embedding provider.";
        }
        
        private void HandleEmbeddingSave() {
            var config = new EmbeddingUserConfig {
                ApiKey = _embeddingApiKeyBuffer,
                EndpointOverride = _embeddingEndpointBuffer,
                ModelOverride = _embeddingModelBuffer
            };
            FrameworkDI.SettingsManager.WriteEmbeddingUserConfig(settings.ActiveEmbeddingProviderId, config);
            FrameworkDI.SettingsManager.ReloadConfigs();
            settings.Write();
            Messages.Message("Embedding settings saved.", MessageTypeDefOf.PositiveEvent);
        }
        
        private async void HandleEmbeddingTest()
        {
            if (string.IsNullOrWhiteSpace(_embeddingApiKeyBuffer))
            {
                _embeddingTestStatusMessage = "API Key is missing.";
                Messages.Message("Cannot test with an empty API Key.", MessageTypeDefOf.CautionInput);
                return;
            }

            _isEmbeddingTesting = true;
            _embeddingTestStatusMessage = "Testing, please wait...";

            try
            {
                var request = new UnifiedEmbeddingRequest { Inputs = new List<string> { "Test input" } };
                using (var cts = new CancellationTokenSource(TimeSpan.FromSeconds(15)))
                {
                    var result = await RimAIApi.GetEmbeddingsAsync(request, cts.Token);
                    if (result.IsSuccess) {
                        _embeddingTestStatusMessage = $"Success! Received {result.Value.Data.Count} embedding vector(s).";
                        Messages.Message("Embedding connection successful!", MessageTypeDefOf.PositiveEvent);
                    } else {
                        _embeddingTestStatusMessage = $"Failed: {result.Error}";
                        Messages.Message($"Embedding connection failed: {result.Error}", MessageTypeDefOf.NegativeEvent);
                    }
                }
            }
            catch (Exception ex)
            {
                _embeddingTestStatusMessage = $"Error: {ex.Message}";
                RimAILogger.Error($"Embedding test connection failed with exception: {ex}");
                Messages.Message($"An error occurred during embedding test: {ex.Message}", MessageTypeDefOf.NegativeEvent);
            }
            finally
            {
                _isEmbeddingTesting = false;
            }
        }
    }
}