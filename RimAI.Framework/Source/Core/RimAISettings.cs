using System;
using Verse;

namespace RimAI.Framework.Core
{
    /// <summary>
    /// Holds the settings for the RimAI Framework.
    /// This class is responsible for storing and exposing data to be saved by RimWorld.
    /// </summary>
    public class RimAISettings : ModSettings
    {
        /// <summary>
        /// The API key for the LLM service.
        /// </summary>
        public string apiKey = "";

        /// <summary>
        /// The base API URL for the LLM service (up to v1, without trailing slash).
        /// The system will automatically append '/chat/completions' for chat requests.
        /// </summary>
        public string apiEndpoint = "https://api.openai.com/v1";

        /// <summary>
        /// The model name for chat completions.
        /// </summary>
        public string modelName = "gpt-4o";

        /// <summary>
        /// Whether to enable streaming responses.
        /// </summary>
        public bool enableStreaming = false;

        /// <summary>
        /// Temperature parameter for controlling AI response creativity.
        /// Range: 0.0 (deterministic) to 2.0 (very random). Recommended: 0.0-1.0.
        /// </summary>
        public float temperature = 0.7f;

        /// <summary>
        /// Maximum tokens in response. Higher values allow longer responses but consume more resources.
        /// </summary>
        public int maxTokens = 1000;

        /// <summary>
        /// Request timeout in seconds.
        /// </summary>
        public int timeoutSeconds = 30;

        /// <summary>
        /// Number of retry attempts on failure.
        /// </summary>
        public int retryCount = 3;

        /// <summary>
        /// Enable response caching for improved performance.
        /// </summary>
        public bool enableCaching = true;

        /// <summary>
        /// Cache size limit (number of cached responses).
        /// </summary>
        public int cacheSize = 1000;

        /// <summary>
        /// Cache TTL (time to live) in minutes.
        /// </summary>
        public int cacheTtlMinutes = 30;

        /// <summary>
        /// Maximum concurrent requests allowed.
        /// </summary>
        public int maxConcurrentRequests = 5;

        /// <summary>
        /// Batch size for batch processing.
        /// </summary>
        public int batchSize = 5;

        /// <summary>
        /// Batch timeout window in seconds.
        /// </summary>
        public int batchTimeoutSeconds = 2;

        /// <summary>
        /// Enable detailed logging for debugging.
        /// </summary>
        public bool enableDetailedLogging = false;

        /// <summary>
        /// Log level: 0=Debug, 1=Info, 2=Warning, 3=Error.
        /// </summary>
        public int logLevel = 1;

        /// <summary>
        /// Enable health check monitoring.
        /// </summary>
        public bool enableHealthCheck = true;

        /// <summary>
        /// Health check interval in minutes.
        /// </summary>
        public int healthCheckIntervalMinutes = 5;

        /// <summary>
        /// Enable memory monitoring.
        /// </summary>
        public bool enableMemoryMonitoring = true;

        /// <summary>
        /// Memory threshold for automatic cleanup (MB).
        /// </summary>
        public int memoryThresholdMB = 100;

        /// <summary>
        /// A master switch to enable embedding-related features.
        /// </summary>
        public bool enableEmbeddings = false;

        /// <summary>
        /// The API key for the embedding service. If empty, falls back to the main apiKey.
        /// </summary>
        public string embeddingApiKey = "";

        /// <summary>
        /// The base API URL for the embedding service (up to v1, without trailing slash).
        /// The system will automatically append '/embeddings' for embedding requests.
        /// </summary>
        public string embeddingEndpoint = "https://api.openai.com/v1";

        /// <summary>
        /// The model name for embeddings.
        /// </summary>
        public string embeddingModelName = "text-embedding-3-small";

