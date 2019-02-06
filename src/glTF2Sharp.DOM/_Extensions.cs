using System;
using System.Collections.Generic;
using System.Text;
using System.Numerics;

namespace glTF2Sharp
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

        internal static bool _IsReal(this float value)
        {
            return !(float.IsNaN(value) | float.IsInfinity(value));
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

        internal static int IndexOfReference<T>(this IReadOnlyList<T> collection, T value) where T : class
        {
            var l = collection.Count;

            for (int i = 0; i < l; ++i)
            {
                if (Object.ReferenceEquals(collection[i], value)) return i;
            }

            return -1;
        }

        internal static int IndexOf<T>(this IReadOnlyList<T> collection, T[] subset) where T : IEquatable<T>
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

        internal static ArraySegment<T> GetSegment<T>(this ArraySegment<T> array, int offset, int count)
        {
            return new ArraySegment<T>(array.Array, array.Offset + offset, count);
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

        internal static T AsValue<T>(this T? value, T defval) where T : struct
        {
            return value ?? defval;
        }

        internal static T? AsNullable<T>(this T value, T defval) where T : struct
        {
            return value.Equals(defval) ? (T?)null : value;
        }

        internal static T? AsNullable<T>(this T value, T defval, T minval, T maxval) where T : struct, IEquatable<T>, IComparable<T>
        {
            if (value.Equals(defval)) return null;

            if (value.CompareTo(minval) < 0) return minval;
            if (value.CompareTo(maxval) > 0) return maxval;

            return value;
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

        #endregion

        #region vertex & index accessors

        public static int ByteLength(this IndexType encoding)
        {
            switch (encoding)
            {
                case IndexType.UNSIGNED_BYTE: return 1;
                case IndexType.UNSIGNED_SHORT: return 2;
                case IndexType.UNSIGNED_INT: return 4;

                default: throw new NotImplementedException();
            }
        }

        public static int ByteLength(this ComponentType encoding)
        {
            switch (encoding)
            {
                case ComponentType.BYTE: return 1;
                case ComponentType.SHORT: return 2;
                case ComponentType.FLOAT: return 4;

                case ComponentType.UNSIGNED_BYTE: return 1;
                case ComponentType.UNSIGNED_SHORT: return 2;
                case ComponentType.UNSIGNED_INT: return 4;

                default: throw new NotImplementedException();
            }
        }

        public static ComponentType ToComponent(this IndexType t)
        {
            switch (t)
            {
                case IndexType.UNSIGNED_BYTE: return ComponentType.UNSIGNED_BYTE;
                case IndexType.UNSIGNED_SHORT: return ComponentType.UNSIGNED_SHORT;
                case IndexType.UNSIGNED_INT: return ComponentType.UNSIGNED_INT;

                default: throw new NotImplementedException();
            }
        }

        public static IndexType ToIndex(this ComponentType t)
        {
            switch (t)
            {
                case ComponentType.UNSIGNED_BYTE: return IndexType.UNSIGNED_BYTE;
                case ComponentType.UNSIGNED_SHORT: return IndexType.UNSIGNED_SHORT;
                case ComponentType.UNSIGNED_INT: return IndexType.UNSIGNED_INT;

                default: throw new NotImplementedException();
            }
        }           

        public static int DimCount(this ElementType dimension)
        {
            switch (dimension)
            {
                case ElementType.SCALAR: return 1;
                case ElementType.VEC2: return 2;
                case ElementType.VEC3: return 3;
                case ElementType.VEC4: return 4;
                case ElementType.MAT2: return 4;
                case ElementType.MAT3: return 9;
                case ElementType.MAT4: return 16;
                default: throw new NotImplementedException();
            }
        }

        internal static ElementType ToDimension(this int l)
        {
            switch (l)
            {
                case 1: return ElementType.SCALAR;
                case 2: return ElementType.VEC2;
                case 3: return ElementType.VEC3;
                case 4: return ElementType.VEC4;
                // case 4: return ElementType.MAT2;
                case 9: return ElementType.MAT3;
                case 16: return ElementType.MAT4;
                default: throw new NotImplementedException();
            }
        }

        #endregion
    }
}
