using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace SharpGLTF
{
    /// <summary>
    /// Function for converting data into binary buffers
    /// Specs see https://github.com/CesiumGS/3d-tiles/tree/main/specification/Metadata#binary-table-format
    /// </summary>
    public static class BinaryTable
    {
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
                return GetStringsAsBytes(values.Cast<string>().ToArray());
            }
            else if (typeof(T).IsPrimitive)
            {
                if(typeof(T) == typeof(bool))
                {
                    // when implementing bool, create a bitstream
                    // see https://github.com/CesiumGS/3d-tiles/tree/main/specification/Metadata#booleans
                    throw new NotImplementedException();
                }
                var size = GetSize<T>();
                var result = new byte[values.Count * size];
                Buffer.BlockCopy(values.ToArray(), 0, result, 0, result.Length);
                return result;
            }
            else
            {
                // other types (like enum, mat2, mat3, mat4, vec2, vec3, vec4, array (fixed length, variable length)) are not implemented
                // see https://github.com/CesiumGS/3d-tiles/tree/main/specification/Metadata#binary-table-format
                throw new NotImplementedException();
            }
        }

        /// <summary>
        /// Creates a list of offsets for a list of strings
        /// see https://github.com/CesiumGS/3d-tiles/tree/main/specification/Metadata#strings
        /// </summary>
        /// <param name="strings"></param>
        /// <returns></returns>
        public static byte[] GetOffsetBuffer(IReadOnlyList<string> strings)
        {
            var offsetBuffer = GetOffsets(strings);
            var offsetBytes = GetBytes(offsetBuffer);
            return offsetBytes;
        }

        public static int GetSize<T>()
        {
            var isValueType = typeof(T).IsValueType;
            Guard.IsTrue(isValueType, nameof(T), "T must be a value type");

            var type = typeof(T);
            int size = Marshal.SizeOf(Activator.CreateInstance(type));
            return size;
        }

        private static byte[] GetStringsAsBytes(IReadOnlyList<string> values)
        {
            var res = string.Join("", values);
            return Encoding.UTF8.GetBytes(res);
        }

        private static List<uint> GetOffsets(IReadOnlyList<string> strings)
        {
            var offsets = new List<uint>() { 0 };
            foreach (string s in strings)
            {
                var length = (uint)Encoding.UTF8.GetByteCount(s);

                offsets.Add(offsets.Last() + length);
            }
            return offsets;
        }
    }
}
