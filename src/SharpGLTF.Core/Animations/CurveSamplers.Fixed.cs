using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SharpGLTF.Animations
{
    /// <summary>
    /// Represents a special sampler for single values.
    /// </summary>
    /// <typeparam name="T">The sample type.</typeparam>
    readonly struct FixedSampler<T> :
        ICurveSampler<T>,
        IConvertibleCurve<T>
    {
        #region lifecycle

        public static ICurveSampler<T> Create(IEnumerable<(float Key, T Value)> sequence)
        {
            System.Diagnostics.Debug.Assert(!sequence.Skip(1).Any());
            return new FixedSampler<T>(sequence.First().Value);
        }

        public static ICurveSampler<T> Create(IEnumerable<(float Key, (T, T, T) Value)> sequence)
        {
            System.Diagnostics.Debug.Assert(!sequence.Skip(1).Any());
            return new FixedSampler<T>(sequence.First().Value.Item2);
        }

        public IConvertibleCurve<T> Clone()
        {
            return new FixedSampler<T>(_Value);
        }

        private FixedSampler(T value)
        {
            _Value = value;
        }

        #endregion

        #region data

        private readonly T _Value;

        #endregion

        #region API

        public int MaxDegree => 0;

        public T GetPoint(float offset) { return _Value; }

        public IReadOnlyDictionary<float, T> ToStepCurve()
        {
            return new Dictionary<float, T> { [0] = _Value };
        }

        public IReadOnlyDictionary<float, T> ToLinearCurve()
        {
            return new Dictionary<float, T> { [0] = _Value };
        }

        public IReadOnlyDictionary<float, (T TangentIn, T Value, T TangentOut)> ToSplineCurve()
        {
            return new Dictionary<float, (T TangentIn, T Value, T TangentOut)> { [0] = (default, _Value, default) };
        }

        #endregion
    }
}
