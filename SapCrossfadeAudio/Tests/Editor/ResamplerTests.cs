using NUnit.Framework;
using SapCrossfadeAudio.Runtime.Core.Foundation.Resampling;
using SapCrossfadeAudio.Runtime.Core.Generators.Crossfade;
using SapCrossfadeAudio.Runtime.Core.Types;

namespace SapCrossfadeAudio.Tests.Editor
{
    /// <summary>
    /// EditMode tests for Resampler.
    /// Verifies interpolation accuracy for each quality level (Nearest/Linear/Hermite4).
    /// </summary>
    [TestFixture]
    public class ResamplerTests
    {
        private const float Tolerance = 0.0001f;

        [Test]
        public void Interp_Nearest_AtZero_ReturnsX0()
        {
            // Arrange
            const float xm1 = 0f;
            const float x0  = 1f;
            const float x1  = 2f;
            const float x2  = 3f;
            const float t   = 0f;

            // Act
            float result = Resampler.Interp(q: ResampleQuality.Nearest, xm1: xm1, x0: x0, x1: x1, x2: x2, t: t);

            // Assert
            Assert.AreEqual(expected: x0, actual: result, delta: Tolerance);
        }

        [Test]
        public void Interp_Nearest_AtHalf_ReturnsX1()
        {
            // Arrange
            const float xm1 = 0f;
            const float x0  = 1f;
            const float x1  = 2f;
            const float x2  = 3f;
            const float t   = 0.5f;

            // Act
            float result = Resampler.Interp(q: ResampleQuality.Nearest, xm1: xm1, x0: x0, x1: x1, x2: x2, t: t);

            // Assert
            Assert.AreEqual(expected: x1, actual: result, delta: Tolerance);
        }

        [Test]
        public void Interp_Nearest_BelowHalf_ReturnsX0()
        {
            // Arrange
            const float xm1 = 0f;
            const float x0  = 1f;
            const float x1  = 2f;
            const float x2  = 3f;
            const float t   = 0.49f;

            // Act
            float result = Resampler.Interp(q: ResampleQuality.Nearest, xm1: xm1, x0: x0, x1: x1, x2: x2, t: t);

            // Assert
            Assert.AreEqual(expected: x0, actual: result, delta: Tolerance);
        }

        [Test]
        public void Interp_Nearest_AtOne_ReturnsX1()
        {
            // Arrange
            const float xm1 = 0f;
            const float x0  = 1f;
            const float x1  = 2f;
            const float x2  = 3f;
            const float t   = 1f;

            // Act
            float result = Resampler.Interp(q: ResampleQuality.Nearest, xm1: xm1, x0: x0, x1: x1, x2: x2, t: t);

            // Assert
            Assert.AreEqual(expected: x1, actual: result, delta: Tolerance);
        }

        [Test]
        public void Interp_Linear_AtZero_ReturnsX0()
        {
            // Arrange
            const float xm1 = 0f;
            const float x0  = 1f;
            const float x1  = 3f;
            const float x2  = 4f;
            const float t   = 0f;

            // Act
            float result = Resampler.Interp(q: ResampleQuality.Linear, xm1: xm1, x0: x0, x1: x1, x2: x2, t: t);

            // Assert
            Assert.AreEqual(expected: x0, actual: result, delta: Tolerance);
        }

        [Test]
        public void Interp_Linear_AtOne_ReturnsX1()
        {
            // Arrange
            const float xm1 = 0f;
            const float x0  = 1f;
            const float x1  = 3f;
            const float x2  = 4f;
            const float t   = 1f;

            // Act
            float result = Resampler.Interp(q: ResampleQuality.Linear, xm1: xm1, x0: x0, x1: x1, x2: x2, t: t);

            // Assert
            Assert.AreEqual(expected: x1, actual: result, delta: Tolerance);
        }

        [Test]
        public void Interp_Linear_AtHalf_ReturnsMidpoint()
        {
            // Arrange
            const float xm1 = 0f;
            const float x0  = 0f;
            const float x1  = 10f;
            const float x2  = 15f;
            const float t   = 0.5f;

            // Act
            float result = Resampler.Interp(q: ResampleQuality.Linear, xm1: xm1, x0: x0, x1: x1, x2: x2, t: t);

            // Assert
            Assert.AreEqual(expected: 5f, actual: result, delta: Tolerance);
        }

        [Test]
        public void Interp_Linear_AtQuarter_ReturnsCorrectValue()
        {
            // Arrange
            const float xm1 = 0f;
            const float x0  = 0f;
            const float x1  = 100f;
            const float x2  = 150f;
            const float t   = 0.25f;

            // Act
            float result = Resampler.Interp(q: ResampleQuality.Linear, xm1: xm1, x0: x0, x1: x1, x2: x2, t: t);

            // Assert - 0 + (100 - 0) * 0.25 = 25
            Assert.AreEqual(expected: 25f, actual: result, delta: Tolerance);
        }

