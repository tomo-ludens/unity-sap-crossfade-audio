using System.Runtime.CompilerServices;
using UnityEngine.Audio;

namespace SapCrossfadeAudio.Runtime.Core.Foundation
{
    internal static class SapCompat
    {
        [MethodImpl(methodImplOptions: MethodImplOptions.AggressiveInlining)]
        public static int GetProcessedFrames(in GeneratorInstance.Result result)
            => result.processedFrames;

        /// <summary>
        /// Indicates child failed to produce requested frames (end of stream, exhausted, or error).
        /// Parent should zero-fill the remaining frames and optionally mark child as finished.
        /// </summary>
        [MethodImpl(methodImplOptions: MethodImplOptions.AggressiveInlining)]
        public static bool IsShortWrite(in GeneratorInstance.Result result, int requestedFrames)
            => result.processedFrames < requestedFrames;

        /// <summary>
        /// Returns true if no frames were produced (0 frames), indicating stream exhaustion.
        /// </summary>
        [MethodImpl(methodImplOptions: MethodImplOptions.AggressiveInlining)]
        public static bool IsExhausted(in GeneratorInstance.Result result)
            => (result.processedFrames <= 0);
    }
}
