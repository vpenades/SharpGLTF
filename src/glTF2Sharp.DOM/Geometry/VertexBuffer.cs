using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using System.Numerics;

namespace glTF2Sharp.Geometry
{
    public class VertexBuffer
    {
        #region lifecycle

        public VertexBuffer(int count, params string[] attributes)
        {
            _Elements = VertexElement.Create(attributes);

            _ByteStride = VertexElement.GetVertexByteSize(_Elements);

            _Buffer = new Byte[_ByteStride * count];
        }

        public VertexBuffer(int count, params VertexElement[] elements)
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

        public IReadOnlyList<VertexElement> Attributes => _Elements;

        public int ByteStride => _ByteStride;

        public Byte[] Data => _Buffer;

        public int Count => _Buffer.Length / _ByteStride;

        #endregion

        #region API

        public Memory.IEncodedArray<Single> GetScalarColumn(String attribute)
        {
            return VertexElement.GetScalarColumn(_Buffer, attribute, _Elements);
        }

        public Memory.IEncodedArray<Vector2> GetVector2Column(String attribute)
        {
            return VertexElement.GetVector2Column(_Buffer, attribute, _Elements);
        }

        public Memory.IEncodedArray<Vector3> GetVector3Column(String attribute)
        {
            return VertexElement.GetVector3Column(_Buffer, attribute, _Elements);
        }

        public Memory.IEncodedArray<Vector4> GetVector4Column(String attribute)
        {
            return VertexElement.GetVector4Column(_Buffer, attribute, _Elements);
        }

        public void SetScalarColumn(String attribute, Single[] values)
        {
            var dstColumn = GetScalarColumn(attribute);
            Memory.EncodedArrayUtils.CopyTo(values, dstColumn);
        }

        public void SetVector2Column(String attribute, Vector2[] values)
        {
            var dstColumn = GetVector2Column(attribute);
            Memory.EncodedArrayUtils.CopyTo(values, dstColumn);
        }

        public void SetVector3Column(String attribute, Vector3[] values)
        {
            var dstColumn = GetVector3Column(attribute);
            Memory.EncodedArrayUtils.CopyTo(values, dstColumn);
        }

        public void SetVector4Column(String attribute, Vector4[] values)
        {
            var dstColumn = GetVector4Column(attribute);
            Memory.EncodedArrayUtils.CopyTo(values, dstColumn);
        }

        #endregion
    }
}
