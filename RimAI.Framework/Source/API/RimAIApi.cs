using RimAI.Framework.Core.Lifecycle;
using RimAI.Framework.Shared.Models;
using RimAI.Framework.Translation.Models;
using RimAI.Framework.UI;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Verse;
using RimWorld; // 【修复】添加 RimWorld 引用

namespace RimAI.Framework.API
{
    public static class RimAIApi
    {
        private const string FrameworkNotActiveError = "RimAI Framework is not active. Please configure at least one provider with a valid API Key in the mod settings.";
        private const string DefaultProviderNotSetError = "No default provider is set in RimAI Framework settings. Please select and save a provider.";

        static RimAIApi()
        {
            FrameworkDI.Assemble();
        }

        public static async Task<Result<UnifiedChatResponse>> GetCompletionAsync(UnifiedChatRequest request, CancellationToken cancellationToken = default)
        {
            if (!FrameworkDI.SettingsManager.IsActive)
                return Result<UnifiedChatResponse>.Failure(FrameworkNotActiveError);

            // 【修复】使用正确的 RimWorld API: LoadedModManager.GetMod()
            var settings = LoadedModManager.GetMod<RimAIFrameworkMod>()?.GetSettings<RimAIFrameworkSettings>();
            if (string.IsNullOrEmpty(settings?.ActiveProviderId))
                return Result<UnifiedChatResponse>.Failure(DefaultProviderNotSetError);

            return await FrameworkDI.ChatManager.ProcessRequestAsync(request, settings.ActiveProviderId, cancellationToken);
        }

        public static async Task<Result<UnifiedEmbeddingResponse>> GetEmbeddingsAsync(UnifiedEmbeddingRequest request, CancellationToken cancellationToken = default)
        {
            if (!FrameworkDI.SettingsManager.IsActive)
                return Result<UnifiedEmbeddingResponse>.Failure(FrameworkNotActiveError);
            
            var settings = LoadedModManager.GetMod<RimAIFrameworkMod>()?.GetSettings<RimAIFrameworkSettings>();
            if (string.IsNullOrEmpty(settings?.ActiveProviderId))
                return Result<UnifiedEmbeddingResponse>.Failure(DefaultProviderNotSetError);
            
            return await FrameworkDI.EmbeddingManager.ProcessRequestAsync(request, settings.ActiveProviderId, cancellationToken);
        }
        
        public static async Task<List<Result<UnifiedChatResponse>>> GetCompletionsAsync(List<UnifiedChatRequest> requests, CancellationToken cancellationToken = default)
        {
            if (!FrameworkDI.SettingsManager.IsActive)
                return requests.Select(_ => Result<UnifiedChatResponse>.Failure(FrameworkNotActiveError)).ToList();

            var settings = LoadedModManager.GetMod<RimAIFrameworkMod>()?.GetSettings<RimAIFrameworkSettings>();
            if (string.IsNullOrEmpty(settings?.ActiveProviderId))
                return requests.Select(_ => Result<UnifiedChatResponse>.Failure(DefaultProviderNotSetError)).ToList();

            return await FrameworkDI.ChatManager.ProcessBatchRequestAsync(requests, settings.ActiveProviderId, cancellationToken);
        }
    }
}