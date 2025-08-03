using RimAI.Framework.Core.Lifecycle;
using RimAI.Framework.Shared.Models;
using RimAI.Framework.Translation.Models;
using RimAI.Framework.UI;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Verse;

namespace RimAI.Framework.API
{
    /// <summary>
    /// RimAI Framework 的主要公共 API 静态类。
    /// V4.2: 重构为从 ModSettings 读取独立的 Chat 和 Embedding 提供商配置。
    /// </summary>
    public static class RimAIApi
    {
        private const string ChatNotActiveError = "聊天服务未激活。请在Mod设置中配置一个聊天服务提供商并填入有效的API Key。";
        private const string EmbeddingNotActiveError = "Embedding服务未激活。请在Mod设置中配置一个Embedding服务提供商并填入有效的API Key。";
        private const string ChatProviderNotSetError = "尚未设置默认的聊天服务提供商。请在Mod设置中选择并保存一个。";
        private const string EmbeddingProviderNotSetError = "尚未设置默认的Embedding服务提供商。请在Mod设置中选择并保存一个。";

        static RimAIApi()
        {
            FrameworkDI.Assemble();
        }

        /// <summary>
        /// 发送单个聊天请求，并异步获取模型的回复。
        /// 这是最核心、最灵活的聊天方法，所有其他聊天辅助方法最终都会调用它。
        /// </summary>
        /// <param name="request">一个包含了所有请求参数（如消息、工具、流式开关等）的统一聊天请求对象。</param>
        /// <param name="cancellationToken">用于取消该异步请求的令牌。</param>
        /// <returns>一个包含操作结果的 `Result` 对象。如果成功，其 `Value` 属性将是包含模型回复的 `UnifiedChatResponse`。</returns>
        public static async Task<Result<UnifiedChatResponse>> GetCompletionAsync(UnifiedChatRequest request, CancellationToken cancellationToken = default)
        {
            // 1. 检查 Chat 服务是否已激活
            if (!FrameworkDI.SettingsManager.IsChatActive)
                return Result<UnifiedChatResponse>.Failure(ChatNotActiveError);

            // 2. 获取设置，并读取【聊天】提供商ID
            var settings = LoadedModManager.GetMod<RimAIFrameworkMod>()?.GetSettings<RimAIFrameworkSettings>();
            if (string.IsNullOrEmpty(settings?.ActiveChatProviderId))
                return Result<UnifiedChatResponse>.Failure(ChatProviderNotSetError);

            // 3. 将请求和对应的提供商ID转发给 ChatManager
            return await FrameworkDI.ChatManager.ProcessRequestAsync(request, settings.ActiveChatProviderId, cancellationToken);
        }

        /// <summary>
        /// 发送单个 Embedding 请求，异步获取文本的向量表示。
        /// </summary>
        /// <param name="request">一个包含了所有输入文本的统一 Embedding 请求对象。</param>
        /// <param name="cancellationToken">用于取消该异步请求的令牌。</param>
        /// <returns>一个包含操作结果的 `Result` 对象。如果成功，其 `Value` 属性将是包含向量化结果的 `UnifiedEmbeddingResponse`。</returns>
        public static async Task<Result<UnifiedEmbeddingResponse>> GetEmbeddingsAsync(UnifiedEmbeddingRequest request, CancellationToken cancellationToken = default)
        {
            var settings = LoadedModManager.GetMod<RimAIFrameworkMod>()?.GetSettings<RimAIFrameworkSettings>();
            if (settings == null)
                return Result<UnifiedEmbeddingResponse>.Failure("Could not load RimAI Framework settings.");

            string providerIdToUse;

            // 1. 根据开关，决定使用哪个提供商ID
            if (settings.IsEmbeddingConfigEnabled)
            {
                // 如果启用了独立配置，则检查 Embedding 服务是否激活，并使用其专用ID
                if (!FrameworkDI.SettingsManager.IsEmbeddingActive)
                    return Result<UnifiedEmbeddingResponse>.Failure(EmbeddingNotActiveError);
                if (string.IsNullOrEmpty(settings.ActiveEmbeddingProviderId))
                    return Result<UnifiedEmbeddingResponse>.Failure(EmbeddingProviderNotSetError);
                
                providerIdToUse = settings.ActiveEmbeddingProviderId;
            }
            else
            {
                // 如果未启用独立配置，则回退使用 Chat 服务的配置
                if (!FrameworkDI.SettingsManager.IsChatActive)
                    return Result<UnifiedEmbeddingResponse>.Failure(ChatNotActiveError);
                if (string.IsNullOrEmpty(settings.ActiveChatProviderId))
                    return Result<UnifiedEmbeddingResponse>.Failure(ChatProviderNotSetError);

                providerIdToUse = settings.ActiveChatProviderId;
            }

            // 2. 将请求和最终决定的提供商ID转发给 EmbeddingManager
            return await FrameworkDI.EmbeddingManager.ProcessRequestAsync(request, providerIdToUse, cancellationToken);
        }
        
        /// <summary>
        /// 【批量处理】发送多个聊天请求，并以并发方式异步获取所有回复。
        /// 并发数量由用户在Mod设置中配置的 `concurrencyLimit` 决定。
        /// </summary>
        /// <param name="requests">包含多个聊天请求的列表。</param>
        /// <param name="cancellationToken">用于取消所有这些异步请求的令牌。</param>
        /// <returns>一个列表，其中每个元素都是对应请求的 `Result` 对象。</returns>
        public static async Task<List<Result<UnifiedChatResponse>>> GetCompletionsAsync(List<UnifiedChatRequest> requests, CancellationToken cancellationToken = default)
        {
            if (!FrameworkDI.SettingsManager.IsChatActive)
                return requests.Select(_ => Result<UnifiedChatResponse>.Failure(ChatNotActiveError)).ToList();

            var settings = LoadedModManager.GetMod<RimAIFrameworkMod>()?.GetSettings<RimAIFrameworkSettings>();
            if (string.IsNullOrEmpty(settings?.ActiveChatProviderId))
                return requests.Select(_ => Result<UnifiedChatResponse>.Failure(ChatProviderNotSetError)).ToList();

            return await FrameworkDI.ChatManager.ProcessBatchRequestAsync(requests, settings.ActiveChatProviderId, cancellationToken);
        }

        /// <summary>
        /// 【辅助方法】一个用于简化带有工具调用（Function Calling）的聊天请求的辅助方法。
        /// </summary>
        /// <param name="messages">对话历史消息列表。</param>
        /// <param name="tools">可供模型调用的工具列表。</param>
        /// <param name="stream">是否以流式方式获取响应。</param>
        /// <param name="cancellationToken">用于取消该异步请求的令牌。</param>
        /// <returns>一个包含模型回复的 `UnifiedChatResponse`，其中可能包含工具调用请求。</returns>
        public static Task<Result<UnifiedChatResponse>> GetCompletionWithToolsAsync(
            List<ChatMessage> messages,
            List<ToolDefinition> tools,
            bool stream = false,
            CancellationToken cancellationToken = default)
        {
            var request = new UnifiedChatRequest
            {
                Messages = messages,
                Tools = tools,
                Stream = stream
            };
            return GetCompletionAsync(request, cancellationToken);
        }
    }
}
