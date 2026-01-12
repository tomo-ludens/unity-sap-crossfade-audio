using System.Runtime.CompilerServices;
using Unity.Burst;
using Unity.Collections;
using Unity.IntegerTime;
using Unity.Mathematics;
using UnityEngine.Audio;
using SapCrossfadeAudio.Runtime.Core.Foundation;
using SapCrossfadeAudio.Runtime.Core.Types;

namespace SapCrossfadeAudio.Runtime.Core.Generators.Crossfade
{
    [BurstCompile(CompileSynchronously = true, FloatMode = FloatMode.Fast, FloatPrecision = FloatPrecision.Medium)]
    public struct CrossfadeGeneratorRealtime : GeneratorInstance.IRealtime
    {
        public GeneratorInstance ChildA;
        public GeneratorInstance ChildB;

        public NativeArray<float> BufferDataA;
        public NativeArray<float> BufferDataB;
        public int BufferChannelCount;

        public float FadePosition01;
        public float TargetPosition01;
        public float FadeIncrementPerFrame;
        public CrossfadeCurve CurrentCurve;
        public float SampleRate;

        private bool _childAFinished;
        private bool _childBFinished;

        public bool ChildAFormatCompatible;
        public bool ChildBFormatCompatible;

        public bool isFinite => false;
        public bool isRealtime => false;
        public DiscreteTime? length => null;

        public void Update(ProcessorInstance.UpdatedDataContext context, ProcessorInstance.Pipe pipe)
        {
            foreach (var element in pipe.GetAvailableData(context: context))
            {
                if (element.TryGetData(data: out CrossfadeCommand command))
                {
                    ApplyCommand(command: in command);
                }
            }
        }

        public GeneratorInstance.Result Process(
            in RealtimeContext context,
            ProcessorInstance.Pipe pipe,
            ChannelBuffer buffer,
            GeneratorInstance.Arguments args)
        {
            int requestedFrames = buffer.frameCount;
            int channels = buffer.channelCount;

            if (requestedFrames <= 0 || channels <= 0)
            {
                return 0; // Result は int 返しで OK
            }

            buffer.Clear();

            int requiredFloats = requestedFrames * channels;

            bool canUseA =
                !_childAFinished &&
                ChildAFormatCompatible &&
                !ChildA.Equals(other: default) &&
                BufferDataA.IsCreated &&
                BufferDataA.Length >= requiredFloats;

            bool canUseB =
                !_childBFinished &&
                ChildBFormatCompatible &&
                !ChildB.Equals(other: default) &&
                BufferDataB.IsCreated &&
                BufferDataB.Length >= requiredFloats;

            int writtenA = 0;
            int writtenB = 0;

            ChannelBuffer childBufferA = default;
            ChannelBuffer childBufferB = default;

            if (canUseA)
            {
                var spanA = BufferDataA.AsSpan().Slice(start: 0, length: requiredFloats);
                childBufferA = new ChannelBuffer(buffer: spanA, channels: channels);
                childBufferA.Clear();

                var resultA = context.Process(generatorInstance: ChildA, buffer: childBufferA, args: args);
                writtenA = SapCompat.GetProcessedFrames(result: in resultA);

                if (SapCompat.IsShortWrite(result: in resultA, requestedFrames: requestedFrames))
                {
                    _childAFinished = true;
                }
            }

            if (canUseB)
            {
                var spanB = BufferDataB.AsSpan().Slice(start: 0, length: requiredFloats);
                childBufferB = new ChannelBuffer(buffer: spanB, channels: channels);
                childBufferB.Clear();

                var resultB = context.Process(generatorInstance: ChildB, buffer: childBufferB, args: args);
                writtenB = SapCompat.GetProcessedFrames(result: in resultB);

                if (SapCompat.IsShortWrite(result: in resultB, requestedFrames: requestedFrames))
                {
                    _childBFinished = true;
                }
            }

            int framesToProcess = math.max(x: writtenA, y: writtenB);
            framesToProcess = math.min(x: framesToProcess, y: requestedFrames);

            if (framesToProcess <= 0)
            {
                return 0;
            }

            Mix(
                output: ref buffer,
                canUseA: canUseA,
                canUseB: canUseB,
                childBufferA: ref childBufferA,
                childBufferB: ref childBufferB,
                frames: framesToProcess,
                channels: channels);

            return framesToProcess;
        }

        [MethodImpl(methodImplOptions: MethodImplOptions.AggressiveInlining)]
        private void ApplyCommand(in CrossfadeCommand command)
        {
            float target = math.clamp(valueToClamp: command.TargetPosition01, lowerBound: 0.0f, upperBound: 1.0f);
            float durationSeconds = math.max(x: 0.0f, y: command.DurationSeconds);

            CurrentCurve = command.Curve;
            TargetPosition01 = target;

            if (durationSeconds <= 0.0f || SampleRate <= 0.0f)
            {
                FadePosition01 = target;
                FadeIncrementPerFrame = 0.0f;
                return;
            }

            float durationFrames = math.max(x: 1.0f, y: durationSeconds * SampleRate);
            FadeIncrementPerFrame = (TargetPosition01 - FadePosition01) / durationFrames;
        }

        private void Mix(
            ref ChannelBuffer output,
            bool canUseA,
            bool canUseB,
            ref ChannelBuffer childBufferA,
            ref ChannelBuffer childBufferB,
            int frames,
            int channels)
        {
            float pos = FadePosition01;
            float inc = FadeIncrementPerFrame;
            float target = TargetPosition01;

            for (int frame = 0; frame < frames; frame++)
            {
                // 到達判定（オーバーシュート抑止）
                if (inc > 0.0f && pos >= target) { pos = target; inc = 0.0f; }
                if (inc < 0.0f && pos <= target) { pos = target; inc = 0.0f; }

                (float wA, float wB) = EvaluateWeights(position01: pos, curve: CurrentCurve);

                for (int ch = 0; ch < channels; ch++)
                {
                    float a = canUseA ? childBufferA[channel: ch, frame: frame] : 0.0f;
                    float b = canUseB ? childBufferB[channel: ch, frame: frame] : 0.0f;

                    output[channel: ch, frame: frame] = a * wA + b * wB;
                }

                pos += inc;
            }

            FadePosition01 = math.clamp(valueToClamp: pos, lowerBound: 0.0f, upperBound: 1.0f);
            FadeIncrementPerFrame = inc;
        }

        [MethodImpl(methodImplOptions: MethodImplOptions.AggressiveInlining)]
        private static (float wA, float wB) EvaluateWeights(float position01, CrossfadeCurve curve)
        {
            float p = math.clamp(valueToClamp: position01, lowerBound: 0.0f, upperBound: 1.0f);

            switch (curve)
            {
                case CrossfadeCurve.Linear:
                {
                    float wB = p;
                    float wA = 1.0f - p;
                    return (wA, wB);
                }
                case CrossfadeCurve.SCurve:
                {
                    float s = p * p * (3.0f - 2.0f * p); // smoothstep
                    float wB = s;
                    float wA = 1.0f - s;
                    return (wA, wB);
                }
                case CrossfadeCurve.EqualPower:
                default:
                {
                    float t = p * (math.PI * 0.5f);
                    float wA = math.cos(x: t);
                    float wB = math.sin(x: t);
                    return (wA, wB);
                }
            }
        }
    }
}
