using System;
using log4net;
using log4net.Core;

namespace QANinjaAdapter.Logging
{
    public static class AppLogger
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(AppLogger));

        public static void Initialize()
        {
            // Initialize log4net if needed
            log4net.Config.XmlConfigurator.Configure();
        }

        // Original method signature
        public static void Log(string message, LoggingLevel level = null)
        {
            level = level ?? LoggingLevel.Information;

            if (level == LoggingLevel.Information)
            {
                Logger.Info(message);
            }
            else if (level == LoggingLevel.Warning)
            {
                Logger.Warn(message);
            }
            else if (level == LoggingLevel.Error)
            {
                Logger.Error(message);
            }
            else if (level == LoggingLevel.Debug)
            {
                Logger.Debug(message);
            }
        }

        // Overload with reversed parameters to match existing code
        public static void Log(LoggingLevel level, string message)
        {
            Log(message, level);
        }
        
        // Support for LogLevel class
        public static void Log(string message, LogLevel level)
        {
            if (level == LogLevel.Information)
            {
                Logger.Info(message);
            }
            else if (level == LogLevel.Warning)
            {
                Logger.Warn(message);
            }
            else if (level == LogLevel.Error)
            {
                Logger.Error(message);
            }
            else if (level == LogLevel.Debug)
            {
                Logger.Debug(message);
            }
        }
        
        // Overload with reversed parameters for LogLevel
        public static void Log(LogLevel level, string message)
        {
            Log(message, level);
        }

        public static void Info(string message)
        {
            Logger.Info(message);
        }

        public static void Warning(string message)
        {
            Logger.Warn(message);
        }

        public static void Error(string message, Exception ex = null)
        {
            if (ex != null)
            {
                Logger.Error(message, ex);
            }
            else
            {
                Logger.Error(message);
            }
        }

        public static void Debug(string message)
        {
            Logger.Debug(message);
        }
    }

    public class LogLevel
    {
        public static readonly LogLevel Information = new LogLevel();
        public static readonly LogLevel Warning = new LogLevel();
        public static readonly LogLevel Error = new LogLevel();
        public static readonly LogLevel Debug = new LogLevel();
    }
}
