namespace TomoLudens.CrossfadeAudio.Runtime.Core.Types
{
    /// <summary>
    /// Control側から送るクロスフェード指示。
    /// </summary>
    public readonly struct CrossfadeCommand
    {
        public readonly float TargetPosition;   // 0=A, 1=B
        public readonly float DurationSeconds;  // 秒
        public readonly CrossfadeCurve Curve;

        public CrossfadeCommand(float targetPosition, float durationSeconds, CrossfadeCurve curve)
        {
            TargetPosition = Clamp01(v: targetPosition);
            DurationSeconds = Max(a: durationSeconds, b: 0.001f);
            Curve = curve;
        }

        internal CrossfadeRealtimeParams ToRealtimeParams(float sampleRate)
        {
            // DurationSamples は後段で increment 算出に使うため float のまま保持
            float durationSamples = DurationSeconds * sampleRate;
            if (durationSamples < 1f) durationSamples = 1f;
            return new CrossfadeRealtimeParams(targetPosition: TargetPosition, durationSamples: durationSamples, curve: Curve);
        }

        private static float Clamp01(float v) => v < 0f ? 0f : (v > 1f ? 1f : v);
        private static float Max(float a, float b) => a > b ? a : b;
    }
}
