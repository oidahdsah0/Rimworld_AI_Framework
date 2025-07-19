using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using RimAI.Framework.Core;
using RimAI.Framework.LLM.Models;
using Verse;

namespace RimAI.Framework.LLM.Services
{
    /// <summary>
    /// Executes individual LLM requests - handles the actual HTTP communication
    /// </summary>
    public class LLMExecutor : ILLMExecutor
    {
        private readonly HttpClient _httpClient;
        private readonly RimAISettings _settings;

        public LLMExecutor(HttpClient httpClient, RimAISettings settings)
        {
            _httpClient = httpClient;
            _settings = settings;
        }

        public async Task<string> ExecuteSingleRequestAsync(string prompt, CancellationToken cancellationToken)
        {
            try
            {
                var requestBody = new
                {
                    model = _settings.modelName,
                    messages = new[]
                    {
                        new { role = "user", content = prompt }
                    },
                    stream = false,
                    temperature = _settings.temperature
                };

                var jsonBody = JsonConvert.SerializeObject(requestBody);
                var response = await SendHttpRequestAsync(_settings.ChatCompletionsEndpoint, jsonBody, _settings.apiKey, cancellationToken);

                if (response.success)
                {
                    return ParseChatCompletionResponse(response.responseBody);
                }
                
                return null;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                Log.Error($"[RimAI] LLMExecutor: Failed to execute single request. Details: {ex}");
                return null;
            }
        }

        public async Task ExecuteStreamingRequestAsync(string prompt, Action<string> onChunkReceived, CancellationToken cancellationToken)
        {
            try
            {
                var requestBody = new
                {
                    model = _settings.modelName,
                    messages = new[]
                    {
                        new { role = "user", content = prompt }
                    },
                    stream = true,
                    temperature = _settings.temperature
                };

                var jsonBody = JsonConvert.SerializeObject(requestBody);
                
                using var request = new HttpRequestMessage(HttpMethod.Post, _settings.ChatCompletionsEndpoint);
                request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _settings.apiKey);
                request.Content = new StringContent(jsonBody, Encoding.UTF8, "application/json");

                using var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
                
                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    Log.Error($"RimAI Framework: Streaming request failed with status {response.StatusCode}: {errorContent}");
                    return;
                }

                using var stream = await response.Content.ReadAsStreamAsync();
                using var reader = new StreamReader(stream);
                
