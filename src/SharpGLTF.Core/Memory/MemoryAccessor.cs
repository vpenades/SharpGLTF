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
            return Attribute._GetDebuggerDisplay();
        }

        #endregion        

        #region constructor

        #if NETSTANDARD
        public MemoryAccessor(Byte[] data, MemoryAccessInfo info)
        {
            this.Attribute = info;
            this.Data = new ArraySegment<Byte>(data);
        }
        #endif

        public MemoryAccessor(BYTES data, MemoryAccessInfo info)
        {
            this.Attribute = info;
            this.Data = data;
        }

        public MemoryAccessor(MemoryAccessInfo info)
        {
            this.Attribute = info;
            this.Data = default;
        }        

        #endregion        

        #region data

        public MemoryAccessInfo Attribute { get; private set; }

        public BYTES Data { get; private set; }

        #endregion

        #region API

        public void Update(BYTES data, MemoryAccessInfo encoding)
        {
            this.Attribute = encoding;
            this.Data = data;
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
            Guard.IsTrue(Attribute.IsValidIndexer, nameof(Attribute));
            Guard.IsTrue(Attribute.Dimensions == DIMENSIONS.SCALAR, nameof(Attribute));
            return new IntegerArray(Data, Attribute.ByteOffset, Attribute.ItemsCount, Attribute.Encoding.ToIndex());
        }

        public ScalarArray AsScalarArray()
        {
            Guard.IsTrue(Attribute.IsValidVertexAttribute, nameof(Attribute));
            Guard.IsTrue(Attribute.Dimensions == DIMENSIONS.SCALAR, nameof(Attribute));
            return new ScalarArray(Data, Attribute.ByteOffset, Attribute.ItemsCount, Attribute.ByteStride, Attribute.Encoding, Attribute.Normalized);
        }

        public Vector2Array AsVector2Array()
        {
            Guard.IsTrue(Attribute.IsValidVertexAttribute, nameof(Attribute));
            Guard.IsTrue(Attribute.Dimensions == DIMENSIONS.VEC2, nameof(Attribute));
            return new Vector2Array(Data, Attribute.ByteOffset, Attribute.ItemsCount, Attribute.ByteStride, Attribute.Encoding, Attribute.Normalized);
        }

        public Vector3Array AsVector3Array()
        {
            Guard.IsTrue(Attribute.IsValidVertexAttribute, nameof(Attribute));
            Guard.IsTrue(Attribute.Dimensions == DIMENSIONS.VEC3, nameof(Attribute));
            return new Vector3Array(Data, Attribute.ByteOffset, Attribute.ItemsCount, Attribute.ByteStride, Attribute.Encoding, Attribute.Normalized);
        }

        public Vector4Array AsVector4Array()
        {
            Guard.IsTrue(Attribute.IsValidVertexAttribute, nameof(Attribute));
            Guard.IsTrue(Attribute.Dimensions == DIMENSIONS.VEC4, nameof(Attribute));
            return new Vector4Array(Data, Attribute.ByteOffset, Attribute.ItemsCount, Attribute.ByteStride, Attribute.Encoding, Attribute.Normalized);
        }        

        public QuaternionArray AsQuaternionArray()
        {
            Guard.IsTrue(Attribute.IsValidVertexAttribute, nameof(Attribute));
            Guard.IsTrue(Attribute.Dimensions == DIMENSIONS.VEC4, nameof(Attribute));
            return new QuaternionArray(Data, Attribute.ByteOffset, Attribute.ItemsCount, Attribute.ByteStride, Attribute.Encoding, Attribute.Normalized);
        }

        public Matrix2x2Array AsMatrix2x2Array()
        {
            Guard.IsTrue(Attribute.IsValidVertexAttribute, nameof(Attribute));
            Guard.IsTrue(Attribute.Dimensions == DIMENSIONS.MAT2, nameof(Attribute));
            return new Matrix2x2Array(Data, Attribute.ByteOffset, Attribute.ItemsCount, Attribute.ByteStride, Attribute.Encoding, Attribute.Normalized);
        }

        public Matrix3x3Array AsMatrix3x3Array()
        {
            Guard.IsTrue(Attribute.IsValidVertexAttribute, nameof(Attribute));
            Guard.IsTrue(Attribute.Dimensions == DIMENSIONS.MAT3, nameof(Attribute));
            return new Matrix3x3Array(Data, Attribute.ByteOffset, Attribute.ItemsCount, Attribute.ByteStride, Attribute.Encoding, Attribute.Normalized);
        }

        public Matrix4x3Array AsMatrix4x3Array()
        {
            Guard.IsTrue(Attribute.IsValidVertexAttribute, nameof(Attribute));
            // Guard.IsTrue(_Slicer.Dimensions == DIMENSIONS.MAT3, nameof(_Slicer));

            return new Matrix4x3Array(Data, Attribute.ByteOffset, Attribute.ItemsCount, Attribute.ByteStride, Attribute.Encoding, Attribute.Normalized);
        }

        public Matrix4x4Array AsMatrix4x4Array()
        {
            Guard.IsTrue(Attribute.IsValidVertexAttribute, nameof(Attribute));
            Guard.IsTrue(Attribute.Dimensions == DIMENSIONS.MAT4, nameof(Attribute));
            return new Matrix4x4Array(Data, Attribute.ByteOffset, Attribute.ItemsCount, Attribute.ByteStride, Attribute.Encoding, Attribute.Normalized);
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
            Guard.IsTrue(Attribute.IsValidVertexAttribute, nameof(Attribute));
            Guard.IsTrue(Attribute.Dimensions == DIMENSIONS.VEC3 || Attribute.Dimensions == DIMENSIONS.VEC4, nameof(Attribute));
            return new ColorArray(Data, Attribute.ByteOffset, Attribute.ItemsCount, Attribute.ByteStride, Attribute.Dimensions.DimCount(), Attribute.Encoding, Attribute.Normalized, defaultW);
        }

        public MultiArray AsMultiArray(int dimensions)
        {
            Guard.IsTrue(Attribute.IsValidVertexAttribute, nameof(Attribute));
            Guard.IsTrue(Attribute.Dimensions == DIMENSIONS.SCALAR, nameof(Attribute));
            Guard.IsTrue(Attribute.ByteStride == 0, nameof(Attribute));
            return new MultiArray(Data, Attribute.ByteOffset, Attribute.ItemsCount, Attribute.ByteStride, dimensions, Attribute.Encoding, Attribute.Normalized);
        }

        public IEnumerable<BYTES> GetItemsAsRawBytes()
        {
            var itemSize = this.Attribute.ByteLength;
            var rowStride = this.Attribute.StepByteLength;
            var rowOffset = this.Attribute.ByteOffset;            

            for (int i = 0; i < this.Attribute.ItemsCount; ++i)
            {
                yield return this.Data.Slice(i * rowStride + rowOffset, itemSize);
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

            var blen = Attribute.ByteLength;

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
