using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;

using SharpGLTF.Geometry.VertexTypes;

namespace SharpGLTF.Geometry
{
    public interface IPrimitiveMorphTargetReader
    {
        /// <summary>
        /// Gets the collection of vertex indices that have morph target deltas.
        /// </summary>
        /// <returns>A collection of vertex indices.</returns>
        IReadOnlyCollection<int> GetTargetIndices();

        /// <summary>
        /// Gets the vertex for the given <paramref name="vertexIndex"/> morphed by the current morph target (if any).
        /// </summary>
        /// <param name="vertexIndex">The index of the vertex.</param>
        /// <returns>If the given index has a morphed vertex, it will return it, else ir will return the base vertex.</returns>
        IVertexBuilder GetVertex(int vertexIndex);

        /// <summary>
        /// Gets the <see cref="VertexGeometryDelta"/> of a given vertex for a given morph target.
        /// </summary>
        /// <param name="vertexIndex">The index of the vertex.</param>
        /// <returns>A Vertex delta (Morphed vertex minus base vertex).</returns>
        VertexBuilder<VertexGeometryDelta, VertexMaterialDelta, VertexEmpty> GetVertexDelta(int vertexIndex);
    }

    /// <summary>
    /// Represents the vertex deltas of a specific morph target.
    /// <see cref="PrimitiveBuilder{TMaterial, TvG, TvM, TvS}._UseMorphTarget(int)"/>
    /// </summary>
    /// <typeparam name="TvG">The vertex fragment type with Position, Normal and Tangent.</typeparam>
    /// <typeparam name="TvM">The vertex fragment type with Color0, Color1, TexCoord0, TexCoord1.</typeparam>
    class PrimitiveMorphTargetBuilder<TvG, TvM> : IPrimitiveMorphTargetReader
        where TvG : struct, IVertexGeometry
        where TvM : struct, IVertexMaterial
    {
        #region lifecycle

        internal PrimitiveMorphTargetBuilder(Func<int, VertexBuilder<TvG, TvM, VertexEmpty>> baseVertexFunc)
        {
            this._BaseVertexFunc = baseVertexFunc;
            this._MorphVertices = new Dictionary<int, VertexBuilder<TvG, TvM, VertexEmpty>>();
        }

        internal PrimitiveMorphTargetBuilder(Func<int, VertexBuilder<TvG, TvM, VertexEmpty>> baseVertexFunc, PrimitiveMorphTargetBuilder<TvG, TvM> other)
        {
            this._BaseVertexFunc = baseVertexFunc;
            this._MorphVertices = new Dictionary<int, VertexBuilder<TvG, TvM, VertexEmpty>>(other._MorphVertices);
        }

        #endregion

        #region data

        private readonly Func<int, VertexBuilder<TvG, TvM, VertexEmpty>> _BaseVertexFunc;

        private readonly Dictionary<int, VertexBuilder<TvG, TvM, VertexEmpty>> _MorphVertices;

        #endregion

        #region API

        public IReadOnlyCollection<int> GetTargetIndices()
        {
            return _MorphVertices.Keys;
        }

        public VertexBuilder<VertexGeometryDelta, VertexMaterialDelta, VertexEmpty> GetVertexDelta(int vertexIndex)
        {
            if (_MorphVertices.TryGetValue(vertexIndex, out VertexBuilder<TvG, TvM, VertexEmpty> value))
            {
                var vertex = _BaseVertexFunc(vertexIndex);
                return new VertexBuilder<VertexGeometryDelta, VertexMaterialDelta, VertexEmpty>(
                    value.Geometry.Subtract(vertex.Geometry),
                    value.Material.Subtract(vertex.Material));
            }

            return default;
        }

        public void SetVertexDelta(int vertexIndex, VertexGeometryDelta geometryDelta, VertexMaterialDelta materialDelta)
        {
            if (object.Equals(geometryDelta, default(VertexGeometryDelta)))
            {
                _RemoveVertex(vertexIndex);
                return;
            }

            var vertex = _BaseVertexFunc(vertexIndex);

            vertex.Geometry.Add(geometryDelta);
            if (typeof(TvM) != typeof(VertexEmpty))
                vertex.Material.Add(materialDelta);

            _SetVertex(vertexIndex, vertex);
        }

        IVertexBuilder IPrimitiveMorphTargetReader.GetVertex(int vertexIndex)
        {
            return _MorphVertices.TryGetValue(vertexIndex, out VertexBuilder<TvG, TvM, VertexEmpty> value) ? value : _BaseVertexFunc(vertexIndex);
        }

        public VertexBuilder<TvG, TvM, VertexEmpty> GetVertex(int vertexIndex)
        {
            return _MorphVertices.TryGetValue(vertexIndex, out VertexBuilder<TvG, TvM, VertexEmpty> value) ? value : _BaseVertexFunc(vertexIndex);
        }

        public void SetVertex(int vertexIndex, VertexBuilder<TvG, TvM, VertexEmpty> vertex)
        {
            if (object.Equals(vertex, _BaseVertexFunc(vertexIndex)))
            {
                _RemoveVertex(vertexIndex);
                return;
            }

            _SetVertex(vertexIndex, vertex);
        }

        private void _SetVertex(int vertexIndex, VertexBuilder<TvG, TvM, VertexEmpty> vertex)
        {
            _MorphVertices[vertexIndex] = vertex;
        }

        private void _RemoveVertex(int vertexIndex)
        {
            _MorphVertices.Remove(vertexIndex);
        }

        #endregion

        #region internals

        internal void TransformVertices(Func<VertexBuilder<TvG, TvM, VertexEmpty>, VertexBuilder<TvG, TvM, VertexEmpty>> vertexFunc)
        {
            foreach (var vidx in _MorphVertices.Keys)
            {
                var g = GetVertex(vidx);

                g = vertexFunc(g);

                SetVertex(vidx, g);
            }
        }

        internal void SetMorphTargets(IPrimitiveMorphTargetReader other, IReadOnlyDictionary<int, int> vertexMap, Func<IVertexGeometry, VertexBuilder<TvG, TvM, VertexEmpty>> vertexFunc)
        {
            Guard.NotNull(vertexFunc, nameof(vertexFunc));

            var indices = other.GetTargetIndices();

            foreach (var srcVidx in indices)
            {
                var g = vertexFunc(other.GetVertex(srcVidx).GetGeometry());

                var dstVidx = srcVidx;

                if (vertexMap != null)
                {
                    if (!vertexMap.TryGetValue(srcVidx, out dstVidx)) dstVidx = -1;
                }

                if (dstVidx >= 0) this.SetVertex(dstVidx, g);
            }
        }

        #endregion
    }

