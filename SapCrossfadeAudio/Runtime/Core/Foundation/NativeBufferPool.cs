using System.Collections.Generic;

using Unity.Collections;

namespace SapCrossfadeAudio.Runtime.Core.Foundation
{
    /// <summary>
    /// Simple NativeArray pool for Control-side only. Reduces alloc/free overhead during frequent Reconfigure.
    /// </summary>
    internal static class NativeBufferPool
    {
        private const int MaxPerSize = 8;
        private const long MaxTotalFloats = 8L * 1024L * 1024L; // 8M floats ≈ 32MB

        // Tracks total pooled floats (returned and reusable). Increments/decrements on Rent/Return.
        private static long _sTotalPooledFloats;
        private static readonly Dictionary<int, Stack<NativeArray<float>>> SPool = new();

        public static NativeArray<float> Rent(int length)
        {
            if (length <= 0) return default;

            if (!SPool.TryGetValue(key: length, value: out var stack) || stack.Count <= 0)
            {
                return new NativeArray<float>(
                    length: length,
                    allocator: Allocator.Persistent,
                    options: NativeArrayOptions.UninitializedMemory
                );
            }

            var arr = stack.Pop();
            _sTotalPooledFloats -= length;
            if (_sTotalPooledFloats < 0) _sTotalPooledFloats = 0;

            return arr;
        }

        public static void Return(ref NativeArray<float> array)
        {
            if (!array.IsCreated)
            {
                array = default;
                return;
            }

            int length = array.Length;

            // Exceeds capacity: dispose immediately instead of pooling (fail-safe)
            if (_sTotalPooledFloats + length > MaxTotalFloats)
            {
                array.Dispose();
                array = default;
                return;
            }

            if (!SPool.TryGetValue(key: length, value: out var stack))
            {
                stack = new Stack<NativeArray<float>>(capacity: MaxPerSize);
                SPool[key: length] = stack;
            }

            if (stack.Count >= MaxPerSize)
            {
                array.Dispose();
                array = default;
                return;
            }

            stack.Push(item: array);
            _sTotalPooledFloats += length;
            array = default;
        }

        public static void Clear()
        {
            foreach (var stack in SPool.Values)
            {
                while (stack.Count > 0)
                {
                    var arr = stack.Pop();
                    if (arr.IsCreated) arr.Dispose();
                }
            }

            SPool.Clear();
            _sTotalPooledFloats = 0;
        }
    }
}
