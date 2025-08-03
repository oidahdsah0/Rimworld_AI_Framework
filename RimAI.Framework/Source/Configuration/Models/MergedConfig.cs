using System.Collections.Generic;
using Newtonsoft.Json.Linq; // 引入 JObject 和 JToken

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
        public ProviderTemplate Provider { get; internal set; }
        public UserConfig User { get; internal set; }

        public MergedConfig() {}

        // --- 统一配置访问属性 ---

        #region 通用配置 (General)

        public string ProviderName => Provider?.ProviderName;
        
        // API Key 永远只来自用户配置。
        public string ApiKey => User?.ApiKey;

        // 【新增】直接暴露 Provider 的 Http 配置
        public HttpConfig Http => Provider?.Http;

        // 【新增】直接暴露用户的 CustomHeaders
        public Dictionary<string, string> CustomHeaders => User?.CustomHeaders;

        #endregion

        #region 聊天API配置 (Chat API)
        
        // 【新增】直接暴露 ChatApi，方便访问其所有子属性
        public ChatApiConfig ChatApi => Provider?.ChatApi;
        
        // 【修正】提供合并后的 ChatEndpoint 和 ChatModel
        public string ChatEndpoint => User?.ChatEndpointOverride ?? Provider?.ChatApi?.Endpoint;
        public string ChatModel => User?.ChatModelOverride ?? Provider?.ChatApi?.DefaultModel;

        public float? Temperature => User?.Temperature ?? Provider?.ChatApi?.DefaultParameters?["temperature"]?.ToObject<float?>();
        public float? TopP => User?.TopP ?? Provider?.ChatApi?.DefaultParameters?["top_p"]?.ToObject<float?>();
        
        public int ConcurrencyLimit => User?.ConcurrencyLimit ?? 4; 

        #endregion

        #region 嵌入API配置 (Embedding API)

        // 【新增】直接暴露 EmbeddingApi
        public EmbeddingApiConfig EmbeddingApi => Provider?.EmbeddingApi;

        // 【修正】提供合并后的 EmbeddingEndpoint 和 EmbeddingModel
        public string EmbeddingEndpoint => User?.EmbeddingEndpointOverride ?? Provider?.EmbeddingApi?.Endpoint;
        public string EmbeddingModel => User?.EmbeddingModelOverride ?? Provider?.EmbeddingApi?.DefaultModel;
        
        #endregion

        #region 静态参数 (Static Parameters)

        // 【新增】直接暴露合并后的 JObject
        public JObject StaticParameters
        {
            get
            {
                // 从 JObject 创建副本，以避免修改原始模板
                var baseParams = Provider?.StaticParameters != null
                    ? new JObject(Provider.StaticParameters)
                    : new JObject();

                var overrideParams = User?.StaticParametersOverride;

                if (overrideParams != null)
                {
                    // Merge 会将 overrideParams 的内容深度合并到 baseParams 中
                    baseParams.Merge(overrideParams, new JsonMergeSettings
                    {
                        // 如果属性已存在，则替换它
                        MergeArrayHandling = MergeArrayHandling.Replace
                    });
                }

                return baseParams;
            }
        }

        #endregion
    }
}