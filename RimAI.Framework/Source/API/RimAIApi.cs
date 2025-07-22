using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using RimAI.Framework.Core;
using RimAI.Framework.LLM.Models;
using RimAI.Framework.LLM.Services;
using RimAI.Framework.Configuration;
using Verse;
using LLMManager = RimAI.Framework.LLM.LLMManager;

namespace RimAI.Framework.API
{
    /// <summary>
    /// Public API for RimAI Framework v3.0 Enhanced.
    /// This is the main entry point for other mods to interact with AI services.
    /// 
    /// <para>
    /// This API provides a clean, well-documented interface that abstracts away internal complexity
    /// while providing powerful features like intelligent caching, configuration management,
    /// and optimized resource usage.
    /// </para>
    /// 
    /// <para>
    /// All public methods are guaranteed to remain stable across minor versions,
    /// following semantic versioning principles.
    /// </para>
    /// </summary>
    /// <example>
    /// Basic usage:
    /// <code>
    /// // Simple message
    /// var response = await RimAIAPI.SendMessageAsync("Hello, world!");
    /// 
    /// // With options
    /// var options = RimAIAPI.Options.Creative(temperature: 1.0);
    /// var creative = await RimAIAPI.SendMessageAsync("Write a story", options);
    /// 
    /// // Streaming
    /// await RimAIAPI.SendStreamingMessageAsync("Tell me about...", chunk => Log.Message(chunk));
    /// 
    /// // Batch processing
    /// var prompts = new List&lt;string&gt; { "Question 1", "Question 2", "Question 3" };
    /// var responses = await RimAIAPI.SendBatchRequestAsync(prompts);
    /// </code>
    /// </example>
    public static class RimAIAPI
    {
        #region Framework Status Properties

        /// <summary>
        /// Gets whether the RimAI Framework is properly initialized and ready to use.
        /// </summary>
        /// <value>
        /// <c>true</c> if the framework is initialized and settings are loaded; otherwise, <c>false</c>.
        /// </value>
        public static bool IsInitialized
        {
            get
            {
                try
                {
                    return LLMManager.Instance != null && LLMManager.Instance.CurrentSettings != null;
                }
                catch (Exception ex)
                {
                    RimAILogger.Debug("Framework initialization check failed: {0}", ex.Message);
                    return false;
                }
            }
        }

        /// <summary>
        /// Gets the current status of the RimAI Framework.
        /// </summary>
        /// <value>A string representation of the framework status.</value>
        public static string Status
        {
            get
            {
                try
                {
                    if (!IsInitialized)
                        return "Not Initialized";
                    
                    return "Ready";
                }
                catch (Exception ex)
                {
                    RimAILogger.Debug("Failed to get framework status: {0}", ex.Message);
                    return "Error";
                }
            }
        }

        #endregion

        #region Basic Messaging Methods

        /// <summary>
        /// Sends a message to the AI service and returns the response.
        /// This is the primary method for simple AI interactions.
        /// </summary>
        /// <param name="prompt">The message to send to the AI service.</param>
        /// <param name="options">Optional parameters for the request. If null, default options will be used.</param>
        /// <param name="cancellationToken">Token to cancel the operation.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the AI's response.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="prompt"/> is null or empty.</exception>
        /// <exception cref="InvalidOperationException">Thrown when the framework is not initialized.</exception>
        /// <example>
        /// <code>
        /// // Simple usage
        /// var response = await RimAIAPI.SendMessageAsync("What is the weather like?");
        /// 
        /// // With custom options
        /// var options = new LLMRequestOptions { Temperature = 0.7, MaxTokens = 150 };
        /// var customResponse = await RimAIAPI.SendMessageAsync("Tell me a story", options);
        /// </code>
        /// </example>
        public static async Task<string> SendMessageAsync(
            string prompt,
            LLMRequestOptions options = null,
            CancellationToken cancellationToken = default)
        {
            ValidatePrompt(prompt, nameof(prompt));

            if (!ValidateFramework())
                return null;

            try
            {
                return await LLMManager.Instance.SendMessageAsync(prompt, options, cancellationToken);
            }
            catch (Exception ex)
            {
                RimAILogger.Error("Failed to send message: {0}", ex.Message);
                return null;
            }
        }

