using UnityEngine;
using UnityEngine.Audio;
using SapCrossfadeAudio.Runtime.Core.Foundation;
using SapCrossfadeAudio.Runtime.Core.Foundation.Resampling;
using static UnityEngine.Audio.ProcessorInstance;

namespace SapCrossfadeAudio.Runtime.Core.Generators.Clip
{
    /// <summary>
    /// Control-side state for ClipGenerator. Handles configuration and resource cleanup.
    /// </summary>
    internal readonly struct ClipGeneratorControl : GeneratorInstance.IControl<ClipGeneratorRealtime>
    {
        private const int DefaultChannelCount = 2;

        private readonly bool _loop;
        private readonly float _gain;
        private readonly ResampleMode _resampleMode;
        private readonly ResampleQuality _resampleQuality;

        public ClipGeneratorControl(
            bool loop,
            float gain,
            ResampleMode resampleMode,
            ResampleQuality resampleQuality)
        {
            _loop = loop;
            _gain = gain;
            _resampleMode = resampleMode;
            _resampleQuality = resampleQuality;
        }

        public void Dispose(ControlContext context, ref ClipGeneratorRealtime realtime)
        {
            if (realtime.ClipDataInterleaved.IsCreated)
            {
                NativeBufferPool.Return(array: ref realtime.ClipDataInterleaved);
            }

            realtime.IsValid = false;
        }

        public void Update(ControlContext context, Pipe pipe)
        {
        }

        public Response OnMessage(ControlContext context, Pipe pipe, Message message)
            => Response.Unhandled;

        public void Configure(
            ControlContext context,
            ref ClipGeneratorRealtime realtime,
            in AudioFormat format,
            out GeneratorInstance.Setup setup,
            ref GeneratorInstance.Properties properties)
        {
            realtime.Loop = _loop;
            realtime.Gain = _gain;
            realtime.ResampleMode = _resampleMode;
            realtime.ResampleQuality = _resampleQuality;

            realtime.OutputSampleRate = format.sampleRate;
            realtime.OutputChannels = ChannelCountFromSpeakerMode(speakerMode: format.speakerMode);

            setup = new GeneratorInstance.Setup(
                speakerMode: format.speakerMode,
                sampleRate: format.sampleRate);
        }

        private static int ChannelCountFromSpeakerMode(AudioSpeakerMode speakerMode)
        {
            return speakerMode switch
            {
                AudioSpeakerMode.Mono => 1,
                AudioSpeakerMode.Stereo => 2,
                AudioSpeakerMode.Quad => 4,
                AudioSpeakerMode.Surround => 5,
                AudioSpeakerMode.Mode5point1 => 6,
                AudioSpeakerMode.Mode7point1 => 8,
                _ => DefaultChannelCount
            };
        }
    }
}
