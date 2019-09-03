using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;

using Newtonsoft.Json;

namespace SharpGLTF.IO
{
    public abstract class JsonSerializable
    {
        #region validation

        public IEnumerable<Exception> Validate()
        {
            var result = new Validation.ValidationContext();

            Validate(result);

            return result.Errors;
        }

        internal virtual void Validate(Validation.ValidationContext result)
        {
        }

        #endregion

        #region serialization

        internal void Serialize(JsonWriter writer)
        {
            Guard.NotNull(writer, nameof(writer));

            writer.WriteStartObject();
            SerializeProperties(writer);
            writer.WriteEndObject();
        }

        protected abstract void SerializeProperties(JsonWriter writer);

        protected static void SerializeProperty(JsonWriter writer, string name, Object value)
        {
            if (value == null) return;

            Guard.NotNull(writer, nameof(writer));

            writer.WritePropertyName(name);
            _Serialize(writer, value);
        }

        protected static void SerializeProperty(JsonWriter writer, string name, Boolean? value, Boolean? defval = null)
        {
            if (!value.HasValue) return;
            if (defval.HasValue && defval.Value.Equals(value.Value)) return;

            Guard.NotNull(writer, nameof(writer));

            writer.WritePropertyName(name);
            writer.WriteValue(value.Value);
        }

        protected static void SerializeProperty(JsonWriter writer, string name, Int32? value, Int32? defval = null)
        {
            if (!value.HasValue) return;
            if (defval.HasValue && defval.Value.Equals(value.Value)) return;

            Guard.NotNull(writer, nameof(writer));

            writer.WritePropertyName(name);
            writer.WriteValue(value.Value);
        }

        protected static void SerializeProperty(JsonWriter writer, string name, Single? value, Single? defval = null)
        {
            if (!value.HasValue) return;
            if (defval.HasValue && defval.Value.Equals(value.Value)) return;

            Guard.NotNull(writer, nameof(writer));

            writer.WritePropertyName(name);
            writer.WriteValue(value.Value);
        }

        protected static void SerializeProperty(JsonWriter writer, string name, Double? value, Double? defval = null)
        {
            if (!value.HasValue) return;
            if (defval.HasValue && defval.Value.Equals(value.Value)) return;

            Guard.NotNull(writer, nameof(writer));

            writer.WritePropertyName(name);
            writer.WriteValue(value.Value);
        }

        protected static void SerializeProperty(JsonWriter writer, string name, Vector2? value, Vector2? defval = null)
        {
            if (!value.HasValue) return;
            if (defval.HasValue && defval.Value.Equals(value.Value)) return;

            Guard.NotNull(writer, nameof(writer));

            writer.WritePropertyName(name);
            _Serialize(writer, value.Value);
        }

        protected static void SerializeProperty(JsonWriter writer, string name, Vector3? value, Vector3? defval = null)
        {
            if (!value.HasValue) return;
            if (defval.HasValue && defval.Value.Equals(value.Value)) return;

            Guard.NotNull(writer, nameof(writer));

            writer.WritePropertyName(name);
            _Serialize(writer, value.Value);
        }

        protected static void SerializeProperty(JsonWriter writer, string name, Vector4? value, Vector4? defval = null)
        {
            if (!value.HasValue) return;
            if (defval.HasValue && defval.Value.Equals(value.Value)) return;

            Guard.NotNull(writer, nameof(writer));

            writer.WritePropertyName(name);
            _Serialize(writer, value.Value);
        }

        protected static void SerializeProperty(JsonWriter writer, string name, Quaternion? value, Quaternion? defval = null)
        {
            if (!value.HasValue) return;
            if (defval.HasValue && defval.Value.Equals(value.Value)) return;

            Guard.NotNull(writer, nameof(writer));

            writer.WritePropertyName(name);
            _Serialize(writer, value.Value);
        }

        protected static void SerializeProperty(JsonWriter writer, string name, Matrix4x4? value, Matrix4x4? defval = null)
        {
            if (!value.HasValue) return;
            if (defval.HasValue && defval.Value.Equals(value.Value)) return;

            Guard.NotNull(writer, nameof(writer));

            writer.WritePropertyName(name);
            _Serialize(writer, value.Value);
        }

        protected static void SerializePropertyEnumValue<T>(JsonWriter writer, string name, T? value, T? defval = null)
            where T : struct
        {
            Guard.IsTrue(typeof(T).IsEnum, nameof(T));

            if (!value.HasValue) return;
            if (defval.HasValue && defval.Value.Equals(value)) return;

            Guard.NotNull(writer, nameof(writer));

            SerializeProperty(writer, name, (int)(Object)value);
        }

