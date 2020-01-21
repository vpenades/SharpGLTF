using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using System.Numerics;

using SharpGLTF.Collections;
using SharpGLTF.Geometry.VertexTypes;

namespace SharpGLTF.Geometry
{
    public interface IPrimitiveReader<TMaterial>
    {
        /// <summary>
        /// Gets a generic type of <see cref="VertexBuilder{TvG, TvM, TvS}"/>.
        /// </summary>
        Type VertexType { get; }

        /// <summary>
        /// Gets the current <typeparamref name="TMaterial"/> instance used by this primitive.
        /// </summary>
        TMaterial Material { get; }

        /// <summary>
        /// Gets the number of vertices used by each primitive shape.
        /// </summary>
        int VerticesPerPrimitive { get; }

        /// <summary>
        /// Gets the list of <see cref="IVertexBuilder"/> vertices.
        /// </summary>
        IReadOnlyList<IVertexBuilder> Vertices { get; }

        /// <summary>
        /// Gets the list of <see cref="IPrimitiveMorphTargetReader"/>.
        /// </summary>
        IReadOnlyList<IPrimitiveMorphTargetReader> MorphTargets { get; }

        /// <summary>
        /// Gets the indices of all points, given that <see cref="VerticesPerPrimitive"/> is 1.
        /// </summary>
        IReadOnlyList<int> Points { get; }

        /// <summary>
        /// Gets the indices of all lines, given that <see cref="VerticesPerPrimitive"/> is 2.
        /// </summary>
        IReadOnlyList<(int A, int B)> Lines { get; }

        /// <summary>
        /// Gets the indices of all the surfaces as triangles, given that <see cref="VerticesPerPrimitive"/> is 3.
        /// </summary>
        IReadOnlyList<(int A, int B, int C)> Triangles { get; }

        /// <summary>
        /// Gets the indices of all the surfaces, given that <see cref="VerticesPerPrimitive"/> is 3.
        /// </summary>
        IReadOnlyList<(int A, int B, int C, int? D)> Surfaces { get; }

        /// <summary>
        /// Calculates the raw list of indices to use for this primitive.
        /// </summary>
        /// <returns>a list of indices.</returns>
        IReadOnlyList<int> GetIndices();
    }

    public interface IPrimitiveBuilder
    {
        /// <summary>
        /// Gets a generic type of <see cref="VertexBuilder{TvG, TvM, TvS}"/>.
        /// </summary>
        Type VertexType { get; }

        void SetVertexDelta(int morphTargetIndex, int vertexIndex, VertexGeometryDelta delta);

        int AddPoint(IVertexBuilder a);

        (int A, int B) AddLine(IVertexBuilder a, IVertexBuilder b);

        (int A, int B, int C) AddTriangle(IVertexBuilder a, IVertexBuilder b, IVertexBuilder c);

