using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;
using System.Linq;

namespace SharpGLTF.Animations
{
    // TODO: define just ONE kind of curve: spline with flags, where a flag might indicate if the current segment is linear or spline.
    // when converting to gltf, check if all segments are linear, and use the appropiate encoding.

    public interface ICurveSampler<T>
        where T : struct
    {
        IReadOnlyCollection<float> Keys { get; }

        (float, float, float) FindLerp(float offset);

        T GetPoint(float offset);

        T GetTangent(float offset);
    }

    interface ISplineCurve<T> : ICurveSampler<T>
        where T : struct
    {
        void RemoveKey(float key);

        T GetControlPoint(float key);

        void SetControlPoint(float key, T value);

        void SetTangentIn(float key, T value, float scale);
        void SetTangentOut(float key, T value, float scale);

        Dictionary<float, (T, T, T)> ToDictionary();
    }

    [System.Diagnostics.DebuggerDisplay("[{_Offset}] = {Sample}")]
    public struct CurvePoint<T>
        where T : struct
    {
        #region lifecycle

        public CurvePoint(ICurveSampler<T> curve, float offset)
        {
            _Curve = curve;
            _Offset = offset;
        }

        #endregion

        #region data

        private readonly ICurveSampler<T> _Curve;
        private readonly float _Offset;

        #endregion

        #region properties

        public T Point => _Curve.GetPoint(_Offset);

        public T Tangent => _Curve.GetTangent(_Offset);

        public float LerpAmount => _Curve.FindLerp(_Offset).Item3;

        #endregion

        #region API

        public CurvePoint<T> Split()
        {
            var c = GetCurrent();

            if (c.HasValue && c.Value._Offset == this._Offset) return this;

            if (_Curve is ISplineCurve<T> spline)
            {
                var p = _Curve.GetPoint(_Offset);
                var t = _Curve.GetTangent(_Offset);

                spline.SetControlPoint(_Offset, p);
                spline.SetTangentIn(_Offset, t, -1);
                spline.SetTangentOut(_Offset, t, 1);
            }

            return this;
        }

        public CurvePoint<T> GetAt(float offset) { return new CurvePoint<T>(_Curve, offset); }

        public CurvePoint<T>? GetCurrent()
        {
            var offsets = _Curve.FindLerp(_Offset);

            if (_Offset < offsets.Item1) return null;

            return new CurvePoint<T>(_Curve, offsets.Item1);
        }

        public CurvePoint<T>? GetNext()
        {
            var offsets = _Curve.FindLerp(_Offset);

            if (_Offset >= offsets.Item2) return null;

            return new CurvePoint<T>(_Curve, offsets.Item2);
        }

        public CurvePoint<T> MovePointTo(T value)
        {
            Split();

            if (_Curve is ISplineCurve<T> spline)
            {
                spline.SetControlPoint(_Offset, value);
                return this;
            }

            throw new NotImplementedException();
        }

        public CurvePoint<T> MoveIncomingTangentTo(T value)
        {
            Split();

            if (_Curve is ISplineCurve<T> spline)
            {
                spline.SetTangentIn(_Offset, value, 1);
                return this;
            }

            throw new NotImplementedException();
        }

        public CurvePoint<T> MoveOutgoingTangentTo(T value)
        {
            Split();

            if (_Curve is ISplineCurve<T> spline)
            {
                spline.SetTangentOut(_Offset, value, 1);
                return this;
            }

            throw new NotImplementedException();
        }

        #endregion
    }

    

    public static class CurveFactory
    {
        // TODO: we could support conversions between linear and cubic (with hermite regression)

        public static ICurveSampler<T> CreateSplineCurve<T>()
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

