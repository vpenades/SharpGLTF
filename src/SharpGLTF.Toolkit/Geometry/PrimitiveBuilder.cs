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

        int VerticesPerPrimitive { get; }

        int VertexCount { get; }

        IVertexBuilder GetVertex(int index);

        IReadOnlyList<int> Indices { get; }

        IReadOnlyList<IVertexBuilder> Vertices { get; }

        IEnumerable<int> Points { get; }

        IEnumerable<(int, int)> Lines { get; }

        IEnumerable<(int, int, int)> Triangles { get; }
    }

    public interface IPrimitiveBuilder
    {
        void AddTriangle(IVertexBuilder a, IVertexBuilder b, IVertexBuilder c);
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
    /// <see cref="VertexColor1Texture2"/>.
    /// <see cref="VertexColor2Texture2"/>.
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

        internal PrimitiveBuilder(MeshBuilder<TMaterial, TvG, TvM, TvS> mesh, TMaterial material, int primitiveVertexCount)
        {
            this._Mesh = mesh;
            this._Material = material;
            this._PrimitiveVertexCount = primitiveVertexCount;
        }

        #endregion

        #region data

        private readonly MeshBuilder<TMaterial, TvG, TvM, TvS> _Mesh;

        private readonly TMaterial _Material;

        private readonly int _PrimitiveVertexCount;

        class PrimitiveVertexList : VertexList<VertexBuilder<TvG, TvM, TvS>>, IReadOnlyList<IVertexBuilder>
        {
            #pragma warning disable SA1100 // Do not prefix calls with base unless local implementation exists
            IVertexBuilder IReadOnlyList<IVertexBuilder>.this[int index] => base[index];
            #pragma warning restore SA1100 // Do not prefix calls with base unless local implementation exists

            IEnumerator<IVertexBuilder> IEnumerable<IVertexBuilder>.GetEnumerator()
            {
                foreach (var item in this) yield return item;
            }
        }

        private readonly PrimitiveVertexList _Vertices = new PrimitiveVertexList();
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

        IReadOnlyList<IVertexBuilder> IPrimitive<TMaterial>.Vertices => _Vertices;

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
            if (_Mesh.VertexPreprocessor != null)
            {
                if (!_Mesh.VertexPreprocessor.PreprocessVertex(ref vertex)) return -1;
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

            if (_Mesh.VertexPreprocessor != null)
            {
                if (!_Mesh.VertexPreprocessor.PreprocessVertex(ref a)) return;
                if (!_Mesh.VertexPreprocessor.PreprocessVertex(ref b)) return;
            }

            var aa = _Vertices.Use(a);
            var bb = _Vertices.Use(b);

            // check for degenerated line
            if (aa == bb) return;

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
        public void AddTriangle(IVertexBuilder a, IVertexBuilder b, IVertexBuilder c)
        {
            Guard.NotNull(a, nameof(a));
            Guard.NotNull(b, nameof(b));
            Guard.NotNull(c, nameof(c));

            var expectedType = typeof(VertexBuilder<TvG, TvM, TvS>);

            var aa = a.GetType() != expectedType ? a.ConvertTo<TvG, TvM, TvS>() : (VertexBuilder<TvG, TvM, TvS>)a;
            var bb = b.GetType() != expectedType ? b.ConvertTo<TvG, TvM, TvS>() : (VertexBuilder<TvG, TvM, TvS>)b;
            var cc = c.GetType() != expectedType ? c.ConvertTo<TvG, TvM, TvS>() : (VertexBuilder<TvG, TvM, TvS>)c;

            AddTriangle(aa, bb, cc);
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

            if (_Mesh.VertexPreprocessor != null)
            {
                if (!_Mesh.VertexPreprocessor.PreprocessVertex(ref a)) return;
                if (!_Mesh.VertexPreprocessor.PreprocessVertex(ref b)) return;
                if (!_Mesh.VertexPreprocessor.PreprocessVertex(ref c)) return;
            }

            // check for degenerated triangle
            if (a.Equals(b) || a.Equals(c) || b.Equals(c)) return;

            var aa = _Vertices.Use(a);
            var bb = _Vertices.Use(b);
            var cc = _Vertices.Use(c);

            System.Diagnostics.Debug.Assert(aa != bb && aa != cc && bb != cc, "unexpected degenerated triangle");

            // TODO: check if a triangle with indices aa-bb-cc already exists, since there's no point in having the same polygon twice.

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

        IVertexBuilder IPrimitive<TMaterial>.GetVertex(int index) { return _Vertices[index]; }

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
