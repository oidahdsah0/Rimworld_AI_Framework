using System;
using System.Threading;
using System.Threading.Tasks;

namespace RimAI.Framework.Execution.Cache
{
    /// <summary>
    /// Unified cache interface for the framework. Provides basic get/set with TTL and namespace invalidation.
    /// </summary>
    public interface ICacheService
    {
        /// <summary>
        /// Try to get a cached value by key.
        /// </summary>
        Task<(bool hit, T value)> TryGetAsync<T>(string key, CancellationToken cancellationToken = default);

        /// <summary>
        /// Set a cached value with TTL.
        /// </summary>
        Task SetAsync<T>(string key, T value, TimeSpan ttl, CancellationToken cancellationToken = default);

        /// <summary>
        /// Invalidate all keys that start with the specified prefix (namespace).
        /// </summary>
        Task InvalidateByPrefixAsync(string prefix, CancellationToken cancellationToken = default);
    }
}


