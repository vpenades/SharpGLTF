using System;
using System.Collections.Generic;
using System.Numerics;
using System.Diagnostics.CodeAnalysis;

using SharpGLTF.Collections;

using JSONEXCEPTION = System.Text.Json.JsonException;
using JSONTOKEN = System.Text.Json.JsonTokenType;

using JSONREADER = System.Text.Json.Utf8JsonReader;
using JSONWRITER = System.Text.Json.Utf8JsonWriter;


namespace SharpGLTF.IO
{
    /// <summary>
    /// Represents the base class of a serializable glTF schema2 object.
    /// Inherited by <see cref="Schema2.ExtraProperties"/>.
    /// </summary>
    public abstract class JsonSerializable
    {
        #region validation

        internal void ValidateReferences(Validation.ValidationContext validate)
        {
            validate = validate.GetContext(this);

            OnValidateReferences(validate);
        }

        internal void ValidateContent(Validation.ValidationContext validate)
        {
            validate = validate.GetContext(this);

            OnValidateContent(validate);
        }

        protected virtual void OnValidateReferences(Validation.ValidationContext validate) { }

        protected virtual void OnValidateContent(Validation.ValidationContext validate) { }

        #endregion

        #region reflection
        internal string _SchemaName => GetSchemaName();
        protected virtual string GetSchemaName() => "JsonSerializable";

        #endregion

        #region serialization

        internal void Serialize(JSONWRITER writer)
        {
            Guard.NotNull(writer, nameof(writer));

            writer.WriteStartObject();
            SerializeProperties(writer);
            writer.WriteEndObject();
        }

        protected abstract void SerializeProperties(JSONWRITER writer);

        protected static void SerializeProperty(JSONWRITER writer, string name, Object value)
        {
            if (value == null) return;

            Guard.NotNull(writer, nameof(writer));

            _SerializeProperty(writer, name, value);
        }

        protected static void SerializeProperty(JSONWRITER writer, string name, Boolean? value, Boolean? defval = null)
        {
            if (!value.HasValue) return;
            if (defval.HasValue && defval.Value.Equals(value.Value)) return;

            Guard.NotNull(writer, nameof(writer));

            writer.WriteBoolean(name, value.Value);
        }

        protected static void SerializeProperty(JSONWRITER writer, string name, Int32? value, Int32? defval = null)
        {
            if (!value.HasValue) return;
            if (defval.HasValue && defval.Value.Equals(value.Value)) return;

            Guard.NotNull(writer, nameof(writer));

            writer.WriteNumber(name, value.Value);
        }

        protected static void SerializeProperty(JSONWRITER writer, string name, Single? value, Single? defval = null)
        {
            if (!value.HasValue) return;
            if (defval.HasValue && defval.Value.Equals(value.Value)) return;

            Guard.NotNull(writer, nameof(writer));

            writer.WriteNumber(name, value.Value);
        }

        protected static void SerializeProperty(JSONWRITER writer, string name, Double? value, Double? defval = null)
        {
            if (!value.HasValue) return;
            if (defval.HasValue && defval.Value.Equals(value.Value)) return;

            Guard.NotNull(writer, nameof(writer));

            writer.WriteNumber(name, value.Value);
        }

        protected static void SerializeProperty(JSONWRITER writer, string name, Vector2? value, Vector2? defval = null)
        {
            if (!value.HasValue) return;
            if (defval.HasValue && defval.Value.Equals(value.Value)) return;

            Guard.NotNull(writer, nameof(writer));

            writer.WritePropertyName(name);
            writer.WriteVector2(value.Value);
        }

        protected static void SerializeProperty(JSONWRITER writer, string name, Vector3? value, Vector3? defval = null)
        {
            if (!value.HasValue) return;
            if (defval.HasValue && defval.Value.Equals(value.Value)) return;

            Guard.NotNull(writer, nameof(writer));

            writer.WritePropertyName(name);
            writer.WriteVector3(value.Value);
        }

