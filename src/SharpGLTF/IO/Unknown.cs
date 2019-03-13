using System;
using System.Collections.Generic;
using System.Text;

using Newtonsoft.Json;

namespace SharpGLTF.IO
{
    [System.Diagnostics.DebuggerDisplay("Unknown {_Name}")]
    class Unknown : JsonSerializable
    {
        public Unknown(string name) { this._Name = name; }

        private readonly string _Name;

        private readonly Dictionary<string, Object> _Properties = new Dictionary<string, object>();

        public string Name => _Name;

        public IDictionary<String, Object> Properties => _Properties;

        protected override void DeserializeProperty(string property, JsonReader reader)
        {
            reader.Read();
            _Properties[property] = DeserializeObject(reader);
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
