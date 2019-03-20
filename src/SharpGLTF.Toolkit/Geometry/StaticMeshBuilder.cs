using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;

namespace SharpGLTF.Geometry
{
    using Collections;

    public class StaticPrimitiveBuilder<TVertex, TMaterial>
        where TVertex : struct
    {
        #region lifecycle

        internal StaticPrimitiveBuilder(StaticMeshBuilder<TVertex, TMaterial> mesh, TMaterial material)
        {
            this._Mesh = mesh;
            this._Material = material;
        }

        #endregion

        #region data

        private readonly StaticMeshBuilder<TVertex, TMaterial> _Mesh;

        private readonly TMaterial _Material;

        private readonly VertexColumn<TVertex> _Vertices = new VertexColumn<TVertex>();
        private readonly List<int> _Indices = new List<int>();

        #endregion

        #region properties

        public StaticMeshBuilder<TVertex, TMaterial> Mesh => _Mesh;

        public TMaterial Material => _Material;

        public IReadOnlyList<TVertex> Vertices => _Vertices;

        public IReadOnlyList<int> Indices => _Indices;

        public IEnumerable<(int, int, int)> Triangles
        {
            get
            {
                for (int i = 2; i < _Indices.Count; i += 3)
                {
                    yield return (_Indices[i - 2], _Indices[i - 1], _Indices[i]);
                }
            }
        }

        #endregion

        #region API

        public void AddTriangle(TVertex a, TVertex b, TVertex c)
        {
            var aa = _Vertices.Use(a);
            var bb = _Vertices.Use(b);
            var cc = _Vertices.Use(c);

            // check for degenerated triangles:
            if (aa == bb) return;
            if (aa == cc) return;
            if (bb == cc) return;

            _Indices.Add(aa);
            _Indices.Add(bb);
            _Indices.Add(cc);
        }

        #endregion
    }

    public class StaticMeshBuilder<TVertex, TMaterial>
        where TVertex : struct
    {
        #region lifecycle

        public StaticMeshBuilder(string name = null)
        {
            this.Name = name;
        }

        #endregion

        #region data

        private readonly Dictionary<TMaterial, StaticPrimitiveBuilder<TVertex, TMaterial>> _Primitives = new Dictionary<TMaterial, StaticPrimitiveBuilder<TVertex, TMaterial>>();

        #endregion

        #region properties

        public string Name { get; set; }

        public IReadOnlyCollection<StaticPrimitiveBuilder<TVertex, TMaterial>> Primitives => _Primitives.Values;

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
            if (!_Primitives.TryGetValue(material, out StaticPrimitiveBuilder<TVertex, TMaterial> primitive))
            {
                primitive = new StaticPrimitiveBuilder<TVertex, TMaterial>(this, material);
                _Primitives[material] = primitive;
            }

            primitive.AddTriangle(a, b, c);
        }

        public IEnumerable<(int, int, int)> GetTriangles(TMaterial material)
        {
            if (_Primitives.TryGetValue(material, out StaticPrimitiveBuilder<TVertex, TMaterial> primitive)) return primitive.Triangles;

            return Enumerable.Empty<(int, int, int)>();
        }

        public IReadOnlyList<int> GetIndices(TMaterial material)
        {
            if (_Primitives.TryGetValue(material, out StaticPrimitiveBuilder<TVertex, TMaterial> primitive)) return primitive.Indices;

            return new int[0];
        }

        #endregion
    }
}
