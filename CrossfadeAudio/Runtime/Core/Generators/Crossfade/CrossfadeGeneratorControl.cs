using System.Runtime.CompilerServices;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine.Audio;
using static UnityEngine.Audio.ProcessorInstance;
using TomoLudens.CrossfadeAudio.Runtime.Core.Foundation;
using TomoLudens.CrossfadeAudio.Runtime.Core.Types;

namespace TomoLudens.CrossfadeAudio.Runtime.Core.Generators.Crossfade
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
            // 公式サンプルと同様：ホストの format に合わせる :contentReference[oaicite:3]{index=3}
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

            // 子の Configure（親と同じ format を提案）
            realtime.ChildAFormatCompatible = false;
            if (!realtime.ChildA.Equals(other: default))
            {
                var nestedFormatA = format;
                context.Configure(generatorInstance: realtime.ChildA, format: nestedFormatA);

                var cfgA = context.GetConfiguration(generatorInstance: realtime.ChildA);
                realtime.ChildAFormatCompatible = cfgA.setup.sampleRate == format.sampleRate && cfgA.setup.speakerMode == format.speakerMode;
            }
            else
            {
                realtime.ChildAFormatCompatible = false;
            }

            realtime.ChildBFormatCompatible = false;
            if (!realtime.ChildB.Equals(other: default))
            {
                var nestedFormatB = format;
                context.Configure(generatorInstance: realtime.ChildB, format: nestedFormatB);

                var cfgB = context.GetConfiguration(generatorInstance: realtime.ChildB);
                realtime.ChildBFormatCompatible = cfgB.setup.sampleRate == format.sampleRate && cfgB.setup.speakerMode == format.speakerMode;
            }
            else
            {
                realtime.ChildBFormatCompatible = false;
            }

            // 作業バッファ確保（最大想定：format.bufferFrameCount）
            int requiredFloats = format.bufferFrameCount * outputChannels;
            ResizeWorkBufferIfNeeded(requiredFloats: requiredFloats, buffer: ref realtime.BufferDataA);
            ResizeWorkBufferIfNeeded(requiredFloats: requiredFloats, buffer: ref realtime.BufferDataB);

            // 初期値
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
        private static void ResizeWorkBufferIfNeeded(int requiredFloats, ref NativeArray<float> buffer)
        {
            if (requiredFloats <= 0)
            {
                NativeBufferPool.Return(array: ref buffer);
                return;
            }

            if (buffer.IsCreated && buffer.Length == requiredFloats)
            {
                return;
            }

            NativeBufferPool.Return(array: ref buffer);
            buffer = NativeBufferPool.Rent(length: requiredFloats);
        }
    }
}
