using UnityEngine;

using SapCrossfadeAudio.Runtime.Core.Generators.Crossfade;
using SapCrossfadeAudio.Runtime.Core.Integration;
using SapCrossfadeAudio.Runtime.Core.Types;

// ReSharper disable once RedundantUsingDirective
using SapCrossfadeAudio.Runtime.Core;

namespace SapCrossfadeAudio.Runtime.Core.Components
{
    /// <summary>
    /// CrossfadeGenerator を Inspector から操作するための MonoBehaviour ラッパー。
    /// AudioSource に Generator を設定し、再生制御とクロスフェード操作を提供する。
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(requiredComponent: typeof(AudioSource))]
    public sealed class CrossfadePlayer : MonoBehaviour
    {
        [Header(header: "Generator")]
        [SerializeField] [Tooltip(tooltip: "CrossfadeGeneratorAsset を設定してください")]
        private CrossfadeGeneratorAsset generator;

        [Header(header: "Playback")]
        [SerializeField] [Tooltip(tooltip: "有効にすると Start() で自動再生します")]
        private bool playOnStart = true;

        private AudioSource _audioSource;

        /// <summary>
        /// 現在の generatorInstance への操作ハンドル。
        /// AudioSource が再生中でない場合、IsValid が false になる。
        /// </summary>
        public CrossfadeHandle Handle => CrossfadeHandle.FromAudioSource(source: _audioSource);

        /// <summary>
        /// 設定されている Generator アセット。
        /// </summary>
        public CrossfadeGeneratorAsset Generator
        {
            get => generator;
            set => generator = value;
        }

        /// <summary>
        /// 内部の AudioSource への参照。
        /// </summary>
        public AudioSource AudioSource => _audioSource;

        /// <summary>
        /// 現在再生中かどうか。
        /// </summary>
        public bool IsPlaying => _audioSource != null && _audioSource.isPlaying;

        private void Awake()
        {
            _audioSource = GetComponent<AudioSource>();
        }

        private void Start()
        {
            if (playOnStart && generator != null)
            {
                Play();
            }
        }

        /// <summary>
        /// Generator を設定して再生を開始する。
        /// </summary>
        public void Play()
        {
            if (_audioSource == null || generator == null)
            {
                CrossfadeLogger.LogWarning<CrossfadePlayer>(message: "AudioSource or Generator is not set.", context: this);
                return;
            }

            _audioSource.generator = generator;
            _audioSource.Play();
        }

        /// <summary>
        /// 再生を停止する。
        /// </summary>
        public void Stop()
        {
            if (_audioSource != null)
            {
                _audioSource.Stop();
            }
        }

        /// <summary>
        /// Source A へクロスフェードする。
        /// </summary>
        /// <param name="durationSeconds">フェード時間（秒）</param>
        /// <param name="curve">フェードカーブ（デフォルト: EqualPower）</param>
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
        /// Source B へクロスフェードする。
        /// </summary>
        /// <param name="durationSeconds">フェード時間（秒）</param>
        /// <param name="curve">フェードカーブ（デフォルト: EqualPower）</param>
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
        /// 指定位置へクロスフェードする。
        /// </summary>
        /// <param name="targetPosition01">ターゲット位置 (0.0 = A, 1.0 = B)</param>
        /// <param name="durationSeconds">フェード時間（秒）</param>
        /// <param name="curve">フェードカーブ（デフォルト: EqualPower）</param>
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
        /// 指定位置へ即座に切り替える（フェードなし）。
        /// </summary>
        /// <param name="position01">位置 (0.0 = A, 1.0 = B)</param>
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
