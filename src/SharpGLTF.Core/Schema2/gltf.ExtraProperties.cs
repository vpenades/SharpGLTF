using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

using System.Text.Json;

using SharpGLTF.IO;
using SharpGLTF.Reflection;

using JSONTOKEN = System.Text.Json.JsonTokenType;
using JSONEXTRAS = System.Text.Json.Nodes.JsonNode;

namespace SharpGLTF.Schema2
{
    public interface IExtraProperties
    {
        IReadOnlyCollection<JsonSerializable> Extensions { get; }
        
        JSONEXTRAS Extras { get; set; }
    }

    /// <summary>
    /// Represents the base class for all glTF 2 Schema objects.
    /// </summary>
    /// <remarks>
    /// Defines the <see cref="Extras"/> property for every glTF object.
    /// </remarks>
    public abstract class ExtraProperties
        : JsonSerializable
        , IExtraProperties
        , IReflectionObject
    {
        #region data

        private readonly List<JsonSerializable> _extensions = new List<JsonSerializable>();

        private JSONEXTRAS _extras;

        #endregion

        #region properties

        /// <summary>
        /// Gets a collection of <see cref="JsonSerializable"/> instances.
        /// </summary>
        public IReadOnlyCollection<JsonSerializable> Extensions => _extensions;

        /// <summary>
        /// Gets or sets the extras content of this instance.
        /// </summary>        
        public JSONEXTRAS Extras
        {
            #if NET6_0_OR_GREATER
            [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(System.Text.Json.Nodes.JsonArray))]
            [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(System.Text.Json.Nodes.JsonValue))]
            [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(System.Text.Json.Nodes.JsonObject))]
            #endif
            get => _extras;

            #if NET6_0_OR_GREATER
            [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(System.Text.Json.Nodes.JsonArray))]
            [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(System.Text.Json.Nodes.JsonValue))]
            [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(System.Text.Json.Nodes.JsonObject))]
            #endif
            set => _extras = value?.DeepClone();
        }

        #endregion

        #region reflection

        protected override string GetSchemaName() => "ExtraProperties";

        IEnumerable<FieldInfo> Reflection.IReflectionObject.GetFields()
        {
            foreach (var name in ReflectFieldsNames())
            {
                if (TryReflectField(name, out var finfo)) yield return finfo;
            }
        }

        bool Reflection.IReflectionObject.TryGetField(string name, out SharpGLTF.Reflection.FieldInfo value)
        {
            return TryReflectField(name ,out value);
        }

        protected virtual IEnumerable<string> ReflectFieldsNames()
        {
            yield return "extensions";
            yield return "extras";

        }
        protected virtual bool TryReflectField(string name, out Reflection.FieldInfo value)
        {
            switch (name)
            {
                case "extensions": value = Reflection.FieldInfo.From("extensions", _extensions, exts => new _ExtensionsReflection(exts)); return true;
                case "extras": value = Reflection.FieldInfo.From("extras", _extras, inst => inst); return true;
                default: value = default; return false;
            }
        }

        /// <summary>
        /// Gets a collection of <see cref="ExtraProperties"/> instances stored by this object.
        /// </summary>
        /// <returns>A collection of <see cref="ExtraProperties"/> instances.</returns>
        /// <remarks>
        /// This is used to traverse the whole glTF document tree and gather all the objects<br/>
        /// So we can identify which extensions are used anywhere in the document.
        /// </remarks>
        protected IEnumerable<ExtraProperties> GetLogicalChildren()
        {
            foreach (var ext in _extensions.OfType<ExtraProperties>())
            {
                yield return ext;
            }

            if (!(this is IReflectionObject robj)) yield break;

            foreach (var field in robj.GetFields())
            {
                var value = field.Value;

                if (value is IReflectionArray array)
                {
                    for (int i = 0; i < array.Count; ++i)
                    {
                        var item = array.GetField(i);
                        if (item.Value is ExtraProperties itemExtra) yield return itemExtra;
                    }
                }
                else if (value is ExtraProperties extra) yield return extra;
            }
        }

        protected static IEnumerable<ExtraProperties> Flatten(ExtraProperties container)
        {
            if (container == null) yield break;

            yield return container;

            foreach (var c in container.GetLogicalChildren())
            {
                var cc = Flatten(c);

                foreach (var ccc in cc) yield return ccc;
            }
        }

        #endregion

        #region API        

        protected static void SetProperty<TParent, TProperty, TValue>(TParent parent, ref TProperty property, TValue value)
            where TParent : ExtraProperties
            where TProperty: class
            where TValue: TProperty
        {
            new Collections.ChildSetter<TParent>(parent).SetProperty(ref property, value);
        }

        protected static Collections.ChildSetter<T> GetChildSetter<T>(T owner) where T:ExtraProperties
        {
            return new Collections.ChildSetter<T>(owner);
        }

        public T GetExtension<T>()
            where T : JsonSerializable
        {
            return _extensions.OfType<T>().FirstOrDefault();
        }

        public T UseExtension<T>()
            where T : JsonSerializable
        {
            var value = GetExtension<T>();
            if (value != null) return value;

            var name = ExtensionsFactory.Identify(this.GetType(), typeof(T));
            Guard.NotNull(name, nameof(T));

            value = ExtensionsFactory.Create(this, name) as T;
            Guard.NotNull(value, nameof(T));

            _extensions.Add(value);

            return value;
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

        #region validation

        protected override void OnValidateReferences(Validation.ValidationContext validate)
        {
            base.OnValidateReferences(validate);

            foreach (var lc in this.GetLogicalChildren())
            {
                lc.ValidateReferences(validate);
            }

            foreach (var ext in this.Extensions) ext.ValidateReferences(validate);

            // if (this._extras.Content is JsonSerializable js) js.ValidateReferences(validate);
        }

        protected override void OnValidateContent(Validation.ValidationContext validate)
        {
            base.OnValidateContent(validate);

            foreach (var lc in this.GetLogicalChildren())
            {
                lc.ValidateContent(validate);
            }

            // if (this._extras.Content is JsonSerializable js) js.ValidateContent(validate);

            // if (this._extras.Content != null) validate.IsJsonSerializable("Extras", this._extras.Content);
        }

        #endregion

        #region serialization API

        /// <summary>
        /// Writes the properties of the current instance to a <see cref="Utf8JsonWriter"/>.
        /// </summary>
        /// <param name="writer">The target writer.</param>
        protected override void SerializeProperties(Utf8JsonWriter writer)
        {
            if (_extensions.Count > 0)
            {
                var dict = _ToDictionary(this, _extensions);
                SerializeProperty(writer, "extensions", dict);
            }

            // todo, only write _extras if it's a known serializable type.
            var content = _extras;
            if (content == null) return;
            // if (!JSONEXTRAS.IsJsonSerializable(content)) return;

            SerializeProperty(writer, "extras", content);
        }

        private static Dictionary<string, JsonSerializable> _ToDictionary(JsonSerializable context, IEnumerable<JsonSerializable> serializables)
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
        /// Reads the properties of the current instance from a <see cref="Utf8JsonReader"/>.
        /// </summary>
        /// <param name="jsonPropertyName">The name of the property.</param>
        /// <param name="reader">The source reader.</param>
        protected override void DeserializeProperty(string jsonPropertyName, ref Utf8JsonReader reader)
        {
            Guard.NotNullOrEmpty(jsonPropertyName, nameof(jsonPropertyName));

            switch (jsonPropertyName)
            {
                case "extensions": _DeserializeExtensions(this, ref reader, _extensions); break;

                case "extras":
                    {
                        var content = JSONEXTRAS.Parse(ref reader);
                        _extras = content;
                        break;
                    }

                default: reader.Skip(); break;
            }
        }

        private static void _DeserializeExtensions(JsonSerializable parent, ref Utf8JsonReader reader, List<JsonSerializable> extensions)
        {
            reader.Read();

            if (reader.TokenType == JSONTOKEN.StartObject)
            {
                while (reader.Read() && reader.TokenType != JSONTOKEN.EndObject)
                {
                    var key = reader.GetString();

                    var val = ExtensionsFactory.Create(parent, key);

                    val ??= new UnknownNode(key);

                    val.Deserialize(ref reader);
                    extensions.Add(val);
                    continue;
                }
            }

            reader.Skip();
        }

        #endregion

        #region nested types

        private readonly struct _ExtensionsReflection : Reflection.IReflectionObject
        {
            public _ExtensionsReflection(IReadOnlyList<JsonSerializable> extensions)
            {
                _Extensions = extensions;
            }

            private readonly IReadOnlyList<JsonSerializable> _Extensions;

            public bool TryGetField(string name, out FieldInfo value)
            {
                var extension = _Extensions.FirstOrDefault(item => item._SchemaName == name);
                if (extension == null)
                {
                    value = default;
                    return false;
                }

                value = Reflection.FieldInfo.From(extension._SchemaName, extension, ext => ext);
                return true;
            }

            public IEnumerable<FieldInfo> GetFields()
            {
                foreach(var extension in _Extensions)
                {
                    yield return Reflection.FieldInfo.From(extension._SchemaName, extension, ext => ext);
                }
            }

            
        }

        #endregion
    }
}