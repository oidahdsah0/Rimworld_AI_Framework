using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;

namespace RimAI.Framework.Exceptions
{
    /// <summary>
    /// Base exception class for all RimAI Framework exceptions.
    /// Provides structured error handling with error codes, context information, and serialization support.
    /// </summary>
    [Serializable]
    public abstract class RimAIException : Exception
    {
        #region Properties

        /// <summary>
        /// Gets the error code that identifies the specific type of error.
        /// </summary>
        public RimAIErrorCode ErrorCode { get; private set; }

        /// <summary>
        /// Gets additional context information about the error.
        /// </summary>
        public Dictionary<string, object> Context { get; private set; }

        /// <summary>
        /// Gets the timestamp when the error occurred.
        /// </summary>
        public DateTime Timestamp { get; private set; }

        /// <summary>
        /// Gets whether this error is considered recoverable.
        /// </summary>
        public virtual bool IsRecoverable => false;

        /// <summary>
        /// Gets the category of this exception for logging and handling purposes.
        /// </summary>
        public abstract RimAIExceptionCategory Category { get; }

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the RimAIException class.
        /// </summary>
        /// <param name="errorCode">The error code that identifies the specific error.</param>
        /// <param name="message">The error message.</param>
        protected RimAIException(RimAIErrorCode errorCode, string message) 
            : base(message)
        {
            ErrorCode = errorCode;
            Context = new Dictionary<string, object>();
            Timestamp = DateTime.UtcNow;
            InitializeContext();
        }

        /// <summary>
        /// Initializes a new instance of the RimAIException class with an inner exception.
        /// </summary>
        /// <param name="errorCode">The error code that identifies the specific error.</param>
        /// <param name="message">The error message.</param>
        /// <param name="innerException">The inner exception that caused this error.</param>
        protected RimAIException(RimAIErrorCode errorCode, string message, Exception innerException) 
            : base(message, innerException)
        {
            ErrorCode = errorCode;
            Context = new Dictionary<string, object>();
            Timestamp = DateTime.UtcNow;
            InitializeContext();
        }

        /// <summary>
        /// Initializes a new instance of the RimAIException class for deserialization.
        /// </summary>
        /// <param name="info">Serialization info.</param>
        /// <param name="context">Streaming context.</param>
        protected RimAIException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
            ErrorCode = (RimAIErrorCode)info.GetValue(nameof(ErrorCode), typeof(RimAIErrorCode));
            Context = (Dictionary<string, object>)info.GetValue(nameof(Context), typeof(Dictionary<string, object>)) ?? new Dictionary<string, object>();
            Timestamp = info.GetDateTime(nameof(Timestamp));
        }

        #endregion

        #region Context Management

        /// <summary>
        /// Adds context information to this exception.
        /// </summary>
        /// <param name="key">The context key.</param>
        /// <param name="value">The context value.</param>
        /// <returns>This exception instance for method chaining.</returns>
        public RimAIException AddContext(string key, object value)
        {
            if (!string.IsNullOrEmpty(key) && value != null)
            {
                Context[key] = value;
            }
            return this;
        }

        /// <summary>
        /// Adds multiple context entries to this exception.
        /// </summary>
        /// <param name="contextData">Dictionary of context data to add.</param>
        /// <returns>This exception instance for method chaining.</returns>
        public RimAIException AddContext(Dictionary<string, object> contextData)
        {
            if (contextData != null)
            {
                foreach (var kvp in contextData)
                {
                    AddContext(kvp.Key, kvp.Value);
                }
            }
            return this;
        }

        /// <summary>
        /// Gets a context value by key.
        /// </summary>
        /// <typeparam name="T">The expected type of the value.</typeparam>
        /// <param name="key">The context key.</param>
        /// <returns>The context value, or default(T) if not found.</returns>
        public T GetContext<T>(string key)
        {
            if (Context.TryGetValue(key, out var value) && value is T typedValue)
            {
                return typedValue;
            }
            return default(T);
        }

        #endregion

        #region Logging Support

        /// <summary>
        /// Gets a formatted string representation of this exception suitable for logging.
        /// </summary>
        /// <returns>Formatted log message.</returns>
        public virtual string ToLogString()
        {
            var contextInfo = Context.Count > 0 ? 
                $" | Context: {string.Join(", ", Context.Select(kvp => $"{kvp.Key}={kvp.Value}"))}" : 
                "";

            return $"[{Category}] {ErrorCode}: {Message}{contextInfo} | Timestamp: {Timestamp:yyyy-MM-dd HH:mm:ss} UTC";
        }