        protected static void SerializePropertyEnumSymbol<T>(JsonWriter writer, string name, T? value, T? defval = null)
            where T : struct
        {
            Guard.IsTrue(typeof(T).IsEnum, nameof(T));

            if (!value.HasValue) return;
            if (defval.HasValue && defval.Value.Equals(value)) return;

            Guard.NotNull(writer, nameof(writer));

            SerializeProperty(writer, name, Enum.GetName(typeof(T), value));
        }

        protected static void SerializePropertyObject<T>(JsonWriter writer, string name, T value)
            where T : JsonSerializable
        {
            if (value == null) return;

            Guard.NotNull(writer, nameof(writer));

            writer.WritePropertyName(name);
            _Serialize(writer, value);
        }

        protected static void SerializeProperty<T>(JsonWriter writer, string name, IReadOnlyList<T> collection, int? minItems = 1)
        {
            if (collection == null) return;
            if (minItems.HasValue && collection.Count < minItems.Value) return;

            Guard.NotNull(writer, nameof(writer));

            writer.WritePropertyName(name);

            writer.WriteStartArray();
            foreach (var item in collection)
            {
                _Serialize(writer, item);
            }

            writer.WriteEndArray();
        }

        protected static void SerializeProperty<T>(JsonWriter writer, string name, IReadOnlyDictionary<String, T> collection)
        {
            if (collection == null) return;
            if (collection.Count < 1) return;

            Guard.NotNull(writer, nameof(writer));

            writer.WritePropertyName(name);

            writer.WriteStartObject();
            foreach (var item in collection)
            {
                writer.WritePropertyName(item.Key);
                _Serialize(writer, item.Value);
            }

            writer.WriteEndObject();
        }

        private static void _Serialize(JsonWriter writer, Object value)
        {
            Guard.NotNull(writer, nameof(writer));
            Guard.NotNull(value, nameof(value));

            System.Diagnostics.Debug.Assert(!value.GetType().IsEnum, "gltf schema does not define a typed way of serializing enums");

            if (value is String vstr) { writer.WriteValue(vstr); return; }
            if (value is Boolean vbol) { writer.WriteValue(vbol); return; }

            if (value is Byte vu8) { writer.WriteValue(vu8); return; }
            if (value is UInt16 vu16) { writer.WriteValue(vu16); return; }
            if (value is UInt32 vu32) { writer.WriteValue(vu32); return; }
            if (value is UInt64 vu64) { writer.WriteValue(vu64); return; }

            if (value is SByte vs8) { writer.WriteValue(vs8); return; }
            if (value is Int16 vs16) { writer.WriteValue(vs16); return; }
            if (value is Int32 vs32) { writer.WriteValue(vs32); return; }
            if (value is Int64 vs64) { writer.WriteValue(vs64); return; }

            if (value is Single vf32) { writer.WriteValue(vf32); return; }
            if (value is Double vf64) { writer.WriteValue(vf64); return; }
            if (value is Decimal vfxx) { writer.WriteValue(vfxx); return; }

            if (value is Vector2 vvv2) { writer.WriteVector2(vvv2); return; }
            if (value is Vector3 vvv3) { writer.WriteVector3(vvv3); return; }
            if (value is Vector4 vvv4) { writer.WriteVector4(vvv4); return; }
            if (value is Quaternion qqq4) { writer.WriteQuaternion(qqq4); return; }
            if (value is Matrix4x4 mm44) { writer.WriteMatrix4x4(mm44); return; }

            if (value is JsonSerializable vgltf) { vgltf.Serialize(writer); return; }

            if (value is System.Collections.IDictionary dict)
            {
                if (dict.Count == 0) return;

                writer.WriteStartObject();
                foreach (var key in dict.Keys)
                {
                    var val = dict[key];
                    if (val == null) continue;

                    // if the value is a collection, we need to check if the collection is empty
                    // to prevent writing the key, without writing the value.
                    if (!(val is String || val is JsonSerializable))
                    {
                        if (val is System.Collections.IList xlist && xlist.Count == 0) continue;
                        if (val is System.Collections.IDictionary xdict && xdict.Count == 0) continue;
                    }

                    writer.WritePropertyName(key.ToString());
                    _Serialize(writer, val);
                }

                writer.WriteEndObject();
                return;
            }

            if (value is System.Collections.IList list)
            {
                if (list.Count == 0) return;

                writer.WriteStartArray();
                foreach (var item in list)
                {
                    _Serialize(writer, item);
                }

                writer.WriteEndArray();
                return;
            }

            throw new NotImplementedException($"Serialization of {value.GetType().Name} types is not supported.");
        }

