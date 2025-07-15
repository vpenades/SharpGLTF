using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

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
            where T: unmanaged
        {
            if (typeof(T) == typeof(UInt32)) return AsIntegerArray() as IAccessorArray<T>;
            if (typeof(T) == typeof(Single)) return AsScalarArray() as IAccessorArray<T>;
            if (typeof(T) == typeof(Vector2)) return AsVector2Array() as IAccessorArray<T>;
            if (typeof(T) == typeof(Vector3)) return AsVector3Array() as IAccessorArray<T>;
            
            if (typeof(T) == typeof(Vector4))
            {
                if (this.Attribute.Dimensions == DIMENSIONS.VEC4) return AsVector4Array() as IAccessorArray<T>;
                if (this.Attribute.Dimensions == DIMENSIONS.VEC3) return AsColorArray() as IAccessorArray<T>;
            }
            
            if (typeof(T) == typeof(Quaternion)) return AsQuaternionArray() as IAccessorArray<T>;

            // TODO: we should create the equivalent of AsColorArray for Matrices

            if (typeof(T) == typeof(Matrix3x2))
            {
                return AsMatrix2x2Array() as IAccessorArray<T>;                
            }

            if (typeof(T) == typeof(Matrix4x4))
            {                
                if (this.Attribute.Dimensions == DIMENSIONS.MAT3) return AsMatrix3x3Array() as IAccessorArray<T>;
                if (this.Attribute.Dimensions == DIMENSIONS.MAT4) return AsMatrix4x4Array() as IAccessorArray<T>;
            }

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


        /// <summary>
        /// Gets an array of "colors"
        /// </summary>
        /// <remarks>
        /// This can be used either on <see cref="Vector3"/> and <see cref="Vector4"/> input data.
        /// </remarks>
        /// <param name="defaultW">default value for the W component if missing.</param>
        /// <returns>An array of colors</returns>
        public ColorArray AsColorArray(Single defaultW = 1)
        {
            Guard.IsTrue(_Slicer.IsValidVertexAttribute, nameof(_Slicer));
            Guard.IsTrue(_Slicer.Dimensions == DIMENSIONS.VEC3 || _Slicer.Dimensions == DIMENSIONS.VEC4, nameof(_Slicer));
            return new ColorArray(_Data, _Slicer.ByteOffset, _Slicer.ItemsCount, _Slicer.ByteStride, _Slicer.Dimensions.DimCount(), _Slicer.Encoding, _Slicer.Normalized, defaultW);
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

        #endregion

        #region sparse API

        public (MemoryAccessor indices, MemoryAccessor values) ConvertToSparse()
        {
            var indices = new List<uint>();
            var values = new List<BYTES>();

            uint index = 0;

            foreach (var item in GetItemsAsRawBytes())
            {
                if (!RepresentsZeroValue(item))
                {
                    indices.Add(index);
                    values.Add(item);
                }

                ++index;
            }

            var indicesBuffer = new byte[indices.Count * 4];
            for (int i = 0; i < indices.Count; ++i)
            {
                System.Buffers.Binary.BinaryPrimitives.WriteUInt32LittleEndian(indicesBuffer.Slice(i * 4), indices[i]);
            }

            var blen = Attribute.ItemByteLength;

            var vertexBuffer = new byte[values.Count * blen];
            for (int i = 0; i < values.Count; ++i)
            {
                var src = values[i];
                var dst = new BYTES(vertexBuffer, i * blen, blen);

                src.AsSpan().CopyTo(dst);
            }

            var indicesMem = new MemoryAccessor(indicesBuffer, new MemoryAccessInfo("SparseIndices", 0, values.Count, 0, ENCODING.UNSIGNED_INT));
            var valuesMem = new MemoryAccessor(vertexBuffer, new MemoryAccessInfo("SparseValues", 0, values.Count, 0, this.Attribute.Format));

            return (indicesMem, valuesMem);
        }

        private bool RepresentsZeroValue(BYTES bytes)
        {
            // we handle floats separately to support negative zero.
            if (this.Attribute.Encoding == ENCODING.FLOAT)
            {
                var floats = System.Runtime.InteropServices.MemoryMarshal.Cast<byte, float>(bytes);
                foreach (var f in floats)
                {
                    if (f != 0) return false;
                }
                return true;
            }

            return bytes.All(b => b == 0);
        }

        public static IAccessorArray<T> CreateSparseArray<T>(MemoryAccessor denseValues, IntegerArray sparseKeys, MemoryAccessor sparseValues)
            where T : unmanaged
        {
            return _CreateSparseArray(denseValues, sparseKeys, sparseValues, m => m.AsArrayOf<T>());
        }

        public static IAccessorArray<T> CreateSparseArray<T>(int denseCount, IntegerArray sparseKeys, MemoryAccessor sparseValues)
            where T : unmanaged
        {
            return _CreateSparseArray(denseCount, sparseKeys, sparseValues, m => m.AsArrayOf<T>());
        }

        public static IAccessorArray<Vector4> CreateColorSparseArray(int denseCount, IntegerArray sparseKeys, MemoryAccessor sparseValues, Single defaultW = 1)
        {
            return _CreateSparseArray(denseCount, sparseKeys, sparseValues, m => m.AsColorArray(defaultW));
        }

        public static IAccessorArray<Vector4> CreateColorSparseArray(MemoryAccessor denseValues, IntegerArray sparseKeys, MemoryAccessor sparseValues, Single defaultW = 1)
        {
            return _CreateSparseArray(denseValues, sparseKeys, sparseValues, m => m.AsColorArray(defaultW));
        }

        private static IAccessorArray<T> _CreateSparseArray<T>(int denseCount, IntegerArray sparseKeys, MemoryAccessor sparseValues, Func<MemoryAccessor, IAccessorArray<T>> toAccessor)
            where T : unmanaged
        {
            Guard.NotNull(sparseValues, nameof(sparseValues));
            Guard.IsTrue(sparseKeys.Count <= denseCount, nameof(sparseKeys));
            System.Diagnostics.Debug.Assert(sparseKeys.All(item => item < (uint)denseCount), nameof(sparseKeys), "index keys exceed bottomCount");

            var typedSparseValues = toAccessor(sparseValues);
            Guard.IsTrue(sparseKeys.Count == typedSparseValues.Count, nameof(sparseValues));

            return new SparseArray<T>(new ZeroAccessorArray<T>(denseCount), typedSparseValues, sparseKeys);
        }

        private static IAccessorArray<T> _CreateSparseArray<T>(MemoryAccessor denseValues, IntegerArray sparseKeys, MemoryAccessor sparseValues, Func<MemoryAccessor, IAccessorArray<T>> toAccessor)
            where T : unmanaged
        {
            Guard.NotNull(denseValues, nameof(denseValues));
            Guard.NotNull(sparseValues, nameof(sparseValues));

            var typedDenseValues = toAccessor(denseValues);
            var typedSparseValues = toAccessor(sparseValues);

            Guard.IsTrue(sparseKeys.Count <= typedDenseValues.Count, nameof(sparseKeys));
            Guard.IsTrue(sparseKeys.Count == typedSparseValues.Count, nameof(sparseValues));
            System.Diagnostics.Debug.Assert(sparseKeys.All(item => item < (uint)typedDenseValues.Count), nameof(sparseKeys), "index keys exceed bottomCount");

            return new SparseArray<T>(typedDenseValues, typedSparseValues, sparseKeys);
        }

        #endregion
    }
}
