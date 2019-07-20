using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace SharpGLTF.Animations
{
    static class CurveFactory
    {
        public static CurveBuilder<T> CreateCurveBuilder<T>()
        {
            if (typeof(T) == typeof(Vector3)) return new Vector3CurveBuilder() as CurveBuilder<T>;
            if (typeof(T) == typeof(Quaternion)) return new QuaternionCurveBuilder() as CurveBuilder<T>;
            if (typeof(T) == typeof(Single[])) return new ArrayCurveBuilder() as CurveBuilder<T>;

            throw new ArgumentException(nameof(T), "Generic argument not supported");
        }
    }

    [System.Diagnostics.DebuggerTypeProxy(typeof(Debug._CurveBuilderDebugProxyVector3))]
    sealed class Vector3CurveBuilder : CurveBuilder<Vector3>, ICurveSampler<Vector3>
    {
        #region lifecycle
        public Vector3CurveBuilder() { }

        private Vector3CurveBuilder(Vector3CurveBuilder other)
            : base(other) { }

        public override CurveBuilder<Vector3> Clone() { return new Vector3CurveBuilder(this); }

        #endregion

        #region API

        protected override Vector3 IsolateValue(Vector3 value)
        {
            return value;
        }

        protected override Vector3 CreateValue(params float[] values)
        {
            Guard.NotNull(values, nameof(values));
            Guard.IsTrue(values.Length == 3, nameof(values));
            return new Vector3(values[0], values[1], values[2]);
        }

        protected override Vector3 GetTangent(Vector3 fromValue, Vector3 toValue)
        {
            return SamplerFactory.CreateTangent(fromValue, toValue);
        }

        public override Vector3 GetPoint(Single offset)
        {
            var sample = FindSample(offset);

            switch (sample.Item1.Degree)
            {
                case 0:
                    return sample.Item1.Point;

                case 1:
                    return Vector3.Lerp(sample.Item1.Point, sample.Item2.Point, sample.Item3);

                case 3:
                    return SamplerFactory.CubicLerp
                            (
                            sample.Item1.Point, sample.Item1.OutgoingTangent,
                            sample.Item2.Point, sample.Item2.IncomingTangent,
                            sample.Item3
                            );

                default:
                    throw new NotSupportedException();
            }
        }

        #endregion
    }

    [System.Diagnostics.DebuggerTypeProxy(typeof(Debug._CurveBuilderDebugProxyQuaternion))]
    sealed class QuaternionCurveBuilder : CurveBuilder<Quaternion>, ICurveSampler<Quaternion>
    {
        #region lifecycle

        public QuaternionCurveBuilder() { }

        private QuaternionCurveBuilder(QuaternionCurveBuilder other)
            : base(other) { }

        public override CurveBuilder<Quaternion> Clone() { return new QuaternionCurveBuilder(this); }

        #endregion

        #region API

        protected override Quaternion IsolateValue(Quaternion value)
        {
            return value;
        }

        protected override Quaternion CreateValue(params float[] values)
        {
            Guard.NotNull(values, nameof(values));
            Guard.IsTrue(values.Length == 4, nameof(values));
            return new Quaternion(values[0], values[1], values[2], values[3]);
        }

        protected override Quaternion GetTangent(Quaternion fromValue, Quaternion toValue)
        {
            return SamplerFactory.CreateTangent(fromValue, toValue);
        }

        public override Quaternion GetPoint(float offset)
        {
            var sample = FindSample(offset);

            switch (sample.Item1.Degree)
            {
                case 0:
                    return sample.Item1.Point;

                case 1:
                    return Quaternion.Slerp(sample.Item1.Point, sample.Item2.Point, sample.Item3);

                case 3:
                    return SamplerFactory.CubicLerp
                            (
                            sample.Item1.Point, sample.Item1.OutgoingTangent,
                            sample.Item2.Point, sample.Item2.IncomingTangent,
                            sample.Item3
                            );

                default:
                    throw new NotSupportedException();
            }
        }

        #endregion
    }

    [System.Diagnostics.DebuggerTypeProxy(typeof(Debug._CurveBuilderDebugProxyArray))]
    sealed class ArrayCurveBuilder : CurveBuilder<Single[]>, ICurveSampler<Single[]>
    {
        #region lifecycle

        public ArrayCurveBuilder() { }

        private ArrayCurveBuilder(ArrayCurveBuilder other)
            : base(other)
        {
            System.Diagnostics.Debug.Assert(other._ValueLength == this._ValueLength);
        }

        public override CurveBuilder<Single[]> Clone() { return new ArrayCurveBuilder(this); }

        #endregion

        #region data

        // the first "CheckValue" will fix any further calls to this value.
        private int _ValueLength = 0;

        #endregion

        #region API

        protected override Single[] IsolateValue(Single[] value)
        {
            Guard.NotNull(value, nameof(value));
            Guard.MustBeGreaterThan(value.Length, 0, nameof(value));

            if (_ValueLength == 0) _ValueLength = value.Length;

            Guard.MustBeBetweenOrEqualTo(value.Length, _ValueLength, _ValueLength, nameof(value));

            var clone = new Single[_ValueLength];
            value.CopyTo(clone, 0);

            return clone;
        }

        protected override Single[] CreateValue(params Single[] values)
        {
            return values;
        }

        protected override Single[] GetTangent(Single[] fromValue, Single[] toValue)
        {
            return SamplerFactory.CreateTangent(fromValue, toValue);
        }

        public override Single[] GetPoint(Single offset)
        {
            var sample = FindSample(offset);

            switch (sample.Item1.Degree)
            {
                case 0:
                    return sample.Item1.Point;

                case 1:
                    return SamplerFactory.Lerp(sample.Item1.Point, sample.Item2.Point, sample.Item3);

                case 3:
                    return SamplerFactory.CubicLerp
                            (
                            sample.Item1.Point, sample.Item1.OutgoingTangent,
                            sample.Item2.Point, sample.Item2.IncomingTangent,
                            sample.Item3
                            );

                default:
                    throw new NotSupportedException();
            }
        }

        #endregion
    }
}
