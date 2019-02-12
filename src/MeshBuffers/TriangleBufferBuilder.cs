using System;
using System.Collections.Generic;
using System.Text;

namespace MeshBuffers
{
    public class TriangleBufferBuilder
    {
        #region lifecycle

        public TriangleBufferBuilder(VertexBuffer vbuffer)
        {
            _Vertices = vbuffer;
        }

        public TriangleBufferBuilder(VertexDeclaration declaration)
        {
            _Vertices = new VertexBuffer(declaration);
        }

        #endregion

        #region data        

        private readonly VertexBuffer _Vertices;

        private readonly List<int> _Indices = new List<int>();

        private readonly Dictionary<Vertex, int> _VertexMap = new Dictionary<Vertex, int>();

        #endregion

        #region properties

        public VertexBuffer Vertices => _Vertices;

        public IReadOnlyList<int> Indices => _Indices;

        #endregion

        #region API

        private int UseVertex(Vertex v)
        {
            if (_VertexMap.TryGetValue(v, out int index)) return index;
            index = _Vertices.Count;

            _Vertices.Add(v);
            v = _Vertices[index];
            _VertexMap[v] = index;

            return index;
        }

        public void AddTriangle(Vertex a, Vertex b, Vertex c)
        {
            _Indices.Add(UseVertex(a));
            _Indices.Add(UseVertex(b));
            _Indices.Add(UseVertex(c));
        }

        #endregion
    }
}
