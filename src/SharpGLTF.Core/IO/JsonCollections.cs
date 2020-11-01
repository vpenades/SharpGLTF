using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;

using JSONELEMENT = System.Text.Json.JsonElement;

namespace SharpGLTF.IO
{
    static class JsonValue
    {
        public static bool IsJsonSerializable(Object value, out Object invalidValue)
        {
            invalidValue = null;

            if (value == null) return false;

            if (value is IConvertible cvt)
            {
                var t = cvt.GetTypeCode();
                if (t == TypeCode.Empty) { invalidValue = value; return false; }
                if (t == TypeCode.DBNull) { invalidValue = value; return false; }
                if (t == TypeCode.Object) { invalidValue = value; return false; }
                if (t == TypeCode.DateTime) { invalidValue = value; return false; }
                return true;
            }

            if (value is JsonList list)
            {
                foreach (var item in list)
                {
                    if (!IsJsonSerializable(item, out invalidValue)) return false;
                }
                return true;
            }

            if (value is JsonDictionary dict)
            {
                foreach (var item in dict.Values)
                {
                    if (!IsJsonSerializable(item, out invalidValue)) return false;
                }
                return true;
            }

            invalidValue = value;
            return false;
        }

        public static bool IsJsonSerializable(Object value) { return IsJsonSerializable(value, out _); }

        public static string SerializeToJson(Object value, System.Text.Json.JsonSerializerOptions options)
        {
            if (!IsJsonSerializable(value, out Object invalidValue)) throw new ArgumentException($"Found {invalidValue}, Expected Values, JsonList and JsonDictionary types allowed.", nameof(value));

            if (options == null)
            {
                options = new System.Text.Json.JsonSerializerOptions
                {
                    PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase,
                    IgnoreNullValues = true,
                    WriteIndented = true
                };
            }

            return System.Text.Json.JsonSerializer.Serialize(value, value.GetType(), options);
        }

        /// <summary>
        /// Clones a json hierarchical object.
        /// </summary>
        /// <param name="value">An Iconvertible object, a List, or a Dictionary</param>
        /// <returns>A cloned object</returns>
        public static Object DeepClone(Object value)
        {
            if (value == null) throw new ArgumentNullException(nameof(value));

            if (value is IConvertible cvt)
            {
                var t = cvt.GetTypeCode();
                if (t == TypeCode.Empty) throw new ArgumentException($"Unexpected type {t}", nameof(value));
                if (t == TypeCode.DBNull) throw new ArgumentException($"Unexpected type {t}", nameof(value));
                if (t == TypeCode.Object) throw new ArgumentException($"Unexpected type {t}", nameof(value));
                if (t == TypeCode.DateTime) throw new ArgumentException($"Unexpected type {t}", nameof(value));
                return value;
            }

            if (value is IDictionary<string, Object> wadict) return new JsonDictionary(wadict);
            if (value is IReadOnlyDictionary<string, Object> rodict) return new JsonDictionary(rodict);

            if (value is IList<Object> walist) return new JsonList(walist);
            if (value is IReadOnlyList<Object> rolist) return new JsonList(rolist);

            throw new ArgumentException($"Unexpected type {value.GetType().Name}", nameof(value));
        }

        public static Object DeepParse(string json, JsonDocumentOptions options = default)
        {
            using (var doc = System.Text.Json.JsonDocument.Parse(json, options))
            {
                return DeepClone(doc);
            }
        }

        public static Object DeepClone(System.Text.Json.JsonDocument doc)
        {
            return DeepClone(doc.RootElement);
        }

        public static Object DeepClone(JSONELEMENT element)
        {
            if (element.ValueKind == JsonValueKind.Null) return null;
            if (element.ValueKind == JsonValueKind.False) return false;
            if (element.ValueKind == JsonValueKind.True) return true;
            if (element.ValueKind == JsonValueKind.String) return element.GetString();
            if (element.ValueKind == JsonValueKind.Number) return element.GetRawText(); // use IConvertible interface when needed.
            if (element.ValueKind == JsonValueKind.Array) return new JsonList(element);
            if (element.ValueKind == JsonValueKind.Object) return new JsonDictionary(element);

            throw new NotImplementedException();
        }
    }

    public class JsonList : List<Object>
    {
        public JsonList() { }

        internal JsonList(IEnumerable<Object> list)
            : base(list) { }

        internal JsonList(JSONELEMENT element)
        {
            if (element.ValueKind != JsonValueKind.Array) throw new ArgumentException("Must be JsonValueKind.Array", nameof(element));

            foreach (var item in element.EnumerateArray())
            {
                var xitem = JsonValue.DeepClone(item);
                this.Add(xitem);
            }
        }
    }

    public class JsonDictionary : Dictionary<String, Object>
    {
        public JsonDictionary() { }

        internal JsonDictionary(IDictionary<String, Object> dict)
            : base(dict) { }

        internal JsonDictionary(IReadOnlyDictionary<String, Object> dict)
        {
            foreach (var kvp in dict)
            {
                this[kvp.Key] = kvp.Value;
            }
        }

        internal JsonDictionary(JSONELEMENT element)
        {
            if (element.ValueKind != JsonValueKind.Object) throw new ArgumentException("Must be JsonValueKind.Object", nameof(element));

            foreach (var item in element.EnumerateObject())
            {
                this[item.Name] = JsonValue.DeepClone(item.Value);
            }
        }
    }
}