                string line;
                while ((line = await reader.ReadLineAsync()) != null)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    ProcessStreamLine(line, onChunkReceived, cancellationToken);
                }
            }
            catch (OperationCanceledException)
            {
                Log.Message("RimAI Framework: Streaming request was cancelled");
                throw;
            }
            catch (Exception ex)
            {
                Log.Error($"[RimAI] LLMExecutor: Failed to execute streaming request. Details: {ex}");
            }
        }

        public async Task<(bool success, string message)> TestConnectionAsync()
        {
            Log.Message("[RimAI] LLMExecutor: TestConnectionAsync called.");

            if (string.IsNullOrEmpty(_settings.apiKey))
            {
                Log.Warning("[RimAI] LLMExecutor: API Key is not set.");
                return (false, "API Key is not set.");
            }
            if (string.IsNullOrEmpty(_settings.apiEndpoint))
            {
                Log.Warning("[RimAI] LLMExecutor: API Endpoint is not set.");
                return (false, "API Endpoint is not set.");
            }

            var requestBody = new
            {
                model = _settings.modelName,
                messages = new[]
                {
                    new { role = "user", content = "Say 'test'." }
                },
                max_tokens = 5
            };

            var jsonBody = JsonConvert.SerializeObject(requestBody);
            
            try
            {
                var response = await SendHttpRequestAsync(_settings.ChatCompletionsEndpoint, jsonBody, _settings.apiKey, CancellationToken.None);

                if (response.success)
                {
                    Log.Message("[RimAI] LLMExecutor: Request successful.");
                    return (true, "Connection successful");
                }
                else
                {
                    Log.Error($"[RimAI] LLMExecutor: Request failed. Status: {response.statusCode}, Body: {response.errorContent}");
                    return (false, $"Request failed with status {response.statusCode}: {response.errorContent}");
                }
            }
            catch (HttpRequestException e)
            {
                Log.Error($"[RimAI] LLMExecutor: HttpRequestException: {e.Message}");
                return (false, $"HTTP Error: {e.Message}");
            }
            catch (TaskCanceledException e)
            {
                Log.Error($"[RimAI] LLMExecutor: TaskCanceledException (Timeout): {e.Message}");
                return (false, $"Timeout: {e.Message}");
            }
            catch (Exception e)
            {
                Log.Error($"[RimAI] LLMExecutor: An unexpected error occurred: {e.ToString()}");
                return (false, $"Unexpected error: {e.Message}");
            }
        }

        private string ParseChatCompletionResponse(string jsonResponse)
        {
            try
            {
                var responseObject = JsonConvert.DeserializeObject<ChatCompletionResponse>(jsonResponse);
                
                if (responseObject?.choices != null && responseObject.choices.Count > 0)
                {
                    var firstChoice = responseObject.choices[0];
                    if (firstChoice?.message != null && !string.IsNullOrEmpty(firstChoice.message.content))
                    {
                        return firstChoice.message.content;
                    }
                }
                
                Log.Error($"RimAI Framework: Response format unexpected. Response: {jsonResponse}");
                return null;
            }
            catch (JsonException ex)
            {
                Log.Error($"RimAI Framework: JSON parsing error: {ex.Message}\nResponse Body: {jsonResponse}");
                return null;
            }
            catch (Exception ex)
            {
                Log.Error($"RimAI Framework: Unexpected error parsing response: {ex}\nResponse Body: {jsonResponse}");
                return null;
            }
        }

        private void ProcessStreamLine(string line, Action<string> onChunkReceived, CancellationToken cancellationToken = default)
        {
            if (cancellationToken.IsCancellationRequested)
                return;

            if (string.IsNullOrWhiteSpace(line))
                return;

            if (!line.StartsWith("data: "))
                return;

            var data = line.Substring(6);
            
            if (data.Trim() == "[DONE]")
                return;

            try
            {
                var chunk = JsonConvert.DeserializeObject<StreamingChatCompletionChunk>(data);
                
                if (chunk?.choices != null && chunk.choices.Count > 0)
                {
                    var delta = chunk.choices[0].delta;
                    if (!string.IsNullOrEmpty(delta?.content))
                    {
                        if (cancellationToken.IsCancellationRequested)
                            return;

                        try
                        {
                            Verse.LongEventHandler.ExecuteWhenFinished(() => 
                            {
                                try
                                {
                                    if (!cancellationToken.IsCancellationRequested)
                                    {
                                        onChunkReceived(delta.content);
                                    }
                                }
                                catch (Exception ex)
                                {
                                    Log.Warning($"RimAI Framework: Exception in streaming callback: {ex.Message}");
                                }
                            });
                        }
                        catch (Exception ex)
                        {
                            Log.Warning($"RimAI Framework: Could not marshal to main thread, calling directly: {ex.Message}");
                            try
                            {
                                if (!cancellationToken.IsCancellationRequested)
                                {
                                    onChunkReceived(delta.content);
                                }
                            }
                            catch (Exception callbackEx)
                            {
                                Log.Warning($"RimAI Framework: Exception in direct streaming callback: {callbackEx.Message}");
                            }
                        }
                    }
                }
            }
            catch (JsonException ex)
            {
                Log.Warning($"RimAI Framework: Failed to parse streaming chunk: {ex.Message}");
            }
            catch (Exception ex)
            {
                Log.Warning($"RimAI Framework: Error processing stream chunk: {ex.Message}");
            }
        }

        private async Task<(bool success, int statusCode, string responseBody, string errorContent)> SendHttpRequestAsync(string endpoint, string jsonBody, string apiKey, CancellationToken cancellationToken)
        {
            try
            {
                using var request = new HttpRequestMessage(HttpMethod.Post, endpoint);
                request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", apiKey);
                request.Content = new StringContent(jsonBody, Encoding.UTF8, "application/json");

                using var response = await _httpClient.SendAsync(request, cancellationToken);
                
                if (response.IsSuccessStatusCode)
                {
                    var responseBody = await response.Content.ReadAsStringAsync();
                    return (true, (int)response.StatusCode, responseBody, null);
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    Log.Error($"RimAI Framework: HTTP request failed with status {response.StatusCode}: {errorContent}");
                    return (false, (int)response.StatusCode, null, errorContent);
                }
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                Log.Error($"RimAI Framework: HTTP request exception: {ex.Message}");
                return (false, 0, null, ex.Message);
            }
        }
    }
}