        (int A, int B, int C, int D) AddQuadrangle(IVertexBuilder a, IVertexBuilder b, IVertexBuilder c, IVertexBuilder d);
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
    /// <see cref="VertexJoints4"/>,
    /// <see cref="VertexJoints8"/>.
    /// </typeparam>
    public abstract class PrimitiveBuilder<TMaterial, TvG, TvM, TvS> : IPrimitiveBuilder, IPrimitiveReader<TMaterial>
        where TvG : struct, IVertexGeometry
        where TvM : struct, IVertexMaterial
        where TvS : struct, IVertexSkinning
    {
        #region lifecycle

        internal PrimitiveBuilder(MeshBuilder<TMaterial, TvG, TvM, TvS> mesh, TMaterial material)
        {
            this._Mesh = mesh;
            this._Material = material;
        }

        protected PrimitiveBuilder(MeshBuilder<TMaterial, TvG, TvM, TvS> mesh, PrimitiveBuilder<TMaterial, TvG, TvM, TvS> other, TMaterial material)
        {
            Guard.NotNull(mesh, nameof(mesh));
            Guard.NotNull(other, nameof(other));

            this._Mesh = mesh;
            this._Material = material != null ? material : other.Material;
            other._Vertices.CopyTo(this._Vertices);

            foreach (var otherMT in other._MorphTargets)
            {
                var thisMT = new PrimitiveMorphTargetBuilder<TvG>(idx => this._Vertices[idx].Geometry, otherMT);
                this._MorphTargets.Add(otherMT);
            }
        }

        internal abstract PrimitiveBuilder<TMaterial, TvG, TvM, TvS> Clone(MeshBuilder<TMaterial, TvG, TvM, TvS> mesh, TMaterial material);

        #endregion

        #region data

        private readonly MeshBuilder<TMaterial, TvG, TvM, TvS> _Mesh;

        private readonly TMaterial _Material;

        private readonly VertexListWrapper _Vertices = new VertexListWrapper();

        private readonly List<PrimitiveMorphTargetBuilder<TvG>> _MorphTargets = new List<PrimitiveMorphTargetBuilder<TvG>>();

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
        public abstract int VerticesPerPrimitive { get; }

        public Type VertexType => typeof(VertexBuilder<TvG, TvM, TvS>);

        public IReadOnlyList<VertexBuilder<TvG, TvM, TvS>> Vertices => _Vertices;

        IReadOnlyList<IVertexBuilder> IPrimitiveReader<TMaterial>.Vertices => _Vertices;

        IReadOnlyList<IPrimitiveMorphTargetReader> IPrimitiveReader<TMaterial>.MorphTargets => _MorphTargets;

        public virtual IReadOnlyList<int> Points => Array.Empty<int>();

        public virtual IReadOnlyList<(int A, int B)> Lines => Array.Empty<(int, int)>();

        public virtual IReadOnlyList<(int A, int B, int C)> Triangles => Array.Empty<(int, int, int)>();

        public virtual IReadOnlyList<(int A, int B, int C, int? D)> Surfaces => Array.Empty<(int, int, int, int?)>();

        #endregion

        #region API - morph targets

        internal PrimitiveMorphTargetBuilder<TvG> _UseMorphTarget(int morphTargetIndex)
        {
            while (this._MorphTargets.Count <= morphTargetIndex) this._MorphTargets.Add(new PrimitiveMorphTargetBuilder<TvG>(idx => _Vertices[idx].Geometry));

            return this._MorphTargets[morphTargetIndex];
        }

        #endregion

        #region API

        /// <summary>
        /// Checks if <paramref name="vertex"/> is a compatible vertex and casts it, or converts it if it is not.
        /// </summary>
        /// <param name="vertex">Any vertex</param>
        /// <returns>A vertex compatible with this primitive.</returns>
        private static VertexBuilder<TvG, TvM, TvS> ConvertVertex(IVertexBuilder vertex)
        {
            Guard.NotNull(vertex, nameof(vertex));

            var vv = vertex.ConvertTo<TvG, TvM, TvS>();
            System.Diagnostics.Debug.Assert(vv.Position == vertex.GetGeometry().GetPosition());

            return vv;
        }

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
        protected int UseVertex(VertexBuilder<TvG, TvM, TvS> vertex)
        {
            return _Vertices.Use(vertex);
        }

        void IPrimitiveBuilder.SetVertexDelta(int morphTargetIndex, int vertexIndex, VertexGeometryDelta delta)
        {
            _UseMorphTarget(morphTargetIndex).SetVertexDelta(vertexIndex, delta);
        }

        /// <summary>
        /// Adds a point.
        /// </summary>
        /// <param name="a">vertex for this point.</param>
        /// <returns>The indices of the vertices.</returns>
        public int AddPoint(IVertexBuilder a)
        {
            Guard.NotNull(a, nameof(a));

            return AddPoint(ConvertVertex(a));
        }

        /// <summary>
        /// Adds a line.
        /// </summary>
        /// <param name="a">First corner of the line.</param>
        /// <param name="b">Second corner of the line.</param>
        /// <returns>The indices of the vertices, or, in case the line is degenerated, (-1,-1).</returns>
        public (int A, int B) AddLine(IVertexBuilder a, IVertexBuilder b)
        {
            Guard.NotNull(a, nameof(a));
            Guard.NotNull(b, nameof(b));

            return AddLine(ConvertVertex(a), ConvertVertex(b));
        }

        /// <summary>
        /// Adds a triangle.
        /// </summary>
        /// <param name="a">First corner of the triangle.</param>
        /// <param name="b">Second corner of the triangle.</param>
        /// <param name="c">Third corner of the triangle.</param>
        /// <returns>The indices of the vertices, or, in case the triangle is degenerated, (-1,-1,-1).</returns>
        public (int A, int B, int C) AddTriangle(IVertexBuilder a, IVertexBuilder b, IVertexBuilder c)
        {
            Guard.NotNull(a, nameof(a));
            Guard.NotNull(b, nameof(b));
            Guard.NotNull(c, nameof(c));

            return AddTriangle(ConvertVertex(a), ConvertVertex(b), ConvertVertex(c));
        }

        /// <summary>
        /// Adds a quadrangle.
        /// </summary>
        /// <param name="a">First corner of the quadrangle.</param>
        /// <param name="b">Second corner of the quadrangle.</param>
        /// <param name="c">Third corner of the quadrangle.</param>
        /// <param name="d">Fourth corner of the quadrangle.</param>
        /// <returns>The indices of the vertices, or, in case the quadrangle is degenerated, (-1,-1,-1,-1).</returns>
        /// <remarks>
        /// If only one of the vertices is degenerated, leading to a single triangle, the resulting indices would
        /// have just one negative index, like this: (16,-1,17,18)
        /// </remarks>
        public (int A, int B, int C, int D) AddQuadrangle(IVertexBuilder a, IVertexBuilder b, IVertexBuilder c, IVertexBuilder d)
        {
            Guard.NotNull(a, nameof(a));
            Guard.NotNull(b, nameof(b));
            Guard.NotNull(c, nameof(c));
            Guard.NotNull(d, nameof(d));

            return AddQuadrangle(ConvertVertex(a), ConvertVertex(b), ConvertVertex(c), ConvertVertex(d));
        }

        public void Validate()
        {
            foreach (var v in _Vertices)
            {
                v.Validate();
            }
        }

        public void TransformVertices(Func<VertexBuilder<TvG, TvM, TvS>, VertexBuilder<TvG, TvM, TvS>> vertexTransformFunc)
        {
            _Vertices.TransformVertices(vertexTransformFunc);

            TvG geoFunc(TvG g) => vertexTransformFunc(new VertexBuilder<TvG, TvM, TvS>(g, default, default(TvS))).Geometry;

            foreach (var mt in _MorphTargets) mt.TransformVertices(geoFunc);
        }

        internal void AddPrimitive(PrimitiveBuilder<TMaterial, TvG, TvM, TvS> primitive, Func<VertexBuilder<TvG, TvM, TvS>, VertexBuilder<TvG, TvM, TvS>> vertexTransformFunc)
        {
            if (primitive == null) return;

            if (vertexTransformFunc == null) vertexTransformFunc = v => v;

            // vertex-vertex map so we can know where to set the morph targets.
            var vmap = new Dictionary<int, int>();

            if (this.VerticesPerPrimitive == 1)
            {
                foreach (var src in primitive.Points)
                {
                    var vrt = vertexTransformFunc(primitive.Vertices[src]);

                    var idx = AddPoint(vrt);

                    vmap[src] = idx;
                }
            }

            if (this.VerticesPerPrimitive == 2)
            {
                foreach (var (srcA, srcB) in primitive.Lines)
                {
                    var vrtA = vertexTransformFunc(primitive.Vertices[srcA]);
                    var vrtB = vertexTransformFunc(primitive.Vertices[srcB]);

                    var (dstA, dstB) = AddLine(vrtA, vrtB);

                    vmap[srcA] = dstA;
                    vmap[srcB] = dstB;
                }
            }

            if (this.VerticesPerPrimitive == 3)
            {
                foreach (var (srcA, srcB, srcC, srcD) in primitive.Surfaces)
                {
                    var vrtA = vertexTransformFunc(primitive.Vertices[srcA]);
                    var vrtB = vertexTransformFunc(primitive.Vertices[srcB]);
                    var vrtC = vertexTransformFunc(primitive.Vertices[srcC]);

                    if (srcD.HasValue)
                    {
                        var vrtD = vertexTransformFunc(primitive.Vertices[srcD.Value]);
                        var (dstA, dstB, dstC, dstD) = AddQuadrangle(vrtA, vrtB, vrtC, vrtD);

                        vmap[srcA] = dstA;
                        vmap[srcB] = dstB;
                        vmap[srcC] = dstC;
                        vmap[srcD.Value] = dstD;
                    }
                    else
                    {
                        var (dstA, dstB, dstC) = AddTriangle(vrtA, vrtB, vrtC);

                        vmap[srcA] = dstA;
                        vmap[srcB] = dstB;
                        vmap[srcC] = dstC;
                    }
                }
            }

            TvG geoFunc(TvG g) => vertexTransformFunc(new VertexBuilder<TvG, TvM, TvS>(g, default, default(TvS))).Geometry;

            for (int i = 0; i < primitive._MorphTargets.Count; ++i)
            {
                _UseMorphTarget(i).SetMorphTargets(primitive._MorphTargets[i], vmap, geoFunc);
            }
        }

        #endregion

        #region abstract API

        public abstract IReadOnlyList<int> GetIndices();

        /// <summary>
        /// Adds a point.
        /// </summary>
        /// <param name="a">vertex for this point.</param>
        /// <returns>The index of the vertex.</returns>
        public virtual int AddPoint(VertexBuilder<TvG, TvM, TvS> a)
        {
            throw new NotSupportedException("Points are not supported for this primitive");
        }

        /// <summary>
        /// Adds a line.
        /// </summary>
        /// <param name="a">First corner of the line.</param>
        /// <param name="b">Second corner of the line.</param>
        /// <returns>The indices of the vertices, or, in case the line is degenerated, (-1,-1).</returns>
        public virtual (int A, int B) AddLine(VertexBuilder<TvG, TvM, TvS> a, VertexBuilder<TvG, TvM, TvS> b)
        {
            throw new NotSupportedException("Lines are not supported for this primitive");
        }

        /// <summary>
        /// Adds a triangle.
        /// </summary>
        /// <param name="a">First corner of the triangle.</param>
        /// <param name="b">Second corner of the triangle.</param>
        /// <param name="c">Third corner of the triangle.</param>
        /// <returns>The indices of the vertices, or, in case the triangle is degenerated, (-1,-1,-1).</returns>
        public virtual (int A, int B, int C) AddTriangle(VertexBuilder<TvG, TvM, TvS> a, VertexBuilder<TvG, TvM, TvS> b, VertexBuilder<TvG, TvM, TvS> c)
        {
            throw new NotSupportedException("Triangles are not supported for this primitive");
        }

        /// <summary>
        /// Adds a quadrangle.
        /// </summary>
        /// <param name="a">First corner of the quadrangle.</param>
        /// <param name="b">Second corner of the quadrangle.</param>
        /// <param name="c">Third corner of the quadrangle.</param>
        /// <param name="d">Fourth corner of the quadrangle.</param>
        /// <returns>The indices of the vertices, or, in case the quadrangle is degenerated, (-1,-1,-1,-1).</returns>
        public virtual (int A, int B, int C, int D) AddQuadrangle(VertexBuilder<TvG, TvM, TvS> a, VertexBuilder<TvG, TvM, TvS> b, VertexBuilder<TvG, TvM, TvS> c, VertexBuilder<TvG, TvM, TvS> d)
        {
            throw new NotSupportedException("Quadrangles are not supported for this primitive");
        }

        #endregion

        #region helper types

        private sealed class VertexListWrapper : VertexList<VertexBuilder<TvG, TvM, TvS>>, IReadOnlyList<IVertexBuilder>
        {
            #pragma warning disable SA1100 // Do not prefix calls with base unless local implementation exists
            IVertexBuilder IReadOnlyList<IVertexBuilder>.this[int index] => base[index];
            #pragma warning restore SA1100 // Do not prefix calls with base unless local implementation exists

            IEnumerator<IVertexBuilder> IEnumerable<IVertexBuilder>.GetEnumerator()
            {
                foreach (var item in this) yield return item;
            }
        }

        #endregion
    }

