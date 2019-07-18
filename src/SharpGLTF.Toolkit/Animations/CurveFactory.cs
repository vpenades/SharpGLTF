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
        protected override bool CheckValue(Vector3 value)
        {
            return true;
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
    }

    [System.Diagnostics.DebuggerTypeProxy(typeof(Debug._CurveBuilderDebugProxyQuaternion))]
    sealed class QuaternionCurveBuilder : CurveBuilder<Quaternion>, ICurveSampler<Quaternion>
    {
        protected override bool CheckValue(Quaternion value)
        {
            return true;
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
    }

    [System.Diagnostics.DebuggerTypeProxy(typeof(Debug._CurveBuilderDebugProxyArray))]
    sealed class ArrayCurveBuilder : CurveBuilder<Single[]>, ICurveSampler<Single[]>
    {
        // the first "CheckValue" will fix any further calls to this value.
        private int _ValueLength = 0;

        protected override bool CheckValue(Single[] value)
        {
            if (value == null || value.Length == 0) return false;

            if (_ValueLength == 0) _ValueLength = value.Length;

            return value.Length == _ValueLength;
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
    }
}
