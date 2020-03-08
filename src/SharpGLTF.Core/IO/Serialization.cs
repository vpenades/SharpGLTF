using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;
using System.Text.Json;

namespace SharpGLTF.IO
{
    static class _JSonSerializationExtensions
    {
        public static Boolean AsBoolean(this in Utf8JsonReader reader)
        {
            switch (reader.TokenType)
            {
                case JsonTokenType.Null: return false;
                case JsonTokenType.True: return true;
                case JsonTokenType.False: return false;
                case JsonTokenType.Number: return reader.GetInt32() != 0;
                default: throw new NotImplementedException();
            }
        }

        public static String AsString(this in Utf8JsonReader reader)
        {
            switch (reader.TokenType)
            {
                case JsonTokenType.Null: return null;
                case JsonTokenType.String: return reader.GetString();
                case JsonTokenType.PropertyName: return reader.GetString();
                case JsonTokenType.True: return "true";
                case JsonTokenType.False: return "false";
                case JsonTokenType.Number: return reader.GetDecimal().ToString(System.Globalization.CultureInfo.InvariantCulture);
                default: throw new NotImplementedException();
            }
        }

        public static Object AsEnum(this in Utf8JsonReader reader, Type enumType)
        {
            if (reader.TokenType == JsonTokenType.String)
            {
                var jsonVal = reader.GetString();

                try { return Enum.Parse(enumType, jsonVal, true); }
                catch (System.ArgumentException ex)
                {
                    throw new System.Text.Json.JsonException($"Value {jsonVal} not found int {enumType}", ex);
                }
            }

            if (reader.TokenType == JsonTokenType.Number)
            {
                if (reader.TryGetInt32(out Int32 int32Val)) return Enum.ToObject(enumType, int32Val);
                if (reader.TryGetInt64(out Int64 int64Val)) return Enum.ToObject(enumType, int64Val);
            }

            throw new NotImplementedException();
        }

        public static Object GetAnyValue(this in Utf8JsonReader reader)
        {
            switch (reader.TokenType)
            {
                case JsonTokenType.Null: return null;
                case JsonTokenType.True: return true;
                case JsonTokenType.False: return false;
                case JsonTokenType.String: return reader.GetString();
                case JsonTokenType.Number: return reader.GetDecimal();
                case JsonTokenType.PropertyName: return reader.GetString();
                default: throw new NotImplementedException();
            }
        }

        public static Object GetValueAs(this in Utf8JsonReader reader, Type vtype)
        {
            System.Diagnostics.Debug.Assert(reader.TokenType != JsonTokenType.StartArray);
            System.Diagnostics.Debug.Assert(reader.TokenType != JsonTokenType.EndArray);
            System.Diagnostics.Debug.Assert(reader.TokenType != JsonTokenType.StartObject);
            System.Diagnostics.Debug.Assert(reader.TokenType != JsonTokenType.EndObject);

            if (reader.TokenType == JsonTokenType.Null) return null;

            if (vtype == typeof(String)) { return reader.AsString(); }
            if (vtype == typeof(Boolean)) { return reader.AsBoolean(); }
            if (vtype == typeof(Int16)) { return reader.GetInt16(); }
            if (vtype == typeof(Int32)) { return reader.GetInt32(); }
            if (vtype == typeof(Int64)) { return reader.GetInt64(); }
            if (vtype == typeof(UInt16)) { return reader.GetUInt16(); }
            if (vtype == typeof(UInt32)) { return reader.GetUInt32(); }
            if (vtype == typeof(UInt64)) { return reader.GetUInt64(); }
            if (vtype == typeof(Single)) { return reader.GetSingle(); }
            if (vtype == typeof(Double)) { return reader.GetDouble(); }
            if (vtype == typeof(Decimal)) { return reader.GetDecimal(); }

            return Convert.ChangeType(reader.GetString(), vtype, System.Globalization.CultureInfo.InvariantCulture);
        }

        public static T GetValueAs<T>(this in Utf8JsonReader reader)
            where T : struct
        {
            return (T)reader.GetValueAs(typeof(T));
        }

