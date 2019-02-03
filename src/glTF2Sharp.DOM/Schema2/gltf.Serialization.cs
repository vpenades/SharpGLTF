using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace glTF2Sharp.Schema2
{
    using ROOT = ModelRoot;

    public delegate Byte[] AssetReader(String assetName);

    public delegate void AssetWriter(String assetName, Byte[] assetData);    

    public class ReadSettings
    {
        public ReadSettings()
        {
            
        }

        internal ReadSettings(string filePath)
        {
            Guard.FilePathMustExist(filePath, nameof(filePath));

            var dir = Path.GetDirectoryName(filePath);

            FileReader = asset => File.ReadAllBytes(Path.Combine(dir, asset));            
        }

        public AssetReader FileReader { get; set; }        
    }

    public class WriteSettings
    {
        #region lifecycle

        internal WriteSettings(string filePath)
        {
            Guard.FilePathMustBeValid(filePath,nameof(filePath));

            var dir = Path.GetDirectoryName(filePath);

            this.FileWriter = (fn, d) => File.WriteAllBytes(Path.Combine(dir, fn), d);
        }

        internal WriteSettings(Func<string, Stream> fileWriter)
        {
            void assetWriter(string n, byte[] d)
            {
                using (var s = fileWriter(n))
                {
                    s.Write(d, 0, d.Length);
                }
            }

            this.FileWriter = assetWriter;
        }

        internal WriteSettings(MemoryStream memory)
        {
            JSonFormatting = Formatting.None;
            BinaryMode = true;
            EmbedImages = true;

            this.FileWriter = (fn, d) => memory.Write(d,0,d.Length);
        }

        #endregion

        #region data

        public Boolean BinaryMode { get; set; }

        public Boolean EmbedImages { get; set; }

        public Formatting JSonFormatting { get; set; }

        public AssetWriter FileWriter { get; set; }

        #endregion
    }

    partial class ModelRoot
    {
        #region Read API

        public static ROOT Load(string filePath)
        {
            Guard.FilePathMustExist(filePath, nameof(filePath));

            var settings = new ReadSettings(filePath);

            using (var s = File.OpenRead(filePath))
            {
                return Read(s, settings);
            }
        }

        public static ROOT ParseGLB(Byte[] glb)
        {
            Guard.NotNull(glb, nameof(glb));

            using (var m = new MemoryStream(glb))
            {
                return ReadGLB(m, new ReadSettings());
            }
        }

        /// <summary>
        /// Reads a <code>Schema.Gltf</code> model from a stream
        /// </summary>
        /// <param name="stream">Readable stream to a gltf/glb model</param>
        /// <returns><code>Schema.Gltf</code> model</returns>
        public static ROOT Read(Stream stream, ReadSettings settings)
        {
            bool binaryFile = glb._Identify(stream);

            if (binaryFile) return ReadGLB(stream, settings);
            else            return ReadGLTF(stream, settings);
        }

        public static ROOT ReadGLTF(Stream stream, ReadSettings settings)
        {
            Guard.NotNull(stream, nameof(stream));
            Guard.NotNull(settings, nameof(settings));

            string content = null;

            using (var streamReader = new StreamReader(stream))
            {
                content = streamReader.ReadToEnd();
            }            

            return Parse(content, settings);
        }

        public static ROOT ReadGLB(Stream stream, ReadSettings settings)
        {
            Guard.NotNull(stream, nameof(stream));
            Guard.NotNull(settings, nameof(settings));

            var chunks = glb.ReadBinaryFile(stream);

            var dom = Encoding.UTF8.GetString(chunks[glb.CHUNKJSON]);

            if (chunks.ContainsKey(glb.CHUNKBIN))
            {
                settings.FileReader = key => string.IsNullOrEmpty(key) ? chunks[glb.CHUNKBIN] : settings.FileReader?.Invoke(key);
            }

            return Parse(dom, settings);
        }        

        public static ROOT Parse(String jsonContent, ReadSettings settings)
        {
            Guard.NotNullOrEmpty(jsonContent, nameof(jsonContent));
            Guard.NotNull(settings, nameof(settings));

            using (var tr = new StringReader(jsonContent))
            {
                return _Read(tr, settings);
            }
        }

        private static ROOT _Read(StringReader textReader, ReadSettings settings)
        {
            Guard.NotNull(textReader, nameof(textReader));
            Guard.NotNull(settings, nameof(settings));

            using (var reader = new JsonTextReader(textReader))
            {
                var root = new ROOT();
                root.DeserializeObject(reader);

                var ex = root.Validate().FirstOrDefault();
                if (ex != null) throw ex;

                foreach (var buffer in root._buffers)
                {
                    buffer._ResolveUri(settings.FileReader);
                }

                foreach (var image in root._images)
                {
                    image._ResolveUri(settings.FileReader);
                }

                return root;
            }
        }        

        #endregion

        #region Write API

        // TODO: usually when we save the gltf file, we need to amend/fix several features,
        // which goes against good practices of not modyfing any file when it is being saved.
        // a possible solution would be to do a shallow copy of RootObject and update Buffers, BufferViews, etc
        // an issue that complicates things is that it requires to copy the extensions of root, buffers, etc

        public void SaveGLB(string filePath)
        {
            Guard.FilePathMustBeValid(filePath,nameof(filePath));

            var settings = new WriteSettings(filePath)
            {
                JSonFormatting = Formatting.None,
                BinaryMode = true
            };

            var name = Path.GetFileNameWithoutExtension(filePath);

            Write(name, settings);
        }        

        public void SaveGLTF(string filePath, Formatting fmt = Formatting.None)
        {
            Guard.FilePathMustBeValid(filePath, nameof(filePath));

            var settings = new WriteSettings(filePath)
            {
                JSonFormatting = fmt,
                BinaryMode = false
            };

            var name = Path.GetFileNameWithoutExtension(filePath);

            Write(name, settings);            
        }        

        public void Write(string name, WriteSettings settings)
        {
            if (settings.BinaryMode)
            {
                var ex = glb.IsBinaryCompatible(this); if (ex != null) throw ex;

                for (int i = 0; i < this._buffers.Count; ++i)
                {
                    var buffer = this._buffers[i];
                    buffer._WriteToInternal();
                }
            }
            else
            {
                for (int i = 0; i < this._buffers.Count; ++i)
                {
                    var buffer = this._buffers[i];
                    var bname = $"{name}.{i}.bin";
                    buffer._WriteToExternal(bname, settings.FileWriter);
                }
            }

            for (int i = 0; i < this._images.Count; ++i)
            {
                var image = this._images[i];
                var iname = $"{name}.{i}";
                if (settings.EmbedImages) image._EmbedAssets();
                else image._WriteExternalAssets(iname, settings.FileWriter);
            }

            using (var m = new MemoryStream())
            {
                if (settings.BinaryMode)
                {
                    using (var w = new BinaryWriter(m))
                    {
                        glb.WriteBinaryModel(w, this);
                    }

                    settings.FileWriter($"{name}.glb", m.ToArray());
                }
                else
                {
                    using (var w = new StreamWriter(m))
                    {
                        WriteJSON(w, settings.JSonFormatting);
                    }

                    settings.FileWriter($"{name}.gltf", m.ToArray());
                }
            }            

            foreach (var b in this._buffers) b._ClearAfterWrite();
            foreach (var i in this._images) i._ClearAfterWrite();
        }

        public void WriteJSON(TextWriter sw, Formatting fmt)
        {
            using (var writer = new JsonTextWriter(sw))
            {
                writer.Formatting = fmt;

                this.Serialize(writer);
            }
        }

        public string GetJSON(Formatting fmt)
        {
            using (var ss = new StringWriter())
            {
                WriteJSON(ss, fmt);
                return ss.ToString();
            }
        }

        public Byte[] GetGLB()
        {
            using (var m = new MemoryStream())
            {
                var settings = new WriteSettings(m);                

                Write("model", settings);

                return m.ToArray();
            }                        
        }

        #endregion        
    }

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
            foreach(var item in collection)
            {
                _Serialize(writer, item);
            }
            writer.WriteEndArray();
        }

        protected static void SerializeProperty<T>(JsonWriter writer, string name, IReadOnlyDictionary<String,T> collection)
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

            if (value is IReadOnlyDictionary<String,Int32> vdsi)
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
                value = new System.Numerics.Vector4(l[0], l[1], l[2],l[3]);
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

                var item = System.Activator.CreateInstance(vtype,true) as JsonSerializable;

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

        public static T GetValueAs<T>(this JsonReader reader) where T:struct
        {
            return (T)reader.GetValueAs(typeof(T));
        }

        public static System.Numerics.Vector3 ReadVector3(this JsonReader reader)
        {
            if (reader.TokenType == JsonToken.StartArray) reader.Read();
            var x = reader.GetValueAs<float>();

            reader.Read();
            var y = reader.GetValueAs<float>();

            reader.Read();
            var z = reader.GetValueAs<float>();

            return new System.Numerics.Vector3(x, y, z);
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
