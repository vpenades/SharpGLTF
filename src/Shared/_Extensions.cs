using System;
using System.Collections.Generic;
using System.Text;
using System.Numerics;
using System.Linq;

using SharpGLTF.Schema2;

namespace SharpGLTF
{
    /// <summary>
    /// Extensions used internally.
    /// </summary>
    static class _Extensions
    {
        #region constants

        // constants from: https://github.com/KhronosGroup/glTF-Validator/blob/master/lib/src/errors.dart

        private const float _UnitLengthThresholdVec3 = 0.00674f;
        private const float _UnitLengthThresholdVec4 = 0.00769f;

        // This value is slightly greater
        // than the maximum error from unsigned 8-bit quantization
        // 1..2 elements - 0 * step
        // 3..4 elements - 1 * step
        // 5..6 elements - 2 * step
        // ...
        private const float _UnitSumThresholdStep = 0.0039216f;

        #endregion

        #region private numerics extensions

        internal static bool IsMultipleOf(this int value, int mult)
        {
            return (value % mult) == 0;
        }

        internal static int WordPadded(this int length)
        {
            var padding = length & 3;

            return length + (padding == 0 ? 0 : 4 - padding);
        }

        internal static bool _IsFinite(this float value)
        {
            return !(float.IsNaN(value) || float.IsInfinity(value));
        }

        internal static bool _IsFinite(this Vector2 v)
        {
            return v.X._IsFinite() && v.Y._IsFinite();
        }

        internal static bool _IsFinite(this Vector3 v)
        {
            return v.X._IsFinite() && v.Y._IsFinite() && v.Z._IsFinite();
        }

        internal static bool _IsFinite(this Vector4 v)
        {
            return v.X._IsFinite() && v.Y._IsFinite() && v.Z._IsFinite() && v.W._IsFinite();
        }

        internal static bool _IsFinite(this Matrix4x4 v)
        {
            if (!(v.M11._IsFinite() && v.M12._IsFinite() && v.M13._IsFinite() && v.M14._IsFinite())) return false;
            if (!(v.M21._IsFinite() && v.M22._IsFinite() && v.M23._IsFinite() && v.M24._IsFinite())) return false;
            if (!(v.M31._IsFinite() && v.M32._IsFinite() && v.M33._IsFinite() && v.M34._IsFinite())) return false;
            if (!(v.M41._IsFinite() && v.M42._IsFinite() && v.M43._IsFinite() && v.M44._IsFinite())) return false;
            return true;
        }

        internal static bool _IsFinite(this Quaternion v)
        {
            return v.X._IsFinite() && v.Y._IsFinite() && v.Z._IsFinite() && v.W._IsFinite();
        }

        internal static Vector3 WithLength(this Vector3 v, float len)
        {
            return Vector3.Normalize(v) * len;
        }

        internal static Boolean IsNormalized(this Vector3 normal)
        {
            if (!normal._IsFinite()) return false;

            return Math.Abs(normal.Length() - 1) <= _UnitLengthThresholdVec3;
        }

        internal static Boolean IsNormalized(this Quaternion rotation)
        {
            if (!rotation._IsFinite()) return false;

            return Math.Abs(rotation.Length() - 1) <= _UnitLengthThresholdVec4;
        }

        internal static Quaternion AsQuaternion(this Vector4 v)
        {
            return new Quaternion(v.X, v.Y, v.Z, v.W);
        }

        internal static Quaternion Sanitized(this Quaternion q)
        {
            return q.IsNormalized() ? q : Quaternion.Normalize(q);
        }

        internal static bool IsInRange(this Vector3 value, Vector3 min, Vector3 max)
        {
            if (value.X < min.X || value.X > max.X) return false;
            if (value.Y < min.Y || value.Y > max.Y) return false;
            if (value.Z < min.Z || value.Z > max.Z) return false;
            return true;
        }