        /// <summary>
        /// Sends a message with a specific temperature setting.
        /// Temperature controls the randomness of the AI's responses.
        /// </summary>
        /// <param name="prompt">The message to send to the AI service.</param>
        /// <param name="temperature">
        /// Controls randomness in the response (0.0 = deterministic, 1.0 = very random).
        /// Must be between 0.0 and 2.0.
        /// </param>
        /// <param name="cancellationToken">Token to cancel the operation.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the AI's response.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="prompt"/> is null or empty.</exception>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="temperature"/> is not between 0.0 and 2.0.</exception>
        /// <exception cref="InvalidOperationException">Thrown when the framework is not initialized.</exception>
        public static async Task<string> SendMessageWithTemperatureAsync(
            string prompt,
            double temperature,
            CancellationToken cancellationToken = default)
        {
            ValidatePrompt(prompt, nameof(prompt));
            ValidateTemperature(temperature, nameof(temperature));

            if (!ValidateFramework())
                return null;

            var options = new LLMRequestOptions { Temperature = temperature };
            return await SendMessageAsync(prompt, options, cancellationToken);
        }

        #endregion

        #region Streaming Methods

        /// <summary>
        /// Sends a message to the AI service and streams the response in real-time.
        /// This is useful for long responses where you want to show progress to the user.
        /// </summary>
        /// <param name="prompt">The message to send to the AI service.</param>
        /// <param name="onChunkReceived">
        /// Callback function that will be called for each chunk of the response.
        /// This function should be thread-safe as it may be called from multiple threads.
        /// </param>
        /// <param name="options">Optional parameters for the request. If null, default options will be used.</param>
        /// <param name="cancellationToken">Token to cancel the operation.</param>
        /// <returns>A task that represents the asynchronous streaming operation.</returns>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="prompt"/> is null or empty, or when <paramref name="onChunkReceived"/> is null.
        /// </exception>
        /// <exception cref="InvalidOperationException">Thrown when the framework is not initialized.</exception>
        /// <example>
        /// <code>
        /// await RimAIAPI.SendStreamingMessageAsync(
        ///     "Tell me a long story about space exploration",
        ///     chunk => Log.Message($"Received: {chunk}"),
        ///     Options.Creative(temperature: 0.9)
        /// );
        /// </code>
        /// </example>
        public static async Task SendStreamingMessageAsync(
            string prompt,
            Action<string> onChunkReceived,
            LLMRequestOptions options = null,
            CancellationToken cancellationToken = default)
        {
            ValidatePrompt(prompt, nameof(prompt));
            ValidateCallback(onChunkReceived, nameof(onChunkReceived));

            if (!ValidateFramework())
                return;

            try
            {
                await LLMManager.Instance.SendStreamingMessageAsync(prompt, onChunkReceived, options, cancellationToken);
            }
            catch (Exception ex)
            {
                RimAILogger.Error("Failed to send streaming message: {0}", ex.Message);
            }
        }

        #endregion

        #region Batch Processing Methods

