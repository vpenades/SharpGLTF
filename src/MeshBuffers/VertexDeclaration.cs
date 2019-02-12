using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Numerics;

namespace MeshBuffers
{
    using ROW = ListSegment<Single>;

    struct _VertexElement
    {
        public _VertexElement(string attribute, int dim, int offset)
        {
            _Attribute = attribute;
            _Dimensions = dim;
            _Offset = offset;
        }

        internal readonly String _Attribute;
        internal readonly int _Dimensions;
        internal readonly int _Offset;
    }

    public class VertexDeclaration : IEquatable<VertexDeclaration>
    {
        #region lifecycle

        public VertexDeclaration() { _Elements = new _VertexElement[0]; }

        private VertexDeclaration(_VertexElement[] elements, _VertexElement extra)
        {
            _Elements = new _VertexElement[elements.Length + 1];
            elements.CopyTo(_Elements, 0);
            _Elements[_Elements.Length - 1] = extra;

            _Stride = _Elements.Sum(item => item._Dimensions);

            _HashCode = _Elements.Select(item => item.GetHashCode()).Aggregate((a, b) => (a * 17) ^ b);

            _PositionV3 = _Offset("POSITION");
            _NormalV3 = _Offset("NORMAL");
        }

        private int _Offset(string attribute)
        {
            var element = _Elements.FirstOrDefault(item => item._Attribute == attribute);
            return element._Attribute == null ? -1 : element._Offset;
        }

        public VertexDeclaration WithSingle(string attribute)
        {
            var e = new _VertexElement(attribute, 1, Stride);

            return new VertexDeclaration(_Elements, e);
        }

        public VertexDeclaration WithVector2(string attribute)
        {
            var e = new _VertexElement(attribute, 2, Stride);

            return new VertexDeclaration(_Elements, e);
        }

        public VertexDeclaration WithVector3(string attribute)
        {
            var e = new _VertexElement(attribute, 3, Stride);

            return new VertexDeclaration(_Elements, e);
        }

        public VertexDeclaration WithVector4(string attribute)
        {
            var e = new _VertexElement(attribute, 4, Stride);

            return new VertexDeclaration(_Elements, e);
        }

        #endregion

        #region data

        private readonly _VertexElement[] _Elements;
        private readonly int _Stride;
        private readonly int _HashCode;

        private readonly int _PositionV3;
        private readonly int _NormalV3;

        public bool Equals(VertexDeclaration other) { return AreEqual(this, other); }

        public static bool AreEqual(VertexDeclaration a, VertexDeclaration b)
        {
            if (Object.ReferenceEquals(a, b)) return true;
            return Enumerable.SequenceEqual(a._Elements, b._Elements);
        }

        public override int GetHashCode() { return _HashCode; }

        #endregion

        #region API

        public int Stride => _Stride;

        public Vertex CreateVertex() { return new Vertex(this); }

        internal Vector3 GetPosition(ROW vertex)
        {
            return new Vector3
                (
                vertex[_PositionV3 + 0],
                vertex[_PositionV3 + 1],
                vertex[_PositionV3 + 2]
                );
        }

        internal void SetPosition(ROW vertex, Vector3 value)
        {
            vertex[_PositionV3 + 0] = value.X;
            vertex[_PositionV3 + 1] = value.Y;
            vertex[_PositionV3 + 2] = value.Z;
        }

        internal Vector3 GetNormal(ROW vertex)
        {
            return new Vector3
                (
                vertex[_NormalV3 + 0],
                vertex[_NormalV3 + 1],
                vertex[_NormalV3 + 2]
                );
        }

        internal void SetNormal(ROW vertex, Vector3 value)
        {
            vertex[_NormalV3 + 0] = value.X;
            vertex[_NormalV3 + 1] = value.Y;
            vertex[_NormalV3 + 2] = value.Z;
        }

        #endregion
    }
}
