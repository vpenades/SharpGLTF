using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;

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
        public static void FillFrom(this IList<UInt32> dst, int dstIndex, IEnumerable<Int32> src)
        {
            using (var ator = src.GetEnumerator())
            {
                while (dstIndex < dst.Count && ator.MoveNext())
                {
                    dst[dstIndex++] = (UInt32)ator.Current;
                }
            }
        }

        public static void FillFrom<T>(this IList<T> dst, int dstIndex, IEnumerable<T> src)
        {
            using (var ator = src.GetEnumerator())
            {
                while (dstIndex < dst.Count && ator.MoveNext())
                {
                    dst[dstIndex++] = ator.Current;
                }
            }
        }

        public static void CopyTo<T>(this IReadOnlyList<T> src, IList<T> dst, int dstOffset = 0)
        {
            for (int i = 0; i < src.Count; ++i)
            {
                dst[i + dstOffset] = src[i];
            }
        }

        public static int FirstIndexOf<T>(this IReadOnlyList<T> src, T value)
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
