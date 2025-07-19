using RimAI.Framework.Core;
using RimAI.Framework.LLM;
using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Verse;

namespace RimAI.Framework.API
{
    /// <summary>
    /// The primary public API for other mods to interact with the RimAI Framework.
    /// Provides easy access to the core functionalities like getting LLM completions.
    /// </summary>
    public static class RimAIApi
    {
        /// <summary>
        /// Asynchronously enqueues a chat completion request to the configured Large Language Model.
        /// The request will be processed according to the queue and concurrency limits.
        /// </summary>
        /// <param name="prompt">The text prompt to send to the LLM.</param>
        /// <param name="cancellationToken">An optional cancellation token to cancel the request.</param>
        /// <returns>A Task that resolves to the LLM's response string, or null if an error occurs.</returns>
        /// <example>
        /// <code>
        /// CancellationTokenSource cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        /// string prompt = "Generate a short, dramatic backstory for a colonist named 'Jax'.";
        /// string backstory = await RimAIApi.GetChatCompletion(prompt, cts.Token);
        /// if (backstory != null)
        /// {
        ///     Log.Message($"Generated backstory for Jax: {backstory}");
        /// }
        /// </code>
        /// </example>
        public static Task<string> GetChatCompletion(string prompt, CancellationToken cancellationToken = default)
        {
            // Ensure the manager is initialized
            if (LLMManager.Instance == null)
            {
                Log.Error("RimAI Framework: LLMManager is not initialized. Cannot get chat completion.");
                return Task.FromResult<string>(null);
            }

            return LLMManager.Instance.GetChatCompletionAsync(prompt, cancellationToken);
        }

        /// <summary>
        /// Gets a chat completion as a stream of tokens, providing real-time feedback.
        /// This is a convenience overload without cancellation token support.
        /// </summary>
        /// <param name="prompt">The text prompt to send to the LLM.</param>
        /// <param name="onChunkReceived">An action to be called for each received token chunk.</param>
        /// <returns>A Task that completes when the streaming is finished.</returns>
        public static async Task GetChatCompletionStream(string prompt, Action<string> onChunkReceived)
        {
            await GetChatCompletionStream(prompt, onChunkReceived, CancellationToken.None);
        }

        /// <summary>
        /// Gets a chat completion as a stream of tokens, providing real-time feedback.
        /// This method allows downstream mods to receive partial responses as they arrive.
        /// </summary>
        /// <param name="prompt">The text prompt to send to the LLM.</param>
        /// <param name="onChunkReceived">An action to be called for each received token chunk.</param>
        /// <param name="cancellationToken">An optional cancellation token to cancel the request.</param>
        /// <returns>A Task that completes when the streaming is finished.</returns>
        /// <example>
        /// <code>
        /// StringBuilder response = new StringBuilder();
        /// await RimAIApi.GetChatCompletionStream(
        ///     "Tell me about RimWorld", 
        ///     chunk => {
        ///         response.Append(chunk);
        ///         UpdateUI(response.ToString());
        ///     }
        /// );
        /// </code>
        /// </example>
        public static async Task GetChatCompletionStream(string prompt, Action<string> onChunkReceived, CancellationToken cancellationToken = default)
        {
            // Ensure the manager is initialized
            if (LLMManager.Instance == null)
            {
                Log.Error("RimAI Framework: LLMManager is not initialized. Cannot get streaming chat completion.");
                return;
            }

            await LLMManager.Instance.GetChatCompletionStreamAsync(prompt, onChunkReceived, cancellationToken);
        }

        /// <summary>
        /// Checks whether streaming is currently enabled in the framework settings.
        /// Downstream mods can use this to adjust their UI behavior and user expectations.
        /// When true, even GetChatCompletion may use streaming internally for better performance.
        /// </summary>
        /// <returns>True if streaming is enabled, false otherwise.</returns>
        /// <example>
        /// <code>
        /// if (RimAIApi.IsStreamingEnabled())
        /// {
        ///     statusLabel.text = "🚀 快速响应模式已启用";
        /// }
        /// else
        /// {
        ///     statusLabel.text = "📝 标准响应模式";
        /// }
        /// </code>
        /// </example>
        public static bool IsStreamingEnabled()
        {
            return LLMManager.Instance?.IsStreamingEnabled ?? false;
        }

        /// <summary>
        /// Gets the current RimAI Framework settings in read-only format.
        /// Downstream mods can use this to understand the current configuration.
        /// </summary>
        /// <returns>The current settings, or null if the manager is not initialized.</returns>
        /// <example>
        /// <code>
        /// var settings = RimAIApi.GetCurrentSettings();
        /// if (settings != null)
        /// {
        ///     Log.Message($"Current model: {settings.modelName}");
        ///     Log.Message($"Streaming enabled: {settings.enableStreaming}");
        /// }
        /// </code>
        /// </example>
        public static RimAISettings GetCurrentSettings()
        {
            return LLMManager.Instance?.CurrentSettings;
        }

        /// <summary>
        /// Cancels all pending LLM requests.
        /// This is useful for emergency situations or when switching contexts rapidly.
        /// </summary>
        /// <example>
        /// <code>
        /// // User clicked "Cancel All" button or switched to a different task
        /// RimAIApi.CancelAllRequests();
        /// </code>
        /// </example>
        public static void CancelAllRequests()
        {
            LLMManager.Instance?.CancelAllRequests();
        }

        /// <summary>
        /// Gets a chat completion with an option to force or disable streaming regardless of global settings.
        /// This gives downstream mods fine-grained control over the request behavior.
        /// </summary>
        /// <param name="prompt">The text prompt to send to the LLM.</param>
        /// <param name="forceStreaming">If not null, overrides the global streaming setting for this request.</param>
        /// <param name="cancellationToken">An optional cancellation token to cancel the request.</param>
        /// <returns>A Task that resolves to the LLM's response string, or null if an error occurs.</returns>
        /// <example>
        /// <code>
        /// // Force streaming for this specific request (for UI that needs real-time updates)
        /// string response = await RimAIApi.GetChatCompletionWithOptions("Quick question", forceStreaming: true);
        /// 
        /// // Force non-streaming for this specific request (for background processing)
        /// string response = await RimAIApi.GetChatCompletionWithOptions("Background task", forceStreaming: false);
        /// </code>
        /// </example>
        public static async Task<string> GetChatCompletionWithOptions(string prompt, bool? forceStreaming = null, CancellationToken cancellationToken = default)
        {
            // Ensure the manager is initialized
            if (LLMManager.Instance == null)
            {
                Log.Error("RimAI Framework: LLMManager is not initialized. Cannot get chat completion.");
                return null;
            }

            // If forceStreaming is specified, we need to temporarily override the behavior
            if (forceStreaming.HasValue)
            {
                if (forceStreaming.Value)
                {
                    // Force streaming: collect chunks into a single response
                    var response = new StringBuilder();
                    await LLMManager.Instance.GetChatCompletionStreamAsync(
                        prompt, 
                        chunk => response.Append(chunk), 
                        cancellationToken
                    );
                    return response.ToString();
                }
                else
                {
                    // Force non-streaming: need to call the manager's non-streaming path directly
                    // This would require exposing a method in LLMManager, for now fall back to normal method
                    Log.Warning("RimAI Framework: Force non-streaming not fully implemented yet, using default behavior.");
                }
            }

            return await LLMManager.Instance.GetChatCompletionAsync(prompt, cancellationToken);
        }
    }
}