    [System.Diagnostics.DebuggerDisplay("Points[{Points.Count}] {_Material}")]
    sealed class PointsPrimitiveBuilder<TMaterial, TvG, TvM, TvS> : PrimitiveBuilder<TMaterial, TvG, TvM, TvS>
        where TvG : struct, IVertexGeometry
        where TvM : struct, IVertexMaterial
        where TvS : struct, IVertexSkinning
    {
        #region lifecycle

        internal PointsPrimitiveBuilder(MeshBuilder<TMaterial, TvG, TvM, TvS> mesh, TMaterial material)
            : base(mesh, material)
        {
        }

        internal override PrimitiveBuilder<TMaterial, TvG, TvM, TvS> Clone(MeshBuilder<TMaterial, TvG, TvM, TvS> mesh, TMaterial material)
        {
            return new PointsPrimitiveBuilder<TMaterial, TvG, TvM, TvS>(mesh, this, material);
        }

        private PointsPrimitiveBuilder(MeshBuilder<TMaterial, TvG, TvM, TvS> mesh, PointsPrimitiveBuilder<TMaterial, TvG, TvM, TvS> other, TMaterial material)
            : base(mesh, other, material)
        {
        }

        #endregion

        #region properties

        public override int VerticesPerPrimitive => 1;

        public override IReadOnlyList<int> Points => new PointListWrapper<VertexBuilder<TvG, TvM, TvS>>(this.Vertices);

        #endregion

        #region API

        public override int AddPoint(VertexBuilder<TvG, TvM, TvS> a)
        {
            if (Mesh.VertexPreprocessor != null)
            {
                if (!Mesh.VertexPreprocessor.PreprocessVertex(ref a)) return -1;
            }

            return UseVertex(a);
        }

        public override IReadOnlyList<int> GetIndices() { return Array.Empty<int>(); }

        #endregion

        #region types

        struct PointListWrapper<T> : IReadOnlyList<int>
        {
            public PointListWrapper(IReadOnlyList<T> vertices)
            {
                _Vertices = vertices;
            }

            private readonly IReadOnlyList<T> _Vertices;

            public int this[int index] => index;

            public int Count => _Vertices.Count;

            public IEnumerator<int> GetEnumerator() { return Enumerable.Range(0, _Vertices.Count).GetEnumerator(); }

            IEnumerator IEnumerable.GetEnumerator() { return Enumerable.Range(0, _Vertices.Count).GetEnumerator(); }
        }

        #endregion
    }

