# 🚀 RimAI Framework v3.0 API Quick Start Guide

## 📋 Overview

Get up and running with RimAI Framework v3.0 in just 5 minutes! This guide covers everything you need to know to integrate AI capabilities into your RimWorld mod.

**Perfect for**: New developers, rapid prototyping, and common use cases.

---

## ⚡ 5-Minute Quick Start

### Step 1: Add Framework Dependency
Add RimAI Framework as a dependency in your mod's `loadFolders.xml`:

```xml
<li>RimAI.Framework</li>
```

### Step 2: Import Namespaces
```csharp
using RimAI.Framework.API;
using RimAI.Framework.LLM.Models;
```

### Step 3: Your First AI Request
```csharp
public async void SendFirstRequest()
{
    // Simple request - that's it!
    var response = await RimAIAPI.SendMessageAsync("Analyze this colony's current status");
    
    if (response.IsSuccess)
    {
        Log.Message($"AI Response: {response.Content}");
        Log.Message($"Response time: {response.ResponseTime.TotalMilliseconds}ms");
        Log.Message($"From cache: {response.FromCache}");
    }
    else
    {
        Log.Error($"Request failed: {response.ErrorMessage}");
    }
}
```

**Congratulations!** You've just made your first AI request. The framework handles all the complexity behind the scenes.

---

## 🎯 Common Scenarios

### Scenario 1: Colony Analysis & Recommendations

```csharp
public class ColonyAnalyzer
{
    public async Task AnalyzeColony()
    {
        var queries = new[]
        {
            "What are the current threats to this colony?",
            "How can we improve food production?", 
            "What defensive improvements are needed?",
            "Which colonists need medical attention?"
        };

        foreach (var query in queries)
        {
            var response = await RimAIAPI.SendMessageAsync(
                query, 
                RimAIAPI.Options.Factual() // Preset for factual responses
            );
            
            if (response.IsSuccess)
            {
                ProcessAnalysis(query, response.Content);
            }
        }
    }
    
    private void ProcessAnalysis(string question, string analysis)
    {
        // Your colony management logic here
        Log.Message($"Q: {question}");
        Log.Message($"A: {analysis}");
    }
}
```

### Scenario 2: Dynamic Event Storytelling

```csharp
public class EventNarrator
{
    public async Task NarrateEvent(string eventType, List<Pawn> involved)
    {
        var prompt = $"Create an engaging narrative for a {eventType} event involving {string.Join(", ", involved.Select(p => p.Name))}";
        
        // Use streaming for real-time narrative display
        var narrative = new StringBuilder();
        
        await RimAIAPI.SendMessageStreamAsync(
            prompt,
            chunk => {
                narrative.Append(chunk);
                UpdateEventDialog(narrative.ToString()); // Real-time updates
            },
            RimAIAPI.Options.Creative() // Preset for creative content
        );
        
        // Final narrative is ready
        ShowCompletedNarrative(narrative.ToString());
    }
}
```

### Scenario 3: Batch Processing Multiple Items

```csharp
public class ItemDescriptionGenerator
{
    public async Task GenerateItemDescriptions(List<Thing> items)
    {
        // Prepare batch requests
        var requests = items.Select(item => 
            $"Generate a creative description for: {item.def.label}"
        ).ToList();
        
        // Process all at once
        var responses = await RimAIAPI.SendBatchRequestAsync(
            requests, 
            RimAIAPI.Options.Creative()
        );
        
        // Apply results
        for (int i = 0; i < responses.Count; i++)
        {
            if (responses[i].IsSuccess && i < items.Count)
            {
                ApplyDescription(items[i], responses[i].Content);
            }
        }
    }
}
```

### Scenario 4: Function Calling
```csharp
// Let the AI decide whether to call functions you provide.
// Note: An alias is recommended to avoid conflict with Verse.Tool
using AITool = RimAI.Framework.LLM.Models.Tool;

public async Task AskQuestionWithTools()
{
    // Define a list of available tools
    var tools = new List<AITool> 
    { 
        /* ... your tool definitions here ... */ 
    };
    var prompt = "What is 128 multiplied by 5.5?";

    // The AI returns the function name and arguments
    var functionCalls = await RimAIAPI.GetFunctionCallAsync(prompt, tools);

    if (functionCalls != null && functionCalls.Count > 0)
    {
        var call = functionCalls.First();
        Log.Message($"AI suggests calling: {call.FunctionName}");
        Log.Message($"With arguments: {call.Arguments}");
        // Your logic to execute the function and handle its result would go here
    }
}
```

