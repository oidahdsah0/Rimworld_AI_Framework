using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using RimAI.Framework.Configuration;
using RimAI.Framework.Core;
using RimAI.Framework.LLM.Models;
using Verse;

namespace RimAI.Framework.LLM.Services
{
    /// <summary>
    /// Modern unified LLM executor implementation - handles all LLM requests through unified interface
    /// </summary>
    public class LLMExecutor
    {
        private readonly HttpClient _httpClient;
        private readonly RimAISettings _settings;
        private readonly Dictionary<string, object> _defaultHeaders;

        public LLMExecutor(HttpClient httpClient, RimAISettings settings)
        {
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _defaultHeaders = new Dictionary<string, object>();
        }

        #region Configuration Priority Methods
        /// <summary>
        /// 获取API端点 - 优先级：配置系统 > 设置对象 > 默认值
        /// </summary>
        private string GetApiEndpoint()
        {
            try
            {
                var config = RimAIConfiguration.Instance;
                var configEndpoint = config?.Get<string>("api.endpoint");
                if (!string.IsNullOrEmpty(configEndpoint))
                    return configEndpoint;
                
                return _settings?.apiEndpoint ?? "https://api.openai.com/v1/chat/completions";
            }
            catch
            {
                return _settings?.apiEndpoint ?? "https://api.openai.com/v1/chat/completions";
            }
        }

        /// <summary>
        /// 获取API密钥 - 优先级：配置系统 > 设置对象 > 空值
        /// </summary>
        private string GetApiKey()
        {
            try
            {
                var config = RimAIConfiguration.Instance;
                var configKey = config?.Get<string>("api.key");
                if (!string.IsNullOrEmpty(configKey))
                    return configKey;
                
                return _settings?.apiKey ?? string.Empty;
            }
            catch
            {
                return _settings?.apiKey ?? string.Empty;
            }
        }

        /// <summary>
        /// 应用全局默认值到请求 - 只在未指定时应用
        /// </summary>
        private void ApplyGlobalDefaults(UnifiedLLMRequest request)
        {
            try
            {
                var config = RimAIConfiguration.Instance;
                
                // 确保Options不为空
                if (request.Options == null)
                {
                    request.Options = new LLMRequestOptions();
                }
                
                // 应用API默认值
                if (!request.Options.Temperature.HasValue)
                {
                    var defaultTemp = config?.Get<float?>("api.temperature") ?? _settings?.temperature ?? 0.7f;
                    request.Options.Temperature = defaultTemp;
                }
                
                if (!request.Options.MaxTokens.HasValue)
                {
                    var defaultTokens = config?.Get<int?>("api.maxTokens") ?? _settings?.maxTokens ?? 1000;
                    request.Options.MaxTokens = defaultTokens;
                }
                
                if (string.IsNullOrEmpty(request.Options.Model))
                {
                    var defaultModel = config?.Get<string>("api.model") ?? _settings?.modelName ?? "gpt-4o";
                    request.Options.Model = defaultModel;
                }
                
                // 应用流式设置（只在没有明确设置时应用）
                if (!request.Options.HasExplicitStreamingSetting)
                {
                    var defaultStreaming = config?.Get<bool?>("api.enableStreaming") ?? _settings?.enableStreaming ?? false;
                    request.Options.EnableStreaming = defaultStreaming;
                }
            }
            catch (Exception ex)
            {
                Log.Warning($"[RimAI] Failed to apply global defaults: {ex.Message}");
            }
        }
        #endregion

        /// <summary>
        /// Execute a unified LLM request - the single entry point for all LLM operations
        /// </summary>
        public async Task<LLMResponse> ExecuteAsync(UnifiedLLMRequest request)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            try
            {
                // 应用全局默认值 - CRITICAL FIX
                ApplyGlobalDefaults(request);
                
                // Validate request
                var validationResult = ValidateRequest(request);
                if (!validationResult.IsValid)
                {
                    return LLMResponse.Failed(validationResult.Error, request.RequestId)
                        .WithMetadata("validation_error", true);
                }

                // Execute based on streaming mode
                if (request.IsStreaming)
                {
                    return await ExecuteStreamingInternalAsync(request);
                }
                else
                {
                    return await ExecuteNonStreamingInternalAsync(request);
                }
            }
            catch (OperationCanceledException)
            {
                return LLMResponse.Failed("Request was cancelled", request.RequestId)
                    .WithMetadata("cancelled", true);
            }
            catch (Exception ex)
            {
                Log.Error($"[RimAI] LLMExecutor: Execution failed: {ex}");
                return LLMResponse.Failed($"Execution failed: {ex.Message}", request.RequestId)
                    .WithMetadata("exception_type", ex.GetType().Name)
                    .WithMetadata("stack_trace", ex.StackTrace);
            }
        }

