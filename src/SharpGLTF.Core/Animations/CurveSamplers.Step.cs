using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SharpGLTF.Animations
{
    readonly struct StepSampler<T> :
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

            return new StepSampler<T>(clonedSequence, traits);
        }

        public StepSampler(IEnumerable<(float, T)> sequence, ISamplerTraits<T> traits)
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

        public int MaxDegree => 0;

        #endregion

        #region API

        public T GetPoint(float offset)
        {
            var (valA, _, _) = CurveSampler.FindRangeContainingOffset(_Sequence, offset);
            return _Traits.Clone(valA);
        }

        public IReadOnlyDictionary<float, T> ToStepCurve()
        {
            var traits = _Traits;
            return _Sequence.ToDictionary(pair => pair.Key, pair => traits.Clone(pair.Value));
        }

        public IReadOnlyDictionary<float, T> ToLinearCurve()
        {
            throw new NotSupportedException(CurveSampler.CurveError(MaxDegree));
        }

        public IReadOnlyDictionary<float, (T TangentIn, T Value, T TangentOut)> ToSplineCurve()
        {
            throw new NotSupportedException(CurveSampler.CurveError(MaxDegree));
        }

        public ICurveSampler<T> ToFastSampler()
        {
            var traits = _Traits;
            return FastCurveSampler<T>.CreateFrom(_Sequence, chunk => new StepSampler<T>(chunk, traits));
        }

        #endregion
    }
}
