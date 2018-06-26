using System;

namespace Microsoft.Xna.Framework
{
    public enum MonoGameLogLevel { Debug, Info, Warn, Error }

    public static class MonoGameLogger
    {
        public static event Action<MonoGameLogLevel, string> MessageLogged;

        public static void LogMessage(MonoGameLogLevel level, string message)
        {
            MessageLogged?.Invoke(level, message);
        }
    }
}
