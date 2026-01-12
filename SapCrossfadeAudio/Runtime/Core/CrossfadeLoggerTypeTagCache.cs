using System;

namespace SapCrossfadeAudio.Runtime.Core
{
    internal static class CrossfadeLoggerTypeTagCache<T>
    {
        // ReSharper disable StaticMemberInGenericType
        private static readonly string Tag = typeof(T).FullName ?? typeof(T).Name;
        internal static readonly string Prefix = "[" + Tag + "] ";
        // ReSharper restore StaticMemberInGenericType
    }
}
