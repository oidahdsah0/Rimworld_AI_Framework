# RimAI Framework API Usage Guide

## Overview

RimAI Framework is a powerful AI framework designed for RimWorld, providing complete Large Language Model (LLM) integration capabilities. The framework supports both streaming and non-streaming responses, custom parameter control, enforced JSON format output, and other advanced features.

## Quick Start

### 1. Basic Dependencies

```csharp
using RimAI.Framework.API;
using RimAI.Framework.LLM.Models;
using System.Threading.Tasks;
```

### 2. Check Framework Status

```csharp
// Check if the framework is initialized
if (RimAIAPI.IsInitialized)
{
    Log.Message("RimAI Framework is ready");
}

// Check current streaming settings
bool streamingEnabled = RimAIAPI.IsStreamingEnabled;
```

## Core API Features

### 1. Basic Chat API

#### Simple Message Sending

```csharp
// Simplest message sending (using default settings)
public async void SendSimpleMessage()
{
    string response = await RimAIAPI.SendMessageAsync("Describe a day in the life of a RimWorld colonist");
    if (response != null)
    {
        Log.Message($"AI Response: {response}");
    }
}
```

#### Message Sending with Options

```csharp
// Send message with custom options
public async void SendMessageWithOptions()
{
    var options = new LLMRequestOptions
    {
        Temperature = 0.8,        // Control creativity
        MaxTokens = 1000,         // Maximum token count
        EnableStreaming = false   // Disable streaming output
    };

    string response = await RimAIAPI.SendMessageAsync(
        "Create an interesting RimWorld event", 
        options
    );
    
    if (response != null)
    {
        Log.Message($"Creative Response: {response}");
    }
}
```

### 2. Streaming Response API

#### Basic Streaming Output

```csharp
// Receive streaming response, display word by word
public async void SendStreamingMessage()
{
    await RimAIAPI.SendStreamingMessageAsync(
        "Tell a colonist adventure story",
        chunk => 
        {
            // This callback is called for each text chunk received
            Log.Message($"Received chunk: {chunk}");
            // Here you can update UI to display content in real-time
        }
    );
}
```

#### Streaming Output with Options

```csharp
// Streaming output with custom options
public async void SendStreamingWithOptions()
{
    var options = RimAIAPI.Options.Creative(temperature: 1.1); // High creativity

    await RimAIAPI.SendStreamingMessageAsync(
        "Describe a crazy RimWorld scenario",
        chunk => UpdateStoryUI(chunk), // Update story UI
        options
    );
}
```

## Preset Options

RimAI Framework provides multiple preset options for convenient use in different scenarios:

### 1. Streaming Control Options

```csharp
// Force streaming mode
var streamingOptions = RimAIAPI.Options.Streaming(
    temperature: 0.7,
    maxTokens: 1500
);

// Force non-streaming mode
var nonStreamingOptions = RimAIAPI.Options.NonStreaming(
    temperature: 0.5,
    maxTokens: 800
);
```

### 2. Scenario-Specific Options

```csharp
// Creative writing (high temperature)
var creativeOptions = RimAIAPI.Options.Creative(temperature: 1.2);
string story = await RimAIAPI.SendMessageAsync("Write a sci-fi story", creativeOptions);

// Factual queries (low temperature)
var factualOptions = RimAIAPI.Options.Factual(temperature: 0.3);
string info = await RimAIAPI.SendMessageAsync("Explain quantum physics", factualOptions);

// JSON format output
var jsonOptions = RimAIAPI.Options.Json(temperature: 0.7);
string jsonData = await RimAIAPI.SendMessageAsync("Return colonist info in JSON format", jsonOptions);
```

## Advanced API Features

### 1. Custom Service

For complete control over request parameters:

```csharp
public async void UseCustomService()
{
    var customService = RimAIAPI.GetCustomService();
    if (customService != null)
    {
        var request = new CustomRequest
        {
            Model = "gpt-4",
            Messages = new List<object> 
            {
                new { role = "system", content = "You are a RimWorld expert" },
                new { role = "user", content = "Analyze this colony layout" }
            },
            Temperature = 0.8,
            MaxTokens = 2000,
            ResponseFormat = new { type = "json_object" },
            AdditionalParameters = new Dictionary<string, object>
            {
                ["top_p"] = 0.9,
                ["frequency_penalty"] = 0.1
            }
        };

        var response = await customService.SendCustomRequestAsync(request);
        if (response.Error == null)
        {
            Log.Message($"Custom Response: {response.Content}");
        }
    }
}
```

### 2. JSON Service

Enforce valid JSON format responses:

```csharp
public async void UseJsonService()
{
    var jsonService = RimAIAPI.GetJsonService();
    if (jsonService != null)
    {
        var options = new LLMRequestOptions { Temperature = 0.5 };
        
        // Generic JSON response
        var response = await jsonService.SendJsonRequestAsync<ColonistData>(
            "Return detailed information about a colonist",
            options
        );

        if (response.Success)
        {
            ColonistData colonist = response.Data;
            Log.Message($"Colonist: {colonist.Name}, Age: {colonist.Age}");
        }
    }
}

// Example data model
public class ColonistData
{
    public string Name { get; set; }
    public int Age { get; set; }
    public List<string> Skills { get; set; }
    public string Background { get; set; }
}
```

