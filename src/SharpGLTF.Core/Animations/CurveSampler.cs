using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

using ROLIST = System.Collections.Generic.IReadOnlyList<float>;
using SEGMENT = System.ArraySegment<float>;
using SPARSE = SharpGLTF.Transforms.SparseWeight8;

namespace SharpGLTF.Animations
{
    /// <summary>
    /// Utility class to create samplers from curve collections.
    /// </summary>
    public static class CurveSampler
    {
        #region constants

        internal const string StepCurveError = "This is a step curve (MaxDegree = 0), use ToStepCurve(); instead.";
        internal const string LinearCurveError = "This is a linear curve (MaxDegree = 1), use ToLinearCurve(); instead.";
        internal const string SplineCurveError = "This is a spline curve (MaxDegree = 3), use ToSplineCurve(); instead.";

        internal static string CurveError(int maxDegree)
        {
            switch (maxDegree)
            {
                case 0: return StepCurveError;
                case 1: return LinearCurveError;
                case 3: return SplineCurveError;
                default: return "Invalid curve degree";
            }
        }

        #endregion

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
        public static (T A, T B, Single Amount) FindRangeContainingOffset<T>(this IEnumerable<(float Key, T Value)> sequence, float offset)
        {
            Guard.NotNull(sequence, nameof(sequence));

            sequence = sequence.EnsureList();

            if (!sequence.Any()) return (default(T), default(T), 0);

            (float Key, T Value)? left = null;
            (float Key, T Value)? right = null;
            (float Key, T Value)? prev = null;

            var (firstKey, _) = sequence.First();
            if (offset < firstKey) offset = firstKey;

            foreach (var item in sequence)
            {
                System.Diagnostics.Debug.Assert(!prev.HasValue || prev.Value.Key < item.Key, "Values in the sequence must be sorted ascending.");

                if (item.Key == offset)
                {
                    left = item; continue;
                }

                if (item.Key > offset)
                {
                    left ??= prev;
                    right = item;
                    break;
                }

                prev = item;
            }

            if (left == null && right == null)
            {
                // if both left and right are null is becase the sequence is empty,
                // or the offset went past the last item.
                if (prev.HasValue) return (prev.Value.Value, prev.Value.Value, 0);
                return (default(T), default(T), 0);
            }

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
        public static (Single A, Single B, Single Amount) FindRangeContainingOffset(IEnumerable<float> sequence, float offset)
        {
            Guard.NotNull(sequence, nameof(sequence));

            sequence = sequence.EnsureList();

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
                    left ??= prev;
                    right = item;
                    break;
                }

                prev = item;
            }

            if (left == null && right == null)
            {
                // if both left and right are null is becase the sequence is empty,
                // or the offset went past the last item.
                if (prev.HasValue) return (prev.Value, prev.Value, 0);
                return (0, 0, 0);
            }

            if (left == null) return (right.Value, right.Value, 0);
            if (right == null) return (left.Value, left.Value, 0);

            var delta = right.Value - left.Value;

            System.Diagnostics.Debug.Assert(delta > 0);

            var amount = (offset - left.Value) / delta;

            System.Diagnostics.Debug.Assert(amount >= 0 && amount <= 1);

            return (left.Value, right.Value, amount);
        }

