
using System.Collections.Generic;
using Newtonsoft.Json;

namespace RimAI.Framework.LLM.Models
{
    /// <summary>
    /// Represents a tool that can be used by the LLM.
    /// </summary>
    public class Tool
    {
        [JsonProperty("type")]
        public string Type { get; set; } = "function";

        [JsonProperty("function")]
        public FunctionDefinition Function { get; set; }
    }

    /// <summary>
    /// Defines a function, including its name, description, and parameters.
    /// </summary>
    public class FunctionDefinition
    {
        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("description")]
        public string Description { get; set; }

        [JsonProperty("parameters")]
        public FunctionParameters Parameters { get; set; }
    }

    /// <summary>
    /// Defines the parameters for a function.
    /// </summary>
    public class FunctionParameters
    {
        [JsonProperty("type")]
        public string Type { get; set; } = "object";

        [JsonProperty("properties")]
        public Dictionary<string, ParameterProperty> Properties { get; set; }

        [JsonProperty("required")]
        public List<string> Required { get; set; }
    }

    /// <summary>
    /// Defines a single property for a function's parameters.
    /// </summary>
    public class ParameterProperty
    {
        [JsonProperty("type")]
        public string Type { get; set; }

        [JsonProperty("description")]
        public string Description { get; set; }
    }

    /// <summary>
    /// Represents a tool call from the LLM response.
    /// </summary>
    public class ToolCall
    {
        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("type")]
        public string Type { get; set; }

        [JsonProperty("function")]
        public FunctionCall Function { get; set; }
    }

    /// <summary>
    /// Represents a function call within a tool call.
    /// </summary>
    public class FunctionCall
    {
        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("arguments")]
        public string Arguments { get; set; } // This is a JSON string
    }

    /// <summary>
    /// Represents the result of a function call request.
    /// </summary>
    public class FunctionCallResult
    {
        public string FunctionName { get; set; }
        public string Arguments { get; set; }
        public string ToolId { get; set; }
    }
} 