using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using System.Numerics;


namespace glTF2Sharp.Geometry
{
    using SCALARARRAY = Memory.IEncodedArray<Single>;
    using VECTOR2ARRAY = Memory.IEncodedArray<Vector2>;
    using VECTOR3ARRAY = Memory.IEncodedArray<Vector3>;
    using VECTOR4ARRAY = Memory.IEncodedArray<Vector4>;

    /// <summary>
    /// represents an abstraction of a vertex buffer.
    /// </summary>
    public abstract class VertexBuffer
    {
        #region properties

        public abstract IReadOnlyList<VertexElement> Attributes { get; }

        public abstract int Count { get; }

        #endregion

        #region API

        public abstract int GetDimensions(string attribute);

        public abstract SCALARARRAY GetScalarColumn(String attribute, int rowStart = 0, int rowCount = int.MaxValue);

        public abstract VECTOR2ARRAY GetVector2Column(String attribute, int rowStart = 0, int rowCount = int.MaxValue);

        public abstract VECTOR3ARRAY GetVector3Column(String attribute, int rowStart = 0, int rowCount = int.MaxValue);

        public abstract VECTOR4ARRAY GetVector4Column(String attribute, int rowStart = 0, int rowCount = int.MaxValue);

        public void SetScalarColumn(String attribute, int rowStart, Single[] values)
        {
            var dstColumn = GetScalarColumn(attribute, rowStart);
            Memory.EncodedArrayUtils.CopyTo(values, dstColumn);
        }

        public void SetVector2Column(String attribute, int rowStart, Vector2[] values)
        {
            var dstColumn = GetVector2Column(attribute, rowStart);
            Memory.EncodedArrayUtils.CopyTo(values, dstColumn);
        }

        public void SetVector3Column(String attribute, int rowStart, Vector3[] values)
        {
            var dstColumn = GetVector3Column(attribute, rowStart);
            Memory.EncodedArrayUtils.CopyTo(values, dstColumn);
        }

        public void SetVector4Column(String attribute, int rowStart, Vector4[] values)
        {
            var dstColumn = GetVector4Column(attribute, rowStart);
            Memory.EncodedArrayUtils.CopyTo(values, dstColumn);
        }

        public void SetBuffer(int rowIndex, VertexBuffer srcBuffer)
        {
            foreach (var ve in this.Attributes)
            {
                var l = ve.Dimensions.DimCount();

                if (l == 1 && srcBuffer.GetDimensions(ve.Attribute) == 1)
                {
                    var values = srcBuffer.GetScalarColumn(ve.Attribute).ToArray();
                    SetScalarColumn(ve.Attribute, rowIndex, values);
                }

                if (l == 2 && srcBuffer.GetDimensions(ve.Attribute) == 2)
                {
                    var values = srcBuffer.GetVector2Column(ve.Attribute).ToArray();
                    SetVector2Column(ve.Attribute, rowIndex, values);
                }

                if (l == 3 && srcBuffer.GetDimensions(ve.Attribute) == 3)
                {
                    var values = srcBuffer.GetVector3Column(ve.Attribute).ToArray();
                    SetVector3Column(ve.Attribute, rowIndex, values);
                }

                if (l == 4 && srcBuffer.GetDimensions(ve.Attribute) == 4)
                {
                    var values = srcBuffer.GetVector4Column(ve.Attribute).ToArray();
                    SetVector4Column(ve.Attribute, rowIndex, values);
                }
            }
        }

        #endregion

        #region static API

        public static VertexArray MergeBuffers(VertexBuffer a, VertexBuffer b, params VertexElement[] elements)
        {
            var dstBuffer = new VertexArray(a.Count + b.Count, elements);

            dstBuffer.SetBuffer(0, a);
            dstBuffer.SetBuffer(a.Count, b);

            return dstBuffer;
        }

        #endregion 
    }

    /// <summary>
    /// Represents a fixed collection of vertices with a specific vertex attributes definition. 
    /// </summary>
    public class VertexArray : VertexBuffer
    {
        #region lifecycle

        public VertexArray(int count, params string[] attributes)
        {
            _Elements = VertexElement.Create(attributes);

            _ByteStride = VertexElement.GetVertexByteSize(_Elements);

            _Buffer = new Byte[_ByteStride * count];
        }

        public VertexArray(int count, params VertexElement[] elements)
        {
            _Elements = elements;

            _ByteStride = VertexElement.GetVertexByteSize(_Elements);

            _Buffer = new Byte[_ByteStride * count];
        }

        #endregion

        #region data

        private VertexElement[] _Elements;
        private int _ByteStride;
        private Byte[] _Buffer;

        #endregion

        #region properties

