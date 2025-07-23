# 📘 RimAI Framework v3.0 API Comprehensive Guide

## 📋 Overview

This comprehensive guide covers all RimAI Framework v3.0 APIs in detail, including advanced parameters, performance optimization, and best practices. Designed for developers who need deep framework integration.

## 🎯 Core API Class: RimAIAPI

`RimAIAPI` serves as the unified entry point, providing all major functionality through static methods.

### Required Namespaces
```csharp
using RimAI.Framework.API;
using RimAI.Framework.LLM.Models;
```

## 1. Basic API vs. Advanced API

To meet the needs of different developers, RimAI v3.0 offers two core APIs:

- **Basic API (`SendMessageAsync`)**:
  - **Returns**: `string`
  - **Features**: Simple, direct, for quickly getting text results.
  - **Use Case**: Ideal for simple requests where details about errors, token usage, or other metadata are not critical.

- **Advanced API (`SendRequestAsync`)**:
  - **Returns**: `LLMResponse` object
  - **Features**: Powerful, providing detailed success/failure status, error messages, and metadata.
  - **Use Case**: Recommended for production-grade features that require reliable error handling and access to response metadata (like token consumption).

---

## 2. SendMessageAsync - Basic Message Sending (returns string)

This method is the fastest way to perform simple interactions with the AI. It directly returns the AI's response text, or `null` if an error occurs.

#### Method Signature
```csharp
public static async Task<string> SendMessageAsync(
    string message, 
    LLMRequestOptions options = null
)
```

#### Parameters

**message (string, required)**
- Content to send to the LLM
- Supports multi-line text and special characters
- Recommended length: 1-8000 characters

