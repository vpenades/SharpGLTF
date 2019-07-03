using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace SharpGLTF.Animations
{
    public interface ICurveSampler<T>
        where T : struct
    {
        T GetSample(float offset);
    }

    public interface ILinearCurve<T> : ICurveSampler<T>
        where T : struct
    {
        IReadOnlyCollection<float> Keys { get; }

        void RemoveKey(float key);

        T GetControlPoint(float key);

        void SetControlPoint(float key, T value);
    }

    public interface ISplineCurve<T> : ILinearCurve<T>
        where T : struct
    {
        void SetControlPointIn(float key, T value);
        void SetControlPointOut(float key, T value);

        void SetTangentIn(float key, T value);
        void SetTangentOut(float key, T value);
    }

    public static class CurveFactory
    {
        // TODO: we could support conversions between linear and cubic (with hermite regression)

        public static ILinearCurve<T> CreateLinearCurve<T>()
            where T : struct
        {
            if (typeof(T) == typeof(Single)) return new ScalarLinearCurve() as ILinearCurve<T>;
            if (typeof(T) == typeof(Vector3)) return new Vector3LinearCurve() as ILinearCurve<T>;
            if (typeof(T) == typeof(Quaternion)) return new QuaternionLinearCurve() as ILinearCurve<T>;

            throw new ArgumentException(nameof(T), "Generic argument not supported");
        }

        public static ISplineCurve<T> CreateSplineCurve<T>()
            where T : struct
        {
            if (typeof(T) == typeof(Single)) return new ScalarSplineCurve() as ISplineCurve<T>;
            if (typeof(T) == typeof(Vector3)) return new Vector3SplineCurve() as ISplineCurve<T>;
            if (typeof(T) == typeof(Quaternion)) return new QuaternionSplineCurve() as ISplineCurve<T>;

            throw new ArgumentException(nameof(T), "Generic argument not supported");
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

    abstract class Curve<Tin, Tout> : ICurveSampler<Tout>
        where Tin : struct
        where Tout : struct
    {
        #region lifecycle

        public Curve() { }

        protected Curve(Curve<Tin, Tout> other)
        {
            foreach (var kvp in other._Keys)
            {
                this._Keys.Add(kvp.Key, kvp.Value);
            }
        }

        #endregion

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

    struct _SplinePoint<T>
        where T : struct
    {
        public T InTangent;
        public T Point;
        public T OutTangent;
    }

    class ScalarLinearCurve : Curve<Single, Single>, ILinearCurve<Single>
    {
        #region lifecycle

        public ScalarLinearCurve() { }

        protected ScalarLinearCurve(ScalarLinearCurve other) : base(other) { }

        #endregion

        #region API

        public override Single GetSample(float offset)
        {
            var sample = FindSample(offset);

            return sample.Item1 * (1 - sample.Item3) + (sample.Item2 * sample.Item3);
        }

        public float GetControlPoint(float key)
        {
            var sample = FindSample(key);
            return sample.Item3 <= 0.5f ? sample.Item1 : sample.Item2;
        }

        public void SetControlPoint(float offset, Single value)
        {
            SetKey(offset, value);
        }

        #endregion
    }

    class ScalarSplineCurve : Curve<_SplinePoint<Single>, Single>, ISplineCurve<Single>
    {
        #region lifecycle

        public ScalarSplineCurve() { }

        protected ScalarSplineCurve(ScalarSplineCurve other) : base(other) { }

        #endregion

        #region API

        public override Single GetSample(float offset)
        {
            var sample = FindSample(offset);

            return Hermite(sample.Item1.Point, sample.Item1.OutTangent, sample.Item2.Point, sample.Item2.InTangent, sample.Item3);
        }

        private static Single Hermite(Single value1, Single tangent1, Single value2, Single tangent2, float amount)
        {
            var hermite = CurveFactory.CalculateHermiteWeights(amount);

            return (value1 * hermite.Item1) + (value2 * hermite.Item2) + (tangent1 * hermite.Item3) + (tangent2 * hermite.Item4);
        }

        public float GetControlPoint(float key)
        {
            var sample = FindSample(key);
            return sample.Item3 <= 0.5f ? sample.Item1.Point : sample.Item2.Point;
        }

        public void SetControlPoint(float key, Single value)
        {
            var val = GetKey(key) ?? default;
            val.Point = value;
            SetKey(key, val);
        }

        public void SetControlPointIn(float key, Single value)
        {
            var val = GetKey(key) ?? default;
            val.InTangent = val.Point - value;
            SetKey(key, val);
        }

        public void SetControlPointOut(float key, Single value)
        {
            var val = GetKey(key) ?? default;
            val.OutTangent = value - val.Point;
            SetKey(key, val);
        }

        public void SetTangentIn(float key, Single value)
        {
            var val = GetKey(key) ?? default;
            val.InTangent = value;
            SetKey(key, val);
        }

        public void SetTangentOut(float key, Single value)
        {
            var val = GetKey(key) ?? default;
            val.OutTangent = value;
            SetKey(key, val);
        }

        #endregion
    }

    class Vector3LinearCurve : Curve<Vector3, Vector3>, ILinearCurve<Vector3>
    {
        #region lifecycle

        public Vector3LinearCurve() { }

        protected Vector3LinearCurve(Vector3LinearCurve other) : base(other) { }

        #endregion

        #region API

        public override Vector3 GetSample(float offset)
        {
            var sample = FindSample(offset);

            return Vector3.Lerp(sample.Item1, sample.Item2, sample.Item3);
        }

        public Vector3 GetControlPoint(float key)
        {
            var sample = FindSample(key);
            return sample.Item3 <= 0.5f ? sample.Item1 : sample.Item2;
        }

        public void SetControlPoint(float offset, Vector3 value)
        {
            SetKey(offset, value);
        }

        #endregion
    }

    class Vector3SplineCurve : Curve<_SplinePoint<Vector3>, Vector3>, ISplineCurve<Vector3>
    {
        #region lifecycle

        public Vector3SplineCurve() { }

        protected Vector3SplineCurve(Vector3SplineCurve other) : base(other) { }

        #endregion

        #region API

        public override Vector3 GetSample(float offset)
        {
            var sample = FindSample(offset);

            return Hermite(sample.Item1.Point, sample.Item1.OutTangent, sample.Item2.Point, sample.Item2.InTangent, sample.Item3);
        }

        private static Vector3 Hermite(Vector3 pointStart, Vector3 tangentOut, Vector3 pointEnd, Vector3 tangentIn, float amount)
        {
            var hermite = CurveFactory.CalculateHermiteWeights(amount);

            return (pointStart * hermite.Item1) + (pointEnd * hermite.Item2) + (tangentOut * hermite.Item3) + (tangentIn * hermite.Item4);
        }

        public Vector3 GetControlPoint(float key)
        {
            var sample = FindSample(key);
            return sample.Item3 <= 0.5f ? sample.Item1.Point : sample.Item2.Point;
        }

        public void SetControlPoint(float key, Vector3 value)
        {
            var val = GetKey(key) ?? default;
            val.Point = value;
            SetKey(key, val);
        }

        public void SetControlPointIn(float key, Vector3 value)
        {
            var val = GetKey(key) ?? default;
            val.InTangent = val.Point - value;
            SetKey(key, val);
        }

        public void SetControlPointOut(float key, Vector3 value)
        {
            var val = GetKey(key) ?? default;
            val.OutTangent = value - val.Point;
            SetKey(key, val);
        }

        public void SetTangentIn(float key, Vector3 value)
        {
            var val = GetKey(key) ?? default;
            val.InTangent = value;
            SetKey(key, val);
        }

        public void SetTangentOut(float key, Vector3 value)
        {
            var val = GetKey(key) ?? default;
            val.OutTangent = value;
            SetKey(key, val);
        }

        #endregion
    }

    class QuaternionLinearCurve : Curve<Quaternion, Quaternion>, ILinearCurve<Quaternion>
    {
        #region lifecycle

        public QuaternionLinearCurve() { }

        protected QuaternionLinearCurve(QuaternionLinearCurve other) : base(other) { }

        #endregion

        #region API

        public override Quaternion GetSample(float offset)
        {
            var sample = FindSample(offset);

            return Quaternion.Slerp(sample.Item1, sample.Item2, sample.Item3);
        }

        public Quaternion GetControlPoint(float key)
        {
            var sample = FindSample(key);
            return sample.Item3 <= 0.5f ? sample.Item1 : sample.Item2;
        }

        public void SetControlPoint(float offset, Quaternion value)
        {
            SetKey(offset, value);
        }

        #endregion
    }

    class QuaternionSplineCurve : Curve<_SplinePoint<Quaternion>, Quaternion> , ISplineCurve<Quaternion>
    {
        #region lifecycle

        public QuaternionSplineCurve() { }

        protected QuaternionSplineCurve(QuaternionSplineCurve other) : base(other) { }

        #endregion

        #region API

        public override Quaternion GetSample(float offset)
        {
            var sample = FindSample(offset);

            return Hermite(sample.Item1.Point, sample.Item1.OutTangent, sample.Item2.Point, sample.Item2.InTangent, sample.Item3);
        }

        private static Quaternion Hermite(Quaternion value1, Quaternion tangent1, Quaternion value2, Quaternion tangent2, float amount)
        {
            var hermite = CurveFactory.CalculateHermiteWeights(amount);

            return Quaternion.Normalize((value1 * hermite.Item1) + (value2 * hermite.Item2) + (tangent1 * hermite.Item3) + (tangent2 * hermite.Item4));
        }

        public Quaternion GetControlPoint(float key)
        {
            var sample = FindSample(key);
            return sample.Item3 <= 0.5f ? sample.Item1.Point : sample.Item2.Point;
        }

        public void SetControlPoint(float key, Quaternion value)
        {
            var val = GetKey(key) ?? default;
            val.Point = Quaternion.Normalize(value);
            SetKey(key, val);
        }

        public void SetControlPointIn(float key, Quaternion value)
        {
            var val = GetKey(key) ?? default;

            var inv = Quaternion.Inverse(value);
            value = Quaternion.Concatenate(val.Point, inv);
            value = Quaternion.Normalize(value);

            val.InTangent = value;
            SetKey(key, val);
        }

        public void SetControlPointOut(float key, Quaternion value)
        {
            var val = GetKey(key) ?? default;

            var inv = Quaternion.Inverse(val.Point);
            value = Quaternion.Concatenate(value, inv);
            value = Quaternion.Normalize(value);

            val.OutTangent = value;
            SetKey(key, val);
        }

        public void SetTangentIn(float key, Quaternion value)
        {
            var val = GetKey(key) ?? default;
            val.InTangent = Quaternion.Normalize(value);
            SetKey(key, val);
        }

        public void SetTangentOut(float key, Quaternion value)
        {
            var val = GetKey(key) ?? default;
            val.OutTangent = Quaternion.Normalize(value);
            SetKey(key, val);
        }

        #endregion
    }
}
