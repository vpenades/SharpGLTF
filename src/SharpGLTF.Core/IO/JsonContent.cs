using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

using JSONOPTIONS = System.Text.Json.JsonSerializerOptions;

namespace SharpGLTF.IO
{
    /// <summary>
    /// Represents an inmutable json object stored in memory.
    /// </summary>
    /// <remarks>
    /// Valid values can be:
    /// - <see cref="IConvertible"/> for literal values.
    /// - <see cref="IReadOnlyList{Object}"/> for arrays.
    /// - <see cref="IReadOnlyDictionary{String, Object}"/> for objects.
    /// </remarks>
    [System.ComponentModel.ImmutableObject(true)]
    public readonly struct JsonContent
    {
        #region constructors

        public static implicit operator JsonContent(Boolean value) { return new JsonContent(value); }

        public static implicit operator JsonContent(String value) { return new JsonContent(value); }

        public static implicit operator JsonContent(Int32 value) { return new JsonContent(value); }

        public static implicit operator JsonContent(Int64 value) { return new JsonContent(value); }

        public static implicit operator JsonContent(Single value) { return new JsonContent(value); }

        public static implicit operator JsonContent(Double value) { return new JsonContent(value); }

        public static implicit operator JsonContent(Object[] value) { return new JsonContent(value); }

        public static implicit operator JsonContent(List<Object> value) { return new JsonContent(value); }

        public static implicit operator JsonContent(Dictionary<String, Object> value) { return new JsonContent(value); }

        public static JsonContent CreateFrom(IConvertible value) { return new JsonContent(value); }
        public static JsonContent CreateFrom(IList value) { return new JsonContent(value); }
        public static JsonContent CreateFrom(IDictionary value) { return new JsonContent(value); }

        internal static JsonContent _Wrap(Object value) { return new JsonContent(value); }

        public JsonContent DeepClone() { return new JsonContent(_Content);  }

        private JsonContent(Object value)
        {
            _Content = value == null ? null : _JsonStaticUtils.Serialize(value);
            if (_Content is IJsonCollection collection && collection.Count == 0)
                _Content = null;
        }

        #endregion

        #region data

        /// <summary>
        /// The dynamic json structure, where it can be any of this:
        /// - A <see cref="IConvertible"/> object.
        /// - A non empty <see cref="IReadOnlyList{Object}"/> object.
        /// - A non empty <see cref="IReadOnlyDictionary{String, Object}"/> object.
        /// </summary>
        [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)]
        private readonly Object _Content;

        // It is tempting to add Equality support, but it's problematic because these reasons:
        // - It's not clear how to compare in-memory floating point values against deserialized string values.
        // - Serialization roundtrip is not well supported in older NetFramework versions; this is specially
        // apparent when using System.Text.JSon in NetCore and Net471, where NetCore is roundtrip safe, and
        // NetFramework is not.

        #endregion

        #region properties

        [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Collapsed)]
        public Object Content => _Content;

        #endregion

        #region serialization

        /// <summary>
        /// Converts the value of a specified type into a <see cref="JsonContent"/> using <see cref="JsonSerializer"/>.
        /// </summary>
        /// <param name="value">The value to convert.</param>
        /// <param name="options">Options to control the conversion behavior.</param>
        /// <returns>A <see cref="JsonContent"/> object.</returns>
        public static JsonContent Serialize(Object value, JSONOPTIONS options = null)
        {
            if (value == null) return default;

            if (options == null)
            {
                options = new JSONOPTIONS
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                    IgnoreNullValues = true,
                    WriteIndented = true
                };
            }

            var json = JsonSerializer.Serialize(value, value.GetType(), options);

            return Parse(json);
        }

        /// <summary>
        /// Parses a json text an converts it to a <see cref="JsonContent"/>
        /// </summary>
        /// <param name="jsonContent">The json text content.</param>
        /// <param name="options">Parser options.</param>
        /// <returns>A <see cref="JsonContent"/> object</returns>
        public static JsonContent Parse(string jsonContent, JsonDocumentOptions options = default)
        {
            using (var doc = JsonDocument.Parse(jsonContent, options))
            {
                return Parse(doc);
            }
        }

        public static JsonContent Parse(JsonDocument root)
        {
            return root == null ? default : new JsonContent(_JsonStaticUtils.Deserialize(root.RootElement));
        }

        public string ToJson(JSONOPTIONS options = null)
        {
            return _JsonStaticUtils.ToJson(_Content, options);
        }

        public Object Deserialize(Type type, JSONOPTIONS options = null)
        {
            return _JsonStaticUtils.Deserialize(_Content, type, options);
        }

        #endregion

        #region static API
        public static bool IsJsonSerializable(Object value) { return IsJsonSerializable(value, out _); }

        public static bool IsJsonSerializable(Object value, out Object invalidValue)
        {
            return _JsonStaticUtils.IsJsonSerializable(value, out invalidValue);
        }

        #endregion

        #region API

        public JsonContent GetNode(params IConvertible[] path)
        {
            var value = _JsonStaticUtils.GetNode(this._Content, path);
            return new JsonContent(value);
        }

        public T GetValue<T>(params IConvertible[] path)
            where T : IConvertible
        {
            return _JsonStaticUtils.GetValue<T>(this._Content, path);
        }

        #endregion
    }
}