        /// <summary>
        /// Sends multiple messages as a batch for improved efficiency.
        /// This method processes requests sequentially but with improved performance tracking.
        /// </summary>
        /// <param name="prompts">List of prompts to send to the AI service.</param>
        /// <param name="options">Optional parameters for all requests. If null, default options will be used.</param>
        /// <param name="cancellationToken">Token to cancel the operation.</param>
        /// <returns>
        /// A task that represents the asynchronous batch operation. 
        /// The task result contains a list of responses in the same order as the input prompts.
        /// </returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="prompts"/> is null.</exception>
        /// <exception cref="ArgumentException">Thrown when <paramref name="prompts"/> is empty or contains null/empty strings.</exception>
        /// <exception cref="InvalidOperationException">Thrown when the framework is not initialized.</exception>
        /// <example>
        /// <code>
        /// var questions = new List&lt;string&gt;
        /// {
        ///     "What is the capital of France?",
        ///     "What is 2 + 2?",
        ///     "What is the largest planet?"
        /// };
        /// 
        /// var answers = await RimAIAPI.SendBatchRequestAsync(questions);
        /// foreach (var answer in answers)
        /// {
        ///     Log.Message($"Answer: {answer}");
        /// }
        /// </code>
        /// </example>
        public static async Task<List<string>> SendBatchRequestAsync(
            List<string> prompts,
            LLMRequestOptions options = null,
            CancellationToken cancellationToken = default)
        {
            if (prompts == null)
                throw new ArgumentNullException(nameof(prompts));

            if (prompts.Count == 0)
                throw new ArgumentException("Prompts list cannot be empty", nameof(prompts));

            foreach (var prompt in prompts)
            {
                ValidatePrompt(prompt, nameof(prompts));
            }

            if (!ValidateFramework())
                return new List<string>();

            try
            {
                RimAILogger.Info("Processing batch request with {0} prompts", prompts.Count);
                var results = new List<string>();

                for (int i = 0; i < prompts.Count; i++)
                {
                    if (cancellationToken.IsCancellationRequested)
                        break;

                    var response = await LLMManager.Instance.SendMessageAsync(prompts[i], options, cancellationToken);
                    results.Add(response);
                    
                    RimAILogger.Debug("Completed batch item {0}/{1}", i + 1, prompts.Count);
                }

                RimAILogger.Info("Batch request completed. Processed {0} items", results.Count);
                return results;
            }
            catch (Exception ex)
            {
                RimAILogger.Error("Failed to process batch request: {0}", ex.Message);
                return new List<string>();
            }
        }

        #endregion

        #region System Operations

        /// <summary>
        /// 获取框架统计信息
        /// </summary>
        /// <returns>包含各种统计信息的字典</returns>
        /// <example>
        /// <code>
        /// var stats = RimAIAPI.GetStatistics();
        /// Log.Message($"Success rate: {stats["SuccessRate"]}");
        /// </code>
        /// </example>
        public static Dictionary<string, object> GetStatistics()
        {
            if (!ValidateFramework())
                return new Dictionary<string, object>();

            return LLMManager.Instance.GetStatistics();
        }

        /// <summary>
        /// 清理缓存
        /// </summary>
        public static void ClearCache()
        {
            if (ValidateFramework())
            {
                ResponseCache.Instance?.Clear();
                Log.Message("RimAI cache cleared successfully");
            }
        }

        /// <summary>
        /// 获取缓存统计信息（已弃用，请使用GetStatistics()）
        /// </summary>
        [Obsolete("Use GetStatistics() instead, which includes cache statistics")]
        public static Dictionary<string, object> GetCacheStatistics()
        {
            return GetStatistics();
        }

        /// <summary>
        /// 监控缓存健康状态
        /// </summary>
        public static void MonitorCacheHealth()
        {
            try
            {
                FrameworkDiagnostics.ExecuteCacheMonitoringCommand();
            }
            catch (Exception ex)
            {
                Log.Error($"Failed to monitor cache health: {ex.Message}");
            }
        }

        #endregion

        #region Options Factory

        /// <summary>
        /// Factory class for creating common LLMRequestOptions configurations.
        /// This provides convenient presets for typical use cases.
        /// </summary>
        public static class Options
        {
            /// <summary>
            /// Creates options optimized for creative writing and storytelling.
            /// Uses higher temperature for more varied and creative responses.
            /// </summary>
            /// <param name="temperature">Creativity level (0.0 = conservative, 1.0 = very creative). Default is 0.8.</param>
            /// <param name="maxTokens">Maximum response length. Default is 500.</param>
            /// <returns>LLMRequestOptions configured for creative tasks.</returns>
            public static LLMRequestOptions Creative(double temperature = 0.8, int maxTokens = 500)
            {
                return new LLMRequestOptions
                {
                    Temperature = temperature,
                    MaxTokens = maxTokens,
                    TopP = 0.9,
                    FrequencyPenalty = 0.1,
                    PresencePenalty = 0.1
                };
            }

