using System;
using System.Runtime.Serialization;

namespace RimAI.Framework.Exceptions
{
    /// <summary>
    /// Exception thrown when configuration errors occur.
    /// This includes missing config files, invalid settings, validation failures, etc.
    /// </summary>
    [Serializable]
    public class ConfigurationException : RimAIException
    {
        #region Properties

        /// <summary>
        /// Gets the category of this exception.
        /// </summary>
        public override RimAIExceptionCategory Category => RimAIExceptionCategory.Configuration;

        /// <summary>
        /// Gets whether this error is recoverable.
        /// Most configuration errors require user intervention.
        /// </summary>
        public override bool IsRecoverable => 
            ErrorCode == RimAIErrorCode.ConfigurationReadError;

        /// <summary>
        /// Gets the configuration file path that caused the error.
        /// </summary>
        public string ConfigurationPath { get; private set; }

        /// <summary>
        /// Gets the configuration key that caused the error.
        /// </summary>
        public string ConfigurationKey { get; private set; }

        /// <summary>
        /// Gets the invalid configuration value.
        /// </summary>
        public object InvalidValue { get; private set; }

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the ConfigurationException class.
        /// </summary>
        /// <param name="errorCode">The specific configuration error code.</param>
        /// <param name="message">The error message.</param>
        public ConfigurationException(RimAIErrorCode errorCode, string message) 
            : base(errorCode, message)
        {
        }

        /// <summary>
        /// Initializes a new instance of the ConfigurationException class with an inner exception.
        /// </summary>
        /// <param name="errorCode">The specific configuration error code.</param>
        /// <param name="message">The error message.</param>
        /// <param name="innerException">The inner exception.</param>
        public ConfigurationException(RimAIErrorCode errorCode, string message, Exception innerException) 
            : base(errorCode, message, innerException)
        {
        }

        /// <summary>
        /// Initializes a new instance of the ConfigurationException class for deserialization.
        /// </summary>
        /// <param name="info">Serialization info.</param>
        /// <param name="context">Streaming context.</param>
        protected ConfigurationException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
            ConfigurationPath = info.GetString(nameof(ConfigurationPath));
            ConfigurationKey = info.GetString(nameof(ConfigurationKey));
            InvalidValue = info.GetValue(nameof(InvalidValue), typeof(object));
        }

        #endregion

        #region Factory Methods

        /// <summary>
        /// Creates a configuration not found exception.
        /// </summary>
        /// <param name="configPath">The path to the missing configuration file.</param>
        /// <param name="message">Optional custom message.</param>
        /// <returns>Configuration exception instance.</returns>
        public static ConfigurationException NotFound(string configPath, string message = null)
        {
            var msg = message ?? $"Configuration file not found: {configPath}";
            return new ConfigurationException(RimAIErrorCode.ConfigurationNotFound, msg)
                .WithPath(configPath);
        }

        /// <summary>
        /// Creates a configuration invalid exception.
        /// </summary>
        /// <param name="configPath">The path to the invalid configuration file.</param>
        /// <param name="reason">The reason the configuration is invalid.</param>
        /// <param name="message">Optional custom message.</param>
        /// <param name="innerException">Optional inner exception.</param>
        /// <returns>Configuration exception instance.</returns>
        public static ConfigurationException Invalid(string configPath, string reason = null, string message = null, Exception innerException = null)
        {
            var msg = message ?? $"Configuration file is invalid: {configPath}";
            var ex = new ConfigurationException(RimAIErrorCode.ConfigurationInvalid, msg, innerException)
                .WithPath(configPath);
            if (!string.IsNullOrEmpty(reason))
            {
                ex.AddContext("Reason", reason);
            }
            return ex;
        }

        /// <summary>
        /// Creates a configuration read error exception.
        /// </summary>
        /// <param name="configPath">The path to the configuration file.</param>
        /// <param name="message">Optional custom message.</param>
        /// <param name="innerException">Optional inner exception.</param>
        /// <returns>Configuration exception instance.</returns>
        public static ConfigurationException ReadError(string configPath, string message = null, Exception innerException = null)
        {
            var msg = message ?? $"Failed to read configuration file: {configPath}";
            return new ConfigurationException(RimAIErrorCode.ConfigurationReadError, msg, innerException)
                .WithPath(configPath);
        }

        /// <summary>
        /// Creates a configuration write error exception.
        /// </summary>
        /// <param name="configPath">The path to the configuration file.</param>
        /// <param name="message">Optional custom message.</param>
        /// <param name="innerException">Optional inner exception.</param>
        /// <returns>Configuration exception instance.</returns>
        public static ConfigurationException WriteError(string configPath, string message = null, Exception innerException = null)
        {
            var msg = message ?? $"Failed to write configuration file: {configPath}";
            return new ConfigurationException(RimAIErrorCode.ConfigurationWriteError, msg, innerException)
                .WithPath(configPath);
        }

        /// <summary>
        /// Creates a configuration validation failed exception.
        /// </summary>
        /// <param name="configKey">The configuration key that failed validation.</param>
        /// <param name="invalidValue">The invalid value.</param>
        /// <param name="validationRule">The validation rule that was violated.</param>
        /// <param name="message">Optional custom message.</param>
        /// <returns>Configuration exception instance.</returns>
        public static ConfigurationException ValidationFailed(string configKey, object invalidValue, string validationRule = null, string message = null)
        {
            var msg = message ?? $"Configuration validation failed for '{configKey}': {invalidValue}";
            var ex = new ConfigurationException(RimAIErrorCode.ConfigurationValidationFailed, msg)
                .WithKey(configKey)
                .WithInvalidValue(invalidValue);
            
            if (!string.IsNullOrEmpty(validationRule))
            {
                ex.AddContext("ValidationRule", validationRule);
            }
            return ex;
        }

