using System.Runtime.CompilerServices;
using Unity.Burst;
using Unity.Collections;
using Unity.IntegerTime;
using UnityEngine.Audio;
using static UnityEngine.Audio.ProcessorInstance;
using SapCrossfadeAudio.Runtime.Core.Foundation;
using SapCrossfadeAudio.Runtime.Core.Foundation.Resampling;
using UnityEngine;

namespace SapCrossfadeAudio.Runtime.Core.Generators.Clip
{
    [BurstCompile(CompileSynchronously = true)]
    internal struct ClipGeneratorRealtime : GeneratorInstance.IRealtime
    {
        // PCM: interleaved (frames * channels)
        internal NativeArray<float> ClipDataInterleaved;
        internal int ClipChannels;
        internal int ClipSampleRate;
        internal int ClipTotalFrames;

        internal int OutputSampleRate;
        internal int OutputChannels;

        internal bool Loop;
        internal float Gain;

        internal ResampleMode ResampleMode;
        internal ResampleQuality ResampleQuality;

        internal float SourceFramePosition;

        internal bool IsValid;

        public bool isFinite => !Loop;
        public bool isRealtime => false;
        public DiscreteTime? length => null;

        public void Update(UpdatedDataContext context, Pipe pipe)
        {
            // v1.1.0: この段階ではイベントなし
        }

        public GeneratorInstance.Result Process(
            in RealtimeContext context,
            Pipe pipe,
            ChannelBuffer buffer,
            GeneratorInstance.Arguments args)
        {
            if (!IsValid)
            {
                return 0;
            }

            int requestedFrames = buffer.frameCount;

            // 現段階では ch 不一致は無音（将来 up/down-mix を入れるならここを拡張）
            if (buffer.channelCount != OutputChannels || OutputChannels != ClipChannels)
            {
                ZeroFill(buffer: buffer, startFrame: 0, frameCount: requestedFrames);
                return buffer.frameCount;
            }

            bool sampleRateMismatch = ClipSampleRate != OutputSampleRate;

            if (ResampleMode == ResampleMode.Off && sampleRateMismatch)
            {
                // 旧互換: 不一致は枯渇扱い（親がいる場合は不足分 0 埋めへ）
                return 0;
            }

            bool doResample = ResampleMode == ResampleMode.Force || sampleRateMismatch;

            int writtenFrames = 0;

            if (!doResample)
            {
                // 1:1 copy
                for (int frame = 0; frame < requestedFrames; frame++)
                {
                    int srcFrame = (int)SourceFramePosition;

                    if (!Loop && srcFrame >= ClipTotalFrames)
                    {
                        break;
                    }

                    srcFrame = WrapFrame(frameIndex: srcFrame, totalFrames: ClipTotalFrames, loop: Loop);

                    int baseIndex = srcFrame * ClipChannels;

                    for (int ch = 0; ch < ClipChannels; ch++)
                    {
                        buffer[channel: ch, frame: frame] = ClipDataInterleaved[index: baseIndex + ch] * Gain;
                    }

                    SourceFramePosition += 1.0f;
                    writtenFrames++;
                }
            }
            else
            {
                float step = 1f * ClipSampleRate / OutputSampleRate;
                for (int frame = 0; frame < requestedFrames; frame++)
                {
                    int src0 = (int)SourceFramePosition;

                    if (!Loop && src0 >= ClipTotalFrames)
                    {
                        break;
                    }

                    float t = SourceFramePosition - src0;

                    int srcm1 = WrapFrame(frameIndex: src0 - 1, totalFrames: ClipTotalFrames, loop: Loop);
                    int src1  = WrapFrame(frameIndex: src0 + 1, totalFrames: ClipTotalFrames, loop: Loop);
                    int src2  = WrapFrame(frameIndex: src0 + 2, totalFrames: ClipTotalFrames, loop: Loop);
                    src0      = WrapFrame(frameIndex: src0,     totalFrames: ClipTotalFrames, loop: Loop);

                    int baseM1 = srcm1 * ClipChannels;
                    int base0  = src0  * ClipChannels;
                    int base1  = src1  * ClipChannels;
                    int base2  = src2  * ClipChannels;

                    for (int ch = 0; ch < ClipChannels; ch++)
                    {
                        float xm1 = ClipDataInterleaved[index: baseM1 + ch];
                        float x0  = ClipDataInterleaved[index: base0 + ch];
                        float x1  = ClipDataInterleaved[index: base1 + ch];
                        float x2  = ClipDataInterleaved[index: base2 + ch];

                        buffer[channel: ch, frame: frame] =
                            Resampler.Interp(
                                q: ResampleQuality,
                                xm1: xm1,
                                x0: x0,
                                x1: x1,
                                x2: x2,
                                t: t) * Gain;
                    }

                    SourceFramePosition += step;
                    writtenFrames++;
                }
            }

            // 不足分 0 埋め（安全側）
            if (writtenFrames < requestedFrames)
            {
                ZeroFill(buffer: buffer, startFrame: writtenFrames, frameCount: requestedFrames - writtenFrames);
            }

            // Unity 公式例同様「書き込んだフレーム数」を返す。
            return writtenFrames;
        }

        [MethodImpl(methodImplOptions: MethodImplOptions.AggressiveInlining)]
        private static int WrapFrame(int frameIndex, int totalFrames, bool loop)
        {
            if (totalFrames <= 0)
            {
                return 0;
            }

            if (loop)
            {
                int m = frameIndex % totalFrames;
                return m < 0 ? (m + totalFrames) : m;
            }

            if (frameIndex < 0)
            {
                return 0;
            }

            if (frameIndex >= totalFrames)
            {
                return totalFrames - 1;
            }

            return frameIndex;
        }

        [MethodImpl(methodImplOptions: MethodImplOptions.AggressiveInlining)]
        private static void ZeroFill(ChannelBuffer buffer, int startFrame, int frameCount)
        {
            int endFrame = startFrame + frameCount;

            for (int frame = startFrame; frame < endFrame; frame++)
            {
                for (int ch = 0; ch < buffer.channelCount; ch++)
                {
                    buffer[channel: ch, frame: frame] = 0.0f;
                }
            }
        }
    }

    internal readonly struct ClipGeneratorControl : GeneratorInstance.IControl<ClipGeneratorRealtime>
    {
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
                _ => 2
            };
        }
    }
}
