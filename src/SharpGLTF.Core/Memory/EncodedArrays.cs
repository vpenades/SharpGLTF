using System;
using System.Collections;
using System.Collections.Generic;

namespace SharpGLTF.Memory
{
    struct EncodedArrayEnumerator<T> : IEnumerator<T>
    {
        #region lifecycle

        public EncodedArrayEnumerator(IReadOnlyList<T> accessor)
        {
            this._Accessor = accessor;
            this._Count = accessor.Count;
            this._Index = -1;
        }

        public void Dispose()
        {
        }

        #endregion

        #region data

        private readonly IReadOnlyList<T> _Accessor;
        private readonly int _Count;
        private int _Index;

        #endregion

        #region API

        public T Current => _Accessor[_Index];

        object IEnumerator.Current => _Accessor[_Index];

        public bool MoveNext()
        {
            ++_Index;
            return _Index < _Count;
        }

        public void Reset()
        {
            _Index = -1;
        }

        #endregion
    }

    static class EncodedArrayUtils
    {
        public static void _CopyTo(this IEnumerable<Int32> src, IList<UInt32> dst, int dstOffset = 0)
        {
            using (var ptr = src.GetEnumerator())
            {
                while (dstOffset < dst.Count && ptr.MoveNext())
                {
                    dst[dstOffset++] = (UInt32)ptr.Current;
                }
            }
        }

        public static void _CopyTo(this IEnumerable<Boolean> src, IList<UInt32> dst, int dstOffset = 0)
        {
            using (var ptr = src.GetEnumerator())
            {
                while (dstOffset < dst.Count && ptr.MoveNext())
                {
                    dst[dstOffset++] = (Byte)(ptr.Current ? 1 : 0);
                }
            }
        }

        public static void _CopyTo<T>(this IEnumerable<T> src, IList<T> dst, int dstOffset = 0)
        {
            using (var ptr = src.GetEnumerator())
            {
                while (dstOffset < dst.Count && ptr.MoveNext())
                {
                    dst[dstOffset++] = ptr.Current;
                }
            }
        }

        public static int _FirstIndexOf<T>(this IReadOnlyList<T> src, T value)
        {
            var comparer = EqualityComparer<T>.Default;

            var c = src.Count;

            for (int i = 0; i < c; ++i)
            {
                if (comparer.Equals(src[i], value)) return i;
            }

            return -1;
        }
    }
}
