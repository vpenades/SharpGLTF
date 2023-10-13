using System;
using System.Collections.Generic;
using System.Text;

using System.Text.Json;

using JSONCONTENT = System.Text.Json.Nodes.JsonNode;

namespace SharpGLTF.IO
{
    /// <summary>
    /// Represents a node with an unknown type in a glTF Schema.
    /// </summary>
    /// <remarks>
    /// When parsing a glTF json, the json object types are usually known at runtime,
    /// So the types are instantiated as the json is being parsed. But it can happen
    /// that we can find a json object for which the type is not known at runtime.
    /// This usually happens with unknown extensions, which are stored using this
    /// object.
    /// </remarks>
    [System.Diagnostics.DebuggerDisplay("Unknown {_Name}")]
    class UnknownNode : JsonSerializable
    {
        #region lifecycle
        public UnknownNode(string name) { this._Name = name; }

        #endregion

        #region data

        private readonly string _Name;

        private readonly Dictionary<String, JSONCONTENT> _Properties = new Dictionary<String, JSONCONTENT>();

        #endregion

        #region properties

        public string Name => _Name;

        public IReadOnlyDictionary<String, JSONCONTENT> Properties => _Properties;

        #endregion

        #region API

        protected override void DeserializeProperty(string property, ref Utf8JsonReader reader)
        {
            reader.Read();
            _Properties[property] = JSONCONTENT.Parse(ref reader);
        }

        protected override void SerializeProperties(Utf8JsonWriter writer)
        {
            foreach (var kvp in _Properties)
            {
                SerializeProperty(writer, kvp.Key, kvp.Value);
            }
        }

        #endregion
    }
}