        internal static bool IsInRange(this Vector4 value, Vector4 min, Vector4 max)
        {
            if (value.X < min.X || value.X > max.X) return false;
            if (value.Y < min.Y || value.Y > max.Y) return false;
            if (value.Z < min.Z || value.Z > max.Z) return false;
            if (value.W < min.W || value.W > max.W) return false;
            return true;
        }

        internal static bool IsRound(this Vector4 value)
        {
            var r = new Vector4((int)value.X, (int)value.Y, (int)value.Z, (int)value.W);

            return (value - r) == Vector4.Zero;
        }

        /*
        internal static void Validate(this Vector3 vector, string msg)
        {
            if (!vector._IsFinite()) throw new NotFiniteNumberException($"{msg} is invalid.");
        }*/

        internal static void ValidateNormal(this Vector3 normal, string msg)
        {
            if (!normal._IsFinite()) throw new NotFiniteNumberException($"{msg} is invalid.");

            if (!normal.IsNormalized()) throw new ArithmeticException($"{msg} is not unit length.");
        }

        internal static void ValidateTangent(this Vector4 tangent, string msg)
        {
            if (tangent.W != 1 && tangent.W != -1) throw new ArithmeticException(msg);

            new Vector3(tangent.X, tangent.Y, tangent.Z).ValidateNormal(msg);
        }

        internal static Vector3 SanitizeNormal(this Vector3 normal)
        {
            return normal.IsNormalized() ? normal : Vector3.Normalize(normal);
        }

        internal static bool IsValidTangent(this Vector4 tangent)
        {
            if (tangent.W != 1 && tangent.W != -1) return false;

            return new Vector3(tangent.X, tangent.Y, tangent.Z).IsNormalized();
        }

        internal static Vector4 SanitizeTangent(this Vector4 tangent)
        {
            var n = new Vector3(tangent.X, tangent.Y, tangent.Z).SanitizeNormal();
            var s = float.IsNaN(tangent.W) ? 1 : tangent.W;
            return new Vector4(n, s > 0 ? 1 : -1);
        }

        internal static Matrix4x4 Inverse(this in Matrix4x4 src)
        {
            if (!Matrix4x4.Invert(src, out Matrix4x4 dst)) Guard.IsTrue(false, nameof(src), "Matrix cannot be inverted.");

            return dst;
        }

        internal static bool IsValid(this in Matrix4x4 matrix, bool mustDecompose = true, bool mustInvert = true, bool mustPositiveDeterminant = false)
        {
            if (!matrix._IsFinite()) return false;
            if (mustDecompose && !Matrix4x4.Decompose(matrix, out _, out _, out _)) return false;
            if (mustInvert && !Matrix4x4.Invert(matrix, out _)) return false;
            if (mustPositiveDeterminant && matrix.GetDeterminant() < 0) return false;

            return true;
        }

        #endregion

        #region linq

        internal static int GetContentHashCode<T>(this IEnumerable<T> collection, int count = int.MaxValue)
        {
            if (collection == null) return 0;

            int h = 0;

            // this will handle default(ArraySegment<T>)
            if (collection is IReadOnlyList<T> list)
            {
                count = Math.Min(count, list.Count);

                for (int i = 0; i < count; ++i)
                {
                    var element = list[i];

                    h ^= element == null ? 0 : element.GetHashCode();
                    h *= 17;
                }

                return h;
            }

            foreach (var element in collection.Take(count))
            {
                h ^= element == null ? 0 : element.GetHashCode();
                h *= 17;
            }

            return h;
        }

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

        internal static void AddRange<Tin, Tout>(this IList<Tout> dst, IEnumerable<Tin> src, Converter<Tin, Tout> cvt)
        {
            foreach (var item in src)
            {
                dst.Add(cvt(item));
            }
        }

        internal static IEnumerable<T> ConcatItems<T>(this IEnumerable<T> collection, params T[] instances)
        {
            return collection.Concat(instances.Where(item => item != null));
        }

