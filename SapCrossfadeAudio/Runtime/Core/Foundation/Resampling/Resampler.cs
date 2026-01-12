using System.Runtime.CompilerServices;

namespace SapCrossfadeAudio.Runtime.Core.Foundation.Resampling
{
    internal static class Resampler
    {
        [MethodImpl(methodImplOptions: MethodImplOptions.AggressiveInlining)]
        public static float Interp(ResampleQuality q, float xm1, float x0, float x1, float x2, float t)
        {
            switch (q)
            {
                case ResampleQuality.Nearest:
                    // Nearest neighbor: t < 0.5 uses x0, t >= 0.5 uses x1
                    return (t < 0.5f) ? x0 : x1;

                case ResampleQuality.Hermite4:
                    return Hermite4(xm1: xm1, x0: x0, x1: x1, x2: x2, t: t);

                case ResampleQuality.Linear:
                default:
                    return x0 + (x1 - x0) * t;
            }
        }

        [MethodImpl(methodImplOptions: MethodImplOptions.AggressiveInlining)]
        private static float Hermite4(float xm1, float x0, float x1, float x2, float t)
        {
            float c1 = 0.5f * (x1 - xm1);
            float c2 = xm1 - 2.5f * x0 + 2f * x1 - 0.5f * x2;
            float c3 = 0.5f * (x2 - xm1) + 1.5f * (x0 - x1);
            return ((c3 * t + c2) * t + c1) * t + x0;
        }
    }
}