        public static bool TryWriteProperty(this Utf8JsonWriter writer, string property, Object value)
        {
            if (value is String vstr) { writer.WriteString(property, vstr); return true; }
            if (value is Boolean vbol) { writer.WriteBoolean(property, vbol); return true; }

            if (value is Byte vu8) { writer.WriteNumber(property, vu8); return true; }
            if (value is UInt16 vu16) { writer.WriteNumber(property, vu16); return true; }
            if (value is UInt32 vu32) { writer.WriteNumber(property, vu32); return true; }
            if (value is UInt64 vu64) { writer.WriteNumber(property, vu64); return true; }

            if (value is SByte vs8) { writer.WriteNumber(property, vs8); return true; }
            if (value is Int16 vs16) { writer.WriteNumber(property, vs16); return true; }
            if (value is Int32 vs32) { writer.WriteNumber(property, vs32); return true; }
            if (value is Int64 vs64) { writer.WriteNumber(property, vs64); return true; }

            if (value is Single vf32) { writer.WriteNumber(property, vf32); return true; }
            if (value is Double vf64) { writer.WriteNumber(property, vf64); return true; }
            if (value is Decimal vfxx) { writer.WriteNumber(property, vfxx); return true; }

            if (value is Vector2 vvv2) { writer.WritePropertyName(property); writer.WriteVector2(vvv2); return true; }
            if (value is Vector3 vvv3) { writer.WritePropertyName(property); writer.WriteVector3(vvv3); return true; }
            if (value is Vector4 vvv4) { writer.WritePropertyName(property); writer.WriteVector4(vvv4); return true; }
            if (value is Quaternion qqq4) { writer.WritePropertyName(property); writer.WriteQuaternion(qqq4); return true; }
            if (value is Matrix4x4 mm44) { writer.WritePropertyName(property); writer.WriteMatrix4x4(mm44); return true; }

            return false;
        }

        public static bool TryWriteValue(this Utf8JsonWriter writer, Object value)
        {
            if (value is String vstr) { writer.WriteStringValue(vstr); return true; }
            if (value is Boolean vbol) { writer.WriteBooleanValue(vbol); return true; }

            if (value is Byte vu8) { writer.WriteNumberValue(vu8); return true; }
            if (value is UInt16 vu16) { writer.WriteNumberValue(vu16); return true; }
            if (value is UInt32 vu32) { writer.WriteNumberValue(vu32); return true; }
            if (value is UInt64 vu64) { writer.WriteNumberValue(vu64); return true; }

            if (value is SByte vs8) { writer.WriteNumberValue(vs8); return true; }
            if (value is Int16 vs16) { writer.WriteNumberValue(vs16); return true; }
            if (value is Int32 vs32) { writer.WriteNumberValue(vs32); return true; }
            if (value is Int64 vs64) { writer.WriteNumberValue(vs64); return true; }

            if (value is Single vf32) { writer.WriteNumberValue(vf32); return true; }
            if (value is Double vf64) { writer.WriteNumberValue(vf64); return true; }
            if (value is Decimal vfxx) { writer.WriteNumberValue(vfxx); return true; }

            if (value is Vector2 vvv2) { writer.WriteVector2(vvv2); return true; }
            if (value is Vector3 vvv3) { writer.WriteVector3(vvv3); return true; }
            if (value is Vector4 vvv4) { writer.WriteVector4(vvv4); return true; }
            if (value is Quaternion qqq4) { writer.WriteQuaternion(qqq4); return true; }
            if (value is Matrix4x4 mm44) { writer.WriteMatrix4x4(mm44); return true; }

            return false;
        }

        public static void WriteVector2(this Utf8JsonWriter writer, Vector2 v)
        {
            writer.WriteStartArray();
            writer.WriteNumberValue(v.X);
            writer.WriteNumberValue(v.Y);
            writer.WriteEndArray();
        }

        public static void WriteVector3(this Utf8JsonWriter writer, Vector3 v)
        {
            writer.WriteStartArray();
            writer.WriteNumberValue(v.X);
            writer.WriteNumberValue(v.Y);
            writer.WriteNumberValue(v.Z);
            writer.WriteEndArray();
        }

        public static void WriteVector4(this Utf8JsonWriter writer, Vector4 v)
        {
            writer.WriteStartArray();
            writer.WriteNumberValue(v.X);
            writer.WriteNumberValue(v.Y);
            writer.WriteNumberValue(v.Z);
            writer.WriteNumberValue(v.W);
            writer.WriteEndArray();
        }

        public static void WriteQuaternion(this Utf8JsonWriter writer, Quaternion q)
        {
            writer.WriteStartArray();
            writer.WriteNumberValue(q.X);
            writer.WriteNumberValue(q.Y);
            writer.WriteNumberValue(q.Z);
            writer.WriteNumberValue(q.W);
            writer.WriteEndArray();
        }

        public static void WriteMatrix4x4(this Utf8JsonWriter writer, Matrix4x4 m)
        {
            writer.WriteStartArray();
            writer.WriteNumberValue(m.M11);
            writer.WriteNumberValue(m.M12);
            writer.WriteNumberValue(m.M13);
            writer.WriteNumberValue(m.M14);
            writer.WriteNumberValue(m.M21);
            writer.WriteNumberValue(m.M22);
            writer.WriteNumberValue(m.M23);
            writer.WriteNumberValue(m.M24);
            writer.WriteNumberValue(m.M31);
            writer.WriteNumberValue(m.M32);
            writer.WriteNumberValue(m.M33);
            writer.WriteNumberValue(m.M34);
            writer.WriteNumberValue(m.M41);
            writer.WriteNumberValue(m.M42);
            writer.WriteNumberValue(m.M43);
            writer.WriteNumberValue(m.M44);
            writer.WriteEndArray();
        }
    }
}
