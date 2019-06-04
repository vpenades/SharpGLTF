using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using System.Numerics;

using SharpGLTF.Collections;
using SharpGLTF.Geometry.VertexTypes;

namespace SharpGLTF.Geometry
{
    public interface IPrimitive<TMaterial>
    {
        TMaterial Material { get; }

        int VertexCount { get; }

        VertexBuilder<TvGG, TvMM, TvSS> GetVertex<TvGG, TvMM, TvSS>(int index)
            where TvGG : struct, IVertexGeometry
            where TvMM : struct, IVertexMaterial
            where TvSS : struct, IVertexSkinning;

        IReadOnlyList<int> Indices { get; }

        IEnumerable<int> Points { get; }

        IEnumerable<(int, int)> Lines { get; }

        IEnumerable<(int, int, int)> Triangles { get; }
    }

    public interface IPrimitiveBuilder
    {
        void AddTriangle<TvGG, TvMM, TvSS>
            (
            VertexBuilder<TvGG, TvMM, TvSS> a,
            VertexBuilder<TvGG, TvMM, TvSS> b,
            VertexBuilder<TvGG, TvMM, TvSS> c
            )
            where TvGG : struct, IVertexGeometry
            where TvMM : struct, IVertexMaterial
            where TvSS : struct, IVertexSkinning;
    }

