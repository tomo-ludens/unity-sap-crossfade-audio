using System.Runtime.CompilerServices;

namespace SapCrossfadeAudio.Runtime.Core.Types
{
    /// <summary>
    /// Command specifying crossfade target position, duration, and curve.
    /// Passed from Control to Realtime via Pipe (unmanaged struct).
    /// </summary>
    public struct CrossfadeCommand
    {
        public float TargetPosition01;   // 0..1
        public float DurationSeconds;    // >=0
        public CrossfadeCurve Curve;

        [MethodImpl(methodImplOptions: MethodImplOptions.AggressiveInlining)]
        public static CrossfadeCommand Create(float targetPosition01, float durationSeconds, CrossfadeCurve curve)
        {
            return new CrossfadeCommand
            {
                TargetPosition01 = targetPosition01,
                DurationSeconds = durationSeconds,
                Curve = curve
            };
        }
    }
}
