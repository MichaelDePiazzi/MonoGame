using System;

namespace Microsoft.Xna.Framework
{
    public static class MonoGameDebug
    {
        public static event EventHandler<string> DebugMessageLogged;

        public static void LogDebugMessage(string message)
        {
            DebugMessageLogged?.Invoke(null, message);
        }
    }
}