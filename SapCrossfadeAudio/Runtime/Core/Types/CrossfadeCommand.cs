using System.Runtime.CompilerServices;

namespace SapCrossfadeAudio.Runtime.Core.Types
{
    /// <summary>
    /// Crossfade のターゲットと遷移時間（秒）、カーブを指定するコマンド。
    /// Control -> Realtime は Pipe 経由で渡す想定（unmanaged）。
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
