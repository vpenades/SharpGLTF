using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;
using System.Linq;

namespace SharpGLTF.Animations
{
    public interface ICurveSampler<T>
        where T : struct
    {
        T GetPoint(float offset);

        T GetTangent(float offset);
    }

    [System.Diagnostics.DebuggerDisplay("[{_Offset}] = {Sample}")]
    public struct CurvePoint<T>
        where T : struct
    {
        private readonly ICurveSampler<T> _Curve;
        private readonly float _Offset;

        public T Point => _Curve.GetPoint(_Offset);

        public T Tangent => _Curve.GetTangent(_Offset);
    }

    public interface ILinearCurve<T> : ICurveSampler<T>
        where T : struct
    {
        IReadOnlyCollection<float> Keys { get; }

        void RemoveKey(float key);

        T GetControlPoint(float key);

        void SetControlPoint(float key, T value);

        Dictionary<float, T> ToDictionary();
    }

    public interface ISplineCurve<T> : ICurveSampler<T>
        where T : struct
    {
        IReadOnlyCollection<float> Keys { get; }

        void RemoveKey(float key);

        T GetControlPoint(float key);

        void SetControlPoint(float key, T value);

        void SetCardinalPointIn(float key, T value);
        void SetCardinalPointOut(float key, T value);

        void SetTangentIn(float key, T value);
        void SetTangentOut(float key, T value);

        Dictionary<float, (T, T, T)> ToDictionary();
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
        public static (float, float, float, float) CalculateHermiteBasis(float amount)
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

        public static (float, float, float, float) CalculateHermiteTangent(float amount)
        {
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
    }

    abstract class Curve<Tin, Tout>
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

        protected SortedDictionary<float, Tin> _Keys = new SortedDictionary<float, Tin>();

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

    // Hermite Point
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

        public Single GetPoint(float offset)
        {
            var sample = FindSample(offset);

            return (sample.Item1 * (1 - sample.Item3)) + (sample.Item2 * sample.Item3);
        }

        public Single GetTangent(float offset)
        {
            var sample = FindSample(offset);
            return sample.Item2 - sample.Item1;
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

        public Dictionary<float, float> ToDictionary()
        {
            return _Keys.ToDictionary(k => k.Key, v => v.Value);
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

        public float GetPoint(float offset)
        {
            var sample = FindSample(offset);
            var pointStart = sample.Item1.Point;
            var tangentOut = sample.Item1.OutTangent;
            var pointEnd = sample.Item2.Point;
            var tangentIn = sample.Item2.InTangent;

            var basis = CurveFactory.CalculateHermiteBasis(sample.Item3);

            return (pointStart * basis.Item1) + (pointEnd * basis.Item2) + (tangentOut * basis.Item3) + (tangentIn * basis.Item4);
        }

        public float GetTangent(float offset)
        {
            var sample = FindSample(offset);
            var pointStart = sample.Item1.Point;
            var tangentOut = sample.Item1.OutTangent;
            var pointEnd = sample.Item2.Point;
            var tangentIn = sample.Item2.InTangent;

            var basis = CurveFactory.CalculateHermiteTangent(sample.Item3);

            return (pointStart * basis.Item1) + (pointEnd * basis.Item2) + (tangentOut * basis.Item3) + (tangentIn * basis.Item4);
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

        public void SetCardinalPointIn(float key, Single value)
        {
            var val = GetKey(key) ?? default;
            val.InTangent = (val.Point - value) * 4;
            SetKey(key, val);
        }

        public void SetCardinalPointOut(float key, Single value)
        {
            var val = GetKey(key) ?? default;
            val.OutTangent = (value - val.Point) * 4;
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

        public Dictionary<float, (float, float, float)> ToDictionary()
        {
            return _Keys.ToDictionary(k => k.Key, v => (v.Value.InTangent, v.Value.Point, v.Value.OutTangent));
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

        public Vector3 GetPoint(float offset)
        {
            var sample = FindSample(offset);

            return Vector3.Lerp(sample.Item1, sample.Item2, sample.Item3);
        }

        public Vector3 GetTangent(float offset)
        {
            var sample = FindSample(offset);

            return sample.Item2 - sample.Item1;
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

        public Dictionary<float, Vector3> ToDictionary()
        {
            return _Keys.ToDictionary(k => k.Key, v => v.Value);
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

        public Vector3 GetPoint(float offset)
        {
            var sample = FindSample(offset);
            var pointStart = sample.Item1.Point;
            var tangentOut = sample.Item1.OutTangent;
            var pointEnd = sample.Item2.Point;
            var tangentIn = sample.Item2.InTangent;

            var basis = CurveFactory.CalculateHermiteBasis(sample.Item3);

            return (pointStart * basis.Item1) + (pointEnd * basis.Item2) + (tangentOut * basis.Item3) + (tangentIn * basis.Item4);
        }

        public Vector3 GetTangent(float offset)
        {
            var sample = FindSample(offset);
            var pointStart = sample.Item1.Point;
            var tangentOut = sample.Item1.OutTangent;
            var pointEnd = sample.Item2.Point;
            var tangentIn = sample.Item2.InTangent;

            var basis = CurveFactory.CalculateHermiteTangent(sample.Item3);

            return (pointStart * basis.Item1) + (pointEnd * basis.Item2) + (tangentOut * basis.Item3) + (tangentIn * basis.Item4);
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

        public void SetCardinalPointIn(float key, Vector3 value)
        {
            var val = GetKey(key) ?? default;
            val.InTangent = (val.Point - value) * 4;
            SetKey(key, val);
        }

        public void SetCardinalPointOut(float key, Vector3 value)
        {
            var val = GetKey(key) ?? default;
            val.OutTangent = (value - val.Point) * 4;
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

        public Dictionary<float, (Vector3, Vector3, Vector3)> ToDictionary()
        {
            return _Keys.ToDictionary(k => k.Key, v => (v.Value.InTangent, v.Value.Point, v.Value.OutTangent));
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

        public Quaternion GetPoint(float offset)
        {
            var sample = FindSample(offset);

            return Quaternion.Slerp(sample.Item1, sample.Item2, sample.Item3);
        }

        public Quaternion GetTangent(float offset)
        {
            throw new NotImplementedException();
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

        public Dictionary<float, Quaternion> ToDictionary()
        {
            return _Keys.ToDictionary(k => k.Key, v => v.Value);
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

        public Quaternion GetPoint(float offset)
        {
            var sample = FindSample(offset);
            var pointStart = sample.Item1.Point;
            var tangentOut = sample.Item1.OutTangent;
            var pointEnd = sample.Item2.Point;
            var tangentIn = sample.Item2.InTangent;

            var basis = CurveFactory.CalculateHermiteBasis(sample.Item3);

            var q = (pointStart * basis.Item1) + (pointEnd * basis.Item2) + (tangentOut * basis.Item3) + (tangentIn * basis.Item4);

            return Quaternion.Normalize(q);
        }

        public Quaternion GetTangent(float offset)
        {
            var sample = FindSample(offset);
            var pointStart = sample.Item1.Point;
            var tangentOut = sample.Item1.OutTangent;
            var pointEnd = sample.Item2.Point;
            var tangentIn = sample.Item2.InTangent;

            var basis = CurveFactory.CalculateHermiteTangent(sample.Item3);

            var q = (pointStart * basis.Item1) + (pointEnd * basis.Item2) + (tangentOut * basis.Item3) + (tangentIn * basis.Item4);

            return Quaternion.Normalize(q);
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

        public void SetCardinalPointIn(float key, Quaternion value)
        {
            var val = GetKey(key) ?? default;

            var inv = Quaternion.Inverse(value);
            value = Quaternion.Concatenate(val.Point, inv); // *4? => convert to axisradians; angle * 4, back to Q
            value = Quaternion.Normalize(value);

            val.InTangent = value;
            SetKey(key, val);
        }

        public void SetCardinalPointOut(float key, Quaternion value)
        {
            var val = GetKey(key) ?? default;

            var inv = Quaternion.Inverse(val.Point);
            value = Quaternion.Concatenate(value, inv); // *4? => convert to axisradians; angle * 4, back to Q
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

        public Dictionary<float, (Quaternion, Quaternion, Quaternion)> ToDictionary()
        {
            return _Keys.ToDictionary(k => k.Key, v => (v.Value.InTangent, v.Value.Point, v.Value.OutTangent));
        }

        #endregion
    }
}