---

## 🔧 Preset Options (Recommended)

Instead of configuring parameters manually, use our preset options for common scenarios:

### Factual Mode
```csharp
var response = await RimAIAPI.SendMessageAsync(
    "What is the current temperature?", 
    RimAIAPI.Options.Factual()
);
// Perfect for: Data analysis, status reports, factual questions
```

### Creative Mode
```csharp
var story = await RimAIAPI.SendMessageAsync(
    "Write a story about space colonists", 
    RimAIAPI.Options.Creative()
);
// Perfect for: Storytelling, flavor text, creative content
```

### Streaming Mode
```csharp
await RimAIAPI.SendMessageStreamAsync(
    "Generate a long description",
    chunk => UpdateUI(chunk),
    RimAIAPI.Options.Streaming()
);
// Perfect for: Real-time updates, long responses, interactive dialogs
```

### Structured Mode
```csharp
var data = await RimAIAPI.SendMessageAsync(
    "Return colony stats as JSON", 
    RimAIAPI.Options.Structured()
);
// Perfect for: JSON responses, data exchange, structured output
```

---

## ⚡ Performance Tips

### Tip 1: Leverage Caching
```csharp
// ✅ Good - Use consistent parameters for better cache hits
var standardOptions = RimAIAPI.Options.Factual();

var response1 = await RimAIAPI.SendMessageAsync("Question 1", standardOptions);
var response2 = await RimAIAPI.SendMessageAsync("Question 2", standardOptions);
// If questions are identical, second request hits cache!
```

### Tip 2: Batch Similar Requests
```csharp
// ✅ Better - Process multiple requests together
var questions = new List<string> { "Q1", "Q2", "Q3" };
var responses = await RimAIAPI.SendBatchRequestAsync(questions);

// ❌ Avoid - Sequential individual requests
foreach (var q in questions) 
{
    await RimAIAPI.SendMessageAsync(q); // Slower
}
```

### Tip 3: Use Streaming for Long Responses
```csharp
// ✅ Good - Streaming provides immediate feedback
await RimAIAPI.SendMessageStreamAsync(
    "Write a detailed analysis",
    chunk => ShowProgress(chunk), // User sees progress
    RimAIAPI.Options.Streaming()
);
```

---

## 🔍 Framework Status & Monitoring

### Check Framework Health
```csharp
public void CheckFrameworkStatus()
{
    var stats = RimAIAPI.GetStatistics();
    
    Log.Message($"Total requests: {stats.TotalRequests}");
    Log.Message($"Success rate: {stats.SuccessfulRequests * 100.0 / stats.TotalRequests:F1}%");
    Log.Message($"Cache hit rate: {stats.CacheHitRate:P2}");
    Log.Message($"Average response time: {stats.AverageResponseTime:F0}ms");
    
    if (!stats.IsHealthy)
    {
        Log.Warning("Framework health issues detected!");
    }
}
```

### Clear Cache When Needed
```csharp
public void ManageCache()
{
    var stats = RimAIAPI.GetStatistics();
    
    // Clear cache if hit rate is too low
    if (stats.CacheHitRate < 0.1 && stats.TotalRequests > 100)
    {
        RimAIAPI.ClearCache();
        Log.Message("Cache cleared due to low hit rate");
    }
}
```

---

## 🚨 Essential Error Handling

### Basic Error Handling
```csharp
public async Task SafeRequest(string message)
{
    try
    {
        var response = await RimAIAPI.SendMessageAsync(message);
        
        if (response.IsSuccess)
        {
            ProcessResponse(response.Content);
        }
        else
        {
            Log.Error($"Request failed: {response.ErrorMessage}");
            ShowUserFriendlyError();
        }
    }
    catch (RimAIException ex)
    {
        Log.Error($"RimAI error: {ex.Message}");
        // Handle framework-specific issues
    }
    catch (Exception ex)
    {
        Log.Error($"Unexpected error: {ex.Message}");
        // Handle general errors
    }
}
```

