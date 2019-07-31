using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

using SPARSE = SharpGLTF.Transforms.SparseWeight8;

namespace SharpGLTF.Animations
{
    static class CurveFactory
    {
        public static CurveBuilder<T> CreateCurveBuilder<T>()
            where T : struct
        {
            if (typeof(T) == typeof(Vector3)) return new Vector3CurveBuilder() as CurveBuilder<T>;
            if (typeof(T) == typeof(Quaternion)) return new QuaternionCurveBuilder() as CurveBuilder<T>;
            if (typeof(T) == typeof(SPARSE)) return new SparseCurveBuilder() as CurveBuilder<T>;

            throw new ArgumentException($"{nameof(T)} not supported.", nameof(T));
        }

        public static CurveBuilder<T> CreateCurveBuilder<T>(ICurveSampler<T> curve)
            where T : struct
        {
            if (curve is Vector3CurveBuilder v3cb) return v3cb.Clone() as CurveBuilder<T>;
            if (curve is QuaternionCurveBuilder q4cb) return q4cb.Clone() as CurveBuilder<T>;
            if (curve is SparseCurveBuilder sscb) return sscb.Clone() as CurveBuilder<T>;

            throw new ArgumentException($"{nameof(T)} not supported.", nameof(T));
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
                    return SamplerFactory.InterpolateCubic
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
                    return SamplerFactory.InterpolateCubic
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

    [System.Diagnostics.DebuggerTypeProxy(typeof(Debug._CurveBuilderDebugProxySparse))]
    sealed class SparseCurveBuilder : CurveBuilder<SPARSE>, ICurveSampler<SPARSE>
    {
        #region lifecycle

        public SparseCurveBuilder() { }

        private SparseCurveBuilder(SparseCurveBuilder other)
            : base(other)
        {
        }

        public override CurveBuilder<SPARSE> Clone() { return new SparseCurveBuilder(this); }

        #endregion

        #region API

        protected override SPARSE CreateValue(params Single[] values)
        {
            return SPARSE.Create(values);
        }

        protected override SPARSE GetTangent(SPARSE fromValue, SPARSE toValue)
        {
            return SPARSE.Subtract(toValue, fromValue);
        }

        public override SPARSE GetPoint(Single offset)
        {
            var sample = FindSample(offset);

            switch (sample.Item1.Degree)
            {
                case 0:
                    return sample.Item1.Point;

                case 1:
                    return SPARSE.InterpolateLinear(sample.Item1.Point, sample.Item2.Point, sample.Item3);

                case 3:
                    return SPARSE.InterpolateCubic
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
