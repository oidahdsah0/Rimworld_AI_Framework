using RimAI.Framework.LLM;
using System;
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
        /// Gets a chat completion from the configured Large Language Model.
        /// This is the main entry point for other mods to leverage the AI capabilities.
        /// </summary>
        /// <param name="prompt">The text prompt to send to the LLM.</param>
        /// <returns>A Task that resolves to the LLM's response string, or null if an error occurs.</returns>
        /// <example>
        /// <code>
        /// string prompt = "Generate a short, dramatic backstory for a colonist named 'Jax'.";
        /// string backstory = await RimAIApi.GetChatCompletion(prompt);
        /// if (backstory != null)
        /// {
        ///     Log.Message($"Generated backstory for Jax: {backstory}");
        /// }
        /// </code>
        /// </example>
        public static async Task<string> GetChatCompletion(string prompt)
        {
            // Ensure the manager is initialized
            if (LLMManager.Instance == null)
            {
                Log.Error("RimAI Framework: LLMManager is not initialized. Cannot get chat completion.");
                return null;
            }

            return await LLMManager.Instance.GetChatCompletionAsync(prompt);
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
