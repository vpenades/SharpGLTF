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
        int TargetsCount { get; }

        /// <summary>
        /// Gets the collection of vertex indices that have deltas.
        /// </summary>
        /// <param name="morphTargetIndex">The morph target to query.</param>
        /// <returns>A collection of vertex indices.</returns>
        IReadOnlyCollection<int> GetTargetIndices(int morphTargetIndex);

        /// <summary>
        /// Gets the <see cref="VertexGeometryDelta"/> of a given vertex for a given morph target.
        /// </summary>
        /// <param name="morphTargetIndex"></param>
        /// <param name="vertexIndex"></param>
        /// <returns></returns>
        VertexGeometryDelta GetVertexDelta(int morphTargetIndex, int vertexIndex);
    }

    public class PrimitiveMorphTargetBuilder<TvG> : IPrimitiveMorphTargetReader
        where TvG : struct, IVertexGeometry
    {
        #region lifecycle
        internal PrimitiveMorphTargetBuilder(Func<int, TvG> baseVertexFunc)
        {
            _BaseVertexFunc = baseVertexFunc;
        }

        #endregion

        #region data

        private readonly Func<int, TvG> _BaseVertexFunc;

        private readonly List<Dictionary<int, TvG>> _Targets = new List<Dictionary<int, TvG>>();

        #endregion

        #region properties

        public int TargetsCount => _Targets.Count;

        #endregion

        #region API

        public IReadOnlyCollection<int> GetTargetIndices(int morphTargetIndex)
        {
            return morphTargetIndex < _Targets.Count ? _Targets[morphTargetIndex].Keys : (IReadOnlyCollection<int>)Array.Empty<int>();
        }

        public VertexGeometryDelta GetVertexDelta(int morphTargetIndex, int vertexIndex)
        {
            var target = _Targets[morphTargetIndex];

            if (target.TryGetValue(vertexIndex, out TvG value))
            {
                return value.Subtract(_BaseVertexFunc(vertexIndex));
            }

            return default;
        }

        public void SetVertexDelta(int morphTargetIndex, int vertexIndex, VertexGeometryDelta value)
        {
            if (object.Equals(value, default(VertexGeometryDelta)))
            {
                _RemoveVertex(morphTargetIndex, vertexIndex);
                return;
            }

            var vertex = _BaseVertexFunc(vertexIndex);
            vertex.Add(value);

            _SetVertex(morphTargetIndex, vertexIndex, vertex);
        }

        public TvG GetVertex(int morphTargetIndex, int vertexIndex)
        {
            var target = _Targets[morphTargetIndex];

            if (target.TryGetValue(vertexIndex, out TvG value))
            {
                return value;
            }

            return _BaseVertexFunc(vertexIndex);
        }

        public void SetVertex(int morphTargetIndex, int vertexIndex, TvG value)
        {
            if (object.Equals(value, _BaseVertexFunc(vertexIndex)))
            {
                _RemoveVertex(morphTargetIndex, vertexIndex);
                return;
            }

            _SetVertex(morphTargetIndex, vertexIndex, value);
        }

        private void _SetVertex(int morphTargetIndex, int vertexIndex, TvG value)
        {
            while (_Targets.Count <= morphTargetIndex)
            {
                _Targets.Add(new Dictionary<int, TvG>());
            }

            _Targets[morphTargetIndex][vertexIndex] = value;
        }

        private void _RemoveVertex(int morphTargetIndex, int vertexIndex)
        {
            if (morphTargetIndex >= _Targets.Count) return;

            _Targets[morphTargetIndex].Remove(vertexIndex);
        }

        #endregion

        #region internals

        internal void TransformVertices(Func<TvG, TvG> vertexFunc)
        {
            for (int tidx = 0; tidx < _Targets.Count; ++tidx)
            {
                var target = _Targets[tidx];

                foreach (var vidx in target.Keys)
                {
                    var g = GetVertex(tidx, vidx);

                    g = vertexFunc(g);

                    SetVertex(tidx, vidx, g);
                }
            }
        }

        internal void SetMorphTargets(PrimitiveMorphTargetBuilder<TvG> other, IReadOnlyDictionary<int, int> vertexMap, Func<TvG, TvG> vertexFunc)
        {
            for (int tidx = 0; tidx < other.TargetsCount; ++tidx)
            {
                var indices = other.GetTargetIndices(tidx);

                foreach (var srcVidx in indices)
                {
                    var g = other.GetVertex(tidx, srcVidx);

                    if (vertexFunc != null) g = vertexFunc(g);

                    var dstVidx = srcVidx;

                    if (vertexMap != null)
                    {
                        if (!vertexMap.TryGetValue(srcVidx, out dstVidx)) dstVidx = -1;
                    }

                    if (dstVidx >= 0) this.SetVertex(tidx, dstVidx, g);
                }
            }
        }

        #endregion
    }

    public class MorphTargetBuilder<TMaterial, TvG, TvS, TvM>
            where TvG : struct, IVertexGeometry
            where TvM : struct, IVertexMaterial
            where TvS : struct, IVertexSkinning
    {
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

        private readonly MeshBuilder<TMaterial, TvG, TvM, TvS> _Mesh;
        private readonly int _MorphTargetIndex;

        private readonly Dictionary<TvG, List<(PrimitiveBuilder<TMaterial, TvG, TvM, TvS>, int)>> _Vertices = new Dictionary<TvG, List<(PrimitiveBuilder<TMaterial, TvG, TvM, TvS>, int)>>();

        private readonly Dictionary<Vector3, List<TvG>> _Positions = new Dictionary<Vector3, List<TvG>>();

        public IReadOnlyCollection<Vector3> Positions => _Positions.Keys;

        public IReadOnlyCollection<TvG> Vertices => _Vertices.Keys;

        public void SetVertexDelta(Vector3 key, VertexGeometryDelta delta)
        {
            if (_Positions.TryGetValue(key, out List<TvG> geos))
            {
                foreach (var g in geos) SetVertexDisplacement(g, delta);
            }
        }

        public void SetVertexDisplacement(TvG vertex, VertexGeometryDelta delta)
        {
            if (_Vertices.TryGetValue(vertex, out List<(PrimitiveBuilder<TMaterial, TvG, TvM, TvS>, int)> val))
            {
                foreach (var entry in val)
                {
                    entry.Item1.MorphTargets.SetVertexDelta(_MorphTargetIndex, entry.Item2, delta);
                }
            }
        }

    }
}
