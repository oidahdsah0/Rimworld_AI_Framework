// =====================================================================================================================
// 文件: RimAIApi.cs
//
// 作用:
//  这是 RimAI.Framework 的公共 API 门面 (Facade)。
//  它是其他 Mod 与本框架交互的【唯一】入口点。
//
// 设计模式:
//  - 门面模式 (Facade Pattern): 提供一个简化的、统一的接口，隐藏内部子系统的复杂性。
//  - 静态类 (Static Class): 所有方法都通过类名直接调用，方便使用。
//
// 核心职责:
//  1. 【安全】封装内部实现，不允许上游 Mod 指定或修改 API 供应商。
//  2. 在首次被调用时，通过静态构造函数触发框架的初始化 (FrameworkDI.Assemble())。
//  3. 提供一组稳定、公开、有良好文档的静态方法，供外部调用。
//  4. 将外部调用请求，根据用户在 Framework 中配置的【默认供应商】，转发给内部对应的 Manager 处理。
// =====================================================================================================================

using RimAI.Framework.Core.Lifecycle;
using RimAI.Framework.Shared.Models;
using RimAI.Framework.Translation.Models;
using System.Collections.Generic; // [新增] 用于 List<T>
using System.Linq; // [新增] 用于 .Select()
using System.Threading;
using System.Threading.Tasks;

namespace RimAI.Framework.API
{
    /// <summary>
    /// RimAI Framework 的公共静态 API。
    /// 其他 Mod 应通过此类与框架交互。
    /// </summary>
    public static class RimAIApi
    {
        /// <summary>
        /// 静态构造函数，在 RimAIApi 类首次被访问时自动执行一次。
        /// </summary>
        static RimAIApi()
        {
            // 在任何 API 方法被调用之前，首先确保我们的 DI 容器已经完成了所有服务的组装。
            // 这就是我们框架的“点火”开关。
            FrameworkDI.Assemble();
        }

        /// <summary>
        /// 【公共API】获取单个聊天补全。
        /// 此方法会自动使用用户在 Framework 设置中配置的默认聊天提供商。
        /// </summary>
        /// <param name="request">一个统一聊天请求对象。</param>
        /// <param name="cancellationToken">【新增】一个可选的取消令牌，用于在需要时中断请求。</param>
        /// <returns>一个包含 UnifiedChatResponse 或错误的异步 Result。</returns>
        public static async Task<Result<UnifiedChatResponse>> GetCompletionAsync(UnifiedChatRequest request, CancellationToken cancellationToken = default)
        {
            var settingsManager = FrameworkDI.SettingsManager;
            string providerId = settingsManager.GetDefaultChatProviderId();

            if (string.IsNullOrEmpty(providerId))
            {
                return Result<UnifiedChatResponse>.Failure("No default chat provider is configured in RimAI.Framework settings.");
            }

            return await FrameworkDI.ChatManager.ProcessRequestAsync(request, providerId, cancellationToken);
        }

        /// <summary>
        /// 【公共API】获取单个或多个文本的 Embedding。
        /// 此方法会自动使用用户在 Framework 设置中配置的默认 Embedding 提供商。
        /// </summary>
        /// <param name="request">一个统一 Embedding 请求对象。</param>
        /// <param name="cancellationToken">【新增】一个可选的取消令牌，用于在需要时中断请求。</param>
        /// <returns>一个包含 UnifiedEmbeddingResponse 或错误的异步 Result。</returns>
        public static async Task<Result<UnifiedEmbeddingResponse>> GetEmbeddingsAsync(UnifiedEmbeddingRequest request, CancellationToken cancellationToken = default)
        {
            var settingsManager = FrameworkDI.SettingsManager;
            string providerId = settingsManager.GetDefaultEmbeddingProviderId();

            if (string.IsNullOrEmpty(providerId))
            {
                return Result<UnifiedEmbeddingResponse>.Failure("No default embedding provider is configured in RimAI.Framework settings.");
            }
            
            return await FrameworkDI.EmbeddingManager.ProcessRequestAsync(request, providerId, cancellationToken);
        }
        
        // 【新增公共API】
        /// <summary>
        /// 【公共API】并发处理多个聊天请求。
        /// 并发数由用户在 Framework 设置中配置 (concurrencyLimit)。
        /// </summary>
        /// <param name="requests">一个包含多个聊天请求的列表。</param>
        /// <param name="cancellationToken">一个可选的取消令牌，用于同时中断所有正在进行的请求。</param>
        /// <returns>一个包含了每个请求的异步 Result 的列表，其顺序与输入列表一致。</returns>
        public static async Task<List<Result<UnifiedChatResponse>>> GetCompletionsAsync(List<UnifiedChatRequest> requests, CancellationToken cancellationToken = default)
        {
            var settingsManager = FrameworkDI.SettingsManager;
            string providerId = settingsManager.GetDefaultChatProviderId();

            // 如果连默认提供商都没有配置，则直接为所有请求返回配置错误。
            if (string.IsNullOrEmpty(providerId))
            {
                var errorResult = Result<UnifiedChatResponse>.Failure("No default chat provider is configured in RimAI.Framework settings.");
                // 使用 LINQ 快速生成一个所有元素都为 errorResult 的列表。
                return requests.Select(_ => errorResult).ToList();
            }

            // 将调用请求转发给 ChatManager 的批量处理方法。
            return await FrameworkDI.ChatManager.ProcessBatchRequestAsync(requests, providerId, cancellationToken);
        }
    }
}