using System;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using RimAI.Framework.Core;
using RimAI.Framework.LLM.Models;
using RimAI.Framework.LLM.RequestQueue;
using RimAI.Framework.LLM.Services;
using Verse;
using RimWorld;

namespace RimAI.Framework.LLM
{
    /// <summary>
    /// Simplified LLM Manager that coordinates between components.
    /// This class now focuses on high-level coordination rather than implementation details.
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
        private readonly ILLMExecutor _executor;
        private readonly LLMRequestQueue _requestQueue;
        private readonly ICustomLLMService _customService;
        private readonly IJsonLLMService _jsonService;
        private readonly IModService _modService;
        #endregion

        #region Constructor
        /// <summary>
        /// Private constructor for singleton pattern.
        /// </summary>
        private LLMManager()
        {
            try
            {
                _httpClient = CreateHttpClient();
                LoadSettings();

                // Initialize services
                _executor = new LLMExecutor(_httpClient, _settings);
                _requestQueue = new LLMRequestQueue(_executor);
                _customService = new CustomLLMService(_httpClient, _settings.apiKey, _settings.apiEndpoint);
                _jsonService = new JsonLLMService(_executor, _settings);
                _modService = new ModService(_executor, _settings);
                
                Log.Message("RimAI Framework: LLMManager initialized successfully.");
            }
            catch (Exception ex)
            {
                Log.Error($"RimAI Framework: Failed to initialize LLMManager: {ex}");
                throw;
            }
        }

