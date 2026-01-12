using System.Threading.Tasks;

using Unity.Collections;
using Unity.IntegerTime;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Audio;
using UnityEngine.ResourceManagement.AsyncOperations;

using SapCrossfadeAudio.Runtime.Core.Foundation;
using SapCrossfadeAudio.Runtime.Core.Foundation.Resampling;
using SapCrossfadeAudio.Runtime.Core.Generators.Clip;

using static UnityEngine.Audio.ProcessorInstance;

namespace SapCrossfadeAudio.Addressables
{
    /// <summary>
    /// Addressables を使用した AudioClip ジェネレーター。
    /// AssetReference による遅延ロードと明示的なリソース管理を提供する。
    /// </summary>
    /// <remarks>
    /// 所有権ポリシー:
    /// - このアセットが AssetReference を所有し、ロード/解放の責務を持つ
    /// - Release() は冪等（何度呼んでも安全）
    /// - 外部ローダーと併用する場合は、責務の明確化が必要
    /// </remarks>
    [CreateAssetMenu(fileName = "AddressableClipGenerator", menuName = "TomoLudens/SapCrossfadeAudio/Generators/AddressableClipGenerator", order = 15)]
    public sealed class AddressableClipGeneratorAsset : ScriptableObject, IAudioGenerator, IPreloadableAudioGenerator
    {
        [Header("Addressable Reference")]
        [SerializeField]
        [Tooltip("ロードする AudioClip の AssetReference")]
        private AssetReferenceT<AudioClip> _clipReference;

        [Header("Playback Settings")]
        [SerializeField]
        private bool _loop;

        [SerializeField]
        [Range(0f, 2f)]
        private float _gain = 1f;

        [Header("Resampling")]
        [SerializeField]
        private ResampleMode _resampleMode = ResampleMode.Auto;

        [SerializeField]
        private ResampleQuality _resampleQuality = ResampleQuality.Linear;

        // 内部状態
        private AsyncOperationHandle<AudioClip> _handle;
        private AudioClip _loadedClip;
        private NativeArray<float> _cachedPcm;
        private bool _isLoading;
        private bool _isReady;

        #region IAudioGenerator

        public bool isFinite => !_loop;
        public bool isRealtime => false;
        public DiscreteTime? length => null;

        public GeneratorInstance CreateInstance(
            ControlContext context,
            AudioFormat? nestedConfiguration,
            CreationParameters creationParameters)
        {
            var realtime = new ClipGeneratorRealtime
            {
                Loop = _loop,
                Gain = _gain,
                ResampleMode = _resampleMode,
                ResampleQuality = _resampleQuality,
                IsValid = false
            };

            // Preload済みの場合はキャッシュされたPCMを使用
            if (_isReady && _cachedPcm.IsCreated && _cachedPcm.Length > 0 && _loadedClip != null)
            {
                int clipFrames = _loadedClip.samples;
                int clipChannels = _loadedClip.channels;

                realtime.ClipDataInterleaved = _cachedPcm;
                realtime.ClipChannels = clipChannels;
                realtime.ClipSampleRate = _loadedClip.frequency;
                realtime.ClipTotalFrames = clipFrames;
                realtime.SourceFramePosition = 0f;
                realtime.IsValid = true;
            }
            // Preloadされていない場合は同期ロードを試みる（非推奨だが安全側に倒す）
            else if (_clipReference != null && _clipReference.RuntimeKeyIsValid())
            {
                TryLoadSynchronously(ref realtime);
            }

            var control = new ClipGeneratorControl(
                loop: _loop,
                gain: _gain,
                resampleMode: _resampleMode,
                resampleQuality: _resampleQuality
            );

            return context.AllocateGenerator(
                realtimeState: realtime,
                controlState: control,
                nestedFormat: nestedConfiguration,
                creationParameters: creationParameters
            );
        }

        #endregion

        #region IPreloadableAudioGenerator

        /// <summary>
        /// アセットがロード済みで再生可能な状態かどうか。
        /// </summary>
        public bool IsReady => _isReady;

