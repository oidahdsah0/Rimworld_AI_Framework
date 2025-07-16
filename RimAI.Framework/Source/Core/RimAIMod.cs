using System;
using System.Threading.Tasks;
using UnityEngine;
using Verse;
using RimWorld;
using RimAI.Framework.LLM;

namespace RimAI.Framework.Core
{
    /// <summary>
    /// The main Mod class for the RimAI Framework.
    /// This class handles the settings window and is the primary entry point for RimWorld.
    /// </summary>
    public class RimAIMod : Mod
    {
        /// <summary>
        /// A reference to our settings instance.
        /// </summary>
        public readonly RimAISettings settings;

        private bool isTesting = false;
        private string testResult = "";
        private Color testResultColor = Color.white;
        private string testingStatus = "";

        /// <summary>
        /// The constructor for the Mod class. It's called once when the mod is loaded.
        /// </summary>
        /// <param name="content">The ModContentPack which contains info about this mod.</param>
        public RimAIMod(ModContentPack content) : base(content)
        {
            // Get a reference to our settings.
            settings = GetSettings<RimAISettings>();
        }

        /// <summary>
        /// The name of the mod in the settings list.
        /// </summary>
        /// <returns>The display name for the settings category.</returns>
        public override string SettingsCategory()
        {
            return "RimAI.Framework.Settings.Category".Translate();
        }

        /// <summary>
        /// This method is called when the user opens the settings window for this mod.
        /// We use it to draw our custom settings UI.
        /// </summary>
        /// <param name="inRect">The rectangle area to draw the settings within.</param>
        public override void DoSettingsWindowContents(Rect inRect)
        {
            Listing_Standard listingStandard = new Listing_Standard();
            listingStandard.Begin(inRect);

            // --- Chat Completion Settings ---
            listingStandard.Label("RimAI.Framework.Settings.ChatCompletion.Title".Translate());
            listingStandard.GapLine();

            listingStandard.Label("RimAI.Framework.Settings.ChatCompletion.APIKey".Translate());
            settings.apiKey = listingStandard.TextEntry(settings.apiKey);

            listingStandard.Label("RimAI.Framework.Settings.ChatCompletion.EndpointURL".Translate());
            settings.apiEndpoint = listingStandard.TextEntry(settings.apiEndpoint);

            listingStandard.Label("RimAI.Framework.Settings.ChatCompletion.ModelName".Translate());
            settings.modelName = listingStandard.TextEntry(settings.modelName);

            listingStandard.CheckboxLabeled("RimAI.Framework.Settings.ChatCompletion.EnableStreaming".Translate(), ref settings.enableStreaming, "RimAI.Framework.Settings.ChatCompletion.EnableStreaming.Tooltip".Translate());

            // Add a description label to clarify the V1 behavior
            Text.Font = GameFont.Tiny;
            GUI.color = Color.gray;
            listingStandard.Label("  " + "RimAI.Framework.Settings.StreamingNotice".Translate());
            GUI.color = Color.white;
            Text.Font = GameFont.Small;

            listingStandard.Gap(12f);

            // --- Test Connection Button ---
            if (isTesting)
            {
                listingStandard.Label("RimAI.Framework.Settings.TestingStatus".Translate());
                if (!string.IsNullOrEmpty(testingStatus))
                {
                    GUI.color = Color.yellow;
                    listingStandard.Label($"  {testingStatus}");
                    GUI.color = Color.white;
                }
            }
            else
            {
                if (listingStandard.ButtonText("RimAI.Framework.Settings.TestConnectionButton".Translate()))
                {
                    Log.Message("[RimAI] RimAIMod: Test Connection button clicked");
                    isTesting = true;
                    testResult = "";
                    testingStatus = "RimAI.Framework.Settings.TestConnectionStatus.Initializing".Translate();
                    _ = TestConnection();
                }
            }

            if (!string.IsNullOrEmpty(testResult))
            {
                GUI.color = testResultColor;
                listingStandard.Label(testResult);
                GUI.color = Color.white;
            }

            listingStandard.Gap(24f);

            // --- Embeddings Settings ---
            listingStandard.Label("RimAI.Framework.Settings.Embeddings.Title".Translate());
            listingStandard.GapLine();

            listingStandard.CheckboxLabeled("RimAI.Framework.Settings.Embeddings.EnableEmbeddings".Translate(), ref settings.enableEmbeddings, "RimAI.Framework.Settings.Embeddings.EnableEmbeddings.Tooltip".Translate());

            if (settings.enableEmbeddings)
            {
                listingStandard.Label("RimAI.Framework.Settings.Embeddings.APIKey".Translate());
                settings.embeddingApiKey = listingStandard.TextEntry(settings.embeddingApiKey);

                listingStandard.Label("RimAI.Framework.Settings.Embeddings.EndpointURL".Translate());
                settings.embeddingEndpoint = listingStandard.TextEntry(settings.embeddingEndpoint);

                listingStandard.Label("RimAI.Framework.Settings.Embeddings.ModelName".Translate());
                settings.embeddingModelName = listingStandard.TextEntry(settings.embeddingModelName);
            }

            listingStandard.Gap(24f);

            // --- Reset Button ---
            if (listingStandard.ButtonText("RimAI.Framework.Settings.ResetButton".Translate()))
            {
                // Reset Chat settings
                settings.apiKey = "";
                settings.apiEndpoint = "https://api.openai.com/v1/chat/completions";
                settings.modelName = "gpt-4o";
                // settings.apiEndpoint = "https://api.deepseek.com/v1/chat/completions";
                // settings.modelName = "deepseek-chat";
                settings.enableStreaming = false;

                // Reset Embeddings settings
                settings.enableEmbeddings = false;
                settings.embeddingApiKey = "";
                settings.embeddingEndpoint = "https://api.openai.com/v1/embeddings";
                settings.embeddingModelName = "text-embedding-3-small";
            }

            listingStandard.End();
            base.DoSettingsWindowContents(inRect);
        }

        private async Task TestConnection()
        {
            Log.Message("[RimAI] RimAIMod: TestConnection started");
            Messages.Message("RimAI.Framework.Messages.TestStarting".Translate(), MessageTypeDefOf.NeutralEvent);
            testingStatus = "RimAI.Framework.Settings.TestConnectionStatus.Validating".Translate();
            
            try
            {
                testingStatus = "RimAI.Framework.Settings.TestConnectionStatus.Connecting".Translate();
                var (success, message) = await LLMManager.Instance.TestConnectionAsync();
                
                Log.Message($"[RimAI] RimAIMod: TestConnection completed. Success: {success}, Message: {message}");
                
                if (success)
                {
                    testResultColor = Color.green;
                    testResult = $"Success: {message}";
                    Messages.Message($"{"RimAI.Framework.Messages.TestSuccess".Translate()} {message}", MessageTypeDefOf.PositiveEvent);
                }
                else
                {
                    testResultColor = Color.red;
                    testResult = $"Failure: {message}";
                    Messages.Message($"{"RimAI.Framework.Messages.TestFailed".Translate()} {message}", MessageTypeDefOf.NegativeEvent);
                }
            }
            catch (Exception ex)
            {
                Log.Error($"[RimAI] RimAIMod: TestConnection exception: {ex.ToString()}");
                testResultColor = Color.red;
                testResult = $"Exception: {ex.Message}";
                Messages.Message($"{"RimAI.Framework.Messages.TestError".Translate()} {ex.Message}", MessageTypeDefOf.RejectInput);
            }
            finally
            {
                Log.Message("[RimAI] RimAIMod: Setting isTesting to false");
                testingStatus = "";
                isTesting = false;
            }
        }
    }
}