**options (LLMRequestOptions, optional)**
- Request configuration options, uses defaults when null
- See [LLMRequestOptions Details](#llmrequestoptions-comprehensive-parameters)

#### Return Value: string
- Returns the AI-generated response content as a string
- Returns null if the request fails
- Framework handles errors and retry logic internally

#### Usage Examples

**Basic Request**
```csharp
// Simplest possible call
var response = await RimAIAPI.SendMessageAsync("Hello, AI!");
if (!string.IsNullOrEmpty(response))
{
    Log.Message($"AI Response: {response}");
}
```

**Configured Request**
```csharp
// Custom configuration
var options = new LLMRequestOptions
{
    Temperature = 0.8f,
    MaxTokens = 500,
    EnableCaching = true
};

var response = await RimAIAPI.SendMessageAsync(
    "Write a short story about RimWorld", 
    options
);
```

**Production-Ready Error Handling**
```csharp
try
{
    var response = await RimAIAPI.SendMessageAsync("Hello");
    if (string.IsNullOrEmpty(response))
    {
        Log.Error("Request failed: No response received");
        return;
    }
    
    // Process successful response
    ProcessResponse(response);
}
catch (RimAIException ex)
{
    Log.Error($"RimAI Exception: {ex.Message}");
}
catch (Exception ex)
{
    Log.Error($"Unexpected error: {ex.Message}");
}
```

### 3. SendMessageStreamAsync - Streaming Message Processing

#### Method Signature
```csharp
public static async Task SendMessageStreamAsync(
    string message,
    Action<string> onChunkReceived,
    LLMRequestOptions options = null,
    CancellationToken cancellationToken = default
)
```

#### Parameters

**message (string, required)**
- Content to send to the LLM

**onChunkReceived (Action<string>, required)**
- Callback for receiving response chunks
- Triggered for each response fragment
- Parameter contains the content fragment

**options (LLMRequestOptions, optional)**
- Request configuration options

**cancellationToken (CancellationToken, optional)**
- For canceling long-running requests

#### Usage Examples

**Basic Streaming**
```csharp
var fullResponse = new StringBuilder();

await RimAIAPI.SendMessageStreamAsync(
    "Explain RimWorld's combat system in detail",
    chunk => {
        // Receive response fragments in real-time
        Log.Message($"Received: {chunk}");
        fullResponse.Append(chunk);
    }
);

Log.Message($"Complete response: {fullResponse.ToString()}");
```

**Streaming with Cancellation**
```csharp
var cts = new CancellationTokenSource();
var responseBuilder = new StringBuilder();

// Set 5-second timeout
cts.CancelAfter(TimeSpan.FromSeconds(5));

try
{
    await RimAIAPI.SendMessageStreamAsync(
        "Write a long novel",
        chunk => {
            responseBuilder.Append(chunk);
            
            // Cancel based on conditions
            if (responseBuilder.Length > 1000)
            {
                cts.Cancel();
            }
        },
        options: new LLMRequestOptions { Temperature = 0.9f },
        cancellationToken: cts.Token
    );
}
catch (OperationCanceledException)
{
    Log.Message("Request was cancelled");
}
```

**Real-Time UI Updates**
```csharp
var dialog = Find.WindowStack.WindowOfType<MyAIDialog>();

await RimAIAPI.SendMessageStreamAsync(
    userInput,
    chunk => {
        // Update UI on main thread
        if (Current.ProgramState == ProgramState.Playing)
        {
            dialog?.UpdateResponseText(chunk);
        }
    },
    new LLMRequestOptions { EnableCaching = false }
);
```

### 3. SendRequestAsync - Advanced Message Sending (returns LLMResponse)

This method offers more powerful functionality and finer control by returning an `LLMResponse` object containing all response details. **This is the recommended method for most production-grade features.**

#### Method Signature
```csharp
public static async Task<LLMResponse> SendRequestAsync(
    string prompt,
    LLMRequestOptions options = null,
    CancellationToken cancellationToken = default
)
```

#### Return Value: LLMResponse

The `LLMResponse` object contains all the detailed information about the request's response:
```csharp
public class LLMResponse
{
    // Core Content
    public string Content { get; }           // The main extracted response text
    public List<ToolCall> ToolCalls { get; } // List of tool calls requested by the AI

    // Status & Error Handling
    public bool IsSuccess { get; }           // Whether the request was successful
    public string ErrorMessage { get; }     // Detailed error message on failure

    // Metadata
    public string Id { get; set; }              // Unique ID of the response
    public string Model { get; set; }           // The model name used
    public Usage Usage { get; set; }           // Token usage statistics
    public string RequestId { get; }         // Internal request ID
}

public class Usage
{
    public int PromptTokens { get; set; }     // Number of input tokens
    public int CompletionTokens { get; set; } // Number of output tokens
    public int TotalTokens { get; set; }      // Total tokens
}
```

#### Usage Example

**Reliable Request with Error Handling**
```csharp
var response = await RimAIAPI.SendRequestAsync("Generate a backstory for a space merchant");

if (response.IsSuccess)
{
    Log.Message($"Success! AI Response: {response.Content}");
    
    // You can also check token usage
    if (response.Usage != null)
    {
        Log.Message($"This request consumed {response.Usage.TotalTokens} tokens.");
    }
}
else
{
    // Know exactly what went wrong
    Log.Error($"AI request failed: {response.ErrorMessage}");
}
```

---

### 4. SendBatchRequestAsync - Batch Processing

#### Method Signature
```csharp
public static async Task<List<string>> SendBatchRequestAsync(
    List<string> messages,
    LLMRequestOptions options = null
)
```

#### Parameters

**messages (List<string>, required)**
- List of messages for batch processing
- Recommended batch size: 1-10 messages
- Framework automatically optimizes concurrent processing

**options (LLMRequestOptions, optional)**
- Configuration applied to all requests

#### Return Value: List<string>
- Response content string list in same order as input messages
- Failed requests have null values at corresponding positions
- Continues processing even if individual requests fail

#### Usage Examples

**Batch Translation**
```csharp
var texts = new List<string>
{
    "Hello World",
    "Good Morning", 
    "How are you?",
    "Thank you"
};

var responses = await RimAIAPI.SendBatchRequestAsync(
    texts.Select(t => $"Translate to Chinese: {t}").ToList(),
    new LLMRequestOptions { Temperature = 0.3f }
);

for (int i = 0; i < responses.Count; i++)
{
    if (responses[i].IsSuccess)
    {
        Log.Message($"{texts[i]} -> {responses[i].Content}");
    }
    else
    {
        Log.Error($"Translation failed: {texts[i]} - {responses[i].ErrorMessage}");
    }
}
```

**Batch Data Analysis**
```csharp
var dataQueries = new List<string>
{
    "Analyze current colony food situation",
    "Assess colony defense capabilities", 
    "Check colonist mood status",
    "Review colony resource inventory"
};

var options = new LLMRequestOptions
{
    MaxTokens = 300,
    Temperature = 0.5f,
    EnableCaching = true
};

var reports = await RimAIAPI.SendBatchRequestAsync(dataQueries, options);

// Parallel result processing
Parallel.ForEach(reports.Where(r => !string.IsNullOrEmpty(r)), response => {
    ProcessAnalysisReport(response);
});
```

---

### 5. GetFunctionCallAsync - Get Function Call Suggestions

This advanced API allows you to provide the model with a list of tools (functions) you've defined. Based on the user's `prompt`, the model intelligently decides which tool to call and with what arguments. This enables the creation of complex AI agents that can interact with your game world or other systems.

**Core Idea**: You define the tools, the AI decides when and how to use them.

#### Method Signature
```csharp
// Note: Use a using alias to avoid conflicts with Verse.Tool
using AITool = RimAI.Framework.LLM.Models.Tool;

public static async Task<List<FunctionCallResult>> GetFunctionCallAsync(
    string prompt,
    List<AITool> tools,
    CancellationToken cancellationToken = default
)
```

#### Parameters

**prompt (string, required)**
- The user's input, which the model will use to decide whether to call a tool.

**tools (List<AITool>, required)**
- A list of available tools you provide to the model.
- This is a critical parameter that defines the model's capabilities.

**cancellationToken (CancellationToken, optional)**
- Used to cancel the request.

#### Return Value: `List<FunctionCallResult>`
- A list containing zero or more `FunctionCallResult` objects.
- Returns `null` if the model decides no tool should be called, or if an error occurs.
- The structure of `FunctionCallResult` is as follows:
```csharp
public class FunctionCallResult
{
    public string ToolId { get; set; }       // A unique ID for the tool call
    public string FunctionName { get; set; } // The name of the suggested function to call
    public string Arguments { get; set; }    // A JSON string of the function's arguments
}
```

#### Data Model: Defining a Tool
A tool consists of its type (always "function") and a function definition.

```csharp
// The AITool, FunctionDefinition, and other models are in the
// RimAI.Framework.LLM.Models namespace.

var myTool = new AITool
{
    Type = "function",
    Function = new FunctionDefinition
    {
        Name = "your_function_name", // A unique name for the function
        Description = "A clear description of what this function does", // The model uses this to decide when to use it
        Parameters = new FunctionParameters
        {
            Type = "object",
            Properties = new Dictionary<string, ParameterProperty>
            {
                // Define your parameters
                { "param1", new ParameterProperty { Type = "string", Description = "Description of param1" } },
                { "param2", new ParameterProperty { Type = "number", Description = "Description of param2" } }
            },
            Required = new List<string> { "param1" } // A list of required parameters
        }
    }
};
```

#### Usage Example: Building an In-Game Calculator

Let's say we want the AI to be able to answer math questions, but instead of calculating itself, it should call our game's precise calculation methods.

**Step 1: Define the Tool**
```csharp
using AITool = RimAI.Framework.LLM.Models.Tool;
using RimAI.Framework.LLM.Models; // For FunctionDefinition, etc.

// Create a multiplication tool
var multiplyTool = new AITool
{
    Type = "function",
    Function = new FunctionDefinition
    {
        Name = "multiply",
        Description = "Calculate the product of two numbers",
        Parameters = new FunctionParameters
        {
            Type = "object",
            Properties = new Dictionary<string, ParameterProperty>
            {
                { "a", new ParameterProperty { Type = "number", Description = "The first number" } },
                { "b", new ParameterProperty { Type = "number", Description = "The second number" } }
            },
            Required = new List<string> { "a", "b" }
        }
    }
};

var tools = new List<AITool> { multiplyTool };
```

**Step 2: Call the API**
```csharp
var prompt = "Could you please tell me, if I have 128 units of silver, and each is worth 5.5, what is the total value?";

List<FunctionCallResult> suggestedCalls = await RimAIAPI.GetFunctionCallAsync(prompt, tools);
```

**Step 3: Process the Result and Execute the Local Method**
```csharp
if (suggestedCalls != null && suggestedCalls.Count > 0)
{
    foreach (var call in suggestedCalls)
    {
        Log.Message($"AI suggested calling function: {call.FunctionName}");
        Log.Message($"Arguments (JSON): {call.Arguments}");

        if (call.FunctionName == "multiply")
        {
            // You'll need a class to deserialize the arguments into
            // e.g., public class MultiplyArgs { public double a { get; set; } public double b { get; set; } }
            var args = JsonConvert.DeserializeObject<MultiplyArgs>(call.Arguments);
            
            // Execute your own local C# method
            double result = MyLocalCalculator.Multiply(args.a, args.b);
            
            Log.Message($"Local calculation result: {result}");
            
            // Next steps: You could send this result back to the AI to provide a natural language response to the user.
        }
    }
}
else
{
    Log.Message("The AI did not suggest calling any function.");
}
```

#### Complete Example: Multi-Tool Selection

This more complex example demonstrates how to define multiple tools and let the AI automatically select the most appropriate one for different user questions.

**Step 1: Define All Tools**
```csharp
using AITool = RimAI.Framework.LLM.Models.Tool;
using RimAI.Framework.LLM.Models;
using System.Collections.Generic;

var tools = new List<AITool>
{
    // Tool 1: Multiplication
    new AITool { Function = new FunctionDefinition {
        Name = "mul",
        Description = "Calculate the product of two numbers",
        Parameters = new FunctionParameters {
            Properties = new Dictionary<string, ParameterProperty> {
                { "a", new ParameterProperty { Type = "number", Description = "The first number" } },
                { "b", new ParameterProperty { Type = "number", Description = "The second number" } }
            },
            Required = new List<string> { "a", "b" }
        }
    }},
    // Tool 2: Comparison
    new AITool { Function = new FunctionDefinition {
        Name = "compare",
        Description = "Compare two numbers to see which is greater",
        Parameters = new FunctionParameters {
            Properties = new Dictionary<string, ParameterProperty> {
                { "a", new ParameterProperty { Type = "number", Description = "The first number" } },
                { "b", new ParameterProperty { Type = "number", Description = "The second number" } }
            },
            Required = new List<string> { "a", "b" }
        }
    }},
    // Tool 3: Letter Count
    new AITool { Function = new FunctionDefinition {
        Name = "count_letter_in_string",
        Description = "Count the occurrences of a letter in a string",
        Parameters = new FunctionParameters {
            Properties = new Dictionary<string, ParameterProperty> {
                { "a", new ParameterProperty { Type = "string", Description = "The source string" } },
                { "b", new ParameterProperty { Type = "string", Description = "The letter to count" } }
            },
            Required = new List<string> { "a", "b" }
        }
    }}
};
```

**Step 2: Call the API with Different Prompts**
```csharp
var prompts = new List<string>
{
    "How many times does the letter 'r' appear in 'strawberry'?",
    "Which is smaller, 9.11 or 9.9?"
};

foreach (var prompt in prompts)
{
    Log.Message($"\n--- Processing new prompt: {prompt} ---");
    var suggestedCalls = await RimAIAPI.GetFunctionCallAsync(prompt, tools);

    if (suggestedCalls != null && suggestedCalls.Count > 0)
    {
        var call = suggestedCalls.First();
        Log.Message($"AI suggested calling: '{call.FunctionName}'");
        Log.Message($"With arguments: {call.Arguments}");
        
        // Here, you would call your local C# method based on call.FunctionName
        // and use the result for the next step (e.g., calling the AI again for a final response).
    }
    else
    {
        Log.Message("The AI did not suggest a function call; it might have answered directly.");
        // Or, send the prompt to SendMessageAsync to get a direct reply.
        var directResponse = await RimAIAPI.SendMessageAsync(prompt);
        Log.Message($"AI direct response: {directResponse}");
    }
}
```

**Expected Output**
```
--- Processing new prompt: How many times does the letter 'r' appear in 'strawberry'? ---
AI suggested calling: 'count_letter_in_string'
With arguments: {"a": "strawberry", "b": "r"}

--- Processing new prompt: Which is smaller, 9.11 or 9.9? ---
AI suggested calling: 'compare'
With arguments: {"a": 9.11, "b": 9.9}
```

This powerful feature connects the AI's natural language understanding with your code's execution capabilities, opening up new possibilities for creating smarter and more interactive mod features.

## ⚙️ LLMRequestOptions Comprehensive Parameters

### Core Parameters

```csharp
public class LLMRequestOptions
{
    // Temperature control (0.0-2.0)
    public float? Temperature { get; set; }
    
    // Maximum response tokens
    public int? MaxTokens { get; set; }
    
    // Enable response caching
    public bool EnableCaching { get; set; } = true;
    
    // Request timeout (seconds)
    public int? TimeoutSeconds { get; set; }
    
    // Retry count on failure
    public int? RetryCount { get; set; }
    
    // Top-p sampling parameter
    public float? TopP { get; set; }
    
    // Frequency penalty
    public float? FrequencyPenalty { get; set; }
    
    // Presence penalty
    public float? PresencePenalty { get; set; }
    
    // Stop words list
    public List<string> StopWords { get; set; }
    
    // Custom HTTP headers
    public Dictionary<string, string> CustomHeaders { get; set; }
    
    // User identifier
    public string UserId { get; set; }
}
```

### Parameter Details

**Temperature (float?, 0.0-2.0)**
- Controls randomness and creativity in responses
- 0.0: Deterministic output, ideal for factual questions
- 0.7: Balanced creativity and accuracy, good for general conversation
- 1.0: More creative, suitable for creative writing
- 2.0: Highly random, perfect for brainstorming

```csharp
// Factual questions - low temperature
var factualOptions = new LLMRequestOptions { Temperature = 0.1f };
var response = await RimAIAPI.SendMessageAsync("When was RimWorld released?", factualOptions);

// Creative writing - high temperature
var creativeOptions = new LLMRequestOptions { Temperature = 1.2f };
var story = await RimAIAPI.SendMessageAsync("Write a sci-fi short story", creativeOptions);
```

**MaxTokens (int?)**
- Limits response length
- 1 token ≈ 0.75 English words ≈ 0.5 Chinese characters
- Recommended values: 50-2000

```csharp
// Short responses
var shortOptions = new LLMRequestOptions { MaxTokens = 50 };

// Detailed responses
var detailedOptions = new LLMRequestOptions { MaxTokens = 1000 };
```

**EnableCaching (bool)**
- Controls response caching behavior
- true: Uses cache for identical requests, improves performance
- false: Always sends new requests

#### 🔍 Deep Dive: Cache Similarity Detection Mechanism

**Cache Key Generation Algorithm**:
```
Cache Key = LLM:{MessageHash}:temp={Temperature}:maxtok={MaxTokens}:model={Model}:json={JsonMode}:schema={SchemaHash}:topp={TopP}:...
```

**Parameters Affecting Cache Hits**:
- **Message Content**: Uses `GetHashCode()` for text hashing
- **Temperature**: Creativity parameter, must match exactly
- **MaxTokens**: Maximum tokens, must be identical
- **Model**: AI model name
- **ForceJsonMode**: JSON output flag
- **JsonSchema**: JSON schema hash
- **TopP**: Top-p sampling parameter
- **Other Parameters**: FrequencyPenalty, PresencePenalty, etc.

**Similarity Detection Examples**:
```csharp
// ✅ These requests are considered identical (cache hit)
var req1 = new LLMRequestOptions { Temperature = 0.7f, MaxTokens = 500 };
var req2 = new LLMRequestOptions { Temperature = 0.7f, MaxTokens = 500 };
// Cache key identical: LLM:12345678:temp=0.7:maxtok=500:model=default:json=False

// ❌ These requests are different (cache miss)
var req3 = new LLMRequestOptions { Temperature = 0.8f, MaxTokens = 500 };
// Cache key different: LLM:12345678:temp=0.8:maxtok=500:model=default:json=False

var req4 = new LLMRequestOptions { Temperature = 0.7f, MaxTokens = 600 };
// Cache key different: LLM:12345678:temp=0.7:maxtok=600:model=default:json=False
```

**Cache Optimization Strategies**:
```csharp
// ✅ Standardize configurations for better hit rates
public static class StandardOptions
{
    public static readonly LLMRequestOptions Creative = new LLMRequestOptions 
    { 
        Temperature = 1.0f, MaxTokens = 800, EnableCaching = true 
    };
    
    public static readonly LLMRequestOptions Factual = new LLMRequestOptions 
    { 
        Temperature = 0.2f, MaxTokens = 500, EnableCaching = true 
    };
}

// ✅ Reuse standard configurations
var response1 = await RimAIAPI.SendMessageAsync("Question 1", StandardOptions.Factual);
var response2 = await RimAIAPI.SendMessageAsync("Question 2", StandardOptions.Factual);
// If questions are identical, cache hit occurs
```

**Cache Lifecycle Management**:
- **Default TTL**: 30 minutes
- **LRU Cleanup**: Removes least recently used entries when limit exceeded
- **Periodic Cleanup**: Every 2 minutes for expired entries
- **Memory Monitoring**: Estimates memory usage to prevent leaks

**TimeoutSeconds (int?)**
- Request timeout duration
- Default: 30 seconds
- Recommended range: 5-120 seconds

**RetryCount (int?)**
- Number of retries on failure
- Default: 3 attempts
- Recommended range: 1-5 attempts

## 🏭 Options Factory Methods

Framework provides preset configurations for common scenarios:

### RimAIAPI.Options Static Factory

```csharp
// Creative mode - high temperature for creative content
var creative = RimAIAPI.Options.Creative();
// Equivalent to: new LLMRequestOptions { Temperature = 1.0f, MaxTokens = 800 }

// Factual mode - low temperature for factual Q&A
var factual = RimAIAPI.Options.Factual();  
// Equivalent to: new LLMRequestOptions { Temperature = 0.2f, MaxTokens = 500 }

// Structured output - optimized for JSON responses
var structured = RimAIAPI.Options.Structured();
// Equivalent to: new LLMRequestOptions { Temperature = 0.3f, MaxTokens = 1000 }

// Streaming optimized - ideal for streaming responses
var streaming = RimAIAPI.Options.Streaming();
// Equivalent to: new LLMRequestOptions { EnableCaching = false, MaxTokens = 1500 }
```

### Factory Method Usage Examples

```csharp
// Creative writing
var story = await RimAIAPI.SendMessageAsync(
    "Write a story about space colonization", 
    RimAIAPI.Options.Creative()
);

// Factual queries
var info = await RimAIAPI.SendMessageAsync(
    "How does RimWorld's medical system work?", 
    RimAIAPI.Options.Factual()
);

// Structured data
var json = await RimAIAPI.SendMessageAsync(
    "Return current colony status as JSON", 
    RimAIAPI.Options.Structured()
);
```

## 📊 Statistics and Monitoring APIs

### GetStatistics - Framework Statistics Retrieval

#### Method Signature
```csharp
public static FrameworkStatistics GetStatistics()
```

#### Return Value: FrameworkStatistics
```csharp
public class FrameworkStatistics
{
    public int TotalRequests { get; set; }        // Total request count
    public int SuccessfulRequests { get; set; }  // Successful request count
    public int FailedRequests { get; set; }      // Failed request count
    public double AverageResponseTime { get; set; } // Average response time (ms)
    public int CacheHits { get; set; }           // Cache hit count
    public int CacheMisses { get; set; }         // Cache miss count
    public double CacheHitRate { get; set; }     // Cache hit rate
    public long TotalTokensUsed { get; set; }    // Total token consumption
    public DateTime LastRequestTime { get; set; } // Last request timestamp
    public bool IsHealthy { get; set; }          // System health status
}
```

#### Usage Examples

```csharp
// Retrieve statistics
var stats = RimAIAPI.GetStatistics();

Log.Message($"=== RimAI Framework Statistics ===");
Log.Message($"Total requests: {stats.TotalRequests}");
Log.Message($"Success rate: {(stats.SuccessfulRequests * 100.0 / stats.TotalRequests):F1}%");
Log.Message($"Average response time: {stats.AverageResponseTime:F2}ms");
Log.Message($"Cache hit rate: {stats.CacheHitRate:P2}");
Log.Message($"Total tokens used: {stats.TotalTokensUsed:N0}");
Log.Message($"System health: {(stats.IsHealthy ? "Healthy" : "Unhealthy")}");

// Performance monitoring
if (stats.AverageResponseTime > 5000)
{
    Log.Warning("Response time too high, check network connection");
}

if (stats.CacheHitRate < 0.1)
{
    Log.Warning("Cache hit rate too low, review cache configuration");
}
```

### ClearCache - Cache Management

#### Method Signature
```csharp
public static void ClearCache()
```

#### Usage Scenarios

```csharp
// Periodic cache cleanup
if (stats.CacheHits + stats.CacheMisses > 1000)
{
    RimAIAPI.ClearCache();
    Log.Message("Cache cleared");
}

// Memory pressure response
var memoryUsage = GC.GetTotalMemory(false);
if (memoryUsage > 100 * 1024 * 1024) // Over 100MB
{
    RimAIAPI.ClearCache();
    GC.Collect();
}
```

## 🔧 Advanced Usage and Best Practices

### 📦 Intelligent Caching Deep Dive

#### Cache Key Construction Algorithm

RimAI Framework uses composite cache keys for precise request similarity detection:

```csharp
// Internal cache key generation logic (simplified)
private string GenerateCacheKey(string prompt, LLMRequestOptions options)
{
    var keyBuilder = new StringBuilder();
    keyBuilder.Append("LLM:");
    keyBuilder.Append(prompt?.GetHashCode().ToString() ?? "null");
    
    if (options != null)
    {
        keyBuilder.Append($":temp={options.Temperature}");
        keyBuilder.Append($":maxtok={options.MaxTokens}"); 
        keyBuilder.Append($":model={options.Model ?? "default"}");
        keyBuilder.Append($":json={options.ForceJsonMode}");
        
        if (options.JsonSchema != null)
            keyBuilder.Append($":schema={options.JsonSchema.GetHashCode()}");
        if (options.TopP.HasValue)
            keyBuilder.Append($":topp={options.TopP}");
    }
    
    return keyBuilder.ToString();
}
```

#### Cache Hit Requirements

**Parameters that must match exactly**:
1. **Message Content**: String exact match
2. **Temperature**: Precision to decimal places
3. **MaxTokens**: Numerical exact match
4. **Model**: Model name string match
5. **ForceJsonMode**: Boolean value match
6. **JsonSchema**: Schema hash value match
7. **TopP**: If set, must match exactly

#### Practical Cache Testing

```csharp
// Test similarity detection
public async Task TestCacheSimilarity()
{
    var stats = RimAIAPI.GetStatistics();
    var initialHits = stats.CacheHits;
    
    // First request - cache miss
    var response1 = await RimAIAPI.SendMessageAsync("Hello World", 
        new LLMRequestOptions { Temperature = 0.7f, MaxTokens = 100 });
    
    // Second identical request - should hit cache
    var response2 = await RimAIAPI.SendMessageAsync("Hello World", 
        new LLMRequestOptions { Temperature = 0.7f, MaxTokens = 100 });
    
    // Verify cache hit
    var newStats = RimAIAPI.GetStatistics();
    var cacheHitIncrease = newStats.CacheHits - initialHits;
    
    Log.Message($"Cache hits increased by: {cacheHitIncrease}");
    Log.Message($"Second request from cache: {response2.FromCache}");
}
```

#### Cache Optimization Strategies

**Strategy 1: Parameter Standardization**
```csharp
// ✅ Define standard parameter sets
public static class CacheOptimizedOptions
{
    // Factual queries - low temperature, high cache value
    public static readonly LLMRequestOptions Facts = new LLMRequestOptions
    {
        Temperature = 0.2f,
        MaxTokens = 300,
        EnableCaching = true
    };
    
    // Creative content - moderate temperature, balanced creativity and caching
    public static readonly LLMRequestOptions Creative = new LLMRequestOptions
    {
        Temperature = 0.8f,
        MaxTokens = 800,
        EnableCaching = true
    };
    
    // Structured data - fixed format, high cache value
    public static readonly LLMRequestOptions Structured = new LLMRequestOptions
    {
        Temperature = 0.1f,
        MaxTokens = 1000,
        ForceJsonMode = true,
        EnableCaching = true
    };
}
```

**Strategy 2: Message Templating**
```csharp
// ✅ Use templates for improved similarity
public class MessageTemplates
{
    public static string AnalyzeColony(string dataType) =>
        $"Analyze current colony {dataType} situation and provide detailed report";
    
    public static string TranslateText(string text, string targetLang) =>
        $"Translate the following text to {targetLang}: {text}";
}

// Use templates - improve cache hits
var analysis1 = await RimAIAPI.SendMessageAsync(
    MessageTemplates.AnalyzeColony("food"), 
    CacheOptimizedOptions.Facts
);

var analysis2 = await RimAIAPI.SendMessageAsync(
    MessageTemplates.AnalyzeColony("defense"), 
    CacheOptimizedOptions.Facts  // Same parameters, possible cache hits for other parts
);
```

**Strategy 3: Cache Prewarming**
```csharp
// ✅ Prewarm cache with common queries
public async Task PrewarmCache()
{
    var commonQueries = new[]
    {
        "What is the current game status?",
        "Any suggestions?",
        "Analyze current situation",
        "What should I do next?"
    };
    
    foreach (var query in commonQueries)
    {
        // Prewarm cache, ignore results
        _ = await RimAIAPI.SendMessageAsync(query, CacheOptimizedOptions.Facts);
    }
    
    Log.Message("Cache prewarming complete");
}
```

### 1. Asynchronous Processing Best Practices

```csharp
// ✅ Correct: Use ConfigureAwait(false)
public async Task ProcessAIRequestAsync(string message)
{
    var response = await RimAIAPI.SendMessageAsync(message)
        .ConfigureAwait(false);
    
    // Process response
    ProcessResponse(response);
}

// ✅ Correct: Exception handling
public async Task SafeAIRequestAsync(string message)
{
    try
    {
        var response = await RimAIAPI.SendMessageAsync(message);
        if (!string.IsNullOrEmpty(response))
        {
            // Success handling
        }
    }
    catch (OperationCanceledException)
    {
        // Operation cancelled
    }
    catch (RimAIException ex)
    {
        // RimAI-specific exceptions
        Log.Error($"RimAI Exception: {ex.Message}");
    }
    catch (Exception ex)
    {
        // Other exceptions
        Log.Error($"Unknown error: {ex.Message}");
    }
}
```

### 2. Performance Optimization Techniques

```csharp
// ✅ Cache optimization: Use caching for similar requests
var options = new LLMRequestOptions 
{ 
    EnableCaching = true,
    Temperature = 0.3f // Low temperature improves cache hit rate
};

// ✅ Batch processing: Reduce network overhead
var messages = new List<string> { /* multiple messages */ };
var responses = await RimAIAPI.SendBatchRequestAsync(messages, options);

// ✅ Timeout control: Avoid long waits
var timeoutOptions = new LLMRequestOptions 
{ 
    TimeoutSeconds = 15,
    RetryCount = 2
};
```

### 3. Memory Management

```csharp
// ✅ Regular cache cleanup
public class AIManager
{
    private static DateTime lastCacheClean = DateTime.MinValue;
    
    public async Task<string> ProcessRequestAsync(string message)
    {
        // Clean cache every hour
        if (DateTime.Now - lastCacheClean > TimeSpan.FromHours(1))
        {
            RimAIAPI.ClearCache();
            lastCacheClean = DateTime.Now;
        }
        
        return await RimAIAPI.SendMessageAsync(message);
    }
}
```

### 4. Error Handling Strategies

```csharp
public async Task<string> ResilientRequestAsync(string message, int maxRetries = 3)
{
    for (int attempt = 0; attempt < maxRetries; attempt++)
    {
        try
        {
            var response = await RimAIAPI.SendMessageAsync(message);
            if (!string.IsNullOrEmpty(response))
                return response;
                
            // Wait before retry on failure
            await Task.Delay(TimeSpan.FromSeconds(Math.Pow(2, attempt)));
        }
        catch (Exception ex)
        {
            if (attempt == maxRetries - 1)
                throw; // Last attempt failed, throw exception
                
            Log.Warning($"Request failed, {attempt + 1}/{maxRetries}, retrying: {ex.Message}");
            await Task.Delay(TimeSpan.FromSeconds(Math.Pow(2, attempt)));
        }
    }
    
    throw new InvalidOperationException($"Request failed after {maxRetries} attempts");
}
```

## 🚨 Common Issues and Solutions

### 1. Network Connection Errors
```csharp
// Error: ConnectionException
// Solution: Check network connection, increase retry count
var options = new LLMRequestOptions 
{ 
    TimeoutSeconds = 60,
    RetryCount = 5
};
```

### 2. Token Limit Errors
```csharp
// Error: TokenLimitException  
// Solution: Reduce MaxTokens or split long messages
var options = new LLMRequestOptions { MaxTokens = 500 };

// Or split long messages
if (message.Length > 2000)
{
    var chunks = SplitMessage(message, 2000);
    var responses = await RimAIAPI.SendBatchRequestAsync(chunks);
}
```

### 3. Configuration Errors
```csharp
// Error: ConfigurationException
// Solution: Check configuration files and API keys
```

---

## 📚 Related Documentation

- [Quick Start Guide](EN_v3.0_API_Quick_Start.md) - Fast onboarding and common scenarios
- [Framework Features](CN_v3.0_功能特性.md) - Detailed feature overview
- [Migration Guide](CN_v3.0_迁移指南.md) - Upgrade instructions from v2.x
- [Architecture Design](CN_v3.0_架构改造完成报告.md) - Technical architecture documentation

**RimAI Framework v3.0 - Making AI integration simple and powerful!** 🚀
