using System;
using System.Threading;
using System.Threading.Tasks;
using RimAI.Framework.Core;
using RimAI.Framework.LLM;
using RimAI.Framework.LLM.Models;
using RimAI.Framework.LLM.Services;
using Verse;

namespace RimAI.Framework.API
{
    /// <summary>
    /// Public API for RimAI Framework. This is the main entry point for other mods.
    /// All public methods here are guaranteed to remain stable across minor versions.
    /// </summary>
    public static class RimAIAPI
    {
        #region Properties

        /// <summary>
        /// Gets whether the RimAI Framework is properly initialized and ready to use.
        /// </summary>
        public static bool IsInitialized
        {
            get
            {
                try
                {
                    return LLMManager.Instance != null && LLMManager.Instance.CurrentSettings != null;
                }
                catch
                {
                    return false;
                }
            }
        }

        /// <summary>
        /// Gets whether streaming mode is currently enabled.
        /// </summary>
        public static bool IsStreamingEnabled => LLMManager.Instance?.IsStreamingEnabled ?? false;

        /// <summary>
        /// Gets read-only access to current settings.
        /// </summary>
        public static RimAISettings CurrentSettings => LLMManager.Instance?.CurrentSettings;

        #endregion

        #region Basic Chat API

        /// <summary>
        /// Sends a message to the LLM and returns the response.
        /// Uses the default settings (streaming/non-streaming based on configuration).
        /// </summary>
        /// <param name="prompt">The message to send</param>
        /// <param name="cancellationToken">Optional cancellation token</param>
        /// <returns>The LLM response, or null if an error occurred</returns>
        public static Task<string> SendMessageAsync(string prompt, CancellationToken cancellationToken = default)
        {
            if (!ValidateFramework()) return Task.FromResult<string>(null);
            return LLMManager.Instance.GetChatCompletionAsync(prompt, cancellationToken);
        }

        /// <summary>
        /// Sends a message with custom options.
        /// </summary>
        /// <param name="prompt">The message to send</param>
        /// <param name="options">Custom options for this request</param>
        /// <param name="cancellationToken">Optional cancellation token</param>
        /// <returns>The LLM response, or null if an error occurred</returns>
        public static Task<string> SendMessageAsync(string prompt, LLMRequestOptions options, CancellationToken cancellationToken = default)
        {
            if (!ValidateFramework()) return Task.FromResult<string>(null);
            return LLMManager.Instance.SendMessageAsync(prompt, options, cancellationToken);
        }

        /// <summary>
        /// Sends a streaming message to the LLM.
        /// </summary>
        /// <param name="prompt">The message to send</param>
        /// <param name="onChunkReceived">Callback for each chunk of the response</param>
        /// <param name="cancellationToken">Optional cancellation token</param>
        /// <returns>A task that completes when streaming is finished</returns>
        public static Task SendStreamingMessageAsync(string prompt, Action<string> onChunkReceived, CancellationToken cancellationToken = default)
        {
            if (!ValidateFramework()) return Task.CompletedTask;
            return LLMManager.Instance.GetChatCompletionStreamAsync(prompt, onChunkReceived, cancellationToken);
        }

        /// <summary>
        /// Sends a streaming message with custom options.
        /// </summary>
        public static Task SendStreamingMessageAsync(string prompt, Action<string> onChunkReceived, LLMRequestOptions options, CancellationToken cancellationToken = default)
        {
            if (!ValidateFramework()) return Task.CompletedTask;
            return LLMManager.Instance.SendMessageStreamAsync(prompt, onChunkReceived, options, cancellationToken);
        }

        #endregion

        #region Advanced API

        /// <summary>
        /// Gets access to the custom LLM service for advanced scenarios.
        /// Allows full control over request parameters.
        /// </summary>
        public static ICustomLLMService GetCustomService()
        {
            return LLMManager.Instance?.CustomService;
        }

        /// <summary>
        /// Gets access to the JSON-enforced LLM service.
        /// Ensures responses are valid JSON.
        /// </summary>
        public static IJsonLLMService GetJsonService()
        {
            return LLMManager.Instance?.JsonService;
        }

        /// <summary>
        /// Gets access to the enhanced mod service.
        /// Provides mod-specific features and integrations.
        /// </summary>
        public static IModService GetModService()
        {
            return LLMManager.Instance?.ModService;
        }

        #endregion

        #region Utility Methods

        /// <summary>
        /// Tests the connection to the LLM API.
        /// </summary>
        /// <returns>A tuple with success status and message</returns>
        public static async Task<(bool success, string message)> TestConnectionAsync()
        {
            if (!ValidateFramework()) 
                return (false, "RimAI Framework is not initialized");

            return await LLMManager.Instance.TestConnectionAsync();
        }

        /// <summary>
        /// Cancels all pending LLM requests.
        /// </summary>
        public static void CancelAllRequests()
        {
            LLMManager.Instance?.CancelAllRequests();
        }

        /// <summary>
        /// Refreshes settings from the mod configuration.
        /// Note: Some services may require restart to apply new settings.
        /// </summary>
        public static void RefreshSettings()
        {
            LLMManager.Instance?.RefreshSettings();
        }

        #endregion

        #region Helper Methods

        /// <summary>
        /// Creates request options with common presets.
        /// </summary>
        public static class Options
        {
            /// <summary>
            /// Creates options for forced streaming mode.
            /// </summary>
            public static LLMRequestOptions Streaming(double? temperature = null, int? maxTokens = null)
            {
                return new LLMRequestOptions
                {
                    EnableStreaming = true,
                    HasExplicitStreamingSetting = true,
                    Temperature = temperature,
                    MaxTokens = maxTokens
                };
            }

            /// <summary>
            /// Creates options for forced non-streaming mode.
            /// </summary>
            public static LLMRequestOptions NonStreaming(double? temperature = null, int? maxTokens = null)
            {
                return new LLMRequestOptions
                {
                    EnableStreaming = false,
                    HasExplicitStreamingSetting = true,
                    Temperature = temperature,
                    MaxTokens = maxTokens
                };
            }

            /// <summary>
            /// Creates options for JSON response mode.
            /// </summary>
            public static LLMRequestOptions Json(object schema = null, double? temperature = null)
            {
                return new LLMRequestOptions
                {
                    ForceJsonMode = true,
                    JsonSchema = schema,
                    Temperature = temperature ?? 0.7 // Lower temperature for structured output
                };
            }

            /// <summary>
            /// Creates options for creative responses.
            /// </summary>
            public static LLMRequestOptions Creative(double temperature = 1.2)
            {
                return new LLMRequestOptions
                {
                    Temperature = temperature,
                    MaxTokens = 2000
                };
            }

            /// <summary>
            /// Creates options for factual/deterministic responses.
            /// </summary>
            public static LLMRequestOptions Factual(double temperature = 0.3)
            {
                return new LLMRequestOptions
                {
                    Temperature = temperature,
                    MaxTokens = 1000
                };
            }
        }

        #endregion

        #region Private Helpers

        private static bool ValidateFramework()
        {
            if (!IsInitialized)
            {
                Log.Error("RimAI Framework: API called before initialization. Please ensure the framework mod is loaded.");
                return false;
            }
            return true;
        }

        #endregion
    }
}