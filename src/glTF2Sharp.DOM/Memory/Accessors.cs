using System;
using System.Collections;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace glTF2Sharp.Memory
{
    public interface IAccessor<T> : IReadOnlyCollection<T>
        where T : unmanaged
    {        
        T this[int index] { get; set; }

        void CopyTo(ArraySegment<T> dst);

        (T, T) GetBounds();
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

        public static (Vector4, Vector4) GetBounds(IAccessor<Vector4> accesor)
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


    /// <summary>
    /// Wraps a collection of Scalar values and exposes it as a collection of Vector4 values
    /// </summary>
    struct _MapScalarToVector4 : IAccessor<Vector4>
    {
        public _MapScalarToVector4(ScalarAccessor source)
        {
            _Accessor = source;
        }

        private ScalarAccessor _Accessor;

        public int Count => _Accessor.Count;

        public Vector4 this[int index]
        {
            get => new Vector4(_Accessor[index], 0, 0, 0);
            set => _Accessor[index] = value.X;
        }        

        public void CopyTo(ArraySegment<Vector4> dst) { AccessorsUtils.Copy(this, dst); }

        public IEnumerator<Vector4> GetEnumerator() { return new AccessorEnumerator<Vector4>(this); }

        IEnumerator IEnumerable.GetEnumerator() { return new AccessorEnumerator<Vector4>(this); }

        public (Vector4, Vector4) GetBounds() { return AccessorsUtils.GetBounds(this); }
    }

    /// <summary>
    /// Wraps a collection of Vector2 values and exposes it as a collection of Vector4 values
    /// </summary>
    struct _MapVector2ToVector4 : IAccessor<Vector4>
    {
        public _MapVector2ToVector4(Vector2Accessor source)
        {
            _Accessor = source;
        }

        private Vector2Accessor _Accessor;

        public int Count => _Accessor.Count;

        public Vector4 this[int index]
        {
            get { var v = _Accessor[index]; return new Vector4(v.X, v.Y, 0, 0); }
            set => _Accessor[index] = new Vector2(value.X, value.Y);
        }

        public void CopyTo(ArraySegment<Vector4> dst) { AccessorsUtils.Copy(this, dst); }

        public IEnumerator<Vector4> GetEnumerator() { return new AccessorEnumerator<Vector4>(this); }

        IEnumerator IEnumerable.GetEnumerator() { return new AccessorEnumerator<Vector4>(this); }

        public (Vector4, Vector4) GetBounds() { return AccessorsUtils.GetBounds(this); }
    }

    /// <summary>
    /// Wraps a collection of Vector3 values and exposes it as a collection of Vector4 values
    /// </summary>
    struct _MapVector3ToVector4 : IAccessor<Vector4>
    {
        public _MapVector3ToVector4(Vector3Accessor source)
        {
            _Accessor = source;
        }

        private Vector3Accessor _Accessor;

        public int Count => _Accessor.Count;

        public Vector4 this[int index]
        {
            get { var v = _Accessor[index]; return new Vector4(v.X, v.Y, v.Z, 0); }
            set => _Accessor[index] = new Vector3(value.X, value.Y, value.Z);
        }

        public void CopyTo(ArraySegment<Vector4> dst) { AccessorsUtils.Copy(this, dst); }

        public IEnumerator<Vector4> GetEnumerator() { return new AccessorEnumerator<Vector4>(this); }

        IEnumerator IEnumerable.GetEnumerator() { return new AccessorEnumerator<Vector4>(this); }

        public (Vector4, Vector4) GetBounds() { return AccessorsUtils.GetBounds(this); }
    }

    /// <summary>
    /// Wraps a collection of Quaternion values and exposes it as a collection of Vector4 values
    /// </summary>
    struct _MapQuaternionToVector4 : IAccessor<Vector4>
    {
        public _MapQuaternionToVector4(QuaternionAccessor source)
        {
            _Accessor = source;
        }

        private QuaternionAccessor _Accessor;

        public int Count => _Accessor.Count;

        public Vector4 this[int index]
        {
            get { var v = _Accessor[index]; return new Vector4(v.X, v.Y, v.Z, v.W); }
            set => _Accessor[index] = new Quaternion(value.X, value.Y, value.Z, value.W);
        }

        public void CopyTo(ArraySegment<Vector4> dst) { AccessorsUtils.Copy(this, dst); }

        public IEnumerator<Vector4> GetEnumerator() { return new AccessorEnumerator<Vector4>(this); }

        IEnumerator IEnumerable.GetEnumerator() { return new AccessorEnumerator<Vector4>(this); }

        public (Vector4, Vector4) GetBounds() { return AccessorsUtils.GetBounds(this); }
    }
}
