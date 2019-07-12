using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;

namespace SharpGLTF.Animations
{
    /// <summary>
    /// Defines a curve that can be sampled at specific points.
    /// </summary>
    /// <typeparam name="T">The type of a point in the curve.</typeparam>
    public interface ICurveSampler<T>
    {
        T GetPoint(float offset);
    }

    /// <summary>
    /// Defines methods that convert the current curve to a Step, Linear or Spline curve.
    /// </summary>
    /// <typeparam name="T">The type of a point of the curve</typeparam>
    public interface IConvertibleCurve<T>
    {
        /// <summary>
        /// Gets a value indicating the maximum degree of the curve, current values are:
        /// 0: STEP.
        /// 1: LINEAR.
        /// 3: CUBIC.
        /// </summary>
        int Degree { get; }

        IReadOnlyDictionary<float, T> ToStepCurve();
        IReadOnlyDictionary<float, T> ToLinearCurve();
        IReadOnlyDictionary<float, (T, T, T)> ToSplineCurve();
    }

    /// <summary>
    /// Utility class to convert curve objects to curve samplers.
    /// </summary>
    public static class SamplerFactory
    {
        #region sampler utils

        public static Quaternion CreateTangent(this Quaternion fromValue, Quaternion toValue, float scale = 1)
        {
            var tangent = Quaternion.Concatenate(toValue, Quaternion.Inverse(fromValue));

            if (scale == 1) return tangent;

            // decompose into Axis - Angle pair
            var axis = Vector3.Normalize(new Vector3(tangent.X, tangent.Y, tangent.Z));
            var angle = Math.Acos(tangent.W) * 2;

            return Quaternion.CreateFromAxisAngle(axis, scale * (float)angle);
        }

        /// <summary>
        /// Calculates the Hermite point weights for a given <paramref name="amount"/>
        /// </summary>
        /// <param name="amount">The input amount (must be between 0 and 1)</param>
        /// <returns>
        /// The output weights.
        /// - Item1: Weight for Start point
        /// - Item2: Weight for End point
        /// - Item3: Weight for Start Outgoing Tangent
        /// - Item4: Weight for End Incoming Tangent
        /// </returns>
        public static (float, float, float, float) CreateHermitePointWeights(float amount)
        {
            System.Diagnostics.Debug.Assert(amount >= 0 && amount <= 1, nameof(amount));

            // http://mathworld.wolfram.com/HermitePolynomial.html

            // https://www.cubic.org/docs/hermite.htm

            var squared = amount * amount;
            var cubed = amount * squared;

            /*
            var part1 = (2.0f * cubed) - (3.0f * squared) + 1.0f;
            var part2 = (-2.0f * cubed) + (3.0f * squared);
            var part3 = cubed - (2.0f * squared) + amount;
            var part4 = cubed - squared;
            */

            var part2 = (3.0f * squared) - (2.0f * cubed);
            var part1 = 1 - part2;
            var part4 = cubed - squared;
            var part3 = part4 - squared + amount;

            return (part1, part2, part3, part4);
        }

        /// <summary>
        /// Calculates the Hermite tangent weights for a given <paramref name="amount"/>
        /// </summary>
        /// <param name="amount">The input amount (must be between 0 and 1)</param>
        /// <returns>
        /// The output weights.
        /// - Item1: Weight for Start point
        /// - Item2: Weight for End point
        /// - Item3: Weight for Start Outgoing Tangent
        /// - Item4: Weight for End Incoming Tangent
        /// </returns>
        public static (float, float, float, float) CreateHermiteTangentWeights(float amount)
        {
            System.Diagnostics.Debug.Assert(amount >= 0 && amount <= 1, nameof(amount));

            // https://math.stackexchange.com/questions/1270776/how-to-find-tangent-at-any-point-along-a-cubic-hermite-spline

            var squared = amount * amount;

            /*
            var part1 = (6 * squared) - (6 * amount);
            var part2 = -(6 * squared) + (6 * amount);
            var part3 = (3 * squared) - (4 * amount) + 1;
            var part4 = (3 * squared) - (2 * amount);
            */

            var part1 = (6 * squared) - (6 * amount);
            var part2 = -part1;
            var part3 = (3 * squared) - (4 * amount) + 1;
            var part4 = (3 * squared) - (2 * amount);

            return (part1, part2, part3, part4);
        }

