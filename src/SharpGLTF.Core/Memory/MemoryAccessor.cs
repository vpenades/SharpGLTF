using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Reflection;

using BYTES = System.ArraySegment<System.Byte>;

using DIMENSIONS = SharpGLTF.Schema2.DimensionType;
using ENCODING = SharpGLTF.Schema2.EncodingType;

namespace SharpGLTF.Memory
{
    /// <summary>
    /// Wraps a <see cref="BYTES"/> decoding it and exposing its content as arrays of different types.
    /// </summary>
    [System.Diagnostics.DebuggerDisplay("{_GetDebuggerDisplay(),nq}")]
    public sealed partial class MemoryAccessor
    {
        #region debug

        internal string _GetDebuggerDisplay()
        {
            return _Slicer._GetDebuggerDisplay();
        }

        #endregion

        #region constructor

        #if NETSTANDARD
        public MemoryAccessor(Byte[] data, MemoryAccessInfo info)
        {
            this._Slicer = info;
            this._Data = new ArraySegment<Byte>(data);
        }
        #endif

        public MemoryAccessor(BYTES data, MemoryAccessInfo info)
        {
            this._Slicer = info;
            this._Data = data;
        }

        public MemoryAccessor(MemoryAccessInfo info)
        {
            this._Slicer = info;
            this._Data = default;
        }

        public static IAccessorArray<Single> CreateScalarSparseArray(MemoryAccessor bottom, IntegerArray topKeys, MemoryAccessor topValues)
        {
            Guard.NotNull(bottom, nameof(bottom));
            Guard.NotNull(topValues, nameof(topValues));
            Guard.IsTrue(bottom._Slicer.Dimensions == topValues._Slicer.Dimensions, nameof(topValues));
            Guard.IsTrue(topKeys.Count <= bottom._Slicer.ItemsCount, nameof(topKeys));
            Guard.IsTrue(topKeys.Count == topValues._Slicer.ItemsCount, nameof(topValues));
            Guard.IsTrue(topKeys.All(item => item < (uint)bottom._Slicer.ItemsCount), nameof(topKeys));

            return new SparseArray<Single>(bottom.AsScalarArray(), topValues.AsScalarArray(), topKeys);
        }

        public static IAccessorArray<Single> CreateScalarSparseArray(int bottomCount, IntegerArray topKeys, MemoryAccessor topValues)
        {            
            Guard.NotNull(topValues, nameof(topValues));            
            Guard.IsTrue(topKeys.Count <= bottomCount, nameof(topKeys));
            Guard.IsTrue(topKeys.Count == topValues._Slicer.ItemsCount, nameof(topValues));
            Guard.IsTrue(topKeys.All(item => item < (uint)bottomCount), nameof(topKeys));

            return new SparseArray<Single>(new ZeroAccessorArray<float>(bottomCount), topValues.AsScalarArray(), topKeys);
        }


        public static IAccessorArray<Vector2> CreateVector2SparseArray(int bottomCount, IntegerArray topKeys, MemoryAccessor topValues)
        {            
            Guard.NotNull(topValues, nameof(topValues));            
            Guard.IsTrue(topKeys.Count <= bottomCount, nameof(topKeys));
            Guard.IsTrue(topKeys.Count == topValues._Slicer.ItemsCount, nameof(topValues));
            Guard.IsTrue(topKeys.All(item => item < (uint)bottomCount), nameof(topKeys));

            return new SparseArray<Vector2>(new ZeroAccessorArray<Vector2>(bottomCount), topValues.AsVector2Array(), topKeys);
        }

        public static IAccessorArray<Vector2> CreateVector2SparseArray(MemoryAccessor bottom, IntegerArray topKeys, MemoryAccessor topValues)
        {
            Guard.NotNull(bottom, nameof(bottom));
            Guard.NotNull(topValues, nameof(topValues));
            Guard.IsTrue(bottom._Slicer.Dimensions == topValues._Slicer.Dimensions, nameof(topValues));
            Guard.IsTrue(topKeys.Count <= bottom._Slicer.ItemsCount, nameof(topKeys));
            Guard.IsTrue(topKeys.Count == topValues._Slicer.ItemsCount, nameof(topValues));
            Guard.IsTrue(topKeys.All(item => item < (uint)bottom._Slicer.ItemsCount), nameof(topKeys));

            return new SparseArray<Vector2>(bottom.AsVector2Array(), topValues.AsVector2Array(), topKeys);
        }


