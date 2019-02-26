using System;
using System.Collections.Generic;
using System.Linq;

using Newtonsoft.Json;

namespace SharpGLTF.Schema2
{
    using IO;

    public abstract class glTFProperty : JsonSerializable
    {
        #region data

        private readonly List<JsonSerializable> _extensions = new List<JsonSerializable>();

        private Object _extras;

        #endregion

        #region properties

        public IReadOnlyCollection<JsonSerializable> Extensions => _extensions;

        #endregion

        #region API

        public T GetExtension<T>()
            where T : JsonSerializable
        {
            return _extensions.OfType<T>().FirstOrDefault();
        }

        public void SetExtension<T>(T value)
            where T : JsonSerializable
        {
            Guard.NotNull(value, nameof(value));

            var idx = _extensions.IndexOf(item => item.GetType() == typeof(T));

            if (idx >= 0) { _extensions[idx] = value; return; }

            _extensions.Add(value);
        }

        public void RemoveExtensions<T>(T value)
            where T : JsonSerializable
        {
            _extensions.RemoveAll(item => item == value);
        }

        public void RemoveExtensions<T>()
            where T : JsonSerializable
        {
            _extensions.RemoveAll(item => item.GetType() == typeof(T));
        }

        #endregion

        #region serialization API

        protected override void SerializeProperties(JsonWriter writer)
        {
            SerializeProperty(writer, "extensions", _extensions);

            // SerializeProperty(writer, "extras", _extras);
        }

        protected override void DeserializeProperty(JsonReader reader, string property)
        {
            switch (property)
            {
                case "extras": reader.Skip(); break;
                case "extensions": _DeserializeExtensions(reader, _extensions); break;

                // case "extras": _extras = DeserializeValue<Object>(reader); break;

                default: reader.Skip(); break;
            }
        }

        private static void _DeserializeExtensions(JsonReader reader, IList<JsonSerializable> extensions)
        {
            while (true)
            {
                reader.Read();

                if (reader.TokenType == JsonToken.EndObject) break;
                if (reader.TokenType == JsonToken.EndArray) break;

                if (reader.TokenType == JsonToken.StartArray)
                {
                    while (true)
                    {
                        if (reader.TokenType == JsonToken.EndArray) break;

                        _DeserializeExtensions(reader, extensions);
                    }

                    break;
                }

                if (reader.TokenType == JsonToken.StartObject) continue;

                System.Diagnostics.Debug.Assert(reader.TokenType == JsonToken.PropertyName);
                var key = reader.Value as String;

                var val = ExtensionsFactory.Create(key);

                if (val == null)
                {
                    reader.Skip();
                }
                else
                {
                    val.DeserializeObject(reader);
                    extensions.Add(val);
                }
            }
        }

        #endregion
    }
}