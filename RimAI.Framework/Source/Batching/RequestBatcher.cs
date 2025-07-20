using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using RimAI.Framework.Core;
using Verse;
using static RimAI.Framework.Core.RimAILogger;

namespace RimAI.Framework.Batching
{
    /// <summary>
    /// 请求批处理器，实现请求的批量处理机制以提升性能
    /// </summary>
    public class RequestBatcher<TRequest, TResponse> : IDisposable
    {
        private readonly ConcurrentQueue<BatchItem> _pendingItems;
        private readonly Timer _batchTimer;
        private readonly Func<List<TRequest>, CancellationToken, Task<List<TResponse>>> _batchProcessor;
        private readonly object _batchLock = new object();
        
        private readonly int _maxBatchSize;
        private readonly TimeSpan _batchWindow;
        private readonly int _maxConcurrentBatches;
        private bool _disposed;
        
        // 统计信息
        private long _totalRequests;
        private long _batchesProcessed;
        private long _successfulBatches;
        private long _failedBatches;
        private int _activeBatches;
        
        /// <summary>
        /// 批处理项
        /// </summary>
        private class BatchItem
        {
            public TRequest Request { get; set; }
            public TaskCompletionSource<TResponse> CompletionSource { get; set; }
            public DateTime EnqueueTime { get; set; }
            public CancellationToken CancellationToken { get; set; }
            
            /// <summary>
            /// 项目年龄
            /// </summary>
            public TimeSpan Age => DateTime.UtcNow - EnqueueTime;
        }
        
        /// <summary>
        /// 当前待处理项目数量
        /// </summary>
        public int PendingCount => _pendingItems.Count;
        
        /// <summary>
        /// 最大批处理大小
        /// </summary>
        public int MaxBatchSize => _maxBatchSize;
        
        /// <summary>
        /// 批处理时间窗口
        /// </summary>
        public TimeSpan BatchWindow => _batchWindow;
        
        /// <summary>
        /// 总请求数
        /// </summary>
        public long TotalRequests => _totalRequests;
        
        /// <summary>
        /// 已处理批次数
        /// </summary>
        public long BatchesProcessed => _batchesProcessed;
        
        /// <summary>
        /// 批处理成功率
        /// </summary>
        public double BatchSuccessRate => _batchesProcessed > 0 ? (double)_successfulBatches / _batchesProcessed : 0.0;
        
        /// <summary>
        /// 当前活跃批次数
        /// </summary>
        public int ActiveBatches => _activeBatches;
        
        /// <summary>
        /// 是否已被释放
        /// </summary>
        public bool IsDisposed => _disposed;
        
        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="batchProcessor">批处理函数</param>
        /// <param name="maxBatchSize">最大批处理大小</param>
        /// <param name="batchWindow">批处理时间窗口</param>
        /// <param name="maxConcurrentBatches">最大并发批次数</param>
        public RequestBatcher(
            Func<List<TRequest>, CancellationToken, Task<List<TResponse>>> batchProcessor,
            int maxBatchSize = 10,
            TimeSpan? batchWindow = null,
            int maxConcurrentBatches = 3)
        {
            _batchProcessor = batchProcessor ?? throw new ArgumentNullException(nameof(batchProcessor));
            _maxBatchSize = Math.Max(1, maxBatchSize);
            _batchWindow = batchWindow ?? TimeSpan.FromMilliseconds(500);
            _maxConcurrentBatches = Math.Max(1, maxConcurrentBatches);
            
            _pendingItems = new ConcurrentQueue<BatchItem>();
            
            // 定时器用于处理时间窗口内的批次
            _batchTimer = new Timer(
                ProcessBatchByTimer,
                null,
                _batchWindow,
                _batchWindow
            );
            
            Info("RequestBatcher initialized - MaxBatch: {0}, Window: {1}ms, MaxConcurrent: {2}", 
                 _maxBatchSize, _batchWindow.TotalMilliseconds, _maxConcurrentBatches);
        }
        
        /// <summary>
        /// 添加请求到批处理队列
        /// </summary>
        /// <param name="request">请求对象</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>批处理结果</returns>
        public async Task<TResponse> AddRequestAsync(TRequest request, CancellationToken cancellationToken = default)
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(RequestBatcher<TRequest, TResponse>));
                