        /// <summary>
        /// Gets detailed information about this exception for diagnostic purposes.
        /// </summary>
        /// <returns>Detailed diagnostic information.</returns>
        public virtual Dictionary<string, object> GetDiagnosticInfo()
        {
            var info = new Dictionary<string, object>
            {
                ["ErrorCode"] = ErrorCode.ToString(),
                ["Category"] = Category.ToString(),
                ["Message"] = Message,
                ["IsRecoverable"] = IsRecoverable,
                ["Timestamp"] = Timestamp,
                ["StackTrace"] = StackTrace
            };

            // Add context
            if (Context.Count > 0)
            {
                info["Context"] = new Dictionary<string, object>(Context);
            }

            // Add inner exception info if present
            if (InnerException != null)
            {
                info["InnerException"] = new Dictionary<string, object>
                {
                    ["Type"] = InnerException.GetType().Name,
                    ["Message"] = InnerException.Message,
                    ["StackTrace"] = InnerException.StackTrace
                };
            }

            return info;
        }

        #endregion

        #region Serialization

        /// <summary>
        /// Populates the serialization info with exception data.
        /// </summary>
        /// <param name="info">Serialization info.</param>
        /// <param name="context">Streaming context.</param>
        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            base.GetObjectData(info, context);
            info.AddValue(nameof(ErrorCode), ErrorCode);
            info.AddValue(nameof(Context), Context);
            info.AddValue(nameof(Timestamp), Timestamp);
        }

        #endregion

        #region Helper Methods

        /// <summary>
        /// Called during construction to initialize context with common information.
        /// Override in derived classes to add specific context.
        /// </summary>
        protected virtual void InitializeContext()
        {
            Context["ExceptionType"] = GetType().Name;
            Context["Thread"] = System.Threading.Thread.CurrentThread.ManagedThreadId;
        }

        /// <summary>
        /// Creates a user-friendly error message for display to end users.
        /// Override in derived classes to provide specific user messages.
        /// </summary>
        /// <returns>User-friendly error message.</returns>
        public virtual string GetUserFriendlyMessage()
        {
            return Message;
        }

        #endregion
    }

    /// <summary>
    /// Categories for RimAI exceptions to help with handling and logging.
    /// </summary>
    public enum RimAIExceptionCategory
    {
        /// <summary>Unknown or uncategorized exception.</summary>
        Unknown,
        /// <summary>LLM service related exceptions.</summary>
        LLM,
        /// <summary>Network connection related exceptions.</summary>
        Connection,
        /// <summary>Configuration related exceptions.</summary>
        Configuration,
        /// <summary>Caching system related exceptions.</summary>
        Cache,
        /// <summary>Request batching related exceptions.</summary>
        Batching,
        /// <summary>Resource lifecycle related exceptions.</summary>
        Lifecycle,
        /// <summary>Validation related exceptions.</summary>
        Validation,
        /// <summary>Security related exceptions.</summary>
        Security
    }

    /// <summary>
    /// Enumeration of specific error codes for structured error handling.
    /// </summary>
    public enum RimAIErrorCode
    {
        // General errors (1000-1999)
        Unknown = 1000,
        InvalidOperation = 1001,
        InvalidArgument = 1002,
        NullReference = 1003,
        NotInitialized = 1004,
        AlreadyInitialized = 1005,
        ResourceNotAvailable = 1006,
        OperationCancelled = 1007,
        Timeout = 1008,

        // LLM Service errors (2000-2999)
        LLMServiceUnavailable = 2000,
        LLMInvalidResponse = 2001,
        LLMRateLimitExceeded = 2002,
        LLMAuthenticationFailed = 2003,
        LLMQuotaExceeded = 2004,
        LLMInvalidModel = 2005,
        LLMTokenLimitExceeded = 2006,
        LLMContentFiltered = 2007,
        LLMRequestTooLarge = 2008,

        // Connection errors (3000-3999)
        NetworkUnavailable = 3000,
        ConnectionTimeout = 3001,
        ConnectionRefused = 3002,
        DNSResolutionFailed = 3003,
        SSLError = 3004,
        HttpError = 3005,
        ProxyError = 3006,
        ConnectionPoolExhausted = 3007,

        // Configuration errors (4000-4999)
        ConfigurationNotFound = 4000,
        ConfigurationInvalid = 4001,
        ConfigurationReadError = 4002,
        ConfigurationWriteError = 4003,
        ConfigurationValidationFailed = 4004,
        ConfigurationVersionMismatch = 4005,

        // Cache errors (5000-5999)
        CacheError = 5000,
        CacheKeyNotFound = 5001,
        CacheMemoryExceeded = 5002,
        CacheCorruption = 5003,
        CacheEvictionFailed = 5004,

        // Batching errors (6000-6999)
        BatchingError = 6000,
        BatchSizeExceeded = 6001,
        BatchTimeout = 6002,
        BatchProcessingFailed = 6003,

        // Lifecycle errors (7000-7999)
        LifecycleError = 7000,
        ResourceLeakDetected = 7001,
        ShutdownTimeout = 7002,
        HealthCheckFailed = 7003,

        // Validation errors (8000-8999)
        ValidationFailed = 8000,
        InvalidFormat = 8001,
        ValueOutOfRange = 8002,
        RequiredFieldMissing = 8003,

        // Security errors (9000-9999)
        SecurityError = 9000,
        AuthenticationRequired = 9001,
        AuthorizationDenied = 9002,
        SecurityTokenExpired = 9003,
        SecurityValidationFailed = 9004
    }
}
