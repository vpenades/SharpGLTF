using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Text;

namespace SharpGLTF.Memory
{
    /// <summary>
    /// Function for converting data into binary buffers
    /// Specs see https://github.com/CesiumGS/3d-tiles/tree/main/specification/Metadata#binary-table-format
    /// </summary>
    public static class BinaryTable
    {
        public static List<byte> ConvertJaggedListToBytes<T>(List<List<T>> values)
        {
            var type = typeof(T);
            if(type == typeof(bool))
            {
                var booleanBytes = ConvertJaggedListOfBooleansToBytes(values.Cast<List<bool>>().ToList());
                return booleanBytes;
            }
            var bytes = new List<byte>();
            foreach (var value in values)
            {
                var b = GetBytes(value);
                bytes.AddRange(b);
            }

            return bytes;
        }

        /// <summary>
        /// Converts a list of primitive types into a byte array
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="values"></param>
        /// <returns>byte array</returns>
        public static byte[] GetBytes<T>(IReadOnlyList<T> values)
        {
            Guard.IsTrue(values.Count > 0, nameof(values), "values must have at least one element");

            if (typeof(T) == typeof(string))
            {
                var res = string.Join("", values);
                return Encoding.UTF8.GetBytes(res);
            }
            else if (typeof(T) == typeof(Vector2))
            {
                return Vector2ToBytes(values);
            }
            else if (typeof(T) == typeof(Vector3))
            {
                return Vector3ToBytes(values);
            }
            else if (typeof(T) == typeof(Vector4))
            {
                return Vector4ToBytes(values);
            }
            else if (typeof(T) == typeof(Matrix4x4))
            {
                return Matrix4x4ToBytes(values);
            }
            else if (typeof(T).IsPrimitive)
            {
                if (typeof(T) == typeof(bool))
                {
                    var bits = new BitArray(values.Cast<bool>().ToArray());
                    byte[] ret = new byte[(bits.Length - 1) / 8 + 1];
                    bits.CopyTo(ret, 0);
                    return ret;
                }

                var size = GetSize<T>();
                var result = new byte[values.Count * size];
                Buffer.BlockCopy(values.ToArray(), 0, result, 0, result.Length);
                return result;
            }
            else
            {
                // other types (like datetime, mat2, mat3) are not implemented
                // see https://github.com/CesiumGS/3d-tiles/tree/main/specification/Metadata#binary-table-format
                throw new NotImplementedException();
            }
        }

        private static List<byte> ConvertJaggedListOfBooleansToBytes(List<List<bool>> values)
        {
            var bits = new BitArray(values.SelectMany(x => x).ToArray());
            var boolBytes = new byte[(bits.Length - 1) / 8 + 1];
            bits.CopyTo(boolBytes, 0);
            return boolBytes.ToList();
        }

        private static byte[] Matrix4x4ToBytes<T>(IReadOnlyList<T> values)
        {
            var result = new byte[values.Count * 64];

            for (int i = 0; i < values.Count; i++)
            {
                var mat = (Matrix4x4)(object)values[i];
                Buffer.BlockCopy(BitConverter.GetBytes(mat.M11), 0, result, i * 64, 4);
                Buffer.BlockCopy(BitConverter.GetBytes(mat.M12), 0, result, i * 64 + 4, 4);
                Buffer.BlockCopy(BitConverter.GetBytes(mat.M13), 0, result, i * 64 + 8, 4);
                Buffer.BlockCopy(BitConverter.GetBytes(mat.M14), 0, result, i * 64 + 12, 4);

                Buffer.BlockCopy(BitConverter.GetBytes(mat.M21), 0, result, i * 64 + 16, 4);
                Buffer.BlockCopy(BitConverter.GetBytes(mat.M22), 0, result, i * 64 + 20, 4);
                Buffer.BlockCopy(BitConverter.GetBytes(mat.M23), 0, result, i * 64 + 24, 4);
                Buffer.BlockCopy(BitConverter.GetBytes(mat.M24), 0, result, i * 64 + 28, 4);

                Buffer.BlockCopy(BitConverter.GetBytes(mat.M31), 0, result, i * 64 + 32, 4);
                Buffer.BlockCopy(BitConverter.GetBytes(mat.M32), 0, result, i * 64 + 36, 4);
                Buffer.BlockCopy(BitConverter.GetBytes(mat.M33), 0, result, i * 64 + 40, 4);
                Buffer.BlockCopy(BitConverter.GetBytes(mat.M34), 0, result, i * 64 + 44, 4);

                Buffer.BlockCopy(BitConverter.GetBytes(mat.M41), 0, result, i * 64 + 48, 4);
                Buffer.BlockCopy(BitConverter.GetBytes(mat.M42), 0, result, i * 64 + 52, 4);
                Buffer.BlockCopy(BitConverter.GetBytes(mat.M43), 0, result, i * 64 + 56, 4);
                Buffer.BlockCopy(BitConverter.GetBytes(mat.M44), 0, result, i * 64 + 60, 4);
            }

            return result;
        }

