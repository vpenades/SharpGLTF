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
        /// Gets the collection of vertex indices that have deltas.
        /// </summary>
        /// <returns>A collection of vertex indices.</returns>
        IReadOnlyCollection<int> GetTargetIndices();

        /// <summary>
        /// Gets the <see cref="VertexGeometryDelta"/> of a given vertex for a given morph target.
        /// </summary>
        /// <param name="vertexIndex">The index of the vertex.</param>
        /// <returns>A Vertex delta.</returns>
        VertexGeometryDelta GetVertexDelta(int vertexIndex);
    }

    sealed class PrimitiveMorphTargetBuilder : IPrimitiveMorphTargetReader
    {
        #region lifecycle

        internal PrimitiveMorphTargetBuilder(Func<int, IVertexGeometry> baseVertexFunc)
        {
            this._BaseVertexFunc = baseVertexFunc;
            this._MorphVertices = new Dictionary<int, IVertexGeometry>();
        }

        internal PrimitiveMorphTargetBuilder(Func<int, IVertexGeometry> baseVertexFunc, PrimitiveMorphTargetBuilder other)
        {
            this._BaseVertexFunc = baseVertexFunc;
            this._MorphVertices = new Dictionary<int, IVertexGeometry>(other._MorphVertices);
        }

        #endregion

        #region data

        private readonly Func<int, IVertexGeometry> _BaseVertexFunc;

        private readonly Dictionary<int, IVertexGeometry> _MorphVertices;

        #endregion

        #region API

        public IReadOnlyCollection<int> GetTargetIndices()
        {
            return _MorphVertices.Keys;
        }

        public VertexGeometryDelta GetVertexDelta(int vertexIndex)
        {
            if (_MorphVertices.TryGetValue(vertexIndex, out IVertexGeometry value))
            {
                return value.Subtract(_BaseVertexFunc(vertexIndex));
            }

            return default;
        }

        public void SetVertexDelta(int vertexIndex, VertexGeometryDelta value)
        {
            if (object.Equals(value, default(VertexGeometryDelta)))
            {
                _RemoveVertex(vertexIndex);
                return;
            }

            var vertex = _BaseVertexFunc(vertexIndex);
            vertex.Add(value);

            _SetVertex(vertexIndex, vertex);
        }

        public IVertexGeometry GetVertex(int vertexIndex)
        {
            if (_MorphVertices.TryGetValue(vertexIndex, out IVertexGeometry value))
            {
                return value;
            }

            return _BaseVertexFunc(vertexIndex);
        }

        public void SetVertex(int vertexIndex, IVertexGeometry value)
        {
            if (object.Equals(value, _BaseVertexFunc(vertexIndex)))
            {
                _RemoveVertex(vertexIndex);
                return;
            }

            _SetVertex(vertexIndex, value);
        }

        private void _SetVertex(int vertexIndex, IVertexGeometry value)
        {
            _MorphVertices[vertexIndex] = value;
        }

        private void _RemoveVertex(int vertexIndex)
        {
            _MorphVertices.Remove(vertexIndex);
        }

        #endregion

        #region internals

        internal void TransformVertices(Func<IVertexGeometry, IVertexGeometry> vertexFunc)
        {
            foreach (var vidx in _MorphVertices.Keys)
            {
                var g = GetVertex(vidx);

                g = vertexFunc(g);

                SetVertex(vidx, g);
            }
        }

        internal void SetMorphTargets(PrimitiveMorphTargetBuilder other, IReadOnlyDictionary<int, int> vertexMap, Func<IVertexGeometry, IVertexGeometry> vertexFunc)
        {
            var indices = other.GetTargetIndices();

            foreach (var srcVidx in indices)
            {
                var g = other.GetVertex(srcVidx);

                if (vertexFunc != null) g = vertexFunc(g);

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
    /// Utility class to edit the Morph targets of a mesh.
    /// </summary>
    public sealed class MorphTargetBuilder
    {
        #region lifecycle

        internal MorphTargetBuilder(IEnumerable<(IPrimitiveBuilder, IReadOnlyList<IVertexBuilder>)> primitives, int morphTargetIndex)
        {
            _MorphTargetIndex = morphTargetIndex;

            foreach (var prim in primitives)
            {
                for (int vidx = 0; vidx < prim.Item2.Count; ++vidx)
                {
                    var key = prim.Item2[vidx].GetGeometry();

                    if (!_Vertices.TryGetValue(key, out List<(IPrimitiveBuilder, int)> val))
                    {
                        _Vertices[key] = val = new List<(IPrimitiveBuilder, int)>();
                    }

                    val.Add((prim.Item1, vidx));

                    if (!_Positions.TryGetValue(key.GetPosition(), out List<IVertexGeometry> geos))
                    {
                        _Positions[key.GetPosition()] = geos = new List<IVertexGeometry>();
                    }

                    geos.Add(key);
                }
            }
        }

        #endregion

        #region data

        private readonly int _MorphTargetIndex;

        private readonly Dictionary<IVertexGeometry, List<(IPrimitiveBuilder, int)>> _Vertices = new Dictionary<IVertexGeometry, List<(IPrimitiveBuilder, int)>>();

        private readonly Dictionary<Vector3, List<IVertexGeometry>> _Positions = new Dictionary<Vector3, List<IVertexGeometry>>();

        #endregion

        #region properties

        public IReadOnlyCollection<Vector3> Positions => _Positions.Keys;

        public IReadOnlyCollection<IVertexGeometry> Vertices => _Vertices.Keys;

        #endregion

        #region API

        public IReadOnlyList<IVertexGeometry> GetVertices(Vector3 position)
        {
            return _Positions.TryGetValue(position, out List<IVertexGeometry> geos) ? (IReadOnlyList<IVertexGeometry>)geos : Array.Empty<IVertexGeometry>();
        }

        public void SetVertexDelta(Vector3 key, VertexGeometryDelta delta)
        {
            if (_Positions.TryGetValue(key, out List<IVertexGeometry> geos))
            {
                foreach (var g in geos) SetVertexDelta(g, delta);
            }
        }

        public void SetVertex(IVertexGeometry meshVertex, IVertexGeometry morphVertex)
        {
            if (_Vertices.TryGetValue(meshVertex, out List<(IPrimitiveBuilder, int)> val))
            {
                foreach (var entry in val)
                {
                    entry.Item1
                        .SetVertex(_MorphTargetIndex, entry.Item2, morphVertex);
                }
            }
        }

        public void SetVertexDelta(IVertexGeometry meshVertex, VertexGeometryDelta delta)
        {
            if (_Vertices.TryGetValue(meshVertex, out List<(IPrimitiveBuilder, int)> val))
            {
                foreach (var entry in val)
                {
                    entry.Item1
                        .SetVertexDelta(_MorphTargetIndex, entry.Item2, delta);
                }
            }
        }

        #endregion
    }
}
