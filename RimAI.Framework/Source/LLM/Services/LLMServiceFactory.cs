using System.Net.Http;
using RimAI.Framework.Core;
using RimAI.Framework.LLM.RequestQueue;
using RimAI.Framework.LLM.Services;

namespace RimAI.Framework.LLM.Services
{
    /// <summary>
    /// Factory class responsible for creating LLM service instances with proper dependencies
    /// </summary>
    public class LLMServiceFactory
    {
        private readonly HttpClient _httpClient;
        private readonly RimAISettings _settings;

        public LLMServiceFactory(HttpClient httpClient, RimAISettings settings)
        {
            _httpClient = httpClient;
            _settings = settings;
        }

        /// <summary>
        /// Creates an LLM executor for handling HTTP communication
        /// </summary>
        public ILLMExecutor CreateExecutor()
        {
            return new LLMExecutor(_httpClient, _settings);
        }

        /// <summary>
        /// Creates a request queue with the specified executor
        /// </summary>
        /// <param name="executor">The executor to use for processing requests</param>
        /// <param name="maxConcurrentRequests">Maximum number of concurrent requests (default: 3)</param>
        public LLMRequestQueue CreateRequestQueue(ILLMExecutor executor, int maxConcurrentRequests = 3)
        {
            return new LLMRequestQueue(executor, maxConcurrentRequests);
        }

        /// <summary>
        /// Creates a custom LLM service for advanced API usage
        /// </summary>
        public ICustomLLMService CreateCustomService()
        {
            return new CustomLLMService(_httpClient, _settings.apiKey, _settings.apiEndpoint);
        }

        /// <summary>
        /// Creates a JSON-enforced LLM service
        /// </summary>
        /// <param name="executor">The executor to use for requests</param>
        public IJsonLLMService CreateJsonService(ILLMExecutor executor)
        {
            return new JsonLLMService(executor, _settings);
        }

        /// <summary>
        /// Creates a Mod service for enhanced mod integration
        /// </summary>
        /// <param name="executor">The executor to use for requests</param>
        public IModService CreateModService(ILLMExecutor executor)
        {
            return new ModService(executor, _settings);
        }
    }
}
