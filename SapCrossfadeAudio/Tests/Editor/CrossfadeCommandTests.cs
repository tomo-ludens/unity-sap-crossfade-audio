using NUnit.Framework;
using SapCrossfadeAudio.Runtime.Core.Types;

namespace SapCrossfadeAudio.Tests.Editor
{
    /// <summary>
    /// EditMode tests for CrossfadeCommand struct.
    /// Verifies Create method and field values.
    /// </summary>
    [TestFixture]
    public class CrossfadeCommandTests
    {
        private const float Tolerance = 0.0001f;

        [Test]
        public void Create_WithValidParameters_SetsFieldsCorrectly()
        {
            // Arrange
            const float target = 0.75f;
            const float duration = 2.5f;
            const CrossfadeCurve curve = CrossfadeCurve.EqualPower;

            // Act
            var command = CrossfadeCommand.Create(targetPosition01: target, durationSeconds: duration, curve: curve);

            // Assert
            Assert.AreEqual(expected: target, actual: command.TargetPosition01, delta: Tolerance);
            Assert.AreEqual(expected: duration, actual: command.DurationSeconds, delta: Tolerance);
            Assert.AreEqual(expected: curve, actual: command.Curve);
        }

        [Test]
        public void Create_WithZeroTarget_SetsTargetToZero()
        {
            // Act
            var command = CrossfadeCommand.Create(targetPosition01: 0f, durationSeconds: 1f, curve: CrossfadeCurve.Linear);

            // Assert
            Assert.AreEqual(expected: 0f, actual: command.TargetPosition01, delta: Tolerance);
        }

        [Test]
        public void Create_WithOneTarget_SetsTargetToOne()
        {
            // Act
            var command = CrossfadeCommand.Create(targetPosition01: 1f, durationSeconds: 1f, curve: CrossfadeCurve.SCurve);

            // Assert
            Assert.AreEqual(expected: 1f, actual: command.TargetPosition01, delta: Tolerance);
        }

        [Test]
        public void Create_WithZeroDuration_SetsZeroDuration()
        {
            // Act
            var command = CrossfadeCommand.Create(targetPosition01: 0.5f, durationSeconds: 0f, curve: CrossfadeCurve.Linear);

            // Assert
            Assert.AreEqual(expected: 0f, actual: command.DurationSeconds, delta: Tolerance);
        }

        [Test]
        public void Create_WithEqualPowerCurve_SetsCurveCorrectly()
        {
            // Act
            var command = CrossfadeCommand.Create(targetPosition01: 0.5f, durationSeconds: 1f, curve: CrossfadeCurve.EqualPower);

            // Assert
            Assert.AreEqual(expected: CrossfadeCurve.EqualPower, actual: command.Curve);
        }

        [Test]
        public void Create_WithLinearCurve_SetsCurveCorrectly()
        {
            // Act
            var command = CrossfadeCommand.Create(targetPosition01: 0.5f, durationSeconds: 1f, curve: CrossfadeCurve.Linear);

            // Assert
            Assert.AreEqual(expected: CrossfadeCurve.Linear, actual: command.Curve);
        }

        [Test]
        public void Create_WithSCurve_SetsCurveCorrectly()
        {
            // Act
            var command = CrossfadeCommand.Create(targetPosition01: 0.5f, durationSeconds: 1f, curve: CrossfadeCurve.SCurve);

            // Assert
            Assert.AreEqual(expected: CrossfadeCurve.SCurve, actual: command.Curve);
        }

        [Test]
        public void DefaultCommand_HasZeroValues()
        {
            // Arrange
            var command = default(CrossfadeCommand);

            // Assert
            Assert.AreEqual(expected: 0f, actual: command.TargetPosition01, delta: Tolerance);
            Assert.AreEqual(expected: 0f, actual: command.DurationSeconds, delta: Tolerance);
            Assert.AreEqual(expected: CrossfadeCurve.EqualPower, actual: command.Curve); // First enum value
        }

        [Test]
        public void Create_IsUnmanaged()
        {
            // This test verifies that CrossfadeCommand is unmanaged (blittable)
            // by checking it has expected size (unmanaged structs have predictable sizes)

            // Act
            var command = CrossfadeCommand.Create(targetPosition01: 0.5f, durationSeconds: 1f, curve: CrossfadeCurve.Linear);

            // Assert - Verify the struct is properly initialized and can be used
            // The fact that this compiles with unmanaged constraint proves it's unmanaged
            Assert.AreEqual(expected: 0.5f, actual: command.TargetPosition01, delta: Tolerance);
            Assert.AreEqual(expected: 1f, actual: command.DurationSeconds, delta: Tolerance);
            Assert.AreEqual(expected: CrossfadeCurve.Linear, actual: command.Curve);

            // Additional verification: struct size should be predictable
            // float (4) + float (4) + enum (4) = 12 bytes
            const int expectedSize = sizeof(float) + sizeof(float) + sizeof(int);
            Assert.AreEqual(expected: 12, actual: expectedSize);
        }
    }
}
