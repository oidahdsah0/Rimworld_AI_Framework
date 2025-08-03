// =====================================================================================================================
// 文件: ChatManager.cs
//
// 作用:
//  这是聊天功能的核心协调器 (Coordinator)。它如同一个项目经理，负责指挥其他服务
//  (SettingsManager, Translators, HttpExecutor) 按正确的顺序协同工作，从而完成一次
//  完整的聊天请求处理流程。
//
// 设计模式:
//  - 协调者模式 (Coordinator Pattern): 将业务流程的控制权集中在此处，避免服务之间直接耦合。
//  - 依赖注入 (Dependency Injection): 它所依赖的所有服务都通过构造函数从外部“注入”，而不是由它自己创建。
// =====================================================================================================================

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using RimAI.Framework.Configuration;
using RimAI.Framework.Execution;
using RimAI.Framework.Shared.Models;
using RimAI.Framework.Translation;
using RimAI.Framework.Translation.Models;


namespace RimAI.Framework.Core
{
    public class ChatManager
    {
        // ... 成员变量和构造函数保持不变 ...
        private readonly SettingsManager _settingsManager;
        private readonly ChatRequestTranslator _requestTranslator;
        private readonly HttpExecutor _httpExecutor;
        private readonly ChatResponseTranslator _responseTranslator;

        public ChatManager(
            SettingsManager settingsManager,
            ChatRequestTranslator requestTranslator,
            HttpExecutor httpExecutor,
            ChatResponseTranslator responseTranslator)
        {
            _settingsManager = settingsManager;
            _requestTranslator = requestTranslator;
            _httpExecutor = httpExecutor;
            _responseTranslator = responseTranslator;
        }

        // ProcessRequestAsync 方法保持不变
        public async Task<Result<UnifiedChatResponse>> ProcessRequestAsync(UnifiedChatRequest request, string providerId, CancellationToken cancellationToken)
        {
            // ... 现有实现不变 ...
            var configResult = _settingsManager.GetMergedConfig(providerId);
            if (!configResult.IsSuccess)
            {
                return Result<UnifiedChatResponse>.Failure(configResult.Error);
            }
            var config = configResult.Value;
            cancellationToken.ThrowIfCancellationRequested();
            var httpRequest = _requestTranslator.Translate(request, config);
            var httpResponseResult = await _httpExecutor.ExecuteAsync(httpRequest, cancellationToken);
            if (!httpResponseResult.IsSuccess)
            {
                 // 检查我们是否收到了一个失败的响应体，如果是，将其返回
                if(httpResponseResult.Value != null)
                {
                    var errorResponse = await _responseTranslator.TranslateAsync(httpResponseResult.Value, config, cancellationToken);
                     return Result<UnifiedChatResponse>.Failure(errorResponse.Message.Content, errorResponse);
                }
                return Result<UnifiedChatResponse>.Failure(httpResponseResult.Error);
            }
            var httpResponse = httpResponseResult.Value;
            var finalResponse = await _responseTranslator.TranslateAsync(httpResponse, config, cancellationToken);
            return Result<UnifiedChatResponse>.Success(finalResponse);
        }

        // 【新增方法】
        /// <summary>
        /// 使用并发控制来“批量”处理多个独立的聊天请求。
        /// </summary>
        /// <param name="requests">一个包含多个聊天请求的列表。</param>
        /// <param name="providerId">提供商的ID。</param>
        /// <param name="cancellationToken">用于中断所有并发操作的令牌。</param>
        /// <returns>一个包含了所有请求结果的列表。</returns>
        public async Task<List<Result<UnifiedChatResponse>>> ProcessBatchRequestAsync(List<UnifiedChatRequest> requests, string providerId, CancellationToken cancellationToken)
        {
            var configResult = _settingsManager.GetMergedConfig(providerId);
            if (!configResult.IsSuccess)
            {
                // 如果配置加载失败，则所有请求都失败。
                return requests.Select(r => Result<UnifiedChatResponse>.Failure(configResult.Error)).ToList();
            }
            var config = configResult.Value;

            // [C# 知识点] SemaphoreSlim 是一个轻量级的信号量，用于限制并发访问资源的线程数。
            // 我们用用户配置的并发数来初始化它。
            var semaphore = new SemaphoreSlim(config.User.ConcurrencyLimit);
            
            // 创建一个任务列表，用来存放所有即将并发执行的请求任务。
            var tasks = new List<Task<Result<UnifiedChatResponse>>>();

            foreach (var request in requests)
            {
                // 为每个请求创建一个独立的异步任务。
                tasks.Add(Task.Run(async () =>
                {
                    // 在任务开始执行前，必须先等待并获取一个信号量许可。
                    // 这就像在进入高速公路前，等待ETC抬杆。
                    await semaphore.WaitAsync(cancellationToken);
                    
                    try
                    {
                        // 一旦获得许可，就调用我们已经写好的单个请求处理方法。
                        return await ProcessRequestAsync(request, providerId, cancellationToken);
                    }
                    finally
                    {
                        // [关键] 无论任务是成功还是失败，都必须在 finally 块中释放信号量。
                        // 这就像离开高速公路时，把ETC通道还给后面的人用。
                        semaphore.Release();
                    }
                }, cancellationToken));
            }

            // Task.WhenAll 会等待列表中的所有任务完成，并返回它们的结果数组。
            var results = await Task.WhenAll(tasks);
            return results.ToList();
        }
    }
}