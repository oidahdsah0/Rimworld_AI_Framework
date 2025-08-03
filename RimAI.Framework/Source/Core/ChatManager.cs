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

// 引入我们需要的各个组件的命名空间
using RimAI.Framework.Configuration;
using RimAI.Framework.Execution;
using RimAI.Framework.Shared.Models; // 为了使用 Result<T>
using RimAI.Framework.Translation;
using RimAI.Framework.Translation.Models;
using System.Threading.Tasks; // 为了使用 Task 和 async/await

namespace RimAI.Framework.Core
{
    /// <summary>
    /// 聊天功能总协调器。
    /// </summary>
    public class ChatManager
    {
        // [C# 知识点] "private readonly" 字段：
        //  - private: 表示这个字段只能在 ChatManager类的内部被访问。
        //  - readonly: 表示这个字段的值一旦在构造函数中被设定后，就不能再被修改。
        // 这是一种非常安全的实践，确保了 ChatManager 的“工具”不会在运行时被意外替换。
        private readonly SettingsManager _settingsManager;
        private readonly ChatRequestTranslator _requestTranslator;
        private readonly HttpExecutor _httpExecutor;
        private readonly ChatResponseTranslator _responseTranslator;

        /// <summary>
        /// ChatManager 的构造函数。
        /// 当创建 ChatManager 实例时，必须提供它所依赖的所有服务的实例。这就是“构造函数注入”。
        /// </summary>
        /// <param name="settingsManager">负责加载配置的服务。</param>
        /// <param name="requestTranslator">负责翻译请求的服务。</param>
        /// <param name="httpExecutor">负责执行 HTTP 通信的服务。</param>
        /// <param name="responseTranslator">负责翻译响应的服务。</param>
        public ChatManager(
            SettingsManager settingsManager,
            ChatRequestTranslator requestTranslator,
            HttpExecutor httpExecutor,
            ChatResponseTranslator responseTranslator)
        {
            // 将外部传入的服务实例，赋值给内部的私有只读字段。
            _settingsManager = settingsManager;
            _requestTranslator = requestTranslator;
            _httpExecutor = httpExecutor;
            _responseTranslator = responseTranslator;
        }

        /// <summary>
        /// 处理一个完整的聊天请求流程。
        /// </summary>
        /// <param name="request">统一的聊天请求。</param>
        /// <param name="providerId">提供商的唯一标识符，如 "openai"。</param>
        /// <returns>一个封装了成功时的 UnifiedChatResponse 或失败时的错误信息的 Result 对象。</returns>
        public async Task<Result<UnifiedChatResponse>> ProcessRequestAsync(UnifiedChatRequest request, string providerId)
        {
            // [步骤 1: 获取配置]
            // 调用 SettingsManager 获取该 providerId 对应的合并后配置。
            var configResult = _settingsManager.GetMergedConfig(providerId);
            if (!configResult.IsSuccess)
            {
                // 如果配置获取失败（比如找不到配置文件），则流程终止，返回一个失败的 Result。
                return Result<UnifiedChatResponse>.Failure(configResult.Error);
            }
            var config = configResult.Value;

            // [步骤 2: 翻译请求]
            // 使用请求翻译器，将内部统一模型翻译成一个 HTTP 请求。
            var httpRequest = _requestTranslator.Translate(request, config);

            // [步骤 3: 执行请求]
            // 将打包好的 HTTP 请求交给执行器去发送。
            var httpResponseResult = await _httpExecutor.ExecuteAsync(httpRequest);
            if (!httpResponseResult.IsSuccess)
            {
                // 如果 HTTP 请求失败（比如网络错误、API返回错误码），则流程终止。
                return Result<UnifiedChatResponse>.Failure(httpResponseResult.Error);
            }
            var httpResponse = httpResponseResult.Value;

            // [步骤 4: 翻译响应]
            // 将收到的原始 HTTP 响应，交给响应翻译器来解析成我们内部的统一模型。
            // 注意：这里我们假设 TranslateAsync 内部会处理所有解析错误，并直接返回 UnifiedChatResponse。
            // 在更复杂的实现中，这一步也可能返回一个 Result<T>。
            var finalResponse = await _responseTranslator.TranslateAsync(httpResponse, config);

            // [步骤 5: 返回成功结果]
            // 将最终的统一响应封装在一个成功的 Result 对象中并返回。
            return Result<UnifiedChatResponse>.Success(finalResponse);
        }
    }
}