        public override IReadOnlyList<VertexElement> Attributes => _Elements;

        public override int Count => _Buffer.Length / _ByteStride;

        public int ByteStride => _ByteStride;

        public Byte[] Data => _Buffer;        

        #endregion

        #region API

        public override int GetDimensions(string attribute) { return VertexElement.FindDimensions(_Elements, attribute); }

        public override SCALARARRAY GetScalarColumn(String attribute, int rowStart = 0, int rowCount = int.MaxValue)
        {
            Guard.MustBeBetweenOrEqualTo(rowStart, 0, Count - rowCount, nameof(rowStart));            

            var column = VertexElement._GetColumn(_Buffer, _Elements, attribute, rowStart, rowCount);
            if (column.Item3.Dimensions.DimCount() != 1) throw new ArgumentException(nameof(attribute));
            return new Memory.ScalarArray(column.Item1, column.Item2, column.Item3.Encoding, column.Item3.Normalized);
        }

        public override VECTOR2ARRAY GetVector2Column(String attribute, int rowStart = 0, int rowCount = int.MaxValue)
        {
            Guard.MustBeBetweenOrEqualTo(rowStart, 0, Count - rowCount, nameof(rowStart));            

            var column = VertexElement._GetColumn(_Buffer, _Elements, attribute, rowStart, rowCount);
            if (column.Item3.Dimensions.DimCount() != 2) throw new ArgumentException(nameof(attribute));
            return new Memory.Vector2Array(column.Item1, column.Item2, column.Item3.Encoding, column.Item3.Normalized);
        }

        public override VECTOR3ARRAY GetVector3Column(String attribute, int rowStart = 0, int rowCount = int.MaxValue)
        {
            Guard.MustBeBetweenOrEqualTo(rowStart, 0, Count - rowCount, nameof(rowStart));            

            var column = VertexElement._GetColumn(_Buffer, _Elements, attribute, rowStart, rowCount);
            if (column.Item3.Dimensions.DimCount() != 3) throw new ArgumentException(nameof(attribute));
            return new Memory.Vector3Array(column.Item1, column.Item2, column.Item3.Encoding, column.Item3.Normalized);
        }

        public override VECTOR4ARRAY GetVector4Column(String attribute, int rowStart = 0, int rowCount = int.MaxValue)
        {
            Guard.MustBeBetweenOrEqualTo(rowStart, 0, Count - rowCount, nameof(rowStart));            

            var column = VertexElement._GetColumn(_Buffer, _Elements, attribute, rowStart, rowCount);
            if (column.Item3.Dimensions.DimCount() != 4) throw new ArgumentException(nameof(attribute));
            return new Memory.Vector4Array(column.Item1, column.Item2, column.Item3.Encoding, column.Item3.Normalized);
        }                
        
        #endregion              
    }

    /// <summary>
    /// Represents a segment within a VertexArray
    /// </summary>
    public class VertexArraySegment : VertexBuffer
    {
        #region lifecycle

        public VertexArraySegment(VertexArray array, int offset, int count)
        {
            _Buffer = array;
            _Offset = offset;
            _Count = count;
        }

        #endregion

        #region data

        private VertexArray _Buffer;
        private int _Offset;
        private int _Count;

        #endregion

        #region properties

        public override IReadOnlyList<VertexElement> Attributes => _Buffer.Attributes;

        public int Offset => _Offset;

        public int ByteOffset => _Buffer.ByteStride * _Offset;

        public override int Count => _Count;

        public VertexArray Array => _Buffer;

        #endregion

        #region API

        public override int GetDimensions(string attribute) { return _Buffer.GetDimensions(attribute); }

        public override SCALARARRAY GetScalarColumn(String attribute, int rowStart = 0, int rowCount = int.MaxValue)
        {
            return _Buffer.GetScalarColumn(attribute, _Offset + rowStart, Math.Min(_Count, rowCount));
        }

        public override VECTOR2ARRAY GetVector2Column(string attribute, int rowStart = 0, int rowCount = int.MaxValue)
        {
            return _Buffer.GetVector2Column(attribute, _Offset + rowStart, Math.Min(_Count, rowCount));
        }

        public override VECTOR3ARRAY GetVector3Column(string attribute, int rowStart = 0, int rowCount = int.MaxValue)
        {
            return _Buffer.GetVector3Column(attribute, _Offset + rowStart, Math.Min(_Count, rowCount));
        }

        public override VECTOR4ARRAY GetVector4Column(string attribute, int rowStart = 0, int rowCount = int.MaxValue)
        {
            return _Buffer.GetVector4Column(attribute, _Offset + rowStart, Math.Min(_Count, rowCount));
        }

        #endregion
    }
}