        /// <summary>
        /// Test connection to the LLM service
        /// </summary>
        public async Task<(bool success, string message)> TestConnectionAsync()
        {
            var testRequest = new UnifiedLLMRequest
            {
                Prompt = "Say 'test'",
                Options = new LLMRequestOptions { MaxTokens = 5 },
                CancellationToken = CancellationToken.None,
                RequestId = "test-connection"
            };

            var response = await ExecuteAsync(testRequest);
            return (response.IsSuccess, response.IsSuccess ? "Connection successful" : response.Error);
        }

        #region Private Implementation Methods
        
        private async Task<LLMResponse> ExecuteNonStreamingInternalAsync(UnifiedLLMRequest request)
        {
            var requestBody = BuildRequestBody(request, false);
            var jsonBody = JsonConvert.SerializeObject(requestBody);
            
            // 使用配置优先级获取API配置 - CRITICAL FIX
            var apiEndpoint = GetApiEndpoint();
            var apiKey = GetApiKey();
            
            var httpResponse = await SendHttpRequestAsync(
                apiEndpoint, 
                jsonBody, 
                apiKey, 
                request.CancellationToken
            );

            if (httpResponse.success)
            {
                var content = ParseChatCompletionResponse(httpResponse.responseBody);
                return LLMResponse.Success(content, request.RequestId)
                    .WithMetadata("status_code", httpResponse.statusCode)
                    .WithMetadata("model", request.Options?.Model ?? _settings.modelName)
                    .WithMetadata("temperature", request.Options?.Temperature ?? _settings.temperature);
            }

            return LLMResponse.Failed($"HTTP {httpResponse.statusCode}: {httpResponse.errorContent}", request.RequestId)
                .WithMetadata("status_code", httpResponse.statusCode)
                .WithMetadata("error_content", httpResponse.errorContent);
        }