            /// <summary>
            /// Creates options optimized for factual, informative responses.
            /// Uses lower temperature for more consistent and reliable answers.
            /// </summary>
            /// <param name="temperature">Precision level (0.0 = very precise, 0.5 = moderate). Default is 0.2.</param>
            /// <param name="maxTokens">Maximum response length. Default is 300.</param>
            /// <returns>LLMRequestOptions configured for factual tasks.</returns>
            public static LLMRequestOptions Factual(double temperature = 0.2, int maxTokens = 300)
            {
                return new LLMRequestOptions
                {
                    Temperature = temperature,
                    MaxTokens = maxTokens,
                    TopP = 0.7,
                    FrequencyPenalty = 0,
                    PresencePenalty = 0
                };
            }

            /// <summary>
            /// Creates options optimized for streaming responses.
            /// Configured to provide good balance between quality and streaming performance.
            /// </summary>
            /// <param name="temperature">Response variation level. Default is 0.5.</param>
            /// <param name="maxTokens">Maximum response length. Default is 1000.</param>
            /// <returns>LLMRequestOptions configured for streaming.</returns>
            public static LLMRequestOptions Streaming(double temperature = 0.5, int maxTokens = 1000)
            {
                return new LLMRequestOptions
                {
                    Temperature = temperature,
                    MaxTokens = maxTokens,
                    TopP = 0.8
                };
            }

            /// <summary>
            /// Creates options optimized for JSON-like structured responses.
            /// Ensures more consistent formatting for data extraction.
            /// </summary>
            /// <param name="temperature">Response variation level. Default is 0.1 for consistency.</param>
            /// <param name="maxTokens">Maximum response length. Default is 800.</param>
            /// <returns>LLMRequestOptions configured for structured output.</returns>
            public static LLMRequestOptions Structured(double temperature = 0.1, int maxTokens = 800)
            {
                return new LLMRequestOptions
                {
                    Temperature = temperature,
                    MaxTokens = maxTokens,
                    TopP = 0.5
                };
            }
        }

        #endregion

        #region Private Helper Methods

        /// <summary>
        /// Validates that the framework is properly initialized.
        /// </summary>
        /// <returns>True if the framework is ready; otherwise, false.</returns>
        private static bool ValidateFramework()
        {
            if (!IsInitialized)
            {
                RimAILogger.Warning("RimAI Framework is not initialized. Please check your configuration.");
                return false;
            }
            return true;
        }

        /// <summary>
        /// Validates that a prompt string is not null or empty.
        /// </summary>
        /// <param name="prompt">The prompt to validate.</param>
        /// <param name="paramName">The parameter name for error reporting.</param>
        /// <exception cref="ArgumentNullException">Thrown when the prompt is null or empty.</exception>
        private static void ValidatePrompt(string prompt, string paramName)
        {
            if (string.IsNullOrEmpty(prompt))
                throw new ArgumentNullException(paramName, "Prompt cannot be null or empty.");
        }

        /// <summary>
        /// Validates that a callback action is not null.
        /// </summary>
        /// <param name="callback">The callback to validate.</param>
        /// <param name="paramName">The parameter name for error reporting.</param>
        /// <exception cref="ArgumentNullException">Thrown when the callback is null.</exception>
        private static void ValidateCallback(Action<string> callback, string paramName)
        {
            if (callback == null)
                throw new ArgumentNullException(paramName, "Callback cannot be null.");
        }

        /// <summary>
        /// Validates that a temperature value is within acceptable range.
        /// </summary>
        /// <param name="temperature">The temperature to validate.</param>
        /// <param name="paramName">The parameter name for error reporting.</param>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when temperature is not between 0.0 and 2.0.</exception>
        private static void ValidateTemperature(double temperature, string paramName)
        {
            if (temperature < 0.0 || temperature > 2.0)
                throw new ArgumentOutOfRangeException(paramName, temperature, "Temperature must be between 0.0 and 2.0.");
        }

        #endregion
    }
}
