using NUnit.Framework;
using CrossfadeAudio.Runtime.Core.Integration;
using CrossfadeAudio.Runtime.Core.Types;
using UnityEngine;
using UnityEngine.Audio;

namespace CrossfadeAudio.Tests.Runtime
{
    /// <summary>
    /// CrossfadeHandle の PlayMode テスト。
    /// コマンド送信と IsValid 判定を検証する。
    /// </summary>
    [TestFixture]
    public class CrossfadeHandleTests
    {
        private GameObject _testObject;
        private AudioSource _audioSource;

        [SetUp]
        public void SetUp()
        {
            _testObject = new GameObject(name: "TestAudioSource");
            _audioSource = _testObject.AddComponent<AudioSource>();
            _audioSource.playOnAwake = false;
        }

        [TearDown]
        public void TearDown()
        {
            if (_testObject != null)
            {
                Object.Destroy(obj: _testObject);
            }
        }

        [Test]
        public void FromAudioSource_WithNullSource_ReturnsInvalidHandle()
        {
            // Act
            var handle = CrossfadeHandle.FromAudioSource(source: null);

            // Assert
            Assert.IsFalse(condition: handle.IsValid);
        }

        [Test]
        public void FromAudioSource_WithNonPlayingSource_ReturnsInvalidHandle()
        {
            // Arrange - AudioSource is not playing

            // Act
            var handle = CrossfadeHandle.FromAudioSource(source: _audioSource);

            // Assert - Without a playing generator, IsValid should be false
            Assert.IsFalse(condition: handle.IsValid);
        }

        [Test]
        public void TryCrossfade_WithInvalidHandle_ReturnsFalse()
        {
            // Arrange
            var handle = CrossfadeHandle.FromAudioSource(source: null);

            // Act
            bool result = handle.TryCrossfade(targetPosition01: 0.5f, durationSeconds: 1f, curve: CrossfadeCurve.EqualPower);

            // Assert
            Assert.IsFalse(condition: result);
        }

        [Test]
        public void TryCrossfadeToA_WithInvalidHandle_ReturnsFalse()
        {
            // Arrange
            var handle = CrossfadeHandle.FromAudioSource(source: null);

            // Act
            bool result = handle.TryCrossfadeToA(durationSeconds: 1f, curve: CrossfadeCurve.Linear);

            // Assert
            Assert.IsFalse(condition: result);
        }

        [Test]
        public void TryCrossfadeToB_WithInvalidHandle_ReturnsFalse()
        {
            // Arrange
            var handle = CrossfadeHandle.FromAudioSource(source: null);

            // Act
            bool result = handle.TryCrossfadeToB(durationSeconds: 1f, curve: CrossfadeCurve.SCurve);

            // Assert
            Assert.IsFalse(condition: result);
        }

        [Test]
        public void TrySetImmediate_WithInvalidHandle_ReturnsFalse()
        {
            // Arrange
            var handle = CrossfadeHandle.FromAudioSource(source: null);

            // Act
            bool result = handle.TrySetImmediate(position01: 0.75f);

            // Assert
            Assert.IsFalse(condition: result);
        }

        [Test]
        public void DefaultHandle_IsNotValid()
        {
            // Arrange
            var handle = default(CrossfadeHandle);

            // Assert
            Assert.IsFalse(condition: handle.IsValid);
        }

        [Test]
        public void Constructor_WithDefaultInstance_IsNotValid()
        {
            // Arrange
            var instance = default(ProcessorInstance);
            var handle = new CrossfadeHandle(instance: instance);

            // Assert
            Assert.IsFalse(condition: handle.IsValid);
        }
    }
}
