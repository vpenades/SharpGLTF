using System;
using System.Collections.Generic;
using System.Text;

using Newtonsoft.Json;

namespace glTF2Sharp.IO
{
    public abstract class JsonSerializable
    {
        #region validation

        public virtual IEnumerable<Exception> Validate()
        {
            yield break;
        }

        #endregion

        #region serialization

        public void Serialize(JsonWriter writer)
        {
            writer.WriteStartObject();
            SerializeProperties(writer);
            writer.WriteEndObject();
        }

        protected abstract void SerializeProperties(JsonWriter writer);

        protected static void SerializeProperty(JsonWriter writer, string name, string value)
        {
            if (value == null) return;
            writer.WritePropertyName(name);
            writer.WriteValue(value);
        }

        protected static void SerializeProperty(JsonWriter writer, string name, Boolean? value, Boolean? defval = null)
        {
            if (!value.HasValue) return;
            if (defval.HasValue && defval.Value.Equals(value.Value)) return;
            writer.WritePropertyName(name);
            writer.WriteValue(value.Value);
        }

        protected static void SerializeProperty(JsonWriter writer, string name, Int32? value, Int32? defval = null)
        {
            if (!value.HasValue) return;
            if (defval.HasValue && defval.Value.Equals(value.Value)) return;
            writer.WritePropertyName(name);
            writer.WriteValue(value.Value);
        }

        protected static void SerializeProperty(JsonWriter writer, string name, Single? value, Single? defval = null)
        {
            if (!value.HasValue) return;
            if (defval.HasValue && defval.Value.Equals(value.Value)) return;
            writer.WritePropertyName(name);
            writer.WriteValue(value.Value);
        }

        protected static void SerializeProperty(JsonWriter writer, string name, Double? value, Double? defval = null)
        {
            if (!value.HasValue) return;
            if (defval.HasValue && defval.Value.Equals(value.Value)) return;
            writer.WritePropertyName(name);
            writer.WriteValue(value.Value);
        }

        protected static void SerializeProperty(JsonWriter writer, string name, System.Numerics.Vector3? value, System.Numerics.Vector3? defval = null)
        {
            if (!value.HasValue) return;
            if (defval.HasValue && defval.Value.Equals(value.Value)) return;
            writer.WritePropertyName(name);
            _Serialize(writer, value.Value);
        }

        protected static void SerializeProperty(JsonWriter writer, string name, System.Numerics.Vector4? value, System.Numerics.Vector4? defval = null)
        {
            if (!value.HasValue) return;
            if (defval.HasValue && defval.Value.Equals(value.Value)) return;
            writer.WritePropertyName(name);
            _Serialize(writer, value.Value);
        }

        protected static void SerializeProperty(JsonWriter writer, string name, System.Numerics.Quaternion? value, System.Numerics.Quaternion? defval = null)
        {
            if (!value.HasValue) return;
            if (defval.HasValue && defval.Value.Equals(value.Value)) return;
            writer.WritePropertyName(name);
            _Serialize(writer, value.Value);
        }

        protected static void SerializeProperty(JsonWriter writer, string name, System.Numerics.Matrix4x4? value, System.Numerics.Matrix4x4? defval = null)
        {
            if (!value.HasValue) return;
            if (defval.HasValue && defval.Value.Equals(value.Value)) return;
            writer.WritePropertyName(name);
            _Serialize(writer, value.Value);
        }

        protected static void SerializePropertyEnumValue<T>(JsonWriter writer, string name, T? value, T? defval = null) where T : struct
        {
            if (!typeof(T).IsEnum) throw new ArgumentException(nameof(value));

            if (!value.HasValue) return;
            if (defval.HasValue && defval.Value.Equals(value)) return;

            SerializeProperty(writer, name, (int)(Object)value);
        }

        protected static void SerializePropertyEnumSymbol<T>(JsonWriter writer, string name, T? value, T? defval = null) where T : struct
        {
            if (!typeof(T).IsEnum) throw new ArgumentException(nameof(value));

            if (!value.HasValue) return;
            if (defval.HasValue && defval.Value.Equals(value)) return;

            SerializeProperty(writer, name, Enum.GetName(typeof(T), value));
        }

        protected static void SerializePropertyObject<T>(JsonWriter writer, string name, T value) where T : JsonSerializable
        {
            if (value == null) return;
            writer.WritePropertyName(name);
            _Serialize(writer, value);
        }

