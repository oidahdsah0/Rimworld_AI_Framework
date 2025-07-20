using System;
using System.Collections.Generic;
using System.Threading;
using RimAI.Framework.LLM.Models;

namespace RimAI.Framework.LLM
{
    /// <summary>
    /// Factory for creating LLM requests with common configurations
    /// </summary>
    public static class LLMRequestFactory
    {
        /// <summary>
        /// Create a basic unified request
        /// </summary>
        public static UnifiedLLMRequest CreateRequest(string prompt, LLMRequestOptions options = null)
        {
            return new UnifiedLLMRequest
            {
                Prompt = prompt,
                Options = options ?? new LLMRequestOptions(),
                RequestId = Guid.NewGuid().ToString(),
                CreatedAt = DateTime.UtcNow
            };
        }

        /// <summary>
        /// Create a streaming request
        /// </summary>
        public static UnifiedLLMRequest CreateStreamingRequest(string prompt, Action<string> onChunkReceived, LLMRequestOptions options = null)
        {
            var request = CreateRequest(prompt, options);
            request.IsStreaming = true;
            request.OnChunkReceived = onChunkReceived;
            return request;
        }

        /// <summary>
        /// Create a request for creative tasks
        /// </summary>
        public static UnifiedLLMRequest CreateCreativeRequest(string prompt)
        {
            return CreateRequest(prompt, LLMRequestOptions.Creative());
        }

        /// <summary>
        /// Create a request for factual tasks
        /// </summary>
        public static UnifiedLLMRequest CreateFactualRequest(string prompt)
        {
            return CreateRequest(prompt, LLMRequestOptions.Factual());
        }

        /// <summary>
        /// Create a request with JSON output format
        /// </summary>
        public static UnifiedLLMRequest CreateJsonRequest(string prompt, LLMRequestOptions baseOptions = null)
        {
            var options = baseOptions ?? new LLMRequestOptions();
            options.WithJsonOutput();
            return CreateRequest(prompt, options);
        }

        /// <summary>
        /// Create a request with custom parameters
        /// </summary>
        public static UnifiedLLMRequest CreateCustomRequest(string prompt, Dictionary<string, object> customParams)
        {
            var options = new LLMRequestOptions();
            foreach (var param in customParams)
            {
                options.WithCustomParameter(param.Key, param.Value);
            }
            return CreateRequest(prompt, options);
        }

        // Legacy compatibility methods for RimAIApi
        public static UnifiedLLMRequest SimpleText(string prompt, CancellationToken cancellationToken = default)
        {
            var request = CreateRequest(prompt);
            request.CancellationToken = cancellationToken;
            return request;
        }

        public static UnifiedLLMRequest Creative(string prompt, double temperature = 1.2, CancellationToken cancellationToken = default)
        {
            var options = LLMRequestOptions.Creative(temperature);
            var request = CreateRequest(prompt, options);
            request.CancellationToken = cancellationToken;
            return request;
        }

        public static UnifiedLLMRequest Factual(string prompt, double temperature = 0.3, CancellationToken cancellationToken = default)
        {
            var options = LLMRequestOptions.Factual(temperature);
            var request = CreateRequest(prompt, options);
            request.CancellationToken = cancellationToken;
            return request;
        }

        public static UnifiedLLMRequest Streaming(string prompt, Action<string> onChunkReceived, CancellationToken cancellationToken = default)
        {
            var request = CreateStreamingRequest(prompt, onChunkReceived);
            request.CancellationToken = cancellationToken;
            return request;
        }

        public static UnifiedLLMRequest JsonRequest(string prompt, object schema = null, double? temperature = null, CancellationToken cancellationToken = default)
        {
            var options = LLMRequestOptions.Json(schema, temperature);
            var request = CreateRequest(prompt, options);
            request.CancellationToken = cancellationToken;
            return request;
        }
    }
}
