using System;
using System.Runtime.Serialization;

namespace RimAI.Framework.Exceptions
{
    /// <summary>
    /// Exception thrown when network connection or HTTP communication errors occur.
    /// This includes timeouts, connection failures, DNS issues, and HTTP protocol errors.
    /// </summary>
    [Serializable]
    public class ConnectionException : RimAIException
    {
        #region Properties

        /// <summary>
        /// Gets the category of this exception.
        /// </summary>
        public override RimAIExceptionCategory Category => RimAIExceptionCategory.Connection;

        /// <summary>
        /// Gets whether this error is recoverable through retry.
        /// Most connection errors are recoverable with appropriate backoff.
        /// </summary>
        public override bool IsRecoverable => 
            ErrorCode != RimAIErrorCode.DNSResolutionFailed &&
            ErrorCode != RimAIErrorCode.SSLError;

        /// <summary>
        /// Gets the URL or endpoint that failed to connect.
        /// </summary>
        public string Endpoint { get; private set; }

        /// <summary>
        /// Gets the timeout value that was used, if applicable.
        /// </summary>
        public TimeSpan? TimeoutDuration { get; private set; }

        /// <summary>
        /// Gets the number of retry attempts made, if any.
        /// </summary>
        public int RetryAttempts { get; private set; }

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the ConnectionException class.
        /// </summary>
        /// <param name="errorCode">The specific connection error code.</param>
        /// <param name="message">The error message.</param>
        public ConnectionException(RimAIErrorCode errorCode, string message) 
            : base(errorCode, message)
        {
        }

        /// <summary>
        /// Initializes a new instance of the ConnectionException class with an inner exception.
        /// </summary>
        /// <param name="errorCode">The specific connection error code.</param>
        /// <param name="message">The error message.</param>
        /// <param name="innerException">The inner exception.</param>
        public ConnectionException(RimAIErrorCode errorCode, string message, Exception innerException) 
            : base(errorCode, message, innerException)
        {
        }

        /// <summary>
        /// Initializes a new instance of the ConnectionException class for deserialization.
        /// </summary>
        /// <param name="info">Serialization info.</param>
        /// <param name="context">Streaming context.</param>
        protected ConnectionException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
            Endpoint = info.GetString(nameof(Endpoint));
            var timeoutTicks = info.GetInt64(nameof(TimeoutDuration));
            TimeoutDuration = timeoutTicks == 0 ? null : new TimeSpan(timeoutTicks);
            RetryAttempts = info.GetInt32(nameof(RetryAttempts));
        }

        #endregion

        #region Factory Methods

        /// <summary>
        /// Creates a connection timeout exception.
        /// </summary>
        /// <param name="endpoint">The endpoint that timed out.</param>
        /// <param name="timeout">The timeout duration.</param>
        /// <param name="message">Optional custom message.</param>
        /// <returns>Connection exception instance.</returns>
        public static ConnectionException Timeout(string endpoint, TimeSpan timeout, string message = null)
        {
            var msg = message ?? $"Connection to {endpoint} timed out after {timeout.TotalSeconds:F1} seconds.";
            return new ConnectionException(RimAIErrorCode.ConnectionTimeout, msg)
                .WithEndpoint(endpoint)
                .WithTimeout(timeout);
        }

        /// <summary>
        /// Creates a connection refused exception.
        /// </summary>
        /// <param name="endpoint">The endpoint that refused connection.</param>
        /// <param name="message">Optional custom message.</param>
        /// <returns>Connection exception instance.</returns>
        public static ConnectionException Refused(string endpoint, string message = null)
        {
            var msg = message ?? $"Connection to {endpoint} was refused.";
            return new ConnectionException(RimAIErrorCode.ConnectionRefused, msg)
                .WithEndpoint(endpoint);
        }

        /// <summary>
        /// Creates a DNS resolution failed exception.
        /// </summary>
        /// <param name="hostname">The hostname that failed to resolve.</param>
        /// <param name="message">Optional custom message.</param>
        /// <param name="innerException">Optional inner exception.</param>
        /// <returns>Connection exception instance.</returns>
        public static ConnectionException DNSFailed(string hostname, string message = null, Exception innerException = null)
        {
            var msg = message ?? $"Failed to resolve hostname: {hostname}";
            return new ConnectionException(RimAIErrorCode.DNSResolutionFailed, msg, innerException)
                .WithEndpoint(hostname);
        }

        /// <summary>
        /// Creates an SSL/TLS error exception.
        /// </summary>
        /// <param name="endpoint">The endpoint where SSL failed.</param>
        /// <param name="sslError">The SSL error details.</param>
        /// <param name="message">Optional custom message.</param>
        /// <param name="innerException">Optional inner exception.</param>
        /// <returns>Connection exception instance.</returns>
        public static ConnectionException SSLError(string endpoint, string sslError = null, string message = null, Exception innerException = null)
        {
            var msg = message ?? $"SSL/TLS error connecting to {endpoint}";
            var ex = new ConnectionException(RimAIErrorCode.SSLError, msg, innerException)
                .WithEndpoint(endpoint);
            if (!string.IsNullOrEmpty(sslError))
            {
                ex.AddContext("SSLError", sslError);
            }
            return ex;
        }