    /// <summary>
    /// Represents the vertex deltas of a specific morph target.
    /// <see cref="IMeshBuilder{TMaterial}.UseMorphTarget(int)"/>
    /// </summary>
    public interface IMorphTargetBuilder
    {
        IReadOnlyCollection<Vector3> Positions { get; }
        IReadOnlyCollection<IVertexGeometry> Vertices { get; }
        IReadOnlyList<IVertexGeometry> GetVertices(Vector3 position);

        /// <summary>
        /// Sets an absolute morph target.
        /// </summary>
        /// <param name="meshVertex">The base mesh vertex to morph.</param>
        /// <param name="morphVertex">The morphed vertex.</param>
        void SetVertex(IVertexGeometry meshVertex, IVertexGeometry morphVertex);

        /// <summary>
        /// Sets an absolute morph target.
        /// </summary>
        /// <param name="meshVertex">The base mesh vertex to morph.</param>
        /// <param name="morphVertex">The morphed vertex.</param>
        /// <param name="morphMaterial">The morphed vertex material.</param>
        void SetVertex(IVertexGeometry meshVertex, IVertexGeometry morphVertex, IVertexMaterial morphMaterial);

        /// <summary>
        /// Sets a relative morph target
        /// </summary>
        /// <param name="meshVertex">The base mesh vertex to morph.</param>
        /// <param name="geometryDelta">The offset from <paramref name="meshVertex"/> to morph.</param>
        void SetVertexDelta(IVertexGeometry meshVertex, VertexGeometryDelta geometryDelta);

        /// <summary>
        /// Sets a relative morph target
        /// </summary>
        /// <param name="meshVertex">The base mesh vertex to morph.</param>
        /// <param name="geometryDelta">The offset from <paramref name="meshVertex"/> to morph.</param>
        /// <param name="materialDelta">The offset from <paramref name="meshVertex"/> material to morph.</param>
        void SetVertexDelta(IVertexGeometry meshVertex, VertexGeometryDelta geometryDelta, VertexMaterialDelta materialDelta);

