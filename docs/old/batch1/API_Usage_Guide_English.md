# RimAI Framework API Usage Guide

**Version**: 1.0  
**Author**: [@oidahdsah0](https://github.com/oidahdsah0)  
**Last Updated**: July 2025

---

## 📋 Overview

The RimAI Framework provides a powerful yet simple API for RimWorld mod developers to interact with Large Language Models (LLMs). This framework uses an asynchronous queue processing mechanism with concurrency control and cancellation token support, ensuring stable and reliable AI service calls during gameplay.

## 🛠️ Quick Start

### 1. Add Dependencies

In your mod project, you need to add a dependency on the RimAI Framework:

#### Add reference in your .csproj file:

```xml
<ItemGroup>
  <Reference Include="RimAI.Framework">
    <HintPath>path/to/RimAI.Framework.dll</HintPath>
  </Reference>
</ItemGroup>
```

#### Add dependency in About.xml:

```xml
<ModMetaData>
  <!-- Other metadata -->
  <dependencies>
    <li>
      <packageId>oidahdsah0.RimAI.Framework</packageId>
      <displayName>RimAI Framework</displayName>
      <steamWorkshopUrl>steam://url/CommunityFilePage/[workshop_id]</steamWorkshopUrl>
    </li>
  </dependencies>
</ModMetaData>
```

### 2. Import Namespaces

Import the necessary namespaces in your C# files:

```csharp
using RimAI.Framework.API;
using System.Threading;
using System.Threading.Tasks;
using Verse;
```

## 🎯 Core API Methods

### GetChatCompletion - Get Chat Completion

This is the main API method for sending prompts to the LLM and receiving responses.

#### Method Signature

```csharp
public static Task<string> GetChatCompletion(string prompt, CancellationToken cancellationToken = default)
```

#### Parameters

- `prompt` (string): The text prompt to send to the LLM
- `cancellationToken` (CancellationToken, optional): Cancellation token for request cancellation

#### Return Value

- `Task<string>`: Asynchronous task that returns the LLM's response string upon completion, or null if an error occurs

## 💡 Usage Examples

### Basic Usage

```csharp
public class MyModExample
{
    public async void GenerateBackstory(Pawn pawn)
    {
        try
        {
            string prompt = $"Generate a short, dramatic backstory for a colonist named '{pawn.Name}'.";
            string backstory = await RimAIApi.GetChatCompletion(prompt);
            
            if (backstory != null)
            {
                Log.Message($"Generated backstory for {pawn.Name}: {backstory}");
                // Process the generated backstory here
            }
            else
            {
                Log.Warning("Failed to generate backstory");
            }
        }
        catch (Exception ex)
        {
            Log.Error($"Error generating backstory: {ex.Message}");
        }
    }
}
```

### Using Cancellation Tokens

```csharp
public class MyModExample
{
    public async void GenerateWithTimeout(Pawn pawn)
    {
        // Create a 30-second timeout cancellation token
        using (var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30)))
        {
            try
            {
                string prompt = $"Describe {pawn.Name}'s activities today";
                string description = await RimAIApi.GetChatCompletion(prompt, cts.Token);
                
                if (description != null)
                {
                    Log.Message($"Activity description: {description}");
                }
            }
            catch (OperationCanceledException)
            {
                Log.Warning("Request timed out and was cancelled");
            }
            catch (Exception ex)
            {
                Log.Error($"Request failed: {ex.Message}");
            }
        }
    }
}
```

### Game Event Response

```csharp
[HarmonyPostfix]
[HarmonyPatch(typeof(Pawn_InteractionsTracker), "TryInteractWith")]
public static void OnPawnInteraction(Pawn pawn, Pawn recipient, InteractionDef intDef)
{
    if (intDef.defName == "Insult")
    {
        GenerateInsultResponse(pawn, recipient);
    }
}

private static async void GenerateInsultResponse(Pawn insulter, Pawn target)
{
    string prompt = $"{insulter.Name} just insulted {target.Name}. " +
                   $"Based on {target.Name}'s personality traits, generate an appropriate reaction.";
    
    string response = await RimAIApi.GetChatCompletion(prompt);
    
    if (response != null)
    {
        // Display reaction or trigger corresponding game events
        Messages.Message($"{target.Name}: {response}", MessageTypeDefOf.NeutralEvent);
    }
}
```

### Batch Processing

```csharp
public class ColonyStoryGenerator
{
    public async Task GenerateColonyHistory(List<Pawn> pawns)
    {
        var tasks = new List<Task<string>>();
        
        foreach (var pawn in pawns)
        {
            string prompt = $"Generate a key historical moment for colonist {pawn.Name}";
            tasks.Add(RimAIApi.GetChatCompletion(prompt));
        }
        
        // Wait for all tasks to complete
        string[] results = await Task.WhenAll(tasks);
        
        for (int i = 0; i < results.Length; i++)
        {
            if (results[i] != null)
            {
                Log.Message($"{pawns[i].Name}'s history: {results[i]}");
            }
        }
    }
}
```

## 🎮 In-Game Integration Patterns

### 1. Event-Driven Pattern

Use Harmony patches to respond to game events and call AI to generate content:

```csharp
[HarmonyPostfix]
[HarmonyPatch(typeof(RaidStrategyWorker), "TryExecuteWorker")]
public static void OnRaidStart(IncidentParms parms)
{
    GenerateRaidNarrative(parms);
}

private static async void GenerateRaidNarrative(IncidentParms parms)
{
    string prompt = "Generate a tense description of a pirate raid";
    string narrative = await RimAIApi.GetChatCompletion(prompt);
    
    if (narrative != null)
    {
        Find.LetterStack.ReceiveLetter("Raid Alert", narrative, LetterDefOf.ThreatBig);
    }
}
```

### 2. UI Integration Pattern

Add AI generation functionality to the game UI:

```csharp
public class AIStoryDialog : Window
{
    private string currentStory = "";
    private bool isGenerating = false;
    
    public override void DoWindowContents(Rect inRect)
    {
        if (Widgets.ButtonText(new Rect(10, 10, 200, 30), "Generate Story"))
        {
            GenerateStory();
        }
        
        Widgets.Label(new Rect(10, 50, inRect.width - 20, inRect.height - 60), currentStory);
    }
    
    private async void GenerateStory()
    {
        if (isGenerating) return;
        
        isGenerating = true;
        currentStory = "Generating story...";
        
        string prompt = "Generate an interesting story about a space colony";
        string story = await RimAIApi.GetChatCompletion(prompt);
        
        currentStory = story ?? "Generation failed";
        isGenerating = false;
    }
}
```

### 3. Scheduled Task Pattern

Periodically generate content to enrich the game experience:

```csharp
public class AIStoryManager : GameComponent
{
    private int ticksSinceLastGeneration = 0;
    private const int GenerationInterval = 60000; // 1 minute
    
    public AIStoryManager(Game game) : base(game) { }
    
    public override void GameComponentTick()
    {
        ticksSinceLastGeneration++;
        
        if (ticksSinceLastGeneration >= GenerationInterval)
        {
            GenerateDailyEvent();
            ticksSinceLastGeneration = 0;
        }
    }
    
    private async void GenerateDailyEvent()
    {
        string prompt = "Generate a daily minor event for the colony";
        string eventText = await RimAIApi.GetChatCompletion(prompt);
        
        if (eventText != null)
        {
            Messages.Message(eventText, MessageTypeDefOf.NeutralEvent);
        }
    }
}
```

## ⚙️ Configuration Requirements

### User Configuration

Before using your mod, users need to configure the following in RimAI Framework settings:

1. **API Key**: Authentication key for accessing LLM services
2. **API Endpoint**: URL of the LLM service (defaults to OpenAI)
3. **Model Name**: The LLM model to use (defaults to gpt-4o)

### Check Configuration in Your Mod

```csharp
public static bool IsAPIReady()
{
    // Check if API is available
    var testPrompt = "test";
    var task = RimAIApi.GetChatCompletion(testPrompt);
    
    // Simple check (actual applications may need more complex validation)
    return task != null;
}
```

## ⚠️ Important Notice: DLL Loading Order Issues

### Problem Description

RimWorld loads assemblies in **alphabetical order**, which can cause dependency library loading order issues. If your mod uses the same dependency libraries as RimAI Framework (such as Newtonsoft.Json), you may encounter `TypeLoadException` errors.

### Solution

1. **Ensure correct dependencies**: Properly declare dependency on RimAI Framework in your `About.xml`
2. **Avoid duplicate dependencies**: Don't include dependency libraries that RimAI Framework already provides
3. **Use Framework dependencies**: RimAI Framework already includes `000_Newtonsoft.Json.dll` (renamed to ensure priority loading)

### Example Error and Resolution

If you see an error like:
```
System.TypeLoadException: Could not resolve type with token 0100003e from typeref 
(expected class 'Newtonsoft.Json.JsonConvert' in assembly 'Newtonsoft.Json, Version=13.0.0.0')
```

**Resolution Steps**:
1. Remove `Newtonsoft.Json.dll` from your mod
2. Ensure your mod loads after RimAI Framework
3. Properly declare dependencies in `About.xml`

### Best Practices

```xml
<!-- In your mod's About.xml -->
<ModMetaData>
  <dependencies>
    <li>
      <packageId>oidahdsah0.RimAI.Framework</packageId>
      <displayName>RimAI Framework</displayName>
      <steamWorkshopUrl>steam://url/CommunityFilePage/[workshop_id]</steamWorkshopUrl>
    </li>
  </dependencies>
</ModMetaData>
```

```xml
<!-- In your mod's .csproj, don't include duplicate dependencies -->
<ItemGroup>
  <!-- Correct: Only reference RimAI Framework -->
  <Reference Include="RimAI.Framework">
    <HintPath>path/to/RimAI.Framework.dll</HintPath>
  </Reference>
  
  <!-- Wrong: Don't include this, it will cause conflicts -->
  <!-- <PackageReference Include="Newtonsoft.Json" Version="13.0.3" /> -->
</ItemGroup>
```

## 🚨 Error Handling

### Common Error Scenarios

1. **API Key Not Configured**: Returns null and logs error
2. **Network Connection Issues**: Returns null and logs corresponding error
3. **API Rate Limits**: Automatically queued with concurrency control
4. **Request Timeout**: Can be handled via cancellation tokens

### Best Practices

```csharp
public static async Task<string> SafeGetCompletion(string prompt, int maxRetries = 3)
{
    for (int i = 0; i < maxRetries; i++)
    {
        try
        {
            using (var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30)))
            {
                string result = await RimAIApi.GetChatCompletion(prompt, cts.Token);
                
                if (result != null)
                {
                    return result;
                }
                
                // Wait before retry
                await Task.Delay(1000 * (i + 1));
            }
        }
        catch (OperationCanceledException)
        {
            Log.Warning($"Request timed out, retry {i + 1}/{maxRetries}");
        }
        catch (Exception ex)
        {
            Log.Error($"Request failed: {ex.Message}");
        }
    }
    
    return null;
}
```

## 📊 Performance Considerations

### Concurrency Control

The framework automatically limits concurrent requests (default 3) to avoid excessive API calls.

### Queue Mechanism

All requests are processed through an internal queue, ensuring:
- Orderly request processing
- Avoidance of API rate limits
- System stability

### Memory Management

- Timely release of unnecessary strings
- Use `CancellationToken` to cancel unneeded requests
- Avoid creating numerous async tasks in loops

## 🔧 Debugging Tips

### Enable Verbose Logging

```csharp
// Enable verbose logging during development
Log.Message($"Sending prompt: {prompt}");
string result = await RimAIApi.GetChatCompletion(prompt);
Log.Message($"Received response: {result ?? "null"}");
```

### Test API Connection

```csharp
public static async void TestAPIConnection()
{
    string testPrompt = "Please reply 'connection successful'";
    
    try
    {
        string response = await RimAIApi.GetChatCompletion(testPrompt);
        Log.Message($"API test result: {response}");
    }
    catch (Exception ex)
    {
        Log.Error($"API test failed: {ex.Message}");
    }
}
```

## 🎯 Best Practice Recommendations

1. **Optimize Prompts**: Use clear, specific prompts for better results
2. **Error Handling**: Always check if return values are null
3. **User Experience**: Display loading states in UI
4. **Performance Optimization**: Avoid frequent API calls
5. **Cancellation Support**: Provide cancellation options for long-running operations

## 🔄 Future Features

### Streaming Response (Planned)

```csharp
// Future versions will support streaming responses
await RimAIApi.GetChatCompletionStream(prompt, (chunk) => {
    // Process each received text chunk
    Log.Message($"Received: {chunk}");
});
```

---

## 📞 Technical Support

If you encounter issues during usage, please:

1. Check game logs for error messages
2. Confirm API configuration is correct
3. Create an issue in the GitHub repository
4. Provide detailed error information and reproduction steps

**GitHub Repository**: https://github.com/oidahdsah0/Rimworld_AI_Framework

---

*This documentation is continuously updated. Please follow the latest version for the newest features and fixes.*