### Timeout Handling
```csharp
public async Task RequestWithTimeout(string message)
{
    var options = new LLMRequestOptions 
    { 
        TimeoutSeconds = 15,  // 15 second timeout
        RetryCount = 2        // Retry twice on failure
    };
    
    var response = await RimAIAPI.SendMessageAsync(message, options);
}
```

---

## 📊 Real-World Integration Example

Here's a complete example showing how to integrate RimAI into a custom mod:

```csharp
[StaticConstructorOnStartup]
public class MyAIMod : Mod
{
    public MyAIMod(ModContentPack content) : base(content)
    {
        Log.Message("AI-Enhanced Mod loaded!");
    }
}

public class SmartColonyManager
{
    private static readonly LLMRequestOptions AnalysisOptions = RimAIAPI.Options.Factual();
    
    public async Task PerformSmartAnalysis()
    {
        // Check framework health first
        var stats = RimAIAPI.GetStatistics();
        if (!stats.IsHealthy)
        {
            Log.Warning("AI framework unavailable, skipping analysis");
            return;
        }
        
        try
        {
            // Analyze colony status
            var statusResponse = await RimAIAPI.SendMessageAsync(
                "Analyze current colony status and provide 3 key recommendations", 
                AnalysisOptions
            );
            
            if (statusResponse.IsSuccess)
            {
                ShowAnalysisResults(statusResponse.Content);
                
                // Log performance metrics
                Log.Message($"Analysis completed in {statusResponse.ResponseTime.TotalSeconds:F1}s " +
                          $"(cached: {statusResponse.FromCache})");
            }
            else
            {
                ShowFallbackAnalysis();
            }
        }
        catch (Exception ex)
        {
            Log.Error($"Analysis failed: {ex.Message}");
            ShowFallbackAnalysis();
        }
    }
    
    private void ShowAnalysisResults(string analysis)
    {
        Find.WindowStack.Add(new Dialog_MessageBox(analysis));
    }
    
    private void ShowFallbackAnalysis()
    {
        // Provide basic analysis when AI is unavailable
        Find.WindowStack.Add(new Dialog_MessageBox("Colony analysis unavailable"));
    }
}
```

---

## 🎓 What's Next?

### Ready for More Advanced Features?
- **[Detailed API Reference](EN_v3.0_API_Comprehensive_Guide.md)** - Complete parameter reference, advanced options
- **[Framework Features](CN_v3.0_功能特性.md)** - Caching, batching, streaming, configuration
- **[Migration Guide](CN_v3.0_迁移指南.md)** - Upgrading from v2.x
- **[Architecture Overview](CN_v3.0_架构改造完成报告.md)** - Technical deep-dive

### Common Questions
- **Q: How do I configure the AI model?** A: Use RimWorld's mod settings menu
- **Q: Can I use this offline?** A: No, requires internet connection to AI services
- **Q: Is there a request limit?** A: Depends on your AI service provider's limits
- **Q: How do I handle long responses?** A: Use `SendMessageStreamAsync` for real-time streaming

---

## 💡 Pro Tips for Success

1. **Start Simple**: Use preset options (`RimAIAPI.Options.*`) before custom parameters
2. **Monitor Performance**: Check `GetStatistics()` regularly to optimize your usage
3. **Handle Failures Gracefully**: Always provide fallback behavior when AI is unavailable
4. **Leverage Caching**: Reuse identical requests to improve performance
5. **Use Streaming for UX**: Stream long responses to keep users engaged

**Happy coding with RimAI Framework v3.0!** 🚀

---

## 📚 Related Documentation

- [Comprehensive API Guide](EN_v3.0_API_Comprehensive_Guide.md) - Complete method reference and advanced usage
- [Framework Features](CN_v3.0_功能特性.md) - Detailed feature overview
- [Migration Guide](CN_v3.0_迁移指南.md) - Upgrade instructions from v2.x
- [Architecture Design](CN_v3.0_架构改造完成报告.md) - Technical architecture documentation

**RimAI Framework v3.0 - Making AI integration simple and powerful!** 🚀