        protected static void SerializeProperty(JSONWRITER writer, string name, Vector4? value, Vector4? defval = null)
        {
            if (!value.HasValue) return;
            if (defval.HasValue && defval.Value.Equals(value.Value)) return;

            Guard.NotNull(writer, nameof(writer));

            writer.WritePropertyName(name);
            writer.WriteVector4(value.Value);
        }

        protected static void SerializeProperty(JSONWRITER writer, string name, Quaternion? value, Quaternion? defval = null)
        {
            if (!value.HasValue) return;
            if (defval.HasValue && defval.Value.Equals(value.Value)) return;

            Guard.NotNull(writer, nameof(writer));

            writer.WritePropertyName(name);
            writer.WriteQuaternion(value.Value);
        }

        protected static void SerializeProperty(JSONWRITER writer, string name, Matrix4x4? value, Matrix4x4? defval = null)
        {
            if (!value.HasValue) return;
            if (defval.HasValue && defval.Value.Equals(value.Value)) return;

            Guard.NotNull(writer, nameof(writer));

            writer.WritePropertyName(name);
            writer.WriteMatrix4x4(value.Value);
        }

        protected static void SerializePropertyEnumValue<T>(JSONWRITER writer, string name, T? value, T? defval = null)
            where T : struct
        {
            Guard.IsTrue(typeof(T).IsEnum, nameof(T));

            if (!value.HasValue) return;
            if (defval.HasValue && defval.Value.Equals(value)) return;

            Guard.NotNull(writer, nameof(writer));

            writer.WriteNumber(name, (int)(Object)value);
        }

        protected static void SerializePropertyEnumSymbol<T>(JSONWRITER writer, string name, T? value, T? defval = null)
            where T : struct
        {
            Guard.IsTrue(typeof(T).IsEnum, nameof(T));

            if (!value.HasValue) return;
            if (defval.HasValue && defval.Value.Equals(value)) return;

            Guard.NotNull(writer, nameof(writer));

            writer.WriteString(name, Enum.GetName(typeof(T), value));
        }

        protected static void SerializePropertyObject<T>(JSONWRITER writer, string name, T value)
            where T : JsonSerializable
        {
            if (value == null) return;

            Guard.NotNull(writer, nameof(writer));

            _SerializeProperty(writer, name, value);
        }

        protected static void SerializeProperty<T>(JSONWRITER writer, string name, IReadOnlyList<T> collection, int? minItems = 1)
        {
            if (collection == null) return;
            if (minItems.HasValue && collection.Count < minItems.Value) return;

            Guard.NotNull(writer, nameof(writer));

            writer.WritePropertyName(name);

            writer.WriteStartArray();

            foreach (var item in collection) _SerializeValue(writer, item);

            writer.WriteEndArray();
        }

        protected static void SerializeProperty<T>(JSONWRITER writer, string name, IReadOnlyDictionary<String, T> collection)
        {
            if (collection == null) return;
            if (collection.Count < 1) return;

            Guard.NotNull(writer, nameof(writer));

            writer.WritePropertyName(name);

            writer.WriteStartObject();

            foreach (var item in collection) _SerializeProperty(writer, item.Key, item.Value);

            writer.WriteEndObject();
        }

        private static void _SerializeProperty(JSONWRITER writer, String name, Object value)
        {
            Guard.NotNull(writer, nameof(writer));
            Guard.NotNull(value, nameof(value));

            if (_IsNullOrEmpty(value)) return;

            if (writer.TryWriteProperty(name, value)) return;

            writer.WritePropertyName(name);
            _SerializeValue(writer, value);
        }

        private static bool _IsNullOrEmpty(Object value)
        {
            if (value == null) return true;
            if (value is System.Collections.ICollection c && c.Count == 0) return true;
            return false;
        }