    /// <summary>
    /// Represents an utility class to help build mesh primitives by adding triangles
    /// </summary>
    /// <typeparam name="TMaterial">The material type used by this <see cref="PrimitiveBuilder{TMaterial, TvP, TvM, TvS}"/> instance.</typeparam>
    /// <typeparam name="TvG">
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
    public class PrimitiveBuilder<TMaterial, TvG, TvM, TvS> : IPrimitiveBuilder, IPrimitive<TMaterial>
        where TvG : struct, IVertexGeometry
        where TvM : struct, IVertexMaterial
        where TvS : struct, IVertexSkinning
    {
        #region lifecycle

        internal PrimitiveBuilder(MeshBuilder<TMaterial, TvG, TvM, TvS> mesh, TMaterial material, int primitiveVertexCount, bool strict)
        {
            this._Scrict = strict;
            this._Mesh = mesh;
            this._Material = material;
            this._PrimitiveVertexCount = primitiveVertexCount;
        }

        #endregion

        #region data

        private readonly bool _Scrict;

        private readonly MeshBuilder<TMaterial, TvG, TvM, TvS> _Mesh;

        private readonly TMaterial _Material;

        private readonly int _PrimitiveVertexCount;

        private readonly VertexList<VertexBuilder<TvG, TvM, TvS>> _Vertices = new VertexList<VertexBuilder<TvG, TvM, TvS>>();
        private readonly List<int> _Indices = new List<int>();

        #endregion

        #region properties

        public MeshBuilder<TMaterial, TvG, TvM, TvS> Mesh => _Mesh;

        public TMaterial Material => _Material;

        /// <summary>
        /// Gets the number of vertices used by each primitive:
        /// 1 - Points
        /// 2 - Lines
        /// 3 - Triangles
        /// </summary>
        public int VerticesPerPrimitive => _PrimitiveVertexCount;

        public int VertexCount => Vertices.Count;

        public IReadOnlyList<VertexBuilder<TvG, TvM, TvS>> Vertices => _Vertices;

        public IReadOnlyList<int> Indices => _Indices;

        public IEnumerable<int> Points => _GetPointIndices();

        public IEnumerable<(int, int)> Lines => _GetLineIndices();

        public IEnumerable<(int, int, int)> Triangles => _GetTriangleIndices();

        #endregion

        #region API

        /// <summary>
        /// Adds or reuses a vertex.
        /// </summary>
        /// <param name="vertex">
        /// A vertex formed by
        /// <typeparamref name="TvG"/>,
        /// <typeparamref name="TvM"/> and
        /// <typeparamref name="TvS"/> fragments.
        /// </param>
        /// <returns>The index of the vertex.</returns>
        public int UseVertex(VertexBuilder<TvG, TvM, TvS> vertex)
        {
            if (!_Mesh._Preprocessor.PreprocessVertex(ref vertex))
            {
                Guard.IsFalse(_Scrict, nameof(vertex));
                return -1;
            }

            return _Vertices.Use(vertex);
        }

        /// <summary>
        /// Adds a point.
        /// </summary>
        /// <param name="a">vertex for this point.</param>
        public void AddPoint(VertexBuilder<TvG, TvM, TvS> a)
        {
            Guard.IsTrue(_PrimitiveVertexCount == 1, nameof(VerticesPerPrimitive), "Points are not supported for this primitive");

            UseVertex(a);
        }

        /// <summary>
        /// Adds a line.
        /// </summary>
        /// <param name="a">First corner of the line.</param>
        /// <param name="b">Second corner of the line.</param>
        public void AddLine(VertexBuilder<TvG, TvM, TvS> a, VertexBuilder<TvG, TvM, TvS> b)
        {
            Guard.IsTrue(_PrimitiveVertexCount == 2, nameof(VerticesPerPrimitive), "Lines are not supported for this primitive");

            var aa = UseVertex(a);
            var bb = UseVertex(b);

            // check for degenerated triangles:
            if (aa < 0 || bb < 0 || aa == bb)
            {
                if (_Scrict) throw new ArgumentException($"Invalid triangle indices {aa} {bb}");
                return;
            }

            // TODO: check if a triangle with indices aa-bb-cc already exists.

            _Indices.Add(aa);
            _Indices.Add(bb);
        }

        /// <summary>
        /// Adds a triangle.
        /// </summary>
        /// <param name="a">First corner of the triangle.</param>
        /// <param name="b">Second corner of the triangle.</param>
        /// <param name="c">Third corner of the triangle.</param>
        public void AddTriangle(VertexBuilder<TvG, TvM, TvS> a, VertexBuilder<TvG, TvM, TvS> b, VertexBuilder<TvG, TvM, TvS> c)
        {
            Guard.IsTrue(_PrimitiveVertexCount == 3, nameof(VerticesPerPrimitive), "Triangles are not supported for this primitive");

            var aa = UseVertex(a);
            var bb = UseVertex(b);
            var cc = UseVertex(c);

            // check for degenerated triangles:
            if (aa < 0 || bb < 0 || cc < 0 || aa == bb || aa == cc || bb == cc)
            {
                if (_Scrict) throw new ArgumentException($"Invalid triangle indices {aa} {bb} {cc}");
                return;
            }

            // TODO: check if a triangle with indices aa-bb-cc already exists.

            _Indices.Add(aa);
            _Indices.Add(bb);
            _Indices.Add(cc);
        }

        internal void AddPrimitive(PrimitiveBuilder<TMaterial, TvG, TvM, TvS> primitive, Func<VertexBuilder<TvG, TvM, TvS>, VertexBuilder<TvG, TvM, TvS>> vertexTransform)
        {
            if (primitive == null) throw new ArgumentNullException(nameof(primitive));

            if (_PrimitiveVertexCount == 1)
            {
                foreach (var p in primitive.Points)
                {
                    var a = vertexTransform(primitive.Vertices[p]);

                    AddPoint(a);
                }

                return;
            }

            if (_PrimitiveVertexCount == 2)
            {
                foreach (var l in primitive.Lines)
                {
                    var a = vertexTransform(primitive.Vertices[l.Item1]);
                    var b = vertexTransform(primitive.Vertices[l.Item2]);

                    AddLine(a, b);
                }

                return;
            }

            if (_PrimitiveVertexCount == 3)
            {
                foreach (var t in primitive.Triangles)
                {
                    var a = vertexTransform(primitive.Vertices[t.Item1]);
                    var b = vertexTransform(primitive.Vertices[t.Item2]);
                    var c = vertexTransform(primitive.Vertices[t.Item3]);

                    AddTriangle(a, b, c);
                }

                return;
            }
        }

        public void Validate()
        {
            foreach (var v in _Vertices)
            {
                v.Validate();
            }
        }

        public void AddTriangle<TvPP, TvMM, TvSS>(VertexBuilder<TvPP, TvMM, TvSS> a, VertexBuilder<TvPP, TvMM, TvSS> b, VertexBuilder<TvPP, TvMM, TvSS> c)
            where TvPP : struct, IVertexGeometry
            where TvMM : struct, IVertexMaterial
            where TvSS : struct, IVertexSkinning
        {
            var aa = a.ConvertTo<TvG, TvM, TvS>();
            var bb = b.ConvertTo<TvG, TvM, TvS>();
            var cc = c.ConvertTo<TvG, TvM, TvS>();

            AddTriangle(aa, bb, cc);
        }

        public VertexBuilder<TvPP, TvMM, TvSS> GetVertex<TvPP, TvMM, TvSS>(int index)
            where TvPP : struct, IVertexGeometry
            where TvMM : struct, IVertexMaterial
            where TvSS : struct, IVertexSkinning
        {
            var v = _Vertices[index];

            return new VertexBuilder<TvPP, TvMM, TvSS>(v.Geometry.ConvertTo<TvPP>(), v.Material.ConvertTo<TvMM>(), v.Skinning.ConvertTo<TvSS>());
        }

        private IEnumerable<int> _GetPointIndices()
        {
            if (_PrimitiveVertexCount != 1) return Enumerable.Empty<int>();

            return Enumerable.Range(0, _Vertices.Count);
        }

        private IEnumerable<(int, int)> _GetLineIndices()
        {
            if (_PrimitiveVertexCount != 2) return Enumerable.Empty<(int, int)>();

            return Schema2.PrimitiveType.LINES.GetLinesIndices(_Indices.Select(item => (uint)item));
        }

        private IEnumerable<(int, int, int)> _GetTriangleIndices()
        {
            if (_PrimitiveVertexCount != 3) return Enumerable.Empty<(int, int, int)>();

            return Schema2.PrimitiveType.TRIANGLES.GetTrianglesIndices(_Indices.Select(item => (uint)item));
        }

        public void TransformVertices(Func<VertexBuilder<TvG, TvM, TvS>, VertexBuilder<TvG, TvM, TvS>> transformFunc)
        {
            _Vertices.TransformVertices(transformFunc);
        }

        #endregion
    }
}
