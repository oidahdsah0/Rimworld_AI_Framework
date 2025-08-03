// 引入 System 命名空间，因为我们会用到 TimeSpan
using System;

namespace RimAI.Framework.Execution.Models
{
    /// <summary>
    /// 定义一个网络请求的重试策略。
    /// 这个类是一个纯粹的数据容器，用于配置 HttpExecutor 的行为。
    /// </summary>
    public class RetryPolicy
    {
        /// <summary>
        /// 允许的最大重试次数。
        /// 例如，如果设置为 3，则最多会尝试 1 次初始请求 + 3 次重试，共 4 次。
        /// 默认值为 3。
        /// </summary>
        public int MaxRetries { get; set; } = 3;

        /// <summary>
        /// 第一次重试前的初始延迟时间。
        /// 使用 TimeSpan 来精确地表示时间间隔。
        /// 默认值为 200 毫秒。
        /// </summary>
        public TimeSpan InitialDelay { get; set; } = TimeSpan.FromMilliseconds(200);

        /// <summary>
        /// 是否使用指数退避策略。
        /// 如果为 true，每次重试的延迟时间将在上一次的基础上翻倍。
        /// (例如：200ms, 400ms, 800ms, ...)
        /// 如果为 false，每次重试都使用固定的 InitialDelay。
        /// 默认值为 true。
        /// </summary>
        public bool UseExponentialBackoff { get; set; } = true;
    }
}