using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using System.Numerics;

namespace SharpGLTF.Geometry
{
    using Collections;
    using VertexTypes;

    /// <summary>
    /// Represents an utility class to help build mesh primitives by adding triangles
    /// </summary>
    /// <typeparam name="TMaterial">The material type used by this <see cref="PrimitiveBuilder{TMaterial, TvP, TvM, JvS}"/> instance.</typeparam>
    /// <typeparam name="TvP">
    /// The vertex fragment type with Position, Normal and Tangent.
    /// Valid types are:
    /// <see cref="VertexPosition"/>,
    /// <see cref="VertexPositionNormal"/>,
    /// <see cref="VertexPositionNormalTangent"/>.
    /// </typeparam>
    /// <typeparam name="TvM">
    /// The vertex fragment type with Colors and Texture Coordinates.
    /// Valid types are:
    /// <see cref="VertexEmpty"/>,
    /// <see cref="VertexColor1"/>,
    /// <see cref="VertexTexture1"/>,
    /// <see cref="VertexColor1Texture1"/>.
    /// </typeparam>
    /// <typeparam name="JvS">
    /// The vertex fragment type with Skin Joint Weights.
    /// Valid types are:
    /// <see cref="VertexEmpty"/>,
    /// <see cref="VertexJoints8x4"/>,
    /// <see cref="VertexJoints8x8"/>,
    /// <see cref="VertexJoints16x4"/>,
    /// <see cref="VertexJoints16x8"/>.
    /// </typeparam>
    public class PrimitiveBuilder<TMaterial, TvP, TvM, JvS>
        where TvP : struct, IVertexPosition
        where TvM : struct, IVertexMaterial
        where JvS : struct, IVertexSkinning
    {
        #region lifecycle

        internal PrimitiveBuilder(MeshBuilder<TMaterial, TvP, TvM, JvS> mesh, TMaterial material, bool strict)
        {
            this._Scrict = strict;
            this._Mesh = mesh;
            this._Material = material;
        }

        #endregion

        #region data

        private readonly bool _Scrict;

        private readonly MeshBuilder<TMaterial, TvP, TvM, JvS> _Mesh;

        private readonly TMaterial _Material;

        private readonly VertexList<(TvP, TvM, JvS)> _Vertices = new VertexList<(TvP, TvM, JvS)>();
        private readonly List<int> _Indices = new List<int>();

        #endregion

        #region properties

        public MeshBuilder<TMaterial, TvP, TvM, JvS> Mesh => _Mesh;

        public TMaterial Material => _Material;

        public IReadOnlyList<(TvP, TvM, JvS)> Vertices => _Vertices;

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

        public int UseVertex((TvP, TvM, JvS) vertex)
        {
            if (_Scrict)
            {
                vertex.Item1.Validate();
                vertex.Item2.Validate();
                vertex.Item3.Validate();
            }

            return _Vertices.Use(vertex);
        }

        public void AddTriangle((TvP, TvM, JvS) a, (TvP, TvM, JvS) b, (TvP, TvM, JvS) c)
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

        public void AddTriangle((TvP, TvM) a, (TvP, TvM) b, (TvP, TvM) c)
        {
            AddTriangle((a.Item1, a.Item2, default), (b.Item1, b.Item2, default), (c.Item1, c.Item2, default));
        }

        public void AddTriangle((TvP, JvS) a, (TvP, JvS) b, (TvP, JvS) c)
        {
            AddTriangle((a.Item1, default, a.Item2), (b.Item1, default, b.Item2), (c.Item1, default, c.Item2));
        }

        public void AddTriangle(TvP a, TvP b, TvP c)
        {
            AddTriangle((a, default, default), (b, default, default), (c, default, default));
        }

        public void AddPolygon(params (TvP, TvM, JvS)[] points)
        {
            for (int i = 2; i < points.Length; ++i)
            {
                AddTriangle(points[0], points[i - 1], points[i]);
            }
        }

        public void AddPolygon(params (TvP, TvM)[] points)
        {
            for (int i = 2; i < points.Length; ++i)
            {
                AddTriangle(points[0], points[i - 1], points[i]);
            }
        }

        public void AddPolygon(params (TvP, JvS)[] points)
        {
            for (int i = 2; i < points.Length; ++i)
            {
                AddTriangle(points[0], points[i - 1], points[i]);
            }
        }

        public void AddPolygon(params TvP[] points)
        {
            for (int i = 2; i < points.Length; ++i)
            {
                AddTriangle(points[0], points[i - 1], points[i]);
            }
        }

        public void AddPrimitive(PrimitiveBuilder<TMaterial, TvP, TvM, JvS> primitive, Matrix4x4 transform)
        {
            if (primitive == null) throw new ArgumentNullException(nameof(primitive));

            foreach (var t in primitive.Triangles)
            {
                var a = primitive.Vertices[t.Item1];
                var b = primitive.Vertices[t.Item2];
                var c = primitive.Vertices[t.Item3];

                var aa = a.Item1; aa.Transform(transform); a.Item1 = aa;
                var bb = b.Item1; bb.Transform(transform); b.Item1 = bb;
                var cc = c.Item1; cc.Transform(transform); c.Item1 = cc;

                AddTriangle(a, b, c);
            }
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

    /// <summary>
    /// Represents an utility class to help build meshes by adding primitives associated with a given material.
    /// </summary>
    /// <typeparam name="TMaterial">The material type used by this <see cref="PrimitiveBuilder{TMaterial, TvP, TvM, JvS}"/> instance.</typeparam>
    /// <typeparam name="TvP">
    /// The vertex fragment type with Position, Normal and Tangent.
    /// Valid types are:
    /// <see cref="VertexPosition"/>,
    /// <see cref="VertexPositionNormal"/>,
    /// <see cref="VertexPositionNormalTangent"/>.
    /// </typeparam>
    /// <typeparam name="TvM">
    /// The vertex fragment type with Colors and Texture Coordinates.
    /// Valid types are:
    /// <see cref="VertexEmpty"/>,
    /// <see cref="VertexColor1"/>,
    /// <see cref="VertexTexture1"/>,
    /// <see cref="VertexColor1Texture1"/>.
    /// </typeparam>
    /// <typeparam name="JvS">
    /// The vertex fragment type with Skin Joint Weights.
    /// Valid types are:
    /// <see cref="VertexEmpty"/>,
    /// <see cref="VertexJoints8x4"/>,
    /// <see cref="VertexJoints8x8"/>,
    /// <see cref="VertexJoints16x4"/>,
    /// <see cref="VertexJoints16x8"/>.
    /// </typeparam>
    public class MeshBuilder<TMaterial, TvP, TvM, JvS>
        where TvP : struct, IVertexPosition
        where TvM : struct, IVertexMaterial
        where JvS : struct, IVertexSkinning
    {
        #region lifecycle

        public MeshBuilder(string name = null)
        {
            this.Name = name;
        }

        #endregion

        #region data

        private readonly Dictionary<TMaterial, PrimitiveBuilder<TMaterial, TvP, TvM, JvS>> _Primitives = new Dictionary<TMaterial, PrimitiveBuilder<TMaterial, TvP, TvM, JvS>>();

        #endregion

        #region properties

        public Boolean StrictMode { get; set; }

        public string Name { get; set; }

        public IReadOnlyCollection<PrimitiveBuilder<TMaterial, TvP, TvM, JvS>> Primitives => _Primitives.Values;

        #endregion

        #region API

        public PrimitiveBuilder<TMaterial, TvP, TvM, JvS> UsePrimitive(TMaterial material)
        {
            if (!_Primitives.TryGetValue(material, out PrimitiveBuilder<TMaterial, TvP, TvM, JvS> primitive))
            {
                primitive = new PrimitiveBuilder<TMaterial, TvP, TvM, JvS>(this, material, StrictMode);
                _Primitives[material] = primitive;
            }

            return primitive;
        }

        public IEnumerable<(int, int, int)> GetTriangles(TMaterial material)
        {
            if (_Primitives.TryGetValue(material, out PrimitiveBuilder<TMaterial, TvP, TvM, JvS> primitive)) return primitive.Triangles;

            return Enumerable.Empty<(int, int, int)>();
        }

        public IReadOnlyList<int> GetIndices(TMaterial material)
        {
            if (_Primitives.TryGetValue(material, out PrimitiveBuilder<TMaterial, TvP, TvM, JvS> primitive)) return primitive.Indices;

            return new int[0];
        }

        public void AddMesh(MeshBuilder<TMaterial, TvP, TvM, JvS> mesh, Matrix4x4 transform)
        {
            foreach (var p in mesh.Primitives)
            {
                UsePrimitive(p.Material).AddPrimitive(p, transform);
            }
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
