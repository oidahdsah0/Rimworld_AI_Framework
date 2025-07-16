using RimAI.Framework.LLM;
using System;
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
        /// Gets a chat completion as a stream of tokens. (NOT IMPLEMENTED IN V1)
        /// This method is reserved for future use.
        /// </summary>
        /// <param name="prompt">The text prompt to send to the LLM.</param>
        /// <param name="onChunkReceived">An action to be called for each received token chunk.</param>
        public static async Task GetChatCompletionStream(string prompt, Action<string> onChunkReceived)
        {
            await LLMManager.Instance.GetChatCompletionStreamAsync(prompt, onChunkReceived);
        }
    }
}
