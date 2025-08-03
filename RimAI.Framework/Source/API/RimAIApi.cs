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
    /// V4.2: 重构为从 ModSettings 读取独立的 Chat 和 Embedding 提供商配置。
    /// </summary>
    public static class RimAIApi
    {
        private const string ChatNotActiveError = "Chat service is not active. Please configure a chat provider with a valid API Key.";
        private const string EmbeddingNotActiveError = "Embedding service is not active. Please configure an embedding provider with a valid API Key.";
        private const string ChatProviderNotSetError = "No default chat provider is set. Please select and save one in settings.";
        private const string EmbeddingProviderNotSetError = "No default embedding provider is set. Please select and save one in settings.";

        static RimAIApi()
        {
            FrameworkDI.Assemble();
        }

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
        
        public static async Task<List<Result<UnifiedChatResponse>>> GetCompletionsAsync(List<UnifiedChatRequest> requests, CancellationToken cancellationToken = default)
        {
            if (!FrameworkDI.SettingsManager.IsChatActive)
                return requests.Select(_ => Result<UnifiedChatResponse>.Failure(ChatNotActiveError)).ToList();

            var settings = LoadedModManager.GetMod<RimAIFrameworkMod>()?.GetSettings<RimAIFrameworkSettings>();
            if (string.IsNullOrEmpty(settings?.ActiveChatProviderId))
                return requests.Select(_ => Result<UnifiedChatResponse>.Failure(ChatProviderNotSetError)).ToList();

            return await FrameworkDI.ChatManager.ProcessBatchRequestAsync(requests, settings.ActiveChatProviderId, cancellationToken);
        }
    }
}