        #endregion

        #region deserialization

        internal void Deserialize(JsonReader reader)
        {
            Guard.NotNull(reader, nameof(reader));

            if (reader.TokenType == JsonToken.PropertyName) reader.Read();

            if (reader.TokenType == JsonToken.StartObject)
            {
                while (reader.Read() && reader.TokenType != JsonToken.EndObject)
                {
                    if (reader.TokenType == JsonToken.PropertyName)
                    {
                        var key = reader.Value as String;

                        DeserializeProperty(key, reader);
                    }
                    else
                    {
                        throw new NotImplementedException();
                    }
                }

                return;
            }

            throw new NotImplementedException();
        }

        protected static Object DeserializeUnknownObject(JsonReader reader)
        {
            Guard.NotNull(reader, nameof(reader));

            if (reader.TokenType == JsonToken.PropertyName) reader.Read();

            if (reader.TokenType == JsonToken.StartArray)
            {
                var list = new JsonList();

                while (reader.Read() && reader.TokenType != JsonToken.EndArray)
                {
                    list.Add(DeserializeUnknownObject(reader));
                }

                return list;
            }

            if (reader.TokenType == JsonToken.StartObject)
            {
                var dict = new JsonDictionary();

                while (reader.Read() && reader.TokenType != JsonToken.EndObject)
                {
                    if (reader.TokenType == JsonToken.PropertyName)
                    {
                        var key = reader.Value as String;

                        dict[key] = DeserializeUnknownObject(reader);
                    }
                    else
                    {
                        throw new JsonReaderException();
                    }
                }

                return dict;
            }

            System.Diagnostics.Debug.Assert(reader.TokenType != JsonToken.None);
            System.Diagnostics.Debug.Assert(reader.TokenType != JsonToken.EndArray);
            System.Diagnostics.Debug.Assert(reader.TokenType != JsonToken.EndObject);
            System.Diagnostics.Debug.Assert(reader.TokenType != JsonToken.EndConstructor);

            return reader.Value;
        }

        protected abstract void DeserializeProperty(string jsonPropertyName, JsonReader reader);

        protected static T DeserializePropertyValue<T>(JsonReader reader)
        {
            Guard.NotNull(reader, nameof(reader));

            _TryCastValue(reader, typeof(T), out Object v);

            System.Diagnostics.Debug.Assert(reader.TokenType != JsonToken.StartArray);
            System.Diagnostics.Debug.Assert(reader.TokenType != JsonToken.StartObject);
            System.Diagnostics.Debug.Assert(reader.TokenType != JsonToken.PropertyName);
            System.Diagnostics.Debug.Assert(reader.TokenType != JsonToken.StartConstructor);

            return (T)v;
        }

        protected static void DeserializePropertyList<T>(JsonReader reader, IList<T> list)
        {
            Guard.NotNull(reader, nameof(reader));
            Guard.NotNull(list, nameof(list));

            if (reader.TokenType == JsonToken.PropertyName) reader.Read();

            if (reader.TokenType != JsonToken.StartArray) throw new JsonReaderException();
            if (reader.TokenType == JsonToken.StartObject) throw new JsonReaderException();

            var path2 = reader.Path;

            while (reader.Read() && reader.TokenType != JsonToken.EndArray)
            {
                if (_TryCastValue(reader, typeof(T), out Object item))
                {
                    list.Add((T)item);
                }

                System.Diagnostics.Debug.Assert(reader.TokenType != JsonToken.StartArray);
                System.Diagnostics.Debug.Assert(reader.TokenType != JsonToken.StartObject);
                System.Diagnostics.Debug.Assert(reader.TokenType != JsonToken.PropertyName);
                System.Diagnostics.Debug.Assert(reader.TokenType != JsonToken.StartConstructor);
            }

            System.Diagnostics.Debug.Assert(reader.TokenType == JsonToken.EndArray);
        }

