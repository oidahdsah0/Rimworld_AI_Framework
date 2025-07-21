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
        /// 获取API端点 - 优先级：设置对象 > 配置系统 > 默认值
        /// </summary>
        private string GetApiEndpoint()
        {
            try
            {
                // 优先使用RimAISettings中的端点（用户在游戏中设置的）
                string baseEndpoint = _settings?.apiEndpoint;
                
                // 如果RimAISettings中没有设置，则检查配置系统
                if (string.IsNullOrEmpty(baseEndpoint))
                {
                    var config = RimAIConfiguration.Instance;
                    baseEndpoint = config?.Get<string>("api.endpoint");
                }
                
                // 如果没有配置，使用默认值
                if (string.IsNullOrEmpty(baseEndpoint))
                {
                    return "https://api.openai.com/v1/chat/completions";
                }
                
                // 统一使用 RimAISettings 的 ChatCompletionsEndpoint 属性来处理URL补全
                var tempSettings = new RimAISettings { apiEndpoint = baseEndpoint };
                return tempSettings.ChatCompletionsEndpoint;
            }
            catch (Exception ex)
            {
                RimAI.Framework.Core.RimAILogger.Error("Error getting API endpoint: {0}", ex.Message);
                return "https://api.openai.com/v1/chat/completions";
            }
        }

        /// <summary>
        /// 获取API密钥 - 优先级：设置对象 > 配置系统 > null（绝不返回空字符串作为默认值）
        /// </summary>
        private string GetApiKey()
        {
            try
            {
                // 优先使用RimAISettings中的API key（用户在游戏中设置的）
                if (!string.IsNullOrEmpty(_settings?.apiKey))
                    return _settings.apiKey;
                
                // 然后检查配置系统
                var config = RimAIConfiguration.Instance;
                var configKey = config?.Get<string>("api.key");
                if (!string.IsNullOrEmpty(configKey))
                    return configKey;
                
                // 绝不返回空字符串作为默认值，返回null表示未配置
                return null;
            }
            catch
            {
                return _settings?.apiKey; // 即使出错也不返回空字符串默认值
            }
        }

        /// <summary>
        /// 获取模型名称 - 优先级：用户设置 > 配置系统 > 默认值
        /// </summary>
        private string GetModelName()
        {
            try
            {
                // 1. 最高优先级：用户在游戏设置中的配置
                if (!string.IsNullOrEmpty(_settings?.modelName))
                    return _settings.modelName;
                
                // 2. 次优先级：配置系统
                var config = RimAIConfiguration.Instance;
                var configModel = config?.Get<string>("api.model");
                if (!string.IsNullOrEmpty(configModel))
                    return configModel;
                
                // 3. 默认值
                return "gpt-4o";
            }
            catch
            {
                return _settings?.modelName ?? "gpt-4o";
            }
        }

        /// <summary>
        /// 应用全局默认值到请求 - 只在未指定时应用，优先级：用户设置 > 配置系统 > 默认值
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
                
                // 应用温度设置 - 优先级：用户设置 > 配置系统 > 默认值
                if (!request.Options.Temperature.HasValue)
                {
                    var defaultTemp = _settings?.temperature ?? config?.Get<float?>("api.temperature") ?? 0.7f;
                    request.Options.Temperature = defaultTemp;
                }
                
                // 应用最大令牌数 - 优先级：用户设置 > 配置系统 > 默认值
                if (!request.Options.MaxTokens.HasValue)
                {
                    var defaultTokens = _settings?.maxTokens ?? config?.Get<int?>("api.maxTokens") ?? 1000;
                    request.Options.MaxTokens = defaultTokens;
                }
                
                // 应用模型名称 - 使用专门的获取方法
                if (string.IsNullOrEmpty(request.Options.Model))
                {
                    request.Options.Model = GetModelName();
                }
                
                // 应用流式设置 - 优先级：用户设置 > 配置系统 > 默认值
                if (!request.Options.HasExplicitStreamingSetting)
                {
                    var defaultStreaming = _settings?.enableStreaming ?? config?.Get<bool?>("api.enableStreaming") ?? false;
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
                // 添加详细的调试日志
                RimAI.Framework.Core.RimAILogger.Debug("=== LLM Request Debug Info ===");
                RimAI.Framework.Core.RimAILogger.Debug($"Settings API Key: {(!string.IsNullOrEmpty(_settings?.apiKey) ? $"Set (length: {_settings.apiKey.Length})" : "Not Set")}");
                RimAI.Framework.Core.RimAILogger.Debug($"Settings Endpoint: {_settings?.apiEndpoint ?? "Not Set"}");
                RimAI.Framework.Core.RimAILogger.Debug($"Settings Model: {_settings?.modelName ?? "Not Set"}");
                RimAI.Framework.Core.RimAILogger.Debug($"Settings Temperature: {_settings?.temperature ?? -1}");
                RimAI.Framework.Core.RimAILogger.Debug($"Settings MaxTokens: {_settings?.maxTokens ?? -1}");
                
                // 应用全局默认值
                ApplyGlobalDefaults(request);
                
                // 记录最终使用的值
                RimAI.Framework.Core.RimAILogger.Debug($"Final API Key: {(!string.IsNullOrEmpty(GetApiKey()) ? $"Set (length: {GetApiKey().Length})" : "Not Set")}");
                RimAI.Framework.Core.RimAILogger.Debug($"Final Endpoint: {GetApiEndpoint()}");
                RimAI.Framework.Core.RimAILogger.Debug($"Final Model: {request.Options?.Model ?? "Not Set"}");
                RimAI.Framework.Core.RimAILogger.Debug($"Final Temperature: {request.Options?.Temperature ?? -1}");
                RimAI.Framework.Core.RimAILogger.Debug($"Final MaxTokens: {request.Options?.MaxTokens ?? -1}");
                RimAI.Framework.Core.RimAILogger.Debug("==============================");
                
                // Validate request
                var validationResult = ValidateRequest(request);
                if (!validationResult.IsValid)
                {
                    RimAI.Framework.Core.RimAILogger.Error($"Request validation failed: {validationResult.Error}");
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
                        var chunk = JsonConvert.DeserializeObject<StreamingChatCompletionChunk>(jsonData);
                        var content = chunk?.choices?[0]?.delta?.content;
                        
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
            
            // 使用ApplyGlobalDefaults已经设置好的值，不再重复获取
            // 如果options中没有值，使用统一的获取方法确保正确的优先级
            var temperature = options.Temperature ?? _settings?.temperature ?? 0.7f;
            var model = options.Model ?? GetModelName(); // 使用统一的获取方法

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

            // 使用配置优先级方法获取API key和endpoint
            var apiKey = GetApiKey();
            if (string.IsNullOrEmpty(apiKey))
            {
                return (false, "API Key is not configured");
            }

            var apiEndpoint = GetApiEndpoint();
            if (string.IsNullOrEmpty(apiEndpoint))
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
                // 详细日志记录 - 帮助诊断连接问题
                Log.Message($"[RimAI] Sending HTTP request to: {endpoint}");
                Log.Message($"[RimAI] API Key configured: {!string.IsNullOrEmpty(apiKey)}");
                Log.Message($"[RimAI] Request body length: {jsonBody?.Length ?? 0}");

                using var request = new HttpRequestMessage(HttpMethod.Post, endpoint);
                request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", apiKey);
                request.Content = new StringContent(jsonBody, Encoding.UTF8, "application/json");

                // Add custom headers if any
                foreach (var header in _defaultHeaders)
                {
                    request.Headers.TryAddWithoutValidation(header.Key, header.Value.ToString());
                }

                Log.Message("[RimAI] Sending HTTP request...");
                using var response = await _httpClient.SendAsync(request, cancellationToken);
                var responseBody = await response.Content.ReadAsStringAsync();
                
                Log.Message($"[RimAI] HTTP response received. Status: {response.StatusCode}");
                Log.Message($"[RimAI] Response body length: {responseBody?.Length ?? 0}");
                
                if (response.IsSuccessStatusCode)
                {
                    Log.Message("[RimAI] HTTP request successful");
                    return (true, (int)response.StatusCode, responseBody, null);
                }
                else
                {
                    Log.Warning($"[RimAI] HTTP request failed. Status: {response.StatusCode}, Response: {responseBody}");
                    return (false, (int)response.StatusCode, null, responseBody);
                }
            }
            catch (TaskCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                // 用户取消请求，这是正常行为，不应该记录为错误
                Log.Message("[RimAI] HTTP request was cancelled by user");
                return (false, 0, null, "Request was cancelled");
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                // 用户取消请求，这是正常行为，不应该记录为错误
                Log.Message("[RimAI] HTTP request was cancelled by user");
                return (false, 0, null, "Request was cancelled");
            }
            catch (TaskCanceledException)
            {
                // 超时取消
                Log.Warning("[RimAI] HTTP request task timed out");
                return (false, 0, null, "Request timed out");
            }
            catch (OperationCanceledException)
            {
                // 超时取消
                Log.Warning("[RimAI] HTTP request timed out");
                return (false, 0, null, "Request timed out");
            }
            catch (Exception ex)
            {
                Log.Error($"[RimAI] HTTP request exception: {ex.Message}");
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