        /// <summary>
        /// Creates and configures HttpClient with safe settings for Unity/Mono environment
        /// </summary>
        private HttpClient CreateHttpClient()
        {
            try
            {
                var handler = new HttpClientHandler();
                
                try
                {
                    handler.ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) => true;
                }
                catch (Exception ex)
                {
                    Log.Warning($"RimAI Framework: Could not configure SSL validation bypass: {ex.Message}");
                }
                
                var client = new HttpClient(handler);
                client.Timeout = TimeSpan.FromSeconds(60);
                client.DefaultRequestHeaders.Add("User-Agent", "RimAI-Framework/1.0");
                client.DefaultRequestHeaders.Add("Accept", "application/json");
                
                Log.Message("RimAI Framework: HttpClient initialized successfully.");
                return client;
            }
            catch (Exception ex)
            {
                Log.Error($"RimAI Framework: Failed to initialize HttpClient with custom handler, using default: {ex.Message}");
                var client = new HttpClient();
                client.Timeout = TimeSpan.FromSeconds(60);
                return client;
            }
        }
        #endregion

        #region Public Methods
        
        /// <summary>
        /// Gets whether streaming is currently enabled in the settings.
        /// </summary>
        public bool IsStreamingEnabled => _settings?.enableStreaming ?? false;

        /// <summary>
        /// Gets the current API settings. Read-only access for downstream mods.
        /// </summary>
        public RimAISettings CurrentSettings => _settings;

        /// <summary>
        /// Gets access to custom LLM service for advanced usage
        /// </summary>
        public ICustomLLMService CustomService => _customService;

        /// <summary>
        /// Gets access to JSON-enforced LLM service
        /// </summary>
        public IJsonLLMService JsonService => _jsonService;

        /// <summary>
        /// Gets access to Mod service for enhanced mod integration
        /// </summary>
        public IModService ModService => _modService;

        /// <summary>
        /// Enhanced API: Sends a message with customizable options
        /// </summary>
        public Task<string> SendMessageAsync(string prompt, LLMRequestOptions options = null, CancellationToken cancellationToken = default)
        {
            if (!ValidateRequest(prompt)) return Task.FromResult<string>(null);

            options ??= new LLMRequestOptions();
            
            // Use streaming or non-streaming based on options
            if (options.EnableStreaming || (_settings.enableStreaming && !options.EnableStreaming))
            {
                return GetStreamingAsNonStreamingAsync(prompt, cancellationToken);
            }

            var tcs = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);
            var requestData = new RequestData(prompt, tcs, cancellationToken);
            
            var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            requestData.SetLinkedCancellationTokenSource(linkedCts);
            linkedCts.Token.Register(() => tcs.TrySetCanceled());

            _requestQueue.EnqueueRequest(requestData);
            return tcs.Task;
        }

        /// <summary>
        /// Enhanced API: Sends a streaming message with customizable options
        /// </summary>
        public Task SendMessageStreamAsync(string prompt, Action<string> onChunkReceived, LLMRequestOptions options = null, CancellationToken cancellationToken = default)
        {
            if (!ValidateRequest(prompt) || onChunkReceived == null) return Task.CompletedTask;

            var streamTcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            var requestData = new RequestData(prompt, onChunkReceived, streamTcs, cancellationToken);
            
            var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            requestData.SetLinkedCancellationTokenSource(linkedCts);
            linkedCts.Token.Register(() => streamTcs.TrySetCanceled());

            _requestQueue.EnqueueRequest(requestData);
            return streamTcs.Task;
        }
        /// <summary>
        /// Legacy API: Asynchronously enqueues a chat completion request to the LLM API.
        /// Maintained for backward compatibility.
        /// </summary>
        /// <param name="prompt">The text prompt to send to the LLM.</param>
        /// <param name="cancellationToken">A cancellation token to cancel the request.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the content of the LLM's response, or null if an error occurred.</returns>
        public Task<string> GetChatCompletionAsync(string prompt, CancellationToken cancellationToken = default)
        {
            return SendMessageAsync(prompt, null, cancellationToken);
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
        /// Legacy API: Gets a chat completion as a stream of tokens.
        /// Maintained for backward compatibility.
        /// </summary>
        /// <param name="prompt">The text prompt to send to the LLM.</param>
        /// <param name="onChunkReceived">An action to be called for each received token chunk.</param>
        /// <param name="cancellationToken">A cancellation token to cancel the stream.</param>
        /// <returns>A task that completes when the streaming is finished.</returns>
        public Task GetChatCompletionStreamAsync(string prompt, Action<string> onChunkReceived, CancellationToken cancellationToken = default)
        {
            return SendMessageStreamAsync(prompt, onChunkReceived, null, cancellationToken);
        }

        /// <summary>
        /// Tests the connection to the LLM API using the current settings.
        /// </summary>
        /// <returns>A tuple containing a boolean for success and a status message.</returns>
        public Task<(bool success, string message)> TestConnectionAsync()
        {
            return _executor.TestConnectionAsync();
        }

        /// <summary>
        /// Cancels all pending requests in the queue.
        /// </summary>
        public void CancelAllRequests()
        {
            _requestQueue.CancelAllRequests();
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
        /// Validates a request prompt and API settings
        /// </summary>
        private bool ValidateRequest(string prompt)
        {
            if (string.IsNullOrEmpty(_settings.apiKey))
            {
                Log.Error("RimAI Framework: API key is not configured. Please check mod settings.");
                return false;
            }

            if (string.IsNullOrWhiteSpace(prompt))
            {
                Log.Warning("RimAI Framework: Empty or whitespace-only prompt provided. No API request will be made.");
                return false;
            }

            return true;
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
                await SendMessageStreamAsync(
                    prompt,
                    chunk => fullResponse.Append(chunk),
                    null,
                    cancellationToken
                );

                return fullResponse.ToString();
            }
            catch (OperationCanceledException)
            {
                Log.Message("RimAI Framework: Streaming request was cancelled by user");
                throw;
            }
            catch (Exception ex)
            {
                Log.Error($"RimAI Framework: Error in streaming-as-non-streaming request: {ex}");
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
                if (_settings == null)
                {
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

        #endregion

        #region Cleanup
        /// <summary>
        /// Disposes of resources when the manager is no longer needed.
        /// </summary>
        public void Dispose()
        {
            _requestQueue?.Dispose();
            _httpClient?.Dispose();
        }
        #endregion
    }
}