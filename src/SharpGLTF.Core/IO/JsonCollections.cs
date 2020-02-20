using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;

namespace SharpGLTF.IO
{
    static class JsonUtils
    {
        public static Memory<Byte> ReadBytesToEnd(this System.IO.Stream s)
        {
            using (var m = new System.IO.MemoryStream())
            {
                s.CopyTo(m);

                if (m.TryGetBuffer(out ArraySegment<Byte> segment)) return segment;

                return m.ToArray();
            }
        }

        public static bool IsJsonSerializable(Object value)
        {
            if (value == null) return false;

            if (value is IConvertible cvt)
            {
                var t = cvt.GetTypeCode();
                if (t == TypeCode.Empty) return false;
                if (t == TypeCode.DBNull) return false;
                if (t == TypeCode.Object) return false;
                if (t == TypeCode.DateTime) return false;
                return true;
            }

            if (value is JsonList list)
            {
                return list.All(item => IsJsonSerializable(item));
            }

            if (value is JsonDictionary dict)
            {
                return dict.Values.All(item => IsJsonSerializable(item));
            }

            return false;
        }
    }

    public class JsonList : List<Object> { }

    public class JsonDictionary : Dictionary<String, Object> { }
}
