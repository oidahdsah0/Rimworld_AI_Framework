using System;
using System.Collections.Generic;
using System.Threading;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RimAI.Framework.API;
using RimAI.Framework.Configuration.Models;
using RimAI.Framework.Contracts;
using RimAI.Framework.Execution;
using RimAI.Framework.Translation;
using RimAI.Framework.Core.Lifecycle;
using RimAI.Framework.Shared.Logging;
using UnityEngine;
using Verse;
using RimWorld;

namespace RimAI.Framework.UI
{
    public partial class RimAIFrameworkMod
    {
        private void DrawChatFields(Listing_Standard listing)
        {
            _chatApiKeyBuffer = Widgets.TextField(listing.GetRect(30f), _chatApiKeyBuffer);
            listing.Gap(5f);
            listing.Label("RimAI.Endpoint".Translate());
            _chatEndpointBuffer = Widgets.TextField(listing.GetRect(30f), _chatEndpointBuffer);
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

        private void LoadChatSettings(string providerId)
        {
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
            _chatTestStatusMessage = "RimAI.ChatTestHint";
        }

        private void HandleChatSave()
        {
            Dictionary<string, string> parsedHeaders = null;
            JObject parsedStaticParams = null;
            try { parsedHeaders = string.IsNullOrWhiteSpace(_chatCustomHeadersBuffer) ? null : JsonConvert.DeserializeObject<Dictionary<string, string>>(_chatCustomHeadersBuffer); }
            catch (Exception ex) { Messages.Message("RimAI.ChatHeadersInvalid".Translate(ex.Message), MessageTypeDefOf.NegativeEvent); return; }
            try { parsedStaticParams = string.IsNullOrWhiteSpace(_chatStaticParamsBuffer) ? null : JObject.Parse(_chatStaticParamsBuffer); }
            catch (Exception ex) { Messages.Message("RimAI.ChatStaticParamsInvalid".Translate(ex.Message), MessageTypeDefOf.NegativeEvent); return; }

            var config = new ChatUserConfig
            {
                ApiKey = _chatApiKeyBuffer,
                EndpointOverride = _chatEndpointBuffer,
                ModelOverride = _chatModelBuffer,
                Temperature = _chatTemperatureBuffer,
                TopP = _chatTopPBuffer,
                TypicalP = _chatTypicalPBuffer,
                MaxTokens = _chatMaxTokensBuffer == 0 ? (int?)null : _chatMaxTokensBuffer,
                ConcurrencyLimit = _chatConcurrencyLimitBuffer,
                CustomHeaders = parsedHeaders,
                StaticParametersOverride = parsedStaticParams
            };
            FrameworkDI.SettingsManager.WriteChatUserConfig(settings.ActiveChatProviderId, config);
            FrameworkDI.SettingsManager.ReloadConfigs();
            settings.Write();
            Messages.Message("RimAI.ChatSaved".Translate(), MessageTypeDefOf.PositiveEvent);
        }

        private void HandleChatReset()
        {
            if (string.IsNullOrEmpty(settings.ActiveChatProviderId)) return;
            try
            {
                var filePath = System.IO.Path.Combine(GenFilePaths.ConfigFolderPath, "RimAI_Framework", $"chat_config_{settings.ActiveChatProviderId}.json");
                if (System.IO.File.Exists(filePath)) System.IO.File.Delete(filePath);
                FrameworkDI.SettingsManager.ReloadConfigs();
                LoadChatSettings(settings.ActiveChatProviderId);
                Messages.Message("RimAI.ChatResetSuccess".Translate(), MessageTypeDefOf.PositiveEvent);
            }
            catch (Exception ex)
            {
                Messages.Message("RimAI.ChatResetFailed".Translate(ex.Message), MessageTypeDefOf.NegativeEvent);
            }
        }

        private async void HandleChatTest()
        {
            if (string.IsNullOrWhiteSpace(_chatApiKeyBuffer))
            {
                _chatTestStatusMessage = "RimAI.ApiMissing";
                Messages.Message("RimAI.CannotTestEmptyKey".Translate(), MessageTypeDefOf.CautionInput);
                return;
            }

            _isChatTesting = true;
            _chatTestStatusMessage = "RimAI.Testing";

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
                    CustomHeaders = string.IsNullOrWhiteSpace(_chatCustomHeadersBuffer) ? null : JsonConvert.DeserializeObject<Dictionary<string, string>>(_chatCustomHeadersBuffer),
                    StaticParametersOverride = string.IsNullOrWhiteSpace(_chatStaticParamsBuffer) ? null : JObject.Parse(_chatStaticParamsBuffer)
                };

                var mergedConfig = new MergedChatConfig { Template = template, User = tempUserConfig };

                var translator = new ChatRequestTranslator();
                var unifiedRequest = new UnifiedChatRequest
                {
                    ConversationId = "__preview__",
                    Messages = new System.Collections.Generic.List<ChatMessage> { new ChatMessage { Role = "user", Content = "Hi" } }
                };
                var httpRequest = translator.Translate(unifiedRequest, mergedConfig);

                using var cts = new System.Threading.CancellationTokenSource(TimeSpan.FromSeconds(15));
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
                finally { httpResult.Value?.Dispose(); }
            }
            catch (Exception ex)
            {
                _chatTestStatusMessage = $"Error: {ex.Message}";
                RimAILogger.Error($"Chat test connection failed with exception: {ex}");
                Messages.Message("RimAI.ChatError".Translate(ex.Message), MessageTypeDefOf.NegativeEvent);
            }
            finally { _isChatTesting = false; }
        }
    }
}
