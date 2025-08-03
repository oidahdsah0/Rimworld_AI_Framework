using System.Net.Http;

namespace RimAI.Framework.Execution
{
    /// <summary>
    /// 负责创建和管理一个全局共享的 HttpClient 实例。
    /// 遵循.NET最佳实践，避免因不当使用 HttpClient 导致的套接字耗尽问题。
    /// </summary>
    public static class HttpClientFactory
    {
        // --- 静态成员变量 ---

        // 'static' 关键字确保 _client 实例在整个应用程序域中是唯一的。
        // 'readonly' 关键字确保一旦 _client 被赋值后，就不能再被改变。
        private static readonly HttpClient _client;

        // --- 静态构造函数 ---

        /// <summary>
        /// 静态构造函数。它在类的任何静态成员被首次访问之前，由.NET运行时自动调用，且只调用一次。
        /// 这是初始化静态资源的完美地点。
        /// </summary>
        static HttpClientFactory()
        {
            // 在这里，我们创建了那个唯一的 HttpClient 实例。
            // 未来如果需要对客户端进行全局配置（比如设置默认的超时时间），
            // 就可以在这里进行。
            _client = new HttpClient();
        }

        // --- 公共静态方法 ---

        /// <summary>
        /// 获取全局共享的 HttpClient 实例。
        /// 框架中的任何需要发起HTTP请求的部分，都应该通过这个方法来获取客户端。
        /// </summary>
        /// <returns>全局共享的 HttpClient 实例。</returns>
        public static HttpClient GetClient()
        {
            return _client;
        }
    }
}