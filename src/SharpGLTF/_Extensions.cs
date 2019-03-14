using System;
using System.Collections.Generic;
using System.Text;
using System.Numerics;
using System.Linq;

namespace SharpGLTF
{
    using Schema2;

    /// <summary>
    /// Extensions used internally.
    /// </summary>
    static class _Extensions
    {
        #region private numerics extensions

        internal static bool IsMultipleOf(this int value, int mult)
        {
            return (value % mult) == 0;
        }

        internal static int PaddingSize(this int size, int mult)
        {
            var rest = size % mult;

            return rest == 0 ? 0 : mult - rest;
        }

        internal static bool _IsReal(this float value)
        {
            return !(float.IsNaN(value) | float.IsInfinity(value));
        }

        internal static bool _IsReal(this Vector2 v)
        {
            return v.X._IsReal() & v.Y._IsReal();
        }

        internal static bool _IsReal(this Vector3 v)
        {
            return v.X._IsReal() & v.Y._IsReal() & v.Z._IsReal();
        }

        internal static bool _IsReal(this Vector4 v)
        {
            return v.X._IsReal() & v.Y._IsReal() & v.Z._IsReal() & v.W._IsReal();
        }

        internal static Vector3 WithLength(this Vector3 v, float len)
        {
            return Vector3.Normalize(v) * len;
        }

        internal static Quaternion AsQuaternion(this Vector4 v)
        {
            return new Quaternion(v.X, v.Y, v.Z, v.W);
        }

        internal static Boolean IsNormalized(this Quaternion q)
        {
            // As per: https://github.com/KhronosGroup/glTF-Validator/issues/33 , quaternions need to be normalized.

            return Math.Abs(1.0 - q.Length()) > 0.000005;
        }

        internal static Quaternion Sanitized(this Quaternion q)
        {
            return q.IsNormalized() ? q : Quaternion.Normalize(q);
        }

        #endregion

        #region base64

        internal static Byte[] _TryParseBase64Unchecked(this string uri, string prefix)
        {
            if (uri == null) return null;
            if (!uri.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)) return null;

            var content = uri.Substring(prefix.Length);
            return Convert.FromBase64String(content);
        }

        #endregion

        #region linq

        internal static ArraySegment<T> Slice<T>(this T[] array, int offset)
        {
            return new ArraySegment<T>(array, offset, array.Length - offset);
        }

        internal static ArraySegment<T> Slice<T>(this ArraySegment<T> array, int offset)
        {
            return new ArraySegment<T>(array.Array, array.Offset + offset, array.Count - offset);
        }

        internal static ArraySegment<T> Slice<T>(this ArraySegment<T> array, int offset, int count)
        {
            return new ArraySegment<T>(array.Array, array.Offset + offset, count);
        }

        internal static T[] CloneArray<T>(this T[] srcArray)
        {
            if (srcArray == null) return null;

            var dstArray = new T[srcArray.Length];
            srcArray.CopyTo(dstArray, 0);

            return dstArray;
        }

        internal static void Fill<T>(this IList<T> collection, T value)
        {
            for (int i = 0; i < collection.Count; ++i)
            {
                collection[i] = value;
            }
        }

        internal static void Fill<T>(this T[] array, T value)
        {
            for (int i = 0; i < array.Length; ++i)
            {
                array[i] = value;
            }
        }

        internal static int IndexOf<T>(this IReadOnlyList<T> collection, T value)
        {
            var l = collection.Count;

            for (int i = 0; i < l; ++i)
            {
                if (Object.Equals(collection[i], value)) return i;
            }

            return -1;
        }

        internal static int IndexOf<T>(this IReadOnlyList<T> collection, Predicate<T> predicate)
        {
            var l = collection.Count;

            for (int i = 0; i < l; ++i)
            {
                if (predicate(collection[i])) return i;
            }

            return -1;
        }

        internal static int IndexOfReference<T>(this IReadOnlyList<T> collection, T value)
            where T : class
        {
            var l = collection.Count;

            for (int i = 0; i < l; ++i)
            {
                if (Object.ReferenceEquals(collection[i], value)) return i;
            }

            return -1;
        }

        internal static int IndexOf<T>(this IReadOnlyList<T> collection, T[] subset)
            where T : IEquatable<T>
        {
            var l = collection.Count - subset.Length;

            for (int i = 0; i < l; ++i)
            {
                bool r = false;

                for (int j = 0; j < subset.Length; ++j)
                {
                    if (!collection[i + j].Equals(subset[j])) break;
                    r = true;
                }

                if (r) return i;
            }

            return -1;
        }

        internal static void CopyTo<T>(this T[] src, int srcOffset, IList<T> dst, int dstOffset, int count)
        {
            var srcArray = new ArraySegment<T>(src);

            srcArray.CopyTo(srcOffset, dst, dstOffset, count);
        }

