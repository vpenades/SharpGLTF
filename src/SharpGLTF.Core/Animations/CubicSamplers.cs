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
    readonly struct Vector3CubicSampler : ICurveSampler<Vector3>, IConvertibleCurve<Vector3>
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
            var (valA, valB, amount) = SamplerFactory.FindPairContainingOffset(_Sequence, offset);

            return SamplerFactory.InterpolateCubic
                (
                valA.Item2, valA.Item3,   // start, startTangentOut
                valB.Item2, valB.Item1,   // end, endTangentIn
                amount                               // amount
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

        public IReadOnlyDictionary<float, (Vector3 TangentIn, Vector3 Value, Vector3 TangentOut)> ToSplineCurve()
        {
            return _Sequence.ToDictionary(pair => pair.Item1, pair => pair.Item2);
        }

        public ICurveSampler<Vector3> ToFastSampler()
        {
            var split = _Sequence
                .SplitByTime()
                .Select(item => new Vector3CubicSampler(item))
                .Cast<ICurveSampler<Vector3>>();

            return new FastSampler<Vector3>(split);
        }

        #endregion
    }

    /// <summary>
    /// Defines a <see cref="Quaternion"/> curve sampler that can be sampled with CUBIC interpolation.
    /// </summary>
    readonly struct QuaternionCubicSampler : ICurveSampler<Quaternion>, IConvertibleCurve<Quaternion>
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
            var (valA, valB, amount) = SamplerFactory.FindPairContainingOffset(_Sequence, offset);

            return SamplerFactory.InterpolateCubic
                (
                valA.Item2, valA.Item3,   // start, startTangentOut
                valB.Item2, valB.Item1,   // end, endTangentIn
                amount                               // amount
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

        public IReadOnlyDictionary<float, (Quaternion TangentIn, Quaternion Value, Quaternion TangentOut)> ToSplineCurve()
        {
            return _Sequence.ToDictionary(pair => pair.Item1, pair => pair.Item2);
        }

        public ICurveSampler<Quaternion> ToFastSampler()
        {
            var split = _Sequence
                .SplitByTime()
                .Select(item => new QuaternionCubicSampler(item))
                .Cast<ICurveSampler<Quaternion>>();

            return new FastSampler<Quaternion>(split);
        }

        #endregion
    }

    /// <summary>
    /// Defines a <see cref="Transforms.SparseWeight8"/> curve sampler that can be sampled with CUBIC interpolation.
    /// </summary>
    readonly struct SparseCubicSampler : ICurveSampler<Transforms.SparseWeight8>, IConvertibleCurve<Transforms.SparseWeight8>
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
            var (valA, valB, amount) = SamplerFactory.FindPairContainingOffset(_Sequence, offset);

            return Transforms.SparseWeight8.InterpolateCubic
                (
                valA.Item2, valA.Item3,   // start, startTangentOut
                valB.Item2, valB.Item1,   // end, endTangentIn
                amount                               // amount
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

        public IReadOnlyDictionary<float, (Transforms.SparseWeight8 TangentIn, Transforms.SparseWeight8 Value, Transforms.SparseWeight8 TangentOut)> ToSplineCurve()
        {
            return _Sequence.ToDictionary(pair => pair.Item1, pair => pair.Item2);
        }

        public ICurveSampler<Transforms.SparseWeight8> ToFastSampler()
        {
            var split = _Sequence
                .SplitByTime()
                .Select(item => new SparseCubicSampler(item))
                .Cast<ICurveSampler<Transforms.SparseWeight8>>();

            return new FastSampler<Transforms.SparseWeight8>(split);
        }

        #endregion
    }

    /// <summary>
    /// Defines a <see cref="float"/>[] curve sampler that can be sampled with CUBIC interpolation.
    /// </summary>
    readonly struct ArrayCubicSampler : ICurveSampler<float[]>, IConvertibleCurve<float[]>
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
            var (valA, valB, amount) = SamplerFactory.FindPairContainingOffset(_Sequence, offset);

            return SamplerFactory.InterpolateCubic
                (
                valA.Item2, valA.Item3,   // start, startTangentOut
                valB.Item2, valB.Item1,   // end, endTangentIn
                amount                               // amount
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

        public IReadOnlyDictionary<float, (float[] TangentIn, float[] Value, float[] TangentOut)> ToSplineCurve()
        {
            return _Sequence.ToDictionary(pair => pair.Item1, pair => pair.Item2);
        }

        public ICurveSampler<float[]> ToFastSampler()
        {
            var split = _Sequence
                .SplitByTime()
                .Select(item => new ArrayCubicSampler(item))
                .Cast<ICurveSampler<float[]>>();

            return new FastSampler<float[]>(split);
        }

        #endregion
    }
}
