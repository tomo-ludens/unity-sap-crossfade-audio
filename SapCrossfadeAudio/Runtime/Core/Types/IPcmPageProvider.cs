using System;

namespace SapCrossfadeAudio.Runtime.Core.Types
{
    /// <summary>
    /// 外部PCM供給（真のストリーミング）用の供給インターフェース。
    /// </summary>
    public interface IPcmPageProvider
    {
        // interleaved: frames * channels
        int FillInterleaved(Span<float> dstInterleaved, int channelCount, int requestedFrames);
        bool EndOfStream { get; }
        int SourceSampleRate { get; } // 0なら不明（＝出力と同一扱い）
    }
}
