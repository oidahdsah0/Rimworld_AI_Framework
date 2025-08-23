using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using Newtonsoft.Json;
using RimAI.Framework.Contracts;

namespace RimAI.Framework.Shared.Logging
{
	public static class RequestLogFormatter
	{
		private const int DefaultMaxChars = 6000;

		public static string FormatApiCallHeader(string apiName, string providerId, string conversationId = null, bool? stream = null, int? messagesCount = null, int? toolsCount = null, int? batchCount = null, int? inputsCount = null)
		{
			var sb = new StringBuilder();
			sb.Append("[Request] API=").Append(apiName);
			if (!string.IsNullOrEmpty(providerId)) sb.Append(" Provider=").Append(providerId);
			if (!string.IsNullOrEmpty(conversationId)) sb.Append(" ConversationId=").Append(conversationId);
			if (stream.HasValue) sb.Append(" Stream=").Append(stream.Value ? "true" : "false");
			if (messagesCount.HasValue) sb.Append(" Messages=").Append(messagesCount.Value);
			if (toolsCount.HasValue) sb.Append(" Tools=").Append(toolsCount.Value);
			if (batchCount.HasValue) sb.Append(" BatchCount=").Append(batchCount.Value);
			if (inputsCount.HasValue) sb.Append(" Inputs=").Append(inputsCount.Value);
			return sb.ToString();
		}

		public static string FormatUnifiedChatRequest(UnifiedChatRequest request, int maxChars = DefaultMaxChars)
		{
			string json = JsonConvert.SerializeObject(request, Formatting.Indented, new JsonSerializerSettings
			{
				NullValueHandling = NullValueHandling.Ignore
			});
			return FormatPayloadJson(json, maxChars);
		}

		public static string FormatUnifiedEmbeddingRequest(UnifiedEmbeddingRequest request, int maxChars = DefaultMaxChars)
		{
			string json = JsonConvert.SerializeObject(request, Formatting.Indented, new JsonSerializerSettings
			{
				NullValueHandling = NullValueHandling.Ignore
			});
			return FormatPayloadJson(json, maxChars);
		}

		public static string FormatChatBatch(List<UnifiedChatRequest> requests, int maxItems = 5, int maxChars = DefaultMaxChars)
		{
			var preview = new
			{
				total = requests?.Count ?? 0,
				items = (requests ?? new List<UnifiedChatRequest>()).Take(maxItems).Select(r => new
				{
					conversationId = r?.ConversationId,
					messages = r?.Messages?.Count ?? 0,
					tools = r?.Tools?.Count ?? 0,
					stream = r?.Stream ?? false
				}).ToList(),
				truncated = (requests?.Count ?? 0) > maxItems
			};
			string json = JsonConvert.SerializeObject(preview, Formatting.Indented);
			return FormatPayloadJson(json, maxChars);
		}

		public static string FormatProviderDispatch(string apiName, string providerId, HttpRequestMessage httpRequest, string requestBodyJson, string conversationId = null, bool? stream = null, int? messagesCount = null, int? toolsCount = null, int? batchCount = null, int? inputsCount = null, int maxChars = DefaultMaxChars)
		{
			var sb = new StringBuilder();
			sb.Append("[Dispatch] API=").Append(apiName);
			if (!string.IsNullOrEmpty(providerId)) sb.Append(" Provider=").Append(providerId);
			if (!string.IsNullOrEmpty(conversationId)) sb.Append(" ConversationId=").Append(conversationId);
			if (stream.HasValue) sb.Append(" Stream=").Append(stream.Value ? "true" : "false");
			if (messagesCount.HasValue) sb.Append(" Messages=").Append(messagesCount.Value);
			if (toolsCount.HasValue) sb.Append(" Tools=").Append(toolsCount.Value);
			if (batchCount.HasValue) sb.Append(" BatchCount=").Append(batchCount.Value);
			if (inputsCount.HasValue) sb.Append(" Inputs=").Append(inputsCount.Value);
			sb.Append(" Method=").Append(httpRequest.Method.Method);
			sb.Append(" Endpoint=").Append(SanitizeEndpoint(httpRequest.RequestUri));
			sb.AppendLine();
			sb.Append("Headers: ").Append(FormatHeaders(httpRequest)).AppendLine();
			sb.Append(FormatPayloadJson(PrettyJsonOrRaw(requestBodyJson), maxChars));
			return sb.ToString();
		}

		private static string FormatHeaders(HttpRequestMessage req)
		{
			var all = new List<KeyValuePair<string, IEnumerable<string>>>();
			if (req.Headers != null)
				all.AddRange(req.Headers.Select(h => new KeyValuePair<string, IEnumerable<string>>(h.Key, h.Value)));
			if (req.Content?.Headers != null)
				all.AddRange(req.Content.Headers.Select(h => new KeyValuePair<string, IEnumerable<string>>(h.Key, h.Value)));
			var parts = all.Select(h =>
			{
				var lower = h.Key.ToLowerInvariant();
				var masked = ShouldMaskHeader(lower)
					? h.Value.Select(_ => "***")
					: h.Value;
				return $"{h.Key}=[{string.Join(",", masked)}]";
			});
			return string.Join("; ", parts);
		}

		private static bool ShouldMaskHeader(string lowerKey)
		{
			return lowerKey == "authorization"
				|| lowerKey.Contains("api-key")
				|| lowerKey.Contains("x-api-key")
				|| lowerKey.Contains("token")
				|| lowerKey.Contains("secret")
				|| lowerKey.EndsWith("key");
		}

		private static string SanitizeEndpoint(Uri uri)
		{
			if (uri == null) return "(null)";
			string pathAndQuery = uri.GetLeftPart(UriPartial.Path);
			var query = Uri.UnescapeDataString(uri.Query);
			if (string.IsNullOrEmpty(query)) return pathAndQuery;
			var trimmed = query.TrimStart('?');
			var pairs = trimmed.Split(new[] { '&' }, StringSplitOptions.RemoveEmptyEntries);
			for (int i = 0; i < pairs.Length; i++)
			{
				var kv = pairs[i].Split(new[] { '=' }, 2);
				var k = kv[0];
				var v = kv.Length > 1 ? kv[1] : "";
				var lk = k.ToLowerInvariant();
				if (lk.Contains("token") || lk.Contains("secret") || lk.Contains("key"))
				{
					v = "***";
				}
				pairs[i] = k + "=" + v;
			}
			return pathAndQuery + "?" + string.Join("&", pairs);
		}

		private static string PrettyJsonOrRaw(string body)
		{
			if (string.IsNullOrEmpty(body)) return "(empty-body)";
			try
			{
				var parsed = Newtonsoft.Json.Linq.JToken.Parse(body);
				return parsed.ToString(Formatting.Indented);
			}
			catch
			{
				return body;
			}
		}

		private static string FormatPayloadJson(string json, int maxChars)
		{
			if (string.IsNullOrEmpty(json)) return "Payload: (empty)";
			string truncated = json.Length > maxChars ? json.Substring(0, maxChars) + "... [truncated]" : json;
			var sb = new StringBuilder();
			sb.AppendLine("Payload(JSON):");
			sb.Append(truncated);
			return sb.ToString();
		}
	}
}

