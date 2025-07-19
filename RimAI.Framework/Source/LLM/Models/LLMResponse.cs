using System.Collections.Generic;

namespace RimAI.Framework.LLM.Models
{
    /// <summary>
    /// Strongly-typed classes for JSON deserialization to avoid dynamic type issues
    /// </summary>
    public class ChatCompletionResponse
    {
        public List<Choice> choices { get; set; }
        public string id { get; set; }
        public string model { get; set; }
        public Usage usage { get; set; }
    }

    public class Choice
    {
        public ResponseMessage message { get; set; }
        public int index { get; set; }
        public string finish_reason { get; set; }
    }

    public class ResponseMessage
    {
        public string role { get; set; }
        public string content { get; set; }
    }

    public class Usage
    {
        public int prompt_tokens { get; set; }
        public int completion_tokens { get; set; }
        public int total_tokens { get; set; }
    }

    // Streaming response DTOs
    public class StreamingChatCompletionChunk
    {
        public List<StreamingChoice> choices { get; set; }
        public string id { get; set; }
        public string model { get; set; }
        public string @object { get; set; }
        public long created { get; set; }
    }

    public class StreamingChoice
    {
        public Delta delta { get; set; }
        public int index { get; set; }
        public string finish_reason { get; set; }
    }

    public class Delta
    {
        public string content { get; set; }
        public string role { get; set; }
    }

    /// <summary>
    /// Generic response wrapper
    /// </summary>
    public class LLMResponse
    {
        public bool Success { get; set; }
        public string Content { get; set; }
        public object JsonContent { get; set; }
        public Dictionary<string, object> RawResponse { get; set; }
        public string Error { get; set; }
        public Usage Usage { get; set; }
    }

    /// <summary>
    /// Custom response for flexible API
    /// </summary>
    public class CustomResponse
    {
        public bool IsStream { get; set; }
        public string Content { get; set; }
        public object JsonContent { get; set; }
        public Dictionary<string, object> RawResponse { get; set; }
        public string Error { get; set; }
    }

    /// <summary>
    /// JSON-specific response wrapper
    /// </summary>
    public class JsonResponse<T>
    {
        public bool Success { get; set; }
        public T Data { get; set; }
        public string Error { get; set; }
        public string RawJson { get; set; }
    }
}
