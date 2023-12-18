using System;
using System.Collections;
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
                var res = string.Join("", values);
                return Encoding.UTF8.GetBytes(res);
            }
            else if (typeof(T).IsPrimitive)
            {
                if(typeof(T) == typeof(bool))
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
                // other types (like enum, mat2, mat3, mat4, vec2, vec3, vec4, array (fixed length, variable length)) are not implemented
                // see https://github.com/CesiumGS/3d-tiles/tree/main/specification/Metadata#binary-table-format
                throw new NotImplementedException();
            }
        }

        public static List<int> GetStringOffsets(List<string> values)
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
            var offsets = new List<int>() {};
            foreach (var arr in values)
            {
                var arrOffsets = GetStringOffsets(arr);
                var last = offsets.LastOrDefault();
                foreach (var offset in arrOffsets)
                {
                    if(!offsets.Contains(last + offset))
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
            var isValueType = typeof(T).IsValueType;
            Guard.IsTrue(isValueType, nameof(T), "T must be a value type");

            var type = typeof(T);
            int size = Marshal.SizeOf(Activator.CreateInstance(type));
            return size;
        }
    }
}
