using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Verse;
using RimAI.Framework.Core;

namespace RimAI.Framework.LLM
{
    /// <summary>
    /// Manages all communication with Large Language Models (LLMs).
    /// This class handles API requests, response parsing, and error management.
    /// </summary>
    public class LLMManager
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
        private HttpClient _httpClient;
        private RimAISettings _settings;
        #endregion

        #region Constructor
        /// <summary>
        /// Private constructor for singleton pattern.
        /// </summary>
        private LLMManager()
        {
            InitializeHttpClient();
            LoadSettings();
        }
        #endregion

        #region Initialization
        /// <summary>
        /// Initializes the HTTP client with default settings.
        /// </summary>
        private void InitializeHttpClient()
        {
            _httpClient = new HttpClient();
            _httpClient.Timeout = TimeSpan.FromSeconds(30); // 30 second timeout
        }

        /// <summary>
        /// Loads the current settings from the mod configuration.
        /// </summary>
        private void LoadSettings()
        {
            // Find the RimAIMod instance to get settings
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
        #endregion

        #region Public Methods
        /// <summary>
        /// Sends a chat completion request to the LLM API.
        /// </summary>
        /// <param name="prompt">The prompt to send to the LLM.</param>
        /// <returns>The response from the LLM, or null if failed.</returns>
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
                var requestBody = CreateChatCompletionRequest(prompt);
                var response = await SendHttpRequestAsync(_settings.apiEndpoint, requestBody, _settings.apiKey);
                
                if (response != null)
                {
                    return ParseChatCompletionResponse(response);
                }
            }
            catch (Exception ex)
            {
                Log.Error($"RimAI Framework: Error in GetChatCompletionAsync: {ex.Message}");
            }

            return null;
        }

        /// <summary>
        /// Refreshes the settings from the mod configuration.
        /// Call this when settings are changed.
        /// </summary>
        public void RefreshSettings()
        {
            LoadSettings();
        }
        #endregion

        #region Private Helper Methods
        /// <summary>
        /// Creates a JSON request body for chat completion.
        /// </summary>
        /// <param name="prompt">The user prompt.</param>
        /// <returns>JSON string for the request body.</returns>
        private string CreateChatCompletionRequest(string prompt)
        {
            // Simple JSON construction for OpenAI API format
            var escapedPrompt = prompt.Replace("\"", "\\\"").Replace("\n", "\\n").Replace("\r", "\\r");
            var streamSetting = _settings.enableStreaming ? "true" : "false";
            
            return $@"{{
                ""model"": ""{_settings.modelName}"",
                ""messages"": [
                    {{
                        ""role"": ""user"",
                        ""content"": ""{escapedPrompt}""
                    }}
                ],
                ""stream"": {streamSetting}
            }}";
        }

        /// <summary>
        /// Sends an HTTP POST request to the specified endpoint.
        /// </summary>
        /// <param name="endpoint">The API endpoint URL.</param>
        /// <param name="jsonBody">The JSON request body.</param>
        /// <param name="apiKey">The API key for authentication.</param>
        /// <returns>The response body as string, or null if failed.</returns>
        private async Task<string> SendHttpRequestAsync(string endpoint, string jsonBody, string apiKey)
        {
            try
            {
                var request = new HttpRequestMessage(HttpMethod.Post, endpoint);
                request.Headers.Add("Authorization", $"Bearer {apiKey}");
                request.Content = new StringContent(jsonBody, Encoding.UTF8, "application/json");

                var response = await _httpClient.SendAsync(request);
                
                if (response.IsSuccessStatusCode)
                {
                    return await response.Content.ReadAsStringAsync();
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    Log.Error($"RimAI Framework: HTTP request failed with status {response.StatusCode}: {errorContent}");
                    return null;
                }
            }
            catch (Exception ex)
            {
                Log.Error($"RimAI Framework: HTTP request exception: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Parses the chat completion response and extracts the content.
        /// </summary>
        /// <param name="jsonResponse">The JSON response from the API.</param>
        /// <returns>The extracted content, or null if parsing failed.</returns>
        private string ParseChatCompletionResponse(string jsonResponse)
        {
            try
            {
                // Simple JSON parsing for OpenAI response format
                // Looking for: "content":"actual_response_text"
                var contentStart = jsonResponse.IndexOf("\"content\":\"");
                if (contentStart == -1)
                {
                    Log.Error("RimAI Framework: Could not find content in API response.");
                    return null;
                }

                contentStart += 11; // Length of "content":"
                var contentEnd = jsonResponse.IndexOf("\"", contentStart);
                
                if (contentEnd == -1)
                {
                    Log.Error("RimAI Framework: Could not find end of content in API response.");
                    return null;
                }

                var content = jsonResponse.Substring(contentStart, contentEnd - contentStart);
                
                // Unescape JSON characters
                content = content.Replace("\\n", "\n").Replace("\\r", "\r").Replace("\\\"", "\"");
                
                return content;
            }
            catch (Exception ex)
            {
                Log.Error($"RimAI Framework: Error parsing API response: {ex.Message}");
                return null;
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