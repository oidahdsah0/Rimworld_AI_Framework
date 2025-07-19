using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using RimAI.Framework.Core;
using RimAI.Framework.LLM.Models;
using Verse;

namespace RimAI.Framework.LLM.Services
{
    /// <summary>
    /// Enhanced Mod service with flexible streaming/non-streaming options
    /// </summary>
    public class ModService : IModService
    {
        private readonly ILLMExecutor _executor;
        private readonly RimAISettings _settings;
        private readonly Dictionary<string, ModConfig> _modConfigs;

        // Simple mod configuration
        private class ModConfig
        {
            public string SystemPrompt { get; set; }
            public string ModelOverride { get; set; }
            public double? TemperatureOverride { get; set; }
        }

        public ModService(ILLMExecutor executor, RimAISettings settings)
        {
            _executor = executor;
            _settings = settings;
            _modConfigs = new Dictionary<string, ModConfig>();
        }

        public async Task<string> SendMessageAsync(string modId, string message, LLMRequestOptions options = null)
        {
            options ??= new LLMRequestOptions();
            var config = GetOrCreateModConfig(modId);

            try
            {
                // Build the full prompt with system context
                var fullPrompt = BuildFullPrompt(config, message);

                if (options.EnableStreaming)
                {
                    // Use streaming but collect all responses
                    var fullResponse = new System.Text.StringBuilder();
                    var tcs = new TaskCompletionSource<bool>();

                    await _executor.ExecuteStreamingRequestAsync(fullPrompt, chunk =>
                    {
                        fullResponse.Append(chunk);
                    }, default);

                    return fullResponse.ToString();
                }
                else
                {
                    return await _executor.ExecuteSingleRequestAsync(fullPrompt, default);
                }
            }
            catch (Exception ex)
            {
                Log.Error($"ModService: Error processing message for mod '{modId}': {ex.Message}");
                return null;
            }
        }

        public async IAsyncEnumerable<string> SendMessageStreamAsync(string modId, string message, LLMRequestOptions options = null)
        {
            options ??= new LLMRequestOptions();
            var config = GetOrCreateModConfig(modId);

            var fullPrompt = BuildFullPrompt(config, message);
            var chunks = new List<string>();
            Exception streamingException = null;

            try
            {
                await _executor.ExecuteStreamingRequestAsync(fullPrompt, chunk =>
                {
                    chunks.Add(chunk);
                }, default);
            }
            catch (Exception ex)
            {
                Log.Error($"ModService: Error in streaming for mod '{modId}': {ex.Message}");
                streamingException = ex;
            }

            // Process results outside try-catch
            if (streamingException != null)
            {
                yield return $"Error: {streamingException.Message}";
            }
            else
            {
                foreach (var chunk in chunks)
                {
                    yield return chunk;
                }
            }
        }

        public async Task<string> SendMessageToModAsync(string modId, string message)
        {
            // Legacy compatibility method
            return await SendMessageAsync(modId, message, new LLMRequestOptions { EnableStreaming = false });
        }

        private ModConfig GetOrCreateModConfig(string modId)
        {
            if (!_modConfigs.TryGetValue(modId, out var config))
            {
                config = new ModConfig
                {
                    SystemPrompt = $"You are an AI assistant for the mod '{modId}'. Provide helpful and relevant responses.",
                    ModelOverride = null,
                    TemperatureOverride = null
                };
                _modConfigs[modId] = config;
            }
            return config;
        }

        private string BuildFullPrompt(ModConfig config, string userMessage)
        {
            if (!string.IsNullOrEmpty(config.SystemPrompt))
            {
                return $"System: {config.SystemPrompt}\n\nUser: {userMessage}";
            }
            return userMessage;
        }

        // Method to allow mods to configure their settings
        public void ConfigureMod(string modId, string systemPrompt, string modelOverride = null, double? temperatureOverride = null)
        {
            _modConfigs[modId] = new ModConfig
            {
                SystemPrompt = systemPrompt,
                ModelOverride = modelOverride,
                TemperatureOverride = temperatureOverride
            };
        }
    }
}
