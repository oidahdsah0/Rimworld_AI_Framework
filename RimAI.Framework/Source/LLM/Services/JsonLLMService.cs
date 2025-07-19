using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Newtonsoft.Json;
using RimAI.Framework.Core;
using RimAI.Framework.LLM.Models;
using Verse;

namespace RimAI.Framework.LLM.Services
{
    /// <summary>
    /// Service for enforced JSON responses with streaming/non-streaming options
    /// </summary>
    public class JsonLLMService : IJsonLLMService
    {
        private readonly ILLMExecutor _executor;
        private readonly RimAISettings _settings;

        public JsonLLMService(ILLMExecutor executor, RimAISettings settings)
        {
            _executor = executor;
            _settings = settings;
        }

        public async Task<JsonResponse<T>> SendJsonRequestAsync<T>(string prompt, LLMRequestOptions options = null)
        {
            options ??= new LLMRequestOptions();
            
            var jsonPrompt = $"{prompt}\n\nPlease respond in valid JSON format only. No additional text or formatting.";
            
            try
            {
                var response = await _executor.ExecuteSingleRequestAsync(jsonPrompt, default);
                
                if (string.IsNullOrEmpty(response))
                {
                    return new JsonResponse<T>
                    {
                        Success = false,
                        Error = "No response received"
                    };
                }

                var data = JsonConvert.DeserializeObject<T>(response);
                
                return new JsonResponse<T>
                {
                    Success = true,
                    Data = data,
                    RawJson = response
                };
            }
            catch (JsonException ex)
            {
                Log.Error($"JsonLLMService: JSON parsing error: {ex.Message}");
                return new JsonResponse<T>
                {
                    Success = false,
                    Error = $"JSON parsing error: {ex.Message}"
                };
            }
            catch (Exception ex)
            {
                Log.Error($"JsonLLMService: Unexpected error: {ex.Message}");
                return new JsonResponse<T>
                {
                    Success = false,
                    Error = ex.Message
                };
            }
        }

        public async IAsyncEnumerable<string> SendJsonStreamRequestAsync(string prompt, LLMRequestOptions options = null)
        {
            options ??= new LLMRequestOptions();
            
            var jsonPrompt = $"{prompt}\n\nPlease respond in valid JSON format only. Stream the JSON response.";
            
            var chunks = new List<string>();
            var tcs = new TaskCompletionSource<bool>();
            
            try
            {
                await _executor.ExecuteStreamingRequestAsync(jsonPrompt, chunk => 
                {
                    chunks.Add(chunk);
                }, default);

                // Yield collected chunks as JSON
                foreach (var chunk in chunks)
                {
                    yield return chunk;
                }
            }
            catch (Exception ex)
            {
                Log.Error($"JsonLLMService: Streaming error: {ex.Message}");
                yield return $"{{\"error\": \"{ex.Message}\"}}";
            }
        }

        public async Task<JsonResponse<object>> SendJsonRequestAsync(string prompt, object schema, LLMRequestOptions options = null)
        {
            var schemaJson = JsonConvert.SerializeObject(schema, Formatting.Indented);
            var enhancedPrompt = $"{prompt}\n\nPlease respond in JSON format matching this schema:\n{schemaJson}";
            
            try
            {
                var response = await _executor.ExecuteSingleRequestAsync(enhancedPrompt, default);
                
                if (string.IsNullOrEmpty(response))
                {
                    return new JsonResponse<object>
                    {
                        Success = false,
                        Error = "No response received"
                    };
                }

                var data = JsonConvert.DeserializeObject(response);
                
                return new JsonResponse<object>
                {
                    Success = true,
                    Data = data,
                    RawJson = response
                };
            }
            catch (Exception ex)
            {
                return new JsonResponse<object>
                {
                    Success = false,
                    Error = ex.Message
                };
            }
        }
    }
}
