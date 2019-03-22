using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;

namespace SharpGLTF.Geometry
{
    using Collections;

    public class PrimitiveBuilder<TMaterial, TvP, TvM, TvJ>
        where TvP : struct, VertexTypes.IVertexPosition
        where TvM : struct, VertexTypes.IVertexMaterial
        where TvJ : struct, VertexTypes.IVertexJoints
    {
        #region lifecycle

        internal PrimitiveBuilder(MeshBuilder<TMaterial, TvP, TvM, TvJ> mesh, TMaterial material, bool strict)
        {
            this._Scrict = strict;
            this._Mesh = mesh;
            this._Material = material;
        }

        #endregion

        #region data

        private readonly bool _Scrict;

        private readonly MeshBuilder<TMaterial, TvP, TvM, TvJ> _Mesh;

        private readonly TMaterial _Material;

        private readonly VertexColumn<(TvP, TvM, TvJ)> _Vertices = new VertexColumn<(TvP, TvM, TvJ)>();
        private readonly List<int> _Indices = new List<int>();

        #endregion

        #region properties

        public MeshBuilder<TMaterial, TvP, TvM, TvJ> Mesh => _Mesh;

        public TMaterial Material => _Material;

        public IReadOnlyList<(TvP, TvM, TvJ)> Vertices => _Vertices;

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

        public int UseVertex((TvP, TvM, TvJ) vertex)
        {
            if (_Scrict)
            {
                vertex.Item1.Validate();
                vertex.Item2.Validate();
                vertex.Item3.Validate();
            }

            return _Vertices.Use(vertex);
        }

        public void AddTriangle((TvP, TvM, TvJ) a, (TvP, TvM, TvJ) b, (TvP, TvM, TvJ) c)
        {
            var aa = UseVertex(a);
            var bb = UseVertex(b);
            var cc = UseVertex(c);

            // check for degenerated triangles:
            if (aa == bb || aa == cc || bb == cc)
            {
                if (_Scrict) throw new ArgumentException($"Invalid triangle {aa} {bb} {cc}");
                return;
            }

            _Indices.Add(aa);
            _Indices.Add(bb);
            _Indices.Add(cc);
        }

        public void Validate()
        {
            foreach (var v in _Vertices)
            {
                v.Item1.Validate();
                v.Item2.Validate();
                v.Item3.Validate();
            }
        }

        #endregion
    }

    public class MeshBuilder<TMaterial, TvP, TvM, TvJ>
        where TvP : struct, VertexTypes.IVertexPosition
        where TvM : struct, VertexTypes.IVertexMaterial
        where TvJ : struct, VertexTypes.IVertexJoints
    {
        #region lifecycle

        public MeshBuilder(string name = null)
        {
            this.Name = name;
        }

        #endregion

        #region data

        private readonly Dictionary<TMaterial, PrimitiveBuilder<TMaterial, TvP, TvM, TvJ>> _Primitives = new Dictionary<TMaterial, PrimitiveBuilder<TMaterial, TvP, TvM, TvJ>>();

        #endregion

        #region properties

        public Boolean StrictMode { get; set; }

        public string Name { get; set; }

        public IReadOnlyCollection<PrimitiveBuilder<TMaterial, TvP, TvM, TvJ>> Primitives => _Primitives.Values;

        #endregion

        #region API

        public void AddPolygon(TMaterial material, params (TvP, TvM, TvJ)[] points)
        {
            for (int i = 2; i < points.Length; ++i)
            {
                AddTriangle(material, points[0], points[i - 1], points[i]);
            }
        }

        public void AddPolygon(TMaterial material, params (TvP, TvM)[] points)
        {
            for (int i = 2; i < points.Length; ++i)
            {
                AddTriangle(material, points[0], points[i - 1], points[i]);
            }
        }

        public void AddPolygon(TMaterial material, params (TvP, TvJ)[] points)
        {
            for (int i = 2; i < points.Length; ++i)
            {
                AddTriangle(material, points[0], points[i - 1], points[i]);
            }
        }

        public void AddPolygon(TMaterial material, params TvP[] points)
        {
            for (int i = 2; i < points.Length; ++i)
            {
                AddTriangle(material, points[0], points[i - 1], points[i]);
            }
        }

        public void AddTriangle(TMaterial material, (TvP, TvM, TvJ) a, (TvP, TvM, TvJ) b, (TvP, TvM, TvJ) c)
        {
            if (!_Primitives.TryGetValue(material, out PrimitiveBuilder<TMaterial, TvP, TvM, TvJ> primitive))
            {
                primitive = new PrimitiveBuilder<TMaterial, TvP, TvM, TvJ>(this, material, StrictMode);
                _Primitives[material] = primitive;
            }

            primitive.AddTriangle(a, b, c);
        }

        public void AddTriangle(TMaterial material, (TvP, TvM) a, (TvP, TvM) b, (TvP, TvM) c)
        {
            AddTriangle(material, (a.Item1, a.Item2, default), (b.Item1, b.Item2, default), (c.Item1, c.Item2, default));
        }

        public void AddTriangle(TMaterial material, (TvP, TvJ) a, (TvP, TvJ) b, (TvP, TvJ) c)
        {
            AddTriangle(material, (a.Item1, default, a.Item2), (b.Item1, default, b.Item2), (c.Item1, default, c.Item2));
        }

        public void AddTriangle(TMaterial material, TvP a, TvP b, TvP c)
        {
            AddTriangle(material, (a, default, default), (b, default, default), (c, default, default));
        }

        public IEnumerable<(int, int, int)> GetTriangles(TMaterial material)
        {
            if (_Primitives.TryGetValue(material, out PrimitiveBuilder<TMaterial, TvP, TvM, TvJ> primitive)) return primitive.Triangles;

            return Enumerable.Empty<(int, int, int)>();
        }

        public IReadOnlyList<int> GetIndices(TMaterial material)
        {
            if (_Primitives.TryGetValue(material, out PrimitiveBuilder<TMaterial, TvP, TvM, TvJ> primitive)) return primitive.Indices;

            return new int[0];
        }

        public void Validate()
        {
            foreach (var p in _Primitives.Values)
            {
                p.Validate();
            }
        }

        #endregion
    }
}