    [System.Diagnostics.DebuggerDisplay("Lines[{Lines.Count}] {_Material}")]
    sealed class LinesPrimitiveBuilder<TMaterial, TvG, TvM, TvS> : PrimitiveBuilder<TMaterial, TvG, TvM, TvS>
        where TvG : struct, IVertexGeometry
        where TvM : struct, IVertexMaterial
        where TvS : struct, IVertexSkinning
    {
        #region lifecycle

        internal LinesPrimitiveBuilder(MeshBuilder<TMaterial, TvG, TvM, TvS> mesh, TMaterial material)
            : base(mesh, material)
        {
        }

        internal override PrimitiveBuilder<TMaterial, TvG, TvM, TvS> Clone(MeshBuilder<TMaterial, TvG, TvM, TvS> mesh, TMaterial material)
        {
            return new LinesPrimitiveBuilder<TMaterial, TvG, TvM, TvS>(mesh, this, material);
        }

        private LinesPrimitiveBuilder(MeshBuilder<TMaterial, TvG, TvM, TvS> mesh, LinesPrimitiveBuilder<TMaterial, TvG, TvM, TvS> other, TMaterial material)
            : base(mesh, other, material)
        {
            this._Indices.AddRange(other._Indices);
        }

        #endregion

        #region data

        private readonly List<(int A, int B)> _Indices = new List<(int A, int B)>();

        #endregion

        #region properties

        public override int VerticesPerPrimitive => 2;

        public override IReadOnlyList<(int A, int B)> Lines => _Indices;

        #endregion

        #region API

        public override (int A, int B) AddLine(VertexBuilder<TvG, TvM, TvS> a, VertexBuilder<TvG, TvM, TvS> b)
        {
            if (Mesh.VertexPreprocessor != null)
            {
                if (!Mesh.VertexPreprocessor.PreprocessVertex(ref a)) return (-1, -1);
                if (!Mesh.VertexPreprocessor.PreprocessVertex(ref b)) return (-1, -1);
            }

            // check for degenerated line
            if (a.Position == b.Position) return (-1, -1);

            var aa = UseVertex(a);
            var bb = UseVertex(b);

            System.Diagnostics.Debug.Assert(aa != bb, "unexpected degenerated line");

            _Indices.Add((aa, bb));

            return (aa, bb);
        }

        public override IReadOnlyList<int> GetIndices()
        {
            return _Indices
                .SelectMany(item => new[] { item.A, item.B })
                .ToList();
        }

        #endregion
    }