        /// <summary>
        /// Given a <paramref name="sequence"/> of float+<typeparamref name="T"/> pairs and an <paramref name="offset"/>,
        /// it finds two consecutive values that contain <paramref name="offset"/> between them.
        /// </summary>
        /// <typeparam name="T">The value type</typeparam>
        /// <param name="sequence">A sequence of float+<typeparamref name="T"/> pairs sorted in ascending order.</param>
        /// <param name="offset">the offset to look for in the sequence.</param>
        /// <returns>Two consecutive <typeparamref name="T"/> values and a float amount to LERP amount.</returns>
        public static (T, T, float) FindPairContainingOffset<T>(this IEnumerable<(float, T)> sequence, float offset)
        {
            if (!sequence.Any()) return (default(T), default(T), 0);

            (float, T)? left = null;
            (float, T)? right = null;
            (float, T)? prev = null;

            var first = sequence.First();
            if (offset < first.Item1) offset = first.Item1;

            foreach (var item in sequence)
            {
                System.Diagnostics.Debug.Assert(!prev.HasValue || prev.Value.Item1 < item.Item1, "Values in the sequence must be sorted ascending.");

                if (item.Item1 == offset)
                {
                    left = item; continue;
                }

                if (item.Item1 > offset)
                {
                    if (left == null) left = prev;
                    right = item;
                    break;
                }

                prev = item;
            }

            if (left == null && right == null) return (default(T), default(T), 0);
            if (left == null) return (right.Value.Item2, right.Value.Item2, 0);
            if (right == null) return (left.Value.Item2, left.Value.Item2, 0);

            var delta = right.Value.Item1 - left.Value.Item1;

            System.Diagnostics.Debug.Assert(delta > 0);

            var amount = (offset - left.Value.Item1) / delta;

            System.Diagnostics.Debug.Assert(amount >= 0 && amount <= 1);

            return (left.Value.Item2, right.Value.Item2, amount);
        }

        /// <summary>
        /// Given a <paramref name="sequence"/> of offsets and an <paramref name="offset"/>,
        /// it finds two consecutive offsets that contain <paramref name="offset"/> between them.
        /// </summary>
        /// <param name="sequence">A sequence of offsets sorted in ascending order.</param>
        /// <param name="offset">the offset to look for in the sequence.</param>
        /// <returns>Two consecutive offsets and a LERP amount.</returns>
        public static (float, float, float) FindPairContainingOffset(IEnumerable<float> sequence, float offset)
        {
            if (!sequence.Any()) return (0, 0, 0);

            float? left = null;
            float? right = null;
            float? prev = null;

            var first = sequence.First();
            if (offset < first) offset = first;

            foreach (var item in sequence)
            {
                System.Diagnostics.Debug.Assert(!prev.HasValue || prev.Value < item, "Values in the sequence must be sorted ascending.");

                if (item == offset)
                {
                    left = item; continue;
                }

                if (item > offset)
                {
                    if (left == null) left = prev;
                    right = item;
                    break;
                }

                prev = item;
            }

            if (left == null && right == null) return (0, 0, 0);
            if (left == null) return (right.Value, right.Value, 0);
            if (right == null) return (left.Value, left.Value, 0);

            var delta = right.Value - left.Value;

            System.Diagnostics.Debug.Assert(delta > 0);

            var amount = (offset - left.Value) / delta;

            System.Diagnostics.Debug.Assert(amount >= 0 && amount <= 1);

            return (left.Value, right.Value, amount);
        }

        #endregion

        #region Extensions

        public static ICurveSampler<Vector3> CreateSampler(this IEnumerable<(float, Vector3)> collection, bool isLinear = true)
        {
            if (collection == null) return null;

            return new Vector3LinearSampler(collection, isLinear);
        }

        public static ICurveSampler<Quaternion> CreateSampler(this IEnumerable<(float, Quaternion)> collection, bool isLinear = true)
        {
            if (collection == null) return null;

            return new QuaternionLinearSampler(collection, isLinear);
        }

        public static ICurveSampler<float[]> CreateSampler(this IEnumerable<(float, float[])> collection, bool isLinear = true)
        {
            if (collection == null) return null;

            return new ArrayLinearSampler(collection, isLinear);
        }

        public static ICurveSampler<Vector3> CreateSampler(this IEnumerable<(float, (Vector3, Vector3, Vector3))> collection)
        {
            if (collection == null) return null;

            return new Vector3CubicSampler(collection);
        }

        public static ICurveSampler<Quaternion> CreateSampler(this IEnumerable<(float, (Quaternion, Quaternion, Quaternion))> collection)
        {
            if (collection == null) return null;

            return new QuaternionCubicSampler(collection);
        }

        public static ICurveSampler<float[]> CreateSampler(this IEnumerable<(float, (float[], float[], float[]))> collection)
        {
            if (collection == null) return null;

            return new ArrayCubicSampler(collection);
        }

        #endregion
    }

    /// <summary>
    /// Defines a <see cref="Vector3"/> curve sampler that can be sampled with STEP or LINEAR interpolations.
    /// </summary>
    struct Vector3LinearSampler : ICurveSampler<Vector3>, IConvertibleCurve<Vector3>
    {
        public Vector3LinearSampler(IEnumerable<(float, Vector3)> sequence, bool isLinear)
        {
            _Sequence = sequence;
            _Linear = isLinear;
        }

        private readonly IEnumerable<(float, Vector3)> _Sequence;
        private readonly Boolean _Linear;

        public int Degree => _Linear ? 1 : 0;

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
    }