        internal static void CopyTo<T>(this ArraySegment<T> src, int srcOffset, IList<T> dst, int dstOffset, int count)
        {
            if (dst is T[] dstArray)
            {
                Array.Copy(src.Array, src.Offset + srcOffset, dstArray, dstOffset, count);
                return;
            }

            for (int i = 0; i < count; ++i)
            {
                dst[dstOffset + i] = src.Array[src.Offset + srcOffset + i];
            }
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
            if (value.CompareTo(minval) < 0) value = minval;
            if (value.CompareTo(maxval) > 0) value = maxval;

            return value.Equals(defval) ? (T?)null : value;
        }

        internal static Vector3? AsNullable(this Vector3 value, Vector3 defval, Vector3 minval, Vector3 maxval)
        {
            value = Vector3.Min(value, minval);
            value = Vector3.Max(value, maxval);

            return value.Equals(defval) ? (Vector3?)null : value;
        }

        internal static Vector4? AsNullable(this Vector4 value, Vector4 defval, Vector4 minval, Vector4 maxval)
        {
            value = Vector4.Min(value, minval);
            value = Vector4.Max(value, maxval);

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

        internal static String AsName(this string name)
        {
            return string.IsNullOrWhiteSpace(name) ? null : name;
        }

        internal static void AddRange<Tin, Tout>(this IList<Tout> dst, IEnumerable<Tin> src, Func<Tin, Tout> cvt)
        {
            foreach (var item in src)
            {
                dst.Add(cvt(item));
            }
        }

        internal static IEnumerable<T> Concat<T>(this IEnumerable<T> collection, params T[] instances)
        {
            return collection.Concat(instances.Where(item => item != null));
        }

        #endregion

        #region vertex & index accessors

        public static int ByteLength(this IndexEncodingType encoding)
        {
            switch (encoding)
            {
                case IndexEncodingType.UNSIGNED_BYTE: return 1;
                case IndexEncodingType.UNSIGNED_SHORT: return 2;
                case IndexEncodingType.UNSIGNED_INT: return 4;

                default: throw new NotImplementedException();
            }
        }

        public static int ByteLength(this EncodingType encoding)
        {
            switch (encoding)
            {
                case EncodingType.BYTE: return 1;
                case EncodingType.SHORT: return 2;
                case EncodingType.FLOAT: return 4;

                case EncodingType.UNSIGNED_BYTE: return 1;
                case EncodingType.UNSIGNED_SHORT: return 2;
                case EncodingType.UNSIGNED_INT: return 4;

                default: throw new NotImplementedException();
            }
        }

        public static EncodingType ToComponent(this IndexEncodingType t)
        {
            switch (t)
            {
                case IndexEncodingType.UNSIGNED_BYTE: return EncodingType.UNSIGNED_BYTE;
                case IndexEncodingType.UNSIGNED_SHORT: return EncodingType.UNSIGNED_SHORT;
                case IndexEncodingType.UNSIGNED_INT: return EncodingType.UNSIGNED_INT;

                default: throw new NotImplementedException();
            }
        }

        public static IndexEncodingType ToIndex(this EncodingType t)
        {
            switch (t)
            {
                case EncodingType.UNSIGNED_BYTE: return IndexEncodingType.UNSIGNED_BYTE;
                case EncodingType.UNSIGNED_SHORT: return IndexEncodingType.UNSIGNED_SHORT;
                case EncodingType.UNSIGNED_INT: return IndexEncodingType.UNSIGNED_INT;

                default: throw new NotImplementedException();
            }
        }

        public static int DimCount(this DimensionType dimension)
        {
            switch (dimension)
            {
                case DimensionType.SCALAR: return 1;
                case DimensionType.VEC2: return 2;
                case DimensionType.VEC3: return 3;
                case DimensionType.VEC4: return 4;
                case DimensionType.MAT2: return 4;
                case DimensionType.MAT3: return 9;
                case DimensionType.MAT4: return 16;
                default: throw new NotImplementedException();
            }
        }

        internal static DimensionType ToDimension(this int l)
        {
            switch (l)
            {
                case 1: return DimensionType.SCALAR;
                case 2: return DimensionType.VEC2;
                case 3: return DimensionType.VEC3;
                case 4: return DimensionType.VEC4;
                // case 4: return ElementType.MAT2;
                case 9: return DimensionType.MAT3;
                case 16: return DimensionType.MAT4;
                default: throw new NotImplementedException();
            }
        }

        #endregion

        #region serialization

        public static ArraySegment<Byte> ToArraySegment(this System.IO.MemoryStream m)
        {
            if (m.TryGetBuffer(out ArraySegment<Byte> data)) return data;
            return new ArraySegment<byte>(m.ToArray());
        }

        public static Byte[] GetPaddedContent(this Byte[] content)
        {
            if (content == null) return null;

            if (content.Length.IsMultipleOf(4)) return content;

            var paddedContent = new Byte[content.Length + content.Length.PaddingSize(4)];
            content.CopyTo(paddedContent, 0);
            return paddedContent;
        }

        #endregion
    }
}
