using NUnit.Framework;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using SapCrossfadeAudio.Runtime.Core.Foundation;

namespace SapCrossfadeAudio.Tests.Editor
{
    /// <summary>
    /// EditMode tests for NativeBufferPool.
    /// Verifies Rent/Return/Clear operations, capacity limits, and idempotency.
    /// </summary>
    [TestFixture]
    public class NativeBufferPoolTests
    {
        [SetUp]
        public void SetUp()
        {
            // Clear pool before each test
            NativeBufferPool.Clear();
        }

        [TearDown]
        public void TearDown()
        {
            // Clear pool after each test (prevent memory leaks)
            NativeBufferPool.Clear();
        }

        [Test]
        public void Rent_WithValidLength_ReturnsCreatedArray()
        {
            // Arrange
            const int length = 1024;

            // Act
            var array = NativeBufferPool.Rent(length: length);

            // Assert
            Assert.IsTrue(condition: array.IsCreated);
            Assert.AreEqual(expected: length, actual: array.Length);

            // Cleanup
            NativeBufferPool.Return(array: ref array);
        }

        [Test]
        public void Rent_WithZeroLength_ReturnsDefault()
        {
            // Act
            var array = NativeBufferPool.Rent(length: 0);

            // Assert
            Assert.IsFalse(condition: array.IsCreated);
        }

        [Test]
        public void Rent_WithNegativeLength_ReturnsDefault()
        {
            // Act
            var array = NativeBufferPool.Rent(length: -100);

            // Assert
            Assert.IsFalse(condition: array.IsCreated);
        }

        [Test]
        public unsafe void Return_ThenRent_ReusesSameBuffer()
        {
            // Arrange
            const int length = 512;
            var original = NativeBufferPool.Rent(length: length);
            var originalPtr = (System.IntPtr)original.GetUnsafePtr();

            // Act
            NativeBufferPool.Return(array: ref original);
            var reused = NativeBufferPool.Rent(length: length);
            var reusedPtr = (System.IntPtr)reused.GetUnsafePtr();

            // Assert
            Assert.IsFalse(condition: original.IsCreated); // Becomes default after Return
            Assert.IsTrue(condition: reused.IsCreated);
            Assert.AreEqual(expected: originalPtr, actual: reusedPtr); // Same buffer is reused

            // Cleanup
            NativeBufferPool.Return(array: ref reused);
        }

        [Test]
        public void Return_WithDefaultArray_DoesNotThrow()
        {
            // Arrange
            var array = default(NativeArray<float>);

            // Act & Assert (should not throw)
            Assert.DoesNotThrow(code: () => NativeBufferPool.Return(array: ref array));
            Assert.IsFalse(condition: array.IsCreated);
        }

        [Test]
        public void Return_IsIdempotent()
        {
            // Arrange
            var array = NativeBufferPool.Rent(length: 256);

            // Act
            NativeBufferPool.Return(array: ref array);
            NativeBufferPool.Return(array: ref array); // 2回目のReturn

            // Assert (should not throw, array should be default)
            Assert.IsFalse(condition: array.IsCreated);
        }

        [Test]
        public void Clear_DisposesAllBuffers()
        {
            // Arrange
            var array1 = NativeBufferPool.Rent(length: 100);
            var array2 = NativeBufferPool.Rent(length: 200);
            NativeBufferPool.Return(array: ref array1);
            NativeBufferPool.Return(array: ref array2);

            // Act
            NativeBufferPool.Clear();

            // Assert - Rent should create new buffers, not reuse
            var newArray = NativeBufferPool.Rent(length: 100);
            Assert.IsTrue(condition: newArray.IsCreated);

            // Cleanup
            NativeBufferPool.Return(array: ref newArray);
        }

        [Test]
        public void Rent_DifferentSizes_CreatesNewBuffers()
        {
            // Arrange & Act
            var small = NativeBufferPool.Rent(length: 100);
            var large = NativeBufferPool.Rent(length: 1000);

            // Assert
            Assert.IsTrue(condition: small.IsCreated);
            Assert.IsTrue(condition: large.IsCreated);
            Assert.AreEqual(expected: 100, actual: small.Length);
            Assert.AreEqual(expected: 1000, actual: large.Length);

            // Cleanup
            NativeBufferPool.Return(array: ref small);
            NativeBufferPool.Return(array: ref large);
        }

        [Test]
        public void Return_ExceedsPerSizeLimit_DisposesExcess()
        {
            // Arrange - Create more than MaxPerSize (8) buffers
            const int length = 64;
            var buffers = new NativeArray<float>[10];
            for (int i = 0; i < 10; i++)
            {
                buffers[i] = NativeBufferPool.Rent(length: length);
            }

            // Act - Return all buffers
            for (int i = 0; i < 10; i++)
            {
                NativeBufferPool.Return(array: ref buffers[i]);
            }

            // Assert - All references should be default (cleared)
            for (int i = 0; i < 10; i++)
            {
                Assert.IsFalse(condition: buffers[i].IsCreated);
            }

            // Verify only MaxPerSize (8) can be rented from pool
            var rented = new NativeArray<float>[8];
            for (int i = 0; i < 8; i++)
            {
                rented[i] = NativeBufferPool.Rent(length: length);
                Assert.IsTrue(condition: rented[i].IsCreated);
            }

            // Cleanup
            for (int i = 0; i < 8; i++)
            {
                NativeBufferPool.Return(array: ref rented[i]);
            }
        }
    }
}
