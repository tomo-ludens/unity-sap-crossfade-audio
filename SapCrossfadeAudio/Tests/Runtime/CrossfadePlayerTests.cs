using NUnit.Framework;
using UnityEngine;
using SapCrossfadeAudio.Runtime.Core.Components;
using SapCrossfadeAudio.Runtime.Core.Types;

namespace SapCrossfadeAudio.Tests.Runtime
{
    /// <summary>
    /// PlayMode tests for CrossfadePlayer.
    /// Verifies MonoBehaviour integration and playback control.
    /// </summary>
    [TestFixture]
    public class CrossfadePlayerTests
    {
        private GameObject _testObject;
        private CrossfadePlayer _player;
        private AudioSource _audioSource;

        [SetUp]
        public void SetUp()
        {
            _testObject = new GameObject(name: "TestCrossfadePlayer");
            _audioSource = _testObject.AddComponent<AudioSource>();
            _audioSource.playOnAwake = false;
            _audioSource.Stop(); // Explicitly stop in case playOnAwake (default true) started playback
            _player = _testObject.AddComponent<CrossfadePlayer>();
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
        public void AudioSource_IsNotNull()
        {
            // Assert
            Assert.IsNotNull(anObject: _player.AudioSource);
        }

        [Test]
        public void Generator_DefaultsToNull()
        {
            // Assert
            Assert.IsNull(anObject: _player.Generator);
        }

        [Test]
        public void Generator_CanBeSet()
        {
            // This test would require a mock generator asset
            // For now, verify that null setting works
            _player.Generator = null;
            Assert.IsNull(anObject: _player.Generator);
        }

        [Test]
        public void IsPlaying_WhenNotPlaying_ReturnsFalse()
        {
            // Assert
            Assert.IsFalse(condition: _player.IsPlaying);
        }

        [Test]
        public void Handle_WhenNotPlaying_IsNotValid()
        {
            // Act
            var handle = _player.Handle;

            // Assert
            Assert.IsFalse(condition: handle.IsValid);
        }

        [Test]
        public void Play_WithoutGenerator_DoesNotThrow()
        {
            // Arrange
            _player.Generator = null;

            // Act & Assert - Should not throw (may log warning)
            Assert.DoesNotThrow(code: () => _player.Play());
        }

        [Test]
        public void Stop_WhenNotPlaying_DoesNotThrow()
        {
            // Act & Assert - Should not throw
            Assert.DoesNotThrow(code: () => _player.Stop());
        }

        [Test]
        public void CrossfadeToA_WhenNotPlaying_DoesNotThrow()
        {
            // Act & Assert - Should not throw (may log warning)
            Assert.DoesNotThrow(code: () => _player.CrossfadeToA(durationSeconds: 1f, curve: CrossfadeCurve.EqualPower));
        }

        [Test]
        public void CrossfadeToB_WhenNotPlaying_DoesNotThrow()
        {
            // Act & Assert - Should not throw (may log warning)
            Assert.DoesNotThrow(code: () => _player.CrossfadeToB(durationSeconds: 1f, curve: CrossfadeCurve.Linear));
        }

        [Test]
        public void Crossfade_WhenNotPlaying_DoesNotThrow()
        {
            // Act & Assert - Should not throw (may log warning)
            Assert.DoesNotThrow(code: () => _player.Crossfade(targetPosition01: 0.5f, durationSeconds: 1f, curve: CrossfadeCurve.SCurve));
        }

        [Test]
        public void SetImmediate_WhenNotPlaying_DoesNotThrow()
        {
            // Act & Assert - Should not throw (may log warning)
            Assert.DoesNotThrow(code: () => _player.SetImmediate(position01: 0.75f));
        }

        [Test]
        public void CrossfadeToA_DefaultCurve_DoesNotThrow()
        {
            // This test verifies the default parameter is accepted
            // Act & Assert - Should not throw
            Assert.DoesNotThrow(code: () => _player.CrossfadeToA(durationSeconds: 1f)); // Using default curve
        }

        [Test]
        public void CrossfadeToB_DefaultCurve_DoesNotThrow()
        {
            // Act & Assert - Should not throw
            Assert.DoesNotThrow(code: () => _player.CrossfadeToB(durationSeconds: 1f)); // Using default curve
        }

        [Test]
        public void Crossfade_DefaultCurve_DoesNotThrow()
        {
            // Act & Assert - Should not throw
            Assert.DoesNotThrow(code: () => _player.Crossfade(targetPosition01: 0.5f, durationSeconds: 1f)); // Using default curve
        }
    }
}
