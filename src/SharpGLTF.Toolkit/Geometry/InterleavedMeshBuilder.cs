using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace SharpGLTF.Geometry
{
    using Collections;

    public class InterleavedMeshBuilder<TVertex, TMaterial>
        where TVertex : struct
    {
        #region data

        private readonly VertexColumn<TVertex> _Vertices = new VertexColumn<TVertex>();
        private readonly Dictionary<TMaterial, List<int>> _Indices = new Dictionary<TMaterial, List<int>>();

        #endregion

        #region properties

        public IReadOnlyList<TVertex> Vertices => _Vertices;

        public IReadOnlyCollection<TMaterial> Materials => _Indices.Keys;

        #endregion

        #region API

        public void AddPolygon(TMaterial material, params TVertex[] points)
        {
            for (int i = 2; i < points.Length; ++i)
            {
                AddTriangle(material, points[0], points[i - 1], points[i]);
            }
        }

        public void AddTriangle(TMaterial material, TVertex a, TVertex b, TVertex c)
        {
            var aa = _Vertices.Use(a);
            var bb = _Vertices.Use(b);
            var cc = _Vertices.Use(c);

            // check for degenerated triangles:
            if (aa == bb) return;
            if (aa == cc) return;
            if (bb == cc) return;

            if (!_Indices.TryGetValue(material, out List<int> indices))
            {
                indices = new List<int>();
                _Indices[material] = indices;
            }

            indices.Add(aa);
            indices.Add(bb);
            indices.Add(cc);
        }

        public IEnumerable<(int, int, int)> GetTriangles(TMaterial material)
        {
            if (!_Indices.TryGetValue(material, out List<int> indices)) yield break;

            for (int i = 2; i < indices.Count; i += 3)
            {
                yield return (indices[i - 2], indices[i - 1], indices[i]);
            }
        }

        public IReadOnlyList<int> GetIndices(TMaterial material)
        {
            return _Indices.TryGetValue(material, out List<int> indices) ? indices : null;
        }

        #endregion
    }
}
