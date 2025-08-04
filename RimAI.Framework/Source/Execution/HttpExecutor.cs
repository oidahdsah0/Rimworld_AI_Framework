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
        /// <param name="cancellationToken">【新增】用于中断操作的令牌。</param>
        /// <param name="policy">本次请求要遵循的重试策略。如果为null，则使用默认策略。</param>
        /// <returns>一个封装了成功时的 HttpResponseMessage 或失败时的错误信息的 Result 对象。</returns>
        public async Task<Result<HttpResponseMessage>> ExecuteAsync(HttpRequestMessage request, CancellationToken cancellationToken, RetryPolicy policy = null)
        {
            policy ??= new RetryPolicy();

            HttpResponseMessage lastResponse = null;

            for (int i = 0; i <= policy.MaxRetries; i++)
            {
                try
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    // 【核心修改】将 cancellationToken 传递给 SendAsync 方法。
                    lastResponse = await _client.SendAsync(request, cancellationToken);

                    // 只要收到了响应（无论状态码是什么），就认为这次HTTP执行是成功的。
                    // 上层逻辑（比如 Manager）会去检查具体的状态码。
                    return Result<HttpResponseMessage>.Success(lastResponse);
                }
                // 【核心修改】为 OperationCanceledException 添加一个专门的 catch 块。
                catch (OperationCanceledException)
                {
                    RimAILogger.Log("HttpExecutor: Request was cancelled by the user.");
                    // 这是真正的“失败”，因为我们没有获取到任何响应。
                    return Result<HttpResponseMessage>.Failure("Request was cancelled by the user.");
                }
                catch (Exception ex)
                {
                    // 其他网络异常，比如 DNS 解析失败，也属于真正的失败。
                    RimAILogger.Error($"HttpExecutor: Request failed. Retrying... (Attempt {i + 1}/{policy.MaxRetries + 1}). Error: {ex.Message}");

                    // 如果这是最后一次尝试，则跳出循环，返回最终的失败结果。
                    if (i >= policy.MaxRetries)
                    {
                        break;
                    }
                }

                try
                {
                    // 【核心修改】在等待时，也监听取消信号。
                    await Task.Delay(policy.InitialDelay, cancellationToken);
                }
                catch (OperationCanceledException)
                {
                    RimAILogger.Log("HttpExecutor: Retry delay was cancelled by the user.");
                    return Result<HttpResponseMessage>.Failure("Request was cancelled by the user during retry delay.");
                }
            }
            
            // 如果所有重试都因网络等异常失败，并且从未收到过任何响应。
            return Result<HttpResponseMessage>.Failure("HttpExecutor: All retry attempts failed to get a response due to network or other exceptions.");
        }
    }
}