using System;
using System.Collections.Generic;
using System.Text;

using SharpGLTF.Geometry.VertexTypes;

namespace SharpGLTF.Geometry
{
    public class MorphTargetBuilder<TvG>
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

        public int Count => _Targets.Count;

        public Boolean AbsoluteMode => true;

        #endregion

        #region API

        public IReadOnlyDictionary<int, TvG> GetTarget(int idx) { return _Targets[idx]; }

        public void SetRelativeVertex(int morphTargetIndex, int vertexIndex, TvG value)
        {
            if (AbsoluteMode)
            {
                // value = value.ToAbsoluteMorph(_BaseVertexFunc(vertexIndex));

                throw new NotImplementedException();
            }

            _SetVertex(morphTargetIndex, vertexIndex, value);
        }

        public void SetAbsoluteVertex(int morphTargetIndex, int vertexIndex, TvG value)
        {
            if (!AbsoluteMode)
            {
                // value = value.ToRelativeMorph(_BaseVertexFunc(vertexIndex));

                throw new NotImplementedException();
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

        public void AddAbsoluteVertices<TvM, TvS>(int morphTargetIndex, IReadOnlyDictionary<int, TvG> vertices, Func<VertexBuilder<TvG, TvM, TvS>, VertexBuilder<TvG, TvM, TvS>> vertexTransform)
            where TvM : struct, IVertexMaterial
            where TvS : struct, IVertexSkinning
        {
            foreach (var kvp in vertices)
            {
                if (!AbsoluteMode)
                {
                    throw new NotImplementedException();
                    // TODO: if vertices are in RELATIVE MODE, we must convert them to absolute mode, transform, and back to RELATIVE
                }

                var v = new VertexBuilder<TvG, TvM, TvS>(kvp.Value, default(TvM), default(TvS));
                v = vertexTransform(v);

                this.SetAbsoluteVertex(morphTargetIndex, kvp.Key, v.Geometry);
            }
        }

        #endregion
    }
}
