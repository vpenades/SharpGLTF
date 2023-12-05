using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SharpGLTF
{
    public static class BinaryTable
    {
        public static byte[] GetOffsetBuffer(IReadOnlyList<string> strings)
        {
            var offsetBuffer = GetOffsets(strings);
            var offsetBytes = GetBytes(offsetBuffer);
            return offsetBytes;
        }

        public static byte[] GetBytes<T>(IReadOnlyList<T> values)
        {
            var type = typeof(T);
            int size = 0;
            if (type == typeof(float))
            {
                size = sizeof(float);
            }
            else if (type == typeof(int))
            {
                size = sizeof(int);
            }
            else if (type == typeof(uint))
            {
                size = sizeof(uint);
            }

            var result = new byte[values.Count * size];
            System.Buffer.BlockCopy(values.ToArray(), 0, result, 0, result.Length);
            return result;
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

        public static byte[] GetStringsAsBytes(IReadOnlyList<string> values)
        {
            var res = string.Join("", values);
            return Encoding.UTF8.GetBytes(res);
        }
    }
}
