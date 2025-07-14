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
    /// Defines the memory encoding pattern for an arbitrary <see cref="BYTES"/>.
    /// </summary>
    [System.Diagnostics.DebuggerDisplay("{_GetDebuggerDisplay(),nq}")]
    public struct MemoryAccessInfo
    {
        #region debug

        internal readonly string _GetDebuggerDisplay()
        {
            return Diagnostics.DebuggerDisplay.ToReport(this);
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

        public readonly MemoryAccessInfo Slice(int itemStart, int itemCount)
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

        public AttributeFormat Format => new AttributeFormat(this.Dimensions, this.Encoding, this.Normalized);

        /// <summary>
        /// number of bytes to advance to the next item.
        /// </summary>
        public readonly int StepByteLength => _GetRowByteLength();

        public readonly int ItemByteLength => _GetItemByteLength();

        public readonly Boolean IsValidVertexAttribute
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

        /// <summary>
        /// returns true if this type can be used as a joint index.
        /// </summary>
        public readonly Boolean IsValidIndexer
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

        private readonly int _GetItemByteLength()
        {
            var xlen = Encoding.ByteLength();

            if (Dimensions != DIMENSIONS.SCALAR || Name != "INDEX")
            {
                xlen *= this.Dimensions.DimCount();
                xlen = xlen.WordPadded();
            }

            return xlen;
        }

        private readonly int _GetRowByteLength()
        {
            return Math.Max(ByteStride, _GetItemByteLength());
        }

        /// <summary>
        /// Assuming that <paramref name="attributes"/> are sequential and adyacent,
        /// it modifies the <see cref="ByteOffset"/> of each item of <paramref name="attributes"/> to ensure
        /// the offsets are sequential.
        /// </summary>
        /// <param name="attributes">A list of attributes to fix.</param>
        /// <param name="byteOffset">The initial byteoffset.</param>
        /// <param name="itemsCount">the default items count.</param>
        /// <returns>The byte stride.</returns>
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
}
