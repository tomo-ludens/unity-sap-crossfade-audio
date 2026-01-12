using System;
using System.Diagnostics;
using Debug = UnityEngine.Debug;

namespace SapCrossfadeAudio.Runtime.Core
{
    /// <summary>
    /// SapCrossfadeAudio infrastructure logger (dev-only).
    /// </summary>
    /// <remarks>
    /// Calls are omitted at the call site unless at least one of these symbols is defined:
    /// UNITY_EDITOR, DEVELOPMENT_BUILD, UNITY_ASSERTIONS.
    /// </remarks>
    internal static class CrossfadeLogger
    {
        private const string EditorSymbol     = "UNITY_EDITOR";
        private const string DevBuildSymbol   = "DEVELOPMENT_BUILD";
        private const string AssertionsSymbol = "UNITY_ASSERTIONS";

        private const string InfraTag = "SapCrossfadeAudio";
        private const string InfraPrefix = "[" + InfraTag + "] ";

        private static class TypeTagCache<T>
        {
            // ReSharper disable StaticMemberInGenericType
            private static readonly string Tag = typeof(T).FullName ?? typeof(T).Name;
            internal static readonly string Prefix = "[" + Tag + "] ";
            // ReSharper restore StaticMemberInGenericType
        }

        [Conditional(conditionString: EditorSymbol), Conditional(conditionString: DevBuildSymbol), Conditional(conditionString: AssertionsSymbol)]
        public static void Log(string message, UnityEngine.Object context = null)
            => Debug.Log(message: InfraPrefix + message, context: context);

        [Conditional(conditionString: EditorSymbol), Conditional(conditionString: DevBuildSymbol), Conditional(conditionString: AssertionsSymbol)]
        public static void Log<T>(string message, UnityEngine.Object context = null)
            => Debug.Log(message: TypeTagCache<T>.Prefix + message, context: context);

        [Conditional(conditionString: EditorSymbol), Conditional(conditionString: DevBuildSymbol), Conditional(conditionString: AssertionsSymbol)]
        public static void LogWarning(string message, UnityEngine.Object context = null)
            => Debug.LogWarning(message: InfraPrefix + message, context: context);

        [Conditional(conditionString: EditorSymbol), Conditional(conditionString: DevBuildSymbol), Conditional(conditionString: AssertionsSymbol)]
        public static void LogWarning<T>(string message, UnityEngine.Object context = null)
            => Debug.LogWarning(message: TypeTagCache<T>.Prefix + message, context: context);

        [Conditional(conditionString: EditorSymbol), Conditional(conditionString: DevBuildSymbol), Conditional(conditionString: AssertionsSymbol)]
        public static void LogError(string message, UnityEngine.Object context = null)
            => Debug.LogError(message: InfraPrefix + message, context: context);

        [Conditional(conditionString: EditorSymbol), Conditional(conditionString: DevBuildSymbol), Conditional(conditionString: AssertionsSymbol)]
        public static void LogError<T>(string message, UnityEngine.Object context = null)
            => Debug.LogError(message: TypeTagCache<T>.Prefix + message, context: context);

        [Conditional(conditionString: EditorSymbol), Conditional(conditionString: DevBuildSymbol), Conditional(conditionString: AssertionsSymbol)]
        public static void LogException(Exception exception, UnityEngine.Object context = null)
        {
            if (exception == null) return;

            LogError(message: $"Exception: {exception.GetType().Name}: {exception.Message}", context: context);
            Debug.LogException(exception: exception, context: context);
        }

        [Conditional(conditionString: EditorSymbol), Conditional(conditionString: DevBuildSymbol), Conditional(conditionString: AssertionsSymbol)]
        public static void LogException<T>(Exception exception, UnityEngine.Object context = null)
        {
            if (exception == null) return;

            LogError<T>(message: $"Exception: {exception.GetType().Name}: {exception.Message}", context: context);
            Debug.LogException(exception: exception, context: context);
        }
    }
}
