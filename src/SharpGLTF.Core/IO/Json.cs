using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SharpGLTF.IO
{
    static class JsonUtils
    {
        public static bool IsSerializable(Object value)
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
                return list.All(item => IsSerializable(item));
            }

            if (value is JsonDictionary dict)
            {
                return dict.Values.All(item => IsSerializable(item));
            }

            return false;
        }
    }

    public class JsonList : List<Object> { }

    public class JsonDictionary : Dictionary<String, Object> { }
}
