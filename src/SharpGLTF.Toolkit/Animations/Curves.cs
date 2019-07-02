using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace SharpGLTF.Animations
{
    // TODO: we could support conversions between linear and cubic (with hermite regression)

    public interface ICurveSampler<T>
        where T : struct
    {
        T GetSample(float offset);
    }

    public interface ICurveWriter<T>
        where T : struct
    {
        void RemoveKey(float key);

        void SetControlPoint(float key, T value);
    }

    public interface ICubicCurveWriter<T> : ICurveWriter<T>
        where T : struct
    {
        void SetControlPointIn(float key, T value);
        void SetControlPointOut(float key, T value);

        void SetTangentIn(float key, T value);
        void SetTangentOut(float key, T value);
    }

    public abstract class Curve<Tin, Tout> : ICurveSampler<Tout>
        where Tin : struct
        where Tout : struct
    {
        #region data

        private SortedDictionary<float, Tin> _Keys = new SortedDictionary<float, Tin>();

        #endregion

        #region properties

        public IReadOnlyCollection<float> Keys => _Keys.Keys;

        #endregion

        #region API

        public void RemoveKey(float key) { _Keys.Remove(key); }

        protected Tin? GetKey(float key) { return _Keys.TryGetValue(key, out Tin value) ? value : (Tin?)null; }

        protected void SetKey(float key, Tin value) { _Keys[key] = value; }

        protected (Tin, Tin, float) FindSample(float offset)
        {
            return _FindSample(_Keys, offset);
        }

        public abstract Tout GetSample(float offset);

        /// <summary>
        /// Given a <paramref name="sequence"/> of float+<typeparamref name="T"/> pairs and a <paramref name="offset"/> time,
        /// it finds two consecutive values and the LERP amout.
        /// </summary>
        /// <typeparam name="T">The value type</typeparam>
        /// <param name="sequence">A sequence of float+<typeparamref name="T"/> pairs</param>
        /// <param name="offset">the time point within the sequence</param>
        /// <returns>Two consecutive <typeparamref name="T"/> values and a float amount to LERP them.</returns>
        private static (T, T, float) _FindSample<T>(IEnumerable<KeyValuePair<float, T>> sequence, float offset)
        {
            KeyValuePair<float, T>? left = null;
            KeyValuePair<float, T>? right = null;
            KeyValuePair<float, T>? prev = null;

            if (offset < 0) offset = 0;

            foreach (var item in sequence)
            {
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

        #endregion
    }

    class SingleLinearCurve : Curve<Single, Single>, ICurveWriter<Single>
    {
        #region API

        public override Single GetSample(float offset)
        {
            var sample = FindSample(offset);

            return sample.Item1 * (1-sample.Item3) + (sample.Item2 * sample.Item3);
        }

        public void SetControlPoint(float offset, Single value)
        {
            SetKey(offset, value);
        }

        #endregion
    }

    class SingleCubicCurve : Curve<(Single, Single, Single), Single>, ICubicCurveWriter<Single>
    {
        #region API

        public override Single GetSample(float offset)
        {
            var sample = FindSample(offset);

            return Hermite(sample.Item1.Item2, sample.Item1.Item3, sample.Item2.Item2, sample.Item2.Item1, sample.Item3);
        }

        private static Single Hermite(Single value1, Single tangent1, Single value2, Single tangent2, float amount)
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

        public void SetControlPoint(float key, Single value)
        {
            var val = GetKey(key) ?? default;
            val.Item2 = value;
            SetKey(key, val);
        }

        public void SetControlPointIn(float key, Single value)
        {
            var val = GetKey(key) ?? default;
            val.Item1 = value - val.Item2;
            SetKey(key, val);
        }

        public void SetControlPointOut(float key, Single value)
        {
            var val = GetKey(key) ?? default;
            val.Item3 = value + val.Item2;
            SetKey(key, val);
        }

        public void SetTangentIn(float key, Single value)
        {
            var val = GetKey(key) ?? default;
            val.Item1 = value;
            SetKey(key, val);
        }

        public void SetTangentOut(float key, Single value)
        {
            var val = GetKey(key) ?? default;
            val.Item3 = value;
            SetKey(key, val);
        }

        #endregion
    }

    class Vector3LinearCurve : Curve<Vector3, Vector3>, ICurveWriter<Vector3>
    {
        #region API

        public override Vector3 GetSample(float offset)
        {
            var sample = FindSample(offset);

            return Vector3.Lerp(sample.Item1, sample.Item2, sample.Item3);
        }

        public void SetControlPoint(float offset, Vector3 value)
        {
            SetKey(offset, value);
        }

        #endregion
    }

    class Vector3CubicCurve : Curve<(Vector3, Vector3, Vector3), Vector3>, ICubicCurveWriter<Vector3>
    {
        #region API

        public override Vector3 GetSample(float offset)
        {
            var sample = FindSample(offset);

            return Hermite(sample.Item1.Item2, sample.Item1.Item3, sample.Item2.Item2, sample.Item2.Item1, sample.Item3);
        }

        private static Vector3 Hermite(Vector3 value1, Vector3 tangent1, Vector3 value2, Vector3 tangent2, float amount)
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

        public void SetControlPoint(float key, Vector3 value)
        {
            var val = GetKey(key) ?? default;
            val.Item2 = value;
            SetKey(key, val);
        }

        public void SetControlPointIn(float key, Vector3 value)
        {
            var val = GetKey(key) ?? default;
            val.Item1 = value - val.Item2;
            SetKey(key, val);
        }

        public void SetControlPointOut(float key, Vector3 value)
        {
            var val = GetKey(key) ?? default;
            val.Item3 = value + val.Item2;
            SetKey(key, val);
        }

        public void SetTangentIn(float key, Vector3 value)
        {
            var val = GetKey(key) ?? default;
            val.Item1 = value;
            SetKey(key, val);
        }

        public void SetTangentOut(float key, Vector3 value)
        {
            var val = GetKey(key) ?? default;
            val.Item3 = value;
            SetKey(key, val);
        }

        #endregion
    }

    class QuaternionLinearCurve : Curve<Quaternion, Quaternion>, ICurveWriter<Quaternion>
    {
        #region API

        public override Quaternion GetSample(float offset)
        {
            var sample = FindSample(offset);

            return Quaternion.Slerp(sample.Item1, sample.Item2, sample.Item3);
        }

        public void SetControlPoint(float offset, Quaternion value)
        {
            SetKey(offset, value);
        }

        #endregion
    }

    class QuaternionCubicCurve : Curve<(Quaternion, Quaternion, Quaternion), Quaternion>
    {
        public override Quaternion GetSample(float offset)
        {
            var sample = FindSample(offset);

            return Hermite(sample.Item1.Item2, sample.Item1.Item3, sample.Item2.Item2, sample.Item2.Item1, sample.Item3);
        }

        private static Quaternion Hermite(Quaternion value1, Quaternion tangent1, Quaternion value2, Quaternion tangent2, float amount)
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