        public static void SanitizeNormals(this IList<Vector3> normals)
        {
            for (int i = 0; i < normals.Count; ++i)
            {
                if (!normals[i].IsNormalized()) normals[i] = normals[i].SanitizeNormal();
            }
        }

        public static void SanitizeTangents(this IList<Vector4> tangents)
        {
            for (int i = 0; i < tangents.Count; ++i)
            {
                if (!tangents[i].IsValidTangent()) tangents[i] = tangents[i].SanitizeTangent();
            }
        }

        #endregion

        #region vertex & index accessors

        public static String ToDebugString(this EncodingType encoding, DimensionType dimensions, bool normalized)
        {
            var txt = string.Empty;

            switch (encoding)
            {
                case EncodingType.BYTE: txt += "SByte"; break;
                case EncodingType.FLOAT: txt += "Float"; break;
                case EncodingType.SHORT: txt += "SShort"; break;
                case EncodingType.UNSIGNED_BYTE: txt += "UByte"; break;
                case EncodingType.UNSIGNED_INT: txt += "UInt"; break;
                case EncodingType.UNSIGNED_SHORT: txt += "UShort"; break;
            }

            switch (dimensions)
            {
                case DimensionType.SCALAR: break;
                case DimensionType.VEC2: txt += "2"; break;
                case DimensionType.VEC3: txt += "3"; break;
                case DimensionType.VEC4: txt += "4"; break;
                case DimensionType.MAT2: txt += "2x2"; break;
                case DimensionType.MAT3: txt += "3x3"; break;
                case DimensionType.MAT4: txt += "4x4"; break;
            }

            if (normalized) txt = "Norm" + txt;

            return txt;
        }

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

        public static int GetPrimitiveVertexSize(this PrimitiveType ptype)
        {
            switch (ptype)
            {
                case PrimitiveType.POINTS: return 1;
                case PrimitiveType.LINES: return 2;
                case PrimitiveType.LINE_LOOP: return 2;
                case PrimitiveType.LINE_STRIP: return 2;
                case PrimitiveType.TRIANGLES: return 3;
                case PrimitiveType.TRIANGLE_FAN: return 3;
                case PrimitiveType.TRIANGLE_STRIP: return 3;
                default: throw new NotImplementedException();
            }
        }

        public static IEnumerable<(int A, int B)> GetLinesIndices(this PrimitiveType ptype, int vertexCount)
        {
            return ptype.GetLinesIndices(Enumerable.Range(0, vertexCount).Select(item => (UInt32)item));
        }

        public static IEnumerable<(int A, int B, int C)> GetTrianglesIndices(this PrimitiveType ptype, int vertexCount)
        {
            return ptype.GetTrianglesIndices(Enumerable.Range(0, vertexCount).Select(item => (UInt32)item));
        }

        public static IEnumerable<(int A, int B)> GetLinesIndices(this PrimitiveType ptype, IEnumerable<UInt32> sourceIndices)
        {
            switch (ptype)
            {
                case PrimitiveType.LINES:
                    {
                        using (var ptr = sourceIndices.GetEnumerator())
                        {
                            while (true)
                            {
                                if (!ptr.MoveNext()) break;
                                var a = ptr.Current;
                                if (!ptr.MoveNext()) break;
                                var b = ptr.Current;

                                if (!_IsDegenerated(a, b)) yield return ((int)a, (int)b);
                            }
                        }

                        break;
                    }

                default: throw new NotImplementedException();
            }
        }

