using System.Collections.Generic;
using System.Linq; // 我们需要 Linq 来检查字典是否为空

namespace RimAI.Framework.Configuration.Models
{
    /// <summary>
    /// 一个“智能”配置对象，它封装了 ProviderTemplate 和 UserConfig，
    /// 并通过只读的计算属性提供了统一的、合并后的配置值。
    /// 调用者无需关心一个值是来自用户设置还是模板默认值。
    /// </summary>
    public class MergedConfig
    {
        // --- 基础数据源 ---
        // internal set 意味着只能在 RimAI.Framework 这个项目内部进行赋值。
        // SettingsManager 可以通过对象初始化器来赋值，但框架外部的调用者不能修改。
        public ProviderTemplate Provider { get; internal set; }
        public UserConfig User { get; internal set; }

        // --- 构造函数 ---
        // 使用无参构造函数，方便 SettingsManager 使用对象初始化器进行创建。
        public MergedConfig() {}

        // --- 统一配置访问属性 (只读计算属性) ---

        #region 通用配置 (General)

        public string ProviderName => Provider?.ProviderName;
        
        // API Key 永远只来自用户配置。
        public string ApiKey => User?.ApiKey;

        // 获取合并后的 HTTP Headers。
        // 用户的自定义 Headers 会覆盖模板中预设的同名 Header。
        public Dictionary<string, string> GetMergedHeaders()
        {
            var providerHeaders = Provider?.Http?.Headers ?? new Dictionary<string, string>();
            var userHeaders = User?.CustomHeaders ?? new Dictionary<string, string>();

            // 以模板的 Headers 为基础，用用户的 Headers 进行覆盖。
            var merged = new Dictionary<string, string>(providerHeaders);
            foreach (var header in userHeaders)
            {
                merged[header.Key] = header.Value;
            }
            return merged;
        }

        #endregion

        #region 聊天API配置 (Chat API)

        // 优先使用用户的 Endpoint，否则使用模板的。
        public string ChatEndpoint => User?.ChatEndpointOverride ?? Provider?.ChatApi?.Endpoint;
        public string ChatModel => User?.ChatModelOverride ?? Provider?.ChatApi?.DefaultModel;

        // 优先使用用户的 Temperature，否则使用模板的。
        // ?? (空合并运算符) 在这里完美地实现了我们的逻辑。
        public float? Temperature => User?.Temperature ?? (float?)Provider?.ChatApi?.DefaultParameters?["temperature"];

        // TopP同理。注意我们从字典取值后需要做类型转换。
        public float? TopP => User?.TopP ?? (float?)Provider?.ChatApi?.DefaultParameters?["top_p"];

        // 并发数限制。如果用户没设置，我们提供一个框架级的默认值 4。
        public int ConcurrencyLimit => User?.ConcurrencyLimit ?? 4; 

        // Chat相关的路径配置，直接从Provider模板获取
        public ChatRequestPaths ChatRequestPaths => Provider?.ChatApi?.RequestPaths;
        public ChatResponsePaths ChatResponsePaths => Provider?.ChatApi?.ResponsePaths;
        public ToolPaths ToolPaths => Provider?.ChatApi?.ToolPaths;
        public JsonModeConfig JsonMode => Provider?.ChatApi?.JsonMode;

        #endregion

        #region 嵌入API配置 (Embedding API)

        public string EmbeddingEndpoint => User?.EmbeddingEndpointOverride ?? Provider?.EmbeddingApi?.Endpoint;
        public string EmbeddingModel => User?.EmbeddingModelOverride ?? Provider?.EmbeddingApi?.DefaultModel;
        
        // 最大批量处理数，直接来自模板定义。如果模板没定义，我们给一个安全的默认值。
        public int EmbeddingMaxBatchSize => Provider?.EmbeddingApi?.MaxBatchSize ?? 1; 

        // Embedding相关的路径配置
        public EmbeddingRequestPaths EmbeddingRequestPaths => Provider?.EmbeddingApi?.RequestPaths;
        public EmbeddingResponsePaths EmbeddingResponsePaths => Provider?.EmbeddingApi?.ResponsePaths;
        
        #endregion

        #region 静态参数 (Static Parameters)

        /// <summary>
        /// 获取深度合并后的静态参数。
        /// 用户的 StaticParametersOverride 会覆盖模板中同名的 StaticParameters。
        /// </summary>
        /// <returns>一个包含所有合并后静态参数的字典。</returns>
        public Dictionary<string, object> GetMergedStaticParameters()
        {
            var baseParams = Provider?.StaticParameters ?? new Dictionary<string, object>();
            var overrideParams = User?.StaticParametersOverride ?? new Dictionary<string, object>();
            
            // 这里需要一个能够深度合并字典的逻辑。为保持简单，我们暂时用简单的覆盖逻辑。
            // 一个真正的深度合并需要递归处理嵌套的字典。
            // 但对于大多数情况，直接覆盖已经够用。
            var merged = new Dictionary<string, object>(baseParams);
            foreach (var param in overrideParams)
            {
                merged[param.Key] = param.Value;
            }
            return merged;
        }

        #endregion
    }
}