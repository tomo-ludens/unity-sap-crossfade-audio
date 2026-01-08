using Unity.IntegerTime;
using UnityEngine;
using UnityEngine.Audio;
using static UnityEngine.Audio.ProcessorInstance;
using TomoLudens.CrossfadeAudio.Runtime.Core.Foundation;
using TomoLudens.CrossfadeAudio.Runtime.Core.Foundation.Resampling;

namespace TomoLudens.CrossfadeAudio.Runtime.Core.Generators.Clip
{
    [CreateAssetMenu(fileName = "ClipGenerator", menuName = "TomoLudens/CrossfadeAudio/Generators/ClipGenerator", order = 10)]
    public sealed class ClipGeneratorAsset : ScriptableObject, IAudioGenerator
    {
        private readonly AudioClip _clip;

        public bool loop;

        [Range(min: 0.0f, max: 2.0f)]
        public float gain = 1.0f;

        public ResampleMode resampleMode = ResampleMode.Auto;
        public ResampleQuality resampleQuality = ResampleQuality.Linear;

        public ClipGeneratorAsset(AudioClip clip)
        {
            _clip = clip;
        }

        public bool isFinite => !loop;
        public bool isRealtime => false;
        public DiscreteTime? length => null;

        public GeneratorInstance CreateInstance(
            ControlContext context,
            AudioFormat? nestedConfiguration,
            CreationParameters creationParameters)
        {
            var realtime = new ClipGeneratorRealtime
            {
                Loop = loop,
                Gain = gain,
                ResampleMode = resampleMode,
                ResampleQuality = resampleQuality,
                IsValid = false
            };

            // AudioClip.GetData は streamed では動かず、圧縮は DecompressOnLoad が必要。:contentReference[oaicite:4]{index=4}
            if (_clip != null && ClipRequirements.CanUseGetData(clip: _clip) && ClipRequirements.EnsureLoaded(clip: _clip))
            {
                int clipFrames = _clip.samples;
                int clipChannels = _clip.channels;
                int requiredFloats = clipFrames * clipChannels;

                if (requiredFloats > 0)
                {
                    realtime.ClipDataInterleaved = NativeBufferPool.Rent(length: requiredFloats);

                    bool ok = _clip.GetData(data: realtime.ClipDataInterleaved, offsetSamples: 0);

                    realtime.ClipChannels = clipChannels;
                    realtime.ClipSampleRate = _clip.frequency;
                    realtime.ClipTotalFrames = clipFrames;
                    realtime.SourceFramePosition = 0.0f;
                    realtime.IsValid = ok;
                }
            }

            var control = new ClipGeneratorControl(
                loop: loop,
                gain: gain,
                resampleMode: resampleMode,
                resampleQuality: resampleQuality);

            // 公式例：AllocateGenerator(realtime, control, nestedConfiguration, creationParameters)
            return context.AllocateGenerator(realtimeState: realtime, controlState: control, nestedFormat: nestedConfiguration, creationParameters: creationParameters);
        }
    }
}
