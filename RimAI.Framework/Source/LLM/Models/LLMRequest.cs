using System;
using System.Collections.Generic;
using System.Threading;

namespace RimAI.Framework.LLM.Models
{
    /// <summary>
    /// Unified request model for all LLM operations - new architecture
    /// </summary>
    public class UnifiedLLMRequest
    {
        public string Prompt { get; set; }
        public LLMRequestOptions Options { get; set; }
        public bool IsStreaming { get; set; }
        public Action<string> OnChunkReceived { get; set; }
        public CancellationToken CancellationToken { get; set; }
        public string RequestId { get; set; }
        public DateTime CreatedAt { get; set; }

        public UnifiedLLMRequest()
        {
            RequestId = Guid.NewGuid().ToString("N")[..8];
            CreatedAt = DateTime.UtcNow;
            Options = new LLMRequestOptions();
        }

        /// <summary>
        /// Builder pattern for fluent request creation
        /// </summary>
        public class Builder
        {
            private readonly UnifiedLLMRequest _request = new UnifiedLLMRequest();

            public Builder WithPrompt(string prompt)
            {
                _request.Prompt = prompt;
                return this;
            }

            public Builder WithOptions(LLMRequestOptions options)
            {
                _request.Options = options ?? new LLMRequestOptions();
                return this;
            }

            public Builder WithTemperature(double temperature)
            {
                _request.Options ??= new LLMRequestOptions();
                _request.Options.Temperature = temperature;
                return this;
            }

            public Builder WithMaxTokens(int maxTokens)
            {
                _request.Options ??= new LLMRequestOptions();
                _request.Options.MaxTokens = maxTokens;
                return this;
            }

            public Builder AsStreaming(Action<string> onChunkReceived)
            {
                _request.IsStreaming = true;
                _request.OnChunkReceived = onChunkReceived;
                _request.Options ??= new LLMRequestOptions();
                _request.Options.EnableStreaming = true;
                _request.Options.HasExplicitStreamingSetting = true;
                return this;
            }

            public Builder AsNonStreaming()
            {
                _request.IsStreaming = false;
                _request.OnChunkReceived = null;
                _request.Options ??= new LLMRequestOptions();
                _request.Options.EnableStreaming = false;
                _request.Options.HasExplicitStreamingSetting = true;
                return this;
            }

            public Builder WithCancellation(CancellationToken cancellationToken)
            {
                _request.CancellationToken = cancellationToken;
                return this;
            }

            public Builder WithRequestId(string requestId)
            {
                _request.RequestId = requestId;
                return this;
            }

            public UnifiedLLMRequest Build()
            {
                if (string.IsNullOrWhiteSpace(_request.Prompt))
                    throw new ArgumentException("Prompt cannot be null or empty");

                if (_request.IsStreaming && _request.OnChunkReceived == null)
                    throw new ArgumentException("OnChunkReceived callback is required for streaming requests");

                return _request;
            }
        }

        /// <summary>
        /// Creates a new builder instance
        /// </summary>
        public static Builder Create() => new Builder();

        /// <summary>
        /// Quick factory methods for common scenarios
        /// </summary>
        public static class Factory
        {
            public static UnifiedLLMRequest Simple(string prompt, CancellationToken cancellationToken = default)
            {
                return Create()
                    .WithPrompt(prompt)
                    .WithCancellation(cancellationToken)
                    .Build();
            }

            public static UnifiedLLMRequest Creative(string prompt, double temperature = 1.2, CancellationToken cancellationToken = default)
            {
                return Create()
                    .WithPrompt(prompt)
                    .WithTemperature(temperature)
                    .WithCancellation(cancellationToken)
                    .Build();
            }

            public static UnifiedLLMRequest Factual(string prompt, double temperature = 0.3, CancellationToken cancellationToken = default)
            {
                return Create()
                    .WithPrompt(prompt)
                    .WithTemperature(temperature)
                    .WithCancellation(cancellationToken)
                    .Build();
            }

            public static UnifiedLLMRequest Streaming(string prompt, Action<string> onChunkReceived, CancellationToken cancellationToken = default)
            {
                return Create()
                    .WithPrompt(prompt)
                    .AsStreaming(onChunkReceived)
                    .WithCancellation(cancellationToken)
                    .Build();
            }
        }
    }

    /// <summary>
    /// Unified response model for all LLM operations
    /// </summary>
    public class LLMResponse
    {
        public string Content { get; set; }
        public bool IsSuccess { get; set; }
        public string Error { get; set; }
        public string RequestId { get; set; }
        public DateTime CompletedAt { get; set; }
        public TimeSpan Duration { get; set; }
        public Dictionary<string, object> Metadata { get; set; }

        public LLMResponse()
        {
            CompletedAt = DateTime.UtcNow;
            Metadata = new Dictionary<string, object>();
        }

        public static LLMResponse Success(string responseContent, string requestId = null)
        {
            return new LLMResponse
            {
                Content = responseContent,
                IsSuccess = true,
                RequestId = requestId
            };
        }

        public static LLMResponse Failed(string errorMessage, string requestId = null)
        {
            return new LLMResponse
            {
                Error = errorMessage,
                IsSuccess = false,
                RequestId = requestId
            };
        }

        public static LLMResponse Failure(string errorMessage, string requestId = null)
        {
            return Failed(errorMessage, requestId);
        }

        /// <summary>
        /// Add metadata to the response (fluent interface)
        /// </summary>
        public LLMResponse WithMetadata(string key, object value)
        {
            Metadata[key] = value;
            return this;
        }

