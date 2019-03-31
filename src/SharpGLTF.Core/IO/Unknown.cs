using System;
using System.Collections.Generic;
using System.Text;

using Newtonsoft.Json;

namespace SharpGLTF.IO
{
    public class JsonDictionary : Dictionary<String, Object> { }

    public class JsonList : List<Object> { }

    [System.Diagnostics.DebuggerDisplay("Unknown {_Name}")]
    class UnknownNode : JsonSerializable
    {
        public UnknownNode(string name) { this._Name = name; }

        private readonly string _Name;

        private readonly JsonDictionary _Properties = new JsonDictionary();

        public string Name => _Name;

        public JsonDictionary Properties => _Properties;

        protected override void DeserializeProperty(string property, JsonReader reader)
        {
            reader.Read();
            _Properties[property] = DeserializeUnknownObject(reader);
        }

        protected override void SerializeProperties(JsonWriter writer)
        {
            foreach (var kvp in _Properties)
            {
                SerializeProperty(writer, kvp.Key, kvp.Value);
            }
        }
    }
}
