using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using RimAI.Framework.LLM.Services;
using Verse;

namespace RimAI.Framework.LLM.RequestQueue
{
    /// <summary>
    /// Manages a queue of LLM requests with concurrency control
    /// </summary>
    public class LLMRequestQueue : IDisposable
    {
        private readonly ConcurrentQueue<RequestData> _requestQueue;
        private readonly SemaphoreSlim _concurrentRequestLimiter;
        private readonly CancellationTokenSource _disposeCts;
        private readonly Task _queueProcessorTask;
        private readonly ILLMExecutor _executor;

        public LLMRequestQueue(ILLMExecutor executor, int maxConcurrentRequests = 3)
        {
            _requestQueue = new ConcurrentQueue<RequestData>();
            _concurrentRequestLimiter = new SemaphoreSlim(maxConcurrentRequests, maxConcurrentRequests);
            _disposeCts = new CancellationTokenSource();
            _executor = executor;
            _queueProcessorTask = Task.Run(() => ProcessQueueAsync(_disposeCts.Token));
        }

        public void EnqueueRequest(RequestData requestData)
        {
            _requestQueue.Enqueue(requestData);
        }

        public void CancelAllRequests()
        {
            Log.Message("RimAI Framework: Cancelling all pending requests");
            
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

        private async Task ProcessQueueAsync(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                if (_requestQueue.TryDequeue(out var requestData))
                {
                    if (requestData.IsCancellationRequested)
                    {
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
                        continue;
                    }

                    await ProcessSingleRequestFromQueue(requestData, cancellationToken);
                }
                else
                {
                    await Task.Delay(100, cancellationToken);
                }
            }
        }

        private async Task ProcessSingleRequestFromQueue(RequestData requestData, CancellationToken cancellationToken)
        {
            await _concurrentRequestLimiter.WaitAsync(cancellationToken);
            try
            {
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

                var effectiveCancellationToken = requestData.LinkedCts?.Token ?? requestData.CancellationToken;

                if (requestData.IsStreaming)
                {
                    await _executor.ExecuteStreamingRequestAsync(requestData.Prompt, requestData.StreamCallback, effectiveCancellationToken);
                    requestData.StreamCompletionSource.TrySetResult(true);
                }
                else
                {
                    var result = await _executor.ExecuteSingleRequestAsync(requestData.Prompt, effectiveCancellationToken);
                    requestData.CompletionSource.TrySetResult(result);
                }
            }
            catch (OperationCanceledException)
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
            catch (Exception ex)
            {
                Log.Error($"[RimAI] LLMRequestQueue: Unhandled exception: {ex}");
                if (requestData.IsStreaming)
                {
                    requestData.StreamCompletionSource.TrySetResult(false);
                }
                else
                {
                    requestData.CompletionSource.TrySetResult(null);
                }
            }
            finally
            {
                requestData.Dispose();
                _concurrentRequestLimiter.Release();
            }
        }

        public void Dispose()
        {
            _disposeCts.Cancel();
            _disposeCts.Dispose();
            _concurrentRequestLimiter?.Dispose();
        }
    }
}
