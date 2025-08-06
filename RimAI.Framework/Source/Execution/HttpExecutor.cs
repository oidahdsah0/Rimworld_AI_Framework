// 引入必要的命名空间
using System;
using System.Net.Http;
using System.Threading; // [新增] 引入 CancellationToken
using System.Threading.Tasks;
using RimAI.Framework.Execution.Models;
using RimAI.Framework.Contracts; // [新增] 引入 Result<T>
using RimAI.Framework.Shared.Logging;

namespace RimAI.Framework.Execution
{
    /// <summary>
    /// 负责发送 HttpRequestMessage、接收 HttpResponseMessage，并应用重试策略。
    /// 【新增】现在它也负责响应取消信号。
    /// </summary>
    public class HttpExecutor
    {
        private readonly HttpClient _client;

        public HttpExecutor()
        {
            _client = HttpClientFactory.GetClient();
        }

        /// <summary>
        /// 异步执行一个HTTP请求，并根据提供的策略进行重试。
        /// </summary>
        /// <param name="request">已完全构建好的HTTP请求消息。</param>
        /// <param name="cancellationToken">用于中断操作的令牌。</param>
        /// <param name="isStreaming">是否为流式请求。流式请求将只读取响应头，非流式将读取完整响应体。</param>
        /// <param name="policy">本次请求要遵循的重试策略。如果为null，则使用默认策略。</param>
        /// <returns>一个封装了成功时的 HttpResponseMessage 或失败时的错误信息的 Result 对象。</returns>
        public async Task<Result<HttpResponseMessage>> ExecuteAsync(HttpRequestMessage request, CancellationToken cancellationToken, bool isStreaming = false, RetryPolicy policy = null)
        {
            policy ??= new RetryPolicy();

            HttpResponseMessage lastResponse = null;

            for (int i = 0; i <= policy.MaxRetries; i++)
            {
                try
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    var completionOption = isStreaming
                        ? HttpCompletionOption.ResponseHeadersRead
                        : HttpCompletionOption.ResponseContentRead;
                    
                    lastResponse = await _client.SendAsync(request, completionOption, cancellationToken);
                    
                    return Result<HttpResponseMessage>.Success(lastResponse);
                }
                catch (OperationCanceledException)
                {
                    RimAILogger.Log("HttpExecutor: Request was cancelled by the user.");
                    return Result<HttpResponseMessage>.Failure("Request was cancelled by the user.");
                }
                catch (Exception ex)
                {
                    RimAILogger.Error($"HttpExecutor: Request failed. Retrying... (Attempt {i + 1}/{policy.MaxRetries + 1}). Error: {ex.Message}");
                    
                    if (i >= policy.MaxRetries)
                    {
                        break;
                    }
                }

                try
                {
                    await Task.Delay(policy.InitialDelay, cancellationToken);
                }
                catch (OperationCanceledException)
                {
                    RimAILogger.Log("HttpExecutor: Retry delay was cancelled by the user.");
                    return Result<HttpResponseMessage>.Failure("Request was cancelled by the user during retry delay.");
                }
            }
            
            return Result<HttpResponseMessage>.Failure("HttpExecutor: All retry attempts failed to get a response due to network or other exceptions.");
        }
    }
}