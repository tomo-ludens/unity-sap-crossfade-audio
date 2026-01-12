namespace SapCrossfadeAudio.Runtime.Core.Foundation.Resampling
{
    public enum ResampleMode
    {
        Off,    // Mismatch outputs silence (legacy compatibility)
        Auto,   // Resample on mismatch (recommended)
        Force   // Always resample (for testing)
    }
}
