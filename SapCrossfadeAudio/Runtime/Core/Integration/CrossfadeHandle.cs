using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.Audio;
using SapCrossfadeAudio.Runtime.Core.Types;

namespace SapCrossfadeAudio.Runtime.Core.Integration
{
    /// <summary>
    /// Lightweight handle for controlling CrossfadeGenerator from non-MonoBehaviour code.
    /// Safely wraps AudioSource.generatorInstance with existence checks and command dispatch.
    /// </summary>
    public readonly struct CrossfadeHandle
    {
        private readonly ProcessorInstance _instance;

        /// <summary>
        /// Creates a handle from the specified ProcessorInstance.
        /// </summary>
        public CrossfadeHandle(ProcessorInstance instance)
        {
            _instance = instance;
        }

        /// <summary>
        /// Whether this handle is valid and operable.
        /// </summary>
        public bool IsValid
        {
            [MethodImpl(methodImplOptions: MethodImplOptions.AggressiveInlining)]
            get => ControlContext.builtIn.Exists(processorInstance: _instance);
        }

        /// <summary>
        /// Starts crossfade to the specified target position.
        /// </summary>
        /// <param name="targetPosition01">Target position (0.0 = SourceA, 1.0 = SourceB)</param>
        /// <param name="durationSeconds">Fade duration in seconds</param>
        /// <param name="curve">Fade curve</param>
        /// <returns>True if command was sent successfully</returns>
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
        /// Crossfades to Source A (position = 0.0).
        /// </summary>
        [MethodImpl(methodImplOptions: MethodImplOptions.AggressiveInlining)]
        public bool TryCrossfadeToA(float durationSeconds, CrossfadeCurve curve)
        {
            return TryCrossfade(targetPosition01: 0f, durationSeconds: durationSeconds, curve: curve);
        }

        /// <summary>
        /// Crossfades to Source B (position = 1.0).
        /// </summary>
        [MethodImpl(methodImplOptions: MethodImplOptions.AggressiveInlining)]
        public bool TryCrossfadeToB(float durationSeconds, CrossfadeCurve curve)
        {
            return TryCrossfade(targetPosition01: 1f, durationSeconds: durationSeconds, curve: curve);
        }

        /// <summary>
        /// Instantly sets position without fading.
        /// </summary>
        [MethodImpl(methodImplOptions: MethodImplOptions.AggressiveInlining)]
        public bool TrySetImmediate(float position01)
        {
            return TryCrossfade(targetPosition01: position01, durationSeconds: 0f, curve: CrossfadeCurve.Linear);
        }

        /// <summary>
        /// Creates a CrossfadeHandle from an AudioSource. Only operable when playing with valid generatorInstance.
        /// </summary>
        [MethodImpl(methodImplOptions: MethodImplOptions.AggressiveInlining)]
        public static CrossfadeHandle FromAudioSource(AudioSource source)
        {
            return source == null ? default : new CrossfadeHandle(instance: source.generatorInstance);
        }
    }
}
