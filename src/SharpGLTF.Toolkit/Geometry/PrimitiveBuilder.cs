using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using System.Numerics;

namespace SharpGLTF.Geometry
{
    using Collections;
    using VertexTypes;

    public interface IPrimitive<TMaterial>
    {
        TMaterial Material { get; }

        int VertexCount { get; }

        TvPP GetVertexGeometry<TvPP>(int index)
            where TvPP : struct, IVertexGeometry;

        TvMM GetVertexMaterial<TvMM>(int index)
            where TvMM : struct, IVertexMaterial;

        TvSS GetVertexSkinning<TvSS>(int index)
            where TvSS : struct, IVertexSkinning;

        IReadOnlyList<int> Indices { get; }

        IEnumerable<(int, int, int)> Triangles { get; }
    }

    public interface IPrimitiveBuilder
    {
        void AddTriangle<TvPP, TvMM, TvSS>
            (
            Vertex<TvPP, TvMM, TvSS> a,
            Vertex<TvPP, TvMM, TvSS> b,
            Vertex<TvPP, TvMM, TvSS> c
            )
            where TvPP : struct, IVertexGeometry
            where TvMM : struct, IVertexMaterial
            where TvSS : struct, IVertexSkinning;
    }

    /// <summary>
    /// Represents an utility class to help build mesh primitives by adding triangles
    /// </summary>
    /// <typeparam name="TMaterial">The material type used by this <see cref="PrimitiveBuilder{TMaterial, TvP, TvM, TvS}"/> instance.</typeparam>
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
    /// <typeparam name="TvS">
    /// The vertex fragment type with Skin Joint Weights.
    /// Valid types are:
    /// <see cref="VertexEmpty"/>,
    /// <see cref="VertexJoints8x4"/>,
    /// <see cref="VertexJoints8x8"/>,
    /// <see cref="VertexJoints16x4"/>,
    /// <see cref="VertexJoints16x8"/>.
    /// </typeparam>
    [System.Diagnostics.DebuggerDisplay("Primitive {_Material}")]
    public class PrimitiveBuilder<TMaterial, TvP, TvM, TvS> : IPrimitiveBuilder, IPrimitive<TMaterial>
        where TvP : struct, IVertexGeometry
        where TvM : struct, IVertexMaterial
        where TvS : struct, IVertexSkinning
    {
        #region lifecycle

        internal PrimitiveBuilder(MeshBuilder<TMaterial, TvP, TvM, TvS> mesh, TMaterial material, bool strict)
        {
            this._Scrict = strict;
            this._Mesh = mesh;
            this._Material = material;
        }

        #endregion

        #region data

        private readonly bool _Scrict;

        private readonly MeshBuilder<TMaterial, TvP, TvM, TvS> _Mesh;

        private readonly TMaterial _Material;

        private readonly VertexList<Vertex<TvP, TvM, TvS>> _Vertices = new VertexList<Vertex<TvP, TvM, TvS>>();
        private readonly List<int> _Indices = new List<int>();

        #endregion

        #region properties

        public MeshBuilder<TMaterial, TvP, TvM, TvS> Mesh => _Mesh;

        public TMaterial Material => _Material;

        public int VertexCount => _Vertices.Count;

        public IReadOnlyList<Vertex<TvP, TvM, TvS>> Vertices => _Vertices;

        public IReadOnlyList<int> Indices => _Indices;

        public IEnumerable<(int, int, int)> Triangles => Schema2.PrimitiveType.TRIANGLES.GetTrianglesIndices(_Indices.Select(item => (uint)item));

        #endregion

        #region API

        /// <summary>
        /// Adds or reuses a vertex.
        /// </summary>
        /// <param name="vertex">
        /// A vertex formed by
        /// <typeparamref name="TvP"/>,
        /// <typeparamref name="TvM"/> and
        /// <typeparamref name="TvS"/> fragments.
        /// </param>
        /// <returns>The index of the vertex.</returns>
        public int UseVertex(Vertex<TvP, TvM, TvS> vertex)
        {
            if (_Scrict) vertex.Validate();

            return _Vertices.Use(vertex);
        }

        /// <summary>
        /// Adds a triangle.
        /// </summary>
        /// <param name="a">First corner of the triangle.</param>
        /// <param name="b">Second corner of the triangle.</param>
        /// <param name="c">Third corner of the triangle.</param>
        public void AddTriangle(Vertex<TvP, TvM, TvS> a, Vertex<TvP, TvM, TvS> b, Vertex<TvP, TvM, TvS> c)
        {
            var aa = UseVertex(a);
            var bb = UseVertex(b);
            var cc = UseVertex(c);

            // check for degenerated triangles:
            if (aa == bb || aa == cc || bb == cc)
            {
                if (_Scrict) throw new ArgumentException($"Invalid triangle indices {aa} {bb} {cc}");
                return;
            }

            // TODO: check if a triangle with indices aa-bb-cc already exists.

            _Indices.Add(aa);
            _Indices.Add(bb);
            _Indices.Add(cc);
        }

        /// <summary>
        /// Adds a polygon as a decomposed collection of triangles.
        /// Currently only convex polygons are supported.
        /// </summary>
        /// <param name="points">The corners of the polygon.</param>
        public void AddPolygon(params Vertex<TvP, TvM, TvS>[] points)
        {
            for (int i = 2; i < points.Length; ++i)
            {
                AddTriangle(points[0], points[i - 1], points[i]);
            }
        }

        public void AddPrimitive(PrimitiveBuilder<TMaterial, TvP, TvM, TvS> primitive, Matrix4x4 transform)
        {
            if (primitive == null) throw new ArgumentNullException(nameof(primitive));

            foreach (var t in primitive.Triangles)
            {
                var a = primitive.Vertices[t.Item1];
                var b = primitive.Vertices[t.Item2];
                var c = primitive.Vertices[t.Item3];

                var aa = a.Geometry; aa.Transform(transform); a.Geometry = aa;
                var bb = b.Geometry; bb.Transform(transform); b.Geometry = bb;
                var cc = c.Geometry; cc.Transform(transform); c.Geometry = cc;

                AddTriangle(a, b, c);
            }
        }

        public void Validate()
        {
            foreach (var v in _Vertices)
            {
                v.Validate();
            }
        }

        public void AddTriangle<TvPP, TvMM, TvSS>(Vertex<TvPP, TvMM, TvSS> a, Vertex<TvPP, TvMM, TvSS> b, Vertex<TvPP, TvMM, TvSS> c)
            where TvPP : struct, IVertexGeometry
            where TvMM : struct, IVertexMaterial
            where TvSS : struct, IVertexSkinning
        {
            var aa = a.CloneAs<TvP, TvM, TvS>();
            var bb = b.CloneAs<TvP, TvM, TvS>();
            var cc = c.CloneAs<TvP, TvM, TvS>();

            AddTriangle(aa, bb, cc);
        }

        public TvPP GetVertexGeometry<TvPP>(int index)
            where TvPP : struct, IVertexGeometry
        {
            return _Vertices[index].Geometry.CloneAs<TvPP>();
        }

        public TvMM GetVertexMaterial<TvMM>(int index)
            where TvMM : struct, IVertexMaterial
        {
            return _Vertices[index].Material.CloneAs<TvMM>();
        }

        public TvSS GetVertexSkinning<TvSS>(int index)
            where TvSS : struct, IVertexSkinning
        {
            return _Vertices[index].Skinning.CloneAs<TvSS>();
        }

        #endregion
    }
}
