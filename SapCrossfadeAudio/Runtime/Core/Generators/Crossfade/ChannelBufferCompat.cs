using UnityEngine.Audio;

namespace SapCrossfadeAudio.Runtime.Core.Generators.Crossfade
{
    internal static class ChannelBufferCompat
    {
        public static void ClearRange(ChannelBuffer buffer, int startFrame, int frameCount)
        {
            if (frameCount <= 0) return;

            int endFrame = startFrame + frameCount;
            if (startFrame < 0) startFrame = 0;
            if (endFrame > buffer.frameCount) endFrame = buffer.frameCount;

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
