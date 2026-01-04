using System.Collections.Generic;
using Unity.Collections;

namespace TomoLudens.CrossfadeAudio.Runtime.Core.Foundation
{
    /// <summary>
    /// Control側のみで使う NativeArray&lt;float&gt; の簡易プール。
    /// Reconfigure が頻発する環境での alloc/free を抑制する。
    /// </summary>
    internal static class NativeBufferPool
    {
        public static int MaxPerSize = 8;
        public static long MaxTotalFloats = 8L * 1024L * 1024L; // 8M floats ≒ 32MB

        // NOTE:
        // - この値は「プールに保持している（＝Return済みで再利用可能な）総float数」を指す。
        // - Rent/Return で増減し、単調増加しない（上限判定が恒久的に悪化しない）ことが重要。
        private static long s_totalPooledFloats;
        private static readonly Dictionary<int, Stack<NativeArray<float>>> s_pool = new();

        public static NativeArray<float> Rent(int length)
        {
            if (length <= 0) return default;

            if (s_pool.TryGetValue(key: length, value: out var stack) && stack.Count > 0)
            {
                var arr = stack.Pop();
                s_totalPooledFloats -= length;
                if (s_totalPooledFloats < 0) s_totalPooledFloats = 0;
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
            if (s_totalPooledFloats + length > MaxTotalFloats)
            {
                array.Dispose();
                array = default;
                return;
            }

            if (!s_pool.TryGetValue(key: length, value: out var stack))
            {
                stack = new Stack<NativeArray<float>>(capacity: MaxPerSize);
                s_pool[key: length] = stack;
            }

            if (stack.Count >= MaxPerSize)
            {
                array.Dispose();
                array = default;
                return;
            }

            stack.Push(item: array);
            s_totalPooledFloats += length;
            array = default;
        }

        public static void Clear()
        {
            foreach (var kv in s_pool)
            {
                var stack = kv.Value;
                while (stack.Count > 0)
                {
                    var arr = stack.Pop();
                    if (arr.IsCreated) arr.Dispose();
                }
            }

            s_pool.Clear();
            s_totalPooledFloats = 0;
        }
    }
}
