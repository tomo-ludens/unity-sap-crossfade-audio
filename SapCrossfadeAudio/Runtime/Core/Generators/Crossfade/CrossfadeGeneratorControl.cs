using System.Runtime.CompilerServices;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Audio;
using SapCrossfadeAudio.Runtime.Core.Foundation;
using SapCrossfadeAudio.Runtime.Core.Types;
using static UnityEngine.Audio.ProcessorInstance;

namespace SapCrossfadeAudio.Runtime.Core.Generators.Crossfade
{
    /// <summary>
    /// Control-side state for CrossfadeGenerator. Handles configuration, message routing, and resource cleanup.
    /// </summary>
    internal struct CrossfadeGeneratorControl : GeneratorInstance.IControl<CrossfadeGeneratorRealtime>
    {
        private const int DefaultChannelCount = 2;
        internal float InitialPosition01;
        internal CrossfadeCurve InitialCurve;

        public void Configure(
            ControlContext context,
            ref CrossfadeGeneratorRealtime realtime,
            in AudioFormat format,
            out GeneratorInstance.Setup setup,
            ref GeneratorInstance.Properties properties)
        {
            setup = new GeneratorInstance.Setup(
                speakerMode: format.speakerMode,
                sampleRate: format.sampleRate);

            int outputChannels = format.channelCount;
            if (outputChannels <= 0)
            {
                outputChannels = DefaultChannelCount;
            }

            realtime.BufferChannelCount = outputChannels;
            realtime.SampleRate = format.sampleRate;

            // IMPORTANT: Do not call context.Configure(child, ...) here - causes ADTM exception
            realtime.ChildAFormatCompatible = IsChildFormatCompatible(
                context: context,
                child: realtime.ChildA,
                expectedSpeakerMode: format.speakerMode,
                expectedSampleRate: format.sampleRate);

            realtime.ChildBFormatCompatible = IsChildFormatCompatible(
                context: context,
                child: realtime.ChildB,
                expectedSpeakerMode: format.speakerMode,
                expectedSampleRate: format.sampleRate);

            // Buffers pre-allocated in CreateInstance (Persistent unavailable in Job context)

            float initialPos = math.clamp(valueToClamp: InitialPosition01, lowerBound: 0.0f, upperBound: 1.0f);
            realtime.CurrentCurve = InitialCurve;
            realtime.FadePosition01 = initialPos;
            realtime.TargetPosition01 = initialPos;
            realtime.FadeIncrementPerFrame = 0.0f;
        }

        public Response OnMessage(ControlContext context, Pipe pipe, Message message)
        {
            if (!message.Is<CrossfadeCommand>()) return Response.Unhandled;

            pipe.SendData(context: context, data: message.Get<CrossfadeCommand>());
            return Response.Handled;
        }

        public void Update(ControlContext context, Pipe pipe)
        {
        }

        public void Dispose(ControlContext context, ref CrossfadeGeneratorRealtime realtime)
        {
            if (!realtime.ChildA.Equals(other: default))
            {
                context.Destroy(generatorInstance: realtime.ChildA);
                realtime.ChildA = default;
            }

            if (!realtime.ChildB.Equals(other: default))
            {
                context.Destroy(generatorInstance: realtime.ChildB);
                realtime.ChildB = default;
            }

            NativeBufferPool.Return(array: ref realtime.BufferDataA);
            NativeBufferPool.Return(array: ref realtime.BufferDataB);

            realtime.BufferChannelCount = 0;
            realtime.SampleRate = 0.0f;
        }

        [MethodImpl(methodImplOptions: MethodImplOptions.AggressiveInlining)]
        private static bool IsChildFormatCompatible(
            ControlContext context,
            GeneratorInstance child,
            AudioSpeakerMode expectedSpeakerMode,
            float expectedSampleRate)
        {
            if (child.Equals(other: default))
            {
                return false;
            }

            if (!context.Exists(processorInstance: child))
            {
                return false;
            }

            var cfg = context.GetConfiguration(generatorInstance: child);

            return cfg.setup.speakerMode == expectedSpeakerMode && Mathf.Approximately(a: cfg.setup.sampleRate, b: expectedSampleRate);
        }

    }
}
