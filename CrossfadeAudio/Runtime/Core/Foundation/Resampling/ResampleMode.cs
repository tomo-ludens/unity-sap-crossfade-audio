namespace CrossfadeAudio.Runtime.Core.Foundation.Resampling
{
    public enum ResampleMode
    {
        Off,    // 不一致は無音 fallback（従来互換）
        Auto,   // 不一致なら resample（推奨）
        Force   // 一致でも resample（検証用途）
    }
}
