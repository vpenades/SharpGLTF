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
    /// Defines the memory encoding pattern for an arbitrary <see cref="BYTES"/>.
    /// </summary>
    [System.Diagnostics.DebuggerDisplay("{_GetDebuggerDisplay(),nq}")]
    public struct MemoryAccessInfo
    {
        #region debug

        internal string _GetDebuggerDisplay()
        {
            return Debug.DebuggerDisplay.ToReport(this);
        }

        #endregion

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
                case "COLOR_1": return new MemoryAccessInfo("COLOR_1", 0, 0, 0, DIMENSIONS.VEC4, ENCODING.UNSIGNED_BYTE, true);
                case "COLOR_2": return new MemoryAccessInfo("COLOR_2", 0, 0, 0, DIMENSIONS.VEC4, ENCODING.UNSIGNED_BYTE, true);
                case "COLOR_3": return new MemoryAccessInfo("COLOR_3", 0, 0, 0, DIMENSIONS.VEC4, ENCODING.UNSIGNED_BYTE, true);

                case "JOINTS_0": return new MemoryAccessInfo("JOINTS_0", 0, 0, 0, DIMENSIONS.VEC4, ENCODING.UNSIGNED_BYTE);
                case "JOINTS_1": return new MemoryAccessInfo("JOINTS_1", 0, 0, 0, DIMENSIONS.VEC4, ENCODING.UNSIGNED_BYTE);

                case "WEIGHTS_0": return new MemoryAccessInfo("WEIGHTS_0", 0, 0, 0, DIMENSIONS.VEC4, ENCODING.UNSIGNED_BYTE, true);
                case "WEIGHTS_1": return new MemoryAccessInfo("WEIGHTS_1", 0, 0, 0, DIMENSIONS.VEC4, ENCODING.UNSIGNED_BYTE, true);
            }

            throw new NotImplementedException();
        }

        public MemoryAccessInfo(string name, int byteOffset, int itemsCount, int byteStride, AttributeFormat format)
        {
            this.Name = name;
            this.ByteOffset = byteOffset;
            this.ItemsCount = itemsCount;
            this.ByteStride = byteStride;
            this.Dimensions = format.Dimensions;
            this.Encoding = format.Encoding;
            this.Normalized = format.Normalized;
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

        public MemoryAccessInfo Slice(int itemStart, int itemCount)
        {
            var stride = _GetRowByteLength();

            var clone = this;
            clone.ByteOffset += itemStart * stride;
            clone.ItemsCount = Math.Min(clone.ItemsCount, itemCount);

            return clone;
        }

        #endregion

        #region data

        /// <summary>
        /// If set, it can be used to identify the data with an attribute name: POSITION, NORMAL, etc
        /// </summary>
        public String Name;

        /// <summary>
        /// number of bytes to advance to the beginning of the first item.
        /// </summary>
        public int ByteOffset;

        /// <summary>
        /// Total number of items
        /// </summary>
        public int ItemsCount;

        /// <summary>
        /// number of bytes to advance to the beginning of the next item
        /// </summary>
        public int ByteStride;

        /// <summary>
        /// number of sub-elements of each item.
        /// </summary>
        public DIMENSIONS Dimensions;

        /// <summary>
        /// byte encoding of sub-elements of each item.
        /// </summary>
        public ENCODING Encoding;

        /// <summary>
        /// normalization of sub-elements of each item.
        /// </summary>
        public Boolean Normalized;

        #endregion

        #region properties

        /// <summary>
        /// number of bytes to advance to the next item.
        /// </summary>
        public int StepByteLength => _GetRowByteLength();

        public int ItemByteLength => _GetItemByteLength();

        public Boolean IsValidVertexAttribute
        {
            get
            {
                if (this.ItemsCount < 0) return false;

                if (this.ByteOffset < 0) return false;
                if (!this.ByteOffset.IsMultipleOf(4)) return false;

                if (this.ByteStride < 0) return false;
                if (!this.ByteStride.IsMultipleOf(4)) return false;

                if (this.ByteStride > 0 && this.ByteStride < this.StepByteLength) return false;

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

        #endregion

        #region API

        private int _GetItemByteLength()
        {
            var xlen = Encoding.ByteLength();

            if (Dimensions != DIMENSIONS.SCALAR || Name != "INDEX")
            {
                xlen *= this.Dimensions.DimCount();
                xlen = xlen.WordPadded();
            }

            return xlen;
        }

        private int _GetRowByteLength()
        {
            return Math.Max(ByteStride, _GetItemByteLength());
        }

        public static int SetInterleavedInfo(MemoryAccessInfo[] attributes, int byteOffset, int itemsCount)
        {
            Guard.NotNull(attributes, nameof(attributes));

            var byteStride = 0;

            for (int i = 0; i < attributes.Length; ++i)
            {
                var a = attributes[i];

                a.ByteOffset = byteOffset;
                a.ItemsCount = itemsCount;

                var step = a.StepByteLength;

                byteStride += step;
                byteOffset += step;

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
            Guard.NotNull(attributes, nameof(attributes));

            var dst = new MemoryAccessInfo[attributes.Length];

            for (int i = 0; i < dst.Length; ++i)
            {
                dst[i] = attributes[i].Slice(start, count);
            }

            return dst;
        }

        #endregion

        #region nested types

        internal static IComparer<string> NameComparer { get; private set; } = new AttributeComparer();

        /// <summary>
        /// Comparer used to sort attribute names in a friendly order.
        /// </summary>
        private class AttributeComparer : IComparer<String>
        {
            public int Compare(string x, string y)
            {
                var xx = _GetSortingScore(x);
                var yy = _GetSortingScore(y);

                return xx.CompareTo(yy);
            }

            private static int _GetSortingScore(string attribute)
            {
                switch (attribute)
                {
                    case "POSITION": return 0;
                    case "NORMAL": return 1;
                    case "TANGENT": return 2;

                    case "COLOR_0": return 10;
                    case "COLOR_1": return 11;
                    case "COLOR_2": return 12;
                    case "COLOR_3": return 13;

                    case "TEXCOORD_0": return 20;
                    case "TEXCOORD_1": return 21;
                    case "TEXCOORD_2": return 22;
                    case "TEXCOORD_3": return 23;

                    case "JOINTS_0": return 50;
                    case "JOINTS_1": return 51;
                    case "WEIGHTS_0": return 50;
                    case "WEIGHTS_1": return 51;
                    default: return 100;
                }
            }
        }

        #endregion
    }

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

        public static IList<Single> CreateScalarSparseArray(MemoryAccessor bottom, IntegerArray topKeys, MemoryAccessor topValues)
        {
            Guard.NotNull(bottom, nameof(bottom));
            Guard.NotNull(topValues, nameof(topValues));
            Guard.IsTrue(bottom._Slicer.Dimensions == topValues._Slicer.Dimensions, nameof(topValues));
            Guard.IsTrue(topKeys.Count <= bottom._Slicer.ItemsCount, nameof(topKeys));
            Guard.IsTrue(topKeys.Count == topValues._Slicer.ItemsCount, nameof(topValues));
            Guard.IsTrue(topKeys.All(item => item < (uint)bottom._Slicer.ItemsCount), nameof(topKeys));

            return new SparseArray<Single>(bottom.AsScalarArray(), topValues.AsScalarArray(), topKeys);
        }

        public static IList<Vector2> CreateVector2SparseArray(MemoryAccessor bottom, IntegerArray topKeys, MemoryAccessor topValues)
        {
            Guard.NotNull(bottom, nameof(bottom));
            Guard.NotNull(topValues, nameof(topValues));
            Guard.IsTrue(bottom._Slicer.Dimensions == topValues._Slicer.Dimensions, nameof(topValues));
            Guard.IsTrue(topKeys.Count <= bottom._Slicer.ItemsCount, nameof(topKeys));
            Guard.IsTrue(topKeys.Count == topValues._Slicer.ItemsCount, nameof(topValues));
            Guard.IsTrue(topKeys.All(item => item < (uint)bottom._Slicer.ItemsCount), nameof(topKeys));

            return new SparseArray<Vector2>(bottom.AsVector2Array(), topValues.AsVector2Array(), topKeys);
        }

        public static IList<Vector3> CreateVector3SparseArray(MemoryAccessor bottom, IntegerArray topKeys, MemoryAccessor topValues)
        {
            Guard.NotNull(bottom, nameof(bottom));
            Guard.NotNull(topValues, nameof(topValues));
            Guard.IsTrue(bottom._Slicer.Dimensions == topValues._Slicer.Dimensions, nameof(topValues));
            Guard.IsTrue(topKeys.Count <= bottom._Slicer.ItemsCount, nameof(topKeys));
            Guard.IsTrue(topKeys.Count == topValues._Slicer.ItemsCount, nameof(topValues));
            Guard.IsTrue(topKeys.All(item => item < (uint)bottom._Slicer.ItemsCount), nameof(topKeys));

            return new SparseArray<Vector3>(bottom.AsVector3Array(), topValues.AsVector3Array(), topKeys);
        }

        public static IList<Vector4> CreateVector4SparseArray(MemoryAccessor bottom, IntegerArray topKeys, MemoryAccessor topValues)
        {
            Guard.NotNull(bottom, nameof(bottom));
            Guard.NotNull(topValues, nameof(topValues));
            Guard.IsTrue(bottom._Slicer.Dimensions == topValues._Slicer.Dimensions, nameof(topValues));
            Guard.IsTrue(topKeys.Count <= bottom._Slicer.ItemsCount, nameof(topKeys));
            Guard.IsTrue(topKeys.Count == topValues._Slicer.ItemsCount, nameof(topValues));
            Guard.IsTrue(topKeys.All(item => item < (uint)bottom._Slicer.ItemsCount), nameof(topKeys));

            return new SparseArray<Vector4>(bottom.AsVector4Array(), topValues.AsVector4Array(), topKeys);
        }

        public static IList<Vector4> CreateColorSparseArray(MemoryAccessor bottom, IntegerArray topKeys, MemoryAccessor topValues, Single defaultW = 1)
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
