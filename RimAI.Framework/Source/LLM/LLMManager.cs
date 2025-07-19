using System;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using RimAI.Framework.Core;
using RimAI.Framework.LLM.Configuration;
using RimAI.Framework.LLM.Http;
using RimAI.Framework.LLM.Models;
using RimAI.Framework.LLM.RequestQueue;
using RimAI.Framework.LLM.Services;
using Verse;

namespace RimAI.Framework.LLM
{
    /// <summary>
    /// Simplified LLM Manager that provides a unified API for LLM interactions.
    /// Coordinates between various services while maintaining backward compatibility.
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

        #region Dependencies
        private readonly HttpClient _httpClient;
        private readonly SettingsManager _settingsManager;
        private readonly LLMServiceFactory _serviceFactory;
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
                // Initialize dependencies
                _httpClient = HttpClientFactory.CreateClient();
                _settingsManager = new SettingsManager();
                
                // Create services through factory
                var settings = _settingsManager.GetSettings();
                _serviceFactory = new LLMServiceFactory(_httpClient, settings);
                
                _executor = _serviceFactory.CreateExecutor();
                _requestQueue = _serviceFactory.CreateRequestQueue(_executor);
                _customService = _serviceFactory.CreateCustomService();
                _jsonService = _serviceFactory.CreateJsonService(_executor);
                _modService = _serviceFactory.CreateModService(_executor);
                
                Log.Message("RimAI Framework: LLMManager initialized successfully.");
            }
            catch (Exception ex)
            {
                Log.Error($"RimAI Framework: Failed to initialize LLMManager: {ex}");
                throw;
            }
        }
        #endregion

        #region Public Properties
        /// <summary>
        /// Gets whether streaming is currently enabled in the settings.
        /// </summary>
        public bool IsStreamingEnabled => _settingsManager.GetSettings()?.enableStreaming ?? false;

        /// <summary>
        /// Gets the current API settings. Read-only access for downstream mods.
        /// </summary>
        public RimAISettings CurrentSettings => _settingsManager.GetSettings();

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
        #endregion

        #region Public API Methods

        /// <summary>
        /// Enhanced API: Sends a message with customizable options
        /// </summary>
        public Task<string> SendMessageAsync(string prompt, LLMRequestOptions options = null, CancellationToken cancellationToken = default)
        {
            if (!ValidateRequest(prompt)) return Task.FromResult<string>(null);

            options ??= new LLMRequestOptions();
            
            // Delegate to streaming handler if needed
            if (ShouldUseStreaming(options))
            {
                return GetStreamingAsNonStreamingAsync(prompt, cancellationToken);
            }

            // Queue non-streaming request
            return QueueNonStreamingRequest(prompt, cancellationToken);
        }

        /// <summary>
        /// Enhanced API: Sends a streaming message with customizable options
        /// </summary>
        public Task SendMessageStreamAsync(string prompt, Action<string> onChunkReceived, LLMRequestOptions options = null, CancellationToken cancellationToken = default)
        {
            if (!ValidateRequest(prompt) || onChunkReceived == null) return Task.CompletedTask;

            return QueueStreamingRequest(prompt, onChunkReceived, cancellationToken);
        }

        /// <summary>
        /// Legacy API: Maintained for backward compatibility
        /// </summary>
        public Task<string> GetChatCompletionAsync(string prompt, CancellationToken cancellationToken = default)
        {
            return SendMessageAsync(prompt, null, cancellationToken);
        }

        /// <summary>
        /// Legacy API: Maintained for backward compatibility
        /// </summary>
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
            _settingsManager.RefreshSettings();
            Log.Warning("RimAI Framework: Settings refreshed. Some services may require restart to apply new settings.");
        }

        #endregion

        #region Private Helper Methods

        /// <summary>
        /// Validates request parameters
        /// </summary>
        private bool ValidateRequest(string prompt)
        {
            if (string.IsNullOrWhiteSpace(prompt))
            {
                Log.Warning("RimAI Framework: Empty prompt provided to SendMessageAsync.");
                return false;
            }

            if (_executor == null)
            {
                Log.Error("RimAI Framework: Executor not initialized. LLMManager may be in an invalid state.");
                return false;
            }

            var settings = _settingsManager.GetSettings();
            if (string.IsNullOrEmpty(settings?.apiKey))
            {
                Log.Error("RimAI Framework: API key is not configured. Please check mod settings.");
                return false;
            }

            return true;
        }

        /// <summary>
        /// Determines if streaming should be used based on options and settings
        /// </summary>
        private bool ShouldUseStreaming(LLMRequestOptions options)
        {
            // Explicit streaming setting takes precedence
            if (options.HasExplicitStreamingSetting)
            {
                return options.EnableStreaming;
            }
            
            // Fall back to global setting
            return _settingsManager.GetSettings().enableStreaming;
        }

        /// <summary>
        /// Queues a non-streaming request
        /// </summary>
        private Task<string> QueueNonStreamingRequest(string prompt, CancellationToken cancellationToken)
        {
            var tcs = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);
            var requestData = new RequestData(prompt, tcs, cancellationToken);
            
            var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            requestData.SetLinkedCancellationTokenSource(linkedCts);
            linkedCts.Token.Register(() => tcs.TrySetCanceled());

            _requestQueue.EnqueueRequest(requestData);
            return tcs.Task;
        }

        /// <summary>
        /// Queues a streaming request
        /// </summary>
        private Task QueueStreamingRequest(string prompt, Action<string> onChunkReceived, CancellationToken cancellationToken)
        {
            var streamTcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            var requestData = new RequestData(prompt, onChunkReceived, streamTcs, cancellationToken);
            
            var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            requestData.SetLinkedCancellationTokenSource(linkedCts);
            linkedCts.Token.Register(() => streamTcs.TrySetCanceled());

            _requestQueue.EnqueueRequest(requestData);
            return streamTcs.Task;
        }

        /// <summary>
        /// Converts streaming response to non-streaming by accumulating chunks
        /// </summary>
        private async Task<string> GetStreamingAsNonStreamingAsync(string prompt, CancellationToken cancellationToken = default)
        {
            var responseBuilder = new StringBuilder();
            var completionTcs = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);

            try
            {
                await SendMessageStreamAsync(prompt, chunk => responseBuilder.Append(chunk), null, cancellationToken);
                completionTcs.SetResult(responseBuilder.ToString());
            }
            catch (Exception ex)
            {
                completionTcs.SetException(ex);
            }

            return await completionTcs.Task;
        }

        #endregion

        #region IDisposable Implementation

        /// <summary>
        /// Releases all resources used by the LLMManager.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Protected implementation of Dispose pattern.
        /// </summary>
        /// <param name="disposing">true if disposing managed resources.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                try
                {
                    _requestQueue?.CancelAllRequests();
                    _requestQueue?.Dispose();
                    _executor?.Dispose();
                    _httpClient?.Dispose();
                }
                catch (Exception ex)
                {
                    Log.Error($"RimAI Framework: Error during LLMManager disposal: {ex.Message}");
                }
            }
        }

        #endregion
    }
}