        /// <summary>
        /// Creates a configuration version mismatch exception.
        /// </summary>
        /// <param name="configPath">The configuration file path.</param>
        /// <param name="expectedVersion">The expected version.</param>
        /// <param name="actualVersion">The actual version found.</param>
        /// <param name="message">Optional custom message.</param>
        /// <returns>Configuration exception instance.</returns>
        public static ConfigurationException VersionMismatch(string configPath, string expectedVersion, string actualVersion, string message = null)
        {
            var msg = message ?? $"Configuration version mismatch in {configPath}. Expected: {expectedVersion}, Found: {actualVersion}";
            var ex = new ConfigurationException(RimAIErrorCode.ConfigurationVersionMismatch, msg);
            ex.WithPath(configPath);
            ex.AddContext("ExpectedVersion", expectedVersion);
            ex.AddContext("ActualVersion", actualVersion);
            return ex;
        }

        /// <summary>
        /// Creates a required field missing exception.
        /// </summary>
        /// <param name="fieldName">The name of the missing field.</param>
        /// <param name="configPath">Optional configuration file path.</param>
        /// <param name="message">Optional custom message.</param>
        /// <returns>Configuration exception instance.</returns>
        public static ConfigurationException RequiredFieldMissing(string fieldName, string configPath = null, string message = null)
        {
            var msg = message ?? $"Required configuration field '{fieldName}' is missing";
            var ex = new ConfigurationException(RimAIErrorCode.RequiredFieldMissing, msg)
                .WithKey(fieldName);
            
            if (!string.IsNullOrEmpty(configPath))
            {
                ex.WithPath(configPath);
            }
            return ex;
        }

        #endregion

        #region Configuration

        /// <summary>
        /// Sets the configuration file path for this exception.
        /// </summary>
        /// <param name="path">The configuration file path.</param>
        /// <returns>This exception instance for method chaining.</returns>
        public ConfigurationException WithPath(string path)
        {
            ConfigurationPath = path;
            if (!string.IsNullOrEmpty(path))
            {
                AddContext("ConfigurationPath", path);
            }
            return this;
        }

        /// <summary>
        /// Sets the configuration key for this exception.
        /// </summary>
        /// <param name="key">The configuration key.</param>
        /// <returns>This exception instance for method chaining.</returns>
        public ConfigurationException WithKey(string key)
        {
            ConfigurationKey = key;
            if (!string.IsNullOrEmpty(key))
            {
                AddContext("ConfigurationKey", key);
            }
            return this;
        }

        /// <summary>
        /// Sets the invalid value for this exception.
        /// </summary>
        /// <param name="value">The invalid value.</param>
        /// <returns>This exception instance for method chaining.</returns>
        public ConfigurationException WithInvalidValue(object value)
        {
            InvalidValue = value;
            if (value != null)
            {
                AddContext("InvalidValue", value);
            }
            return this;
        }

        #endregion

        #region Overrides

        /// <summary>
        /// Initializes configuration-specific context information.
        /// </summary>
        protected override void InitializeContext()
        {
            base.InitializeContext();
            AddContext("Component", "Configuration");
        }

        /// <summary>
        /// Gets a user-friendly error message.
        /// </summary>
        /// <returns>User-friendly error message.</returns>
        public override string GetUserFriendlyMessage()
        {
            switch (ErrorCode)
            {
                case RimAIErrorCode.ConfigurationNotFound:
                    return "Configuration file is missing. The mod may need to be reinstalled or reconfigured.";
                case RimAIErrorCode.ConfigurationInvalid:
                    return "Configuration file contains errors. Please check the mod settings or reset to defaults.";
                case RimAIErrorCode.ConfigurationReadError:
                    return "Cannot read configuration file. Check file permissions and disk space.";
                case RimAIErrorCode.ConfigurationWriteError:
                    return "Cannot save configuration changes. Check file permissions and disk space.";
                case RimAIErrorCode.ConfigurationValidationFailed:
                    var key = ConfigurationKey ?? "setting";
                    return $"Invalid value for '{key}'. Please check the mod settings.";
                case RimAIErrorCode.ConfigurationVersionMismatch:
                    return "Configuration file is from a different mod version. Settings may need to be reset.";
                case RimAIErrorCode.RequiredFieldMissing:
                    var field = ConfigurationKey ?? "setting";
                    return $"Required setting '{field}' is missing. Please check the mod configuration.";
                default:
                    return "Configuration error occurred. Please check the mod settings.";
            }
        }

        /// <summary>
        /// Populates serialization info with configuration-specific data.
        /// </summary>
        /// <param name="info">Serialization info.</param>
        /// <param name="context">Streaming context.</param>
        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            base.GetObjectData(info, context);
            info.AddValue(nameof(ConfigurationPath), ConfigurationPath);
            info.AddValue(nameof(ConfigurationKey), ConfigurationKey);
            info.AddValue(nameof(InvalidValue), InvalidValue);
        }

        #endregion
    }
}
