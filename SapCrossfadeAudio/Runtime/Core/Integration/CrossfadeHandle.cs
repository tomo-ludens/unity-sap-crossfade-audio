using System.Runtime.CompilerServices;

using UnityEngine;
using UnityEngine.Audio;

using SapCrossfadeAudio.Runtime.Core.Types;

namespace SapCrossfadeAudio.Runtime.Core.Integration
{
    /// <summary>
    /// CrossfadeGenerator を非 MonoBehaviour から操作するための軽量ハンドル。
    /// AudioSource.generatorInstance を安全にラップし、存在チェックとコマンド送信を提供する。
    /// </summary>
    public readonly struct CrossfadeHandle
    {
        private readonly ProcessorInstance _instance;

        /// <summary>
        /// 指定された ProcessorInstance からハンドルを生成する。
        /// </summary>
        public CrossfadeHandle(ProcessorInstance instance)
        {
            _instance = instance;
        }

        /// <summary>
        /// ハンドルが有効かどうか。generatorInstance が存在し、操作可能な場合に true。
        /// </summary>
        public bool IsValid
        {
            [MethodImpl(methodImplOptions: MethodImplOptions.AggressiveInlining)]
            get => ControlContext.builtIn.Exists(processorInstance: _instance);
        }

        /// <summary>
        /// 指定したターゲット位置へクロスフェードを開始する。
        /// </summary>
        /// <param name="targetPosition01">ターゲット位置 (0.0 = SourceA, 1.0 = SourceB)</param>
        /// <param name="durationSeconds">フェード時間（秒）</param>
        /// <param name="curve">フェードカーブ</param>
        /// <returns>コマンド送信に成功した場合 true</returns>
        [MethodImpl(methodImplOptions: MethodImplOptions.AggressiveInlining)]
        public bool TryCrossfade(float targetPosition01, float durationSeconds, CrossfadeCurve curve)
        {
            if (!IsValid)
                return false;

            var command = CrossfadeCommand.Create(
                targetPosition01: Mathf.Clamp01(value: targetPosition01),
                durationSeconds: Mathf.Max(a: 0f, b: durationSeconds),
                curve: curve
            );

            ControlContext.builtIn.SendMessage(processorInstance: _instance, message: ref command);
            return true;
        }

        /// <summary>
        /// Source A へクロスフェードする (position = 0.0)。
        /// </summary>
        [MethodImpl(methodImplOptions: MethodImplOptions.AggressiveInlining)]
        public bool TryCrossfadeToA(float durationSeconds, CrossfadeCurve curve)
        {
            return TryCrossfade(targetPosition01: 0f, durationSeconds: durationSeconds, curve: curve);
        }

        /// <summary>
        /// Source B へクロスフェードする (position = 1.0)。
        /// </summary>
        [MethodImpl(methodImplOptions: MethodImplOptions.AggressiveInlining)]
        public bool TryCrossfadeToB(float durationSeconds, CrossfadeCurve curve)
        {
            return TryCrossfade(targetPosition01: 1f, durationSeconds: durationSeconds, curve: curve);
        }

        /// <summary>
        /// 指定位置へ即座に切り替える（フェードなし）。
        /// </summary>
        [MethodImpl(methodImplOptions: MethodImplOptions.AggressiveInlining)]
        public bool TrySetImmediate(float position01)
        {
            return TryCrossfade(targetPosition01: position01, durationSeconds: 0f, curve: CrossfadeCurve.Linear);
        }

        /// <summary>
        /// AudioSource から CrossfadeHandle を生成する。
        /// AudioSource が再生中で generatorInstance が有効な場合のみ操作可能。
        /// </summary>
        [MethodImpl(methodImplOptions: MethodImplOptions.AggressiveInlining)]
        public static CrossfadeHandle FromAudioSource(AudioSource source)
        {
            if (source == null)
                return default;

            return new CrossfadeHandle(instance: source.generatorInstance);
        }
    }
}