        /// <summary>
        /// Sets a relative morph target to all base mesh vertices matching <paramref name="meshPosition"/>.
        /// </summary>
        /// <param name="meshPosition">The base vertex position.</param>
        /// <param name="geometryDelta">The offset to apply to each matching vertex found.</param>
        void SetVertexDelta(Vector3 meshPosition, VertexGeometryDelta geometryDelta);

        /// <summary>
        /// Sets a relative morph target to all base mesh vertices matching <paramref name="meshPosition"/>.
        /// </summary>
        /// <param name="meshPosition">The base vertex position.</param>
        /// <param name="geometryDelta">The offset to apply to each matching vertex found.</param>
        /// <param name="materialDelta">The offset to apply to each matching vertex material found.</param>
        void SetVertexDelta(Vector3 meshPosition, VertexGeometryDelta geometryDelta, VertexMaterialDelta materialDelta);
    }

    /// <summary>
    /// Represents the vertex deltas of a specific morph target.
    /// <see cref="MeshBuilder{TMaterial, TvG, TvM, TvS}.UseMorphTarget(int)"/>
    /// </summary>
    /// <typeparam name="TMaterial">The material type used by the base mesh.</typeparam>
    /// <typeparam name="TvG">The vertex geometry type used by the base mesh.</typeparam>
    /// <typeparam name="TvS">The vertex skinning type used by the base mesh.</typeparam>
    /// <typeparam name="TvM">The vertex material type used by the base mesh.</typeparam>
    /// <remarks>
    /// Morph targets are stored separately on each <see cref="PrimitiveBuilder{TMaterial, TvG, TvM, TvS}"/>,
    /// so connecting vertices between two primitives might be duplicated. This means that when we set
    /// a displaced vertex, we must be sure we do so for all instances we can find.
    /// </remarks>
    public sealed class MorphTargetBuilder<TMaterial, TvG, TvS, TvM> : IMorphTargetBuilder
            where TvG : struct, IVertexGeometry
            where TvM : struct, IVertexMaterial
            where TvS : struct, IVertexSkinning
    {
        #region lifecycle

        internal MorphTargetBuilder(MeshBuilder<TMaterial, TvG, TvM, TvS> mesh, int morphTargetIndex)
        {
            _Mesh = mesh;
            _MorphTargetIndex = morphTargetIndex;

            foreach (var prim in _Mesh.Primitives)
            {
                for (int vidx = 0; vidx < prim.Vertices.Count; ++vidx)
                {
                    var key = prim.Vertices[vidx].Geometry;

                    if (!_Vertices.TryGetValue(key, out List<(PrimitiveBuilder<TMaterial, TvG, TvM, TvS>, int)> val))
                    {
                        _Vertices[key] = val = new List<(PrimitiveBuilder<TMaterial, TvG, TvM, TvS>, int)>();
                    }

                    val.Add((prim, vidx));

                    if (!_Positions.TryGetValue(key.GetPosition(), out List<TvG> geos))
                    {
                        _Positions[key.GetPosition()] = geos = new List<TvG>();
                    }

                    geos.Add(key);
                }
            }
        }

        #endregion

        #region data

        private readonly MeshBuilder<TMaterial, TvG, TvM, TvS> _Mesh;
        private readonly int _MorphTargetIndex;

        private readonly Dictionary<TvG, List<(PrimitiveBuilder<TMaterial, TvG, TvM, TvS>, int)>> _Vertices = new Dictionary<TvG, List<(PrimitiveBuilder<TMaterial, TvG, TvM, TvS>, int)>>();

        private readonly Dictionary<Vector3, List<TvG>> _Positions = new Dictionary<Vector3, List<TvG>>();

        #endregion

        #region properties

        public IReadOnlyCollection<Vector3> Positions => _Positions.Keys;

        public IReadOnlyCollection<TvG> Vertices => _Vertices.Keys;

        #endregion

        #region API

        public IReadOnlyList<TvG> GetVertices(Vector3 position)
        {
            return _Positions.TryGetValue(position, out List<TvG> geos) ? (IReadOnlyList<TvG>)geos : Array.Empty<TvG>();
        }

        public void SetVertexDelta(Vector3 meshVertex, VertexGeometryDelta geometryDelta)
        {
            if (_Positions.TryGetValue(meshVertex, out List<TvG> geos))
            {
                foreach (var g in geos) SetVertexDelta(g, geometryDelta, VertexMaterialDelta.Zero);
            }
        }

        public void SetVertexDelta(Vector3 meshVertex, VertexGeometryDelta geometryDelta, VertexMaterialDelta materialDelta)
        {
            if (_Positions.TryGetValue(meshVertex, out List<TvG> geos))
            {
                foreach (var g in geos) SetVertexDelta(g, geometryDelta, materialDelta);
            }
        }

        public void SetVertexDelta(TvG meshVertex, VertexGeometryDelta geometryDelta)
        {
            if (_Vertices.TryGetValue(meshVertex, out List<(PrimitiveBuilder<TMaterial, TvG, TvM, TvS>, int)> val))
            {
                foreach (var entry in val)
                {
                    entry.Item1
                        ._UseMorphTarget(_MorphTargetIndex)
                        .SetVertexDelta(entry.Item2, geometryDelta, VertexMaterialDelta.Zero);
                }
            }
        }

        public void SetVertexDelta(TvG meshVertex, VertexGeometryDelta geometryDelta, VertexMaterialDelta materialDelta)
        {
            if (_Vertices.TryGetValue(meshVertex, out List<(PrimitiveBuilder<TMaterial, TvG, TvM, TvS>, int)> val))
            {
                foreach (var entry in val)
                {
                    entry.Item1
                        ._UseMorphTarget(_MorphTargetIndex)
                        .SetVertexDelta(entry.Item2, geometryDelta, materialDelta);
                }
            }
        }

        public void SetVertex(TvG meshVertex, VertexBuilder<TvG, TvM, VertexEmpty> morphVertex)
        {
            if (_Vertices.TryGetValue(meshVertex, out List<(PrimitiveBuilder<TMaterial, TvG, TvM, TvS>, int)> val))
            {
                foreach (var entry in val)
                {
                    entry.Item1
                        ._UseMorphTarget(_MorphTargetIndex)
                        .SetVertex(entry.Item2, morphVertex);
                }
            }
        }

        public void SetVertex(TvG meshVertex, TvG morphVertex)
        {
            if (_Vertices.TryGetValue(meshVertex, out List<(PrimitiveBuilder<TMaterial, TvG, TvM, TvS>, int)> val))
            {
                foreach (var entry in val)
                {
                    var vertexMaterial = entry.Item1.Vertices[entry.Item2].Material;
                    entry.Item1
                        ._UseMorphTarget(_MorphTargetIndex)
                        .SetVertex(entry.Item2, new VertexBuilder<TvG, TvM, VertexEmpty>(morphVertex, vertexMaterial));
                }
            }
        }

        #endregion

        #region IMorphTargetBuilder

        IReadOnlyCollection<IVertexGeometry> IMorphTargetBuilder.Vertices => (IReadOnlyList<IVertexGeometry>)(IReadOnlyCollection<TvG>)_Vertices.Keys;

        IReadOnlyList<IVertexGeometry> IMorphTargetBuilder.GetVertices(Vector3 position)
        {
            return _Positions.TryGetValue(position, out List<TvG> geos)
                ? (IReadOnlyList<IVertexGeometry>)geos
                : Array.Empty<IVertexGeometry>();
        }

        void IMorphTargetBuilder.SetVertex(IVertexGeometry meshVertex, IVertexGeometry morphVertex)
        {
            SetVertex(
                meshVertex.ConvertToGeometry<TvG>(),
                new VertexBuilder<TvG, TvM, VertexEmpty>(morphVertex.ConvertToGeometry<TvG>(), default(VertexEmpty).ConvertToMaterial<TvM>())
                );
        }

        void IMorphTargetBuilder.SetVertex(IVertexGeometry meshVertex, IVertexGeometry morphVertex, IVertexMaterial morphMaterial)
        {
            SetVertex(
                meshVertex.ConvertToGeometry<TvG>(),
                new VertexBuilder<TvG, TvM, VertexEmpty>(morphVertex.ConvertToGeometry<TvG>(), morphMaterial.ConvertToMaterial<TvM>())
                );
        }

        void IMorphTargetBuilder.SetVertexDelta(IVertexGeometry meshVertex, VertexGeometryDelta geometryDelta)
        {
            SetVertexDelta(meshVertex.ConvertToGeometry<TvG>(), geometryDelta, VertexMaterialDelta.Zero);
        }

        void IMorphTargetBuilder.SetVertexDelta(IVertexGeometry meshVertex, VertexGeometryDelta geometryDelta, VertexMaterialDelta materialDelta)
        {
            SetVertexDelta(meshVertex.ConvertToGeometry<TvG>(), geometryDelta, materialDelta);
        }

        #endregion
    }
}
