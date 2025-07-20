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

## 📝 Message Processing APIs

### 1. SendMessageAsync - Standard Message Processing

#### Method Signature
```csharp
public static async Task<LLMResponse> SendMessageAsync(
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

#### Return Value: LLMResponse
```csharp
public class LLMResponse
{
    public string Content { get; set; }           // Response content
    public bool IsSuccess { get; set; }           // Success indicator
    public string ErrorMessage { get; set; }     // Error details (if any)
    public TimeSpan ResponseTime { get; set; }   // Response latency
    public int TokensUsed { get; set; }          // Token consumption
    public bool FromCache { get; set; }          // Cache hit indicator
    public string RequestId { get; set; }        // Unique request identifier
}
```

#### Usage Examples

**Basic Request**
```csharp
// Simplest possible call
var response = await RimAIAPI.SendMessageAsync("Hello, AI!");
if (response.IsSuccess)
{
    Log.Message($"AI Response: {response.Content}");
    Log.Message($"Latency: {response.ResponseTime.TotalMilliseconds}ms");
    Log.Message($"Cache Hit: {response.FromCache}");
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
    if (!response.IsSuccess)
    {
        Log.Error($"Request failed: {response.ErrorMessage}");
        return;
    }
    
    // Process successful response
    ProcessResponse(response.Content);
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

### 2. SendMessageStreamAsync - Streaming Message Processing

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

### 3. SendBatchRequestAsync - Batch Processing

#### Method Signature
```csharp
public static async Task<List<LLMResponse>> SendBatchRequestAsync(
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

#### Return Value: List<LLMResponse>
- Response list in same order as input messages
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
Parallel.ForEach(reports.Where(r => r.IsSuccess), response => {
    ProcessAnalysisReport(response.Content);
});
```

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
        if (response.IsSuccess)
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
    
    public async Task<LLMResponse> ProcessRequestAsync(string message)
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
public async Task<LLMResponse> ResilientRequestAsync(string message, int maxRetries = 3)
{
    for (int attempt = 0; attempt < maxRetries; attempt++)
    {
        try
        {
            var response = await RimAIAPI.SendMessageAsync(message);
            if (response.IsSuccess)
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