        /// <summary>
        /// Splits the input sequence into chunks of 1 second for faster access
        /// </summary>
        /// <remarks>
        ///  The first and last keys outside the range of each chunk are duplicated, so each chunk can be evaluated for the whole second.
        /// </remarks>
        /// <typeparam name="T">The curve key type.</typeparam>
        /// <param name="sequence">A timed sequence of curve keys.</param>
        /// <returns>A sequence of 1 second chunks.</returns>
        internal static IEnumerable<(float, T)[]> SplitByTime<T>(this IEnumerable<(Single Time, T Value)> sequence)
        {
            List<(float, T)> segment = null;

            int time = 0;
            
            (Single Time, T Value) last = default;

            bool isFirst = true;

            foreach (var item in sequence)
            {
                if (isFirst)
                {
                    last = item;
                    segment ??= new List<(float, T)>();
                    isFirst = false;
                }

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

            if (segment != null && segment.Count > 0) yield return segment.ToArray();
        }

        #endregion

        #region interpolation utils

        public static Single[] Subtract(ROLIST left, ROLIST right)
        {
            Guard.NotNull(left, nameof(left));
            Guard.NotNull(right, nameof(right));
            Guard.MustBeEqualTo(right.Count, left.Count, nameof(right));

            var dst = new Single[left.Count];

            for (int i = 0; i < dst.Length; ++i)
            {
                dst[i] = left[i] - right[i];
            }

            return dst;
        }

        public static Single[] InterpolateLinear(ROLIST start, ROLIST end, Single amount)
        {
            Guard.NotNull(start, nameof(start));
            Guard.NotNull(end, nameof(end));

            var startW = 1 - amount;
            var endW = amount;

            var result = new float[start.Count];

            for (int i = 0; i < result.Length; ++i)
            {
                result[i] = (start[i] * startW) + (end[i] * endW);
            }

            return result;
        }

        public static Single InterpolateCubic(Single start, Single outgoingTangent, Single end, Single incomingTangent, Single amount)
        {
            var hermite = CreateHermitePointWeights(amount);

            return (start * hermite.StartPosition) + (end * hermite.EndPosition) + (outgoingTangent * hermite.StartTangent) + (incomingTangent * hermite.EndTangent);
        }

        public static Vector2 InterpolateCubic(Vector2 start, Vector2 outgoingTangent, Vector2 end, Vector2 incomingTangent, Single amount)
        {
            var hermite = CreateHermitePointWeights(amount);

            return (start * hermite.StartPosition) + (end * hermite.EndPosition) + (outgoingTangent * hermite.StartTangent) + (incomingTangent * hermite.EndTangent);
        }

        public static Vector3 InterpolateCubic(Vector3 start, Vector3 outgoingTangent, Vector3 end, Vector3 incomingTangent, Single amount)
        {
            var hermite = CreateHermitePointWeights(amount);

            return (start * hermite.StartPosition) + (end * hermite.EndPosition) + (outgoingTangent * hermite.StartTangent) + (incomingTangent * hermite.EndTangent);
        }

        public static Vector4 InterpolateCubic(Vector4 start, Vector4 outgoingTangent, Vector4 end, Vector4 incomingTangent, Single amount)
        {
            var hermite = CreateHermitePointWeights(amount);

            return (start * hermite.StartPosition) + (end * hermite.EndPosition) + (outgoingTangent * hermite.StartTangent) + (incomingTangent * hermite.EndTangent);
        }

        public static Quaternion InterpolateCubic(Quaternion start, Quaternion outgoingTangent, Quaternion end, Quaternion incomingTangent, Single amount)
        {
            var hermite = CreateHermitePointWeights(amount);

            return Quaternion.Normalize((start * hermite.StartPosition) + (end * hermite.EndPosition) + (outgoingTangent * hermite.StartTangent) + (incomingTangent * hermite.EndTangent));
        }

        public static Single[] InterpolateCubic(ROLIST start, ROLIST outgoingTangent, ROLIST end, ROLIST incomingTangent, Single amount)
        {
            Guard.NotNull(start, nameof(start));
            Guard.NotNull(outgoingTangent, nameof(outgoingTangent));
            Guard.NotNull(end, nameof(end));
            Guard.NotNull(incomingTangent, nameof(incomingTangent));

            var hermite = CreateHermitePointWeights(amount);

            var result = new float[start.Count];

            for (int i = 0; i < result.Length; ++i)
            {
                result[i] = (start[i] * hermite.StartPosition) + (end[i] * hermite.EndPosition) + (outgoingTangent[i] * hermite.StartTangent) + (incomingTangent[i] * hermite.EndTangent);
            }

            return result;
        }

        #endregion

        #region sampler creation

        private static bool _HasZero<T>(this IEnumerable<T> collection) { return collection == null || !collection.Any(); }
        private static bool _HasOne<T>(this IEnumerable<T> collection) { return !collection.Skip(1).Any(); }

        public static ICurveSampler<Single> CreateSampler(this IEnumerable<(Single, Single)> collection, bool isLinear = true, bool optimize = false)
        {
            if (collection._HasZero()) return null;
            if (collection._HasOne()) return FixedSampler<Single>.Create(collection);

            if (isLinear)
            {
                var sampler = new LinearSampler<Single>(collection, SamplerTraits.Scalar);
                return optimize ? sampler.ToFastSampler() : sampler;
            }
            else
            {
                var sampler = new StepSampler<Single>(collection, SamplerTraits.Scalar);
                return optimize ? sampler.ToFastSampler() : sampler;
            }
        }

        public static ICurveSampler<Vector2> CreateSampler(this IEnumerable<(Single, Vector2)> collection, bool isLinear = true, bool optimize = false)
        {
            if (collection._HasZero()) return null;
            if (collection._HasOne()) return FixedSampler<Vector2>.Create(collection);

            if (isLinear)
            {
                var sampler = new LinearSampler<Vector2>(collection, SamplerTraits.Vector2);
                return optimize ? sampler.ToFastSampler() : sampler;
            }
            else
            {
                var sampler = new StepSampler<Vector2>(collection, SamplerTraits.Vector2);
                return optimize ? sampler.ToFastSampler() : sampler;    
            }
        }

        public static ICurveSampler<Vector3> CreateSampler(this IEnumerable<(Single, Vector3)> collection, bool isLinear = true, bool optimize = false)
        {
            if (collection._HasZero()) return null;
            if (collection._HasOne()) return FixedSampler<Vector3>.Create(collection);

            if (isLinear)
            {
                var sampler = new LinearSampler<Vector3>(collection, SamplerTraits.Vector3);
                return optimize ? sampler.ToFastSampler() : sampler;
            }
            else
            {
                var sampler = new StepSampler<Vector3>(collection, SamplerTraits.Vector3);
                return optimize ? sampler.ToFastSampler() : sampler;
            }
        }

        public static ICurveSampler<Vector4> CreateSampler(this IEnumerable<(Single, Vector4)> collection, bool isLinear = true, bool optimize = false)
        {
            if (collection._HasZero()) return null;
            if (collection._HasOne()) return FixedSampler<Vector4>.Create(collection);

            if (isLinear)
            {
                var sampler = new LinearSampler<Vector4>(collection, SamplerTraits.Vector4);
                return optimize ? sampler.ToFastSampler() : sampler;
            }
            else
            {
                var sampler = new StepSampler<Vector4>(collection, SamplerTraits.Vector4);
                return optimize ? sampler.ToFastSampler() : sampler;
            }
        }

        public static ICurveSampler<Quaternion> CreateSampler(this IEnumerable<(Single, Quaternion)> collection, bool isLinear = true, bool optimize = false)
        {
            if (collection._HasZero()) return null;
            if (collection._HasOne()) return FixedSampler<Quaternion>.Create(collection);

            if (isLinear)
            {
                var sampler = new LinearSampler<Quaternion>(collection, SamplerTraits.Quaternion);
                return optimize ? sampler.ToFastSampler() : sampler;
            }
            else
            {
                var sampler = new StepSampler<Quaternion>(collection, SamplerTraits.Quaternion);
                return optimize ? sampler.ToFastSampler() : sampler;
            }
        }

        public static ICurveSampler<Single[]> CreateSampler(this IEnumerable<(Single, Single[])> collection, bool isLinear = true, bool optimize = false)
        {
            if (collection._HasZero()) return null;
            if (collection._HasOne()) return FixedSampler<Single[]>.Create(collection);

            if (isLinear)
            {
                var sampler = new LinearSampler<Single[]>(collection, SamplerTraits.Array);
                return optimize ? sampler.ToFastSampler() : sampler;
            }
            else
            {
                var sampler = new StepSampler<Single[]>(collection, SamplerTraits.Array);
                return optimize ? sampler.ToFastSampler() : sampler;
            }
        }

        public static ICurveSampler<SEGMENT> CreateSampler(this IEnumerable<(Single, SEGMENT)> collection, bool isLinear = true, bool optimize = false)
        {
            if (collection._HasZero()) return null;
            if (collection._HasOne()) return FixedSampler<SEGMENT>.Create(collection);

            if (isLinear)
            {
                var sampler = new LinearSampler<SEGMENT>(collection, SamplerTraits.Segment);
                return optimize ? sampler.ToFastSampler() : sampler;
            }
            else
            {
                var sampler = new StepSampler<SEGMENT>(collection, SamplerTraits.Segment);
                return optimize ? sampler.ToFastSampler() : sampler;
            }
        }

        public static ICurveSampler<SPARSE> CreateSampler(this IEnumerable<(Single, SPARSE)> collection, bool isLinear = true, bool optimize = false)
        {
            if (collection._HasZero()) return null;
            if (collection._HasOne()) return FixedSampler<SPARSE>.Create(collection);

            if (isLinear)
            {
                var sampler = new LinearSampler<SPARSE>(collection, SamplerTraits.Sparse);
                return optimize ? sampler.ToFastSampler() : sampler;
            }
            else
            {
                var sampler = new StepSampler<SPARSE>(collection, SamplerTraits.Sparse);
                return optimize ? sampler.ToFastSampler() : sampler;
            }
        }

        public static ICurveSampler<Single> CreateSampler(this IEnumerable<(Single, (Single, Single, Single))> collection, bool optimize = false)
        {
            if (collection._HasZero()) return null;
            if (collection._HasOne()) return FixedSampler<Single>.Create(collection);

            var sampler = new CubicSampler<Single>(collection, SamplerTraits.Scalar);
            return optimize ? sampler.ToFastSampler() : sampler;
        }

        public static ICurveSampler<Vector2> CreateSampler(this IEnumerable<(Single, (Vector2, Vector2, Vector2))> collection, bool optimize = false)
        {
            if (collection._HasZero()) return null;
            if (collection._HasOne()) return FixedSampler<Vector2>.Create(collection);

            var sampler = new CubicSampler<Vector2>(collection, SamplerTraits.Vector2);
            return optimize ? sampler.ToFastSampler() : sampler;
        }

        public static ICurveSampler<Vector3> CreateSampler(this IEnumerable<(Single, (Vector3, Vector3, Vector3))> collection, bool optimize = false)
        {
            if (collection._HasZero()) return null;
            if (collection._HasOne()) return FixedSampler<Vector3>.Create(collection);

            var sampler = new CubicSampler<Vector3>(collection, SamplerTraits.Vector3);
            return optimize ? sampler.ToFastSampler() : sampler;
        }

        public static ICurveSampler<Vector4> CreateSampler(this IEnumerable<(Single, (Vector4, Vector4, Vector4))> collection, bool optimize = false)
        {
            if (collection._HasZero()) return null;
            if (collection._HasOne()) return FixedSampler<Vector4>.Create(collection);

            var sampler = new CubicSampler<Vector4>(collection, SamplerTraits.Vector4);
            return optimize ? sampler.ToFastSampler() : sampler;
        }

        public static ICurveSampler<Quaternion> CreateSampler(this IEnumerable<(Single, (Quaternion, Quaternion, Quaternion))> collection, bool optimize = false)
        {
            if (collection._HasZero()) return null;
            if (collection._HasOne()) return FixedSampler<Quaternion>.Create(collection);

            var sampler = new CubicSampler<Quaternion>(collection, SamplerTraits.Quaternion);
            return optimize ? sampler.ToFastSampler() : sampler;
        }

        public static ICurveSampler<Single[]> CreateSampler(this IEnumerable<(Single, (Single[], Single[], Single[]))> collection, bool optimize = false)
        {
            if (collection._HasZero()) return null;
            if (collection._HasOne()) return FixedSampler<Single[]>.Create(collection);

            var sampler = new CubicSampler<Single[]>(collection, SamplerTraits.Array);
            return optimize ? sampler.ToFastSampler() : sampler;
        }

        public static ICurveSampler<SEGMENT> CreateSampler(this IEnumerable<(Single, (SEGMENT, SEGMENT, SEGMENT))> collection, bool optimize = false)
        {
            if (collection._HasZero()) return null;
            if (collection._HasOne()) return FixedSampler<SEGMENT>.Create(collection);

            var sampler = new CubicSampler<SEGMENT>(collection, SamplerTraits.Segment);
            return optimize ? sampler.ToFastSampler() : sampler;
        }

        public static ICurveSampler<SPARSE> CreateSampler(this IEnumerable<(Single, (SPARSE, SPARSE, SPARSE))> collection, bool optimize = false)
        {
            if (collection._HasZero()) return null;
            if (collection._HasOne()) return FixedSampler<SPARSE>.Create(collection);

            var sampler = new CubicSampler<SPARSE>(collection, SamplerTraits.Sparse);
            return optimize ? sampler.ToFastSampler() : sampler;
        }

        #endregion
    }
}
