using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace RimAI.Framework.Execution.Cache
{
    /// <summary>
    /// Simple thread-safe in-memory cache with absolute expiration.
    /// </summary>
    public class MemoryCacheService : ICacheService
    {
        private class CacheItem
        {
            public object Value { get; set; }
            public DateTimeOffset Expiration { get; set; }
        }

        private readonly ConcurrentDictionary<string, CacheItem> _store = new ConcurrentDictionary<string, CacheItem>(StringComparer.Ordinal);

        public Task<(bool hit, T value)> TryGetAsync<T>(string key, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (_store.TryGetValue(key, out var item))
            {
                if (item.Expiration > DateTimeOffset.UtcNow && item.Value is T typed)
                {
                    return Task.FromResult((true, typed));
                }
                // Expired or type mismatch -> evict
                _store.TryRemove(key, out _);
            }
            return Task.FromResult((false, default(T)));
        }

        public Task SetAsync<T>(string key, T value, TimeSpan ttl, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var item = new CacheItem
            {
                Value = value,
                Expiration = DateTimeOffset.UtcNow.Add(ttl)
            };
            _store[key] = item;
            return Task.CompletedTask;
        }

        public Task InvalidateByPrefixAsync(string prefix, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            foreach (var key in _store.Keys)
            {
                if (key.StartsWith(prefix, StringComparison.Ordinal))
                {
                    _store.TryRemove(key, out _);
                }
            }
            return Task.CompletedTask;
        }
    }
}


