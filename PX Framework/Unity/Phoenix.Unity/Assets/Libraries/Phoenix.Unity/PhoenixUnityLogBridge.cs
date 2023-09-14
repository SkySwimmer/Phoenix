using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Phoenix.Common.Logging;
using Phoenix.Common.Logging.Impl;

namespace Phoenix.Unity.Bindings
{

    public class PhoenixUnityLogBridge : Logger, ILoggerImplementationProvider
    {
        private string name;
        private LogLevel level = LogLevel.GLOBAL;
        private PhoenixUnityLogBridge(string name)
        {
            this.name = name;
        }

        public override LogLevel Level { get => level; set => level = value; }

        public static void Register()
        {
            Logger.provider = new PhoenixUnityLogBridge(null);
        }

        public Logger CreateInstance(string name)
        {
            return new PhoenixUnityLogBridge(name);
        }

        public override void Log(LogLevel level, string message)
        {
            if (Level != LogLevel.QUIET && Level >= level)
            {
                switch (level)
                {
                    case LogLevel.FATAL:
                        UnityEngine.Debug.LogError("[FATAL] " + message);
                        break;
                    case LogLevel.ERROR:
                        UnityEngine.Debug.LogError(message);
                        break;
                    case LogLevel.WARN:
                        UnityEngine.Debug.LogWarning(message);
                        break;
                    case LogLevel.INFO:
                        UnityEngine.Debug.Log(message);
                        break;
                    case LogLevel.TRACE:
                        UnityEngine.Debug.Log("[TRACE] " + message);
                        break;
                    case LogLevel.DEBUG:
                        UnityEngine.Debug.Log("[DEBUG] " + message);
                        break;
                }
            }
        }

        public override void Log(LogLevel level, string message, Exception exception)
        {
            if (Level != LogLevel.QUIET && Level >= level)
            {
                switch (level)
                {
                    case LogLevel.FATAL:
                        UnityEngine.Debug.LogError("[FATAL] " + message + "\nException: " + exception.GetType().FullName + (exception.Message != null ? ": " + exception.Message : ""));
                        break;
                    case LogLevel.ERROR:
                        UnityEngine.Debug.LogError(message + "\nException: " + exception.GetType().FullName + (exception.Message != null ? ": " + exception.Message : ""));
                        break;
                    case LogLevel.WARN:
                        UnityEngine.Debug.LogWarning(message + "\nException: " + exception.GetType().FullName + (exception.Message != null ? ": " + exception.Message : ""));
                        break;
                    case LogLevel.INFO:
                        UnityEngine.Debug.Log(message + "\nException: " + exception.GetType().FullName + (exception.Message != null ? ": " + exception.Message : ""));
                        break;
                    case LogLevel.TRACE:
                        UnityEngine.Debug.Log("[TRACE] " + message + "\nException: " + exception.GetType().FullName + (exception.Message != null ? ": " + exception.Message : ""));
                        break;
                    case LogLevel.DEBUG:
                        UnityEngine.Debug.Log("[DEBUG] " + message + "\nException: " + exception.GetType().FullName + (exception.Message != null ? ": " + exception.Message : ""));
                        break;
                }
            }
        }
    }
}