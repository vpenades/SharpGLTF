using System;
using System.Collections.Generic;
using System.Linq;

using Newtonsoft.Json;

namespace SharpGLTF.Schema2
{
    using IO;

    public abstract class ExtraProperties : JsonSerializable
    {
        #region data

        private readonly List<JsonSerializable> _extensions = new List<JsonSerializable>();

        private Object _extras;

        #endregion

        #region properties

        /// <summary>
        /// Gets a collection of <see cref="JsonSerializable"/> instances.
        /// </summary>
        public IReadOnlyCollection<JsonSerializable> Extensions => _extensions;

        /// <summary>
        /// Gets the extras value, where the value can be either an intrinsic type <see cref="TypeCode"/> , a <see cref="JsonList"/> or a <see cref="JsonDictionary"/>
        /// </summary>
        public Object Extras => _extras;

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

        /// <summary>
        /// Gets a collection of <see cref="ExtraProperties"/> instances stored by this object.
        /// </summary>
        /// <returns>A collection of <see cref="ExtraProperties"/> instances.</returns>
        protected virtual IEnumerable<ExtraProperties> GetLogicalChildren()
        {
            return _extensions.OfType<ExtraProperties>();
        }

        protected static IEnumerable<ExtraProperties> Flatten(ExtraProperties container)
        {
            yield return container;

            foreach (var c in container.GetLogicalChildren())
            {
                var cc = Flatten(c);

                foreach (var ccc in cc) yield return ccc;
            }
        }

        /// <summary>
        /// Gets the Extras property as a <see cref="JsonDictionary"/>
        /// </summary>
        /// <param name="overwrite">true if the current value is to be replaced by a <see cref="JsonDictionary"/> instance.</param>
        /// <returns>A <see cref="JsonDictionary"/> instance or null.</returns>
        public JsonDictionary TryUseExtrasAsDictionary(bool overwrite)
        {
            if (this._extras is JsonDictionary dict) return dict;

            if (overwrite) this._extras = new JsonDictionary();

            return this._extras as JsonDictionary;
        }

        /// <summary>
        /// Gets the Extras property as a <see cref="JsonList"/>
        /// </summary>
        /// <param name="overwrite">true if the current value is to be replaced by a <see cref="JsonList"/> instance.</param>
        /// <returns>A <see cref="JsonDictionary"/> instance or null.</returns>
        public JsonList TryUseExtrasAsList(bool overwrite)
        {
            if (this._extras is JsonList list) return list;

            if (overwrite) this._extras = new JsonList();

            return this._extras as JsonList;
        }

        #endregion

        #region validation

        internal override void Validate(Validation.ValidationContext result)
        {
            base.Validate(result);

            if (this._extras != null)
            {
                if (!IO.JsonUtils.IsSerializable(this._extras)) result.InvalidJson(this, "Extras");
            }
        }

        #endregion

        #region serialization API

        /// <summary>
        /// Writes the properties of the current instance to a <see cref="JsonWriter"/>.
        /// </summary>
        /// <param name="writer">The target writer.</param>
        protected override void SerializeProperties(JsonWriter writer)
        {
            if (_extensions.Count > 0)
            {
                var dict = _ToDictionary(this, _extensions);
                SerializeProperty(writer, "extensions", dict);
            }

            if (_extras != null)
            {
                SerializeProperty(writer, "extras", _extras);
            }
        }

        private static IReadOnlyDictionary<string, JsonSerializable> _ToDictionary(JsonSerializable context, IEnumerable<JsonSerializable> serializables)
        {
            var dict = new Dictionary<string, JsonSerializable>();

            foreach (var val in serializables)
            {
                if (val == null) continue;

                string key = null;

                if (val is UnknownNode unk) key = unk.Name;
                else key = ExtensionsFactory.Identify(context.GetType(), val.GetType());

                if (key == null) continue;
                dict[key] = val;
            }

            return dict;
        }

        /// <summary>
        /// Reads the properties of the current instance from a <see cref="JsonReader"/>.
        /// </summary>
        /// <param name="property">The name of the property.</param>
        /// <param name="reader">The source reader.</param>
        protected override void DeserializeProperty(string property, JsonReader reader)
        {
            switch (property)
            {
                case "extensions": _DeserializeExtensions(this, reader, _extensions); break;

                case "extras": _extras = DeserializeUnknownObject(reader); break;

                default: reader.Skip(); break;
            }
        }

        private static void _DeserializeExtensions(JsonSerializable parent, JsonReader reader, IList<JsonSerializable> extensions)
        {
            reader.Read();

            if (reader.TokenType == JsonToken.StartObject)
            {
                while (reader.Read() && reader.TokenType != JsonToken.EndObject)
                {
                    var key = reader.Value as String;

                    var val = ExtensionsFactory.Create(parent, key);

                    if (val == null) val = new UnknownNode(key);

                    val.Deserialize(reader);
                    extensions.Add(val);
                    continue;
                }
            }

            reader.Skip();
        }

        #endregion
    }
}