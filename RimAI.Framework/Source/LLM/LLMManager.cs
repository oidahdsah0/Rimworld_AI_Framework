using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using RimAI.Framework.Core;
using Verse;

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
        /// Refreshes the settings from the mod configuration.
        /// Call this when settings are changed.
        /// </summary>
        public void RefreshSettings()
        {
            LoadSettings();
        }

        /// <summary>
        /// Tests the connection to the LLM API with a simple request.
        /// </summary>
        /// <returns>Tuple indicating success and message.</returns>
        public async Task<(bool success, string message)> TestConnectionAsync()
        {
            if (string.IsNullOrEmpty(_settings.apiKey))
            {
                return (false, "API Key is missing.");
            }

            try
            {
                var requestBody = new
                {
                    model = _settings.modelName,
                    messages = new[]
                    {
                        new { role = "user", content = "Say 'Hello, World!' to confirm connection." }
                    },
                    max_tokens = 15
                };

                var jsonBody = JsonConvert.SerializeObject(requestBody);
                var response = await SendHttpRequestAsync(_settings.apiEndpoint, jsonBody, _settings.apiKey);

                if (response.success)
                {
                    return (true, $"Success! Model '{_settings.modelName}' responded.");
                }
                else
                {
                    return (false, $"Error: {response.statusCode}. Details: {response.errorContent}");
                }
            }
            catch (Exception e)
            {
                return (false, $"Exception: {e.Message}");
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