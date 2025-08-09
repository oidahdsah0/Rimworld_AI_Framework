using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;

namespace RimAI.Framework.Execution.Cache
{
    /// <summary>
    /// Basic in-flight de-duplication: coalesces concurrent requests for the same key.
    /// </summary>
    public class InFlightCoordinator : IInFlightCoordinator
    {
        private readonly ConcurrentDictionary<string, Lazy<Task<object>>> _inFlight = new ConcurrentDictionary<string, Lazy<Task<object>>>(StringComparer.Ordinal);

        public async Task<T> GetOrJoinAsync<T>(string key, Func<Task<T>> factory)
        {
            // Wrap factory in Lazy<Task<object>> so only one instance runs
            var lazy = _inFlight.GetOrAdd(key, _ => new Lazy<Task<object>>(async () => (object)await factory().ConfigureAwait(false)));
            try
            {
                var result = await lazy.Value.ConfigureAwait(false);
                return (T)result;
            }
            finally
            {
                _inFlight.TryRemove(key, out _);
            }
        }
    }
}