        /// <summary>
        /// Addressables からアセットを事前ロードする。
        /// PCM データのキャッシュまで行い、CreateInstance 時のヒッチを回避する。
        /// </summary>
        public async Task PreloadAsync()
        {
            if (_isReady || _isLoading)
                return;

            if (_clipReference == null || !_clipReference.RuntimeKeyIsValid())
            {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                Debug.LogWarning($"[AddressableClipGenerator] AssetReference が無効です: {name}", this);
#endif
                return;
            }

            _isLoading = true;

            try
            {
                _handle = Addressables.LoadAssetAsync<AudioClip>(_clipReference);
                await _handle.Task;

                if (_handle.Status == AsyncOperationStatus.Succeeded)
                {
                    _loadedClip = _handle.Result;

                    // PCM をキャッシュ
                    if (_loadedClip != null && ClipRequirements.CanUseGetData(_loadedClip))
                    {
                        if (ClipRequirements.EnsureLoaded(_loadedClip))
                        {
                            CachePcmData();
                            _isReady = true;
                        }
                    }
                }
                else
                {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                    Debug.LogError($"[AddressableClipGenerator] ロード失敗: {name}", this);
#endif
                }
            }
            catch (System.Exception ex)
            {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                Debug.LogException(ex, this);
#endif
            }
            finally
            {
                _isLoading = false;
            }
        }

        /// <summary>
        /// ロードしたアセットを解放する。
        /// このメソッドは冪等（何度呼んでも安全）。
        /// </summary>
        public void Release()
        {
            _isReady = false;
            _isLoading = false;
            _loadedClip = null;

            // PCM キャッシュを解放
            if (_cachedPcm.IsCreated)
            {
                _cachedPcm.Dispose();
                _cachedPcm = default;
            }

            // Addressables ハンドルを解放
            if (_handle.IsValid())
            {
                Addressables.Release(_handle);
                _handle = default;
            }
        }

        #endregion

        #region Unity Lifecycle

        private void OnDisable()
        {
            Release();
        }

        private void OnDestroy()
        {
            Release();
        }

        #endregion

        #region Private Methods

        private void CachePcmData()
        {
            if (_loadedClip == null)
                return;

            int frames = _loadedClip.samples;
            int channels = _loadedClip.channels;
            int requiredFloats = frames * channels;

            if (requiredFloats <= 0)
                return;

            // 既存のキャッシュがあれば解放
            if (_cachedPcm.IsCreated)
            {
                _cachedPcm.Dispose();
            }

            _cachedPcm = new NativeArray<float>(requiredFloats, Allocator.Persistent);
            _loadedClip.GetData(_cachedPcm, 0);
        }

        private void TryLoadSynchronously(ref ClipGeneratorRealtime realtime)
        {
            // 同期ロードは非推奨だが、Preload されていない場合のフォールバック
            try
            {
                var syncHandle = Addressables.LoadAssetAsync<AudioClip>(_clipReference);
                var clip = syncHandle.WaitForCompletion();

                if (clip != null && ClipRequirements.CanUseGetData(clip))
                {
                    if (ClipRequirements.EnsureLoaded(clip))
                    {
                        int frames = clip.samples;
                        int channels = clip.channels;
                        int requiredFloats = frames * channels;

                        if (requiredFloats > 0)
                        {
                            var pcm = NativeBufferPool.Rent(requiredFloats);
                            if (clip.GetData(pcm, 0))
                            {
                                realtime.ClipDataInterleaved = pcm;
                                realtime.ClipChannels = channels;
                                realtime.ClipSampleRate = clip.frequency;
                                realtime.ClipTotalFrames = frames;
                                realtime.SourceFramePosition = 0f;
                                realtime.IsValid = true;
                            }
                            else
                            {
                                NativeBufferPool.Return(ref pcm);
                            }
                        }
                    }
                }

                // 同期ロードの場合、ハンドルを保持しておく
                _handle = syncHandle;
                _loadedClip = clip;
            }
            catch (System.Exception ex)
            {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                Debug.LogException(ex, this);
#endif
            }
        }

        #endregion
    }
}
