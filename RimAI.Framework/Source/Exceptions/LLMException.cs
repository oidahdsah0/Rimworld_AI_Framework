using System;
using System.Runtime.Serialization;

namespace RimAI.Framework.Exceptions
{
    /// <summary>
    /// Exception thrown when LLM service encounters errors.
    /// This includes API errors, model errors, and service unavailability.
    /// </summary>
    [Serializable]
    public class LLMException : RimAIException
    {
        #region Properties

        /// <summary>
        /// Gets the category of this exception.
        /// </summary>
        public override RimAIExceptionCategory Category => RimAIExceptionCategory.LLM;

        /// <summary>
        /// Gets whether this error is recoverable through retry or other means.
        /// </summary>
        public override bool IsRecoverable => 
            ErrorCode == RimAIErrorCode.LLMRateLimitExceeded ||
            ErrorCode == RimAIErrorCode.ConnectionTimeout ||
            ErrorCode == RimAIErrorCode.LLMServiceUnavailable;

        /// <summary>
        /// Gets the HTTP status code if this was an HTTP-related error.
        /// </summary>
        public int? HttpStatusCode { get; private set; }

        /// <summary>
        /// Gets the model that was being used when the error occurred.
        /// </summary>
        public string Model { get; private set; }

        /// <summary>
        /// Gets the request ID if available from the service.
        /// </summary>
        public string RequestId { get; private set; }

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the LLMException class.
        /// </summary>
        /// <param name="errorCode">The specific LLM error code.</param>
        /// <param name="message">The error message.</param>
        public LLMException(RimAIErrorCode errorCode, string message) 
            : base(errorCode, message)
        {
        }

        /// <summary>
        /// Initializes a new instance of the LLMException class with an inner exception.
        /// </summary>
        /// <param name="errorCode">The specific LLM error code.</param>
        /// <param name="message">The error message.</param>
        /// <param name="innerException">The inner exception.</param>
        public LLMException(RimAIErrorCode errorCode, string message, Exception innerException) 
            : base(errorCode, message, innerException)
        {
        }

        /// <summary>
        /// Initializes a new instance of the LLMException class for deserialization.
        /// </summary>
        /// <param name="info">Serialization info.</param>
        /// <param name="context">Streaming context.</param>
        protected LLMException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
            HttpStatusCode = (int?)info.GetValue(nameof(HttpStatusCode), typeof(int?));
            Model = info.GetString(nameof(Model));
            RequestId = info.GetString(nameof(RequestId));
        }

        #endregion

        #region Factory Methods

        /// <summary>
        /// Creates a service unavailable exception.
        /// </summary>
        /// <param name="message">Optional custom message.</param>
        /// <param name="innerException">Optional inner exception.</param>
        /// <returns>LLM exception instance.</returns>
        public static LLMException ServiceUnavailable(string message = null, Exception innerException = null)
        {
            var msg = message ?? "The LLM service is currently unavailable. Please try again later.";
            return new LLMException(RimAIErrorCode.LLMServiceUnavailable, msg, innerException);
        }

        /// <summary>
        /// Creates a rate limit exceeded exception.
        /// </summary>
        /// <param name="retryAfter">Number of seconds to wait before retrying.</param>
        /// <param name="message">Optional custom message.</param>
        /// <returns>LLM exception instance.</returns>
        public static LLMException RateLimitExceeded(int? retryAfter = null, string message = null)
        {
            var msg = message ?? "Rate limit exceeded. Please slow down your requests.";
            var ex = new LLMException(RimAIErrorCode.LLMRateLimitExceeded, msg);
            if (retryAfter.HasValue)
            {
                ex.AddContext("RetryAfter", retryAfter.Value);
            }
            return ex;
        }

        /// <summary>
        /// Creates an authentication failed exception.
        /// </summary>
        /// <param name="message">Optional custom message.</param>
        /// <returns>LLM exception instance.</returns>
        public static LLMException AuthenticationFailed(string message = null)
        {
            var msg = message ?? "Authentication with the LLM service failed. Check your API key and settings.";
            return new LLMException(RimAIErrorCode.LLMAuthenticationFailed, msg);
        }

        /// <summary>
        /// Creates an invalid response exception.
        /// </summary>
        /// <param name="response">The invalid response received.</param>
        /// <param name="message">Optional custom message.</param>
        /// <returns>LLM exception instance.</returns>
        public static LLMException InvalidResponse(string response = null, string message = null)
        {
            var msg = message ?? "Received an invalid or unexpected response from the LLM service.";
            var ex = new LLMException(RimAIErrorCode.LLMInvalidResponse, msg);
            if (!string.IsNullOrEmpty(response))
            {
                ex.AddContext("Response", response);
            }
            return ex;
        }

