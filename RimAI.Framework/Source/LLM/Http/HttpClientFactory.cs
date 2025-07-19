using System;
using System.Net.Http;
using Verse;

namespace RimAI.Framework.LLM.Http
{
    /// <summary>
    /// Factory for creating properly configured HttpClient instances
    /// </summary>
    public static class HttpClientFactory
    {
        /// <summary>
        /// Creates a properly configured HttpClient instance for RimAI Framework
        /// </summary>
        /// <param name="timeoutSeconds">Request timeout in seconds</param>
        /// <returns>Configured HttpClient instance</returns>
        public static HttpClient CreateClient(int timeoutSeconds = 60)
        {
            try
            {
                var handler = new HttpClientHandler();
                
                try
                {
                    handler.ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) => true;
                }
                catch (Exception ex)
                {
                    Log.Warning($"RimAI Framework: Could not configure SSL validation bypass: {ex.Message}");
                }
                
                var client = new HttpClient(handler);
                ConfigureClient(client, timeoutSeconds);
                
                Log.Message("RimAI Framework: HttpClient initialized successfully.");
                return client;
            }
            catch (Exception ex)
            {
                Log.Error($"RimAI Framework: Failed to initialize HttpClient with custom handler, using default: {ex.Message}");
                var client = new HttpClient();
                ConfigureClient(client, timeoutSeconds);
                return client;
            }
        }

        private static void ConfigureClient(HttpClient client, int timeoutSeconds)
        {
            client.Timeout = TimeSpan.FromSeconds(timeoutSeconds);
            client.DefaultRequestHeaders.Add("User-Agent", "RimAI-Framework/1.0");
            client.DefaultRequestHeaders.Add("Accept", "application/json");
        }
    }
}
