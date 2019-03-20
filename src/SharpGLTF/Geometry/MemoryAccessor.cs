using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;

namespace SharpGLTF.Geometry
{
    using Memory;

    using DIMENSIONS = Schema2.DimensionType;
    using ENCODING = Schema2.EncodingType;

    /// <summary>
    /// Defines the pattern in which a <see cref="ArraySegment{Byte}"/> is accessed and decoded to meaningful values.
    /// </summary>
    [System.Diagnostics.DebuggerDisplay("{Name} {Dimensions}.{Encoding}.{Normalized} {ByteStride}   {ByteOffset} [{ItemsCount}]")]
    public struct MemoryAccessInfo
    {
        #region constructor

        public static MemoryAccessInfo[] Create(params string[] attributes)
        {
            return attributes.Select(item => CreateDefaultElement(item)).ToArray();
        }

        public static MemoryAccessInfo CreateDefaultElement(string attribute)
        {
            switch (attribute)
            {
                case "INDEX": return new MemoryAccessInfo("INDEX", 0, 0, 0, DIMENSIONS.SCALAR, ENCODING.UNSIGNED_INT, false);

                case "POSITION": return new MemoryAccessInfo("POSITION", 0, 0, 0, DIMENSIONS.VEC3);
                case "NORMAL": return new MemoryAccessInfo("NORMAL", 0, 0, 0, DIMENSIONS.VEC3);
                case "TANGENT": return new MemoryAccessInfo("TANGENT", 0, 0, 0, DIMENSIONS.VEC4);

                case "TEXCOORD_0": return new MemoryAccessInfo("TEXCOORD_0", 0, 0, 0, DIMENSIONS.VEC2);
                case "TEXCOORD_1": return new MemoryAccessInfo("TEXCOORD_1", 0, 0, 0, DIMENSIONS.VEC2);
                case "TEXCOORD_2": return new MemoryAccessInfo("TEXCOORD_2", 0, 0, 0, DIMENSIONS.VEC2);
                case "TEXCOORD_3": return new MemoryAccessInfo("TEXCOORD_3", 0, 0, 0, DIMENSIONS.VEC2);

                case "COLOR_0": return new MemoryAccessInfo("COLOR_0", 0, 0, 0, DIMENSIONS.VEC4, ENCODING.UNSIGNED_BYTE, true);

                case "JOINTS_0": return new MemoryAccessInfo("JOINTS_0", 0, 0, 0, DIMENSIONS.VEC4, ENCODING.UNSIGNED_BYTE);
                case "WEIGHTS_0": return new MemoryAccessInfo("WEIGHTS_0", 0, 0, 0, DIMENSIONS.VEC4, ENCODING.UNSIGNED_BYTE, true);
            }

            throw new NotImplementedException();
        }

        public MemoryAccessInfo(string name, int byteOffset, int itemsCount, int byteStride, DIMENSIONS dimensions, ENCODING encoding = ENCODING.FLOAT, Boolean normalized = false)
        {
            this.Name = name;
            this.ByteOffset = byteOffset;
            this.ItemsCount = itemsCount;
            this.ByteStride = byteStride;
            this.Dimensions = dimensions;
            this.Encoding = encoding;
            this.Normalized = normalized;
        }

        public MemoryAccessInfo Slice(int start, int count)
        {
            var stride = Math.Max(this.ByteStride, this.Dimensions.DimCount() * this.Encoding.ByteLength());

            var clone = this;
            clone.ByteOffset += start * stride;
            clone.ItemsCount = Math.Min(clone.ItemsCount, count);

            return clone;
        }

        #endregion

        #region data

        public String Name;
        public int ByteOffset;
        public int ItemsCount;
        public int ByteStride;
        public DIMENSIONS Dimensions;
        public ENCODING Encoding;
        public Boolean Normalized;

        #endregion

        #region API

        public int ByteLength => this.Dimensions.DimCount() * this.Encoding.ByteLength();

        public Boolean IsValidVertexAttribute
        {
            get
            {
                if (this.ByteOffset < 0) return false;
                if (this.ItemsCount < 0) return false;
                if (this.ByteStride < 0) return false;
                var blen = this.ByteLength;
                if (blen == 0 || (blen & 3) != 0) return false;

                if (this.ByteStride > 0 && this.ByteStride < blen) return false;
                if ((this.ByteStride & 3) != 0) return false;

                return true;
            }
        }

        public Boolean IsValidIndexer
        {
            get
            {
                if (this.ByteOffset < 0) return false;
                if (this.ItemsCount < 0) return false;
                if (this.ByteStride < 0) return false;
                if (this.Dimensions != DIMENSIONS.SCALAR) return false;
                if (this.Normalized) return false;
                if (this.ByteStride == 0) return true;
                if (this.ByteStride == 1) return true;
                if (this.ByteStride == 2) return true;
                if (this.ByteStride == 4) return true;
                return false;
            }
        }

