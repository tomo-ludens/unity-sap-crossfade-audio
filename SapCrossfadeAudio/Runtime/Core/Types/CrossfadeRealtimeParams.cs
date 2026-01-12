namespace SapCrossfadeAudio.Runtime.Core.Types
{
    /// <summary>
    /// Parameters converted to a format directly consumable by the Realtime (audio thread).
    /// </summary>
    internal readonly struct CrossfadeRealtimeParams
    {
        internal readonly float TargetPosition;
        internal readonly float DurationSamples;
        internal readonly CrossfadeCurve Curve;

        internal CrossfadeRealtimeParams(float targetPosition, float durationSamples, CrossfadeCurve curve)
        {
            TargetPosition = targetPosition;
            DurationSamples = durationSamples;
            Curve = curve;
        }
    }
}