        private static byte[] Vector2ToBytes<T>(IReadOnlyList<T> values)
        {
            var result = new byte[values.Count * 8];
            for (int i = 0; i < values.Count; i++)
            {
                var vec = (Vector2)(object)values[i];
                Buffer.BlockCopy(BitConverter.GetBytes(vec.X), 0, result, i * 8, 4);
                Buffer.BlockCopy(BitConverter.GetBytes(vec.Y), 0, result, i * 8 + 4, 4);
            }
            return result;
        }

        private static byte[] Vector3ToBytes<T>(IReadOnlyList<T> values)
        {
            var result = new byte[values.Count * 12];
            for (int i = 0; i < values.Count; i++)
            {
                var vec = (Vector3)(object)values[i];
                Buffer.BlockCopy(BitConverter.GetBytes(vec.X), 0, result, i * 12, 4);
                Buffer.BlockCopy(BitConverter.GetBytes(vec.Y), 0, result, i * 12 + 4, 4);
                Buffer.BlockCopy(BitConverter.GetBytes(vec.Z), 0, result, i * 12 + 8, 4);
            }
            return result;
        }

        private static byte[] Vector4ToBytes<T>(IReadOnlyList<T> values)
        {
            var result = new byte[values.Count * 16];
            for (int i = 0; i < values.Count; i++)
            {
                var vec = (Vector4)(object)values[i];
                Buffer.BlockCopy(BitConverter.GetBytes(vec.X), 0, result, i * 16, 4);
                Buffer.BlockCopy(BitConverter.GetBytes(vec.Y), 0, result, i * 16 + 4, 4);
                Buffer.BlockCopy(BitConverter.GetBytes(vec.Z), 0, result, i * 16 + 8, 4);
                Buffer.BlockCopy(BitConverter.GetBytes(vec.W), 0, result, i * 16 + 12, 4);
            }
            return result;
        }

        public static List<int> GetStringOffsets(IReadOnlyList<string> values)
        {
            var offsets = new List<int>() { 0 };
            foreach (var value in values)
            {
                var length = Encoding.UTF8.GetBytes(value).Length;
                offsets.Add(offsets.Last() + length);
            }

            return offsets;
        }


        public static List<int> GetStringOffsets(List<List<string>> values)
        {
            var offsets = new List<int>() { };
            foreach (var arr in values)
            {
                var arrOffsets = GetStringOffsets(arr);
                var last = offsets.LastOrDefault();
                foreach (var offset in arrOffsets)
                {
                    if (!offsets.Contains(last + offset))
                    {
                        offsets.Add(last + offset);
                    }
                }
            }

            return offsets;
        }


        public static List<int> GetArrayOffsets<T>(List<List<T>> values)
        {
            var offsets = new List<int>() { 0 };
            foreach (var value in values)
            {
                offsets.Add(offsets.Last() + value.Count);

            }
            return offsets;
        }

        public static int GetSize<T>()
        {
#if NETSTANDARD2_0
                var isValueType = typeof(T).IsValueType;
#else
            var isValueType = !System.Runtime.CompilerServices.RuntimeHelpers.IsReferenceOrContainsReferences<T>();
#endif

            Guard.IsTrue(isValueType, nameof(T), "T must be a value type");

            var type = typeof(T);
            var size = Marshal.SizeOf<T>();
            return size;
        }
    }
}