        public static IEnumerable<(int A, int B, int C)> GetTrianglesIndices(this PrimitiveType ptype, IEnumerable<UInt32> sourceIndices)
        {
            switch (ptype)
            {
                case PrimitiveType.TRIANGLES:
                    {
                        using (var ptr = sourceIndices.GetEnumerator())
                        {
                            while (true)
                            {
                                if (!ptr.MoveNext()) break;
                                var a = ptr.Current;
                                if (!ptr.MoveNext()) break;
                                var b = ptr.Current;
                                if (!ptr.MoveNext()) break;
                                var c = ptr.Current;

                                if (!_IsDegenerated(a, b, c)) yield return ((int)a, (int)b, (int)c);
                            }
                        }

                        break;
                    }

                case PrimitiveType.TRIANGLE_FAN:
                    {
                        using (var ptr = sourceIndices.GetEnumerator())
                        {
                            if (!ptr.MoveNext()) break;
                            var a = ptr.Current;
                            if (!ptr.MoveNext()) break;
                            var b = ptr.Current;

                            while (true)
                            {
                                if (!ptr.MoveNext()) break;
                                var c = ptr.Current;

                                if (!_IsDegenerated(a, b, c)) yield return ((int)a, (int)b, (int)c);

                                b = c;
                            }
                        }

                        break;
                    }

                case PrimitiveType.TRIANGLE_STRIP:
                    {
                        using (var ptr = sourceIndices.GetEnumerator())
                        {
                            if (!ptr.MoveNext()) break;
                            var a = ptr.Current;
                            if (!ptr.MoveNext()) break;
                            var b = ptr.Current;

                            bool reversed = false;

                            while (true)
                            {
                                if (!ptr.MoveNext()) break;
                                var c = ptr.Current;

                                if (!_IsDegenerated(a, b, c))
                                {
                                    if (reversed) yield return ((int)b, (int)a, (int)c);
                                    else yield return ((int)a, (int)b, (int)c);
                                }

                                a = b;
                                b = c;
                                reversed = !reversed;
                            }
                        }

                        break;
                    }

                default: throw new NotImplementedException();
            }
        }

        private static bool _IsDegenerated(uint a, uint b)
        {
            return a == b;
        }

        private static bool _IsDegenerated(uint a, uint b, uint c)
        {
            if (a == b) return true;
            if (a == c) return true;
            if (b == c) return true;
            return false;
        }

        #endregion

        #region serialization

        public static Byte[] ToUnderlayingArray(this ArraySegment<Byte> segment)
        {
            if (segment.Offset == 0 && segment.Count == segment.Array.Length) return segment.Array;

            return segment.ToArray();
        }

        public static ArraySegment<Byte> ToArraySegment(this System.IO.MemoryStream m)
        {
            if (m.TryGetBuffer(out ArraySegment<Byte> data)) return data;
            return new ArraySegment<byte>(m.ToArray());
        }

        public static Byte[] GetPaddedContent(this Byte[] content)
        {
            if (content == null) return null;

            if (content.Length.IsMultipleOf(4)) return content;

            var rest = content.Length % 4;
            rest = rest == 0 ? 0 : 4 - rest;

            var paddedContent = new Byte[content.Length + rest];
            content.CopyTo(paddedContent, 0);
            return paddedContent;
        }

        public static Byte[] TryParseBase64Unchecked(this string uri, params string[] prefixes)
        {
            if (uri == null) return null;

            if (!uri.StartsWith("data:", StringComparison.OrdinalIgnoreCase)) return null;

            foreach (var prefix in prefixes)
            {
                var data = _TryParseBase64Unchecked(uri, prefix);
                if (data != null) return data;
            }

            return null;
        }

        private static Byte[] _TryParseBase64Unchecked(string uri, string prefix)
        {
            if (!uri.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)) return null;

            var content = uri.Substring(prefix.Length);

            if (content.StartsWith(";base64,", StringComparison.OrdinalIgnoreCase))
            {
                content = content.Substring(";base64,".Length);
                return Convert.FromBase64String(content);
            }

            if (content.StartsWith(",", StringComparison.OrdinalIgnoreCase))
            {
                content = content.Substring(",".Length);

                if (content.Length == 1) return new Byte[] { Byte.Parse(content, System.Globalization.NumberStyles.HexNumber, System.Globalization.CultureInfo.InvariantCulture) };

                throw new NotImplementedException();
            }

            throw new NotImplementedException();
        }

        #endregion
    }
}