        public static IAccessorArray<Vector3> CreateVector3SparseArray(int bottomCount, IntegerArray topKeys, MemoryAccessor topValues)
        {            
            Guard.NotNull(topValues, nameof(topValues));            
            Guard.IsTrue(topKeys.Count <= bottomCount, nameof(topKeys));
            Guard.IsTrue(topKeys.Count == topValues._Slicer.ItemsCount, nameof(topValues));
            Guard.IsTrue(topKeys.All(item => item < (uint)bottomCount), nameof(topKeys));

            return new SparseArray<Vector3>(new ZeroAccessorArray<Vector3>(bottomCount), topValues.AsVector3Array(), topKeys);
        }

        public static IAccessorArray<Vector3> CreateVector3SparseArray(MemoryAccessor bottom, IntegerArray topKeys, MemoryAccessor topValues)
        {
            Guard.NotNull(bottom, nameof(bottom));
            Guard.NotNull(topValues, nameof(topValues));
            Guard.IsTrue(bottom._Slicer.Dimensions == topValues._Slicer.Dimensions, nameof(topValues));
            Guard.IsTrue(topKeys.Count <= bottom._Slicer.ItemsCount, nameof(topKeys));
            Guard.IsTrue(topKeys.Count == topValues._Slicer.ItemsCount, nameof(topValues));
            Guard.IsTrue(topKeys.All(item => item < (uint)bottom._Slicer.ItemsCount), nameof(topKeys));

            return new SparseArray<Vector3>(bottom.AsVector3Array(), topValues.AsVector3Array(), topKeys);
        }

        public static IAccessorArray<Vector4> CreateVector4SparseArray(int bottomCount, IntegerArray topKeys, MemoryAccessor topValues)
        {            
            Guard.NotNull(topValues, nameof(topValues));            
            Guard.IsTrue(topKeys.Count <= bottomCount, nameof(topKeys));
            Guard.IsTrue(topKeys.Count == topValues._Slicer.ItemsCount, nameof(topValues));
            Guard.IsTrue(topKeys.All(item => item < (uint)bottomCount), nameof(topKeys));

            return new SparseArray<Vector4>(new ZeroAccessorArray<Vector4>(bottomCount), topValues.AsVector4Array(), topKeys);
        }

        public static IAccessorArray<Vector4> CreateVector4SparseArray(MemoryAccessor bottom, IntegerArray topKeys, MemoryAccessor topValues)
        {
            Guard.NotNull(bottom, nameof(bottom));
            Guard.NotNull(topValues, nameof(topValues));
            Guard.IsTrue(bottom._Slicer.Dimensions == topValues._Slicer.Dimensions, nameof(topValues));
            Guard.IsTrue(topKeys.Count <= bottom._Slicer.ItemsCount, nameof(topKeys));
            Guard.IsTrue(topKeys.Count == topValues._Slicer.ItemsCount, nameof(topValues));
            Guard.IsTrue(topKeys.All(item => item < (uint)bottom._Slicer.ItemsCount), nameof(topKeys));

            return new SparseArray<Vector4>(bottom.AsVector4Array(), topValues.AsVector4Array(), topKeys);
        }

        public static IAccessorArray<Vector4> CreateColorSparseArray(int bottomCount, IntegerArray topKeys, MemoryAccessor topValues, Single defaultW = 1)
        {            
            Guard.NotNull(topValues, nameof(topValues));            
            Guard.IsTrue(topKeys.Count <= bottomCount, nameof(topKeys));
            Guard.IsTrue(topKeys.Count == topValues._Slicer.ItemsCount, nameof(topValues));
            Guard.IsTrue(topKeys.All(item => item < (uint)bottomCount), nameof(topKeys));

            return new SparseArray<Vector4>(new ZeroAccessorArray<Vector4>(bottomCount), topValues.AsColorArray(defaultW), topKeys);
        }

