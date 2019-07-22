using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;

namespace SharpGLTF.Animations
{
    /// <summary>
    /// Defines a <see cref="Vector3"/> curve sampler that can be sampled with CUBIC interpolation.
    /// </summary>
    struct Vector3CubicSampler : ICurveSampler<Vector3>, IConvertibleCurve<Vector3>
    {
        #region lifecycle

        public Vector3CubicSampler(IEnumerable<(float, (Vector3, Vector3, Vector3))> sequence)
        {
            _Sequence = sequence;
        }

        #endregion

        #region data

        private readonly IEnumerable<(float, (Vector3, Vector3, Vector3))> _Sequence;

        #endregion

        #region API

        public int MaxDegree => 3;

        public Vector3 GetPoint(float offset)
        {
            var segment = SamplerFactory.FindPairContainingOffset(_Sequence, offset);

            return SamplerFactory.InterpolateCubic
                (
                segment.Item1.Item2, segment.Item1.Item3,   // start, startTangentOut
                segment.Item2.Item2, segment.Item2.Item1,   // end, endTangentIn
                segment.Item3                               // amount
                );
        }

        public IReadOnlyDictionary<float, Vector3> ToStepCurve()
        {
            throw new NotImplementedException();
        }

        public IReadOnlyDictionary<float, Vector3> ToLinearCurve()
        {
            throw new NotImplementedException();
        }

        public IReadOnlyDictionary<float, (Vector3, Vector3, Vector3)> ToSplineCurve()
        {
            return _Sequence.ToDictionary(pair => pair.Item1, pair => pair.Item2);
        }

        #endregion
    }

    /// <summary>
    /// Defines a <see cref="Quaternion"/> curve sampler that can be sampled with CUBIC interpolation.
    /// </summary>
    struct QuaternionCubicSampler : ICurveSampler<Quaternion>, IConvertibleCurve<Quaternion>
    {
        #region lifecycle

        public QuaternionCubicSampler(IEnumerable<(float, (Quaternion, Quaternion, Quaternion))> sequence)
        {
            _Sequence = sequence;
        }

        #endregion

        #region data

        private readonly IEnumerable<(float, (Quaternion, Quaternion, Quaternion))> _Sequence;

        #endregion

        #region API

        public int MaxDegree => 3;

        public Quaternion GetPoint(float offset)
        {
            var segment = SamplerFactory.FindPairContainingOffset(_Sequence, offset);

            return SamplerFactory.InterpolateCubic
                (
                segment.Item1.Item2, segment.Item1.Item3,   // start, startTangentOut
                segment.Item2.Item2, segment.Item2.Item1,   // end, endTangentIn
                segment.Item3                               // amount
                );
        }

        public IReadOnlyDictionary<float, Quaternion> ToStepCurve()
        {
            throw new NotImplementedException();
        }

        public IReadOnlyDictionary<float, Quaternion> ToLinearCurve()
        {
            throw new NotImplementedException();
        }

        public IReadOnlyDictionary<float, (Quaternion, Quaternion, Quaternion)> ToSplineCurve()
        {
            return _Sequence.ToDictionary(pair => pair.Item1, pair => pair.Item2);
        }

        #endregion
    }

    /// <summary>
    /// Defines a <see cref="Transforms.SparseWeight8"/> curve sampler that can be sampled with CUBIC interpolation.
    /// </summary>
    struct SparseCubicSampler : ICurveSampler<Transforms.SparseWeight8>, IConvertibleCurve<Transforms.SparseWeight8>
    {
        #region lifecycle

        public SparseCubicSampler(IEnumerable<(float, (Transforms.SparseWeight8, Transforms.SparseWeight8, Transforms.SparseWeight8))> sequence)
        {
            _Sequence = sequence;
        }

        #endregion

        #region data

        private readonly IEnumerable<(float, (Transforms.SparseWeight8, Transforms.SparseWeight8, Transforms.SparseWeight8))> _Sequence;

        #endregion

        #region API

        public int MaxDegree => 3;

        public Transforms.SparseWeight8 GetPoint(float offset)
        {
            var segment = SamplerFactory.FindPairContainingOffset(_Sequence, offset);

            return Transforms.SparseWeight8.InterpolateCubic
                (
                segment.Item1.Item2, segment.Item1.Item3,   // start, startTangentOut
                segment.Item2.Item2, segment.Item2.Item1,   // end, endTangentIn
                segment.Item3                               // amount
                );
        }

        public IReadOnlyDictionary<float, Transforms.SparseWeight8> ToStepCurve()
        {
            throw new NotImplementedException();
        }

        public IReadOnlyDictionary<float, Transforms.SparseWeight8> ToLinearCurve()
        {
            throw new NotImplementedException();
        }

        public IReadOnlyDictionary<float, (Transforms.SparseWeight8, Transforms.SparseWeight8, Transforms.SparseWeight8)> ToSplineCurve()
        {
            return _Sequence.ToDictionary(pair => pair.Item1, pair => pair.Item2);
        }

        #endregion
    }

    /// <summary>
    /// Defines a <see cref="float"/>[] curve sampler that can be sampled with CUBIC interpolation.
    /// </summary>
    struct ArrayCubicSampler : ICurveSampler<float[]>, IConvertibleCurve<float[]>
    {
        #region lifecycle

        public ArrayCubicSampler(IEnumerable<(float, (float[], float[], float[]))> sequence)
        {
            _Sequence = sequence;
        }

        #endregion

        #region data

        private readonly IEnumerable<(float, (float[], float[], float[]))> _Sequence;

        #endregion

        #region API

        public int MaxDegree => 3;

        public float[] GetPoint(float offset)
        {
            var segment = SamplerFactory.FindPairContainingOffset(_Sequence, offset);

            return SamplerFactory.InterpolateCubic
                (
                segment.Item1.Item2, segment.Item1.Item3,   // start, startTangentOut
                segment.Item2.Item2, segment.Item2.Item1,   // end, endTangentIn
                segment.Item3                               // amount
                );
        }

        public IReadOnlyDictionary<float, float[]> ToStepCurve()
        {
            throw new NotImplementedException();
        }

        public IReadOnlyDictionary<float, float[]> ToLinearCurve()
        {
            throw new NotImplementedException();
        }

        public IReadOnlyDictionary<float, (float[], float[], float[])> ToSplineCurve()
        {
            return _Sequence.ToDictionary(pair => pair.Item1, pair => pair.Item2);
        }

        #endregion
    }
}