        public static int SetInterleavedInfo(MemoryAccessInfo[] attributes, int byteOffset, int itemsCount)
        {
            var byteStride = 0;

            for (int i = 0; i < attributes.Length; ++i)
            {
                var a = attributes[i];

                a.ByteOffset = byteOffset;
                a.ItemsCount = itemsCount;

                var attributeStride = a.ByteLength;

                byteStride += attributeStride;
                byteOffset += attributeStride;

                attributes[i] = a;
            }

            for (int i = 0; i < attributes.Length; ++i)
            {
                var a = attributes[i];
                a.ByteStride = byteStride;
                attributes[i] = a;
            }

            return byteStride;
        }

        public static MemoryAccessInfo[] Slice(MemoryAccessInfo[] attributes, int start, int count)
        {
            var dst = new MemoryAccessInfo[attributes.Length];

            for (int i = 0; i < dst.Length; ++i)
            {
                dst[i] = attributes[i].Slice(start, count);
            }

            return dst;
        }

        #endregion
    }

    /// <summary>
    /// Wraps a <see cref="ArraySegment{Byte}"/> decoding it and exposing its content as arrays of different types.
    /// </summary>
    public sealed class MemoryAccessor
    {
        #region constructor

        public MemoryAccessor(ArraySegment<Byte> data, MemoryAccessInfo info)
        {
            this._Attribute = info;
            this._Data = data;
        }

        public MemoryAccessor(MemoryAccessInfo info)
        {
            this._Attribute = info;
            this._Data = default;
        }

        #endregion

        #region data

        private MemoryAccessInfo _Attribute;
        private ArraySegment<Byte> _Data;

        #endregion

        #region properties

        public MemoryAccessInfo Attribute => _Attribute;

        public ArraySegment<Byte> Data => _Data;

        #endregion

        #region API

        internal void SetName(string name)
        {
            _Attribute.Name = name;
        }

        public void SetIndexDataSource(ArraySegment<Byte> data, int byteOffset, int itemsCount)
        {
            Guard.IsTrue(_Attribute.IsValidIndexer, nameof(_Attribute));
            _Data = data;
            _Attribute.ByteOffset = byteOffset;
            _Attribute.ItemsCount = itemsCount;
            _Attribute.ByteStride = 0;
        }

        public void SetVertexDataSource(ArraySegment<Byte> data, int byteOffset, int itemsCount, int byteStride)
        {
            Guard.IsTrue(_Attribute.IsValidVertexAttribute, nameof(_Attribute));
            _Data = data;
            _Attribute.ByteOffset = byteOffset;
            _Attribute.ItemsCount = itemsCount;
            _Attribute.ByteStride = byteStride;
        }

        public IntegerArray AsIntegerArray()
        {
            Guard.IsTrue(_Attribute.IsValidIndexer, nameof(_Attribute));
            Guard.IsTrue(_Attribute.Dimensions == DIMENSIONS.SCALAR, nameof(_Attribute));
            return new IntegerArray(_Data, _Attribute.ByteOffset, _Attribute.ItemsCount, _Attribute.Encoding.ToIndex());
        }

        public ScalarArray AsScalarArray()
        {
            Guard.IsTrue(_Attribute.IsValidVertexAttribute, nameof(_Attribute));
            Guard.IsTrue(_Attribute.Dimensions == DIMENSIONS.SCALAR, nameof(_Attribute));
            return new ScalarArray(_Data, _Attribute.ByteOffset, _Attribute.ItemsCount, _Attribute.ByteStride, _Attribute.Encoding, _Attribute.Normalized);
        }

        public Vector2Array AsVector2Array()
        {
            Guard.IsTrue(_Attribute.IsValidVertexAttribute, nameof(_Attribute));
            Guard.IsTrue(_Attribute.Dimensions == DIMENSIONS.VEC2, nameof(_Attribute));
            return new Vector2Array(_Data, _Attribute.ByteOffset, _Attribute.ItemsCount, _Attribute.ByteStride, _Attribute.Encoding, _Attribute.Normalized);
        }

        public Vector3Array AsVector3Array()
        {
            Guard.IsTrue(_Attribute.IsValidVertexAttribute, nameof(_Attribute));
            Guard.IsTrue(_Attribute.Dimensions == DIMENSIONS.VEC3, nameof(_Attribute));
            return new Vector3Array(_Data, _Attribute.ByteOffset, _Attribute.ItemsCount, _Attribute.ByteStride, _Attribute.Encoding, _Attribute.Normalized);
        }

