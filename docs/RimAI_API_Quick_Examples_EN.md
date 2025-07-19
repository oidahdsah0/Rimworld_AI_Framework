# RimAI API Quick Start Examples

This document provides practical code examples for using the RimAI Framework API. These examples show common usage patterns and best practices.

## Basic Usage Examples

### Example 1: Simple Message Sending

```csharp
public async Task SimpleExample()
{
    // Get the LLM service and send a simple message
    var llmManager = RimAIAPI.GetLLMManager();
    
    var result = await llmManager.GenerateAsync(
        "Help me name this colonist", 
        new RequestOptions()
    );
    
    if (result.Success)
    {
        Log.Message($"AI Response: {result.Content}");
    }
    else
    {
        Log.Error($"Request failed: {result.ErrorMessage}");
    }
}
```

### Example 2: Streaming Response

```csharp
public async Task StreamingExample()
{
    var llmManager = RimAIAPI.GetLLMManager();
    
    await llmManager.GenerateStreamingAsync(
        "Tell me a story about this colony",
        new RequestOptions 
        { 
            Temperature = 0.8,
            MaxTokens = 200 
        },
        OnPartialResponse  // Callback function
    );
}

private void OnPartialResponse(string partialResponse)
{
    // Handle streaming data - update UI in real-time
    UpdateUI(partialResponse);
}
```

### Example 3: Using JSON Service

```csharp
public async Task JsonExample()
{
    var jsonService = RimAIAPI.GetJsonService();
    
    var prompt = @"Generate a colonist with the following structure:
    {
        ""Name"": ""string"",
        ""Age"": number,
        ""Skills"": [
            {""Name"": ""string"", ""Level"": number}
        ]
    }";
    
    var result = await jsonService.GenerateJsonAsync<ColonistData>(
        prompt,
        new JsonRequestOptions { Temperature = 0.7 }
    );
    
    if (result.Success)
    {
        Log.Message($"Generated colonist: {result.Data.Name}, Age: {result.Data.Age}");
    }
}
```

### Example 4: Custom Request with Full Control

```csharp
public async Task CustomExample()
{
    var customService = RimAIAPI.GetCustomService();
    var request = new CustomRequest
    {
        Model = "gpt-4",
        Messages = new List<object>
        {
            new { role = "system", content = "You are a Rimworld storyteller" },
            new { role = "user", content = "Create an interesting event" }
        },
        Temperature = 0.9,
        MaxTokens = 500,
        Stream = false
    };
    
    var response = await customService.SendCustomRequestAsync(request);
    if (!string.IsNullOrEmpty(response.Error))
    {
        Log.Error($"Custom request failed: {response.Error}");
    }
}
```

### Example 5: Using the Mod Service

```csharp
public async Task ModServiceExample()
{
    var modService = RimAIAPI.GetModService();
    
    // Send message with mod context
    var response = await modService.SendMessageAsync(
        "myModId", 
        "Analyze this situation",
        new RequestOptions { Temperature = 0.5 }
    );
}
```

## Complete Example Class

```csharp
using RimAI.Framework;
using System.Threading.Tasks;
using System.Collections.Generic;
using Verse;

namespace MyMod
{
    public class RimAIExamples
    {
        // Example 1: Simple message sending
        public async Task SimpleExample()
        {
            var llmManager = RimAIAPI.GetLLMManager();
            
            var result = await llmManager.GenerateAsync(
                "Help me name this colonist", 
                new RequestOptions()
            );
            
            if (result.Success)
            {
                Log.Message($"AI Response: {result.Content}");
            }
            else
            {
                Log.Error($"Request failed: {result.ErrorMessage}");
            }
        }

        // Example 2: Streaming response
        public async Task StreamingExample()
        {
            var llmManager = RimAIAPI.GetLLMManager();
            
            await llmManager.GenerateStreamingAsync(
                "Tell me a story about this colony",
                new RequestOptions 
                { 
                    Temperature = 0.8,
                    MaxTokens = 200 
                },
                OnPartialResponse
            );
        }

        private void OnPartialResponse(string partialResponse)
        {
            // Handle streaming data
            UpdateUI(partialResponse);
        }

        // Example 3: Using JSON service
        public async Task JsonExample()
        {
            var jsonService = RimAIAPI.GetJsonService();
            
            var prompt = @"Generate a colonist with the following structure:
            {
                ""Name"": ""string"",
                ""Age"": number,
                ""Skills"": [
                    {""Name"": ""string"", ""Level"": number}
                ]
            }";
            
            var result = await jsonService.GenerateJsonAsync<ColonistData>(
                prompt,
                new JsonRequestOptions { Temperature = 0.7 }
            );
            
            if (result.Success)
            {
                Log.Message($"Generated colonist: {result.Data.Name}, Age: {result.Data.Age}");
            }
        }

        // Example 4: Custom request with full control
        public async Task CustomExample()
        {
            var customService = RimAIAPI.GetCustomService();
            var request = new CustomRequest
            {
                Model = "gpt-4",
                Messages = new List<object>
                {
                    new { role = "system", content = "You are a Rimworld storyteller" },
                    new { role = "user", content = "Create an interesting event" }
                },
                Temperature = 0.9,
                MaxTokens = 500,
                Stream = false
            };
            
            var response = await customService.SendCustomRequestAsync(request);
            if (!string.IsNullOrEmpty(response.Error))
            {
                Log.Error($"Custom request failed: {response.Error}");
            }
        }

        // Example 5: Using the Mod Service
        public async Task ModServiceExample()
        {
            var modService = RimAIAPI.GetModService();
            
            // Send message with mod context
            var response = await modService.SendMessageAsync(
                "myModId", 
                "Analyze this situation",
                new RequestOptions { Temperature = 0.5 }
            );
        }

        // Helper method
        private void UpdateUI(string partialResponse)
        {
            // Update your UI here
        }
    }

    // Example data class for JSON mode
    public class ColonistData
    {
        public string Name { get; set; }
        public int Age { get; set; }
        public List<SkillData> Skills { get; set; }
    }

    public class SkillData
    {
        public string Name { get; set; }
        public int Level { get; set; }
    }
}
```

## Key Points

1. **Always check Success status** before using results
2. **Use appropriate Temperature values**: 0.0-0.3 for factual content, 0.7-1.0 for creative content
3. **Handle errors gracefully** with proper error logging
4. **Use streaming** for long responses to improve user experience
5. **JSON mode** is perfect for structured data generation
6. **Custom requests** give you full control over the API call

## Performance Tips

- Cache service instances when possible
- Use streaming for responses longer than a few sentences
- Set appropriate MaxTokens to control response length
- Use lower Temperature values for more consistent results
