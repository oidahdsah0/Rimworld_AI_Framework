// 引入必要的命名空间
using System;
using System.Net.Http; // 核心网络类
using System.Threading.Tasks; // 用于异步编程 (async/await)
using RimAI.Framework.Execution.Models; // 我们的 RetryPolicy
using RimAI.Framework.Shared.Logging; // 我们的日志工具

namespace RimAI.Framework.Execution
{
    /// <summary>
    /// 负责发送 HttpRequestMessage、接收 HttpResponseMessage，并应用重试策略。
    /// 这是框架中所有出站 HTTP 通信的执行者。
    /// </summary>
    public class HttpExecutor
    {
        // 持有一个从工厂获取的 HttpClient 实例。
        private readonly HttpClient _client;

        /// <summary>
        /// 构造函数。在创建 HttpExecutor 实例时，
        /// 会从 HttpClientFactory 获取那个全局共享的客户端。
        /// </summary>
        public HttpExecutor()
        {
            _client = HttpClientFactory.GetClient();
        }

        /// <summary>
        /// 异步执行一个HTTP请求，并根据提供的策略进行重试。
        /// </summary>
        /// <param name="request">已完全构建好的HTTP请求消息。</param>
        /// <param name="policy">本次请求要遵循的重试策略。</param>
        /// <returns>一个表示异步操作的任务，其结果是最终的HTTP响应消息。</returns>
        public async Task<HttpResponseMessage> ExecuteAsync(HttpRequestMessage request, RetryPolicy policy)
        {
            // 如果没有提供策略，就使用一个默认的策略，确保代码健壮。
            policy ??= new RetryPolicy();

            HttpResponseMessage response = null;
            var currentDelay = policy.InitialDelay;

            // 我们使用一个 for 循环来实现重试逻辑。
            // 循环次数为 1 (初始尝试) + MaxRetries。
            for (int i = 0; i <= policy.MaxRetries; i++)
            {
                try
                {
                    // 使用 await 关键字异步地发送请求，这不会阻塞当前线程。
                    response = await _client.SendAsync(request);

                    // 检查响应是否成功。IsSuccessStatusCode 会检查状态码是否在 200-299 范围内。
                    // 如果成功了，我们直接跳出循环，返回成功的响应。
                    if (response.IsSuccessStatusCode)
                    {
                        return response;
                    }

                    // 如果响应码表示这是一个服务器端错误 (5xx)，我们认为这可能是一个瞬时问题，值得重试。
                    if ((int)response.StatusCode >= 500)
                    {
                        RimAILogger.Warning($"HttpExecutor: Received server error ({(int)response.StatusCode}). Retrying in {currentDelay.TotalMilliseconds}ms... (Attempt {i + 1}/{policy.MaxRetries + 1})");
                        // 继续循环，进入下面的延迟和重试逻辑。
                    }
                    else
                    {
                        // 如果是客户端错误 (4xx)，比如 401 Unauthorized 或 400 Bad Request，
                        // 这通常不是重试能解决的，我们应该立即返回这个失败的响应，让上层逻辑去处理。
                        return response;
                    }
                }
                catch (Exception ex)
                {
                    // 捕获可能发生的任何网络异常，比如 HttpRequestException 或 TaskCanceledException (超时)。
                    RimAILogger.Error($"HttpExecutor: Request failed with exception. Retrying in {currentDelay.TotalMilliseconds}ms... (Attempt {i + 1}/{policy.MaxRetries + 1}). Error: {ex.Message}");
                }

                // --- 如果代码执行到这里，说明需要进行一次重试 ---

                // 在最后一次尝试失败后，我们不应该再等待了，直接退出循环。
                if (i >= policy.MaxRetries)
                {
                    break;
                }

                // 使用 await Task.Delay 来实现异步等待，这同样不会阻塞线程。
                await Task.Delay(currentDelay);

                // 如果启用了指数退避，将下一次的延迟时间加倍。
                if (policy.UseExponentialBackoff)
                {
                    currentDelay = TimeSpan.FromMilliseconds(currentDelay.TotalMilliseconds * 2);
                }
            }
            
            // 如果所有尝试都用尽了，返回最后一次收到的响应（无论它是什么）。
            // 如果连一次响应都没收到（比如每次都抛出网络异常），response 可能是 null。
            // 上层逻辑需要能处理这种情况。
            return response;
        }
    }
}