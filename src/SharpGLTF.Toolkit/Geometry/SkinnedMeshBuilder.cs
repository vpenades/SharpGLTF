using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;

namespace SharpGLTF.Geometry
{
    using Collections;

    public class SkinnedPrimitiveBuilder<TVertex, TSkin, TMaterial>
        where TVertex : struct
        where TSkin : struct
    {
        #region lifecycle

        internal SkinnedPrimitiveBuilder(SkinnedMeshBuilder<TVertex, TSkin, TMaterial> mesh, TMaterial material)
        {
            this._Mesh = mesh;
            this._Material = material;
        }

        #endregion

        #region data

        private readonly SkinnedMeshBuilder<TVertex, TSkin, TMaterial> _Mesh;

        private readonly TMaterial _Material;

        private readonly VertexColumn<(TVertex, TSkin)> _Vertices = new VertexColumn<(TVertex, TSkin)>();
        private readonly List<int> _Indices = new List<int>();

        #endregion

        #region properties

        public SkinnedMeshBuilder<TVertex, TSkin, TMaterial> Mesh => _Mesh;

        public TMaterial Material => _Material;

        public IReadOnlyList<(TVertex, TSkin)> Vertices => _Vertices;

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

        public void AddTriangle((TVertex, TSkin) a, (TVertex, TSkin) b, (TVertex, TSkin) c)
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

    public class SkinnedMeshBuilder<TVertex, TSkin, TMaterial>
        where TVertex : struct
        where TSkin : struct
    {
        #region lifecycle

        public SkinnedMeshBuilder(string name = null)
        {
            this.Name = name;
        }

        #endregion

        #region data

        private readonly Dictionary<TMaterial, SkinnedPrimitiveBuilder<TVertex,TSkin, TMaterial>> _Primitives = new Dictionary<TMaterial, SkinnedPrimitiveBuilder<TVertex, TSkin, TMaterial>>();

        #endregion

        #region properties

        public string Name { get; set; }

        public IReadOnlyCollection<SkinnedPrimitiveBuilder<TVertex, TSkin, TMaterial>> Primitives => _Primitives.Values;

        #endregion

        #region API

        public void AddPolygon(TMaterial material, params (TVertex, TSkin)[] points)
        {
            for (int i = 2; i < points.Length; ++i)
            {
                AddTriangle(material, points[0], points[i - 1], points[i]);
            }
        }

        public void AddTriangle(TMaterial material, (TVertex, TSkin) a, (TVertex, TSkin) b, (TVertex, TSkin) c)
        {
            if (!_Primitives.TryGetValue(material, out SkinnedPrimitiveBuilder<TVertex, TSkin, TMaterial> primitive))
            {
                primitive = new SkinnedPrimitiveBuilder<TVertex, TSkin, TMaterial>(this, material);
                _Primitives[material] = primitive;
            }

            primitive.AddTriangle(a, b, c);
        }

        public IEnumerable<(int, int, int)> GetTriangles(TMaterial material)
        {
            if (_Primitives.TryGetValue(material, out SkinnedPrimitiveBuilder<TVertex, TSkin, TMaterial> primitive)) return primitive.Triangles;

            return Enumerable.Empty<(int, int, int)>();
        }

        public IReadOnlyList<int> GetIndices(TMaterial material)
        {
            if (_Primitives.TryGetValue(material, out SkinnedPrimitiveBuilder<TVertex, TSkin, TMaterial> primitive)) return primitive.Indices;

            return new int[0];
        }

        #endregion
    }
}
