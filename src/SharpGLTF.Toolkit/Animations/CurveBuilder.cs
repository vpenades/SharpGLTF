using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;
using System.Linq;

namespace SharpGLTF.Animations
{
    /// <summary>
    /// Represents an editable curve of <typeparamref name="T"/> elements.
    /// </summary>
    /// <typeparam name="T">An element of the curve.</typeparam>
    public abstract class CurveBuilder<T> :
        ICurveSampler<T>,
        IConvertibleCurve<T>
        where T : struct
    {
        #region lifecycle

        protected CurveBuilder() { }

        protected CurveBuilder(CurveBuilder<T> other)
        {
            if (other == null) return;

            foreach (var kvp in other._Keys)
            {
                this._Keys[kvp.Key] = kvp.Value.Clone(CloneValue);
            }
        }

        IConvertibleCurve<T> IConvertibleCurve<T>.Clone()
        {
            return this.Clone();
        }

        public abstract CurveBuilder<T> Clone();

        #endregion

        #region data

        [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)]
        private SortedDictionary<float, _CurveNode<T>> _Keys = new SortedDictionary<float, _CurveNode<T>>();

        #endregion

        #region properties

        [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.RootHidden)]
        internal IReadOnlyDictionary<float, _CurveNode<T>> _DebugKeys => _Keys;

        [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)]
        public IReadOnlyCollection<float> Keys => _Keys.Keys;

        public int MaxDegree => _Keys.Count == 0 ? 0 : _Keys.Values.Max(item => item.Degree);

        #endregion

        #region abstract API

        protected abstract bool AreEqual(T left, T right);

        protected abstract T CloneValue(T value);

        /// <summary>
        /// Creates a <typeparamref name="T"/> instance from an <see cref="Single"/>[] array.
        /// </summary>
        /// <param name="values">An array of floats.</param>
        /// <returns>A <typeparamref name="T"/> instance.</returns>
        protected abstract T CreateValue(IReadOnlyList<float> values);

        /// <summary>
        /// Samples the curve at a given <paramref name="offset"/>
        /// </summary>
        /// <param name="offset">The curve offset to sample.</param>
        /// <returns>A curve <typeparamref name="T"/> point.</returns>
        public abstract T GetPoint(float offset);

        protected abstract T GetTangent(T fromValue, T toValue);

        #endregion

        #region API

        public void Clear() { _Keys.Clear(); }

        public void RemoveKey(float offset) { _Keys.Remove(offset); }

        public void SetPoint(float offset, bool isLinear, params float[] elements)
        {
            SetPoint(offset, CreateValue(elements), isLinear);
        }

        public void SetPoint(float offset, T value, bool isLinear = true)
        {
            value = CloneValue(value);

            if (_Keys.TryGetValue(offset, out _CurveNode<T> existing))
            {
                existing.Point = value;
            }
            else
            {
                existing = new _CurveNode<T>(value, isLinear);
            }

            _Keys[offset] = existing;
        }

        /// <summary>
        /// Sets the incoming tangent to an existing point.
        /// </summary>
        /// <param name="offset">The offset of the existing point.</param>
        /// <param name="tangent">The tangent value.</param>
        public void SetIncomingTangent(float offset, T tangent)
        {
            Guard.IsTrue(_Keys.ContainsKey(offset), nameof(offset));

            tangent = CloneValue(tangent);

            offset -= float.Epsilon;

            var (keyA, keyB, _) = CurveSampler.FindRangeContainingOffset(_Keys.Keys, offset);

            if (keyA == keyB)
            {
                var a = _Keys[keyA];

                a.Degree = 3;
                a.IncomingTangent = tangent;

                _Keys[keyA] = a;
            }
            else
            {
                var a = _Keys[keyA];
                var b = _Keys[keyB];

                if (a.Degree == 1) a.OutgoingTangent = GetTangent(a.Point, b.Point);

                a.Degree = 3;

                b.IncomingTangent = tangent;

                _Keys[keyA] = a;
                _Keys[keyB] = b;
            }
        }

        /// <summary>
        /// Sets the outgoing tangent to an existing point.
        /// </summary>
        /// <param name="offset">The offset of the existing point.</param>
        /// <param name="tangent">The tangent value.</param>
        public void SetOutgoingTangent(float offset, T tangent)
        {
            Guard.IsTrue(_Keys.ContainsKey(offset), nameof(offset));

            tangent = CloneValue(tangent);

            var (keyA, keyB, _) = CurveSampler.FindRangeContainingOffset(_Keys.Keys, offset);

            if (keyA == keyB)
            {
                var a = _Keys[keyA];

                a.Degree = 3;
                a.OutgoingTangent = tangent;

                _Keys[keyA] = a;
            }
            else
            {
                var a = _Keys[keyA];
                var b = _Keys[keyB];

                if (keyA != keyB)
                {
                    if (a.Degree == 1) b.IncomingTangent = GetTangent(a.Point, b.Point);
                    _Keys[keyB] = b;
                }

                a.Degree = 3;

                a.OutgoingTangent = tangent;

                _Keys[keyA] = a;
            }
        }

        private protected (_CurveNode<T> A, _CurveNode<T> B, float Amount) FindSample(float offset)
        {
            if (_Keys.Count == 0) return (default(_CurveNode<T>), default(_CurveNode<T>), 0);

            var (keyA, keyB, amount) = CurveSampler.FindRangeContainingOffset(_Keys.Keys, offset);

            return (_Keys[keyA], _Keys[keyB], amount);
        }

        public void SetCurve(ICurveSampler<T> curve)
        {
            if (curve is IConvertibleCurve<T> convertible)
            {
                SetCurve(convertible);
            }
            else
            {
                throw new NotImplementedException();
            }
        }

        public void SetCurve(IConvertibleCurve<T> convertible)
        {
            Guard.NotNull(convertible, nameof(convertible));

            if (convertible.MaxDegree == 0)
            {
                var step = convertible.ToStepCurve();
                foreach (var p in step) this.SetPoint(p.Key, p.Value, false);

                #if DEBUG
                foreach (var p in step)
                {
                    var dstKey = _Keys[p.Key];
                    System.Diagnostics.Debug.Assert(dstKey.Degree <= 1);
                    System.Diagnostics.Debug.Assert(AreEqual(dstKey.Point, p.Value));
                }
                #endif

                return;
            }

            if (convertible.MaxDegree == 1)
            {
                var linear = convertible.ToLinearCurve();
                foreach (var p in linear) this.SetPoint(p.Key, p.Value);

                #if DEBUG
                foreach (var p in linear)
                {
                    var dstKey = _Keys[p.Key];
                    System.Diagnostics.Debug.Assert(dstKey.Degree <= 1);
                    System.Diagnostics.Debug.Assert(AreEqual(dstKey.Point, p.Value));
                }
                #endif

                return;
            }

            if (convertible.MaxDegree == 3)
            {
                var spline = convertible.ToSplineCurve();
                foreach (var ppp in spline)
                {
                    this.SetPoint(ppp.Key, ppp.Value.Value);
                    this.SetIncomingTangent(ppp.Key, ppp.Value.TangentIn);
                    this.SetOutgoingTangent(ppp.Key, ppp.Value.TangentOut);
                }

                #if DEBUG
                foreach (var ppp in spline)
                {
                    var dstKey = _Keys[ppp.Key];
                    System.Diagnostics.Debug.Assert(dstKey.Degree == 3);
                    System.Diagnostics.Debug.Assert(AreEqual(dstKey.Point, ppp.Value.Value));
                    System.Diagnostics.Debug.Assert(AreEqual(dstKey.IncomingTangent, ppp.Value.TangentIn));
                    System.Diagnostics.Debug.Assert(AreEqual(dstKey.OutgoingTangent, ppp.Value.TangentOut));
                }
                #endif

                return;
            }
        }

        public void SetCurve(Schema2.IAnimationSampler<T> curve)
        {
            Guard.NotNull(curve, nameof(curve));

            switch (curve.InterpolationMode)
            {
                case Schema2.AnimationInterpolationMode.STEP:
                case Schema2.AnimationInterpolationMode.LINEAR:
                    {
                        var isLinear = curve.InterpolationMode == Schema2.AnimationInterpolationMode.LINEAR;

                        foreach (var (key, value) in curve.GetLinearKeys())
                        {
                            this.SetPoint(key, value, isLinear);
                        }

                        #if DEBUG
                        foreach (var (key, value) in curve.GetLinearKeys())
                        {
                            var dstKey = _Keys[key];
                            System.Diagnostics.Debug.Assert(dstKey.Degree <= 1);
                            System.Diagnostics.Debug.Assert(AreEqual(dstKey.Point, value));
                        }
                        #endif

                        break;
                    }

                case Schema2.AnimationInterpolationMode.CUBICSPLINE:
                    {
                        foreach (var (key, value) in curve.GetCubicKeys())
                        {
                            this.SetPoint(key, value.Value);
                            this.SetIncomingTangent(key, value.TangentIn);
                            this.SetOutgoingTangent(key, value.TangentOut);
                        }

                        #if DEBUG
                        foreach (var (key, value) in curve.GetCubicKeys())
                        {
                            var dstKey = _Keys[key];
                            System.Diagnostics.Debug.Assert(dstKey.Degree == 3);
                            System.Diagnostics.Debug.Assert(AreEqual(dstKey.Point, value.Value));
                            System.Diagnostics.Debug.Assert(AreEqual(dstKey.IncomingTangent, value.TangentIn));
                            System.Diagnostics.Debug.Assert(AreEqual(dstKey.OutgoingTangent, value.TangentOut));
                        }
                        #endif

                        break;
                    }

                default: throw new NotImplementedException();
            }
        }

        #endregion

        #region With* API

        public CurveBuilder<T> WithPoint(float offset, T value, bool isLinear = true)
        {
            SetPoint(offset, value, isLinear);
            return this;
        }

        public CurveBuilder<T> WithIncomingTangent(float offset, T tangent)
        {
            SetIncomingTangent(offset, tangent);
            return this;
        }

        public CurveBuilder<T> WithOutgoingTangent(float offset, T tangent)
        {
            SetOutgoingTangent(offset, tangent);
            return this;
        }

        public CurveBuilder<T> WithPoint(float offset, params float[] values)
        {
            return WithPoint(offset, CreateValue(values));
        }

        public CurveBuilder<T> WithOutgoingTangent(float offset, params float[] values)
        {
            return WithOutgoingTangent(offset, CreateValue(values));
        }

        public CurveBuilder<T> WithIncomingTangent(float offset, params float[] values)
        {
            return WithIncomingTangent(offset, CreateValue(values));
        }

        #endregion

        #region IConvertibleCurve API

        #pragma warning disable CA1033 // Interface methods should be callable by child types

        IReadOnlyDictionary<float, T> IConvertibleCurve<T>.ToStepCurve()
        {
            if (MaxDegree != 0) throw new NotSupportedException();
            if (_Keys.Count == 0) return new Dictionary<float, T>();

            return _Keys.ToDictionary(item => item.Key, item => CloneValue(item.Value.Point));
        }

        IReadOnlyDictionary<float, T> IConvertibleCurve<T>.ToLinearCurve()
        {
            if (_Keys.Count == 0) return new Dictionary<float, T>();

            if (_Keys.All(item => item.Value.Degree == 1))
            {
                return _Keys.ToDictionary(item => item.Key, item => CloneValue(item.Value.Point));
            }

            var d = new Dictionary<float, T>();

            if (_Keys.Count == 0) return d;

            if (Keys.Count == 1)
            {
                var k = _Keys.First();
                d[k.Key] = k.Value.Point;
                return d;
            }

            var orderedKeys = _Keys.Keys.ToList();

            for (int i = 0; i < orderedKeys.Count - 1; ++i)
            {
                var a = orderedKeys[i + 0];
                var b = orderedKeys[i + 1];

                var sa = _Keys[a];
                var sb = _Keys[b];

                switch (sa.Degree)
                {
                    case 0: // simulate a step with an extra key
                        d[a] = sa.Point;
                        d[b - float.Epsilon] = sa.Point;
                        d[b] = sb.Point;
                        break;

                    case 1:
                        d[a] = sa.Point;
                        d[b] = sb.Point;
                        break;

                    case 3:
                        var t = a;
                        while (t < b)
                        {
                            d[t] = this.GetPoint(t);
                            t += 1.0f / 30.0f;
                        }

                        break;

                    default: throw new NotImplementedException();
                }
            }

            return d;
        }

        IReadOnlyDictionary<float, (T TangentIn, T Value, T TangentOut)> IConvertibleCurve<T>.ToSplineCurve()
        {
            if (_Keys.Count == 0) return new Dictionary<float, (T TangentIn, T Value, T TangentOut)>();

            if (_Keys.All(item => item.Value.Degree == 3))
            {
                return _Keys.ToDictionary
                    (
                    item => item.Key,
                    item => (item.Value.IncomingTangent, item.Value.Point, item.Value.OutgoingTangent)
                    );
            }

            var d = new Dictionary<float, (T, T, T)>();

            var orderedKeys = _Keys.Keys.ToList();

            for (int i = 0; i < orderedKeys.Count - 1; ++i)
            {
                var a = orderedKeys[i + 0];
                var b = orderedKeys[i + 1];

                var sa = _Keys[a];
                var sb = _Keys[b];

                if (!d.TryGetValue(a, out (T, T, T) da)) da = default;
                if (!d.TryGetValue(b, out (T, T, T) db)) db = default;

                da.Item2 = sa.Point;
                db.Item2 = sb.Point;

                var delta = GetTangent(da.Item2, db.Item2);

                switch (sa.Degree)
                {
                    case 0: // simulate a step with an extra key
                        da.Item3 = default;
                        d[b - float.Epsilon] = (default, sa.Point, delta);
                        db.Item1 = delta;
                        break;

                    case 1: // tangents are the delta between points
                        da.Item3 = db.Item1 = delta;
                        break;

                    case 3: // actual tangents
                        da.Item3 = sa.OutgoingTangent;
                        db.Item1 = sb.IncomingTangent;
                        break;

                    default: throw new NotImplementedException();
                }

                d[a] = da;
                d[b] = db;
            }

            return d;
        }

        #pragma warning restore CA1033 // Interface methods should be callable by child types

        #endregion
    }

    [System.Diagnostics.DebuggerDisplay("{ToDebuggerDisplayString(),nq}")]
    struct _CurveNode<T>
    {
        #region diagnostics

        private string ToDebuggerDisplayString()
        {
            switch (Degree)
            {
                case 0: return $"{_ToString(Point)}";
                case 1: return $"{_ToString(Point)}";
                case 3: return $"{_ToString(IncomingTangent)} -> ({_ToString(Point)}) -> {_ToString(OutgoingTangent)}";
            }

            return "Unsupported";
        }

        private static string _ToString(T value)
        {
            if (value is ArraySegment<float> segm)
            {
                if (segm.Count < 20) return string.Join(" ", segm.ToArray());
                return Transforms.SparseWeight8.Create(segm.ToArray()).ToString();
            }

            if (value is Transforms.SparseWeight8 sparse) return sparse.ToString();

            return value.ToString();
        }

        #endregion

        #region constructors
        public _CurveNode(T value, bool isLinear)
        {
            Degree = isLinear ? 1 : 0;
            IncomingTangent = default;
            Point = value;
            OutgoingTangent = default;
        }

        public _CurveNode(T incoming, T value, T outgoing)
        {
            Degree = 3;
            IncomingTangent = incoming;
            Point = value;
            OutgoingTangent = outgoing;
        }

        public _CurveNode<T> Clone(Func<T, T> cloneValue)
        {
            return new _CurveNode<T>
            {
                Degree = this.Degree,
                IncomingTangent = cloneValue(this.IncomingTangent),
                Point = cloneValue(this.Point),
                OutgoingTangent = cloneValue(this.OutgoingTangent)
            };
        }

        #endregion

        #region data

        public int Degree;
        public T IncomingTangent;
        public T Point;
        public T OutgoingTangent;

        #endregion
    }
}
