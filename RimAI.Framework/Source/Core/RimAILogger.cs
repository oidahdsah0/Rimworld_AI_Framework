using System;
using Verse;

namespace RimAI.Framework.Core
{
    /// <summary>
    /// RimAI框架的日志记录帮助类，支持不同的日志级别和控制
    /// </summary>
    public static class RimAILogger
    {
        /// <summary>
        /// 日志级别枚举
        /// </summary>
        public enum LogLevel
        {
            Debug = 0,
            Info = 1,
            Warning = 2,
            Error = 3
        }
        
        /// <summary>
        /// 当前日志级别，可以在运行时修改
        /// </summary>
        public static LogLevel CurrentLogLevel { get; set; } = LogLevel.Info;
        
        /// <summary>
        /// 是否启用详细日志（包括Debug级别）
        /// </summary>
        public static bool EnableVerboseLogging { get; set; } = false;
        
        /// <summary>
        /// 日志前缀
        /// </summary>
        private const string LOG_PREFIX = "[RimAI]";
        
        /// <summary>
        /// 记录调试信息
        /// </summary>
        public static void Debug(string message, params object[] args)
        {
            if (ShouldLog(LogLevel.Debug))
            {
                var formattedMessage = FormatMessage("DEBUG", message, args);
                Log.Message(formattedMessage);
            }
        }
        
        /// <summary>
        /// 记录常规信息
        /// </summary>
        public static void Info(string message, params object[] args)
        {
            if (ShouldLog(LogLevel.Info))
            {
                var formattedMessage = FormatMessage("INFO", message, args);
                Log.Message(formattedMessage);
            }
        }
        
        /// <summary>
        /// 记录警告信息
        /// </summary>
        public static void Warning(string message, params object[] args)
        {
            if (ShouldLog(LogLevel.Warning))
            {
                var formattedMessage = FormatMessage("WARN", message, args);
                Log.Warning(formattedMessage);
            }
        }
        
        /// <summary>
        /// 记录错误信息
        /// </summary>
        public static void Error(string message, params object[] args)
        {
            if (ShouldLog(LogLevel.Error))
            {
                var formattedMessage = FormatMessage("ERROR", message, args);
                Log.Error(formattedMessage);
            }
        }
        
        /// <summary>
        /// 记录异常信息
        /// </summary>
        public static void Exception(Exception ex, string message = null, params object[] args)
        {
            if (ShouldLog(LogLevel.Error))
            {
                var baseMessage = string.IsNullOrEmpty(message) ? "Exception occurred" : string.Format(message, args);
                var formattedMessage = FormatMessage("ERROR", $"{baseMessage}: {ex.Message}");
                
                if (EnableVerboseLogging)
                {
                    formattedMessage += $"\nStack trace: {ex.StackTrace}";
                }
                
                Log.Error(formattedMessage);
            }
        }
        
        /// <summary>
        /// 检查是否应该记录指定级别的日志
        /// </summary>
        private static bool ShouldLog(LogLevel level)
        {
            if (EnableVerboseLogging && level == LogLevel.Debug)
                return true;
                
            return level >= CurrentLogLevel;
        }
        
        /// <summary>
        /// 格式化日志消息
        /// </summary>
        private static string FormatMessage(string level, string message, params object[] args)
        {
            try
            {
                var formattedMessage = args.Length > 0 ? string.Format(message, args) : message;
                return $"{LOG_PREFIX} [{level}] {formattedMessage}";
            }
            catch (FormatException)
            {
                // 如果格式化失败，返回原始消息
                return $"{LOG_PREFIX} [{level}] {message} (Format args: {string.Join(", ", args)})";
            }
        }
        
        /// <summary>
        /// 设置日志配置
        /// </summary>
        public static void Configure(LogLevel logLevel, bool verboseLogging = false)
        {
            CurrentLogLevel = logLevel;
            EnableVerboseLogging = verboseLogging;
            Info("RimAI logging configured - Level: {0}, Verbose: {1}", logLevel, verboseLogging);
        }
        
        /// <summary>
        /// 获取当前日志配置信息
        /// </summary>
        public static string GetLogConfiguration()
        {
            return $"Current log level: {CurrentLogLevel}, Verbose logging: {EnableVerboseLogging}";
        }
    }
}
