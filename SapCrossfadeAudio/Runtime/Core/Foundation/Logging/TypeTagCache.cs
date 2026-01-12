namespace SapCrossfadeAudio.Runtime.Core.Foundation.Logging
{
    internal static class TypeTagCache<T>
    {
        // ReSharper disable StaticMemberInGenericType
        private static readonly string Tag = typeof(T).FullName ?? typeof(T).Name;
        internal static readonly string Prefix = "[" + Tag + "] ";
        // ReSharper restore StaticMemberInGenericType
    }
}