    /// <summary>
    /// Represents a collection of consecutive nodes that can be sampled into a continuous curve.
    /// </summary>
    /// <typeparam name="Tout">The type of value evaluated at any point in the curve.</typeparam>
    abstract class Curve<Tout>
        where Tout : struct
    {
        #region lifecycle

        public Curve() { }

        protected Curve(Curve<Tout> other)
        {
            foreach (var kvp in other._Keys)
            {
                this._Keys.Add(kvp.Key, kvp.Value);
            }
        }

        #endregion

        #region data

        protected SortedDictionary<float, _SplinePoint<Tout>> _Keys = new SortedDictionary<float, _SplinePoint<Tout>>();

        #endregion

        #region properties
        public IReadOnlyCollection<float> Keys => _Keys.Keys;

        #endregion

        #region API

        public void RemoveKey(float key) { _Keys.Remove(key); }

        protected _SplinePoint<Tout>? GetKey(float key) { return _Keys.TryGetValue(key, out _SplinePoint<Tout> value) ? value : (_SplinePoint<Tout>?)null; }

        protected void SetKey(float key, _SplinePoint<Tout> value) { _Keys[key] = value; }

        protected (_SplinePoint<Tout>, _SplinePoint<Tout>, float) FindSample(float offset)
        {
            if (_Keys.Count == 0) return (default(_SplinePoint<Tout>), default(_SplinePoint<Tout>), 0);

            var offsets = _FindPairContainingOffset(_Keys.Keys, offset);

            return (_Keys[offsets.Item1], _Keys[offsets.Item2], offsets.Item3);
        }

        public (float, float, float) FindLerp(float offset) { return _FindPairContainingOffset(_Keys.Keys, offset); }

        /// <summary>
        /// Given a <paramref name="sequence"/> of offsets and an <paramref name="offset"/>,
        /// it finds two consecutive offsets that contain <paramref name="offset"/> between them.
        /// </summary>
        /// <param name="sequence">A sequence of offsets.</param>
        /// <param name="offset">the offset to look for in the sequence.</param>
        /// <returns>Two consecutive offsets and a LERP value.</returns>
        private static (float, float, float) _FindPairContainingOffset(IEnumerable<float> sequence, float offset)
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
    }

    // Hermite Point
    struct _SplinePoint<T>
        where T : struct
    {
        public T IncomingTangent;
        public T Point;
        public T OutgoingTangent;
        public int OutgoingMode;
    }

    class ScalarSplineCurve : Curve<Single>, ISplineCurve<Single>
    {
        #region lifecycle

        public ScalarSplineCurve() { }

        protected ScalarSplineCurve(ScalarSplineCurve other) : base(other) { }

        #endregion

        #region API

        public float GetPoint(float offset)
        {
            var sample = FindSample(offset);

            if (sample.Item1.OutgoingMode == 0) return sample.Item1.Point;

            if (sample.Item1.OutgoingMode == 1)
            {
                return (sample.Item1.Point * (1 - sample.Item3)) + (sample.Item2.Point * sample.Item3);
            }

            var pointStart = sample.Item1.Point;
            var tangentOut = sample.Item1.OutgoingTangent;
            var pointEnd = sample.Item2.Point;
            var tangentIn = sample.Item2.IncomingTangent;

            var basis = CurveFactory.CalculateHermiteBasis(sample.Item3);

            return (pointStart * basis.Item1) + (pointEnd * basis.Item2) + (tangentOut * basis.Item3) + (tangentIn * basis.Item4);
        }

        public float GetTangent(float offset)
        {
            var sample = FindSample(offset);

            if (sample.Item1.OutgoingMode == 0) return 0;

            if (sample.Item1.OutgoingMode == 1) return sample.Item2.Point - sample.Item1.Point;

            var pointStart = sample.Item1.Point;
            var tangentOut = sample.Item1.OutgoingTangent;
            var pointEnd = sample.Item2.Point;
            var tangentIn = sample.Item2.IncomingTangent;

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
            val.IncomingTangent = (val.Point - value) * 4;
            SetKey(key, val);
        }

        public void SetCardinalPointOut(float key, Single value)
        {
            var val = GetKey(key) ?? default;
            val.OutgoingTangent = (value - val.Point) * 4;
            SetKey(key, val);
        }

        public void SetTangentIn(float key, Single value, float scale)
        {
            var val = GetKey(key) ?? default;
            val.IncomingTangent = value * scale;
            SetKey(key, val);
        }

        public void SetTangentOut(float key, Single value, float scale)
        {
            var val = GetKey(key) ?? default;
            val.OutgoingTangent = value * scale;
            SetKey(key, val);
        }

        public Dictionary<float, (float, float, float)> ToDictionary()
        {
            return _Keys.ToDictionary(k => k.Key, v => (v.Value.IncomingTangent, v.Value.Point, v.Value.OutgoingTangent));
        }

        #endregion
    }

    class Vector3SplineCurve : Curve<Vector3>, ISplineCurve<Vector3>
    {
        #region lifecycle

        public Vector3SplineCurve() { }

        protected Vector3SplineCurve(Vector3SplineCurve other) : base(other) { }

        #endregion

        #region API

        public Vector3 GetPoint(float offset)
        {
            var sample = FindSample(offset);

            if (sample.Item1.OutgoingMode == 0) return sample.Item1.Point;

            if (sample.Item1.OutgoingMode == 1)
            {
                return Vector3.Lerp(sample.Item1.Point, sample.Item2.Point, sample.Item3);
            }

            var pointStart = sample.Item1.Point;
            var tangentOut = sample.Item1.OutgoingTangent;
            var pointEnd = sample.Item2.Point;
            var tangentIn = sample.Item2.IncomingTangent;

            var basis = CurveFactory.CalculateHermiteBasis(sample.Item3);

            return (pointStart * basis.Item1) + (pointEnd * basis.Item2) + (tangentOut * basis.Item3) + (tangentIn * basis.Item4);
        }