        public Vector4Array AsVector4Array()
        {
            Guard.IsTrue(_Attribute.IsValidVertexAttribute, nameof(_Attribute));
            Guard.IsTrue(_Attribute.Dimensions == DIMENSIONS.VEC4, nameof(_Attribute));
            return new Vector4Array(_Data, _Attribute.ByteOffset, _Attribute.ItemsCount, _Attribute.ByteStride, _Attribute.Encoding, _Attribute.Normalized);
        }

        public QuaternionArray AsQuaternionArray()
        {
            Guard.IsTrue(_Attribute.IsValidVertexAttribute, nameof(_Attribute));
            Guard.IsTrue(_Attribute.Dimensions == DIMENSIONS.VEC4, nameof(_Attribute));
            return new QuaternionArray(_Data, _Attribute.ByteOffset, _Attribute.ItemsCount, _Attribute.ByteStride, _Attribute.Encoding, _Attribute.Normalized);
        }

        public Matrix4x4Array AsMatrix4x4Array()
        {
            Guard.IsTrue(_Attribute.IsValidVertexAttribute, nameof(_Attribute));
            Guard.IsTrue(_Attribute.Dimensions == DIMENSIONS.MAT4, nameof(_Attribute));
            return new Matrix4x4Array(_Data, _Attribute.ByteOffset, _Attribute.ItemsCount, _Attribute.ByteStride, _Attribute.Encoding, _Attribute.Normalized);
        }

        public static IEncodedArray<Single> CreateScalarSparseArray(MemoryAccessor bottom, IntegerArray topKeys, MemoryAccessor topValues)
        {
            Guard.IsTrue(bottom._Attribute.Dimensions == topValues._Attribute.Dimensions, nameof(topValues));
            Guard.IsTrue(topKeys.Count <= bottom._Attribute.ItemsCount, nameof(topKeys));
            Guard.IsTrue(topKeys.Count == topValues._Attribute.ItemsCount, nameof(topValues));
            Guard.IsTrue(topKeys.All(item => item < (uint)bottom._Attribute.ItemsCount), nameof(topKeys));

            return new SparseArray<Single>(bottom.AsScalarArray(), topValues.AsScalarArray(), topKeys);
        }

        public static IEncodedArray<Vector2> CreateVector2SparseArray(MemoryAccessor bottom, IntegerArray topKeys, MemoryAccessor topValues)
        {
            Guard.IsTrue(bottom._Attribute.Dimensions == topValues._Attribute.Dimensions, nameof(topValues));
            Guard.IsTrue(topKeys.Count <= bottom._Attribute.ItemsCount, nameof(topKeys));
            Guard.IsTrue(topKeys.Count == topValues._Attribute.ItemsCount, nameof(topValues));
            Guard.IsTrue(topKeys.All(item => item < (uint)bottom._Attribute.ItemsCount), nameof(topKeys));

            return new SparseArray<Vector2>(bottom.AsVector2Array(), topValues.AsVector2Array(), topKeys);
        }

        public static IEncodedArray<Vector3> CreateVector3SparseArray(MemoryAccessor bottom, IntegerArray topKeys, MemoryAccessor topValues)
        {
            Guard.IsTrue(bottom._Attribute.Dimensions == topValues._Attribute.Dimensions, nameof(topValues));
            Guard.IsTrue(topKeys.Count <= bottom._Attribute.ItemsCount, nameof(topKeys));
            Guard.IsTrue(topKeys.Count == topValues._Attribute.ItemsCount, nameof(topValues));
            Guard.IsTrue(topKeys.All(item => item < (uint)bottom._Attribute.ItemsCount), nameof(topKeys));

            return new SparseArray<Vector3>(bottom.AsVector3Array(), topValues.AsVector3Array(), topKeys);
        }

        public static IEncodedArray<Vector4> CreateVector4SparseArray(MemoryAccessor bottom, IntegerArray topKeys, MemoryAccessor topValues)
        {
            Guard.IsTrue(bottom._Attribute.Dimensions == topValues._Attribute.Dimensions, nameof(topValues));
            Guard.IsTrue(topKeys.Count <= bottom._Attribute.ItemsCount, nameof(topKeys));
            Guard.IsTrue(topKeys.Count == topValues._Attribute.ItemsCount, nameof(topValues));
            Guard.IsTrue(topKeys.All(item => item < (uint)bottom._Attribute.ItemsCount), nameof(topKeys));

            return new SparseArray<Vector4>(bottom.AsVector4Array(), topValues.AsVector4Array(), topKeys);
        }

        #endregion
    }
}
