using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using RimAI.Framework.LLM.Models;

namespace RimAI.Framework.LLM.Services
{
    /// <summary>
    /// Interface for executing LLM requests
    /// </summary>
    public interface ILLMExecutor : IDisposable
    {
        Task<string> ExecuteSingleRequestAsync(string prompt, CancellationToken cancellationToken);
        Task ExecuteStreamingRequestAsync(string prompt, Action<string> onChunkReceived, CancellationToken cancellationToken);
        Task<(bool success, string message)> TestConnectionAsync();
    }

    /// <summary>
    /// Interface for custom LLM requests
    /// </summary>
    public interface ICustomLLMService
    {
        Task<CustomResponse> SendCustomRequestAsync(CustomRequest request);
        IAsyncEnumerable<string> SendCustomStreamRequestAsync(CustomRequest request);
    }

    /// <summary>
    /// Interface for JSON-enforced LLM requests
    /// </summary>
    public interface IJsonLLMService
    {
        Task<JsonResponse<T>> SendJsonRequestAsync<T>(string prompt, LLMRequestOptions options = null);
        IAsyncEnumerable<string> SendJsonStreamRequestAsync(string prompt, LLMRequestOptions options = null);
        Task<JsonResponse<object>> SendJsonRequestAsync(string prompt, object schema, LLMRequestOptions options = null);
    }

    /// <summary>
    /// Enhanced interface for Mod services with flexible options
    /// </summary>
    public interface IModService
    {
        // Unified interface with options parameter
        Task<string> SendMessageAsync(string modId, string message, LLMRequestOptions options = null);
        IAsyncEnumerable<string> SendMessageStreamAsync(string modId, string message, LLMRequestOptions options = null);
        
        // Compatibility - existing interface
        Task<string> SendMessageToModAsync(string modId, string message);
    }
}
