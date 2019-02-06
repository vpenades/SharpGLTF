using System;
using System.Collections;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace glTF2Sharp.Memory
{
    public interface IAccessor<T> // : IReadOnlyList<T>
        where T : unmanaged
    {
        int Count { get; }
        T this[int index] { get; set; }

        void CopyTo(ArraySegment<T> dst);
    }


    struct AccessorEnumerator<T> : IEnumerator<T>
        where T: unmanaged
    {
        #region lifecycle

        public AccessorEnumerator(IAccessor<T> accessor)
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

        private readonly IAccessor<T> _Accessor;
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

    public static class AccessorsUtils
    {
        public static void Copy<T>(IAccessor<T> src, T[] dst) where T : unmanaged
        {
            Copy<T>(src, new ArraySegment<T>(dst));
        }

        public static void Copy<T>(IAccessor<T> src, ArraySegment<T> dst) where T : unmanaged
        {
            var c = src.Count;
            for (int i = 0; i < c; ++i) dst.Array[dst.Offset + i] = src[i];
        }        

        public static (Single, Single) GetBounds(ScalarAccessor accesor)
        {
            var min = Single.MaxValue;
            var max = Single.MinValue;

            int c = accesor.Count;
            for (int i = 0; i < c; ++i)
            {
                var v = accesor[i];
                min = Math.Min(min, v);
                max = Math.Max(max, v);
            }

            return (min, max);
        }

        public static (Vector2, Vector2) GetBounds(Vector2Accessor accesor)
        {
            var min = new Vector2(Single.MaxValue);
            var max = new Vector2(Single.MinValue);

            int c = accesor.Count;
            for (int i = 0; i < c; ++i)
            {
                var v = accesor[i];
                min = Vector2.Min(min, v);
                max = Vector2.Max(max, v);
            }

            return (min, max);
        }

        public static (Vector3, Vector3) GetBounds(Vector3Accessor accesor)
        {
            var min = new Vector3(Single.MaxValue);
            var max = new Vector3(Single.MinValue);

            int c = accesor.Count;
            for (int i = 0; i < c; ++i)
            {
                var v = accesor[i];
                min = Vector3.Min(min, v);
                max = Vector3.Max(max, v);
            }

            return (min, max);
        }

        public static (Vector4, Vector4) GetBounds(Vector4Accessor accesor)
        {
            var min = new Vector4(Single.MaxValue);
            var max = new Vector4(Single.MinValue);

            int c = accesor.Count;
            for (int i = 0; i < c; ++i)
            {
                var v = accesor[i];
                min = Vector4.Min(min, v);
                max = Vector4.Max(max, v);
            }

            return (min, max);
        }
    }
}