            if (request == null)
                throw new ArgumentNullException(nameof(request));
            
            var completionSource = new TaskCompletionSource<TResponse>();
            var batchItem = new BatchItem
            {
                Request = request,
                CompletionSource = completionSource,
                EnqueueTime = DateTime.UtcNow,
                CancellationToken = cancellationToken
            };
            
            _pendingItems.Enqueue(batchItem);
            Interlocked.Increment(ref _totalRequests);
            
            Debug("Request added to batch queue. Pending: {0}", _pendingItems.Count);
            
            // 如果队列达到最大批处理大小，立即触发批处理
            if (_pendingItems.Count >= _maxBatchSize)
            {
                _ = Task.Run(() => ProcessBatchIfReady(), cancellationToken);
            }
            
            // 注册取消令牌
            if (cancellationToken.CanBeCanceled)
            {
                cancellationToken.Register(() =>
                {
                    if (!completionSource.Task.IsCompleted)
                    {
                        completionSource.TrySetCanceled(cancellationToken);
                    }
                });
            }
            
            try
            {
                return await completionSource.Task;
            }
            catch (TaskCanceledException)
            {
                Debug("Request was cancelled");
                throw;
            }
        }
        
        /// <summary>
        /// 立即刷新所有待处理的请求
        /// </summary>
        /// <param name="cancellationToken">取消令牌</param>
        public async Task FlushAsync(CancellationToken cancellationToken = default)
        {
            if (_disposed || _pendingItems.IsEmpty)
                return;
                
            Debug("Flushing all pending requests. Count: {0}", _pendingItems.Count);
            
            await ProcessBatchIfReady(forceProcess: true, cancellationToken);
        }
        
        /// <summary>
        /// 获取批处理统计信息
        /// </summary>
        public BatcherStats GetStats()
        {
            var oldestItemAge = TimeSpan.Zero;
            
            // 计算最老项目的年龄
            if (!_pendingItems.IsEmpty)
            {
                var items = _pendingItems.ToArray();
                if (items.Length > 0)
                {
                    oldestItemAge = items.Max(item => item.Age);
                }
            }
            
            return new BatcherStats
            {
                PendingRequests = _pendingItems.Count,
                TotalRequests = _totalRequests,
                BatchesProcessed = _batchesProcessed,
                SuccessfulBatches = _successfulBatches,
                FailedBatches = _failedBatches,
                BatchSuccessRate = BatchSuccessRate,
                ActiveBatches = _activeBatches,
                MaxBatchSize = _maxBatchSize,
                BatchWindow = _batchWindow,
                OldestPendingAge = oldestItemAge
            };
        }
        
        /// <summary>
        /// 定时器触发的批处理
        /// </summary>
        private void ProcessBatchByTimer(object state)
        {
            if (_disposed)
                return;
                
            _ = Task.Run(async () =>
            {
                try
                {
                    await ProcessBatchIfReady();
                }
                catch (Exception ex)
                {
                    Error("Error in timer-triggered batch processing: {0}", ex.Message);
                }
            });
        }
        
        /// <summary>
        /// 如果准备就绪则处理批次
        /// </summary>
        private async Task ProcessBatchIfReady(bool forceProcess = false, CancellationToken cancellationToken = default)
        {
            if (_disposed || _pendingItems.IsEmpty)
                return;
                
            // 检查并发限制
            if (_activeBatches >= _maxConcurrentBatches)
            {
                Debug("Maximum concurrent batches reached: {0}", _maxConcurrentBatches);
                return;
            }
            
            List<BatchItem> batchItems;
            
            lock (_batchLock)
            {
                // 再次检查队列状态
                if (_pendingItems.IsEmpty)
                    return;
                
                batchItems = new List<BatchItem>();
                
                // 收集批处理项目
                while (batchItems.Count < _maxBatchSize && _pendingItems.TryDequeue(out var item))
                {
                    // 检查项目是否已取消
                    if (item.CancellationToken.IsCancellationRequested)
                    {
                        item.CompletionSource.TrySetCanceled(item.CancellationToken);
                        continue;
                    }
                    
                    batchItems.Add(item);
                }
                
                // 如果没有有效项目，返回
                if (batchItems.Count == 0)
                    return;
                
                // 检查是否应该处理批次
                if (!forceProcess && batchItems.Count < _maxBatchSize)
                {
                    var oldestItem = batchItems.OrderBy(item => item.EnqueueTime).First();
                    
                    // 如果最老的项目还没有达到批处理窗口时间，推迟处理
                    if (oldestItem.Age < _batchWindow)
                    {
                        // 将项目重新入队
                        foreach (var item in batchItems.Reverse<BatchItem>())
                        {
                            _pendingItems.Enqueue(item);
                        }
                        return;
                    }
                }
            }
            
            // 处理批次
            await ProcessBatch(batchItems, cancellationToken);
        }
        
        /// <summary>
        /// 处理批次
        /// </summary>
        private async Task ProcessBatch(List<BatchItem> batchItems, CancellationToken cancellationToken = default)
        {
            if (batchItems.Count == 0)
                return;
                
            Interlocked.Increment(ref _activeBatches);
            Interlocked.Increment(ref _batchesProcessed);
            
            Debug("Processing batch with {0} items", batchItems.Count);
            
            try
            {
                var requests = batchItems.Select(item => item.Request).ToList();
                var responses = await _batchProcessor(requests, cancellationToken);
                
                // 验证响应数量与请求数量匹配
                if (responses.Count != requests.Count)
                {
                    throw new InvalidOperationException(
                        $"Batch processor returned {responses.Count} responses for {requests.Count} requests");
                }
                
                // 分发响应给对应的TaskCompletionSource
                for (int i = 0; i < batchItems.Count; i++)
                {
                    var item = batchItems[i];
                    var response = responses[i];
                    
                    if (!item.CompletionSource.Task.IsCompleted)
                    {
                        item.CompletionSource.TrySetResult(response);
                    }
                }
                
                Interlocked.Increment(ref _successfulBatches);
                Debug("Batch processed successfully");
            }
            catch (OperationCanceledException)
            {
                // 处理取消的情况
                foreach (var item in batchItems)
                {
                    if (!item.CompletionSource.Task.IsCompleted)
                    {
                        item.CompletionSource.TrySetCanceled(cancellationToken);
                    }
                }
                
                Debug("Batch processing was cancelled");
            }
            catch (Exception ex)
            {
                // 处理异常的情况
                foreach (var item in batchItems)
                {
                    if (!item.CompletionSource.Task.IsCompleted)
                    {
                        item.CompletionSource.TrySetException(ex);
                    }
                }
                
                Interlocked.Increment(ref _failedBatches);
                Error("Batch processing failed: {0}", ex.Message);
            }
            finally
            {
                Interlocked.Decrement(ref _activeBatches);
            }
        }
        
        /// <summary>
        /// 释放资源
        /// </summary>
        public void Dispose()
        {
            if (_disposed)
                return;
                
            _disposed = true;
            
            try
            {
                _batchTimer?.Dispose();
                
                var stats = GetStats();
                Info("RequestBatcher disposed. Final stats - Pending: {0}, Success Rate: {1:P1}, Total Batches: {2}", 
                     stats.PendingRequests, stats.BatchSuccessRate, stats.BatchesProcessed);
                
                // 取消所有待处理的请求
                while (_pendingItems.TryDequeue(out var item))
                {
                    if (!item.CompletionSource.Task.IsCompleted)
                    {
                        item.CompletionSource.TrySetCanceled();
                    }
                }
            }
            catch (Exception ex)
            {
                Error("Error disposing RequestBatcher: {0}", ex.Message);
            }
        }
    }
    
    /// <summary>
    /// 批处理统计信息
    /// </summary>
    public class BatcherStats
    {
        public int PendingRequests { get; set; }
        public long TotalRequests { get; set; }
        public long BatchesProcessed { get; set; }
        public long SuccessfulBatches { get; set; }
        public long FailedBatches { get; set; }
        public double BatchSuccessRate { get; set; }
        public int ActiveBatches { get; set; }
        public int MaxBatchSize { get; set; }
        public TimeSpan BatchWindow { get; set; }
        public TimeSpan OldestPendingAge { get; set; }
    }
}