    [System.Diagnostics.DebuggerDisplay("Triangles[{Triangles.Count}] {_Material}")]
    sealed class TrianglesPrimitiveBuilder<TMaterial, TvG, TvM, TvS> : PrimitiveBuilder<TMaterial, TvG, TvM, TvS>
        where TvG : struct, IVertexGeometry
        where TvM : struct, IVertexMaterial
        where TvS : struct, IVertexSkinning
    {
        #region lifecycle

        internal TrianglesPrimitiveBuilder(MeshBuilder<TMaterial, TvG, TvM, TvS> mesh, TMaterial material)
            : base(mesh, material)
        {
        }

        internal override PrimitiveBuilder<TMaterial, TvG, TvM, TvS> Clone(MeshBuilder<TMaterial, TvG, TvM, TvS> mesh, TMaterial material)
        {
            return new TrianglesPrimitiveBuilder<TMaterial, TvG, TvM, TvS>(mesh, this, material);
        }

        private TrianglesPrimitiveBuilder(MeshBuilder<TMaterial, TvG, TvM, TvS> mesh, TrianglesPrimitiveBuilder<TMaterial, TvG, TvM, TvS> other, TMaterial material)
            : base(mesh, other, material)
        {
            this._TriIndices.AddRange(other._TriIndices);
            this._QuadIndices.AddRange(other._QuadIndices);
        }

        #endregion

        #region data

        private readonly List<(int A, int B, int C)> _TriIndices = new List<(int A, int B, int C)>();
        private readonly List<(int A, int B, int C, int D)> _QuadIndices = new List<(int A, int B, int C, int D)>();

        #endregion

        #region properties

        public override int VerticesPerPrimitive => 3;

        public override IReadOnlyList<(int A, int B, int C)> Triangles => new TriangleList(_TriIndices, _QuadIndices);

        public override IReadOnlyList<(int A, int B, int C, int? D)> Surfaces => new SurfaceList(_TriIndices, _QuadIndices);

        #endregion

        #region API

        public override (int A, int B, int C) AddTriangle(VertexBuilder<TvG, TvM, TvS> a, VertexBuilder<TvG, TvM, TvS> b, VertexBuilder<TvG, TvM, TvS> c)
        {
            if (Mesh.VertexPreprocessor != null)
            {
                if (!Mesh.VertexPreprocessor.PreprocessVertex(ref a)) return (-1, -1, -1);
                if (!Mesh.VertexPreprocessor.PreprocessVertex(ref b)) return (-1, -1, -1);
                if (!Mesh.VertexPreprocessor.PreprocessVertex(ref c)) return (-1, -1, -1);
            }

            return _AddTriangle(a, b, c);
        }

        public override (int A, int B, int C, int D) AddQuadrangle(VertexBuilder<TvG, TvM, TvS> a, VertexBuilder<TvG, TvM, TvS> b, VertexBuilder<TvG, TvM, TvS> c, VertexBuilder<TvG, TvM, TvS> d)
        {
            if (Mesh.VertexPreprocessor != null)
            {
                if (!Mesh.VertexPreprocessor.PreprocessVertex(ref a)) return (-1, -1, -1, -1);
                if (!Mesh.VertexPreprocessor.PreprocessVertex(ref b)) return (-1, -1, -1, -1);
                if (!Mesh.VertexPreprocessor.PreprocessVertex(ref c)) return (-1, -1, -1, -1);
                if (!Mesh.VertexPreprocessor.PreprocessVertex(ref d)) return (-1, -1, -1, -1);
            }

            // if diagonals are degenerated, the whole quad is degenerated.
            if (a.Position == c.Position || b.Position == d.Position) return (-1, -1, -1, -1);

            // A-B degenerated
            if (a.Position == b.Position)
            {
                var tri = _AddTriangle(b, c, d);
                return (-1, tri.A, tri.B, tri.C);
            }

            // B-C degenerated
            if (b.Position == c.Position)
            {
                var tri = _AddTriangle(a, c, d);
                return (tri.A, -1, tri.B, tri.C);
            }

            // C-D degenerated
            if (c.Position == d.Position)
            {
                var tri = _AddTriangle(a, b, d);
                return (tri.A, tri.B, -1, tri.C);
            }

            // D-A degenerated
            if (d.Position == a.Position)
            {
                var tri = _AddTriangle(a, b, c);
                return (tri.A, tri.B, tri.C, -1);
            }

            // at some points it could be interesting to comply with https://github.com/KhronosGroup/glTF/pull/1620

            var aa = UseVertex(a);
            var bb = UseVertex(b);
            var cc = UseVertex(c);
            var dd = UseVertex(d);

            System.Diagnostics.Debug.Assert(aa != bb && aa != cc && bb != cc && cc != dd, "unexpected degenerated triangle");

            var oddEven = MeshBuilderToolkit.GetQuadrangleDiagonal(a.Position, b.Position, c.Position, d.Position);

            if (oddEven)
            {
                _QuadIndices.Add((aa, bb, cc, dd));
                return (aa, bb, cc, dd);
            }
            else
            {
                _QuadIndices.Add((bb, cc, dd, aa));
                return (aa, bb, cc, dd); // notice that we return the indices in the same order we got them.
            }
        }

        private (int A, int B, int C) _AddTriangle(VertexBuilder<TvG, TvM, TvS> a, VertexBuilder<TvG, TvM, TvS> b, VertexBuilder<TvG, TvM, TvS> c)
        {
            // check for degenerated triangle
            if (a.Position == b.Position || a.Position == c.Position || b.Position == c.Position) return (-1, -1, -1);

            var aa = UseVertex(a);
            var bb = UseVertex(b);
            var cc = UseVertex(c);

            System.Diagnostics.Debug.Assert(aa != bb && aa != cc && bb != cc, "unexpected degenerated triangle");

            // TODO: check if a triangle with indices aa-bb-cc, bb-cc-aa or cc-aa-bb already exists, since there's no point in having the same polygon twice.

            _TriIndices.Add((aa, bb, cc));

            return (aa, bb, cc);
        }

        public override IReadOnlyList<int> GetIndices()
        {
            return Triangles
                .SelectMany(item => new[] { item.A, item.B, item.C })
                .ToList();
        }

        #endregion

        #region Types

        private struct TriangleList : IReadOnlyList<(int A, int B, int C)>
        {
            public TriangleList(IReadOnlyList<(int, int, int)> tris, IReadOnlyList<(int, int, int, int)> quads)
            {
                _Tris = tris;
                _Quads = quads;
            }

            private readonly IReadOnlyList<(int A, int B, int C)> _Tris;
            private readonly IReadOnlyList<(int A, int B, int C, int D)> _Quads;

            public int Count => _Tris.Count + (_Quads.Count * 2);

            public (int A, int B, int C) this[int index]
            {
                get
                {
                    if (index < _Tris.Count)
                    {
                        var tri = _Tris[index];
                        return tri;
                    }

                    index -= _Tris.Count;

                    var quad = _Quads[index / 2];

                    return (index & 1) == 0 ? (quad.A, quad.B, quad.C) : (quad.A, quad.C, quad.D);
                }
            }

            public IEnumerator<(int A, int B, int C)> GetEnumerator()
            {
                var c = this.Count;
                for (int i = 0; i < c; ++i) yield return this[i];
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                var c = this.Count;
                for (int i = 0; i < c; ++i) yield return this[i];
            }
        }

        private struct SurfaceList : IReadOnlyList<(int A, int B, int C, int? D)>
        {
            public SurfaceList(IReadOnlyList<(int, int, int)> tris, IReadOnlyList<(int, int, int, int)> quads)
            {
                _Tris = tris;
                _Quads = quads;
            }

            private readonly IReadOnlyList<(int A, int B, int C)> _Tris;
            private readonly IReadOnlyList<(int A, int B, int C, int D)> _Quads;

            public int Count => _Tris.Count + _Quads.Count;

            public (int A, int B, int C, int? D) this[int index]
            {
                get
                {
                    if (index < _Tris.Count)
                    {
                        var tri = _Tris[index];
                        return (tri.A, tri.B, tri.C, null);
                    }

                    index -= _Tris.Count;

                    var quad = _Quads[index];
                    return (quad.A, quad.B, quad.C, quad.D);
                }
            }

            public IEnumerator<(int A, int B, int C, int? D)> GetEnumerator()
            {
                var c = this.Count;
                for (int i = 0; i < c; ++i) yield return this[i];
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                var c = this.Count;
                for (int i = 0; i < c; ++i) yield return this[i];
            }
        }

        #endregion
    }

