using System.Collections.Generic;
using Unity.Collections;

namespace CrossfadeAudio.Runtime.Core.Foundation
{
    /// <summary>
    /// Control側のみで使う NativeArray&lt;float&gt; の簡易プール。
    /// Reconfigure が頻発する環境での alloc/free を抑制する。
    /// </summary>
    internal static class NativeBufferPool
    {
        private const int MaxPerSize = 8;
        private const long MaxTotalFloats = 8L * 1024L * 1024L; // 8M floats ≒ 32MB

        // NOTE:
        // - この値は「プールに保持している（＝Return済みで再利用可能な）総float数」を指す。
        // - Rent/Return で増減し、単調増加しない（上限判定が恒久的に悪化しない）ことが重要。
        private static long _sTotalPooledFloats;
        private static readonly Dictionary<int, Stack<NativeArray<float>>> SPool = new();

        public static NativeArray<float> Rent(int length)
        {
            if (length <= 0) return default;

            if (SPool.TryGetValue(key: length, value: out var stack) && stack.Count > 0)
            {
                var arr = stack.Pop();
                _sTotalPooledFloats -= length;
                if (_sTotalPooledFloats < 0) _sTotalPooledFloats = 0;
                return arr;
            }

            return new NativeArray<float>(length: length, allocator: Allocator.Persistent, options: NativeArrayOptions.UninitializedMemory);
        }

        public static void Return(ref NativeArray<float> array)
        {
            if (!array.IsCreated)
            {
                array = default;
                return;
            }

            int length = array.Length;

            // 上限超過時は「保持しない」で即Dispose（冪等・安全側）
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
