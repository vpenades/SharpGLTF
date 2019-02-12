using System;
using System.Collections.Generic;
using System.Text;
using System.Numerics;
using System.Linq;

namespace MeshBuffers
{
    using ROW = ListSegment<Single>;

    public struct Vertex : IEquatable<Vertex>
    {
        #region constructor

        internal Vertex(VertexDeclaration declaration)
        {
            _Declaration = declaration;
            _Data = new ROW(new Single[_Declaration.Stride]);
        }

        internal Vertex(VertexDeclaration declaration, ROW data)
        {
            System.Diagnostics.Debug.Assert(declaration.Stride == data.Count);

            _Declaration = declaration;
            _Data = data;
        }

        #endregion

        #region data

        internal readonly VertexDeclaration _Declaration;
        internal readonly ROW _Data;

        public bool Equals(Vertex other) { return AreEqual(this, other); }

        public static bool AreEqual(Vertex a, Vertex b)
        {
            VertexDeclaration.AreEqual(a._Declaration, b._Declaration);
            return a._Data.SequenceEqual(b._Data);
        }

        public override int GetHashCode()
        {
            return _Declaration.GetHashCode() ^ _Data.Select(item => item.GetHashCode()).Aggregate((a, b) => (a * 17) ^ b);
        }

        #endregion

        #region properties

        public VertexDeclaration Declaration => _Declaration;

        public Vector3 Position
        {
            get => _Declaration.GetPosition(_Data);
            set => _Declaration.SetPosition(_Data, value);
        }

        public Vector3 Normal
        {
            get => _Declaration.GetNormal(_Data);
            set => _Declaration.SetNormal(_Data, value);
        }

        #endregion
    }
}