        protected static void SerializeProperty<T>(JsonWriter writer, string name, IReadOnlyList<T> collection, int? minItems = 1)
        {
            if (collection == null) return;

            if (minItems.HasValue && collection.Count < minItems.Value) return;

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

            // a dictionary is serialized as a regular object where every "property" is a key.

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
            if (value == null) throw new ArgumentNullException(nameof(value));

            System.Diagnostics.Debug.Assert(!value.GetType().IsEnum, "gltf schema does not define a typed way of serializing enums");

            if (value is String vstr) { writer.WriteValue(vstr); return; }
            if (value is Boolean vbol) { writer.WriteValue(vbol); return; }
            if (value is Int32 vi32) { writer.WriteValue(vi32); return; }
            if (value is Int64 vi64) { writer.WriteValue(vi64); return; }
            if (value is Single vfps) { writer.WriteValue(vfps); return; }
            if (value is Double vfpd) { writer.WriteValue(vfpd); return; }
            if (value is Decimal vfpx) { writer.WriteValue(vfpx); return; }

            if (value is System.Numerics.Vector3 vvv3) { writer.WriteVector3(vvv3); return; }
            if (value is System.Numerics.Vector4 vvv4) { writer.WriteVector4(vvv4); return; }
            if (value is System.Numerics.Quaternion vqq4) { writer.WriteQuaternion(vqq4); return; }
            if (value is System.Numerics.Matrix4x4 vm44) { writer.WriteMatrix4x4(vm44); return; }

            if (value is JsonSerializable vgltf) { vgltf.Serialize(writer); return; }

            if (value is IReadOnlyDictionary<String, Int32> vdsi)
            {
                writer.WriteStartObject();
                foreach (var item in vdsi)
                {
                    writer.WritePropertyName(item.Key);
                    _Serialize(writer, item.Value);
                }
                writer.WriteEndObject();

                return;
            }

            throw new NotImplementedException();
        }

        #endregion

        #region deserialization

        public void DeserializeObject(JsonReader reader)
        {
            while (reader.TokenType != JsonToken.StartObject)
            {
                reader.Read();
            }

            while (reader.Read() && reader.TokenType != JsonToken.EndObject)
            {
                if (reader.TokenType == JsonToken.PropertyName)
                {
                    var curProp = reader.Value.ToString();
                    DeserializeProperty(reader, curProp);
                }
                else throw new NotImplementedException(); // skip
            }
        }

        protected abstract void DeserializeProperty(JsonReader reader, string property);

        protected static T DeserializeValue<T>(JsonReader reader)
        {
            reader.Read();

            _TryCastValue(reader, typeof(T), out Object v);

            return (T)v;
        }

        protected static void DeserializeValue<T>(JsonReader reader, Action<T> assign)
        {
            var value = DeserializeValue<T>(reader);
            assign(value);
        }

        protected static void DeserializeList<T>(JsonReader reader, IList<T> list)
        {
            // System.Diagnostics.Debug.Assert(typeof(T) != typeof(MeshPrimitive));

            while (true)
            {
                reader.Read();

                if (reader.TokenType == JsonToken.StartArray) continue;
                if (reader.TokenType == JsonToken.EndArray) break;

                if (_TryCastValue(reader, typeof(T), out Object item))
                {
                    list.Add((T)item);
                }
            }
        }

        protected static void DeserializeDictionary<T>(JsonReader reader, IDictionary<string, T> dict)
        {
            // System.Diagnostics.Debug.Assert(typeof(T) != typeof(MeshPrimitive));

            while (true)
            {
                reader.Read();

                if (reader.TokenType == JsonToken.StartObject) continue;
                if (reader.TokenType == JsonToken.EndObject) break;

                System.Diagnostics.Debug.Assert(reader.TokenType == JsonToken.PropertyName);
                var key = reader.Value as String;

                reader.Read();

                if (_TryCastValue(reader, typeof(T), out Object item))
                {
                    dict[key] = (T)item;
                }
            }
        }

        private static bool _TryCastValue(JsonReader reader, Type vtype, out Object value)
        {
            value = null;

            if (reader.TokenType == JsonToken.EndArray) return false;
            if (reader.TokenType == JsonToken.EndConstructor) return false;
            if (reader.TokenType == JsonToken.EndObject) return false;

            // untangle nullable
            var ntype = Nullable.GetUnderlyingType(vtype);
            if (ntype != null) vtype = ntype;

            if (vtype == typeof(String) ||
                vtype == typeof(Boolean) ||
                vtype == typeof(Int32) ||
                vtype == typeof(Int64) ||
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

            if (vtype == typeof(System.Numerics.Vector3))
            {
                var l = new List<float>();
                DeserializeList<float>(reader, l);
                value = new System.Numerics.Vector3(l[0], l[1], l[2]);
                return true;
            }

            if (vtype == typeof(System.Numerics.Vector4))
            {
                var l = new List<float>();
                DeserializeList<float>(reader, l);
                value = new System.Numerics.Vector4(l[0], l[1], l[2], l[3]);
                return true;
            }

            if (vtype == typeof(System.Numerics.Quaternion))
            {
                var l = new List<float>();
                DeserializeList<float>(reader, l);
                value = new System.Numerics.Quaternion(l[0], l[1], l[2], l[3]);
                return true;
            }

            if (vtype == typeof(System.Numerics.Matrix4x4))
            {
                var l = new List<float>();
                DeserializeList<float>(reader, l);
                value = new System.Numerics.Matrix4x4
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
                System.Diagnostics.Debug.Assert(reader.TokenType == JsonToken.StartObject);

                var item = System.Activator.CreateInstance(vtype, true) as JsonSerializable;

                item.DeserializeObject(reader);

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
                        DeserializeDictionary(reader, dict);
                        value = dict;
                        return true;
                    }

                    // var dict = System.Activator.CreateInstance(vtype);
                }

                throw new NotImplementedException($"Can't deserialize {vtype}");
            }

            throw new NotImplementedException($"Can't deserialize {vtype}");
        }

        #endregion
    }
}