        /// <summary>
        /// Creates an HTTP error exception.
        /// </summary>
        /// <param name="statusCode">The HTTP status code.</param>
        /// <param name="reasonPhrase">The HTTP reason phrase.</param>
        /// <param name="endpoint">The endpoint that returned the error.</param>
        /// <param name="message">Optional custom message.</param>
        /// <returns>Connection exception instance.</returns>
        public static ConnectionException HttpError(int statusCode, string reasonPhrase = null, string endpoint = null, string message = null)
        {
            var msg = message ?? $"HTTP error {statusCode}: {reasonPhrase ?? "Unknown error"}";
            var ex = new ConnectionException(RimAIErrorCode.HttpError, msg);
            
            ex.AddContext("HttpStatusCode", statusCode);
            if (!string.IsNullOrEmpty(reasonPhrase))
            {
                ex.AddContext("ReasonPhrase", reasonPhrase);
            }
            if (!string.IsNullOrEmpty(endpoint))
            {
                ex.WithEndpoint(endpoint);
            }
            return ex;
        }

        /// <summary>
        /// Creates a network unavailable exception.
        /// </summary>
        /// <param name="message">Optional custom message.</param>
        /// <param name="innerException">Optional inner exception.</param>
        /// <returns>Connection exception instance.</returns>
        public static ConnectionException NetworkUnavailable(string message = null, Exception innerException = null)
        {
            var msg = message ?? "Network is unavailable. Please check your internet connection.";
            return new ConnectionException(RimAIErrorCode.NetworkUnavailable, msg, innerException);
        }

        /// <summary>
        /// Creates a proxy error exception.
        /// </summary>
        /// <param name="proxyEndpoint">The proxy endpoint.</param>
        /// <param name="message">Optional custom message.</param>
        /// <param name="innerException">Optional inner exception.</param>
        /// <returns>Connection exception instance.</returns>
        public static ConnectionException ProxyError(string proxyEndpoint, string message = null, Exception innerException = null)
        {
            var msg = message ?? $"Proxy error connecting through {proxyEndpoint}";
            var ex = new ConnectionException(RimAIErrorCode.ProxyError, msg, innerException);
            ex.AddContext("ProxyEndpoint", proxyEndpoint);
            return ex;
        }

        /// <summary>
        /// Creates a connection pool exhausted exception.
        /// </summary>
        /// <param name="poolSize">The maximum pool size.</param>
        /// <param name="message">Optional custom message.</param>
        /// <returns>Connection exception instance.</returns>
        public static ConnectionException PoolExhausted(int poolSize, string message = null)
        {
            var msg = message ?? $"Connection pool exhausted. Maximum {poolSize} connections reached.";
            var ex = new ConnectionException(RimAIErrorCode.ConnectionPoolExhausted, msg);
            ex.AddContext("PoolSize", poolSize);
            return ex;
        }

        #endregion

        #region Configuration

        /// <summary>
        /// Sets the endpoint for this exception.
        /// </summary>
        /// <param name="endpoint">The endpoint URL or hostname.</param>
        /// <returns>This exception instance for method chaining.</returns>
        public ConnectionException WithEndpoint(string endpoint)
        {
            Endpoint = endpoint;
            if (!string.IsNullOrEmpty(endpoint))
            {
                AddContext("Endpoint", endpoint);
            }
            return this;
        }

        /// <summary>
        /// Sets the timeout duration for this exception.
        /// </summary>
        /// <param name="timeout">The timeout duration.</param>
        /// <returns>This exception instance for method chaining.</returns>
        public ConnectionException WithTimeout(TimeSpan timeout)
        {
            TimeoutDuration = timeout;
            AddContext("Timeout", timeout);
            return this;
        }

        /// <summary>
        /// Sets the retry attempts count for this exception.
        /// </summary>
        /// <param name="attempts">The number of retry attempts made.</param>
        /// <returns>This exception instance for method chaining.</returns>
        public ConnectionException WithRetryAttempts(int attempts)
        {
            RetryAttempts = attempts;
            AddContext("RetryAttempts", attempts);
            return this;
        }

        #endregion

        #region Overrides

        /// <summary>
        /// Initializes connection-specific context information.
        /// </summary>
        protected override void InitializeContext()
        {
            base.InitializeContext();
            AddContext("Component", "Connection");
        }

        /// <summary>
        /// Gets a user-friendly error message.
        /// </summary>
        /// <returns>User-friendly error message.</returns>
        public override string GetUserFriendlyMessage()
        {
            switch (ErrorCode)
            {
                case RimAIErrorCode.NetworkUnavailable:
                    return "No internet connection available. Please check your network settings.";
                case RimAIErrorCode.ConnectionTimeout:
                    return "The connection timed out. The server may be busy or unreachable.";
                case RimAIErrorCode.ConnectionRefused:
                    return "Connection was refused by the server. The service may be down.";
                case RimAIErrorCode.DNSResolutionFailed:
                    return "Could not find the server. Please check the server address in settings.";
                case RimAIErrorCode.SSLError:
                    return "Secure connection failed. There may be a certificate problem.";
                case RimAIErrorCode.HttpError:
                    var statusCode = GetContext<int>("HttpStatusCode");
                    if (statusCode >= 500)
                    {
                        return "The server encountered an error. Please try again later.";
                    }
                    else if (statusCode >= 400)
                    {
                        return "Request error. Please check your configuration.";
                    }
                    return "HTTP communication error occurred.";
                case RimAIErrorCode.ProxyError:
                    return "Proxy connection failed. Please check your proxy settings.";
                case RimAIErrorCode.ConnectionPoolExhausted:
                    return "Too many concurrent connections. Please try again in a moment.";
                default:
                    return "Connection error occurred. Please check your network and try again.";
            }
        }

        /// <summary>
        /// Populates serialization info with connection-specific data.
        /// </summary>
        /// <param name="info">Serialization info.</param>
        /// <param name="context">Streaming context.</param>
        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            base.GetObjectData(info, context);
            info.AddValue(nameof(Endpoint), Endpoint);
            info.AddValue(nameof(TimeoutDuration), TimeoutDuration?.Ticks ?? 0);
            info.AddValue(nameof(RetryAttempts), RetryAttempts);
        }

        #endregion
    }
}
