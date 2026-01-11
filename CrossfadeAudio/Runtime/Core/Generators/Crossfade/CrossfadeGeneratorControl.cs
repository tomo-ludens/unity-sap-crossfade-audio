using System.Runtime.CompilerServices;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Audio;
using static UnityEngine.Audio.ProcessorInstance;
using CrossfadeAudio.Runtime.Core.Foundation;
using CrossfadeAudio.Runtime.Core.Types;

namespace CrossfadeAudio.Runtime.Core.Generators.Crossfade
{
    public struct CrossfadeGeneratorControl : GeneratorInstance.IControl<CrossfadeGeneratorRealtime>
    {
        public float InitialPosition01;
        public CrossfadeCurve InitialCurve;
        public float DefaultFadeSeconds;

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
                outputChannels = 2;
            }

            realtime.BufferChannelCount = outputChannels;
            realtime.SampleRate = format.sampleRate;

            // 重要：ここで context.Configure(child, ...) を呼ばない（ADTM例外の原因）
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

            // バッファは CreateInstance で事前割り当て済み（Job コンテキストでは Persistent 割り当て不可のため）

            float initialPos = math.clamp(valueToClamp: InitialPosition01, lowerBound: 0.0f, upperBound: 1.0f);
            realtime.CurrentCurve = InitialCurve;
            realtime.FadePosition01 = initialPos;
            realtime.TargetPosition01 = initialPos;
            realtime.FadeIncrementPerFrame = 0.0f;
        }

        public Response OnMessage(ControlContext context, Pipe pipe, Message message)
        {
            if (message.Is<CrossfadeCommand>())
            {
                pipe.SendData(
                    context: context,
                    data: message.Get<CrossfadeCommand>());

                return Response.Handled;
            }

            return Response.Unhandled;
        }

        public void Update(ControlContext context, Pipe pipe)
        {
            // Crossfade は Control 側 tick で特に何もしない
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

            return cfg.setup.speakerMode == expectedSpeakerMode &&
                   Mathf.Approximately(a: cfg.setup.sampleRate, b: expectedSampleRate);
        }

    }
}