        private async Task<LLMResponse> ExecuteStreamingInternalAsync(UnifiedLLMRequest request)
        {
            var responseBuilder = new StringBuilder();
            var chunkCount = 0;

            try
            {
                var requestBody = BuildRequestBody(request, true);
                var jsonBody = JsonConvert.SerializeObject(requestBody);
                
                // 使用配置优先级获取API配置 - CRITICAL FIX
                var apiEndpoint = GetApiEndpoint();
                var apiKey = GetApiKey();
                
                using var httpRequest = new HttpRequestMessage(HttpMethod.Post, apiEndpoint);
                httpRequest.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", apiKey);
                httpRequest.Content = new StringContent(jsonBody, Encoding.UTF8, "application/json");

                using var response = await _httpClient.SendAsync(httpRequest, HttpCompletionOption.ResponseHeadersRead, request.CancellationToken);
                
                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    return LLMResponse.Failed($"HTTP {response.StatusCode}: {errorContent}", request.RequestId)
                        .WithMetadata("status_code", (int)response.StatusCode)
                        .WithMetadata("error_content", errorContent);
                }

                using var stream = await response.Content.ReadAsStreamAsync();
                using var reader = new StreamReader(stream);
                
                string line;
                while ((line = await reader.ReadLineAsync()) != null)
                {
                    if (request.CancellationToken.IsCancellationRequested)
                        break;

                    if (string.IsNullOrWhiteSpace(line) || !line.StartsWith("data: "))
                        continue;

                    var jsonData = line.Substring(6);
                    if (jsonData == "[DONE]")
                        break;

                    try
                    {
                        var chunk = JsonConvert.DeserializeObject<dynamic>(jsonData);
                        var content = chunk?.choices?[0]?.delta?.content?.ToString();
                        
                        if (!string.IsNullOrEmpty(content))
                        {
                            responseBuilder.Append(content);
                            chunkCount++;
                            request.OnChunkReceived?.Invoke(content);
                        }
                    }
                    catch (Exception chunkEx)
                    {
                        Log.Warning($"RimAI Framework: Failed to parse streaming chunk: {chunkEx.Message}");
                    }
                }

                return LLMResponse.Success(responseBuilder.ToString(), request.RequestId)
                    .WithMetadata("streaming", true)
                    .WithMetadata("chunk_count", chunkCount)
                    .WithMetadata("model", request.Options?.Model ?? _settings.modelName);
            }
            catch (Exception ex)
            {
                return LLMResponse.Failed($"Streaming failed: {ex.Message}", request.RequestId)
                    .WithContent(responseBuilder.ToString())
                    .WithMetadata("streaming", true)
                    .WithMetadata("chunk_count", chunkCount)
                    .WithMetadata("partial_content", responseBuilder.Length > 0);
            }
        }

        private object BuildRequestBody(UnifiedLLMRequest request, bool streaming)
        {
            var options = request.Options ?? new LLMRequestOptions();
            var temperature = options.Temperature ?? _settings.temperature;
            var model = options.Model ?? _settings.modelName;

            var body = new Dictionary<string, object>
            {
                ["model"] = model,
                ["messages"] = new[]
                {
                    new { role = "user", content = request.Prompt }
                },
                ["stream"] = streaming,
                ["temperature"] = temperature
            };

            if (options.MaxTokens.HasValue)
            {
                body["max_tokens"] = options.MaxTokens.Value;
            }

            // Add any additional parameters from options
            if (options.TopP.HasValue)
            {
                body["top_p"] = options.TopP.Value;
            }

            if (options.FrequencyPenalty.HasValue)
            {
                body["frequency_penalty"] = options.FrequencyPenalty.Value;
            }

            if (options.PresencePenalty.HasValue)
            {
                body["presence_penalty"] = options.PresencePenalty.Value;
            }

            // Add all custom parameters from AdditionalParameters
            if (options.AdditionalParameters != null && options.AdditionalParameters.Count > 0)
            {
                foreach (var param in options.AdditionalParameters)
                {
                    // Avoid overwriting core parameters that are already set
                    if (!body.ContainsKey(param.Key))
                    {
                        body[param.Key] = param.Value;
                    }
                }
            }

            return body;
        }

        private (bool IsValid, string Error) ValidateRequest(UnifiedLLMRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Prompt))
            {
                return (false, "Prompt cannot be empty");
            }

            if (request.Prompt.Length > 32000) // Example limit
            {
                return (false, "Prompt exceeds maximum length");
            }

            if (string.IsNullOrEmpty(_settings.apiKey))
            {
                return (false, "API Key is not configured");
            }

            if (string.IsNullOrEmpty(_settings.apiEndpoint))
            {
                return (false, "API Endpoint is not configured");
            }

            return (true, null);
        }

        private string ParseChatCompletionResponse(string jsonResponse)
        {
            try
            {
                var responseObject = JsonConvert.DeserializeObject<ChatCompletionResponse>(jsonResponse);
                
                if (responseObject?.choices != null && responseObject.choices.Count > 0)
                {
                    var firstChoice = responseObject.choices[0];
                    if (firstChoice?.message != null && !string.IsNullOrEmpty(firstChoice.message.content))
                    {
                        return firstChoice.message.content;
                    }
                }
                
                Log.Error($"RimAI Framework: Response format unexpected. Response: {jsonResponse}");
                return null;
            }
            catch (Exception ex)
            {
                Log.Error($"RimAI Framework: Error parsing response: {ex.Message}");
                return null;
            }
        }

        private async Task<(bool success, int statusCode, string responseBody, string errorContent)> SendHttpRequestAsync(
            string endpoint, string jsonBody, string apiKey, CancellationToken cancellationToken)
        {
            try
            {
                using var request = new HttpRequestMessage(HttpMethod.Post, endpoint);
                request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", apiKey);
                request.Content = new StringContent(jsonBody, Encoding.UTF8, "application/json");

                // Add custom headers if any
                foreach (var header in _defaultHeaders)
                {
                    request.Headers.TryAddWithoutValidation(header.Key, header.Value.ToString());
                }

                using var response = await _httpClient.SendAsync(request, cancellationToken);
                var responseBody = await response.Content.ReadAsStringAsync();
                
                if (response.IsSuccessStatusCode)
                {
                    return (true, (int)response.StatusCode, responseBody, null);
                }
                else
                {
                    return (false, (int)response.StatusCode, null, responseBody);
                }
            }
            catch (Exception ex)
            {
                Log.Error($"RimAI Framework: HTTP request exception: {ex}");
                return (false, 0, null, ex.Message);
            }
        }

        #endregion

        public void Dispose()
        {
            // HttpClient is managed externally
        }
    }
}