        public static IAccessorArray<Vector4> CreateColorSparseArray(MemoryAccessor bottom, IntegerArray topKeys, MemoryAccessor topValues, Single defaultW = 1)
        {
            Guard.NotNull(bottom, nameof(bottom));
            Guard.NotNull(topValues, nameof(topValues));
            Guard.IsTrue(bottom._Slicer.Dimensions == topValues._Slicer.Dimensions, nameof(topValues));
            Guard.IsTrue(topKeys.Count <= bottom._Slicer.ItemsCount, nameof(topKeys));
            Guard.IsTrue(topKeys.Count == topValues._Slicer.ItemsCount, nameof(topValues));
            Guard.IsTrue(topKeys.All(item => item < (uint)bottom._Slicer.ItemsCount), nameof(topKeys));

            return new SparseArray<Vector4>(bottom.AsColorArray(defaultW), topValues.AsColorArray(defaultW), topKeys);
        }

        #endregion

        #region data

        [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)]
        private MemoryAccessInfo _Slicer;

        [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)]
        private BYTES _Data;

        #endregion

        #region properties

        public MemoryAccessInfo Attribute => _Slicer;

        public BYTES Data => _Data;

        #endregion

        #region API

        public void Update(BYTES data, MemoryAccessInfo encoding)
        {
            this._Slicer = encoding;
            this._Data = data;
        }

        public IAccessorArray<T> AsArrayOf<T>()
        {
            if (typeof(T) == typeof(int)) return AsIntegerArray() as IAccessorArray<T>;
            if (typeof(T) == typeof(float)) return AsScalarArray() as IAccessorArray<T>;
            if (typeof(T) == typeof(Vector2)) return AsVector2Array() as IAccessorArray<T>;
            if (typeof(T) == typeof(Vector3)) return AsVector3Array() as IAccessorArray<T>;

            // AsColorArray is able to handle both Vector3 and Vector4 underlaying data
            if (typeof(T) == typeof(Vector4)) return AsColorArray() as IAccessorArray<T>;
            
            if (typeof(T) == typeof(Quaternion)) return AsQuaternionArray() as IAccessorArray<T>;

            // we should create the equivalent of AsColorArray for Matrices
            if (typeof(T) == typeof(Matrix4x4)) return AsMatrix4x4Array() as IAccessorArray<T>;

            throw new NotSupportedException(typeof(T).Name);
        }

        public IntegerArray AsIntegerArray()
        {
            Guard.IsTrue(_Slicer.IsValidIndexer, nameof(_Slicer));
            Guard.IsTrue(_Slicer.Dimensions == DIMENSIONS.SCALAR, nameof(_Slicer));
            return new IntegerArray(_Data, _Slicer.ByteOffset, _Slicer.ItemsCount, _Slicer.Encoding.ToIndex());
        }

        public ScalarArray AsScalarArray()
        {
            Guard.IsTrue(_Slicer.IsValidVertexAttribute, nameof(_Slicer));
            Guard.IsTrue(_Slicer.Dimensions == DIMENSIONS.SCALAR, nameof(_Slicer));
            return new ScalarArray(_Data, _Slicer.ByteOffset, _Slicer.ItemsCount, _Slicer.ByteStride, _Slicer.Encoding, _Slicer.Normalized);
        }

        public Vector2Array AsVector2Array()
        {
            Guard.IsTrue(_Slicer.IsValidVertexAttribute, nameof(_Slicer));
            Guard.IsTrue(_Slicer.Dimensions == DIMENSIONS.VEC2, nameof(_Slicer));
            return new Vector2Array(_Data, _Slicer.ByteOffset, _Slicer.ItemsCount, _Slicer.ByteStride, _Slicer.Encoding, _Slicer.Normalized);
        }

        public Vector3Array AsVector3Array()
        {
            Guard.IsTrue(_Slicer.IsValidVertexAttribute, nameof(_Slicer));
            Guard.IsTrue(_Slicer.Dimensions == DIMENSIONS.VEC3, nameof(_Slicer));
            return new Vector3Array(_Data, _Slicer.ByteOffset, _Slicer.ItemsCount, _Slicer.ByteStride, _Slicer.Encoding, _Slicer.Normalized);
        }

        public Vector4Array AsVector4Array()
        {
            Guard.IsTrue(_Slicer.IsValidVertexAttribute, nameof(_Slicer));
            Guard.IsTrue(_Slicer.Dimensions == DIMENSIONS.VEC4, nameof(_Slicer));
            return new Vector4Array(_Data, _Slicer.ByteOffset, _Slicer.ItemsCount, _Slicer.ByteStride, _Slicer.Encoding, _Slicer.Normalized);
        }

        public ColorArray AsColorArray(Single defaultW = 1)
        {
            Guard.IsTrue(_Slicer.IsValidVertexAttribute, nameof(_Slicer));
            Guard.IsTrue(_Slicer.Dimensions == DIMENSIONS.VEC3 || _Slicer.Dimensions == DIMENSIONS.VEC4, nameof(_Slicer));
            return new ColorArray(_Data, _Slicer.ByteOffset, _Slicer.ItemsCount, _Slicer.ByteStride, _Slicer.Dimensions.DimCount(), _Slicer.Encoding, _Slicer.Normalized, defaultW);
        }

        public QuaternionArray AsQuaternionArray()
        {
            Guard.IsTrue(_Slicer.IsValidVertexAttribute, nameof(_Slicer));
            Guard.IsTrue(_Slicer.Dimensions == DIMENSIONS.VEC4, nameof(_Slicer));
            return new QuaternionArray(_Data, _Slicer.ByteOffset, _Slicer.ItemsCount, _Slicer.ByteStride, _Slicer.Encoding, _Slicer.Normalized);
        }

        public Matrix2x2Array AsMatrix2x2Array()
        {
            Guard.IsTrue(_Slicer.IsValidVertexAttribute, nameof(_Slicer));
            Guard.IsTrue(_Slicer.Dimensions == DIMENSIONS.MAT2, nameof(_Slicer));
            return new Matrix2x2Array(_Data, _Slicer.ByteOffset, _Slicer.ItemsCount, _Slicer.ByteStride, _Slicer.Encoding, _Slicer.Normalized);
        }

        public Matrix3x3Array AsMatrix3x3Array()
        {
            Guard.IsTrue(_Slicer.IsValidVertexAttribute, nameof(_Slicer));
            Guard.IsTrue(_Slicer.Dimensions == DIMENSIONS.MAT3, nameof(_Slicer));
            return new Matrix3x3Array(_Data, _Slicer.ByteOffset, _Slicer.ItemsCount, _Slicer.ByteStride, _Slicer.Encoding, _Slicer.Normalized);
        }

        public Matrix4x3Array AsMatrix4x3Array()
        {
            Guard.IsTrue(_Slicer.IsValidVertexAttribute, nameof(_Slicer));
            // Guard.IsTrue(_Slicer.Dimensions == DIMENSIONS.MAT3, nameof(_Slicer));

            return new Matrix4x3Array(_Data, _Slicer.ByteOffset, _Slicer.ItemsCount, _Slicer.ByteStride, _Slicer.Encoding, _Slicer.Normalized);
        }

        public Matrix4x4Array AsMatrix4x4Array()
        {
            Guard.IsTrue(_Slicer.IsValidVertexAttribute, nameof(_Slicer));
            Guard.IsTrue(_Slicer.Dimensions == DIMENSIONS.MAT4, nameof(_Slicer));
            return new Matrix4x4Array(_Data, _Slicer.ByteOffset, _Slicer.ItemsCount, _Slicer.ByteStride, _Slicer.Encoding, _Slicer.Normalized);
        }

        public MultiArray AsMultiArray(int dimensions)
        {
            Guard.IsTrue(_Slicer.IsValidVertexAttribute, nameof(_Slicer));
            Guard.IsTrue(_Slicer.Dimensions == DIMENSIONS.SCALAR, nameof(_Slicer));
            Guard.IsTrue(_Slicer.ByteStride == 0, nameof(_Slicer));
            return new MultiArray(_Data, _Slicer.ByteOffset, _Slicer.ItemsCount, _Slicer.ByteStride, dimensions, _Slicer.Encoding, _Slicer.Normalized);
        }

        public IEnumerable<BYTES> GetItemsAsRawBytes()
        {
            var rowOffset = this._Slicer.ByteOffset;
            var rowStride = this._Slicer.StepByteLength;
            var itemSize = this._Slicer.Dimensions.DimCount() * _Slicer.Encoding.ByteLength();

            for (int i = 0; i < this._Slicer.ItemsCount; ++i)
            {
                yield return this._Data.Slice((i * rowStride) + rowOffset, itemSize);
            }
        }

        internal BYTES _GetBytes()
        {
            var o = this._Slicer.ByteOffset;
            var l = this._Slicer.StepByteLength * this._Slicer.ItemsCount;

            var data = _Data.Slice(o);

            data = data.Slice(0, Math.Min(data.Count, l));

            return data;
        }

        #endregion
    }
}
