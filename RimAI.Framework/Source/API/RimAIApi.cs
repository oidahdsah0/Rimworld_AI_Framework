using RimAI.Framework.Core.Lifecycle;
using RimAI.Framework.Contracts;
using RimAI.Framework.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Verse;

namespace RimAI.Framework.API
{
    /// <summary>
    /// RimAI Framework 的主要公共 API 静态类。
    /// V4.4: 为所有公共异步方法添加了取消异常的捕获，使其更加健壮。
    /// </summary>
    public static class RimAIApi
    {
        private const string ChatNotActiveError = "Chat service is not active. Please configure a chat provider with a valid API Key in Mod settings.";
        private const string EmbeddingNotActiveError = "Embedding service is not active. Please configure an embedding provider with a valid API Key in Mod settings.";
        private const string ChatProviderNotSetError = "No default chat provider is set. Please select and save one in Mod settings.";
        private const string EmbeddingProviderNotSetError = "No default embedding provider is set. Please select and save one in Mod settings.";
        private const string CancellationError = "Request was cancelled by the user.";

        static RimAIApi()
        {
            FrameworkDI.Assemble();
        }

        /// <summary>
        /// 【流式】发送单个聊天请求，并以异步流的方式获取模型的回复数据块 (Chunks)。
        /// </summary>
        public static async IAsyncEnumerable<Result<UnifiedChatChunk>> StreamCompletionAsync(UnifiedChatRequest request, [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            if (!FrameworkDI.SettingsManager.IsChatActive)
            {
                yield return Result<UnifiedChatChunk>.Failure(ChatNotActiveError);
                yield break;
            }

            var settings = LoadedModManager.GetMod<RimAIFrameworkMod>()?.GetSettings<RimAIFrameworkSettings>();
            if (string.IsNullOrEmpty(settings?.ActiveChatProviderId))
            {
                yield return Result<UnifiedChatChunk>.Failure(ChatProviderNotSetError);
                yield break;
            }

            request.Stream = true;

            // 【最终修复 CS1626/CS1631】: 使用状态标志将 yield return 完全移出 try-catch 块
            var stream = FrameworkDI.ChatManager.ProcessStreamRequestAsync(request, settings.ActiveChatProviderId, cancellationToken);
            await using var enumerator = stream.GetAsyncEnumerator(cancellationToken);
            
            bool cancelled = false;
            while (true)
            {
                bool hasNext;
                try
                {
                    hasNext = await enumerator.MoveNextAsync();
                }
                catch (OperationCanceledException)
                {
                    cancelled = true;
                    break;
                }

                if (!hasNext)
                {
                    break;
                }

                yield return enumerator.Current;
            }

            if (cancelled)
            {
                yield return Result<UnifiedChatChunk>.Failure(CancellationError);
            }
        }

        /// <summary>
        /// (非流式) 发送单个聊天请求，并异步获取模型的完整回复。
        /// </summary>
        public static async Task<Result<UnifiedChatResponse>> GetCompletionAsync(UnifiedChatRequest request, CancellationToken cancellationToken = default)
        {
            if (!FrameworkDI.SettingsManager.IsChatActive)
                return Result<UnifiedChatResponse>.Failure(ChatNotActiveError);

            var settings = LoadedModManager.GetMod<RimAIFrameworkMod>()?.GetSettings<RimAIFrameworkSettings>();
            if (string.IsNullOrEmpty(settings?.ActiveChatProviderId))
                return Result<UnifiedChatResponse>.Failure(ChatProviderNotSetError);

            request.Stream = false;

            try
            {
                return await FrameworkDI.ChatManager.ProcessRequestAsync(request, settings.ActiveChatProviderId, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                return Result<UnifiedChatResponse>.Failure(CancellationError);
            }
        }

        /// <summary>
        /// (非流式) 发送单个 Embedding 请求，异步获取文本的向量表示。
        /// </summary>
        public static async Task<Result<UnifiedEmbeddingResponse>> GetEmbeddingsAsync(UnifiedEmbeddingRequest request, CancellationToken cancellationToken = default)
        {
            var settings = LoadedModManager.GetMod<RimAIFrameworkMod>()?.GetSettings<RimAIFrameworkSettings>();
            if (settings == null)
                return Result<UnifiedEmbeddingResponse>.Failure("Could not load RimAI Framework settings.");

            string providerIdToUse;
            if (settings.IsEmbeddingConfigEnabled)
            {
                if (!FrameworkDI.SettingsManager.IsEmbeddingActive) return Result<UnifiedEmbeddingResponse>.Failure(EmbeddingNotActiveError);
                if (string.IsNullOrEmpty(settings.ActiveEmbeddingProviderId)) return Result<UnifiedEmbeddingResponse>.Failure(EmbeddingProviderNotSetError);
                providerIdToUse = settings.ActiveEmbeddingProviderId;
            }
            else
            {
                if (!FrameworkDI.SettingsManager.IsChatActive) return Result<UnifiedEmbeddingResponse>.Failure(ChatNotActiveError);
                if (string.IsNullOrEmpty(settings.ActiveChatProviderId)) return Result<UnifiedEmbeddingResponse>.Failure(ChatProviderNotSetError);
                providerIdToUse = settings.ActiveChatProviderId;
            }

            try
            {
                return await FrameworkDI.EmbeddingManager.ProcessRequestAsync(request, providerIdToUse, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                return Result<UnifiedEmbeddingResponse>.Failure(CancellationError);
            }
        }
        
        /// <summary>
        /// 【批量处理】发送多个聊天请求，并以并发方式异步获取所有完整回复。
        /// </summary>
        public static async Task<List<Result<UnifiedChatResponse>>> GetCompletionsAsync(List<UnifiedChatRequest> requests, CancellationToken cancellationToken = default)
        {
            if (!FrameworkDI.SettingsManager.IsChatActive)
                return requests.Select(_ => Result<UnifiedChatResponse>.Failure(ChatNotActiveError)).ToList();

            var settings = LoadedModManager.GetMod<RimAIFrameworkMod>()?.GetSettings<RimAIFrameworkSettings>();
            if (string.IsNullOrEmpty(settings?.ActiveChatProviderId))
                return requests.Select(_ => Result<UnifiedChatResponse>.Failure(ChatProviderNotSetError)).ToList();

            try
            {
                return await FrameworkDI.ChatManager.ProcessBatchRequestAsync(requests, settings.ActiveChatProviderId, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                return requests.Select(_ => Result<UnifiedChatResponse>.Failure(CancellationError)).ToList();
            }
        }

        /// <summary>
        /// 【辅助方法】一个用于简化带有工具调用（Function Calling）的聊天请求的辅助方法 (非流式)。
        /// </summary>
        public static Task<Result<UnifiedChatResponse>> GetCompletionWithToolsAsync(
            List<ChatMessage> messages,
            List<ToolDefinition> tools,
            CancellationToken cancellationToken = default)
        {
            var request = new UnifiedChatRequest
            {
                Messages = messages,
                Tools = tools,
                Stream = false
            };
            return GetCompletionAsync(request, cancellationToken);
        }
    }
}
