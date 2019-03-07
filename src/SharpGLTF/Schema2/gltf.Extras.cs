using System;
using System.Collections.Generic;
using System.Text;

using Newtonsoft.Json;

namespace SharpGLTF.Schema2
{
    using IO;

    class Extras : JsonSerializable
    {
        private readonly Dictionary<string, Object> _Properties = new Dictionary<string, object>();

        public IDictionary<String, Object> Properties => _Properties;

        protected override void DeserializeProperty(JsonReader reader, string property)
        {
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
