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

        TvPP GetVertexPosition<TvPP>(int index)
            where TvPP : struct, IVertexPosition;

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
            (TvPP, TvMM, TvSS) a,
            (TvPP, TvMM, TvSS) b,
            (TvPP, TvMM, TvSS) c
            )
            where TvPP : struct, IVertexPosition
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
        where TvP : struct, IVertexPosition
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

        private readonly VertexList<(TvP, TvM, TvS)> _Vertices = new VertexList<(TvP, TvM, TvS)>();
        private readonly List<int> _Indices = new List<int>();

        #endregion

        #region properties

        public MeshBuilder<TMaterial, TvP, TvM, TvS> Mesh => _Mesh;

        public TMaterial Material => _Material;

        public int VertexCount => _Vertices.Count;

        public IReadOnlyList<(TvP, TvM, TvS)> Vertices => _Vertices;

        public IReadOnlyList<int> Indices => _Indices;

        public IEnumerable<(int, int, int)> Triangles
        {
            get
            {
                // TODO: use Schema2.PrimitiveType.TRIANGLES.GetTrianglesIndices()

                for (int i = 2; i < _Indices.Count; i += 3)
                {
                    yield return (_Indices[i - 2], _Indices[i - 1], _Indices[i]);
                }
            }
        }

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
        public int UseVertex((TvP, TvM, TvS) vertex)
        {
            if (_Scrict)
            {
                vertex.Item1.Validate();
                vertex.Item2.Validate();
                vertex.Item3.Validate();
            }

            return _Vertices.Use(vertex);
        }

        public void AddTriangle((TvP, TvM, TvS) a, (TvP, TvM, TvS) b, (TvP, TvM, TvS) c)
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

        public void AddTriangle((TvP, TvS) a, (TvP, TvS) b, (TvP, TvS) c)
        {
            AddTriangle((a.Item1, default, a.Item2), (b.Item1, default, b.Item2), (c.Item1, default, c.Item2));
        }

        public void AddTriangle(TvP a, TvP b, TvP c)
        {
            AddTriangle((a, default, default), (b, default, default), (c, default, default));
        }

        public void AddPolygon(params (TvP, TvM, TvS)[] points)
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

        public void AddPolygon(params (TvP, TvS)[] points)
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

        public void AddPrimitive(PrimitiveBuilder<TMaterial, TvP, TvM, TvS> primitive, Matrix4x4 transform)
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

        public void AddTriangle<TvPP, TvMM, TvSS>((TvPP, TvMM, TvSS) a, (TvPP, TvMM, TvSS) b, (TvPP, TvMM, TvSS) c)
            where TvPP : struct, IVertexPosition
            where TvMM : struct, IVertexMaterial
            where TvSS : struct, IVertexSkinning
        {
            var p1 = a.Item1.CloneAs<TvP>();
            var p2 = b.Item1.CloneAs<TvP>();
            var p3 = c.Item1.CloneAs<TvP>();

            var m1 = a.Item2.CloneAs<TvM>();
            var m2 = b.Item2.CloneAs<TvM>();
            var m3 = c.Item2.CloneAs<TvM>();

            var s1 = a.Item3.CloneAs<TvS>();
            var s2 = b.Item3.CloneAs<TvS>();
            var s3 = c.Item3.CloneAs<TvS>();

            AddTriangle((p1, m1, s1), (p2, m2, s2), (p3, m3, s3));
        }

        public TvPP GetVertexPosition<TvPP>(int index)
            where TvPP : struct, IVertexPosition
        {
            return _Vertices[index].Item1.CloneAs<TvPP>();
        }

        public TvMM GetVertexMaterial<TvMM>(int index)
            where TvMM : struct, IVertexMaterial
        {
            return _Vertices[index].Item2.CloneAs<TvMM>();
        }

        public TvSS GetVertexSkinning<TvSS>(int index)
            where TvSS : struct, IVertexSkinning
        {
            return _Vertices[index].Item3.CloneAs<TvSS>();
        }

        #endregion
    }
}
