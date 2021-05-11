using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;

using SEGMENT = System.ArraySegment<float>;
using SPARSE = SharpGLTF.Transforms.SparseWeight8;

namespace SharpGLTF.Animations
{
    interface ISamplerTraits<T>
    {
        T Clone(T value);
        T InterpolateLinear(T left, T right, float amount);
        T InterpolateCubic(T start, T outgoingTangent, T end, T incomingTangent, Single amount);
    }

    static class SamplerTraits
    {
        sealed class _Vector3 : ISamplerTraits<Vector3>
        {
            public Vector3 Clone(Vector3 value) { return value; }
            public Vector3 InterpolateLinear(Vector3 left, Vector3 right, float amount) { return System.Numerics.Vector3.Lerp(left, right, amount); }
            public Vector3 InterpolateCubic(Vector3 start, Vector3 outgoingTangent, Vector3 end, Vector3 incomingTangent, Single amount)
            {
                return CurveSampler.InterpolateCubic(start, outgoingTangent, end, incomingTangent, amount);
            }
        }

        sealed class _Quaternion : ISamplerTraits<Quaternion>
        {
            public Quaternion Clone(Quaternion value) { return value; }
            public Quaternion InterpolateLinear(Quaternion left, Quaternion right, float amount) { return System.Numerics.Quaternion.Slerp(left, right, amount); }
            public Quaternion InterpolateCubic(Quaternion start, Quaternion outgoingTangent, Quaternion end, Quaternion incomingTangent, Single amount)
            {
                return CurveSampler.InterpolateCubic(start, outgoingTangent, end, incomingTangent, amount);
            }
        }

        sealed class _Array : ISamplerTraits<Single[]>
        {
            public Single[] Clone(Single[] value) { return (Single[])value.Clone(); }
            public float[] InterpolateLinear(float[] left, float[] right, float amount)
            {
                return CurveSampler.InterpolateLinear(left, right, amount);
            }

            public float[] InterpolateCubic(float[] start, float[] outgoingTangent, float[] end, float[] incomingTangent, float amount)
            {
                return CurveSampler.InterpolateCubic(start, outgoingTangent, end, incomingTangent, amount);
            }
        }

        sealed class _Segment : ISamplerTraits<SEGMENT>
        {
            public SEGMENT Clone(SEGMENT value) { return new SEGMENT(value.ToArray()); }
            public SEGMENT InterpolateLinear(SEGMENT left, SEGMENT right, float amount)
            {
                return new SEGMENT(CurveSampler.InterpolateLinear(left, right, amount));
            }

            public SEGMENT InterpolateCubic(SEGMENT start, SEGMENT outgoingTangent, SEGMENT end, SEGMENT incomingTangent, Single amount)
            {
                return new SEGMENT(CurveSampler.InterpolateCubic(start, outgoingTangent, end, incomingTangent, amount));
            }
        }

        sealed class _Sparse : ISamplerTraits<SPARSE>
        {
            public SPARSE Clone(SPARSE value) { return value; }
            public SPARSE InterpolateLinear(SPARSE left, SPARSE right, float amount)
            {
                return SPARSE.InterpolateLinear(left, right, amount);
            }

            public SPARSE InterpolateCubic(SPARSE start, SPARSE outgoingTangent, SPARSE end, SPARSE incomingTangent, Single amount)
            {
                return SPARSE.InterpolateCubic(start, outgoingTangent, end, incomingTangent, amount);
            }
        }

        public static readonly ISamplerTraits<Vector3> Vector3 = new _Vector3();
        public static readonly ISamplerTraits<Quaternion> Quaternion = new _Quaternion();
        public static readonly ISamplerTraits<Single[]> Array = new _Array();
        public static readonly ISamplerTraits<SPARSE> Sparse = new _Sparse();
        public static readonly ISamplerTraits<SEGMENT> Segment = new _Segment();
    }
}
