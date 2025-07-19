using System;
using System.Threading;
using System.Threading.Tasks;

namespace RimAI.Framework.LLM.RequestQueue
{
    /// <summary>
    /// Internal class to hold all information about a single request.
    /// </summary>
    public class RequestData : IDisposable
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
}