### 3. Mod Service

Enhanced integration for other mods:

```csharp
public async void UseModService()
{
    var modService = RimAIAPI.GetModService();
    if (modService != null)
    {
        // Mod-specific enhanced features
        var response = await modService.ProcessModRequest("Colonist psychological analysis");
        Log.Message($"Mod Service Response: {response}");
    }
}
```

## Utility Methods

### 1. Connection Testing

```csharp
public async void TestAPIConnection()
{
    var (success, message) = await RimAIAPI.TestConnectionAsync();
    
    if (success)
    {
        Log.Message($"Connection successful: {message}");
    }
    else
    {
        Log.Error($"Connection failed: {message}");
    }
}
```

### 2. Request Management

```csharp
// Cancel all ongoing requests
public void CancelAllRequests()
{
    RimAIAPI.CancelAllRequests();
    Log.Message("Cancelled all AI requests");
}

// Refresh settings (when user changes configuration)
public void RefreshSettings()
{
    RimAIAPI.RefreshSettings();
    Log.Message("Refreshed AI settings");
}
```

## Complete Example: Smart Event Generator

```csharp
public class SmartEventGenerator
{
    private string currentStory = "";

    // Generate random event
    public async Task<string> GenerateRandomEvent()
    {
        var options = RimAIAPI.Options.Creative(temperature: 1.0);
        
        return await RimAIAPI.SendMessageAsync(
            "Generate an interesting random event description for RimWorld", 
            options
        );
    }

    // Stream story generation
    public async Task GenerateStoryStream(Action<string> onStoryUpdate)
    {
        currentStory = "";
        var options = RimAIAPI.Options.Streaming(temperature: 0.9, maxTokens: 2000);

        await RimAIAPI.SendStreamingMessageAsync(
            "Create a long story about a RimWorld colony",
            chunk => 
            {
                currentStory += chunk;
                onStoryUpdate?.Invoke(currentStory);
            },
            options
        );
    }

    // Analyze colonist data (JSON format)
    public async Task<ColonistAnalysis> AnalyzeColonist(string colonistInfo)
    {
        var jsonService = RimAIAPI.GetJsonService();
        var options = RimAIAPI.Options.Json(temperature: 0.4);

        var response = await jsonService.SendJsonRequestAsync<ColonistAnalysis>(
            $"Analyze this colonist's information and return detailed analysis: {colonistInfo}",
            options
        );

        return response.Success ? response.Data : null;
    }
}

public class ColonistAnalysis
{
    public string Name { get; set; }
    public string PsychologicalProfile { get; set; }
    public List<string> Strengths { get; set; }
    public List<string> Weaknesses { get; set; }
    public string Recommendation { get; set; }
}
```

## Best Practices

### 1. Error Handling

```csharp
public async Task<string> SafeAICall(string prompt)
{
    try
    {
        if (!RimAIAPI.IsInitialized)
        {
            Log.Warning("RimAI Framework not initialized");
            return null;
        }

        var response = await RimAIAPI.SendMessageAsync(prompt);
        return response ?? "AI temporarily unavailable";
    }
    catch (Exception ex)
    {
        Log.Error($"AI call failed: {ex.Message}");
        return null;
    }
}
```

### 2. Performance Optimization

```csharp
// Use appropriate Temperature values
// 0.0-0.3: Factual queries, data analysis
// 0.4-0.7: Balanced creativity and accuracy
// 0.8-1.2: Creative writing, story generation
// 1.3-2.0: Experimental, highly random

// Control Token count to avoid overly long responses
var options = new LLMRequestOptions 
{ 
    MaxTokens = 500,  // Limit response length
    Temperature = 0.6 
};
```

### 3. User Experience

```csharp
// Use streaming output for long responses
public async void ShowProgressiveStory()
{
    var storyText = "";
    
    await RimAIAPI.SendStreamingMessageAsync(
        "Tell a long RimWorld story",
        chunk => 
        {
            storyText += chunk;
            // Update UI in real-time, let users see content being generated
            UpdateStoryDisplay(storyText);
        }
    );
}
```

## Configuration

Global settings for the framework are configured through RimWorld's Mod Settings interface:

- **API Key**: OpenAI or other compatible API key
- **Endpoint URL**: API endpoint address
- **Model Name**: Model name to use
- **Temperature**: Global creativity parameter (0.0-2.0)
- **Enable Streaming**: Global streaming output toggle
- **Enable Embeddings**: Embedding feature toggle

## Important Notes

1. **Initialization Check**: Always check `RimAIAPI.IsInitialized` before using the API
2. **Exception Handling**: Network requests may fail, proper error handling is needed
3. **Token Limits**: Be aware of API token usage limits and costs
4. **Thread Safety**: All APIs are thread-safe and can be called from any context
5. **Settings Changes**: Call `RefreshSettings()` after modifying settings to ensure they take effect

---

This framework provides the RimWorld modding community with powerful and flexible AI integration capabilities, making it easy to implement everything from simple text generation to complex data analysis.
