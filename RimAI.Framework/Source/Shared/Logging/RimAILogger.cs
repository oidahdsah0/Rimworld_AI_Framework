using Verse;

namespace RimAI.Framework.Shared.Logging
{

    // 定义公共的、静态的日志记录器类
    public static class RimAILogger
    {
        // 定义一个私有的、常量的日志前缀字符串
        private const string LogPrefix = "[RimAI.Framework]";

        // 创建一个公共的、静态的方法，用于记录普通信息
        public static void Log(string message)
        {
            // 使用Rimworld的Log方法记录日志, 使用LogPrefix作为前缀
            Verse.Log.Message(LogPrefix + message);
        }

        // 创建一个公共的、静态的方法，用于记录警告信息
        public static void Warning(string message)
        {
            // 使用Rimworld的Log方法记录日志, 使用LogPrefix作为前缀
            Verse.Log.Warning(LogPrefix + message);
        }

        // 创建一个公共的、静态的方法，用于记录错误信息
        public static void Error(string message)
        {
            // 使用Rimworld的Log方法记录日志, 使用LogPrefix作为前缀
            Verse.Log.Error(LogPrefix + message);
        }
    }
}