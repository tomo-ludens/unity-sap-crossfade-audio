namespace SapCrossfadeAudio.Runtime.Core.Types
{
    /// <summary>
    /// Parameters converted to a format directly consumable by the Realtime (audio thread).
    /// </summary>
    internal readonly struct CrossfadeRealtimeParams
    {
        public readonly float TargetPosition;
        public readonly float DurationSamples;
        public readonly CrossfadeCurve Curve;

        public CrossfadeRealtimeParams(float targetPosition, float durationSamples, CrossfadeCurve curve)
        {
            TargetPosition = targetPosition;
            DurationSamples = durationSamples;
            Curve = curve;
        }
    }
}
