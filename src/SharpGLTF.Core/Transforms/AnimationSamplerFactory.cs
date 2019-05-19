using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace SharpGLTF.Transforms
{
    internal static class AnimationSamplerFactory
    {
        private static (T, T, float) _GetSample<T>(this IEnumerable<(float, T)> sequence, float offset)
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

        internal static Func<float, Vector3> CreateLinearSamplerFunc(this IEnumerable<(float, Vector3)> collection)
        {
            if (collection == null) return null;

            Vector3 _sampler(float offset)
            {
                var sample = collection._GetSample(offset);
                return Vector3.Lerp(sample.Item1, sample.Item2, sample.Item3);
            }

            return _sampler;
        }

        internal static Func<float, Quaternion> CreateLinearSamplerFunc(this IEnumerable<(float, Quaternion)> collection)
        {
            if (collection == null) return null;

            Quaternion _sampler(float offset)
            {
                var sample = collection._GetSample(offset);
                return Quaternion.Slerp(sample.Item1, sample.Item2, sample.Item3);
            }

            return _sampler;
        }

        internal static Func<float, float[]> CreateLinearSamplerFunc(this IEnumerable<(float, float[])> collection)
        {
            if (collection == null) return null;

            float[] _sampler(float offset)
            {
                var sample = collection._GetSample(offset);
                var result = new float[sample.Item1.Length];

                for (int i = 0; i < result.Length; ++i)
                {
                    result[i] = sample.Item1[i] * (1 - sample.Item3) + sample.Item2[i] * sample.Item3;
                }

                return result;
            }

            return _sampler;
        }

        internal static Func<float, Vector3> CreateCubicSamplerFunc(this IEnumerable<(float, (Vector3, Vector3, Vector3))> collection)
        {
            return CreateCubicSamplerFunc<Vector3>(collection, Hermite);
        }

        internal static Func<float, Quaternion> CreateCubicSamplerFunc(this IEnumerable<(float, (Quaternion, Quaternion, Quaternion))> collection)
        {
            return CreateCubicSamplerFunc<Quaternion>(collection, Hermite);
        }

        internal static Func<float, T> CreateCubicSamplerFunc<T>(this IEnumerable<(float, (T, T, T))> collection, Func<T, T, T, T, float, T> hermiteFunc)
        {
            if (collection == null) return null;

            T _sampler(float offset)
            {
                var sample = collection._GetSample(offset);

                return hermiteFunc(sample.Item1.Item2, sample.Item1.Item3, sample.Item2.Item2, sample.Item2.Item1, sample.Item3);
            }

            return _sampler;
        }

        internal static Vector3 Hermite(Vector3 value1, Vector3 tangent1, Vector3 value2, Vector3 tangent2, float amount)
        {
            // http://mathworld.wolfram.com/HermitePolynomial.html

            var squared = amount * amount;
            var cubed = amount * squared;

            var part1 = (2.0f * cubed) - (3.0f * squared) + 1.0f;
            var part2 = (-2.0f * cubed) + (3.0f * squared);
            var part3 = (cubed - (2.0f * squared)) + amount;
            var part4 = cubed - squared;

            return (value1 * part1) + (value2 * part2) + (tangent1 * part3) + (tangent2 * part4);
        }

        internal static Quaternion Hermite(Quaternion value1, Quaternion tangent1, Quaternion value2, Quaternion tangent2, float amount)
        {
            // http://mathworld.wolfram.com/HermitePolynomial.html

            var squared = amount * amount;
            var cubed = amount * squared;

            var part1 = (2.0f * cubed) - (3.0f * squared) + 1.0f;
            var part2 = (-2.0f * cubed) + (3.0f * squared);
            var part3 = (cubed - (2.0f * squared)) + amount;
            var part4 = cubed - squared;

            return Quaternion.Normalize((value1 * part1) + (value2 * part2) + (tangent1 * part3) + (tangent2 * part4));
        }
    }
}
