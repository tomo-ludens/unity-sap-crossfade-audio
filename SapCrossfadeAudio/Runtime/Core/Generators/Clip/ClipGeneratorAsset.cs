using Unity.IntegerTime;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.Audio;
using SapCrossfadeAudio.Runtime.Core.Foundation;
using SapCrossfadeAudio.Runtime.Core.Foundation.Resampling;
using static UnityEngine.Audio.ProcessorInstance;

namespace SapCrossfadeAudio.Runtime.Core.Generators.Clip
{
    [CreateAssetMenu(fileName = "ClipGenerator", menuName = "SapCrossfadeAudio/Generators/ClipGenerator", order = 10)]
    public sealed class ClipGeneratorAsset : ScriptableObject, IAudioGenerator
    {
        [SerializeField]
        private AudioClip clip;

        [SerializeField] [Range(min: 0.0f, max: 2.0f)]
        private float gain = 1.0f;

        [SerializeField]
        [FormerlySerializedAs("resampleMode")]
        private ResampleMode _resampleMode = ResampleMode.Auto;

        [SerializeField]
        [FormerlySerializedAs("resampleQuality")]
        private ResampleQuality _resampleQuality = ResampleQuality.Linear;

        [SerializeField]
        [FormerlySerializedAs("loop")]
        private bool _loop;

        public ResampleMode resampleMode
        {
            get => _resampleMode;
            set => _resampleMode = value;
        }

        public ResampleQuality resampleQuality
        {
            get => _resampleQuality;
            set => _resampleQuality = value;
        }

        public bool loop
        {
            get => _loop;
            set => _loop = value;
        }

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
                Gain = gain,
                ResampleMode = _resampleMode,
                ResampleQuality = _resampleQuality,
                IsValid = false
            };

            // AudioClip.GetData requires DecompressOnLoad; streaming clips are not supported
            if (clip != null && ClipRequirements.CanUseGetData(clip: clip) && ClipRequirements.EnsureLoaded(clip: clip))
            {
                int clipFrames = clip.samples;
                int clipChannels = clip.channels;
                int requiredFloats = clipFrames * clipChannels;

                if (requiredFloats > 0)
                {
                    realtime.ClipDataInterleaved = NativeBufferPool.Rent(length: requiredFloats);
                    realtime.ClipDataIsPooled = true;

                    bool ok = clip.GetData(data: realtime.ClipDataInterleaved, offsetSamples: 0);

                    realtime.ClipChannels = clipChannels;
                    realtime.ClipSampleRate = clip.frequency;
                    realtime.ClipTotalFrames = clipFrames;
                    realtime.SourceFramePosition = 0.0f;
                    realtime.IsValid = ok;
                }
            }

            var control = new ClipGeneratorControl(
                loop: _loop,
                gain: gain,
                resampleMode: _resampleMode,
                resampleQuality: _resampleQuality);

            return context.AllocateGenerator(realtimeState: realtime, controlState: control, nestedFormat: nestedConfiguration, creationParameters: creationParameters);
        }
    }
}
