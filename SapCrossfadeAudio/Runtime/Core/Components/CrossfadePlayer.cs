using UnityEngine;
using SapCrossfadeAudio.Runtime.Core.Generators.Crossfade;
using SapCrossfadeAudio.Runtime.Core.Integration;
using SapCrossfadeAudio.Runtime.Core.Types;

namespace SapCrossfadeAudio.Runtime.Core.Components
{
    /// <summary>
    /// MonoBehaviour wrapper for controlling CrossfadeGenerator from Inspector.
    /// Manages AudioSource setup and provides crossfade operations.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(requiredComponent: typeof(AudioSource))]
    public sealed class CrossfadePlayer : MonoBehaviour
    {
        [Header(header: "Generator")]
        [SerializeField] [Tooltip(tooltip: "Assign a CrossfadeGeneratorAsset")]
        private CrossfadeGeneratorAsset generator;

        [Header(header: "Playback")]
        [SerializeField] [Tooltip(tooltip: "Auto-play on Start()")]
        private bool playOnStart = true;

        /// <summary>
        /// Handle for the current generatorInstance. IsValid is false when AudioSource is not playing.
        /// </summary>
        public CrossfadeHandle Handle => CrossfadeHandle.FromAudioSource(source: AudioSource);

        /// <summary>
        /// The assigned Generator asset.
        /// </summary>
        public CrossfadeGeneratorAsset Generator
        {
            get => generator;
            set => generator = value;
        }

        /// <summary>
        /// Reference to the internal AudioSource.
        /// </summary>
        public AudioSource AudioSource { get; private set; }

        /// <summary>
        /// Whether audio is currently playing.
        /// </summary>
        public bool IsPlaying => AudioSource != null && AudioSource.isPlaying;

        private void Awake()
        {
            AudioSource = GetComponent<AudioSource>();
        }

        private void Start()
        {
            if (playOnStart && generator != null)
            {
                Play();
            }
        }

        /// <summary>
        /// Sets the generator and starts playback.
        /// </summary>
        public void Play()
        {
            if (AudioSource == null || generator == null)
            {
                CrossfadeLogger.LogWarning<CrossfadePlayer>(message: "AudioSource or Generator is not set.", context: this);
                return;
            }

            AudioSource.generator = generator;
            AudioSource.Play();
        }

        /// <summary>
        /// Stops playback.
        /// </summary>
        public void Stop()
        {
            if (AudioSource != null)
            {
                AudioSource.Stop();
            }
        }

        /// <summary>
        /// Crossfades to Source A.
        /// </summary>
        /// <param name="durationSeconds">Fade duration in seconds</param>
        /// <param name="curve">Fade curve (default: EqualPower)</param>
        public void CrossfadeToA(float durationSeconds, CrossfadeCurve curve = CrossfadeCurve.EqualPower)
        {
            var handle = Handle;
            if (!handle.IsValid)
            {
                CrossfadeLogger.LogWarning<CrossfadePlayer>(message: "generatorInstance is not available. Is the AudioSource playing?", context: this);
                return;
            }

            handle.TryCrossfadeToA(durationSeconds: durationSeconds, curve: curve);
        }

        /// <summary>
        /// Crossfades to Source B.
        /// </summary>
        /// <param name="durationSeconds">Fade duration in seconds</param>
        /// <param name="curve">Fade curve (default: EqualPower)</param>
        public void CrossfadeToB(float durationSeconds, CrossfadeCurve curve = CrossfadeCurve.EqualPower)
        {
            var handle = Handle;
            if (!handle.IsValid)
            {
                CrossfadeLogger.LogWarning<CrossfadePlayer>(message: "generatorInstance is not available. Is the AudioSource playing?", context: this);
                return;
            }

            handle.TryCrossfadeToB(durationSeconds: durationSeconds, curve: curve);
        }

        /// <summary>
        /// Crossfades to a specified position.
        /// </summary>
        /// <param name="targetPosition01">Target position (0.0 = A, 1.0 = B)</param>
        /// <param name="durationSeconds">Fade duration in seconds</param>
        /// <param name="curve">Fade curve (default: EqualPower)</param>
        public void Crossfade(float targetPosition01, float durationSeconds, CrossfadeCurve curve = CrossfadeCurve.EqualPower)
        {
            var handle = Handle;
            if (!handle.IsValid)
            {
                CrossfadeLogger.LogWarning<CrossfadePlayer>(message: "generatorInstance is not available. Is the AudioSource playing?", context: this);
                return;
            }

            handle.TryCrossfade(targetPosition01: targetPosition01, durationSeconds: durationSeconds, curve: curve);
        }

        /// <summary>
        /// Instantly sets position without fading.
        /// </summary>
        /// <param name="position01">Position (0.0 = A, 1.0 = B)</param>
        public void SetImmediate(float position01)
        {
            var handle = Handle;
            if (!handle.IsValid)
            {
                CrossfadeLogger.LogWarning<CrossfadePlayer>(message: "generatorInstance is not available. Is the AudioSource playing?", context: this);
                return;
            }

            handle.TrySetImmediate(position01: position01);
        }
    }
}
