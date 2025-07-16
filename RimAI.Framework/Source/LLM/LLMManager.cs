using System;
using System.Collections.Concurrent;
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
            _httpClient = new HttpClient();
            _httpClient.Timeout = TimeSpan.FromSeconds(60); // 60 second timeout
            LoadSettings();

            // Initialize concurrency controls
            _requestQueue = new ConcurrentQueue<RequestData>();
            _concurrentRequestLimiter = new SemaphoreSlim(3, 3); // Allow up to 3 concurrent requests
            _disposeCts = new CancellationTokenSource();
            _queueProcessorTask = Task.Run(() => ProcessQueueAsync(_disposeCts.Token));
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
                Messages.Message("RimAI.Framework.Messages.SendingRequest".Translate(), MessageTypeDefOf.NeutralEvent);
                var response = await SendHttpRequestAsync(_settings.apiEndpoint, jsonBody, _settings.apiKey, CancellationToken.None);

                if (response.success)
                {
                    Log.Message("[RimAI] LLMManager: Request successful.");
                    Messages.Message("RimAI.Framework.Messages.TestSuccess".Translate(), MessageTypeDefOf.PositiveEvent);
                    return (true, "RimAI.Framework.Messages.TestSuccess".Translate());
                }
                else
                {
                    var failMessage = "RimAI.Framework.Messages.TestFailed".Translate(response.statusCode, response.errorContent);
                    Log.Error($"[RimAI] LLMManager: Request failed. Status: {response.statusCode}, Body: {response.errorContent}");
                    Messages.Message(failMessage, MessageTypeDefOf.NegativeEvent);
                    return (false, failMessage);
                }
            }
            catch (HttpRequestException e)
            {
                var errorMessage = "RimAI.Framework.Messages.TestError".Translate(e.Message);
                Log.Error($"[RimAI] LLMManager: HttpRequestException: {e.Message}");
                Messages.Message(errorMessage, MessageTypeDefOf.NegativeEvent);
                return (false, errorMessage);
            }
            catch (TaskCanceledException e)
            {
                var errorMessage = "RimAI.Framework.Messages.TestError".Translate(e.Message);
                Log.Error($"[RimAI] LLMManager: TaskCanceledException (Timeout): {e.Message}");
                Messages.Message(errorMessage, MessageTypeDefOf.NegativeEvent);
                return (false, errorMessage);
            }
            catch (Exception e)
            {
                var errorMessage = "RimAI.Framework.Messages.TestError".Translate(e.Message);
                Log.Error($"[RimAI] LLMManager: An unexpected error occurred: {e.ToString()}");
                Messages.Message(errorMessage, MessageTypeDefOf.NegativeEvent);
                return (false, errorMessage);
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
                    await _concurrentRequestLimiter.WaitAsync(cancellationToken);

                    _ = Task.Run(async () =>
                    {
                        try
                        {
                            if (requestData.CancellationToken.IsCancellationRequested)
                            {
                                requestData.CompletionSource.TrySetCanceled();
                                return;
                            }

                            var result = await ExecuteSingleRequestAsync(requestData.Prompt, requestData.CancellationToken);
                            requestData.CompletionSource.TrySetResult(result);
                        }
                        catch (OperationCanceledException)
                        {
                            requestData.CompletionSource.TrySetCanceled();
                        }
                        catch (Exception ex)
                        {
                            requestData.CompletionSource.TrySetException(ex);
                        }
                        finally
                        {
                            _concurrentRequestLimiter.Release();
                        }
                    }, cancellationToken);
                }
                else
                {
                    await Task.Delay(100, cancellationToken); // Wait if queue is empty
                }
            }
        }

        /// <summary>
        /// Executes a single chat completion request.
        /// </summary>
        private async Task<string> ExecuteSingleRequestAsync(string prompt, CancellationToken cancellationToken)
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
            
            // Error is logged within SendHttpRequestAsync
            return null;
        }
        
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