    /// <summary>
    /// Helper class used to calculate Normals and Tangents of missing meshes.
    /// </summary>
    /// <typeparam name="TMaterial">default material</typeparam>
    class MeshPrimitiveNormalsAndTangents<TMaterial> : VertexNormalsFactory.IMeshPrimitive, VertexTangentsFactory.IMeshPrimitive
    {
        #region constructor

        // TODO: we need a solution for morph targets.

        public static IReadOnlyDictionary<IPrimitiveReader<TMaterial>, MeshPrimitiveNormalsAndTangents<TMaterial>> GenerateNormalsTangents(IMeshBuilder<TMaterial> mesh)
        {
            var pairs = mesh.Primitives
                .Where(item => item.VerticesPerPrimitive > 2)
                .ToDictionary(p => p, p => new MeshPrimitiveNormalsAndTangents<TMaterial>(p));

            // we can safaly generate both sets because MeshPrimitiveNormalsAndTangents will still return the good normals and tangets if they exist.

            VertexNormalsFactory.CalculateSmoothNormals(pairs.Values.ToList());
            VertexTangentsFactory.CalculateTangents(pairs.Values.ToList());

            return pairs;
        }

        public MeshPrimitiveNormalsAndTangents(IPrimitiveReader<TMaterial> source)
        {
            _Source = source;
        }

