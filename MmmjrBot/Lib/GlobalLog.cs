using DreamPoeBot.Loki.Common;
using log4net;

namespace MmmjrBot.Lib
{
    public static class GlobalLog
    {
        private static readonly ILog Log = Logger.GetLoggerInstanceForType();
        public static bool IsDebugEnabled { get; set; }

        public static void Debug(object message)
        {
            if(IsDebugEnabled)
                Log.Debug(message);
        }

        public static void Debug(string message)
        {
            if (IsDebugEnabled)
                Log.Debug(message);
        }

        public static void Info(object message)
        {
            if (IsDebugEnabled)
                Log.Info(message);
        }

        public static void Info(string message)
        {
            if (IsDebugEnabled)
                Log.Info(message);
        }

        public static void Warn(object message)
        {
            Log.Warn(message);
        }

        public static void Warn(string message)
        {
            Log.Warn(message);
        }

        public static void Error(object message)
        {
            Log.Error(message);
        }

        public static void Error(string message)
        {
            Log.Error(message);
        }

        public static void Fatal(object message)
        {
            if (IsDebugEnabled)
                Log.Fatal(message);
        }

        public static void Fatal(string message)
        {
            if (IsDebugEnabled)
                Log.Fatal(message);
        }
    }
}