        /// <summary>
        /// Creates a quota exceeded exception.
        /// </summary>
        /// <param name="quotaType">The type of quota that was exceeded.</param>
        /// <param name="message">Optional custom message.</param>
        /// <returns>LLM exception instance.</returns>
        public static LLMException QuotaExceeded(string quotaType = null, string message = null)
        {
            var msg = message ?? "Usage quota has been exceeded.";
            var ex = new LLMException(RimAIErrorCode.LLMQuotaExceeded, msg);
            if (!string.IsNullOrEmpty(quotaType))
            {
                ex.AddContext("QuotaType", quotaType);
            }
            return ex;
        }

        /// <summary>
        /// Creates a token limit exceeded exception.
        /// </summary>
        /// <param name="tokenCount">The number of tokens in the request.</param>
        /// <param name="maxTokens">The maximum allowed tokens.</param>
        /// <param name="message">Optional custom message.</param>
        /// <returns>LLM exception instance.</returns>
        public static LLMException TokenLimitExceeded(int? tokenCount = null, int? maxTokens = null, string message = null)
        {
            var msg = message ?? "Request exceeds the maximum token limit.";
            var ex = new LLMException(RimAIErrorCode.LLMTokenLimitExceeded, msg);
            if (tokenCount.HasValue)
            {
                ex.AddContext("TokenCount", tokenCount.Value);
            }
            if (maxTokens.HasValue)
            {
                ex.AddContext("MaxTokens", maxTokens.Value);
            }
            return ex;
        }

        /// <summary>
        /// Creates a content filtered exception.
        /// </summary>
        /// <param name="reason">The reason the content was filtered.</param>
        /// <param name="message">Optional custom message.</param>
        /// <returns>LLM exception instance.</returns>
        public static LLMException ContentFiltered(string reason = null, string message = null)
        {
            var msg = message ?? "Request was blocked by content filtering.";
            var ex = new LLMException(RimAIErrorCode.LLMContentFiltered, msg);
            if (!string.IsNullOrEmpty(reason))
            {
                ex.AddContext("FilterReason", reason);
            }
            return ex;
        }

        #endregion

        #region Configuration

        /// <summary>
        /// Sets the HTTP status code for this exception.
        /// </summary>
        /// <param name="statusCode">The HTTP status code.</param>
        /// <returns>This exception instance for method chaining.</returns>
        public LLMException WithHttpStatus(int statusCode)
        {
            HttpStatusCode = statusCode;
            AddContext("HttpStatusCode", statusCode);
            return this;
        }

        /// <summary>
        /// Sets the model that was being used when the error occurred.
        /// </summary>
        /// <param name="model">The model identifier.</param>
        /// <returns>This exception instance for method chaining.</returns>
        public LLMException WithModel(string model)
        {
            Model = model;
            if (!string.IsNullOrEmpty(model))
            {
                AddContext("Model", model);
            }
            return this;
        }

        /// <summary>
        /// Sets the request ID for this exception.
        /// </summary>
        /// <param name="requestId">The request identifier.</param>
        /// <returns>This exception instance for method chaining.</returns>
        public LLMException WithRequestId(string requestId)
        {
            RequestId = requestId;
            if (!string.IsNullOrEmpty(requestId))
            {
                AddContext("RequestId", requestId);
            }
            return this;
        }

        #endregion

        #region Overrides

        /// <summary>
        /// Initializes LLM-specific context information.
        /// </summary>
        protected override void InitializeContext()
        {
            base.InitializeContext();
            AddContext("Component", "LLM");
        }

        /// <summary>
        /// Gets a user-friendly error message.
        /// </summary>
        /// <returns>User-friendly error message.</returns>
        public override string GetUserFriendlyMessage()
        {
            switch (ErrorCode)
            {
                case RimAIErrorCode.LLMServiceUnavailable:
                    return "The AI service is temporarily unavailable. Please try again in a few minutes.";
                case RimAIErrorCode.LLMRateLimitExceeded:
                    var retryAfter = GetContext<int>("RetryAfter");
                    if (retryAfter > 0)
                    {
                        return $"You're sending requests too quickly. Please wait {retryAfter} seconds before trying again.";
                    }
                    return "You're sending requests too quickly. Please slow down and try again.";
                case RimAIErrorCode.LLMAuthenticationFailed:
                    return "Authentication failed. Please check your API key in the mod settings.";
                case RimAIErrorCode.LLMQuotaExceeded:
                    return "Your usage quota has been exceeded. Please check your account limits.";
                case RimAIErrorCode.LLMTokenLimitExceeded:
                    return "Your request is too long. Please try with a shorter message.";
                case RimAIErrorCode.LLMContentFiltered:
                    return "Your request was blocked by content filters. Please modify your message.";
                default:
                    return "An AI service error occurred. Please try again or check your settings.";
            }
        }

        /// <summary>
        /// Populates serialization info with LLM-specific data.
        /// </summary>
        /// <param name="info">Serialization info.</param>
        /// <param name="context">Streaming context.</param>
        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            base.GetObjectData(info, context);
            info.AddValue(nameof(HttpStatusCode), HttpStatusCode);
            info.AddValue(nameof(Model), Model);
            info.AddValue(nameof(RequestId), RequestId);
        }

        #endregion
    }
}
