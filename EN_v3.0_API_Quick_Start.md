public class ItemDescriptionGenerator
{
    public async Task GenerateItemDescriptions(List<Thing> items)
    {
        // ... (existing code) ...
    }
}

### 🤖 Function Calling
```csharp
// Let the AI decide whether and how to call functions you provide.
// Note: Use an alias to avoid conflict with Verse.Tool
using AITool = RimAI.Framework.LLM.Models.Tool;

var tools = new List<AITool> { /* ... define your tools ... */ };
var prompt = "What is 128 multiplied by 5.5?";

// The AI will return the name and arguments of the function it thinks should be called.
var functionCallResults = await RimAIAPI.GetFunctionCallAsync(prompt, tools);

if (functionCallResults != null)
{
    foreach(var call in functionCallResults)
    {
        Log.Message($"Function Name: {call.FunctionName}");
        Log.Message($"Arguments (JSON): {call.Arguments}");
        // Next, you need to execute this function yourself.
    }
}
```

---

## 🔧 Preset Options (Recommended) 