using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;

namespace SharpGLTF.Animations
{
    /// <summary>
    /// Defines a <see cref="Vector3"/> curve sampler that can be sampled with STEP or LINEAR interpolations.
    /// </summary>
    struct Vector3LinearSampler : ICurveSampler<Vector3>, IConvertibleCurve<Vector3>
    {
        #region lifecycle

        public Vector3LinearSampler(IEnumerable<(float, Vector3)> sequence, bool isLinear)
        {
            _Sequence = sequence;
            _Linear = isLinear;
        }

        #endregion

        #region data

        private readonly IEnumerable<(float, Vector3)> _Sequence;
        private readonly Boolean _Linear;

        #endregion

        #region API

        public int MaxDegree => _Linear ? 1 : 0;

        public Vector3 GetPoint(float offset)
        {
            var segment = SamplerFactory.FindPairContainingOffset(_Sequence, offset);

            if (!_Linear) return segment.Item1;

            return Vector3.Lerp(segment.Item1, segment.Item2, segment.Item3);
        }

        public IReadOnlyDictionary<float, Vector3> ToStepCurve()
        {
            Guard.IsFalse(_Linear, nameof(_Linear));
            return _Sequence.ToDictionary(pair => pair.Item1, pair => pair.Item2);
        }

        public IReadOnlyDictionary<float, Vector3> ToLinearCurve()
        {
            Guard.IsTrue(_Linear, nameof(_Linear));
            return _Sequence.ToDictionary(pair => pair.Item1, pair => pair.Item2);
        }

        public IReadOnlyDictionary<float, (Vector3, Vector3, Vector3)> ToSplineCurve()
        {
            throw new NotImplementedException();
        }

        #endregion
    }

    /// <summary>
    /// Defines a <see cref="Quaternion"/> curve sampler that can be sampled with STEP or LINEAR interpolations.
    /// </summary>
    struct QuaternionLinearSampler : ICurveSampler<Quaternion>, IConvertibleCurve<Quaternion>
    {
        #region lifecycle

        public QuaternionLinearSampler(IEnumerable<(float, Quaternion)> sequence, bool isLinear)
        {
            _Sequence = sequence;
            _Linear = isLinear;
        }

        #endregion

        #region data

        private readonly IEnumerable<(float, Quaternion)> _Sequence;
        private readonly Boolean _Linear;

        #endregion

        #region API

        public int MaxDegree => _Linear ? 1 : 0;

        public Quaternion GetPoint(float offset)
        {
            var segment = SamplerFactory.FindPairContainingOffset(_Sequence, offset);

            if (!_Linear) return segment.Item1;

            return Quaternion.Slerp(segment.Item1, segment.Item2, segment.Item3);
        }

        public IReadOnlyDictionary<float, Quaternion> ToStepCurve()
        {
            Guard.IsFalse(_Linear, nameof(_Linear));
            return _Sequence.ToDictionary(pair => pair.Item1, pair => pair.Item2);
        }

        public IReadOnlyDictionary<float, Quaternion> ToLinearCurve()
        {
            Guard.IsTrue(_Linear, nameof(_Linear));
            return _Sequence.ToDictionary(pair => pair.Item1, pair => pair.Item2);
        }

        public IReadOnlyDictionary<float, (Quaternion, Quaternion, Quaternion)> ToSplineCurve()
        {
            throw new NotImplementedException();
        }

        #endregion
    }

    /// <summary>
    /// Defines a <see cref="Transforms.SparseWeight8"/> curve sampler that can be sampled with STEP or LINEAR interpolation.
    /// </summary>
    struct SparseLinearSampler : ICurveSampler<Transforms.SparseWeight8>, IConvertibleCurve<Transforms.SparseWeight8>
    {
        #region lifecycle

        public SparseLinearSampler(IEnumerable<(float, Transforms.SparseWeight8)> sequence, bool isLinear)
        {
            _Sequence = sequence;
            _Linear = isLinear;
        }

        #endregion

        #region data

        private readonly IEnumerable<(float, Transforms.SparseWeight8)> _Sequence;
        private readonly Boolean _Linear;

        #endregion

        #region API

        public int MaxDegree => _Linear ? 1 : 0;

        public Transforms.SparseWeight8 GetPoint(float offset)
        {
            var segment = SamplerFactory.FindPairContainingOffset(_Sequence, offset);

            if (!_Linear) return segment.Item1;

            var weights = Transforms.SparseWeight8.InterpolateLinear(segment.Item1, segment.Item2, segment.Item3);

            return weights;
        }

        public IReadOnlyDictionary<float, Transforms.SparseWeight8> ToStepCurve()
        {
            throw new NotImplementedException();
        }

        public IReadOnlyDictionary<float, Transforms.SparseWeight8> ToLinearCurve()
        {
            Guard.IsTrue(_Linear, nameof(_Linear));
            return _Sequence.ToDictionary(pair => pair.Item1, pair => pair.Item2);
        }

        public IReadOnlyDictionary<float, (Transforms.SparseWeight8, Transforms.SparseWeight8, Transforms.SparseWeight8)> ToSplineCurve()
        {
            throw new NotImplementedException();
        }

        #endregion
    }

    /// <summary>
    /// Defines a <see cref="float"/>[] curve sampler that can be sampled with STEP or LINEAR interpolations.
    /// </summary>
    struct ArrayLinearSampler : ICurveSampler<float[]>, IConvertibleCurve<float[]>
    {
        #region lifecycle

        public ArrayLinearSampler(IEnumerable<(float, float[])> sequence, bool isLinear)
        {
            _Sequence = sequence;
            _Linear = isLinear;
        }

        #endregion

        #region data

        private readonly IEnumerable<(float, float[])> _Sequence;
        private readonly Boolean _Linear;

        #endregion

        #region API

        public int MaxDegree => _Linear ? 1 : 0;

        public float[] GetPoint(float offset)
        {
            var segment = SamplerFactory.FindPairContainingOffset(_Sequence, offset);

            if (!_Linear) return segment.Item1;

            return SamplerFactory.InterpolateLinear(segment.Item1, segment.Item2, segment.Item3);
        }

        public IReadOnlyDictionary<float, float[]> ToStepCurve()
        {
            Guard.IsFalse(_Linear, nameof(_Linear));
            return _Sequence.ToDictionary(pair => pair.Item1, pair => pair.Item2);
        }

        public IReadOnlyDictionary<float, float[]> ToLinearCurve()
        {
            Guard.IsTrue(_Linear, nameof(_Linear));
            return _Sequence.ToDictionary(pair => pair.Item1, pair => pair.Item2);
        }

        public IReadOnlyDictionary<float, (float[], float[], float[])> ToSplineCurve()
        {
            throw new NotImplementedException();
        }

        #endregion
    }
}
