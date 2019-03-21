using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;

namespace SharpGLTF.Geometry
{
    using Collections;

    public class SkinnedPrimitiveBuilder<TMaterial, TVertex, TJoints>
        where TVertex : struct, VertexTypes.IVertex
        where TJoints : struct, VertexTypes.IJoints
    {
        #region lifecycle

        internal SkinnedPrimitiveBuilder(SkinnedMeshBuilder<TMaterial, TVertex, TJoints> mesh, TMaterial material)
        {
            this._Mesh = mesh;
            this._Material = material;
        }

        #endregion

        #region data

        private readonly SkinnedMeshBuilder<TMaterial, TVertex, TJoints> _Mesh;

        private readonly TMaterial _Material;

        private readonly VertexColumn<(TVertex, TJoints)> _Vertices = new VertexColumn<(TVertex, TJoints)>();
        private readonly List<int> _Indices = new List<int>();

        #endregion

        #region properties

        public SkinnedMeshBuilder<TMaterial, TVertex, TJoints> Mesh => _Mesh;

        public TMaterial Material => _Material;

        public IReadOnlyList<(TVertex, TJoints)> Vertices => _Vertices;

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

        public void AddTriangle((TVertex, TJoints) a, (TVertex, TJoints) b, (TVertex, TJoints) c)
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

    public class SkinnedMeshBuilder<TMaterial, TVertex, TJoints>
        where TVertex : struct, VertexTypes.IVertex
        where TJoints : struct, VertexTypes.IJoints
    {
        #region lifecycle

        public SkinnedMeshBuilder(string name = null)
        {
            this.Name = name;
        }

        #endregion

        #region data

        private readonly Dictionary<TMaterial, SkinnedPrimitiveBuilder<TMaterial, TVertex, TJoints>> _Primitives = new Dictionary<TMaterial, SkinnedPrimitiveBuilder<TMaterial, TVertex, TJoints>>();

        #endregion

        #region properties

        public string Name { get; set; }

        public IReadOnlyCollection<SkinnedPrimitiveBuilder<TMaterial, TVertex, TJoints>> Primitives => _Primitives.Values;

        #endregion

        #region API

        public void AddPolygon(TMaterial material, params (TVertex, TJoints)[] points)
        {
            for (int i = 2; i < points.Length; ++i)
            {
                AddTriangle(material, points[0], points[i - 1], points[i]);
            }
        }

        public void AddTriangle(TMaterial material, (TVertex, TJoints) a, (TVertex, TJoints) b, (TVertex, TJoints) c)
        {
            if (!_Primitives.TryGetValue(material, out SkinnedPrimitiveBuilder<TMaterial, TVertex, TJoints> primitive))
            {
                primitive = new SkinnedPrimitiveBuilder<TMaterial, TVertex, TJoints>(this, material);
                _Primitives[material] = primitive;
            }

            primitive.AddTriangle(a, b, c);
        }

        public IEnumerable<(int, int, int)> GetTriangles(TMaterial material)
        {
            if (_Primitives.TryGetValue(material, out SkinnedPrimitiveBuilder<TMaterial, TVertex, TJoints> primitive)) return primitive.Triangles;

            return Enumerable.Empty<(int, int, int)>();
        }

        public IReadOnlyList<int> GetIndices(TMaterial material)
        {
            if (_Primitives.TryGetValue(material, out SkinnedPrimitiveBuilder<TMaterial, TVertex, TJoints> primitive)) return primitive.Indices;

            return new int[0];
        }
        
        #endregion
    }
}
