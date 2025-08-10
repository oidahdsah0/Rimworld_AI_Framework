using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RimAI.Framework.API;
using RimAI.Framework.Configuration;
using RimAI.Framework.Configuration.Models;
using RimAI.Framework.Core.Lifecycle;
using RimAI.Framework.Shared.Logging;
using RimAI.Framework.Contracts;
using RimAI.Framework.Execution;
using RimAI.Framework.Translation;
using System.IO;
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
        private float _chatTopPBuffer = 1.0f;
        private float _chatTypicalPBuffer = 1.0f;
        private int _chatMaxTokensBuffer = 0;
        private int _chatConcurrencyLimitBuffer = 5;
        private string _chatCustomHeadersBuffer = "";
        private string _chatStaticParamsBuffer = "";
        private string _lastChatProviderId = null;
        private bool _isChatTesting = false;
        private string _chatTestStatusMessage = "RimAI.ChatTestHint";

        // Embedding
        private string _embeddingApiKeyBuffer = "";
        private string _embeddingModelBuffer = "";
        private string _embeddingEndpointBuffer = "";
        private string _embeddingCustomHeadersBuffer = "";
        private string _embeddingStaticParamsBuffer = "";
        private string _lastEmbeddingProviderId = null;
        private bool _isEmbeddingTesting = false;
        private string _embeddingTestStatusMessage = "RimAI.EmbedTestHint";
        private int _embeddingConcurrencyLimitBuffer = 4;

        private Vector2 _scrollPosition = Vector2.zero;
        private float _viewHeight = 1500f;
        // Cache UI buffers
        private bool _cacheEnabledBuffer = true;
        private int _cacheTtlBuffer = 120;
        // HTTP timeout UI buffer
        private int _httpTimeoutBuffer = 100;
        // One-time init guard for Network & Cache UI buffers
        private bool _netCacheUIInited = false;

        public RimAIFrameworkMod(ModContentPack content) : base(content)
        {
            FrameworkDI.Assemble();
            settings = GetSettings<RimAIFrameworkSettings>();
        }

        public override string SettingsCategory() => "RimAI.SettingsCategory".Translate();

        public override void DoSettingsWindowContents(Rect inRect)
        {
            // 确保语言系统已就绪后再翻译状态提示
            if (_chatTestStatusMessage == "RimAI.ChatTestHint") _chatTestStatusMessage = "RimAI.ChatTestHint".Translate();
            if (_embeddingTestStatusMessage == "RimAI.EmbedTestHint") _embeddingTestStatusMessage = "RimAI.EmbedTestHint".Translate();

            var listing = new Listing_Standard();

            // 定义可滚动内容区域，宽度减 16f 预留滚动条
            Rect viewRect = new Rect(0f, 0f, inRect.width - 16f, _viewHeight);
            Widgets.BeginScrollView(inRect, ref _scrollPosition, viewRect);

            listing.Begin(viewRect);

            // 第一排：保存/重置/测试（显示在“聊天服务”标题的上一行）
            Rect topBtnRect = listing.GetRect(30f);
            float topBtnW = topBtnRect.width / 3f - 6f;
            if (Widgets.ButtonText(new Rect(topBtnRect.x, topBtnRect.y, topBtnW, 30f), "RimAI.Save".Translate()))
                HandleCombinedSave();
            if (Widgets.ButtonText(new Rect(topBtnRect.x + topBtnW + 4f, topBtnRect.y, topBtnW, 30f), "RimAI.Reset".Translate()))
                HandleCombinedReset();
            if (Widgets.ButtonText(new Rect(topBtnRect.x + 2 * (topBtnW + 4f), topBtnRect.y, topBtnW, 30f), "RimAI.Test".Translate(), active: !_isChatTesting && !_isEmbeddingTesting))
                HandleCombinedTest();
            listing.Gap(6f);
            // 顶部按钮下方显示 Chat/Embedding 的返回信息
            listing.Label(_chatTestStatusMessage);
            listing.Label(_embeddingTestStatusMessage);
            listing.Gap(8f);

            // ----- Chat 区域 -----
            DrawSection(listing, "RimAI.ChatService".Translate(), settings.ActiveChatProviderId, ref _lastChatProviderId,
                (newId) => {
                    settings.ActiveChatProviderId = newId;
                    // 自动同步 Embedding 供应商
                    settings.ActiveEmbeddingProviderId = newId;
                },
                FrameworkDI.SettingsManager.GetAllChatProviderIds(),
                LoadChatSettings, DrawChatFields);

            listing.GapLine(24f);

            // ----- Embedding 区域 -----
            DrawSection(listing, "RimAI.EmbeddingService".Translate(), settings.ActiveEmbeddingProviderId, ref _lastEmbeddingProviderId,
                (newId) => settings.ActiveEmbeddingProviderId = newId,
                FrameworkDI.SettingsManager.GetAllEmbeddingProviderIds(),
                LoadEmbeddingSettings, DrawEmbeddingFields);


            // 记录内容高度并限制滚动范围
            // ----- Network & Cache 区域与操作按钮 -----
            listing.GapLine(24f);
            // Initialize network/cache buffers once per window open
            if (!_netCacheUIInited)
            {
                _cacheEnabledBuffer = settings.CacheEnabled;
                _cacheTtlBuffer = settings.CacheTtlSeconds;
                _httpTimeoutBuffer = settings.HttpTimeoutSeconds;
                _netCacheUIInited = true;
            }
            DrawCacheSection(listing);
            listing.Gap(8f);
            DrawHttpSection(listing);
            listing.GapLine(24f);
            Rect btnRect = listing.GetRect(30f);
            float btnW = btnRect.width / 2f - 4f;
            if (Widgets.ButtonText(new Rect(btnRect.x, btnRect.y, btnW, 30f), "RimAI.NetworkCacheSaveBtn".Translate()))
                HandleNetworkCacheSave();
            if (Widgets.ButtonText(new Rect(btnRect.x + btnW + 4f, btnRect.y, btnW, 30f), "RimAI.NetworkCacheResetBtn".Translate()))
                HandleNetworkCacheReset();
            listing.Gap(8f);

            if (Event.current.type == EventType.Layout)
            {
                _viewHeight = Mathf.Max(_viewHeight, listing.CurHeight);
                float maxScroll = Mathf.Max(0f, _viewHeight - inRect.height);
                _scrollPosition.y = Mathf.Clamp(_scrollPosition.y, 0f, maxScroll);
            }

            listing.End();
            Widgets.EndScrollView();
        }

        private void DrawSection(
            Listing_Standard listing,
            string title,
            string activeProviderId,
            ref string lastProviderId,
            Action<string> setActiveProviderId,
            IEnumerable<string> providerIds,
            Action<string> loadAction,
            Action<Listing_Standard> drawFieldsAction)
        {
            listing.Label(title);
            listing.Gap(4f);
            
            var providerIdList = providerIds.ToList();
            if (!providerIdList.Any()) { listing.Label("RimAI.NoProviders".Translate()); return; }
            string currentLabel = string.IsNullOrEmpty(activeProviderId) ? "RimAI.SelectProvider".Translate() : activeProviderId.CapitalizeFirst();
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
            if (string.IsNullOrEmpty(activeProviderId)) { listing.Label("RimAI.PlsSelectProvider".Translate()); return; }
            listing.Gap(12f);
            drawFieldsAction(listing);
        }

        // --- Chat Specific Logic ---
        private void DrawChatFields(Listing_Standard listing) {
            listing.Label("RimAI.ApiKey".Translate());
            _chatApiKeyBuffer = Widgets.TextField(listing.GetRect(30f), _chatApiKeyBuffer);
            listing.Gap(5f);
            listing.Label("RimAI.Endpoint".Translate());
            _chatEndpointBuffer = Widgets.TextField(listing.GetRect(30f), _chatEndpointBuffer);
            listing.Gap(5f);
            listing.Label("RimAI.Model".Translate());
            _chatModelBuffer = Widgets.TextField(listing.GetRect(30f), _chatModelBuffer);
            listing.Gap(5f);
            listing.Label("RimAI.Temperature".Translate(_chatTemperatureBuffer.ToString("F2")));
            _chatTemperatureBuffer = listing.Slider(_chatTemperatureBuffer, 0f, 2.0f);
            listing.Gap(5f);
            listing.Label("RimAI.TopP".Translate(_chatTopPBuffer.ToString("F2")));
            _chatTopPBuffer = listing.Slider(_chatTopPBuffer, 0f, 1.0f);
            listing.Gap(5f);
            listing.Label("RimAI.TypicalP".Translate(_chatTypicalPBuffer.ToString("F2")));
            _chatTypicalPBuffer = listing.Slider(_chatTypicalPBuffer, 0f, 1.0f);
            listing.Gap(5f);
            listing.Label("RimAI.MaxTokens".Translate(_chatMaxTokensBuffer.ToString()));
            _chatMaxTokensBuffer = (int)listing.Slider(_chatMaxTokensBuffer, 0, 8192);
            listing.Gap(5f);
            listing.Label("RimAI.Concurrency".Translate(_chatConcurrencyLimitBuffer.ToString()));
            _chatConcurrencyLimitBuffer = (int)listing.Slider(_chatConcurrencyLimitBuffer, 1, 20);
            listing.Gap(5f);
            listing.Label("RimAI.CustomHeaders".Translate());
            _chatCustomHeadersBuffer = Widgets.TextField(listing.GetRect(30f), _chatCustomHeadersBuffer);
            listing.Gap(5f);
            listing.Label("RimAI.StaticParamsOverride".Translate());
            _chatStaticParamsBuffer = Widgets.TextField(listing.GetRect(30f), _chatStaticParamsBuffer);
            listing.Gap(5f);
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
            _chatTopPBuffer = userConfig?.TopP ?? template?.ChatApi?.DefaultParameters?["top_p"]?.Value<float>() ?? 1.0f;
            _chatTypicalPBuffer = userConfig?.TypicalP ?? template?.ChatApi?.DefaultParameters?["typical_p"]?.Value<float>() ?? 1.0f;
            _chatMaxTokensBuffer = userConfig?.MaxTokens ?? template?.ChatApi?.DefaultParameters?["max_tokens"]?.Value<int>() ?? 300;
            _chatConcurrencyLimitBuffer = userConfig?.ConcurrencyLimit ?? 5;
            _chatCustomHeadersBuffer = userConfig?.CustomHeaders != null ? JsonConvert.SerializeObject(userConfig.CustomHeaders, Formatting.None) : "";
            _chatStaticParamsBuffer = userConfig?.StaticParametersOverride != null ? userConfig.StaticParametersOverride.ToString(Formatting.None) : "";
            _chatTestStatusMessage = "RimAI.ChatTestHint".Translate();
        }
        
        private void HandleChatSave() {
            Dictionary<string,string> parsedHeaders = null;
            JObject parsedStaticParams = null;
            try { parsedHeaders = string.IsNullOrWhiteSpace(_chatCustomHeadersBuffer) ? null : JsonConvert.DeserializeObject<Dictionary<string,string>>(_chatCustomHeadersBuffer); }
            catch (Exception ex) { Messages.Message("RimAI.ChatHeadersInvalid".Translate(ex.Message), MessageTypeDefOf.NegativeEvent); return; }
            try { parsedStaticParams = string.IsNullOrWhiteSpace(_chatStaticParamsBuffer) ? null : JObject.Parse(_chatStaticParamsBuffer); }
            catch (Exception ex) { Messages.Message("RimAI.ChatStaticParamsInvalid".Translate(ex.Message), MessageTypeDefOf.NegativeEvent); return; }

            var config = new ChatUserConfig {
                ApiKey = _chatApiKeyBuffer,
                EndpointOverride = _chatEndpointBuffer,
                ModelOverride = _chatModelBuffer,
                Temperature = _chatTemperatureBuffer,
                TopP = _chatTopPBuffer,
                TypicalP = _chatTypicalPBuffer,
                MaxTokens = _chatMaxTokensBuffer == 0 ? null : (int?)_chatMaxTokensBuffer,
                ConcurrencyLimit = _chatConcurrencyLimitBuffer,
                CustomHeaders = parsedHeaders,
                StaticParametersOverride = parsedStaticParams
            };
            FrameworkDI.SettingsManager.WriteChatUserConfig(settings.ActiveChatProviderId, config);
            FrameworkDI.SettingsManager.ReloadConfigs();
            settings.Write();
            Messages.Message("RimAI.ChatSaved".Translate(), MessageTypeDefOf.PositiveEvent);
        }

        // --- 新增 Chat Reset ---
        private void HandleChatReset()
        {
            if (string.IsNullOrEmpty(settings.ActiveChatProviderId)) return;
            try
            {
                var filePath = Path.Combine(GenFilePaths.ConfigFolderPath, "RimAI_Framework", $"chat_config_{settings.ActiveChatProviderId}.json");
                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                }
                FrameworkDI.SettingsManager.ReloadConfigs();
                LoadChatSettings(settings.ActiveChatProviderId);
                Messages.Message("RimAI.ChatResetSuccess".Translate(), MessageTypeDefOf.PositiveEvent);
            }
            catch (Exception ex)
            {
                Messages.Message("RimAI.ChatResetFailed".Translate(ex.Message), MessageTypeDefOf.NegativeEvent);
            }
        }

        // --- 【修复】恢复完整的 Chat 测试方法 ---
        private async void HandleChatTestLegacy()
        {
            if (string.IsNullOrWhiteSpace(_chatApiKeyBuffer))
            {
                _chatTestStatusMessage = "RimAI.ApiMissing".Translate();
                Messages.Message("RimAI.CannotTestEmptyKey".Translate(), MessageTypeDefOf.CautionInput);
                return;
            }

            _isChatTesting = true;
            _chatTestStatusMessage = "RimAI.Testing".Translate();

            try
            {
                var request = new UnifiedChatRequest { ConversationId = "__preview__", Messages = new List<ChatMessage> { new ChatMessage { Role = "user", Content = "Hi" } } };
                using (var cts = new CancellationTokenSource(TimeSpan.FromSeconds(15)))
                {
                    var result = await RimAIApi.GetCompletionAsync(request, cts.Token);
                    if (result.IsSuccess) {
                        _chatTestStatusMessage = $"Success! Response: {result.Value.Message.Content.Truncate(50)}";
                        Messages.Message("RimAI.ChatSuccess".Translate(), MessageTypeDefOf.PositiveEvent);
                    } else {
                        _chatTestStatusMessage = $"Failed: {result.Error}";
                        Messages.Message("RimAI.ChatFailed".Translate(result.Error), MessageTypeDefOf.NegativeEvent);
                    }
                }
            }
            catch (Exception ex)
            {
                _chatTestStatusMessage = $"Error: {ex.Message}";
                RimAILogger.Error($"Chat test connection failed with exception: {ex}");
                Messages.Message("RimAI.ChatError".Translate(ex.Message), MessageTypeDefOf.NegativeEvent);
            }
            finally
            {
                _isChatTesting = false;
            }
        }

        // --- Embedding Specific Logic ---
        private void DrawEmbeddingFields(Listing_Standard listing) {
            listing.Label("RimAI.ApiKey".Translate());
            _embeddingApiKeyBuffer = Widgets.TextField(listing.GetRect(30f), _embeddingApiKeyBuffer);
            listing.Gap(5f);
            listing.Label("RimAI.Endpoint".Translate());
            _embeddingEndpointBuffer = Widgets.TextField(listing.GetRect(30f), _embeddingEndpointBuffer);
            listing.Gap(5f);
            listing.Label("RimAI.Model".Translate());
            _embeddingModelBuffer = Widgets.TextField(listing.GetRect(30f), _embeddingModelBuffer);
            listing.Gap(5f);
            listing.Label("RimAI.CustomHeaders".Translate());
            _embeddingCustomHeadersBuffer = Widgets.TextField(listing.GetRect(30f), _embeddingCustomHeadersBuffer);
            listing.Gap(5f);
            listing.Label("RimAI.StaticParamsOverride".Translate());
            _embeddingStaticParamsBuffer = Widgets.TextField(listing.GetRect(30f), _embeddingStaticParamsBuffer);
            listing.Gap(5f);
            listing.Label("RimAI.EmbedConcurrency".Translate(_embeddingConcurrencyLimitBuffer.ToString()));
            _embeddingConcurrencyLimitBuffer = (int)listing.Slider(_embeddingConcurrencyLimitBuffer, 1, 20);
            listing.Gap(5f);
        }
        
        private void LoadEmbeddingSettings(string providerId) {
            var userConfig = FrameworkDI.SettingsManager.GetEmbeddingUserConfig(providerId);
            var templateResult = FrameworkDI.SettingsManager.GetMergedEmbeddingConfig(providerId);
            if (!templateResult.IsSuccess) return;
            var template = templateResult.Value.Template;
            _embeddingApiKeyBuffer = userConfig?.ApiKey ?? "";
            _embeddingEndpointBuffer = userConfig?.EndpointOverride ?? template?.EmbeddingApi?.Endpoint ?? "";
            _embeddingModelBuffer = userConfig?.ModelOverride ?? template?.EmbeddingApi?.DefaultModel ?? "";
            _embeddingCustomHeadersBuffer = userConfig?.CustomHeaders != null ? JsonConvert.SerializeObject(userConfig.CustomHeaders, Formatting.None) : "";
            _embeddingStaticParamsBuffer = userConfig?.StaticParametersOverride != null ? userConfig.StaticParametersOverride.ToString(Formatting.None) : "";
            _embeddingConcurrencyLimitBuffer = userConfig?.ConcurrencyLimit ?? 4;
            _embeddingTestStatusMessage = "RimAI.EmbedTestHint".Translate();
        }

        // --- Cache Section ---
        private void DrawCacheSection(Listing_Standard listing)
        {
            listing.Label("RimAI.CacheSettings".Translate());
            listing.Gap(4f);
            // enable toggle
            var row = listing.GetRect(24f);
            Widgets.CheckboxLabeled(row, "RimAI.CacheEnabled".Translate(), ref _cacheEnabledBuffer);
            listing.Gap(6f);
            // TTL slider
            listing.Label("RimAI.CacheTtl".Translate(_cacheTtlBuffer.ToString()));
            _cacheTtlBuffer = (int)listing.Slider(_cacheTtlBuffer, 10, 3600);
        }

        // --- HTTP Settings Section ---
        private void DrawHttpSection(Listing_Standard listing)
        {
            listing.Label("RimAI.HttpSettings".Translate());
            listing.Gap(4f);
            listing.Label("RimAI.HttpTimeout".Translate(_httpTimeoutBuffer.ToString()));
            _httpTimeoutBuffer = (int)listing.Slider(_httpTimeoutBuffer, 5, 3600);
        }
        
        private void HandleEmbeddingSave() {
            Dictionary<string,string> parsedHeaders = null;
            JObject parsedStaticParams = null;
            try { parsedHeaders = string.IsNullOrWhiteSpace(_embeddingCustomHeadersBuffer) ? null : JsonConvert.DeserializeObject<Dictionary<string,string>>(_embeddingCustomHeadersBuffer); }
            catch (Exception ex) { Messages.Message("RimAI.EmbedHeadersInvalid".Translate(ex.Message), MessageTypeDefOf.NegativeEvent); return; }
            try { parsedStaticParams = string.IsNullOrWhiteSpace(_embeddingStaticParamsBuffer) ? null : JObject.Parse(_embeddingStaticParamsBuffer); }
            catch (Exception ex) { Messages.Message("RimAI.EmbedStaticParamsInvalid".Translate(ex.Message), MessageTypeDefOf.NegativeEvent); return; }

            var config = new EmbeddingUserConfig {
                ApiKey = _embeddingApiKeyBuffer,
                EndpointOverride = _embeddingEndpointBuffer,
                ModelOverride = _embeddingModelBuffer,
                CustomHeaders = parsedHeaders,
                StaticParametersOverride = parsedStaticParams,
                ConcurrencyLimit = _embeddingConcurrencyLimitBuffer
            };
            FrameworkDI.SettingsManager.WriteEmbeddingUserConfig(settings.ActiveEmbeddingProviderId, config);
            FrameworkDI.SettingsManager.ReloadConfigs();
            settings.Write();
            Messages.Message("RimAI.EmbedSaved".Translate(), MessageTypeDefOf.PositiveEvent);
        }

        // --- 新增 Embedding Reset ---
        private void HandleEmbeddingReset()
        {
            if (string.IsNullOrEmpty(settings.ActiveEmbeddingProviderId)) return;
            try
            {
                var filePath = Path.Combine(GenFilePaths.ConfigFolderPath, "RimAI_Framework", $"embedding_config_{settings.ActiveEmbeddingProviderId}.json");
                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                }
                FrameworkDI.SettingsManager.ReloadConfigs();
                LoadEmbeddingSettings(settings.ActiveEmbeddingProviderId);
                Messages.Message("RimAI.EmbedResetSuccess".Translate(), MessageTypeDefOf.PositiveEvent);
            }
            catch (Exception ex)
            {
                Messages.Message("RimAI.EmbedResetFailed".Translate(ex.Message), MessageTypeDefOf.NegativeEvent);
            }
        }
        
        private async void HandleEmbeddingTestLegacy()
        {
            if (string.IsNullOrWhiteSpace(_embeddingApiKeyBuffer))
            {
                _embeddingTestStatusMessage = "RimAI.ApiMissing".Translate();
                Messages.Message("RimAI.CannotTestEmptyKey".Translate(), MessageTypeDefOf.CautionInput);
                return;
            }

            _isEmbeddingTesting = true;
            _embeddingTestStatusMessage = "RimAI.Testing".Translate();

            try
            {
                var request = new UnifiedEmbeddingRequest { Inputs = new List<string> { "Test input" } };
                using (var cts = new CancellationTokenSource(TimeSpan.FromSeconds(15)))
                {
                    var result = await RimAIApi.GetEmbeddingsAsync(request, cts.Token);
                    if (result.IsSuccess) {
                        _embeddingTestStatusMessage = $"Success! Received {result.Value.Data.Count} embedding vector(s).";
                        Messages.Message("RimAI.EmbedSuccess".Translate(), MessageTypeDefOf.PositiveEvent);
                    } else {
                        _embeddingTestStatusMessage = $"Failed: {result.Error}";
                        Messages.Message("RimAI.EmbedFailed".Translate(result.Error), MessageTypeDefOf.NegativeEvent);
                    }
                }
            }
            catch (Exception ex)
            {
                _embeddingTestStatusMessage = $"Error: {ex.Message}";
                RimAILogger.Error($"Embedding test connection failed with exception: {ex}");
                Messages.Message("RimAI.EmbedError".Translate(ex.Message), MessageTypeDefOf.NegativeEvent);
            }
            finally
            {
                _isEmbeddingTesting = false;
            }
        }

        // --- 新版 Chat 测试方法 ---
        private async void HandleChatTest()
        {
            if (string.IsNullOrWhiteSpace(_chatApiKeyBuffer))
            {
                _chatTestStatusMessage = "RimAI.ApiMissing".Translate();
                Messages.Message("RimAI.CannotTestEmptyKey".Translate(), MessageTypeDefOf.CautionInput);
                return;
            }

            _isChatTesting = true;
            _chatTestStatusMessage = "RimAI.Testing".Translate();

            try
            {
                var templateResult = FrameworkDI.SettingsManager.GetMergedChatConfig(settings.ActiveChatProviderId);
                if (!templateResult.IsSuccess)
                {
                    _chatTestStatusMessage = templateResult.Error;
                    Messages.Message(templateResult.Error, MessageTypeDefOf.NegativeEvent);
                    return;
                }

                var template = templateResult.Value.Template;

                var tempUserConfig = new ChatUserConfig
                {
                    ApiKey = _chatApiKeyBuffer,
                    EndpointOverride = _chatEndpointBuffer,
                    ModelOverride = _chatModelBuffer,
                    Temperature = _chatTemperatureBuffer,
                    TopP = _chatTopPBuffer,
                    TypicalP = _chatTypicalPBuffer,
                    MaxTokens = _chatMaxTokensBuffer == 0 ? null : (int?)_chatMaxTokensBuffer,
                    ConcurrencyLimit = _chatConcurrencyLimitBuffer,
                    CustomHeaders = string.IsNullOrWhiteSpace(_chatCustomHeadersBuffer) ? null : JsonConvert.DeserializeObject<Dictionary<string,string>>(_chatCustomHeadersBuffer),
                    StaticParametersOverride = string.IsNullOrWhiteSpace(_chatStaticParamsBuffer) ? null : JObject.Parse(_chatStaticParamsBuffer)
                };

                var mergedConfig = new MergedChatConfig { Template = template, User = tempUserConfig };

                var translator = new ChatRequestTranslator();
                var unifiedRequest = new UnifiedChatRequest
                {
                    ConversationId = "__preview__",
                    Messages = new List<ChatMessage> { new ChatMessage { Role = "user", Content = "Hi" } }
                };
                var httpRequest = translator.Translate(unifiedRequest, mergedConfig);

                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(15));
                var executor = new HttpExecutor();
                var httpResult = await executor.ExecuteAsync(httpRequest, cts.Token);

                if (!httpResult.IsSuccess)
                {
                    _chatTestStatusMessage = $"Failed: {httpResult.Error}";
                    Messages.Message("RimAI.ChatFailed".Translate(httpResult.Error), MessageTypeDefOf.NegativeEvent);
                    return;
                }

                var responseTranslator = new ChatResponseTranslator();
                try
                {
                    var unifiedResponse = await responseTranslator.TranslateAsync(httpResult.Value, mergedConfig, cts.Token);

                    if (!string.IsNullOrEmpty(unifiedResponse?.Message?.Content))
                    {
                        _chatTestStatusMessage = $"Success! Response: {unifiedResponse.Message.Content.Truncate(50)}";
                        Messages.Message("RimAI.ChatSuccess".Translate(), MessageTypeDefOf.PositiveEvent);
                    }
                    else
                    {
                        _chatTestStatusMessage = "Failed: Empty response";
                        Messages.Message("RimAI.ChatFailed".Translate("Empty response"), MessageTypeDefOf.NegativeEvent);
                    }
                }
                finally
                {
                    httpResult.Value?.Dispose();
                }
            }
            catch (Exception ex)
            {
                _chatTestStatusMessage = $"Error: {ex.Message}";
                RimAILogger.Error($"Chat test connection failed with exception: {ex}");
                Messages.Message("RimAI.ChatError".Translate(ex.Message), MessageTypeDefOf.NegativeEvent);
            }
            finally
            {
                _isChatTesting = false;
            }
        }

        // --- 新版 Embedding 测试方法 ---
        private async void HandleEmbeddingTest()
        {
            if (string.IsNullOrWhiteSpace(_embeddingApiKeyBuffer))
            {
                _embeddingTestStatusMessage = "RimAI.ApiMissing".Translate();
                Messages.Message("RimAI.CannotTestEmptyKey".Translate(), MessageTypeDefOf.CautionInput);
                return;
            }

            _isEmbeddingTesting = true;
            _embeddingTestStatusMessage = "RimAI.Testing".Translate();

            try
            {
                var templateResult = FrameworkDI.SettingsManager.GetMergedEmbeddingConfig(settings.ActiveEmbeddingProviderId);
                if (!templateResult.IsSuccess)
                {
                    _embeddingTestStatusMessage = templateResult.Error;
                    Messages.Message(templateResult.Error, MessageTypeDefOf.NegativeEvent);
                    return;
                }

                var template = templateResult.Value.Template;

                var tempUserConfig = new EmbeddingUserConfig
                {
                    ApiKey = _embeddingApiKeyBuffer,
                    EndpointOverride = _embeddingEndpointBuffer,
                    ModelOverride = _embeddingModelBuffer,
                    CustomHeaders = string.IsNullOrWhiteSpace(_embeddingCustomHeadersBuffer) ? null : JsonConvert.DeserializeObject<Dictionary<string,string>>(_embeddingCustomHeadersBuffer),
                    StaticParametersOverride = string.IsNullOrWhiteSpace(_embeddingStaticParamsBuffer) ? null : JObject.Parse(_embeddingStaticParamsBuffer)
                };

                var mergedConfig = new MergedEmbeddingConfig { Template = template, User = tempUserConfig };

                var translator = new EmbeddingRequestTranslator();
                var unifiedRequest = new UnifiedEmbeddingRequest { Inputs = new List<string> { "Test input" } };
                var httpRequest = translator.Translate(unifiedRequest, mergedConfig);

                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(15));
                var executor = new HttpExecutor();
                var httpResult = await executor.ExecuteAsync(httpRequest, cts.Token);

                if (!httpResult.IsSuccess)
                {
                    _embeddingTestStatusMessage = $"Failed: {httpResult.Error}";
                    Messages.Message("RimAI.EmbedFailed".Translate(httpResult.Error), MessageTypeDefOf.NegativeEvent);
                    return;
                }

                var responseTranslator = new EmbeddingResponseTranslator();
                try
                {
                    var parseResult = await responseTranslator.TranslateAsync(httpResult.Value, mergedConfig, cts.Token);

                    if (parseResult.IsSuccess)
                    {
                        _embeddingTestStatusMessage = $"Success! Received {parseResult.Value.Data.Count} embedding vector(s).";
                        Messages.Message("RimAI.EmbedSuccess".Translate(), MessageTypeDefOf.PositiveEvent);
                    }
                    else
                    {
                        _embeddingTestStatusMessage = $"Failed: {parseResult.Error}";
                        Messages.Message("RimAI.EmbedFailed".Translate(parseResult.Error), MessageTypeDefOf.NegativeEvent);
                    }
                }
                finally
                {
                    httpResult.Value?.Dispose();
                }
            }
            catch (Exception ex)
            {
                _embeddingTestStatusMessage = $"Error: {ex.Message}";
                RimAILogger.Error($"Embedding test connection failed with exception: {ex}");
                Messages.Message("RimAI.EmbedError".Translate(ex.Message), MessageTypeDefOf.NegativeEvent);
            }
            finally
            {
                _isEmbeddingTesting = false;
            }
        }

        // --- Combined Chat + Embedding 操作 ---
        private void HandleCombinedSave()
        {
            // 保存 Chat 设置
            HandleChatSave();
            // 如 Embedding Key 为空，则同步 Chat Key 和 Provider
            if (string.IsNullOrWhiteSpace(_embeddingApiKeyBuffer))
            {
                settings.ActiveEmbeddingProviderId = settings.ActiveChatProviderId;
                _embeddingApiKeyBuffer = _chatApiKeyBuffer;
                Messages.Message("RimAI.EmbeddingAutoSync".Translate(), MessageTypeDefOf.CautionInput);
            }
            // 保存 Embedding 设置
            HandleEmbeddingSave();
        }

        private void HandleNetworkCacheSave()
        {
            settings.CacheEnabled = _cacheEnabledBuffer;
            settings.CacheTtlSeconds = _cacheTtlBuffer;
            settings.HttpTimeoutSeconds = _httpTimeoutBuffer;
            settings.Write();
            HttpClientFactory.ApplyConfiguredTimeout();
            Messages.Message("RimAI.NetworkCacheSaved".Translate(), MessageTypeDefOf.PositiveEvent);
        }

        private void HandleNetworkCacheReset()
        {
            _cacheEnabledBuffer = true;
            _cacheTtlBuffer = 120;
            _httpTimeoutBuffer = 100;
            settings.CacheEnabled = _cacheEnabledBuffer;
            settings.CacheTtlSeconds = _cacheTtlBuffer;
            settings.HttpTimeoutSeconds = _httpTimeoutBuffer;
            settings.Write();
            HttpClientFactory.ApplyConfiguredTimeout();
            Messages.Message("RimAI.NetworkCacheReset".Translate(), MessageTypeDefOf.PositiveEvent);
        }

        private void HandleCombinedReset()
        {
            HandleChatReset();
            HandleEmbeddingReset();
        }

        private void HandleCombinedTest()
        {
            HandleChatTest();
            HandleEmbeddingTest();
        }
    }
}