using System.Collections.Generic;

namespace RimAI.Framework.LLM.Models
{
    /// <summary>
    /// Represents a request to be sent to the LLM API
    /// </summary>
    public class LLMRequest
    {
        public string Model { get; set; }
        public List<Message> Messages { get; set; } = new List<Message>();
        public bool Stream { get; set; } = false;
        public double? Temperature { get; set; }
        public int? MaxTokens { get; set; }
        public object ResponseFormat { get; set; }
        public Dictionary<string, object> AdditionalParameters { get; set; }
    }

    /// <summary>
    /// Message in a conversation
    /// </summary>
    public class Message
    {
        public string Role { get; set; }
        public string Content { get; set; }
        
        // Additional properties for API compatibility
        public string role => Role;
        public string content => Content;
    }

    /// <summary>
    /// Options for customizing LLM requests
    /// </summary>
    public class LLMRequestOptions
    {
        public bool EnableStreaming { get; set; } = false;
        public double? Temperature { get; set; }
        public int? MaxTokens { get; set; }
        public bool ForceJsonMode { get; set; } = false;
        public object JsonSchema { get; set; }
        public string Model { get; set; }
        public Dictionary<string, object> AdditionalParameters { get; set; }

        /// <summary>
        /// Indicates whether streaming was explicitly set by the caller
        /// </summary>
        public bool HasExplicitStreamingSetting { get; set; } = false;

        /// <summary>
        /// Default constructor
        /// </summary>
        public LLMRequestOptions()
        {
        }

        /// <summary>
        /// Constructor that explicitly sets streaming preference
        /// </summary>
        /// <param name="enableStreaming">Whether to enable streaming</param>
        public LLMRequestOptions(bool enableStreaming)
        {
            EnableStreaming = enableStreaming;
            HasExplicitStreamingSetting = true;
        }

        /// <summary>
        /// Fluent API method to explicitly set streaming preference
        /// </summary>
        /// <param name="enableStreaming">Whether to enable streaming</param>
        /// <returns>This options instance for chaining</returns>
        public LLMRequestOptions WithStreaming(bool enableStreaming)
        {
            EnableStreaming = enableStreaming;
            HasExplicitStreamingSetting = true;
            return this;
        }
    }

    /// <summary>
    /// Custom request allowing full control over parameters
    /// </summary>
    public class CustomRequest
    {
        public string Model { get; set; }
        public List<object> Messages { get; set; } = new List<object>();
        public double? Temperature { get; set; }
        public int? MaxTokens { get; set; }
        public bool? Stream { get; set; }
        public object ResponseFormat { get; set; }
        public Dictionary<string, object> AdditionalParameters { get; set; }
    }
}
