using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace SharpGLTF
{
    public static class VectorsUtils
    {
        public static bool IsFinite(this float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value);
        }

        public static Single NextSingle(this Random rnd)
        {
            return (Single)rnd.NextDouble();
        }

        public static Vector2 NextVector2(this Random rnd)
        {
            return new Vector2(rnd.NextSingle(), rnd.NextSingle());
        }

        public static Vector3 NextVector3(this Random rnd)
        {
            return new Vector3(rnd.NextSingle(), rnd.NextSingle(), rnd.NextSingle());
        }

        public static Vector4 NextVector4(this Random rnd)
        {
            return new Vector4(rnd.NextSingle(), rnd.NextSingle(), rnd.NextSingle(), rnd.NextSingle());
        }

        public static float GetAngle(this (Quaternion a, Quaternion b) pair)
        {
            var w = Quaternion.Concatenate(pair.b, Quaternion.Inverse(pair.a)).W;

            if (w < -1) w = -1;
            if (w > 1) w = 1;

            return (float)Math.Acos(w) * 2;
        }

        public static float GetAngle(this (Vector3 a, Vector3 b) pair)
        {
            var a = Vector3.Normalize(pair.a);
            var b = Vector3.Normalize(pair.b);

            var c = Vector3.Dot(a, b);
            if (c > 1) c = 1;
            if (c < -1) c = -1;

            return (float)Math.Acos(c);
        }

        public static float GetAngle(this (Vector2 a, Vector2 b) pair)
        {
            var a = Vector2.Normalize(pair.a);
            var b = Vector2.Normalize(pair.b);

            var c = Vector2.Dot(a, b);
            if (c > 1) c = 1;
            if (c < -1) c = -1;

            return (float)Math.Acos(c);
        }

        public static (Vector3, Vector3) GetBounds(this IEnumerable<Vector3> collection)
        {
            var min = new Vector3(float.MaxValue);
            var max = new Vector3(float.MinValue);

            foreach (var v in collection)
            {
                min = Vector3.Min(v, min);
                max = Vector3.Max(v, max);
            }

            return (min, max);
        }

        public static Vector3 GetMin(this IEnumerable<Vector3> collection)
        {
            var min = new Vector3(float.MaxValue);

            foreach (var v in collection)
            {
                min = Vector3.Min(v, min);
            }

            return min;
        }

        public static Vector3 GetMax(this IEnumerable<Vector3> collection)
        {
            var max = new Vector3(float.MinValue);

            foreach (var v in collection)
            {
                max = Vector3.Max(v, max);
            }

            return max;
        }
    }
}