    /// <summary>
    /// Defines a <see cref="Quaternion"/> curve sampler that can be sampled with STEP or LINEAR interpolations.
    /// </summary>
    struct QuaternionLinearSampler : ICurveSampler<Quaternion>, IConvertibleCurve<Quaternion>
    {
        public QuaternionLinearSampler(IEnumerable<(float, Quaternion)> sequence, bool isLinear)
        {
            _Sequence = sequence;
            _Linear = isLinear;
        }

        private readonly IEnumerable<(float, Quaternion)> _Sequence;
        private readonly Boolean _Linear;

        public int Degree => _Linear ? 1 : 0;

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
    }

    /// <summary>
    /// Defines a <see cref="float"/>[] curve sampler that can be sampled with STEP or LINEAR interpolations.
    /// </summary>
    struct ArrayLinearSampler : ICurveSampler<float[]>, IConvertibleCurve<float[]>
    {
        public ArrayLinearSampler(IEnumerable<(float, float[])> sequence, bool isLinear)
        {
            _Sequence = sequence;
            _Linear = isLinear;
        }

        private readonly IEnumerable<(float, float[])> _Sequence;
        private readonly Boolean _Linear;

        public int Degree => _Linear ? 1 : 0;

        public float[] GetPoint(float offset)
        {
            var segment = SamplerFactory.FindPairContainingOffset(_Sequence, offset);

            if (!_Linear) return segment.Item1;

            var result = new float[segment.Item1.Length];

            for (int i = 0; i < result.Length; ++i)
            {
                result[i] = (segment.Item1[i] * (1 - segment.Item3)) + (segment.Item2[i] * segment.Item3);
            }

            return result;
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
    }

    /// <summary>
    /// Defines a <see cref="Vector3"/> curve sampler that can be sampled with CUBIC interpolation.
    /// </summary>
    struct Vector3CubicSampler : ICurveSampler<Vector3>, IConvertibleCurve<Vector3>
    {
        public Vector3CubicSampler(IEnumerable<(float, (Vector3, Vector3, Vector3))> sequence)
        {
            _Sequence = sequence;
        }

        private readonly IEnumerable<(float, (Vector3, Vector3, Vector3))> _Sequence;

        public int Degree => 3;

        public Vector3 GetPoint(float offset)
        {
            var segment = SamplerFactory.FindPairContainingOffset(_Sequence, offset);

            var hermite = SamplerFactory.CreateHermitePointWeights(segment.Item3);

            var start = segment.Item1.Item2;
            var tangentOut = segment.Item1.Item3;
            var tangentIn = segment.Item2.Item1;
            var end = segment.Item2.Item2;

            return (start * hermite.Item1) + (end * hermite.Item2) + (tangentOut * hermite.Item3) + (tangentIn * hermite.Item4);
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
    }

    /// <summary>
    /// Defines a <see cref="Quaternion"/> curve sampler that can be sampled with CUBIC interpolation.
    /// </summary>
    struct QuaternionCubicSampler : ICurveSampler<Quaternion>, IConvertibleCurve<Quaternion>
    {
        public QuaternionCubicSampler(IEnumerable<(float, (Quaternion, Quaternion, Quaternion))> sequence)
        {
            _Sequence = sequence;
        }

        private readonly IEnumerable<(float, (Quaternion, Quaternion, Quaternion))> _Sequence;

        public int Degree => 3;

        public Quaternion GetPoint(float offset)
        {
            var segment = SamplerFactory.FindPairContainingOffset(_Sequence, offset);

            var hermite = SamplerFactory.CreateHermitePointWeights(segment.Item3);

            var start = segment.Item1.Item2;
            var tangentOut = segment.Item1.Item3;
            var tangentIn = segment.Item2.Item1;
            var end = segment.Item2.Item2;

            return Quaternion.Normalize((start * hermite.Item1) + (end * hermite.Item2) + (tangentOut * hermite.Item3) + (tangentIn * hermite.Item4));
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
    }

    /// <summary>
    /// Defines a <see cref="float"/>[] curve sampler that can be sampled with CUBIC interpolation.
    /// </summary>
    struct ArrayCubicSampler : ICurveSampler<float[]>, IConvertibleCurve<float[]>
    {
        public ArrayCubicSampler(IEnumerable<(float, (float[], float[], float[]))> sequence)
        {
            _Sequence = sequence;
        }

        private readonly IEnumerable<(float, (float[], float[], float[]))> _Sequence;

        public int Degree => 3;

        public float[] GetPoint(float offset)
        {
            var segment = SamplerFactory.FindPairContainingOffset(_Sequence, offset);

            var hermite = SamplerFactory.CreateHermitePointWeights(segment.Item3);

            var start = segment.Item1.Item2;
            var tangentOut = segment.Item1.Item3;
            var tangentIn = segment.Item2.Item1;
            var end = segment.Item2.Item2;

            var result = new float[start.Length];

            for (int i = 0; i < result.Length; ++i)
            {
                result[i] = (start[i] * hermite.Item1) + (end[i] * hermite.Item2) + (tangentOut[i] * hermite.Item3) + (tangentIn[i] * hermite.Item4);
            }

            return result;
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
    }
}
