using System;
using System.Threading.Tasks;
using RimAI.Framework.API;
using RimAI.Framework.LLM.Models;
using Verse;

namespace MyMod.Examples
{
    /// <summary>
    /// Examples of using the RimAI Framework API
    /// </summary>
    public class RimAIAPIExamples
    {
        // Example 1: Basic usage - let framework decide streaming mode
        public async Task BasicExample()
        {
            var response = await RimAIAPI.SendMessageAsync("Tell me about Rimworld");
            Log.Message($"AI Response: {response}");
        }

        // Example 2: Force non-streaming mode
        public async Task NonStreamingExample()
        {
            var options = RimAIAPI.Options.NonStreaming(temperature: 0.8);
            var response = await RimAIAPI.SendMessageAsync("Generate a colonist backstory", options);
            Log.Message($"Backstory: {response}");
        }

        // Example 3: Streaming mode with progress updates
        public async Task StreamingExample()
        {
            var fullResponse = "";
            await RimAIAPI.SendStreamingMessageAsync(
                "Write a detailed raid event description",
                chunk => {
                    fullResponse += chunk;
                    // Update UI with partial response
                    UpdateUI(fullResponse);
                },
                RimAIAPI.Options.Streaming(temperature: 1.0)
            );
        }

        // Example 4: JSON mode for structured data
        public async Task JsonExample()
        {
            var jsonService = RimAIAPI.GetJsonService();
            var result = await jsonService.SendJsonRequestAsync<ColonistData>(
                "Generate a colonist with name, age, and three skills",
                new JsonRequestOptions { Temperature = 0.7 }
            );
            
            if (result.Success)
            {
                Log.Message($"Generated colonist: {result.Data.Name}, Age: {result.Data.Age}");
            }
        }

        // Example 5: Custom request with full control
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

        // Example 6: Using the Mod Service
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