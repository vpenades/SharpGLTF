using System;
using System.Collections.Generic;
using System.Text;

using System.Text.Json;

namespace SharpGLTF.IO
{
    [System.Diagnostics.DebuggerDisplay("Unknown {_Name}")]
    class UnknownNode : JsonSerializable
    {
        public UnknownNode(string name) { this._Name = name; }

        private readonly string _Name;

        private readonly JsonDictionary _Properties = new JsonDictionary();

        public string Name => _Name;

        public JsonDictionary Properties => _Properties;

        protected override void DeserializeProperty(string property, ref Utf8JsonReader reader)
        {
            reader.Read();
            _Properties[property] = DeserializeUnknownObject(ref reader);
        }

        protected override void SerializeProperties(Utf8JsonWriter writer)
        {
            foreach (var kvp in _Properties)
            {
                SerializeProperty(writer, kvp.Key, kvp.Value);
            }
        }
    }
}