        protected static void DeserializePropertyDictionary<T>(JsonReader reader, IDictionary<string, T> dict)
        {
            Guard.NotNull(reader, nameof(reader));
            Guard.NotNull(dict, nameof(dict));

            if (reader.TokenType == JsonToken.PropertyName) reader.Read();

            if (reader.TokenType == JsonToken.StartArray) throw new JsonReaderException();
            if (reader.TokenType != JsonToken.StartObject) throw new JsonReaderException();

            while (reader.Read() && reader.TokenType != JsonToken.EndObject)
            {
                if (reader.TokenType == JsonToken.PropertyName)
                {
                    var key = reader.Value as String;

                    if (_TryCastValue(reader, typeof(T), out Object val))
                    {
                        dict[key] = (T)val;
                    }

                    System.Diagnostics.Debug.Assert(reader.TokenType != JsonToken.StartArray);
                    System.Diagnostics.Debug.Assert(reader.TokenType != JsonToken.StartObject);
                    System.Diagnostics.Debug.Assert(reader.TokenType != JsonToken.PropertyName);
                    System.Diagnostics.Debug.Assert(reader.TokenType != JsonToken.StartConstructor);
                }
            }
        }

        private static bool _TryCastValue(JsonReader reader, Type vtype, out Object value)
        {
            Guard.NotNull(reader, nameof(reader));

            value = null;

            if (reader.TokenType == JsonToken.EndArray) return false;
            if (reader.TokenType == JsonToken.EndObject) return false;
            if (reader.TokenType == JsonToken.EndConstructor) return false;

            if (reader.TokenType == JsonToken.PropertyName) reader.Read();

            // untangle nullable
            var ntype = Nullable.GetUnderlyingType(vtype);
            if (ntype != null) vtype = ntype;

            if (vtype == typeof(String) ||
                vtype == typeof(Boolean) ||
                vtype == typeof(Int16) ||
                vtype == typeof(Int32) ||
                vtype == typeof(Int64) ||
                vtype == typeof(UInt16) ||
                vtype == typeof(UInt32) ||
                vtype == typeof(UInt64) ||
                vtype == typeof(Single) ||
                vtype == typeof(Double))
            {
                value = Convert.ChangeType(reader.Value, vtype, System.Globalization.CultureInfo.InvariantCulture);
                return true;
            }

            if (vtype.IsEnum)
            {
                if (reader.Value is String xstrVal) { value = Enum.Parse(vtype, xstrVal, true); return true; }
                if (reader.Value is Int32 int32Val) { value = Enum.ToObject(vtype, int32Val); return true; }
                if (reader.Value is Int64 int64Val) { value = Enum.ToObject(vtype, int64Val); return true; }

                throw new NotImplementedException();
            }

            if (vtype == typeof(Vector2))
            {
                var l = new List<float>();
                DeserializePropertyList<float>(reader, l);
                value = new Vector2(l[0], l[1]);
                return true;
            }

            if (vtype == typeof(Vector3))
            {
                var l = new List<float>();
                DeserializePropertyList<float>(reader, l);
                value = new Vector3(l[0], l[1], l[2]);
                return true;
            }

            if (vtype == typeof(Vector4))
            {
                var l = new List<float>();
                DeserializePropertyList<float>(reader, l);
                value = new Vector4(l[0], l[1], l[2], l[3]);
                return true;
            }

            if (vtype == typeof(Quaternion))
            {
                var l = new List<float>();
                DeserializePropertyList<float>(reader, l);
                value = new System.Numerics.Quaternion(l[0], l[1], l[2], l[3]);
                return true;
            }

            if (vtype == typeof(Matrix4x4))
            {
                var l = new List<float>();
                DeserializePropertyList<float>(reader, l);
                value = new Matrix4x4
                    (
                    l[0], l[1], l[2], l[3],
                    l[4], l[5], l[6], l[7],
                    l[8], l[9], l[10], l[11],
                    l[12], l[13], l[14], l[15]
                    );
                return true;
            }

            if (typeof(JsonSerializable).IsAssignableFrom(vtype))
            {
                var item = Activator.CreateInstance(vtype, true) as JsonSerializable;

                System.Diagnostics.Debug.Assert(reader.TokenType == JsonToken.StartObject);
                item.Deserialize(reader);
                System.Diagnostics.Debug.Assert(reader.TokenType == JsonToken.EndObject);

                value = item;

                return true;
            }

            if (vtype.IsGenericType)
            {
                if (vtype.GetGenericTypeDefinition() == typeof(Dictionary<,>))
                {
                    var valType = vtype.GetGenericArguments()[1];

                    if (valType == typeof(Int32))
                    {
                        var dict = new Dictionary<string, Int32>();
                        DeserializePropertyDictionary(reader, dict);
                        value = dict;
                        return true;
                    }
                }

                throw new NotImplementedException($"Can't deserialize {vtype}");
            }

            throw new NotImplementedException($"Can't deserialize {vtype}");
        }

        #endregion
    }
}