        /// <summary>
        /// Set content for the response (fluent interface)
        /// </summary>
        public LLMResponse WithContent(string content)
        {
            Content = content;
            return this;
        }
    }

    /// <summary>
    /// Enhanced request options with builder pattern support
    /// </summary>
    public class LLMRequestOptions
    {
        public bool EnableStreaming { get; set; } = false;
        public double? Temperature { get; set; }
        public int? MaxTokens { get; set; }
        public double? TopP { get; set; }
        public double? FrequencyPenalty { get; set; }
        public double? PresencePenalty { get; set; }
        public bool ForceJsonMode { get; set; } = false;
        public object JsonSchema { get; set; }
        public string Model { get; set; }
        public Dictionary<string, object> AdditionalParameters { get; set; }

        /// <summary>
        /// Indicates whether streaming was explicitly set by the caller
        /// </summary>
        public bool HasExplicitStreamingSetting { get; set; } = false;

        public LLMRequestOptions()
        {
            AdditionalParameters = new Dictionary<string, object>();
        }

        /// <summary>
        /// Constructor that explicitly sets streaming preference
        /// </summary>
        /// <param name="enableStreaming">Whether to enable streaming</param>
        public LLMRequestOptions(bool enableStreaming)
        {
            EnableStreaming = enableStreaming;
            HasExplicitStreamingSetting = true;
            AdditionalParameters = new Dictionary<string, object>();
        }

        /// <summary>
        /// Fluent API methods for chaining
        /// </summary>
        public LLMRequestOptions WithStreaming(bool enableStreaming)
        {
            EnableStreaming = enableStreaming;
            HasExplicitStreamingSetting = true;
            return this;
        }

        public LLMRequestOptions WithTemperature(double temperature)
        {
            Temperature = temperature;
            return this;
        }

        public LLMRequestOptions WithMaxTokens(int maxTokens)
        {
            MaxTokens = maxTokens;
            return this;
        }

        public LLMRequestOptions WithTopP(double topP)
        {
            TopP = topP;
            return this;
        }

        public LLMRequestOptions WithFrequencyPenalty(double frequencyPenalty)
        {
            FrequencyPenalty = frequencyPenalty;
            return this;
        }

        public LLMRequestOptions WithPresencePenalty(double presencePenalty)
        {
            PresencePenalty = presencePenalty;
            return this;
        }

        public LLMRequestOptions WithJsonMode(bool forceJsonMode = true, object schema = null)
        {
            ForceJsonMode = forceJsonMode;
            JsonSchema = schema;
            return this;
        }

        public LLMRequestOptions WithModel(string model)
        {
            Model = model;
            return this;
        }

        public LLMRequestOptions WithParameter(string key, object value)
        {
            AdditionalParameters ??= new Dictionary<string, object>();
            AdditionalParameters[key] = value;
            return this;
        }

        /// <summary>
        /// Add custom parameter (alias for WithParameter)
        /// </summary>
        public LLMRequestOptions WithCustomParameter(string key, object value)
        {
            return WithParameter(key, value);
        }

        /// <summary>
        /// Enable JSON output format
        /// </summary>
        public LLMRequestOptions WithJsonOutput(bool enabled = true)
        {
            if (enabled)
            {
                AdditionalParameters["response_format"] = new { type = "json_object" };
            }
            else
            {
                AdditionalParameters.Remove("response_format");
            }
            return this;
        }

        /// <summary>
        /// Set stop sequences
        /// </summary>
        public LLMRequestOptions WithStopSequences(params string[] sequences)
        {
            AdditionalParameters["stop"] = sequences;
            return this;
        }

        /// <summary>
        /// Set system prompt
        /// </summary>
        public LLMRequestOptions WithSystemPrompt(string systemPrompt)
        {
            AdditionalParameters["system_prompt"] = systemPrompt;
            return this;
        }

        /// <summary>
        /// Set tools/functions for function calling
        /// </summary>
        public LLMRequestOptions WithTools(object tools)
        {
            AdditionalParameters["tools"] = tools;
            return this;
        }

        /// <summary>
        /// Set random seed for reproducible results
        /// </summary>
        public LLMRequestOptions WithSeed(int seed)
        {
            AdditionalParameters["seed"] = seed;
            return this;
        }

        /// <summary>
        /// Preset factory methods
        /// </summary>
        public static LLMRequestOptions Default() => new LLMRequestOptions();

        public static LLMRequestOptions Creative(double temperature = 1.2)
        {
            return new LLMRequestOptions()
                .WithTemperature(temperature)
                .WithMaxTokens(2000);
        }

        public static LLMRequestOptions Factual(double temperature = 0.3)
        {
            return new LLMRequestOptions()
                .WithTemperature(temperature)
                .WithMaxTokens(1000);
        }

        public static LLMRequestOptions Json(object schema = null, double? temperature = null)
        {
            var options = new LLMRequestOptions()
                .WithJsonMode(true, schema);
            
            if (temperature.HasValue)
                options.WithTemperature(temperature.Value);
                
            return options;
        }

        public static LLMRequestOptions Streaming(double? temperature = null, int? maxTokens = null)
        {
            var options = new LLMRequestOptions()
                .WithStreaming(true);
            
            if (temperature.HasValue)
                options.WithTemperature(temperature.Value);
            
            if (maxTokens.HasValue)
                options.WithMaxTokens(maxTokens.Value);
                
            return options;
        }
    }
}
