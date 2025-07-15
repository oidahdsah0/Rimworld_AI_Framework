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
        /// The API endpoint URL for the LLM service.
        /// Defaults to OpenAI's chat completions endpoint.
        /// </summary>
        public string apiEndpoint = "https://api.openai.com/v1/chat/completions";

        /// <summary>
        /// The model name for chat completions.
        /// </summary>
        public string modelName = "gpt-4o";

        /// <summary>
        /// Whether to enable streaming responses.
        /// </summary>
        public bool enableStreaming = true;

        /// <summary>
        /// A master switch to enable embedding-related features.
        /// </summary>
        public bool enableEmbeddings = false;

        /// <summary>
        /// The API key for the embedding service. If empty, falls back to the main apiKey.
        /// </summary>
        public string embeddingApiKey = "";

        /// <summary>
        /// The API endpoint URL for the embedding service.
        /// </summary>
        public string embeddingEndpoint = "https://api.openai.com/v1/embeddings";

        /// <summary>
        /// The model name for embeddings.
        /// </summary>
        public string embeddingModelName = "text-embedding-3-small";


        /// <summary>
        /// This method is called by RimWorld to save and load the mod's settings.
        /// It's crucial for persisting the settings across game sessions.
        /// </summary>
        public override void ExposeData()
        {
            base.ExposeData();
            
            // Chat Completion
            Scribe_Values.Look(ref apiKey, "apiKey", "");
            Scribe_Values.Look(ref apiEndpoint, "apiEndpoint", "https://api.openai.com/v1/chat/completions");
            Scribe_Values.Look(ref modelName, "modelName", "gpt-4o");
            Scribe_Values.Look(ref enableStreaming, "enableStreaming", true);

            // Embeddings
            Scribe_Values.Look(ref enableEmbeddings, "enableEmbeddings", false);
            Scribe_Values.Look(ref embeddingApiKey, "embeddingApiKey", "");
            Scribe_Values.Look(ref embeddingEndpoint, "embeddingEndpoint", "https://api.openai.com/v1/embeddings");
            Scribe_Values.Look(ref embeddingModelName, "embeddingModelName", "text-embedding-3-small");
        }
    }
}
