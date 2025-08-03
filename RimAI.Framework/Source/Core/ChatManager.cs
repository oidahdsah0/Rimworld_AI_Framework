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
        // ... (成员变量和构造函数保持不变) ...
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

        public async Task<Result<UnifiedChatResponse>> ProcessRequestAsync(UnifiedChatRequest request, string providerId, CancellationToken cancellationToken)
        {
            var configResult = _settingsManager.GetMergedConfig(providerId);
            if (!configResult.IsSuccess)
            {
                return Result<UnifiedChatResponse>.Failure(configResult.Error);
            }
            var config = configResult.Value;
            cancellationToken.ThrowIfCancellationRequested();
            
            var httpRequest = _requestTranslator.Translate(request, config);

            // 【逻辑修正】HttpExecutor 返回的 Result<T> 需要被完整处理
            var httpResult = await _httpExecutor.ExecuteAsync(httpRequest, cancellationToken);
            if (httpResult.IsFailure)
            {
                // 如果是网络层面的失败 (e.g., 请求被取消, DNS错误), 直接返回失败
                return Result<UnifiedChatResponse>.Failure(httpResult.Error);
            }

            var httpResponse = httpResult.Value;
            // 如果 HTTP 请求成功发出了，但服务器返回了非 2xx 的状态码
            if (!httpResponse.IsSuccessStatusCode)
            {
                // 我们尝试解析这个失败的响应体，因为它可能包含有价值的错误信息
                var errorResponse = await _responseTranslator.TranslateAsync(httpResponse, config, cancellationToken);
                var errorMessage = errorResponse?.Message?.Content ?? $"Request failed with status code {httpResponse.StatusCode}";
                
                // 【调用修正】现在这个调用是正确的，因为它匹配我们新增的 Failure 重载
                return Result<UnifiedChatResponse>.Failure(errorMessage, errorResponse);
            }
            
            var finalResponse = await _responseTranslator.TranslateAsync(httpResponse, config, cancellationToken);
            return Result<UnifiedChatResponse>.Success(finalResponse);
        }

        public async Task<List<Result<UnifiedChatResponse>>> ProcessBatchRequestAsync(List<UnifiedChatRequest> requests, string providerId, CancellationToken cancellationToken)
        {
            var configResult = _settingsManager.GetMergedConfig(providerId);
            if (!configResult.IsSuccess)
            {
                return requests.Select(r => Result<UnifiedChatResponse>.Failure(configResult.Error)).ToList();
            }
            var config = configResult.Value;

            // 【访问修正】从 MergedConfig 的根级属性获取 ConcurrencyLimit
            var semaphore = new SemaphoreSlim(config.ConcurrencyLimit);
            
            var tasks = new List<Task<Result<UnifiedChatResponse>>>();

            foreach (var request in requests)
            {
                tasks.Add(Task.Run(async () =>
                {
                    await semaphore.WaitAsync(cancellationToken);
                    try
                    {
                        return await ProcessRequestAsync(request, providerId, cancellationToken);
                    }
                    finally
                    {
                        semaphore.Release();
                    }
                }, cancellationToken));
            }

            var results = await Task.WhenAll(tasks);
            return results.ToList();
        }
    }
}