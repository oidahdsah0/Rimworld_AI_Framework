using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace RimAI.Framework.LLM.Models
{
    /// <summary>
    /// Represents the unified, structured response from an LLM API.
    /// This single class, along with its nested helper classes, is designed to handle all response types,
    /// including standard text, tool-calling, and streaming chunks.
    /// </summary>
    public class LLMResponse
    {
        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("object")]
        public string Object { get; set; }

        [JsonProperty("created")]
        public long Created { get; set; }

        [JsonProperty("model")]
        public string Model { get; set; }

        [JsonProperty("choices")]
        public List<Choice> Choices { get; set; }

        [JsonProperty("usage")]
        public Usage Usage { get; set; }

        // --- Metadata for internal framework use ---

        [JsonIgnore]
        public bool IsSuccess { get; private set; } = true;

        [JsonIgnore]
        public string ErrorMessage { get; private set; }

        [JsonIgnore]
        public string RequestId { get; private set; }

        [JsonIgnore]
        public string Content => Choices?.Count > 0 ? Choices[0].Message?.Content : null;
        
        [JsonIgnore]
        public List<ToolCall> ToolCalls => Choices?.Count > 0 ? Choices[0].Message?.ToolCalls : null;

        // Private constructor for static helpers
        private LLMResponse()
        {
            Choices = new List<Choice>();
        }
        
        // Constructor for successful text response
        private LLMResponse(string content, string requestId = null)
        {
            IsSuccess = true;
            RequestId = requestId;
            Choices = new List<Choice>
            {
                new Choice
                {
                    Message = new ResponseMessage { Content = content },
                    FinishReason = "stop"
                }
            };
        }
        
        // --- Static helper methods for creating responses ---

        public static LLMResponse Success(string content, string requestId = null)
        {
            return new LLMResponse(content, requestId);
        }
        
        public static LLMResponse Failed(string errorMessage, string requestId = null)
        {
            return new LLMResponse
            {
                IsSuccess = false,
                ErrorMessage = errorMessage,
                RequestId = requestId
            };
        }

        public LLMResponse WithContent(string content)
        {
            if (Choices == null || Choices.Count == 0)
            {
                Choices = new List<Choice> { new Choice { Message = new ResponseMessage() } };
            }
            else if (Choices[0].Message == null)
            {
                Choices[0].Message = new ResponseMessage();
            }

            Choices[0].Message.Content = content;
            return this;
        }
        
        public LLMResponse WithMetadata(string key, object value)
        {
            // Placeholder for a more robust metadata system if needed in the future.
            return this;
        }
    }

    // --- Helper classes for deserializing the LLMResponse ---

    public class Choice
    {
        [JsonProperty("message")]
        public ResponseMessage Message { get; set; }

        [JsonProperty("index")]
        public int Index { get; set; }

        [JsonProperty("finish_reason")]
        public string FinishReason { get; set; }
        
        [JsonProperty("delta")]
        public ResponseMessage Delta { get; set; } // For streaming
    }

    public class ResponseMessage
    {
        [JsonProperty("role")]
        public string Role { get; set; }

        [JsonProperty("content")]
        public string Content { get; set; }

        [JsonProperty("tool_calls")]
        public List<ToolCall> ToolCalls { get; set; }
    }

    public class Usage
    {
        [JsonProperty("prompt_tokens")]
        public int PromptTokens { get; set; }

        [JsonProperty("completion_tokens")]
        public int CompletionTokens { get; set; }

        [JsonProperty("total_tokens")]
        public int TotalTokens { get; set; }
    }
}
