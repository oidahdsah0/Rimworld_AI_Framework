using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using RimAI.Framework.LLM.Models;
using Verse;

namespace RimAI.Framework.LLM.Services
{
    /// <summary>
    /// Custom LLM service allowing full control over request parameters
    /// </summary>
    public class CustomLLMService : ICustomLLMService
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiKey;
        private readonly string _baseUrl;

        public CustomLLMService(HttpClient httpClient, string apiKey, string baseUrl)
        {
            _httpClient = httpClient;
            _apiKey = apiKey;
            _baseUrl = baseUrl;
        }

        public async Task<CustomResponse> SendCustomRequestAsync(CustomRequest request)
        {
            try
            {
                var requestBody = new Dictionary<string, object>
                {
                    ["model"] = request.Model,
                    ["messages"] = request.Messages,
                    ["stream"] = false
                };

                if (request.Temperature.HasValue)
                    requestBody["temperature"] = request.Temperature.Value;
                if (request.MaxTokens.HasValue)
                    requestBody["max_tokens"] = request.MaxTokens.Value;
                if (request.ResponseFormat != null)
                    requestBody["response_format"] = request.ResponseFormat;

                if (request.AdditionalParameters != null)
                {
                    foreach (var param in request.AdditionalParameters)
                    {
                        requestBody[param.Key] = param.Value;
                    }
                }

                var json = JsonConvert.SerializeObject(requestBody);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                _httpClient.DefaultRequestHeaders.Clear();
                _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_apiKey}");

                var response = await _httpClient.PostAsync($"{_baseUrl}/chat/completions", content);
                var responseContent = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    return new CustomResponse { Error = responseContent };
                }

                var responseData = JsonConvert.DeserializeObject<Dictionary<string, object>>(responseContent);
                return new CustomResponse
                {
                    IsStream = false,
                    RawResponse = responseData,
                    Content = ExtractContent(responseData),
                    JsonContent = ExtractJsonContent(responseData)
                };
            }
            catch (Exception ex)
            {
                return new CustomResponse { Error = ex.Message };
            }
        }

        public IAsyncEnumerable<string> SendCustomStreamRequestAsync(CustomRequest request)
        {
            // Implement streaming for custom requests
            return SendCustomStreamRequestAsyncInternal(request);
        }

        private async IAsyncEnumerable<string> SendCustomStreamRequestAsyncInternal(CustomRequest request)
        {
            // Implement streaming for custom requests
            yield return "Streaming implementation pending";
            await Task.CompletedTask; // Satisfy async requirement
        }

        private string ExtractContent(Dictionary<string, object> response)
        {
            try
            {
                if (response.ContainsKey("choices") && response["choices"] is Newtonsoft.Json.Linq.JArray choices && choices.Count > 0)
                {
                    var firstChoice = choices[0] as Newtonsoft.Json.Linq.JObject;
                    if (firstChoice?.ContainsKey("message") == true)
                    {
                        var message = firstChoice["message"] as Newtonsoft.Json.Linq.JObject;
                        return message?["content"]?.ToString();
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Warning($"CustomLLMService: Error extracting content: {ex.Message}");
            }
            return null;
        }

        private object ExtractJsonContent(Dictionary<string, object> response)
        {
            var content = ExtractContent(response);
            if (string.IsNullOrEmpty(content)) return null;

            try
            {
                return JsonConvert.DeserializeObject(content);
            }
            catch
            {
                return content; // Return as string if not valid JSON
            }
        }
    }
}
