using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
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
        private class RequestData : IDisposable
        {
            public string Prompt { get; }
            public TaskCompletionSource<string> CompletionSource { get; }
            public CancellationToken CancellationToken { get; }
            public bool IsStreaming { get; }
            public Action<string> StreamCallback { get; }
            public TaskCompletionSource<bool> StreamCompletionSource { get; }
            public CancellationTokenSource LinkedCts { get; private set; }

            // Constructor for non-streaming requests
            public RequestData(string prompt, TaskCompletionSource<string> tcs, CancellationToken ct)
            {
                Prompt = prompt;
                CompletionSource = tcs;
                CancellationToken = ct;
                IsStreaming = false;
                StreamCallback = null;
                StreamCompletionSource = null;
            }

            // Constructor for streaming requests
            public RequestData(string prompt, Action<string> streamCallback, TaskCompletionSource<bool> streamTcs, CancellationToken ct)
            {
                Prompt = prompt;
                CompletionSource = null;
                CancellationToken = ct;
                IsStreaming = true;
                StreamCallback = streamCallback;
                StreamCompletionSource = streamTcs;
            }

            public void SetLinkedCancellationTokenSource(CancellationTokenSource linkedCts)
            {
                LinkedCts = linkedCts;
            }

            public bool IsCancellationRequested => CancellationToken.IsCancellationRequested || LinkedCts?.Token.IsCancellationRequested == true;

            public void Dispose()
            {
                LinkedCts?.Dispose();
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
        /// Gets whether streaming is currently enabled in the settings.
        /// Downstream mods can use this to adjust their UI behavior accordingly.
        /// </summary>
        public bool IsStreamingEnabled => _settings?.enableStreaming ?? false;

        /// <summary>
        /// Gets the current API settings. Read-only access for downstream mods.
        /// </summary>
        public RimAISettings CurrentSettings => _settings;

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

            // Enhanced validation: reject null, empty, or whitespace-only prompts
            if (string.IsNullOrWhiteSpace(prompt))
            {
                Log.Warning("RimAI Framework: Empty or whitespace-only prompt provided to GetChatCompletionAsync. No API request will be made.");
                return Task.FromResult<string>(null);
            }

            // Check if streaming is enabled in settings
            if (_settings.enableStreaming)
            {
                // Use streaming mode but collect all chunks into a single response
                return GetStreamingAsNonStreamingAsync(prompt, cancellationToken);
            }

            var tcs = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);
            var requestData = new RequestData(prompt, tcs, cancellationToken);
            
            // Link the external cancellation token with the disposal token
            var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(_disposeCts.Token, cancellationToken);
            requestData.SetLinkedCancellationTokenSource(linkedCts);
            linkedCts.Token.Register(() => tcs.TrySetCanceled());

            _requestQueue.Enqueue(requestData);
            
            return tcs.Task;
        }

        /// <summary>
        /// Internal method to handle streaming requests when enableStreaming is true,
        /// but the caller expects a non-streaming interface.
        /// </summary>
        private async Task<string> GetStreamingAsNonStreamingAsync(string prompt, CancellationToken cancellationToken)
        {
            var fullResponse = new StringBuilder();

            try
            {
                await GetChatCompletionStreamAsync(
                    prompt,
                    chunk => fullResponse.Append(chunk),
                    cancellationToken
                );

                return fullResponse.ToString();
            }
            catch (OperationCanceledException)
            {
                Log.Message("RimAI Framework: Streaming request was cancelled by user");
                throw; // Re-throw to allow proper handling by caller
            }
            catch (Exception ex)
            {
                Log.Error($"RimAI Framework: Error in streaming-as-non-streaming request: {ex}");
                return null;
            }
        }

        /// <summary>
        /// Gets a chat completion as a stream of tokens.
        /// This method sends a request and processes the response as a stream.
        /// </summary>
        /// <param name="prompt">The text prompt to send to the LLM.</param>
        /// <param name="onChunkReceived">An action to be called for each received token chunk.</param>
        /// <param name="cancellationToken">A cancellation token to cancel the stream.</param>
        /// <returns>A task that completes when the streaming is finished.</returns>
        public Task GetChatCompletionStreamAsync(string prompt, Action<string> onChunkReceived, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(_settings.apiKey))
            {
                Log.Error("RimAI Framework: API key is not configured for streaming request.");
                return Task.CompletedTask;
            }

            // Enhanced validation: reject null, empty, or whitespace-only prompts
            if (string.IsNullOrWhiteSpace(prompt))
            {
                Log.Warning("RimAI Framework: Empty or whitespace-only prompt provided to GetChatCompletionStreamAsync. No API request will be made.");
                return Task.CompletedTask;
            }

            if (onChunkReceived == null)
            {
                Log.Error("RimAI Framework: onChunkReceived callback is null.");
                return Task.CompletedTask;
            }

            var streamTcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            var requestData = new RequestData(prompt, onChunkReceived, streamTcs, cancellationToken);
            
            // Link the external cancellation token with the disposal token
            var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(_disposeCts.Token, cancellationToken);
            requestData.SetLinkedCancellationTokenSource(linkedCts);
            linkedCts.Token.Register(() => streamTcs.TrySetCanceled());

            _requestQueue.Enqueue(requestData);
            
            return streamTcs.Task;
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
                var response = await SendHttpRequestAsync(_settings.ChatCompletionsEndpoint, jsonBody, _settings.apiKey, CancellationToken.None);

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

        /// <summary>
        /// Cancels all pending requests in the queue.
        /// This method can be called from downstream mods to interrupt all ongoing requests.
        /// </summary>
        public void CancelAllRequests()
        {
            Log.Message("RimAI Framework: Cancelling all pending requests");
            
            // Cancel all requests in the queue
            while (_requestQueue.TryDequeue(out var requestData))
            {
                if (requestData != null)
                {
                    if (requestData.IsStreaming)
                    {
                        requestData.StreamCompletionSource?.TrySetCanceled();
                    }
                    else
                    {
                        requestData.CompletionSource?.TrySetCanceled();
                    }
                    requestData.LinkedCts?.Cancel();
                    requestData.Dispose();
                }
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
                    // Check if the request was already cancelled before processing
                    if (requestData.IsCancellationRequested)
                    {
                        // Complete the cancelled request immediately without processing
                        try
                        {
                            if (requestData.IsStreaming)
                            {
                                requestData.StreamCompletionSource.TrySetCanceled();
                            }
                            else
                            {
                                requestData.CompletionSource.TrySetCanceled();
                            }
                        }
                        finally
                        {
                            requestData.Dispose();
                        }
                        continue; // Skip to next request
                    }

                    // Process the request normally
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
                // Check for cancellation using the improved method that checks both tokens
                if (requestData.IsCancellationRequested)
                {
                    if (requestData.IsStreaming)
                    {
                        requestData.StreamCompletionSource.TrySetCanceled();
                    }
                    else
                    {
                        requestData.CompletionSource.TrySetCanceled();
                    }
                    return;
                }

                // Get the appropriate cancellation token for the actual HTTP request
                var effectiveCancellationToken = requestData.LinkedCts?.Token ?? requestData.CancellationToken;

                if (requestData.IsStreaming)
                {
                    // Handle streaming requests
                    await ExecuteStreamingRequestAsync(requestData.Prompt, requestData.StreamCallback, effectiveCancellationToken);
                    requestData.StreamCompletionSource.TrySetResult(true);
                }
                else
                {
                    // Handle non-streaming requests (existing logic)
                    var result = await ExecuteSingleRequestAsync(requestData.Prompt, effectiveCancellationToken);
                    requestData.CompletionSource.TrySetResult(result);
                }
            }
            catch (OperationCanceledException)
            {
                // Handle cancellation properly
                if (requestData.IsStreaming)
                {
                    requestData.StreamCompletionSource.TrySetCanceled();
                }
                else
                {
                    requestData.CompletionSource.TrySetCanceled();
                }
            }
            catch (Exception ex)
            {
                // This is a fallback catch that should rarely be hit
                Log.Error($"[RimAI] LLMManager: Unhandled exception in ProcessSingleRequestFromQueue: {ex}");
                if (requestData.IsStreaming)
                {
                    requestData.StreamCompletionSource.TrySetResult(false); // Indicate failure
                }
                else
                {
                    requestData.CompletionSource.TrySetResult(null); // Safe fallback - return null instead of crashing
                }
            }
            finally
            {
                // Dispose the request data to clean up linked cancellation token
                requestData.Dispose();
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
                var response = await SendHttpRequestAsync(_settings.ChatCompletionsEndpoint, jsonBody, _settings.apiKey, cancellationToken);

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
        /// Executes a streaming chat completion request.
        /// This method handles all exceptions internally and invokes the callback for each chunk.
        /// </summary>
        private async Task ExecuteStreamingRequestAsync(string prompt, Action<string> onChunkReceived, CancellationToken cancellationToken)
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
                    stream = true  // Enable streaming
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
                    // Check cancellation more frequently during streaming
                    cancellationToken.ThrowIfCancellationRequested();
                        
                    ProcessStreamLine(line, onChunkReceived, cancellationToken);
                }
            }
            catch (OperationCanceledException)
            {
                Log.Message("RimAI Framework: Streaming request was cancelled");
                // Re-throw cancellation to be handled by the calling method
                throw;
            }
            catch (Exception ex)
            {
                Log.Error($"[RimAI] LLMManager: Failed to execute streaming request. Details: {ex}");
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
        /// Processes a single line from the SSE stream.
        /// </summary>
        /// <param name="line">A single line from the streaming response.</param>
        /// <param name="onChunkReceived">The callback to invoke when content is received.</param>
        /// <param name="cancellationToken">Cancellation token to check for interruption.</param>
        private void ProcessStreamLine(string line, Action<string> onChunkReceived, CancellationToken cancellationToken = default)
        {
            // Early cancellation check
            if (cancellationToken.IsCancellationRequested)
                return;

            if (string.IsNullOrWhiteSpace(line))
                return;

            // SSE format: "data: {json}"
            if (!line.StartsWith("data: "))
                return;

            var data = line.Substring(6); // Remove "data: " prefix
            
            // Check for end of stream
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
                        // Check cancellation before processing chunk
                        if (cancellationToken.IsCancellationRequested)
                            return;

                        // Execute callback safely - check if we need to marshal to main thread
                        try
                        {
                            // For RimWorld, we need to ensure UI updates happen on the main thread
                            LongEventHandler.ExecuteWhenFinished(() => 
                            {
                                try
                                {
                                    // Final cancellation check before callback
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
                            // Fallback: call directly if LongEventHandler fails
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

        /// <summary>
        /// Processes a single chunk from a streaming HTTP response. (LEGACY METHOD - use ProcessStreamLine instead)
        /// This method is kept for compatibility.
        /// </summary>
        /// <param name="chunk">A single data chunk from the stream.</param>
        private void ProcessStreamChunk(string chunk)
        {
            // Legacy method - now redirects to ProcessStreamLine
            ProcessStreamLine(chunk, (content) => 
            {
                Log.Message($"RimAI Framework: Received chunk: {content}");
            });
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

        // Streaming response DTOs
        private class StreamingChatCompletionChunk
        {
            public List<StreamingChoice> choices { get; set; }
            public string id { get; set; }
            public string model { get; set; }
            public string @object { get; set; }
            public long created { get; set; }
        }

        private class StreamingChoice
        {
            public Delta delta { get; set; }
            public int index { get; set; }
            public string finish_reason { get; set; }
        }

        private class Delta
        {
            public string content { get; set; }
            public string role { get; set; }
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