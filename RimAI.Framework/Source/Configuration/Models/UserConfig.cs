using System.Text.Json.Serialization;

namespace RimAI.Framework.Configuration.Models
{
    public class UserConfig
    {
        [JsonPropertyName("apiKey")]
        public string ApiKey { get; set; }

        [JsonPropertyName("concurrencyLimit")]
        public int ConcurrencyLimit { get; set; } = 3;
    }
}