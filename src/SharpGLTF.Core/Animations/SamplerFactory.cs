using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace SharpGLTF.Animations
{
    /// <summary>
    /// Utility class to create samplers from curve collections.
    /// </summary>
    public static class SamplerFactory
    {
        #region sampler utils

        public static Vector3 CreateTangent(Vector3 fromValue, Vector3 toValue, Single scale = 1)
        {
            return (toValue - fromValue) * scale;
        }

        public static Quaternion CreateTangent(Quaternion fromValue, Quaternion toValue, Single scale = 1)
        {
            var tangent = Quaternion.Concatenate(toValue, Quaternion.Inverse(fromValue));

            if (scale == 1) return tangent;

            // decompose into Axis - Angle pair
            var axis = Vector3.Normalize(new Vector3(tangent.X, tangent.Y, tangent.Z));
            var angle = Math.Acos(tangent.W) * 2;

            return Quaternion.CreateFromAxisAngle(axis, scale * (float)angle);
        }

        public static Single[] CreateTangent(Single[] fromValue, Single[] toValue, Single scale = 1)
        {
            Guard.NotNull(fromValue, nameof(fromValue));
            Guard.NotNull(toValue, nameof(toValue));
            Guard.IsTrue(fromValue.Length == toValue.Length, nameof(toValue));

            var r = new float[fromValue.Length];

            for (int i = 0; i < r.Length; ++i)
            {
                r[i] = (toValue[i] - fromValue[i]) * scale;
            }

            return r;
        }

        /// <summary>
        /// Calculates the Hermite point weights for a given <paramref name="amount"/>
        /// </summary>
        /// <param name="amount">The input amount (must be between 0 and 1)</param>
        /// <returns>
        /// The output weights.
        /// - StartPosition: Weight for Start point
        /// - EndPosition: Weight for End point
        /// - StartTangent: Weight for Start Outgoing Tangent
        /// - EndTangent: Weight for End Incoming Tangent
        /// </returns>
        public static (float StartPosition, float EndPosition, float StartTangent, float EndTangent) CreateHermitePointWeights(float amount)
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
        /// - StartPosition: Weight for Start point
        /// - EndPosition: Weight for End point
        /// - StartTangent: Weight for Start Outgoing Tangent
        /// - EndTangent: Weight for End Incoming Tangent
        /// </returns>
        public static (float StartPosition, float EndPosition, float StartTangent, float EndTangent) CreateHermiteTangentWeights(float amount)
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
        public static (T A, T B, Single Amount) FindPairContainingOffset<T>(this IEnumerable<(float Key, T Value)> sequence, float offset)
        {
            Guard.NotNull(sequence, nameof(sequence));

            if (!sequence.Any()) return (default(T), default(T), 0);

            (float Key, T Value)? left = null;
            (float Key, T Value)? right = null;
            (float Key, T Value)? prev = null;

            var first = sequence.First();
            if (offset < first.Key) offset = first.Key;

            foreach (var item in sequence)
            {
                System.Diagnostics.Debug.Assert(condition: !prev.HasValue || prev.Value.Key < item.Key, "Values in the sequence must be sorted ascending.");

                if (item.Key == offset)
                {
                    left = item; continue;
                }

                if (item.Key > offset)
                {
                    if (left == null) left = prev;
                    right = item;
                    break;
                }

                prev = item;
            }

            if (left == null && right == null) return (default(T), default(T), 0);
            if (left == null) return (right.Value.Value, right.Value.Value, 0);
            if (right == null) return (left.Value.Value, left.Value.Value, 0);

            var delta = right.Value.Key - left.Value.Key;

            System.Diagnostics.Debug.Assert(delta > 0);

            var amount = (offset - left.Value.Key) / delta;

            System.Diagnostics.Debug.Assert(amount >= 0 && amount <= 1);

            return (left.Value.Value, right.Value.Value, amount);
        }

        /// <summary>
        /// Given a <paramref name="sequence"/> of offsets and an <paramref name="offset"/>,
        /// it finds two consecutive offsets that contain <paramref name="offset"/> between them.
        /// </summary>
        /// <param name="sequence">A sequence of offsets sorted in ascending order.</param>
        /// <param name="offset">the offset to look for in the sequence.</param>
        /// <returns>Two consecutive offsets and a LERP amount.</returns>
        public static (Single A, Single B, Single Amount) FindPairContainingOffset(IEnumerable<float> sequence, float offset)
        {
            Guard.NotNull(sequence, nameof(sequence));

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

        internal static IEnumerable<(float, T)[]> SplitByTime<T>(this IEnumerable<(Single Time, T Value)> sequence)
        {
            if (!sequence.Any()) yield break;

            var segment = new List<(float, T)>();
            int time = 0;

            var last = sequence.First();

            foreach (var item in sequence)
            {
                var t = (int)item.Time;

                if (time > t) throw new InvalidOperationException("unexpected data encountered.");

                while (time < t)
                {
                    if (segment.Count == 0 && item.Time > last.Time) segment.Add(last);

                    segment.Add(item);
                    yield return segment.ToArray();
                    segment.Clear();

                    ++time;
                }

                if (time == t)
                {
                    if (segment.Count == 0 && time > (int)last.Time && time < item.Time) segment.Add(last);
                    segment.Add(item);
                }

                last = item;
            }

            if (segment.Count > 0) yield return segment.ToArray();
        }

        #endregion

        #region interpolation utils

        public static Single[] InterpolateLinear(Single[] start, Single[] end, Single amount)
        {
            Guard.NotNull(start, nameof(start));
            Guard.NotNull(end, nameof(end));

            var startW = 1 - amount;
            var endW = amount;

            var result = new float[start.Length];

            for (int i = 0; i < result.Length; ++i)
            {
                result[i] = (start[i] * startW) + (end[i] * endW);
            }

            return result;
        }

        public static Vector3 InterpolateCubic(Vector3 start, Vector3 outgoingTangent, Vector3 end, Vector3 incomingTangent, Single amount)
        {
            var hermite = CreateHermitePointWeights(amount);

            return (start * hermite.StartPosition) + (end * hermite.EndPosition) + (outgoingTangent * hermite.StartTangent) + (incomingTangent * hermite.EndTangent);
        }

        public static Quaternion InterpolateCubic(Quaternion start, Quaternion outgoingTangent, Quaternion end, Quaternion incomingTangent, Single amount)
        {
            var hermite = CreateHermitePointWeights(amount);

            return Quaternion.Normalize((start * hermite.StartPosition) + (end * hermite.EndPosition) + (outgoingTangent * hermite.StartTangent) + (incomingTangent * hermite.EndTangent));
        }

        public static Single[] InterpolateCubic(Single[] start, Single[] outgoingTangent, Single[] end, Single[] incomingTangent, Single amount)
        {
            Guard.NotNull(start, nameof(start));
            Guard.NotNull(outgoingTangent, nameof(outgoingTangent));
            Guard.NotNull(end, nameof(end));
            Guard.NotNull(incomingTangent, nameof(incomingTangent));

            var hermite = CreateHermitePointWeights(amount);

            var result = new float[start.Length];

            for (int i = 0; i < result.Length; ++i)
            {
                result[i] = (start[i] * hermite.StartPosition) + (end[i] * hermite.EndPosition) + (outgoingTangent[i] * hermite.StartTangent) + (incomingTangent[i] * hermite.EndTangent);
            }

            return result;
        }

        #endregion

        #region sampler creation

        internal static IEnumerable<T> Isolate<T>(this IEnumerable<T> collection, bool isolateMemory)
        {
            if (isolateMemory) collection = collection.ToArray();
            return collection;
        }

        public static ICurveSampler<Vector3> CreateSampler(this IEnumerable<(Single, Vector3)> collection, bool isLinear = true, bool optimize = false)
        {
            if (collection == null) return null;

            var single = SingleValueSampler<Vector3>.CreateForSingle(collection);
            if (single != null) return single;

            var sampler = new Vector3LinearSampler(collection, isLinear);

            return optimize ? sampler.ToFastSampler() : sampler;
        }

        public static ICurveSampler<Quaternion> CreateSampler(this IEnumerable<(Single, Quaternion)> collection, bool isLinear = true, bool optimize = false)
        {
            if (collection == null) return null;

            var single = SingleValueSampler<Quaternion>.CreateForSingle(collection);
            if (single != null) return single;

            var sampler = new QuaternionLinearSampler(collection, isLinear);

            return optimize ? sampler.ToFastSampler() : sampler;
        }

        public static ICurveSampler<Transforms.SparseWeight8> CreateSampler(this IEnumerable<(Single, Transforms.SparseWeight8)> collection, bool isLinear = true, bool optimize = false)
        {
            if (collection == null) return null;

            var single = SingleValueSampler<Transforms.SparseWeight8>.CreateForSingle(collection);
            if (single != null) return single;

            var sampler = new SparseLinearSampler(collection, isLinear);

            return optimize ? sampler.ToFastSampler() : sampler;
        }

        public static ICurveSampler<Single[]> CreateSampler(this IEnumerable<(Single, Single[])> collection, bool isLinear = true, bool optimize = false)
        {
            if (collection == null) return null;

            var single = SingleValueSampler<Single[]>.CreateForSingle(collection);
            if (single != null) return single;

            var sampler = new ArrayLinearSampler(collection, isLinear);

            return optimize ? sampler.ToFastSampler() : sampler;
        }

        public static ICurveSampler<Vector3> CreateSampler(this IEnumerable<(Single, (Vector3, Vector3, Vector3))> collection, bool optimize = false)
        {
            if (collection == null) return null;

            var single = SingleValueSampler<Vector3>.CreateForSingle(collection);
            if (single != null) return single;

            var sampler = new Vector3CubicSampler(collection);

            return optimize ? sampler.ToFastSampler() : sampler;
        }

        public static ICurveSampler<Quaternion> CreateSampler(this IEnumerable<(Single, (Quaternion, Quaternion, Quaternion))> collection, bool optimize = false)
        {
            if (collection == null) return null;

            var single = SingleValueSampler<Quaternion>.CreateForSingle(collection);
            if (single != null) return single;

            var sampler = new QuaternionCubicSampler(collection);

            return optimize ? sampler.ToFastSampler() : sampler;
        }

        public static ICurveSampler<Transforms.SparseWeight8> CreateSampler(this IEnumerable<(Single, (Transforms.SparseWeight8, Transforms.SparseWeight8, Transforms.SparseWeight8))> collection, bool optimize = false)
        {
            if (collection == null) return null;

            var single = SingleValueSampler<Transforms.SparseWeight8>.CreateForSingle(collection);
            if (single != null) return single;

            var sampler = new SparseCubicSampler(collection);

            return optimize ? sampler.ToFastSampler() : sampler;
        }

        public static ICurveSampler<Single[]> CreateSampler(this IEnumerable<(Single, (Single[], Single[], Single[]))> collection, bool optimize = false)
        {
            if (collection == null) return null;

            var single = SingleValueSampler<Single[]>.CreateForSingle(collection);
            if (single != null) return single;

            var sampler = new ArrayCubicSampler(collection);

            return optimize ? sampler.ToFastSampler() : sampler;
        }

        #endregion
    }
}
