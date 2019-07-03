using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace SharpGLTF.Transforms
{
    public delegate T CurveSampler<T>(Single time);

    internal static class AnimationSamplerFactory
    {
        /// <summary>
        /// Given a <paramref name="sequence"/> of float+<typeparamref name="T"/> pairs and a <paramref name="offset"/> time,
        /// it finds two consecutive values and the LERP amout.
        /// </summary>
        /// <typeparam name="T">The value type</typeparam>
        /// <param name="sequence">A sequence of float+<typeparamref name="T"/> pairs</param>
        /// <param name="offset">the time point within the sequence</param>
        /// <returns>Two consecutive <typeparamref name="T"/> values and a float amount to LERP them.</returns>
        private static (T, T, float) _FindSample<T>(this IEnumerable<(float, T)> sequence, float offset)
        {
            (float, T)? left = null;
            (float, T)? right = null;
            (float, T)? prev = null;

            if (offset < 0) offset = 0;

            foreach (var item in sequence)
            {
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

        internal static CurveSampler<T> CreateStepSamplerFunc<T>(this IEnumerable<(float, T)> collection)
        {
            if (collection == null) return null;

            T _sampler(float offset)
            {
                var sample = collection._FindSample(offset);
                return sample.Item1;
            }

            return _sampler;
        }

        internal static CurveSampler<Vector3> CreateLinearSamplerFunc(this IEnumerable<(float, Vector3)> collection)
        {
            if (collection == null) return null;

            Vector3 _sampler(float offset)
            {
                var sample = collection._FindSample(offset);
                return Vector3.Lerp(sample.Item1, sample.Item2, sample.Item3);
            }

            return _sampler;
        }

        internal static CurveSampler<Quaternion> CreateLinearSamplerFunc(this IEnumerable<(float, Quaternion)> collection)
        {
            if (collection == null) return null;

            Quaternion _sampler(float offset)
            {
                var sample = collection._FindSample(offset);
                return Quaternion.Slerp(sample.Item1, sample.Item2, sample.Item3);
            }

            return _sampler;
        }

        internal static CurveSampler<float[]> CreateLinearSamplerFunc(this IEnumerable<(float, float[])> collection)
        {
            if (collection == null) return null;

            float[] _sampler(float offset)
            {
                var sample = collection._FindSample(offset);
                var result = new float[sample.Item1.Length];

                for (int i = 0; i < result.Length; ++i)
                {
                    result[i] = (sample.Item1[i] * (1 - sample.Item3)) + (sample.Item2[i] * sample.Item3);
                }

                return result;
            }

            return _sampler;
        }

        internal static CurveSampler<Vector3> CreateSplineSamplerFunc(this IEnumerable<(float, (Vector3, Vector3, Vector3))> collection)
        {
            return CreateSplineSamplerFunc<Vector3>(collection, Hermite);
        }

        internal static CurveSampler<Quaternion> CreateSplineSamplerFunc(this IEnumerable<(float, (Quaternion, Quaternion, Quaternion))> collection)
        {
            return CreateSplineSamplerFunc<Quaternion>(collection, Hermite);
        }

        internal static CurveSampler<float[]> CreateSplineSamplerFunc(this IEnumerable<(float, (float[], float[], float[]))> collection)
        {
            return CreateSplineSamplerFunc<float[]>(collection, Hermite);
        }

        internal static CurveSampler<T> CreateSplineSamplerFunc<T>(this IEnumerable<(float, (T, T, T))> collection, Func<T, T, T, T, float, T> hermiteFunc)
        {
            if (collection == null) return null;

            T _sampler(float offset)
            {
                var sample = collection._FindSample(offset);

                return hermiteFunc(sample.Item1.Item2, sample.Item1.Item3, sample.Item2.Item2, sample.Item2.Item1, sample.Item3);
            }

            return _sampler;
        }

        internal static Vector3 Hermite(Vector3 start, Vector3 tangentOut, Vector3 end, Vector3 tangentIn, float amount)
        {
            var hermite = CalculateHermiteWeights(amount);

            return (start * hermite.Item1) + (end * hermite.Item2) + (tangentOut * hermite.Item3) + (tangentIn * hermite.Item4);
        }

        internal static Quaternion Hermite(Quaternion value1, Quaternion tangent1, Quaternion value2, Quaternion tangent2, float amount)
        {
            var hermite = CalculateHermiteWeights(amount);

            return Quaternion.Normalize((value1 * hermite.Item1) + (value2 * hermite.Item2) + (tangent1 * hermite.Item3) + (tangent2 * hermite.Item4));
        }

        internal static float[] Hermite(float[] value1, float[] tangent1, float[] value2, float[] tangent2, float amount)
        {
            var hermite = CalculateHermiteWeights(amount);

            var result = new float[value1.Length];

            for (int i = 0; i < result.Length; ++i)
            {
                result[i] = (value1[i] * hermite.Item1) + (value2[i] * hermite.Item2) + (tangent1[i] * hermite.Item3) + (tangent2[i] * hermite.Item4);
            }

            return result;
        }

        /// <summary>
        /// for a given cubic interpolation <paramref name="amount"/>, it calculates
        /// the weights to multiply each component:
        /// 1: Weight for Start point
        /// 2: Weight for End Tangent
        /// 3: Weight for Out Tangent
        /// 4: Weight for In Tangent
        /// </summary>
        /// <param name="amount">the input amount</param>
        /// <returns>the output weights</returns>
        public static (float, float, float, float) CalculateHermiteWeights(float amount)
        {
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
    }
}
