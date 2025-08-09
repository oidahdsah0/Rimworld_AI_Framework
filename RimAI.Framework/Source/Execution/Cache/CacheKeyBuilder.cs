using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RimAI.Framework.Configuration.Models;
using RimAI.Framework.Contracts;

namespace RimAI.Framework.Execution.Cache
{
    /// <summary>
    /// Builds stable cache keys for chat and embedding requests.
    /// </summary>
    public static class CacheKeyBuilder
    {
        private static readonly JsonSerializerSettings CanonicalJsonSettings = new JsonSerializerSettings
        {
            NullValueHandling = NullValueHandling.Ignore,
            DefaultValueHandling = DefaultValueHandling.Ignore,
        };

        public static string BuildChatKey(UnifiedChatRequest request, MergedChatConfig cfg)
        {
            var fingerprint = new JObject
            {
                ["ns"] = "chat",
                ["provider"] = cfg.ProviderName ?? string.Empty,
                ["endpoint"] = StripApiKeyPlaceholder(cfg.Endpoint ?? string.Empty),
                ["model"] = cfg.Model ?? string.Empty,
            };

            // Normalize messages
            var messages = new JArray();
            foreach (var m in request.Messages ?? Enumerable.Empty<ChatMessage>())
            {
                var jm = new JObject
                {
                    ["role"] = m.Role,
                    ["content"] = m.Content,
                };
                if (!string.IsNullOrEmpty(m.ToolCallId)) jm["tool_call_id"] = m.ToolCallId;
                if (m.ToolCalls != null && m.ToolCalls.Count > 0) jm["tool_calls"] = JToken.FromObject(m.ToolCalls);
                messages.Add(jm);
            }

            // Normalize tools (tool definitions affect model output; include full definitions)
            JToken tools = null;
            if (request.Tools != null && request.Tools.Count > 0)
            {
                tools = JToken.FromObject(request.Tools);
            }

            // Dynamic parameters (from user/config where applicable)
            var parameters = new JObject();
            var defParams = cfg.Template?.ChatApi?.DefaultParameters as JObject;
            if (defParams != null) parameters.Merge(defParams);

            void PutIfHas(string name, JToken value)
            {
                if (value != null && !(value.Type == JTokenType.Null)) parameters[name] = value;
            }

            PutIfHas("temperature", cfg.User?.Temperature);
            PutIfHas("top_p", cfg.User?.TopP);
            PutIfHas("typical_p", cfg.User?.TypicalP);
            PutIfHas("max_tokens", cfg.User?.MaxTokens);

            // Static parameters and overrides
            var staticMerged = new JObject();
            if (cfg.Template?.StaticParameters is JObject t) staticMerged.Merge(t);
            if (cfg.User?.StaticParametersOverride is JObject u) staticMerged.Merge(u);

            // JSON mode: include flag and, when enabled, the provider-specific value to avoid collisions
            var jsonModeEnabled = request.ForceJsonOutput;
            JToken jsonModeValue = null;
            if (jsonModeEnabled)
            {
                var jm = cfg?.Template?.ChatApi?.JsonMode?.Value;
                if (jm != null)
                    jsonModeValue = jm.DeepClone();
            }

            var body = new JObject
            {
                ["messages"] = messages,
                ["tools"] = tools,
                ["parameters"] = parameters,
                ["static"] = staticMerged,
                ["json_mode"] = new JObject
                {
                    ["enabled"] = jsonModeEnabled,
                    ["value"] = jsonModeValue
                },
            };

            // IMPORTANT: ignore request.Stream in key to unify stream/non-stream

            var canonical = Canonicalize(body);
            var hash = Sha256Hex(canonical);
            return $"chat:{cfg.ProviderName}:{cfg.Model}:{hash}";
        }

        public static string BuildEmbeddingKey(string input, MergedEmbeddingConfig cfg)
        {
            var normalized = input ?? string.Empty;
            var contentHash = Sha256Hex(normalized);
            return $"embed:{cfg.ProviderName}:{cfg.Model}:{contentHash}";
        }

        private static string StripApiKeyPlaceholder(string endpoint)
        {
            if (string.IsNullOrEmpty(endpoint)) return string.Empty;
            return endpoint.Replace("{apiKey}", "{key}");
        }

        private static string Canonicalize(JToken token)
        {
            if (token == null) return string.Empty;
            if (token is JObject obj)
            {
                var props = obj.Properties().OrderBy(p => p.Name, StringComparer.Ordinal)
                    .Select(p => new JProperty(p.Name, JToken.Parse(Canonicalize(p.Value))));
                var ordered = new JObject();
                foreach (var p in props) ordered.Add(p);
                return ordered.ToString(Formatting.None);
            }
            if (token is JArray arr)
            {
                var items = arr.Select(i => Canonicalize(i));
                return new JArray(items.Select(JToken.Parse)).ToString(Formatting.None);
            }
            return token.ToString(Formatting.None);
        }

        private static string Sha256Hex(string s)
        {
            using (var sha = SHA256.Create())
            {
                var bytes = Encoding.UTF8.GetBytes(s);
                var hash = sha.ComputeHash(bytes);
                var sb = new StringBuilder(hash.Length * 2);
                foreach (var b in hash) sb.Append(b.ToString("x2"));
                return sb.ToString();
            }
        }
    }
}


