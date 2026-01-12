using System.Runtime.CompilerServices;
using Unity.Burst;
using Unity.Collections;
using Unity.IntegerTime;
using UnityEngine.Audio;
using SapCrossfadeAudio.Runtime.Core.Foundation.Resampling;
using static UnityEngine.Audio.ProcessorInstance;

namespace SapCrossfadeAudio.Runtime.Core.Generators.Clip
{
    /// <summary>
    /// Burst-compiled realtime processor for AudioClip playback with optional resampling.
    /// </summary>
    [BurstCompile(CompileSynchronously = true)]
    internal struct ClipGeneratorRealtime : GeneratorInstance.IRealtime
    {
        // PCM data in interleaved format (frames * channels)
        internal NativeArray<float> ClipDataInterleaved;
        internal bool ClipDataIsPooled;
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
        }

        public GeneratorInstance.Result Process(
            in RealtimeContext context,
            Pipe pipe,
            ChannelBuffer buffer,
            GeneratorInstance.Arguments args)
        {
            int requestedFrames = buffer.frameCount;
            if (requestedFrames <= 0 || buffer.channelCount <= 0)
            {
                return 0;
            }

            if (!IsValid)
            {
                ZeroFill(buffer: buffer, startFrame: 0, frameCount: requestedFrames);
                // Fail-safe: keep AudioSource alive even if clip data is not ready/invalid.
                return requestedFrames;
            }

            if (!ClipDataInterleaved.IsCreated || ClipChannels <= 0 || ClipTotalFrames <= 0 || ClipSampleRate <= 0 || OutputSampleRate <= 0 || OutputChannels <= 0)
            {
                ZeroFill(buffer: buffer, startFrame: 0, frameCount: requestedFrames);
                return requestedFrames;
            }

            int requiredPcmFloats = ClipTotalFrames * ClipChannels;
            if (requiredPcmFloats <= 0 || ClipDataInterleaved.Length < requiredPcmFloats)
            {
                ZeroFill(buffer: buffer, startFrame: 0, frameCount: requestedFrames);
                return requestedFrames;
            }

            // Channel mismatch: output silence (future: add up/down-mix support here)
            if (buffer.channelCount != OutputChannels || OutputChannels != ClipChannels)
            {
                ZeroFill(buffer: buffer, startFrame: 0, frameCount: requestedFrames);
                return requestedFrames;
            }

            bool sampleRateMismatch = ClipSampleRate != OutputSampleRate;

            if (ResampleMode == ResampleMode.Off && sampleRateMismatch)
            {
                ZeroFill(buffer: buffer, startFrame: 0, frameCount: requestedFrames);
                return requestedFrames;
            }

            bool doResample = ResampleMode == ResampleMode.Force || sampleRateMismatch;

            int writtenFrames = 0;

            if (!doResample)
            {
                // Direct copy (no resampling needed)
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
                // Resampling ratio: how many source frames per output frame
                float step = (float)ClipSampleRate / OutputSampleRate;
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

                        buffer[channel: ch, frame: frame] = Resampler.Interp(
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

            // Zero-fill remaining frames (fail-safe)
            if (writtenFrames < requestedFrames)
            {
                ZeroFill(buffer: buffer, startFrame: writtenFrames, frameCount: requestedFrames - writtenFrames);
            }

            // Return written frame count (per Unity official examples)
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
}