        [Test]
        public void Interp_Hermite4_AtZero_ReturnsX0()
        {
            // Arrange
            const float xm1 = 0f;
            const float x0  = 1f;
            const float x1  = 2f;
            const float x2  = 3f;
            const float t   = 0f;

            // Act
            float result = Resampler.Interp(q: ResampleQuality.Hermite4, xm1: xm1, x0: x0, x1: x1, x2: x2, t: t);

            // Assert
            Assert.AreEqual(expected: x0, actual: result, delta: Tolerance);
        }

        [Test]
        public void Interp_Hermite4_AtOne_ReturnsX1()
        {
            // Arrange
            const float xm1 = 0f;
            const float x0  = 1f;
            const float x1  = 2f;
            const float x2  = 3f;
            const float t   = 1f;

            // Act
            float result = Resampler.Interp(q: ResampleQuality.Hermite4, xm1: xm1, x0: x0, x1: x1, x2: x2, t: t);

            // Assert
            Assert.AreEqual(expected: x1, actual: result, delta: Tolerance);
        }

        [Test]
        public void Interp_Hermite4_SmoothTransition()
        {
            // Arrange - Linear data should give smooth result
            const float xm1 = 0f;
            const float x0  = 1f;
            const float x1  = 2f;
            const float x2  = 3f;
            const float t   = 0.5f;

            // Act
            float result = Resampler.Interp(q: ResampleQuality.Hermite4, xm1: xm1, x0: x0, x1: x1, x2: x2, t: t);

            // Assert - For linear data, Hermite should also be linear
            Assert.AreEqual(expected: 1.5f, actual: result, delta: Tolerance);
        }

        [Test]
        public void Interp_Hermite4_PreservesMonotonicity()
        {
            // Arrange - Monotonically increasing data
            const float xm1 = 0f;
            const float x0  = 1f;
            const float x1  = 4f;
            const float x2  = 9f;

            // Act - Sample at multiple points
            float r0 = Resampler.Interp(q: ResampleQuality.Hermite4, xm1: xm1, x0: x0, x1: x1, x2: x2, t: 0f);
            float r25 = Resampler.Interp(q: ResampleQuality.Hermite4, xm1: xm1, x0: x0, x1: x1, x2: x2, t: 0.25f);
            float r50 = Resampler.Interp(q: ResampleQuality.Hermite4, xm1: xm1, x0: x0, x1: x1, x2: x2, t: 0.5f);
            float r75 = Resampler.Interp(q: ResampleQuality.Hermite4, xm1: xm1, x0: x0, x1: x1, x2: x2, t: 0.75f);
            float r1 = Resampler.Interp(q: ResampleQuality.Hermite4, xm1: xm1, x0: x0, x1: x1, x2: x2, t: 1f);

            // Assert - Should be monotonically increasing
            Assert.Less(arg1: r0, arg2: r25);
            Assert.Less(arg1: r25, arg2: r50);
            Assert.Less(arg1: r50, arg2: r75);
            Assert.Less(arg1: r75, arg2: r1);
        }

        [Test]
        public void Interp_DefaultQuality_UsesLinear()
        {
            // Arrange
            const float xm1 = 0f;
            const float x0  = 0f;
            const float x1  = 10f;
            const float x2  = 15f;
            const float t   = 0.5f;

            // Act
            float linearResult = Resampler.Interp(q: ResampleQuality.Linear, xm1: xm1, x0: x0, x1: x1, x2: x2, t: t);
            float defaultResult = Resampler.Interp(q: (ResampleQuality)999, xm1: xm1, x0: x0, x1: x1, x2: x2, t: t); // Invalid enum

            // Assert - Default should behave like Linear
            Assert.AreEqual(expected: linearResult, actual: defaultResult, delta: Tolerance);
        }

        [Test]
        public void CrossfadeWeights_AtPosition0_TargetBIsSilent_ForAllCurves()
        {
            foreach (var curve in (CrossfadeCurve[])System.Enum.GetValues(enumType: typeof(CrossfadeCurve)))
            {
                (float wA, float wB) = CrossfadeGeneratorRealtime.EvaluateWeightsForTesting(position01: 0f, curve: curve);
                Assert.AreEqual(expected: 1f, actual: wA, delta: Tolerance, message: $"curve={curve}");
                Assert.AreEqual(expected: 0f, actual: wB, delta: Tolerance, message: $"curve={curve}");
            }
        }

        [Test]
        public void CrossfadeWeights_AtPosition1_TargetAIsSilent_ForAllCurves()
        {
            foreach (var curve in (CrossfadeCurve[])System.Enum.GetValues(enumType: typeof(CrossfadeCurve)))
            {
                (float wA, float wB) = CrossfadeGeneratorRealtime.EvaluateWeightsForTesting(position01: 1f, curve: curve);
                Assert.AreEqual(expected: 0f, actual: wA, delta: Tolerance, message: $"curve={curve}");
                Assert.AreEqual(expected: 1f, actual: wB, delta: Tolerance, message: $"curve={curve}");
            }
        }
    }
}
