using System;
using System.Collections.Generic;
using System.Text;

using SharpGLTF.Geometry.VertexTypes;

namespace SharpGLTF.Geometry
{
    public interface IMorphTargetReader
    {
        int TargetsCount { get; }

        IReadOnlyCollection<int> GetTargetIndices(int morphTargetIndex);

        IVertexGeometry GetVertexDisplacement(int morphTargetIndex, int vertexIndex);
    }

    public class MorphTargetBuilder<TvG> : IMorphTargetReader
        where TvG : struct, IVertexGeometry
    {
        #region lifecycle
        internal MorphTargetBuilder(Func<int, TvG> baseVertexFunc)
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

        public Boolean AbsoluteMode => true;

        #endregion

        #region API

        public IReadOnlyCollection<int> GetTargetIndices(int morphTargetIndex)
        {
            return morphTargetIndex < _Targets.Count ? _Targets[morphTargetIndex].Keys : (IReadOnlyCollection<int>)Array.Empty<int>();
        }

        IVertexGeometry IMorphTargetReader.GetVertexDisplacement(int morphTargetIndex, int vertexIndex)
        {
            return _GetVertexDisplacement(morphTargetIndex, vertexIndex);
        }

        TvG GetVertexDisplacement(int morphTargetIndex, int vertexIndex)
        {
            return _GetVertexDisplacement(morphTargetIndex, vertexIndex);
        }

        private TvG _GetVertexDisplacement(int morphTargetIndex, int vertexIndex)
        {
            var target = _Targets[morphTargetIndex];

            if (target.TryGetValue(vertexIndex, out TvG value))
            {
                if (this.AbsoluteMode) value = (TvG)value.ToDisplaceMorph(_BaseVertexFunc(vertexIndex));

                return value;
            }

            return default;
        }

        public TvG GetVertex(int morphTargetIndex, int vertexIndex)
        {
            var target = _Targets[morphTargetIndex];

            if (target.TryGetValue(vertexIndex, out TvG value))
            {
                if (!this.AbsoluteMode) value = (TvG)value.ToAbsoluteMorph(_BaseVertexFunc(vertexIndex));

                return value;
            }

            return _BaseVertexFunc(vertexIndex);
        }

        public void SetVertexDisplacement(int morphTargetIndex, int vertexIndex, TvG value)
        {
            if (object.Equals(value, default(TvG)))
            {
                _RemoveVertex(morphTargetIndex, vertexIndex);
                return;
            }

            if (this.AbsoluteMode)
            {
                value = (TvG)value.ToAbsoluteMorph(_BaseVertexFunc(vertexIndex));
            }

            _SetVertex(morphTargetIndex, vertexIndex, value);
        }

        public void SetVertex(int morphTargetIndex, int vertexIndex, TvG value)
        {
            if (object.Equals(value, _BaseVertexFunc(vertexIndex)))
            {
                _RemoveVertex(morphTargetIndex, vertexIndex);
                return;
            }

            if (!this.AbsoluteMode)
            {
                value = (TvG)value.ToDisplaceMorph(_BaseVertexFunc(vertexIndex));
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

        internal void SetMorphTargets(MorphTargetBuilder<TvG> other, IReadOnlyDictionary<int, int> vertexMap, Func<TvG, TvG> vertexFunc)
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
}
