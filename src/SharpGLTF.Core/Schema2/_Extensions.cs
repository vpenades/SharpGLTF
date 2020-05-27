using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace SharpGLTF.Schema2
{
    /// <summary>
    /// Extensions used internally.
    /// </summary>
    static class _Schema2Extensions
    {
        #region morph weights

        public static void SetMorphWeights(this IList<Double> list, int maxCount, Transforms.SparseWeight8 weights)
        {
            while (list.Count > maxCount) list.RemoveAt(list.Count - 1);
            while (list.Count < maxCount) list.Add(0);

            if (list.Count > 0)
            {
                foreach (var (index, weight) in weights.GetIndexedWeights())
                {
                    list[index] = weight;
                }
            }
        }

        #endregion

        #region nullables

        internal static String AsName(this string name)
        {
            return string.IsNullOrWhiteSpace(name) ? null : name;
        }

        internal static T AsValue<T>(this T? value, T defval)
            where T : struct
        {
            return value ?? defval;
        }

        internal static T? AsNullable<T>(this T value, T defval)
            where T : struct
        {
            return value.Equals(defval) ? (T?)null : value;
        }

        internal static T? AsNullable<T>(this T value, T defval, T minval, T maxval)
            where T : struct, IEquatable<T>, IComparable<T>
        {
            if (value.Equals(defval)) return null;

            if (value.CompareTo(minval) < 0) value = minval;
            if (value.CompareTo(maxval) > 0) value = maxval;

            return value.Equals(defval) ? (T?)null : value;
        }

        internal static Vector2? AsNullable(this Vector2 value, Vector2 defval, Vector2 minval, Vector2 maxval)
        {
            if (value.Equals(defval)) return null;

            value = Vector2.Min(value, maxval);
            value = Vector2.Max(value, minval);

            return value.Equals(defval) ? (Vector2?)null : value;
        }

        internal static Vector3? AsNullable(this Vector3 value, Vector3 defval, Vector3 minval, Vector3 maxval)
        {
            if (value.Equals(defval)) return null;

            value = Vector3.Min(value, maxval);
            value = Vector3.Max(value, minval);

            return value.Equals(defval) ? (Vector3?)null : value;
        }

        internal static Vector4? AsNullable(this Vector4 value, Vector4 defval, Vector4 minval, Vector4 maxval)
        {
            if (value.Equals(defval)) return (Vector4?)null;

            value = Vector4.Min(value, maxval);
            value = Vector4.Max(value, minval);

            return value.Equals(defval) ? (Vector4?)null : value;
        }

        internal static String AsNullable(this string value)
        {
            return string.IsNullOrEmpty(value) ? null : value;
        }

        internal static String AsEmptyNullable(this string value)
        {
            return string.IsNullOrWhiteSpace(value) ? null : value;
        }

        #endregion
    }
}
