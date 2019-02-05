using System;
using System.Collections.Generic;
using System.Text;

using Newtonsoft.Json;

namespace glTF2Sharp.IO
{
    static class _JSonSerializationExtensions
    {
        public static Object GetValueAs(this JsonReader reader, Type vtype)
        {
            System.Diagnostics.Debug.Assert
                (
                reader.TokenType == JsonToken.Boolean ||
                reader.TokenType == JsonToken.String ||
                reader.TokenType == JsonToken.Integer ||
                reader.TokenType == JsonToken.Float
                );

            return Convert.ChangeType(reader.Value, vtype, System.Globalization.CultureInfo.InvariantCulture);
        }

        public static T GetValueAs<T>(this JsonReader reader) where T : struct
        {
            return (T)reader.GetValueAs(typeof(T));
        }        

        public static void WriteVector2(this JsonWriter writer, System.Numerics.Vector2 v)
        {
            writer.WriteStartArray();
            writer.WriteValue(v.X);
            writer.WriteValue(v.Y);            
            writer.WriteEndArray();
        }

        public static void WriteVector3(this JsonWriter writer, System.Numerics.Vector3 v)
        {
            writer.WriteStartArray();
            writer.WriteValue(v.X);
            writer.WriteValue(v.Y);
            writer.WriteValue(v.Z);
            writer.WriteEndArray();
        }

        public static void WriteVector4(this JsonWriter writer, System.Numerics.Vector4 v)
        {
            writer.WriteStartArray();
            writer.WriteValue(v.X);
            writer.WriteValue(v.Y);
            writer.WriteValue(v.Z);
            writer.WriteValue(v.W);
            writer.WriteEndArray();
        }

        public static void WriteQuaternion(this JsonWriter writer, System.Numerics.Quaternion q)
        {
            writer.WriteStartArray();
            writer.WriteValue(q.X);
            writer.WriteValue(q.Y);
            writer.WriteValue(q.Z);
            writer.WriteValue(q.W);
            writer.WriteEndArray();
        }

        public static void WriteMatrix4x4(this JsonWriter writer, System.Numerics.Matrix4x4 m)
        {
            writer.WriteStartArray();
            writer.WriteValue(m.M11);
            writer.WriteValue(m.M12);
            writer.WriteValue(m.M13);
            writer.WriteValue(m.M14);
            writer.WriteValue(m.M21);
            writer.WriteValue(m.M22);
            writer.WriteValue(m.M23);
            writer.WriteValue(m.M24);
            writer.WriteValue(m.M31);
            writer.WriteValue(m.M32);
            writer.WriteValue(m.M33);
            writer.WriteValue(m.M34);
            writer.WriteValue(m.M41);
            writer.WriteValue(m.M42);
            writer.WriteValue(m.M43);
            writer.WriteValue(m.M44);
            writer.WriteEndArray();
        }
    }
}
