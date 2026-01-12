using System.Threading;
using System.Threading.Tasks;
using Unity.Collections;
using Unity.IntegerTime;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Audio;
using UnityEngine.ResourceManagement.AsyncOperations;
using SapCrossfadeAudio.Runtime.Core.Foundation;
using SapCrossfadeAudio.Runtime.Core.Foundation.Logging;
using SapCrossfadeAudio.Runtime.Core.Foundation.Resampling;
using SapCrossfadeAudio.Runtime.Core.Generators.Clip;
using static UnityEngine.Audio.ProcessorInstance;

namespace SapCrossfadeAudio.Addressables
{
    /// <summary>
    /// AudioClip generator using Addressables for lazy loading with explicit resource management.
    /// </summary>
    /// <remarks>
    /// Ownership policy:
    /// - This asset owns the AssetReference and is responsible for load/release
    /// - Release() is idempotent (safe to call multiple times)
    /// - When used with external loaders, clarify ownership responsibilities
    /// </remarks>
    [CreateAssetMenu(fileName = "AddressableClipGenerator", menuName = "SapCrossfadeAudio/Generators/AddressableClipGenerator", order = 15)]
    public sealed class AddressableClipGeneratorAsset : ScriptableObject, IAudioGenerator, IPreloadableAudioGenerator
    {
        [Header("Addressable Reference")]
        [SerializeField]
        [Tooltip("AssetReference for the AudioClip to load")]
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

        [Header("Advanced")]
        [SerializeField]
        private bool _allowSynchronousLoadFallback;

        // Internal state (volatile for safe cross-context reads in async workflows)
        private AsyncOperationHandle<AudioClip> _handle;
        private AudioClip _loadedClip;
        private NativeArray<float> _cachedPcm;
        private volatile bool _isLoading;
        private volatile bool _isReady;

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

            // Use cached PCM if preloaded
            if (_isReady && _cachedPcm.IsCreated && _cachedPcm.Length > 0 && _loadedClip != null)
            {
                int clipFrames = _loadedClip.samples;
                int clipChannels = _loadedClip.channels;

                var pcm = NativeBufferPool.Rent(length: _cachedPcm.Length);
                NativeArray<float>.Copy(src: _cachedPcm, dst: pcm);

                realtime.ClipDataInterleaved = pcm;
                realtime.ClipDataIsPooled = true;
                realtime.ClipChannels = clipChannels;
                realtime.ClipSampleRate = _loadedClip.frequency;
                realtime.ClipTotalFrames = clipFrames;
                realtime.SourceFramePosition = 0f;
                realtime.IsValid = true;
            }
            // Fallback: synchronous load if not preloaded (not recommended but fail-safe)
            else if (_allowSynchronousLoadFallback && _clipReference != null && _clipReference.RuntimeKeyIsValid())
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
        /// Whether the asset is loaded and ready for playback.
        /// </summary>
        public bool IsReady => _isReady;

        /// <summary>
        /// Preloads the asset from Addressables. Caches PCM data to avoid hitches during CreateInstance.
        /// </summary>
        public async Task PreloadAsync()
        {
            if (_isReady || _isLoading)
                return;

            if (_clipReference == null || !_clipReference.RuntimeKeyIsValid())
            {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                CrossfadeLogger.LogWarning<AddressableClipGeneratorAsset>(message: $"Invalid AssetReference: {name}", context: this);
#endif
                return;
            }

            _isLoading = true;

            try
            {
                // In case a previous load attempt failed, ensure any old handle/cache is released before retrying.
                if (_handle.IsValid())
                {
                    Addressables.Release(_handle);
                    _handle = default;
                }
                if (_cachedPcm.IsCreated)
                {
                    _cachedPcm.Dispose();
                    _cachedPcm = default;
                }

                _handle = Addressables.LoadAssetAsync<AudioClip>(_clipReference);
                await _handle.Task;

                if (_handle.Status == AsyncOperationStatus.Succeeded)
                {
                    _loadedClip = _handle.Result;

                    // Cache PCM data
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
                    CrossfadeLogger.LogError<AddressableClipGeneratorAsset>(message: $"Load failed: {name}", context: this);
#endif
                    if (_handle.IsValid())
                    {
                        Addressables.Release(_handle);
                        _handle = default;
                    }
                }
            }
            catch (System.Exception ex)
            {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                CrossfadeLogger.LogException<AddressableClipGeneratorAsset>(exception: ex, context: this);
#endif

                if (_handle.IsValid())
                {
                    Addressables.Release(_handle);
                    _handle = default;
                }
            }
            finally
            {
                _isLoading = false;
            }
        }

        /// <summary>
        /// Releases the loaded asset. This method is idempotent (safe to call multiple times).
        /// </summary>
        public void Release()
        {
            _isReady = false;
            _isLoading = false;
            _loadedClip = null;

            // Release PCM cache
            if (_cachedPcm.IsCreated)
            {
                _cachedPcm.Dispose();
                _cachedPcm = default;
            }

            // Release Addressables handle
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

            // Dispose existing cache if present
            if (_cachedPcm.IsCreated)
            {
                _cachedPcm.Dispose();
            }

            _cachedPcm = new NativeArray<float>(requiredFloats, Allocator.Persistent);
            _loadedClip.GetData(_cachedPcm, 0);
        }

        private void TryLoadSynchronously(ref ClipGeneratorRealtime realtime)
        {
            // Sync load is discouraged but serves as fallback when not preloaded
            try
            {
                var syncHandle = Addressables.LoadAssetAsync<AudioClip>(_clipReference);
                var clip = syncHandle.WaitForCompletion();

                if (clip == null || !ClipRequirements.CanUseGetData(clip) || !ClipRequirements.EnsureLoaded(clip))
                {
                    if (syncHandle.IsValid())
                    {
                        Addressables.Release(syncHandle);
                    }
                    return;
                }

                int frames = clip.samples;
                int channels = clip.channels;
                int requiredFloats = frames * channels;

                if (requiredFloats > 0)
                {
                    var pcm = NativeBufferPool.Rent(requiredFloats);
                    if (clip.GetData(pcm, 0))
                    {
                        realtime.ClipDataInterleaved = pcm;
                        realtime.ClipDataIsPooled = true;
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

                _handle = syncHandle;
                _loadedClip = clip;
            }
            catch (System.Exception ex)
            {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                CrossfadeLogger.LogException<AddressableClipGeneratorAsset>(exception: ex, context: this);
#endif
            }
        }

        #endregion
    }
}
