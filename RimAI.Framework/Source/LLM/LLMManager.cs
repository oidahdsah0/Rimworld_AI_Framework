using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using RimAI.Framework.Core;
using Verse;
using RimWorld;

namespace RimAI.Framework.LLM
{
    /// <summary>
    /// Manages all communication with Large Language Models (LLMs).
    /// This class handles API requests, response parsing, and error management.
    /// </summary>
    public class LLMManager : IDisposable
    {
        #region Singleton Pattern
        private static LLMManager _instance;
        private static readonly object _lock = new object();

        /// <summary>
        /// Gets the singleton instance of LLMManager.
        /// </summary>
        public static LLMManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (_lock)
                    {
                        if (_instance == null)
                        {
                            _instance = new LLMManager();
                        }
                    }
                }
                return _instance;
            }
        }
        #endregion

        #region Private Fields
        private readonly HttpClient _httpClient;
        private RimAISettings _settings;
        #endregion

        #region Constructor
        /// <summary>
        /// Private constructor for singleton pattern.
        /// </summary>
        private LLMManager()
        {
            _httpClient = new HttpClient();
            _httpClient.Timeout = TimeSpan.FromSeconds(60); // 60 second timeout
            LoadSettings();
        }
        #endregion

        #region Public Methods
        
        /// <summary>
        /// Sends a chat completion request to the LLM API.
        /// This is the primary public method for other mods to interact with the LLM.
        /// </summary>
        /// <param name="prompt">The text prompt to send to the LLM.</param>
        /// <returns>The content of the LLM's response as a string, or null if an error occurred.</returns>
        public async Task<string> GetChatCompletionAsync(string prompt)
        {
            if (string.IsNullOrEmpty(_settings.apiKey))
            {
                Log.Error("RimAI Framework: API key is not configured. Please check mod settings.");
                return null;
            }

            if (string.IsNullOrEmpty(prompt))
            {
                Log.Warning("RimAI Framework: Empty prompt provided to GetChatCompletionAsync.");
                return null;
            }

            try
            {
                var requestBody = new
                {
                    model = _settings.modelName,
                    messages = new[]
                    {
                        new { role = "user", content = prompt }
                    },
                    stream = false
                };

                var jsonBody = JsonConvert.SerializeObject(requestBody);
                var response = await SendHttpRequestAsync(_settings.apiEndpoint, jsonBody, _settings.apiKey);

                if (response.success)
                {
                    return ParseChatCompletionResponse(response.responseBody);
                }
                else
                {
                    // Error is logged within SendHttpRequestAsync
                    return null;
                }
            }
            catch (Exception ex)
            {
                Log.Error($"RimAI Framework: Error in GetChatCompletionAsync: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Gets a chat completion as a stream of tokens. (NOT IMPLEMENTED IN V1)
        /// This method is reserved for future use. It will send a request and process the response as a stream.
        /// </summary>
        /// <param name="prompt">The text prompt to send to the LLM.</param>
        /// <param name="onChunkReceived">An action to be called for each received token chunk.</param>
        public async Task GetChatCompletionStreamAsync(string prompt, Action<string> onChunkReceived)
        {
            Log.Warning("RimAI Framework: GetChatCompletionStreamAsync is not implemented in this version.");
            // In a future version, this would handle streaming responses by calling a method like ProcessStreamChunk.
            await Task.CompletedTask;
        }

        /// <summary>
        /// Refreshes the settings from the mod configuration.
        /// Call this when settings are changed.
        /// </summary>
        public void RefreshSettings()
        {
            LoadSettings();
        }

        /// <summary>
        /// Tests the connection to the LLM API using the current settings.
        /// </summary>
        /// <returns>A tuple containing a boolean for success and a status message.</returns>
        public async Task<(bool success, string message)> TestConnectionAsync()
        {
            Log.Message("[RimAI] LLMManager: TestConnectionAsync called.");

            if (string.IsNullOrEmpty(_settings.apiKey))
            {
                Log.Warning("[RimAI] LLMManager: API Key is not set.");
                return (false, "API Key is not set.");
            }
            if (string.IsNullOrEmpty(_settings.apiEndpoint))
            {
                Log.Warning("[RimAI] LLMManager: API Endpoint is not set.");
                return (false, "API Endpoint is not set.");
            }

            // Create JSON request body using JsonConvert
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
            var content = new StringContent(jsonBody, Encoding.UTF8, "application/json");

            Log.Message($"[RimAI] LLMManager: Sending request to endpoint: {_settings.apiEndpoint}");
            Log.Message($"[RimAI] LLMManager: Request body: {jsonBody}");

            try
            {
                // Clear existing headers and add the API key
                _httpClient.DefaultRequestHeaders.Clear();
                _httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _settings.apiKey);

                Log.Message("[RimAI] LLMManager: Sending POST request...");
                Messages.Message("RimAI: Sending request to API...", MessageTypeDefOf.NeutralEvent);
                var response = await _httpClient.PostAsync(_settings.apiEndpoint, content);
                Log.Message($"[RimAI] LLMManager: Received response with status code: {response.StatusCode}");

                var responseBody = await response.Content.ReadAsStringAsync();
                Log.Message($"[RimAI] LLMManager: Response body: {responseBody}");

                if (response.IsSuccessStatusCode)
                {
                    Log.Message("[RimAI] LLMManager: Request successful.");
                    return (true, "Connection successful!");
                }
                else
                {
                    Log.Error($"[RimAI] LLMManager: Request failed. Status: {response.StatusCode}, Body: {responseBody}");
                    return (false, $"Connection failed: {response.StatusCode} - {responseBody}");
                }
            }
            catch (HttpRequestException e)
            {
                Log.Error($"[RimAI] LLMManager: HttpRequestException: {e.Message}");
                return (false, $"Network error: {e.Message}");
            }
            catch (TaskCanceledException e)
            {
                Log.Error($"[RimAI] LLMManager: TaskCanceledException (Timeout): {e.Message}");
                return (false, $"Request timed out: {e.Message}");
            }
            catch (Exception e)
            {
                Log.Error($"[RimAI] LLMManager: An unexpected error occurred: {e.ToString()}");
                return (false, $"An unexpected error occurred: {e.Message}");
            }
        }
        #endregion

        #region Private Helper Methods
        
        /// <summary>
        /// Loads the current settings from the mod configuration.
        /// </summary>
        private void LoadSettings()
        {
            var rimAIMod = LoadedModManager.GetMod<RimAIMod>();
            if (rimAIMod != null)
            {
                _settings = rimAIMod.settings;
            }
            else
            {
                Log.Error("RimAI Framework: Could not find RimAIMod instance. Using default settings.");
                _settings = new RimAISettings();
            }
        }

        /// <summary>
        /// Parses the JSON response from a chat completion request and extracts the message content.
        /// </summary>
        /// <param name="jsonResponse">The JSON string from the API.</param>
        /// <returns>The extracted text content, or null if parsing fails.</returns>
        private string ParseChatCompletionResponse(string jsonResponse)
        {
            try
            {
                dynamic responseObject = JsonConvert.DeserializeObject(jsonResponse);
                string content = responseObject.choices[0].message.content;
                return content;
            }
            catch (Exception ex)
            {
                Log.Error($"RimAI Framework: Failed to parse LLM response. Details: {ex.Message}\nResponse Body: {jsonResponse}");
                return null;
            }
        }

        /// <summary>
        /// Processes a single chunk from a streaming HTTP response. (NOT IMPLEMENTED IN V1)
        /// This method is a placeholder for future streaming functionality.
        /// </summary>
        /// <param name="chunk">A single data chunk from the stream.</param>
        private void ProcessStreamChunk(string chunk)
        {
            // In a future version, this would parse Server-Sent Events (SSE)
            // and extract the content from each chunk.
        }

        /// <summary>
        /// Sends an HTTP POST request to the specified endpoint.
        /// </summary>
        private async Task<(bool success, int statusCode, string responseBody, string errorContent)> SendHttpRequestAsync(string endpoint, string jsonBody, string apiKey)
        {
            try
            {
                var request = new HttpRequestMessage(HttpMethod.Post, endpoint);
                request.Headers.Add("Authorization", $"Bearer {apiKey}");
                request.Content = new StringContent(jsonBody, Encoding.UTF8, "application/json");

                var response = await _httpClient.SendAsync(request);
                
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
            catch (Exception ex)
            {
                Log.Error($"RimAI Framework: HTTP request exception: {ex.Message}");
                return (false, 0, null, ex.Message);
            }
        }
        
        #endregion

        #region Cleanup
        /// <summary>
        /// Disposes of resources when the manager is no longer needed.
        /// </summary>
        public void Dispose()
        {
            _httpClient?.Dispose();
        }
        #endregion
    }
}