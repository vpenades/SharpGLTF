using System;
using System.Collections.Generic;
using System.Linq;

namespace SharpGLTF.Animations
{
    readonly struct LinearSampler<T> :
        ICurveSampler<T>,
        IConvertibleCurve<T>
    {
        #region lifecycle

        public IConvertibleCurve<T> Clone()
        {
            var traits = _Traits;
            var clonedSequence = _Sequence
                .Select(pair => (pair.Key, traits.Clone(pair.Value)))
                .ToArray();

            return new LinearSampler<T>(clonedSequence, traits);
        }

        public LinearSampler(IEnumerable<(float, T)> sequence, ISamplerTraits<T> traits)
        {
            _Sequence = sequence;
            _Traits = traits;
        }

        #endregion

        #region data

        [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)]
        private readonly ISamplerTraits<T> _Traits;

        [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.RootHidden)]
        private readonly IEnumerable<(float Key, T Value)> _Sequence;
        public int MaxDegree => 1;

        #endregion

        #region API

        public T GetPoint(float offset)
        {
            var (valA, valB, amount) = CurveSampler.FindRangeContainingOffset(_Sequence, offset);

            return _Traits.InterpolateLinear(valA, valB, amount);
        }

        public IReadOnlyDictionary<float, T> ToStepCurve()
        {
            throw new NotSupportedException(CurveSampler.CurveError(MaxDegree));
        }

        public IReadOnlyDictionary<float, T> ToLinearCurve()
        {
            var traits = _Traits;
            return _Sequence.ToDictionary(pair => pair.Key, pair => traits.Clone(pair.Value));
        }

        public IReadOnlyDictionary<float, (T TangentIn, T Value, T TangentOut)> ToSplineCurve()
        {
            throw new NotSupportedException(CurveSampler.CurveError(MaxDegree));
        }

        public ICurveSampler<T> ToFastSampler()
        {
            var traits = _Traits;
            return FastCurveSampler<T>.CreateFrom(_Sequence, chunk => new LinearSampler<T>(chunk, traits));
        }

        #endregion
    }
}
