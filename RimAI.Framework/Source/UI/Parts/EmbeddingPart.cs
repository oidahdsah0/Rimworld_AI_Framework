using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RimAI.Framework.Configuration.Models;
using RimAI.Framework.Contracts;
using RimAI.Framework.Execution;
using RimAI.Framework.Translation;
using RimAI.Framework.Core.Lifecycle;
using RimAI.Framework.Shared.Logging;
using Verse;
using RimWorld;

namespace RimAI.Framework.UI
{
    public partial class RimAIFrameworkMod
    {
        private void DrawEmbeddingFields(Listing_Standard listing)
        {
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

        private void LoadEmbeddingSettings(string providerId)
        {
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
            _embeddingTestStatusMessage = "RimAI.EmbedTestHint";
        }

        private void HandleEmbeddingSave()
        {
            Dictionary<string, string> parsedHeaders = null;
            JObject parsedStaticParams = null;
            try { parsedHeaders = string.IsNullOrWhiteSpace(_embeddingCustomHeadersBuffer) ? null : JsonConvert.DeserializeObject<Dictionary<string, string>>(_embeddingCustomHeadersBuffer); }
            catch (Exception ex) { Messages.Message("RimAI.EmbedHeadersInvalid".Translate(ex.Message), MessageTypeDefOf.NegativeEvent); return; }
            try { parsedStaticParams = string.IsNullOrWhiteSpace(_embeddingStaticParamsBuffer) ? null : JObject.Parse(_embeddingStaticParamsBuffer); }
            catch (Exception ex) { Messages.Message("RimAI.EmbedStaticParamsInvalid".Translate(ex.Message), MessageTypeDefOf.NegativeEvent); return; }

            var config = new EmbeddingUserConfig
            {
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

        private void HandleEmbeddingReset()
        {
            if (string.IsNullOrEmpty(settings.ActiveEmbeddingProviderId)) return;
            try
            {
                var filePath = System.IO.Path.Combine(GenFilePaths.ConfigFolderPath, "RimAI_Framework", $"embedding_config_{settings.ActiveEmbeddingProviderId}.json");
                if (System.IO.File.Exists(filePath)) System.IO.File.Delete(filePath);
                FrameworkDI.SettingsManager.ReloadConfigs();
                LoadEmbeddingSettings(settings.ActiveEmbeddingProviderId);
                Messages.Message("RimAI.EmbedResetSuccess".Translate(), MessageTypeDefOf.PositiveEvent);
            }
            catch (Exception ex)
            {
                Messages.Message("RimAI.EmbedResetFailed".Translate(ex.Message), MessageTypeDefOf.NegativeEvent);
            }
        }

        private async void HandleEmbeddingTest()
        {
            if (string.IsNullOrWhiteSpace(_embeddingApiKeyBuffer))
            {
                _embeddingTestStatusMessage = "RimAI.ApiMissing";
                Messages.Message("RimAI.CannotTestEmptyKey".Translate(), MessageTypeDefOf.CautionInput);
                return;
            }

            _isEmbeddingTesting = true;
            _embeddingTestStatusMessage = "RimAI.Testing";

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
                    CustomHeaders = string.IsNullOrWhiteSpace(_embeddingCustomHeadersBuffer) ? null : JsonConvert.DeserializeObject<Dictionary<string, string>>(_embeddingCustomHeadersBuffer),
                    StaticParametersOverride = string.IsNullOrWhiteSpace(_embeddingStaticParamsBuffer) ? null : JObject.Parse(_embeddingStaticParamsBuffer)
                };

                var mergedConfig = new MergedEmbeddingConfig { Template = template, User = tempUserConfig };

                var translator = new EmbeddingRequestTranslator();
                var unifiedRequest = new UnifiedEmbeddingRequest { Inputs = new System.Collections.Generic.List<string> { "Test input" } };
                var httpRequest = translator.Translate(unifiedRequest, mergedConfig);

                using var cts = new System.Threading.CancellationTokenSource(System.TimeSpan.FromSeconds(15));
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
                finally { httpResult.Value?.Dispose(); }
            }
            catch (System.Exception ex)
            {
                _embeddingTestStatusMessage = $"Error: {ex.Message}";
                RimAILogger.Error($"Embedding test connection failed with exception: {ex}");
                Messages.Message("RimAI.EmbedError".Translate(ex.Message), MessageTypeDefOf.NegativeEvent);
            }
            finally { _isEmbeddingTesting = false; }
        }
    }
}
