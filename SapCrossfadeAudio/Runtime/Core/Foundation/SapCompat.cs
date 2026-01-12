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
        /// 子が要求フレームを満たせなかった（終端・枯渇・失敗など）ことを示す。
        /// 親は不足分を 0 埋めし、必要なら「子 finished」扱いへ遷移する。
        /// </summary>
        [MethodImpl(methodImplOptions: MethodImplOptions.AggressiveInlining)]
        public static bool IsShortWrite(in GeneratorInstance.Result result, int requestedFrames)
            => result.processedFrames < requestedFrames;

        /// <summary>
        /// 明確に何も出てこなかった（0 フレーム）＝終了扱いに寄せる。
        /// </summary>
        [MethodImpl(methodImplOptions: MethodImplOptions.AggressiveInlining)]
        public static bool IsExhausted(in GeneratorInstance.Result result)
            => result.processedFrames <= 0;
    }
}
