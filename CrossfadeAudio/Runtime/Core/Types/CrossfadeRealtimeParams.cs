namespace TomoLudens.CrossfadeAudio.Runtime.Core.Types
{
    /// <summary>
    /// Realtime側（audio thread）でそのまま消費できる形式に変換したパラメータ。
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
