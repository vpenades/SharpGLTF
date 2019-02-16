using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;

namespace glTF2Sharp.Memory
{
    /// <summary>
    /// Represents the vertex attribute access info
    /// </summary>
    public struct AttributeInfo
    {
        public AttributeInfo(string name, int byteOffset, int itemsCount, int byteStride, Schema2.ElementType dimensions, Schema2.ComponentType encoding, Boolean normalized)
        {
            this.Name = name;
            this.ByteOffset = byteOffset;
            this.ItemsCount = itemsCount;
            this.ByteStride = byteStride;
            this.Dimensions = dimensions;
            this.Encoding = encoding;
            this.Normalized = normalized;
        }

        public String Name;
        public int ByteOffset;
        public int ItemsCount;
        public int ByteStride;
        public Schema2.ElementType Dimensions;
        public Schema2.ComponentType Encoding;
        public Boolean Normalized;

        public int ByteLength => this.Dimensions.DimCount() * this.Encoding.ByteLength();

        public Boolean IsValidVertexAttribute
        {
            get
            {
                if (this.ByteOffset < 0) return false;
                if (this.ItemsCount < 0) return false;
                if (this.ByteStride < 0) return false;
                var len = this.Dimensions.DimCount() * this.Encoding.ByteLength();
                if (len == 0 || (len & 3) != 0) return false;

                if (this.ByteStride > 0 && this.ByteStride < len) return false;
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
                if (this.Dimensions != Schema2.ElementType.SCALAR) return false;
                if (this.Normalized) return false;
                if (this.ByteStride == 0) return true;
                if (this.ByteStride == 1) return true;
                if (this.ByteStride == 2) return true;
                if (this.ByteStride == 4) return true;
                return false;
            }
        }
    }

    public struct AttributeAccessor
    {
        public AttributeAccessor(AttributeInfo info, ArraySegment<Byte> data)
        {
            this.Attribute = info;
            this.Data = data;
        }

        public AttributeInfo Attribute;
        public ArraySegment<Byte> Data;

        public ScalarArray AsScalarArray()
        {
            Guard.IsTrue(Attribute.IsValidVertexAttribute, nameof(Attribute));
            Guard.IsTrue(Attribute.Dimensions == Schema2.ElementType.SCALAR, nameof(Attribute));
            return new ScalarArray(Data, Attribute.ByteOffset, Attribute.ItemsCount, Attribute.ByteStride, Attribute.Encoding, Attribute.Normalized);
        }

        public Vector2Array AsVector2Array()
        {
            Guard.IsTrue(Attribute.IsValidVertexAttribute, nameof(Attribute));
            Guard.IsTrue(Attribute.Dimensions == Schema2.ElementType.VEC2, nameof(Attribute));
            return new Vector2Array(Data, Attribute.ByteOffset, Attribute.ItemsCount, Attribute.ByteStride, Attribute.Encoding, Attribute.Normalized);
        }

        public Vector3Array AsVector3Array()
        {
            Guard.IsTrue(Attribute.IsValidVertexAttribute, nameof(Attribute));
            Guard.IsTrue(Attribute.Dimensions == Schema2.ElementType.VEC3, nameof(Attribute));
            return new Vector3Array(Data, Attribute.ByteOffset, Attribute.ItemsCount, Attribute.ByteStride, Attribute.Encoding, Attribute.Normalized);
        }

        public Vector4Array AsVector4Array()
        {
            Guard.IsTrue(Attribute.IsValidVertexAttribute, nameof(Attribute));
            Guard.IsTrue(Attribute.Dimensions == Schema2.ElementType.VEC4, nameof(Attribute));
            return new Vector4Array(Data, Attribute.ByteOffset, Attribute.ItemsCount, Attribute.ByteStride, Attribute.Encoding, Attribute.Normalized);
        }

        public Matrix4x4Array AsMatrix4x4Array()
        {
            Guard.IsTrue(Attribute.IsValidVertexAttribute, nameof(Attribute));
            Guard.IsTrue(Attribute.Dimensions == Schema2.ElementType.MAT4, nameof(Attribute));
            return new Matrix4x4Array(Data, Attribute.ByteOffset, Attribute.ItemsCount, Attribute.ByteStride, Attribute.Encoding, Attribute.Normalized);
        }

        public static IEncodedArray<Single> CreateScalarSparseArray(AttributeAccessor bottom, IntegerArray topKeys, AttributeAccessor topValues)
        {
            Guard.IsTrue(bottom.Attribute.Dimensions == topValues.Attribute.Dimensions, nameof(topValues));
            Guard.IsTrue(topKeys.Count <= bottom.Attribute.ItemsCount, nameof(topKeys));
            Guard.IsTrue(topKeys.Count == topValues.Attribute.ItemsCount, nameof(topValues));
            Guard.IsTrue(topKeys.All(item => item < (uint)bottom.Attribute.ItemsCount), nameof(topKeys));

            return new SparseArray<Single>(bottom.AsScalarArray(), topValues.AsScalarArray(), topKeys);
        }

        public static IEncodedArray<Vector2> CreateVector2SparseArray(AttributeAccessor bottom, IntegerArray topKeys, AttributeAccessor topValues)
        {
            Guard.IsTrue(bottom.Attribute.Dimensions == topValues.Attribute.Dimensions, nameof(topValues));
            Guard.IsTrue(topKeys.Count <= bottom.Attribute.ItemsCount, nameof(topKeys));
            Guard.IsTrue(topKeys.Count == topValues.Attribute.ItemsCount, nameof(topValues));
            Guard.IsTrue(topKeys.All(item => item < (uint)bottom.Attribute.ItemsCount), nameof(topKeys));

            return new SparseArray<Vector2>(bottom.AsVector2Array(), topValues.AsVector2Array(), topKeys);
        }

        public static IEncodedArray<Vector3> CreateVector3SparseArray(AttributeAccessor bottom, IntegerArray topKeys, AttributeAccessor topValues)
        {
            Guard.IsTrue(bottom.Attribute.Dimensions == topValues.Attribute.Dimensions, nameof(topValues));
            Guard.IsTrue(topKeys.Count <= bottom.Attribute.ItemsCount, nameof(topKeys));
            Guard.IsTrue(topKeys.Count == topValues.Attribute.ItemsCount, nameof(topValues));
            Guard.IsTrue(topKeys.All(item => item < (uint)bottom.Attribute.ItemsCount), nameof(topKeys));

            return new SparseArray<Vector3>(bottom.AsVector3Array(), topValues.AsVector3Array(), topKeys);
        }

        public static IEncodedArray<Vector4> CreateVector4SparseArray(AttributeAccessor bottom, IntegerArray topKeys, AttributeAccessor topValues)
        {
            Guard.IsTrue(bottom.Attribute.Dimensions == topValues.Attribute.Dimensions, nameof(topValues));
            Guard.IsTrue(topKeys.Count <= bottom.Attribute.ItemsCount, nameof(topKeys));
            Guard.IsTrue(topKeys.Count == topValues.Attribute.ItemsCount, nameof(topValues));
            Guard.IsTrue(topKeys.All(item => item < (uint)bottom.Attribute.ItemsCount), nameof(topKeys));

            return new SparseArray<Vector4>(bottom.AsVector4Array(), topValues.AsVector4Array(), topKeys);
        }
    }
}