        private static void _SerializeValue(JSONWRITER writer, Object value)
        {
            Guard.NotNull(writer, nameof(writer));
            Guard.NotNull(value, nameof(value));

            System.Diagnostics.Debug.Assert(!value.GetType().IsEnum, "gltf schema does not define a typed way of serializing enums");

            if (writer.TryWriteValue(value)) return;

            if (value is System.Text.Json.Nodes.JsonNode jnode)
            {
                jnode.WriteTo(writer);
                return;
            }

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

                    _SerializeProperty(writer, key.ToString(), val);
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
                    _SerializeValue(writer, item);
                }

                writer.WriteEndArray();

                return;
            }

            throw new NotImplementedException($"Serialization of {value.GetType().Name} types is not supported.");
        }

        #endregion

        #region deserialization

        internal void Deserialize(ref JSONREADER reader)
        {
            if (reader.TokenType == JSONTOKEN.PropertyName) reader.Read();

            if (reader.TokenType == JSONTOKEN.StartObject)
            {
                while (reader.Read() && reader.TokenType != JSONTOKEN.EndObject)
                {
                    if (reader.TokenType == JSONTOKEN.PropertyName)
                    {
                        var key = reader.GetString();

                        DeserializeProperty(key, ref reader);
                    }
                    else
                    {
                        throw new NotImplementedException();
                    }
                }

                return;
            }

            throw new JSONEXCEPTION($"Unexpected token {reader.TokenType}");
        }

        protected static Object DeserializeUnknownObject(ref JSONREADER reader)
        {
            if (reader.TokenType == JSONTOKEN.PropertyName) reader.Read();

            if (reader.TokenType == JSONTOKEN.StartArray)
            {
                var list = new List<Object>();

                while (reader.Read() && reader.TokenType != JSONTOKEN.EndArray)
                {
                    list.Add(DeserializeUnknownObject(ref reader));
                }

                return list;
            }

            if (reader.TokenType == JSONTOKEN.StartObject)
            {
                var dict = new Dictionary<String, Object>();

                while (reader.Read() && reader.TokenType != JSONTOKEN.EndObject)
                {
                    if (reader.TokenType == JSONTOKEN.PropertyName)
                    {
                        var key = reader.GetString();

                        dict[key] = DeserializeUnknownObject(ref reader);
                    }
                    else
                    {
                        throw new JSONEXCEPTION();
                    }
                }

                return dict;
            }

            System.Diagnostics.Debug.Assert(reader.TokenType != JSONTOKEN.None);
            System.Diagnostics.Debug.Assert(reader.TokenType != JSONTOKEN.EndArray);
            System.Diagnostics.Debug.Assert(reader.TokenType != JSONTOKEN.EndObject);
            // System.Diagnostics.Debug.Assert(reader.TokenType != JsonToken.EndConstructor);

            return reader.GetAnyValue();
        }

        protected abstract void DeserializeProperty(string jsonPropertyName, ref JSONREADER reader);

        protected static T DeserializePropertyValue<
            #if !NETSTANDARD
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.NonPublicConstructors)]
            #endif
            T>(ref JSONREADER reader)
        {
            _TryCastValue<T>(ref reader, out Object v);

            System.Diagnostics.Debug.Assert(reader.TokenType != JSONTOKEN.StartArray);
            System.Diagnostics.Debug.Assert(reader.TokenType != JSONTOKEN.StartObject);
            System.Diagnostics.Debug.Assert(reader.TokenType != JSONTOKEN.PropertyName);
            // System.Diagnostics.Debug.Assert(reader.TokenType != JsonToken.StartConstructor);

            return (T)v;
        }

        protected static void DeserializePropertyValue<TParent,
            #if !NETSTANDARD
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.NonPublicConstructors)]
            #endif
            T>(ref JSONREADER reader, TParent owner, out T property)
            where TParent: class
        {
            _TryCastValue<T>(ref reader, out Object v);

            System.Diagnostics.Debug.Assert(reader.TokenType != JSONTOKEN.StartArray);
            System.Diagnostics.Debug.Assert(reader.TokenType != JSONTOKEN.StartObject);
            System.Diagnostics.Debug.Assert(reader.TokenType != JSONTOKEN.PropertyName);
            // System.Diagnostics.Debug.Assert(reader.TokenType != JsonToken.StartConstructor);

            property = (T)v;

            if (property is IChildOf<TParent> child)
            {
                child.SetLogicalParent(owner);
            }
        }

        protected static void DeserializePropertyList<TParent,
            #if !NETSTANDARD
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.NonPublicConstructors)]
            #endif
            T>(ref JSONREADER reader, TParent owner, IList<T> list)
            where TParent : class
        {
            DeserializePropertyList<T>(ref reader, list);
        }

        protected static void DeserializePropertyList<
            #if !NETSTANDARD
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.NonPublicConstructors)]
            #endif
            T>(ref JSONREADER reader, IList<T> list)
        {
            // Guard.NotNull(reader, nameof(reader));
            Guard.NotNull(list, nameof(list));

            if (reader.TokenType == JSONTOKEN.PropertyName) reader.Read();

            if (reader.TokenType != JSONTOKEN.StartArray) throw new JSONEXCEPTION();
            if (reader.TokenType == JSONTOKEN.StartObject) throw new JSONEXCEPTION();

            while (reader.Read() && reader.TokenType != JSONTOKEN.EndArray)
            {
                if (_TryCastValue<T>(ref reader, out Object item))
                {
                    list.Add((T)item);
                }

                System.Diagnostics.Debug.Assert(reader.TokenType != JSONTOKEN.StartArray);
                System.Diagnostics.Debug.Assert(reader.TokenType != JSONTOKEN.StartObject);
                System.Diagnostics.Debug.Assert(reader.TokenType != JSONTOKEN.PropertyName);
                // System.Diagnostics.Debug.Assert(reader.TokenType != JsonToken.StartConstructor);
            }

            if (list.Count == 0) throw new JSONEXCEPTION("Empty array found.");

            System.Diagnostics.Debug.Assert(reader.TokenType == JSONTOKEN.EndArray);
        }        

        protected static void DeserializePropertyDictionary<TParent,
            #if !NETSTANDARD
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.NonPublicConstructors)]
            #endif
            T>(ref JSONREADER reader, TParent owner, IDictionary<string, T> dict)
            where TParent : class
        {
            DeserializePropertyDictionary<T>(ref reader, dict);
        }

        protected static void DeserializePropertyDictionary<
            #if !NETSTANDARD
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.NonPublicConstructors)]
            #endif
            T>(ref JSONREADER reader, IDictionary<string, T> dict)            
        {
            Guard.NotNull(dict, nameof(dict));

            if (reader.TokenType == JSONTOKEN.PropertyName) reader.Read();

            if (reader.TokenType == JSONTOKEN.StartArray) throw new JSONEXCEPTION();
            if (reader.TokenType != JSONTOKEN.StartObject) throw new JSONEXCEPTION();

            while (reader.Read() && reader.TokenType != JSONTOKEN.EndObject)
            {
                if (reader.TokenType == JSONTOKEN.PropertyName)
                {
                    var key = reader.GetString();

                    if (_TryCastValue<T>(ref reader, out Object val))
                    {
                        dict[key] = (T)val;
                    }

                    System.Diagnostics.Debug.Assert(reader.TokenType != JSONTOKEN.StartArray);
                    System.Diagnostics.Debug.Assert(reader.TokenType != JSONTOKEN.StartObject);
                    System.Diagnostics.Debug.Assert(reader.TokenType != JSONTOKEN.PropertyName);
                    // System.Diagnostics.Debug.Assert(reader.TokenType != JsonToken.StartConstructor);
                }
            }

            if (dict.Count == 0) throw new JSONEXCEPTION("Empty dictionary found.");
        }

        private static bool _TryCastValue
            <
            #if !NETSTANDARD
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicParameterlessConstructor | DynamicallyAccessedMemberTypes.NonPublicConstructors)]
            #endif
            T>(ref JSONREADER reader, out Object value)
        {
            value = null;

            if (reader.TokenType == JSONTOKEN.EndArray) return false;
            if (reader.TokenType == JSONTOKEN.EndObject) return false;
            // if (reader.TokenType == JsonToken.EndConstructor) return false;

            if (reader.TokenType == JSONTOKEN.PropertyName) reader.Read();

            // untangle nullable            

            var vtype = typeof(T);
            var ntype = Nullable.GetUnderlyingType(vtype);
            if (ntype != null) vtype = ntype;

            // known types            

            if (vtype == typeof(String)) { value = reader.AsString(); return true; }
            if (vtype == typeof(Boolean)) { value = reader.AsBoolean(); return true; }

            if (vtype == typeof(Int16)) { value = reader.GetInt16(); return true; }
            if (vtype == typeof(Int32)) { value = reader.GetInt32(); return true; }
            if (vtype == typeof(Int64)) { value = reader.GetInt64(); return true; }
            if (vtype == typeof(UInt16)) { value = reader.GetUInt16(); return true; }
            if (vtype == typeof(UInt32)) { value = reader.GetUInt32(); return true; }
            if (vtype == typeof(UInt64)) { value = reader.GetUInt64(); return true; }
            if (vtype == typeof(Single)) { value = reader.GetSingle(); return true; }
            if (vtype == typeof(Double)) { value = reader.GetDouble(); return true; }
            if (vtype == typeof(Decimal)) { value = reader.GetDecimal(); return true; }

            if (vtype.IsEnum) { value = reader.AsEnum(vtype); return true; }

            if (vtype == typeof(Vector2))
            {
                var l = new List<float>(2);
                DeserializePropertyList<float>(ref reader, l);
                value = new Vector2(l[0], l[1]);
                return true;
            }

            if (vtype == typeof(Vector3))
            {
                var l = new List<float>(3);
                DeserializePropertyList<float>(ref reader, l);
                value = new Vector3(l[0], l[1], l[2]);
                return true;
            }

            if (vtype == typeof(Vector4))
            {
                var l = new List<float>(4);
                DeserializePropertyList<float>(ref reader, l);
                value = new Vector4(l[0], l[1], l[2], l[3]);
                return true;
            }

            if (vtype == typeof(Quaternion))
            {
                var l = new List<float>(4);
                DeserializePropertyList<float>(ref reader, l);
                value = new Quaternion(l[0], l[1], l[2], l[3]);
                return true;
            }

            if (vtype == typeof(Matrix4x4))
            {
                var l = new List<float>(16);
                DeserializePropertyList<float>(ref reader, l);
                value = new Matrix4x4
                    (
                    l[0], l[1], l[2], l[3],
                    l[4], l[5], l[6], l[7],
                    l[8], l[9], l[10], l[11],
                    l[12], l[13], l[14], l[15]
                    );
                return true;
            }

            if (typeof(System.Text.Json.Nodes.JsonNode).IsAssignableFrom(vtype))
            {
                value = System.Text.Json.Nodes.JsonNode.Parse(ref reader);
                return true;
            }

            if (typeof(JsonSerializable).IsAssignableFrom(vtype))
            {
                // Instance creation on AOT compiled binaries depends on classes defining:
                // [System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembers(System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.NonPublicConstructors | System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicConstructors)]

                var item = Activator.CreateInstance(vtype, true) as JsonSerializable;

                // System.Diagnostics.Debug.Assert(reader.TokenType == JSONTOKEN.StartObject);
                item.Deserialize(ref reader);
                // System.Diagnostics.Debug.Assert(reader.TokenType == JSONTOKEN.EndObject);

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
                        DeserializePropertyDictionary(ref reader, dict);
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