        public Vector3 GetTangent(float offset)
        {
            var sample = FindSample(offset);

            if (sample.Item1.OutgoingMode == 0) return Vector3.Zero;

            if (sample.Item1.OutgoingMode == 1) return sample.Item2.Point - sample.Item1.Point;

            var pointStart = sample.Item1.Point;
            var tangentOut = sample.Item1.OutgoingTangent;
            var pointEnd = sample.Item2.Point;
            var tangentIn = sample.Item2.IncomingTangent;

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
            val.IncomingTangent = (val.Point - value) * 4;
            SetKey(key, val);
        }

        public void SetCardinalPointOut(float key, Vector3 value)
        {
            var val = GetKey(key) ?? default;
            val.OutgoingTangent = (value - val.Point) * 4;
            SetKey(key, val);
        }

        public void SetTangentIn(float key, Vector3 value, float scale)
        {
            var val = GetKey(key) ?? default;
            val.IncomingTangent = value * scale;
            SetKey(key, val);
        }

        public void SetTangentOut(float key, Vector3 value, float scale)
        {
            var val = GetKey(key) ?? default;
            val.OutgoingTangent = value * scale;
            SetKey(key, val);
        }

        public Dictionary<float, (Vector3, Vector3, Vector3)> ToDictionary()
        {
            return _Keys.ToDictionary(k => k.Key, v => (v.Value.IncomingTangent, v.Value.Point, v.Value.OutgoingTangent));
        }

        #endregion
    }

    class QuaternionSplineCurve : Curve<Quaternion> , ISplineCurve<Quaternion>
    {
        #region lifecycle

        public QuaternionSplineCurve() { }

        protected QuaternionSplineCurve(QuaternionSplineCurve other) : base(other) { }

        #endregion

        #region API

        public Quaternion GetPoint(float offset)
        {
            var sample = FindSample(offset);

            if (sample.Item1.OutgoingMode == 0) return sample.Item1.Point;

            if (sample.Item1.OutgoingMode == 1)
            {
                return Quaternion.Slerp(sample.Item1.Point, sample.Item2.Point, sample.Item3);
            }

            var pointStart = sample.Item1.Point;
            var tangentOut = sample.Item1.OutgoingTangent;
            var pointEnd = sample.Item2.Point;
            var tangentIn = sample.Item2.IncomingTangent;

            var basis = CurveFactory.CalculateHermiteBasis(sample.Item3);

            var q = (pointStart * basis.Item1) + (pointEnd * basis.Item2) + (tangentOut * basis.Item3) + (tangentIn * basis.Item4);

            return Quaternion.Normalize(q);
        }

        public Quaternion GetTangent(float offset)
        {
            var sample = FindSample(offset);

            if (sample.Item1.OutgoingMode == 0) return Quaternion.Identity;

            if (sample.Item1.OutgoingMode == 1)
            {
                throw new NotImplementedException();
            }

            var pointStart = sample.Item1.Point;
            var tangentOut = sample.Item1.OutgoingTangent;
            var pointEnd = sample.Item2.Point;
            var tangentIn = sample.Item2.IncomingTangent;

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

            val.IncomingTangent = value;
            SetKey(key, val);
        }

        public void SetCardinalPointOut(float key, Quaternion value)
        {
            var val = GetKey(key) ?? default;

            var inv = Quaternion.Inverse(val.Point);
            value = Quaternion.Concatenate(value, inv); // *4? => convert to axisradians; angle * 4, back to Q
            value = Quaternion.Normalize(value);

            val.OutgoingTangent = value;
            SetKey(key, val);
        }

        public void SetTangentIn(float key, Quaternion value, float scale)
        {
            var val = GetKey(key) ?? default;
            val.IncomingTangent = _Scale(value, scale);
            SetKey(key, val);
        }

        public void SetTangentOut(float key, Quaternion value, float scale)
        {
            var val = GetKey(key) ?? default;
            val.OutgoingTangent = _Scale(value, scale);
            SetKey(key, val);
        }

        internal Quaternion _Scale(Quaternion q, float scale)
        {
            var axis = Vector3.Normalize(new Vector3(q.X, q.Y, q.Z));
            var angle = Math.Acos(q.W) * 2 * scale;

            return Quaternion.CreateFromAxisAngle(axis, (float)angle);
        }

        public Dictionary<float, (Quaternion, Quaternion, Quaternion)> ToDictionary()
        {
            return _Keys.ToDictionary(k => k.Key, v => (v.Value.IncomingTangent, v.Value.Point, v.Value.OutgoingTangent));
        }

        #endregion
    }
}
