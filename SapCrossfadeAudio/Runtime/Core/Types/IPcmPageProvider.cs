using System;

namespace SapCrossfadeAudio.Runtime.Core.Types
{
    /// <summary>
    /// Interface for external PCM supply (true streaming support).
    /// </summary>
    public interface IPcmPageProvider
    {
        // interleaved: frames * channels
        int FillInterleaved(Span<float> dstInterleaved, int channelCount, int requestedFrames);
        bool EndOfStream { get; }
        int SourceSampleRate { get; } // 0 means unknown (treated as matching output)
    }
}