        #endregion

        #region data

        private readonly IPrimitiveReader<TMaterial> _Source;
        private Vector3[] _Normals;
        private Vector4[] _Tangents;

        #endregion

        #region API

        public int VertexCount => _Source.Vertices.Count;

        public IEnumerable<(int A, int B, int C)> GetTriangleIndices()
        {
            return _Source.Triangles;
        }

        public Vector3 GetVertexPosition(int idx)
        {
            var v = _Source.Vertices[idx];
            return v.GetGeometry().GetPosition();
        }

        public Vector3 GetVertexNormal(int idx)
        {
            var v = _Source.Vertices[idx];
            return v.GetGeometry().TryGetNormal(out Vector3 normal) ? normal : _Normals[idx];
        }

        public Vector4 GetVertexTangent(int idx)
        {
            var v = _Source.Vertices[idx];
            return v.GetGeometry().TryGetTangent(out Vector4 tangent) ? tangent : _Tangents[idx];
        }

        public Vector2 GetVertexTexCoord(int idx)
        {
            var v = _Source.Vertices[idx];
            var m = v.GetMaterial();
            return m.MaxTextCoords > 0 ? m.GetTexCoord(0) : Vector2.Zero;
        }

        public void SetVertexNormal(int idx, Vector3 normal)
        {
            if (_Normals == null) _Normals = new Vector3[VertexCount];
            _Normals[idx] = normal;
        }

        public void SetVertexTangent(int idx, Vector4 tangent)
        {
            if (_Tangents == null) _Tangents = new Vector4[VertexCount];
            _Tangents[idx] = tangent;
        }

        #endregion
    }
}
