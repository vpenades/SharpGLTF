using System;
using System.Collections.Generic;
using System.Linq;

namespace SharpGLTF.Animations
{
    readonly struct CubicSampler<T> :
        ICurveSampler<T>,
        IConvertibleCurve<T>
    {
        #region lifecycle

        public IConvertibleCurve<T> Clone()
        {
            var traits = _Traits;
            var clonedSequence = _Sequence.Select
                (
                pair =>
                    (
                    pair.Key,
                    (traits.Clone(pair.Item2.TangentIn), traits.Clone(pair.Item2.Value), traits.Clone(pair.Item2.TangentOut))
                    )
                ).ToArray();

            return new CubicSampler<T>(clonedSequence, traits);
        }

        public CubicSampler(IEnumerable<(float, (T, T, T))> sequence, ISamplerTraits<T> traits)
        {
            _Sequence = sequence;
            _Traits = traits;
        }

        #endregion

        #region data

        [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)]
        private readonly ISamplerTraits<T> _Traits;

        [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.RootHidden)]
        private readonly IEnumerable<(float Key, (T TangentIn, T Value, T TangentOut))> _Sequence;

        #endregion

        #region API

        public int MaxDegree => 3;

        public T GetPoint(float offset)
        {
            var (valA, valB, amount) = CurveSampler.FindRangeContainingOffset(_Sequence, offset);

            return _Traits.InterpolateCubic
                (
                valA.Value, valA.TangentOut,   // start, startTangentOut
                valB.Value, valB.TangentIn,   // end, endTangentIn
                amount                    // amount
                );
        }

        IReadOnlyDictionary<float, T> IConvertibleCurve<T>.ToStepCurve()
        {
            throw new NotSupportedException(CurveSampler.SplineCurveError);
        }

        IReadOnlyDictionary<float, T> IConvertibleCurve<T>.ToLinearCurve()
        {
            throw new NotSupportedException(CurveSampler.SplineCurveError);
        }

        IReadOnlyDictionary<float, (T TangentIn, T Value, T TangentOut)> IConvertibleCurve<T>.ToSplineCurve()
        {
            var traits = _Traits;

            return _Sequence.ToDictionary
                (
                pair => pair.Key,
                pair => (traits.Clone(pair.Item2.TangentIn), traits.Clone(pair.Item2.Value), traits.Clone(pair.Item2.TangentOut))
                );
        }

        public ICurveSampler<T> ToFastSampler()
        {
            var traits = _Traits;
            return FastCurveSampler<T>.CreateFrom(_Sequence, chunk => new CubicSampler<T>(chunk, traits));
        }

        #endregion
    }
}
