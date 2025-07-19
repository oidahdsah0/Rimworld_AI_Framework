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
                
                string baseUrl = apiEndpoint.TrimEnd('/');
                return $"{baseUrl}/chat/completions";
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

            // Embeddings Settings
            Scribe_Values.Look(ref enableEmbeddings, "RimAIFramework_enableEmbeddings", false);
            Scribe_Values.Look(ref embeddingApiKey, "RimAIFramework_embeddingApiKey", "");
            Scribe_Values.Look(ref embeddingEndpoint, "RimAIFramework_embeddingEndpoint", "https://api.openai.com/v1");
            Scribe_Values.Look(ref embeddingModelName, "RimAIFramework_embeddingModelName", "text-embedding-3-small");
        }
    }
}
