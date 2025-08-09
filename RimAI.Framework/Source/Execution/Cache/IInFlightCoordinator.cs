using System;
using System.Threading.Tasks;

namespace RimAI.Framework.Execution.Cache
{
    /// <summary>
    /// Coordinates concurrent requests with the same key to avoid duplicate upstream calls.
    /// </summary>
    public interface IInFlightCoordinator
    {
        /// <summary>
        /// Join or start a single upstream task for the specified key.
        /// </summary>
        Task<T> GetOrJoinAsync<T>(string key, Func<Task<T>> factory);
    }
}


