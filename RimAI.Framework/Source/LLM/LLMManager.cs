using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading;
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
    /// It features a request queue with concurrency limiting to prevent API rate limiting.
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

        // Concurrency and Queueing
        private readonly ConcurrentQueue<RequestData> _requestQueue;
        private readonly SemaphoreSlim _concurrentRequestLimiter;
        private readonly CancellationTokenSource _disposeCts;
        private readonly Task _queueProcessorTask;
        #endregion

        #region RequestData Helper Class
        /// <summary>
        /// Internal class to hold all information about a single request.
        /// </summary>
        private class RequestData
        {
            public string Prompt { get; }
            public TaskCompletionSource<string> CompletionSource { get; }
            public CancellationToken CancellationToken { get; }

            public RequestData(string prompt, TaskCompletionSource<string> tcs, CancellationToken ct)
            {
                Prompt = prompt;
                CompletionSource = tcs;
                CancellationToken = ct;
            }
        }
        #endregion

        #region Constructor
        /// <summary>
        /// Private constructor for singleton pattern.
        /// </summary>
        private LLMManager()
        {
            try
            {
                // Configure HttpClient with safer settings for Unity/Mono environment
                var handler = new HttpClientHandler();
                
                // Try to configure SSL settings safely - this might fail in some Unity environments
                try
                {
                    handler.ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) => true;
                }
                catch (Exception ex)
                {
                    Log.Warning($"RimAI Framework: Could not configure SSL validation bypass: {ex.Message}");
                }
                
                _httpClient = new HttpClient(handler);
                _httpClient.Timeout = TimeSpan.FromSeconds(60);
                
                // Add default headers that might be required
                _httpClient.DefaultRequestHeaders.Add("User-Agent", "RimAI-Framework/1.0");
                _httpClient.DefaultRequestHeaders.Add("Accept", "application/json");
                
                Log.Message("RimAI Framework: HttpClient initialized successfully.");
            }
            catch (Exception ex)
            {
                Log.Error($"RimAI Framework: Failed to initialize HttpClient with custom handler, using default: {ex.Message}");
                // Fallback to basic HttpClient if custom configuration fails
                _httpClient = new HttpClient();
                _httpClient.Timeout = TimeSpan.FromSeconds(60);
            }
            
            LoadSettings();

            // Initialize concurrency controls
            _requestQueue = new ConcurrentQueue<RequestData>();
            _concurrentRequestLimiter = new SemaphoreSlim(3, 3); // Allow up to 3 concurrent requests
            _disposeCts = new CancellationTokenSource();
            _queueProcessorTask = Task.Run(() => ProcessQueueAsync(_disposeCts.Token));
            
            Log.Message("RimAI Framework: LLMManager initialized successfully.");
        }
        #endregion

        #region Public Methods
        
        /// <summary>
        /// Asynchronously enqueues a chat completion request to the LLM API.
        /// The request will be processed when a slot is available.
        /// </summary>
        /// <param name="prompt">The text prompt to send to the LLM.</param>
        /// <param name="cancellationToken">A cancellation token to cancel the request.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the content of the LLM's response, or null if an error occurred.</returns>
        public Task<string> GetChatCompletionAsync(string prompt, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(_settings.apiKey))
            {
                Log.Error("RimAI Framework: API key is not configured. Please check mod settings.");
                return Task.FromResult<string>(null);
            }

            if (string.IsNullOrEmpty(prompt))
            {
                Log.Warning("RimAI Framework: Empty prompt provided to GetChatCompletionAsync.");
                return Task.FromResult<string>(null);
            }

            var tcs = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);
            var requestData = new RequestData(prompt, tcs, cancellationToken);
            
            // Link the external cancellation token with the disposal token
            var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(_disposeCts.Token, cancellationToken);
            linkedCts.Token.Register(() => tcs.TrySetCanceled());

            _requestQueue.Enqueue(requestData);
            
            return tcs.Task;
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
            
            try
            {
                var response = await SendHttpRequestAsync(_settings.apiEndpoint, jsonBody, _settings.apiKey, CancellationToken.None);

                if (response.success)
                {
                    Log.Message("[RimAI] LLMManager: Request successful.");
                    return (true, "Connection successful");
                }
                else
                {
                    Log.Error($"[RimAI] LLMManager: Request failed. Status: {response.statusCode}, Body: {response.errorContent}");
                    return (false, $"Request failed with status {response.statusCode}: {response.errorContent}");
                }
            }
            catch (HttpRequestException e)
            {
                Log.Error($"[RimAI] LLMManager: HttpRequestException: {e.Message}");
                return (false, $"HTTP Error: {e.Message}");
            }
            catch (TaskCanceledException e)
            {
                Log.Error($"[RimAI] LLMManager: TaskCanceledException (Timeout): {e.Message}");
                return (false, $"Timeout: {e.Message}");
            }
            catch (Exception e)
            {
                Log.Error($"[RimAI] LLMManager: An unexpected error occurred: {e.ToString()}");
                return (false, $"Unexpected error: {e.Message}");
            }
        }
        #endregion

        #region Private Helper Methods

        /// <summary>
        /// Processes the request queue in a background task.
        /// </summary>
        private async Task ProcessQueueAsync(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                if (_requestQueue.TryDequeue(out var requestData))
                {
                    // Process one request at a time to avoid overwhelming the system
                    await ProcessSingleRequestFromQueue(requestData, cancellationToken);
                }
                else
                {
                    await Task.Delay(100, cancellationToken); // Wait if queue is empty
                }
            }
        }

        /// <summary>
        /// Safely processes a single dequeued request, handling all exceptions internally
        /// and ensuring the TaskCompletionSource is always completed with a safe result.
        /// </summary>
        private async Task ProcessSingleRequestFromQueue(RequestData requestData, CancellationToken cancellationToken)
        {
            await _concurrentRequestLimiter.WaitAsync(cancellationToken);
            try
            {
                if (requestData.CancellationToken.IsCancellationRequested)
                {
                    requestData.CompletionSource.TrySetCanceled();
                    return;
                }

                // Execute the request safely - ExecuteSingleRequestAsync now handles its own exceptions
                var result = await ExecuteSingleRequestAsync(requestData.Prompt, requestData.CancellationToken);

                // Always set a result, never set an exception. If ExecuteSingleRequestAsync fails, result will be null.
                requestData.CompletionSource.TrySetResult(result);
            }
            catch (OperationCanceledException)
            {
                // Handle cancellation properly
                requestData.CompletionSource.TrySetCanceled();
            }
            catch (Exception ex)
            {
                // This is a fallback catch that should rarely be hit if ExecuteSingleRequestAsync is robust
                // We log the error and return null to prevent downstream mod crashes
                Log.Error($"[RimAI] LLMManager: Unhandled exception in ProcessSingleRequestFromQueue: {ex}");
                requestData.CompletionSource.TrySetResult(null); // Safe fallback - return null instead of crashing
            }
            finally
            {
                _concurrentRequestLimiter.Release();
            }
        }

        /// <summary>
        /// Executes a single chat completion request.
        /// This method handles all exceptions internally and returns null on failure.
        /// </summary>
        private async Task<string> ExecuteSingleRequestAsync(string prompt, CancellationToken cancellationToken)
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
                    stream = false
                };

                var jsonBody = JsonConvert.SerializeObject(requestBody);
                var response = await SendHttpRequestAsync(_settings.apiEndpoint, jsonBody, _settings.apiKey, cancellationToken);

                if (response.success)
                {
                    return ParseChatCompletionResponse(response.responseBody);
                }
                
                // Error is already logged within SendHttpRequestAsync
                return null;
            }
            catch (OperationCanceledException)
            {
                // Re-throw cancellation to be handled by the calling method
                throw;
            }
            catch (Exception ex)
            {
                // Log the error and return null instead of propagating the exception
                Log.Error($"[RimAI] LLMManager: Failed to execute single request. Details: {ex}");
                return null;
            }
        }
        
        /// <summary>
        /// Loads the current settings from the mod configuration.
        /// Thread-safe version that handles cross-thread access gracefully.
        /// </summary>
        private void LoadSettings()
        {
            try
            {
                // Only attempt to load from ModManager if settings are null
                // This avoids repeated cross-thread calls
                if (_settings == null)
                {
                    // Try to get settings, but be prepared for thread-safety issues
                    try
                    {
                        var rimAIMod = LoadedModManager.GetMod<RimAIMod>();
                        if (rimAIMod != null)
                        {
                            _settings = rimAIMod.settings;
                            Log.Message("RimAI Framework: Settings loaded successfully.");
                        }
                    }
                    catch (Exception ex)
                    {
                        Log.Warning($"RimAI Framework: Could not load settings from ModManager (possibly called from wrong thread): {ex.Message}");
                    }
                }
                
                // Fallback to default settings if still null
                if (_settings == null)
                {
                    _settings = new RimAISettings();
                    Log.Message("RimAI Framework: Using default settings due to loading failure.");
                }
            }
            catch (Exception ex)
            {
                Log.Error($"RimAI Framework: Critical error in LoadSettings: {ex}");
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
                // Use strongly-typed classes instead of dynamic to avoid runtime binding issues
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
                // Don't log cancellation as an error
                throw;
            }
            catch (Exception ex)
            {
                Log.Error($"RimAI Framework: HTTP request exception: {ex.Message}");
                return (false, 0, null, ex.Message);
            }
        }
        
        #endregion

        #region Response DTOs
        /// <summary>
        /// Strongly-typed classes for JSON deserialization to avoid dynamic type issues
        /// </summary>
        private class ChatCompletionResponse
        {
            public List<Choice> choices { get; set; }
            public string id { get; set; }
            public string model { get; set; }
            public Usage usage { get; set; }
        }

        private class Choice
        {
            public Message message { get; set; }
            public int index { get; set; }
            public string finish_reason { get; set; }
        }

        private class Message
        {
            public string role { get; set; }
            public string content { get; set; }
        }

        private class Usage
        {
            public int prompt_tokens { get; set; }
            public int completion_tokens { get; set; }
            public int total_tokens { get; set; }
        }
        #endregion

        #region Cleanup
        /// <summary>
        /// Disposes of resources when the manager is no longer needed.
        /// </summary>
        public void Dispose()
        {
            _disposeCts.Cancel();
            _disposeCts.Dispose();
            _concurrentRequestLimiter?.Dispose();
            _httpClient?.Dispose();
        }
        #endregion
    }
}