        /// <summary>
        /// Gets the complete chat completions endpoint URL.
        /// </summary>
        public string ChatCompletionsEndpoint
        {
            get
            {
                if (string.IsNullOrWhiteSpace(apiEndpoint))
                    return "";
                
                // 清理端点URL
                var endpoint = apiEndpoint.Trim().TrimEnd('/');
                
                // 如果已经包含完整的API路径，直接返回
                if (endpoint.EndsWith("/chat/completions", StringComparison.OrdinalIgnoreCase) ||
                    endpoint.EndsWith("/completions", StringComparison.OrdinalIgnoreCase))
                {
                    return endpoint;
                }
                
                // 如果是基础的v1端点，自动补全 /chat/completions
                if (endpoint.EndsWith("/v1", StringComparison.OrdinalIgnoreCase))
                {
                    return endpoint + "/chat/completions";
                }
                
                // 对于其他情况（可能是自定义端点），不做自动补全，直接返回
                return endpoint;
            }
        }

        /// <summary>
        /// Gets the complete embeddings endpoint URL.
        /// </summary>
        public string EmbeddingsEndpoint
        {
            get
            {
                if (string.IsNullOrWhiteSpace(embeddingEndpoint))
                    return "";
                
                string baseUrl = embeddingEndpoint.TrimEnd('/');
                return $"{baseUrl}/embeddings";
            }
        }


        /// <summary>
        /// This method is called by RimWorld to save and load the mod's settings.
        /// It's crucial for persisting the settings across game sessions.
        /// </summary>
        public override void ExposeData()
        {
            base.ExposeData();

            // Chat Completion Settings
            Scribe_Values.Look(ref apiKey, "RimAIFramework_apiKey", "");
            Scribe_Values.Look(ref apiEndpoint, "RimAIFramework_apiEndpoint", "https://api.openai.com/v1");
            Scribe_Values.Look(ref modelName, "RimAIFramework_modelName", "gpt-4o");
            Scribe_Values.Look(ref enableStreaming, "RimAIFramework_enableStreaming", false);
            Scribe_Values.Look(ref temperature, "RimAIFramework_temperature", 0.7f);

            // Performance Settings
            Scribe_Values.Look(ref maxTokens, "RimAIFramework_maxTokens", 1000);
            Scribe_Values.Look(ref timeoutSeconds, "RimAIFramework_timeoutSeconds", 30);
            Scribe_Values.Look(ref retryCount, "RimAIFramework_retryCount", 3);
            Scribe_Values.Look(ref maxConcurrentRequests, "RimAIFramework_maxConcurrentRequests", 5);

            // Cache Settings
            Scribe_Values.Look(ref enableCaching, "RimAIFramework_enableCaching", true);
            Scribe_Values.Look(ref cacheSize, "RimAIFramework_cacheSize", 1000);
            Scribe_Values.Look(ref cacheTtlMinutes, "RimAIFramework_cacheTtlMinutes", 30);

            // Batch Processing Settings
            Scribe_Values.Look(ref batchSize, "RimAIFramework_batchSize", 5);
            Scribe_Values.Look(ref batchTimeoutSeconds, "RimAIFramework_batchTimeoutSeconds", 2);

            // Diagnostics Settings
            Scribe_Values.Look(ref enableDetailedLogging, "RimAIFramework_enableDetailedLogging", false);
            Scribe_Values.Look(ref logLevel, "RimAIFramework_logLevel", 1);
            Scribe_Values.Look(ref enableHealthCheck, "RimAIFramework_enableHealthCheck", true);
            Scribe_Values.Look(ref healthCheckIntervalMinutes, "RimAIFramework_healthCheckIntervalMinutes", 5);
            Scribe_Values.Look(ref enableMemoryMonitoring, "RimAIFramework_enableMemoryMonitoring", true);
            Scribe_Values.Look(ref memoryThresholdMB, "RimAIFramework_memoryThresholdMB", 100);

            // Embeddings Settings
            Scribe_Values.Look(ref enableEmbeddings, "RimAIFramework_enableEmbeddings", false);
            Scribe_Values.Look(ref embeddingApiKey, "RimAIFramework_embeddingApiKey", "");
            Scribe_Values.Look(ref embeddingEndpoint, "RimAIFramework_embeddingEndpoint", "https://api.openai.com/v1");
            Scribe_Values.Look(ref embeddingModelName, "RimAIFramework_embeddingModelName", "text-embedding-3-small");
        }
    }
}
