using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using System.Numerics;

namespace glTF2Sharp.Geometry
{
    using Schema2;    

    /// <summary>
    /// Defines a vertex attribute, dimensions and encoding.
    /// </summary>
    public struct VertexElement
    {
        #region lifecycle

        public static VertexElement[] Create(params string[] attributes)
        {
            return attributes.Select(item => CreateDefaultElement(item)).ToArray();
        }

        public static VertexElement CreateDefaultElement(string attribute)
        {
            switch (attribute)
            {
                case "POSITION": return new VertexElement("POSITION", ElementType.VEC3);
                case "NORMAL": return new VertexElement("NORMAL", ElementType.VEC3);
                case "TANGENT": return new VertexElement("TANGENT", ElementType.VEC4);

                case "TEXCOORD_0": return new VertexElement("TEXCOORD_0", ElementType.VEC2);
                case "TEXCOORD_1": return new VertexElement("TEXCOORD_1", ElementType.VEC2);
                case "TEXCOORD_2": return new VertexElement("TEXCOORD_2", ElementType.VEC2);
                case "TEXCOORD_3": return new VertexElement("TEXCOORD_3", ElementType.VEC2);

                case "COLOR_0": return new VertexElement("COLOR_0", ElementType.VEC4, ComponentType.UNSIGNED_BYTE, true);

                case "JOINTS_0": return new VertexElement("JOINTS_0", ElementType.VEC4, ComponentType.UNSIGNED_BYTE);
                case "WEIGHTS_0": return new VertexElement("WEIGHTS_0", ElementType.VEC4, ComponentType.UNSIGNED_BYTE, true);
            }

            throw new NotImplementedException();
        }

        public VertexElement(String attr, ElementType dim, ComponentType enc = ComponentType.FLOAT, Boolean nrm = false)
        {
            Attribute = attr;
            Dimensions = dim;
            Encoding = enc;
            Normalized = nrm;
        }

        #endregion

        #region data

        public String Attribute;
        public ElementType Dimensions;
        public ComponentType Encoding;
        public Boolean Normalized;

        #endregion

        #region API

        public int ByteSize => Dimensions.DimCount() * Encoding.ByteLength();

        public static int GetVertexByteSize(params VertexElement[] elements)
        {
            return elements.Sum(item => item.ByteSize);
        }

        public static int FindDimensions(VertexElement[] elements, string attribute)
        {
            var idx = Array.FindIndex(elements, item => item.Attribute == attribute);
            return idx < 0 ? 0 : elements[idx].Dimensions.DimCount();
        }

        public static int GetBufferByteSize(int count, params VertexElement[] elements)
        {
            var stride = GetVertexByteSize(elements);
            return stride * count;
        }

        public static Memory.IEncodedArray<Single> GetScalarColumn(Byte[] data, string attribute, params VertexElement[] elements)
        {
            var column = _GetColumn(data, elements, attribute, 0, int.MaxValue);
            if (column.Item3.Dimensions.DimCount() != 1) throw new ArgumentException(nameof(elements));
            return new Memory.ScalarArray(column.Item1, column.Item2, column.Item3.Encoding, column.Item3.Normalized);
        }

        public static Memory.IEncodedArray<Vector2> GetVector2Column(Byte[] data, string attribute, params VertexElement[] elements)
        {
            var column = _GetColumn(data, elements, attribute, 0, int.MaxValue);
            if (column.Item3.Dimensions.DimCount() != 2) throw new ArgumentException(nameof(elements));
            return new Memory.Vector2Array(column.Item1, column.Item2, column.Item3.Encoding, column.Item3.Normalized);
        }

        public static Memory.IEncodedArray<Vector3> GetVector3Column(Byte[] data, string attribute, params VertexElement[] elements)
        {
            var column = _GetColumn(data, elements, attribute, 0, int.MaxValue);
            if (column.Item3.Dimensions.DimCount() != 3) throw new ArgumentException(nameof(elements));
            return new Memory.Vector3Array(column.Item1, column.Item2, column.Item3.Encoding, column.Item3.Normalized);
        }

        public static Memory.IEncodedArray<Vector4> GetVector4Column(Byte[] data, string attribute, params VertexElement[] elements)
        {
            var column = _GetColumn(data, elements, attribute, 0, int.MaxValue);
            if (column.Item3.Dimensions.DimCount() != 4) throw new ArgumentException(nameof(elements));
            return new Memory.Vector4Array(column.Item1, column.Item2, column.Item3.Encoding, column.Item3.Normalized);
        }

        internal static (ArraySegment<Byte>, int, VertexElement) _GetColumn(Byte[] data, VertexElement[] elements, string attribute, int rowStart, int rowCount)
        {
            var index = Array.FindIndex(elements, item => item.Attribute == attribute);
            if (index < 0) throw new ArgumentException(nameof(attribute));

            var element = elements[index];

            var byteStride = GetVertexByteSize(elements);
            var byteOffset = elements.Take(index).Sum(item => item.ByteSize) + rowStart * byteStride;
            var byteLength = data.Length - byteOffset;

            if (rowCount < int.MaxValue) byteLength = rowCount * byteStride;

            var source = new ArraySegment<Byte>(data, byteOffset, byteLength);

            return (source, byteStride, element);
        }

        #endregion
    }
}
