using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

using CULTURE = System.Globalization.CultureInfo;
using JSONELEMENT = System.Text.Json.JsonElement;
using JSONOPTIONS = System.Text.Json.JsonSerializerOptions;
using JSONPROPERTY = System.Collections.Generic.KeyValuePair<string, object>;

namespace SharpGLTF.IO
{
    interface IJsonCollection : ICloneable
    {
        bool IsArray { get; }
        bool IsObject { get; }
        int Count { get; }
    }

    struct _JsonStaticUtils
    {
        public static string ToJson(Object obj, JSONOPTIONS options)
        {
            if (obj == null) return String.Empty;

            if (options == null)
            {
                options = new JSONOPTIONS
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                    IgnoreNullValues = true,
                    WriteIndented = true
                };
            }

            return JsonSerializer.Serialize(obj, obj.GetType(), options);
        }

        /// <summary>
        /// Serializes data trees into trees of <see cref="IConvertible"/>, <see cref="_JsonArray"/> and <see cref="_JsonObject"/>.
        /// </summary>
        /// <param name="value">Any <see cref="IConvertible"/> array, list, or dictionary.</param>
        /// <returns>An <see cref="IConvertible"/>, <see cref="_JsonArray"/> or <see cref="_JsonObject"/>.</returns>
        public static Object Serialize(Object value)
        {
            if (value == null) throw new ArgumentNullException(nameof(value));
            if (value is IConvertible cvalue && IsJsonSerializable(cvalue)) return cvalue;
            if (_JsonObject.TryCreate(value, out _JsonObject dict)) return dict;
            if (_JsonArray.TryCreate(value, out _JsonArray array)) return array;

            throw new ArgumentException($"Can't serialize {value.GetType().Name}", nameof(value));
        }

        public static Object Deserialize(Object obj, Type type, JSONOPTIONS options = null)
        {
            if (options == null)
            {
                options = new JSONOPTIONS
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                    IgnoreNullValues = true,
                    WriteIndented = true
                };
            }

            var json = ToJson(obj, options);

            return JsonSerializer.Deserialize(json, type, options);
        }

        public static Object Deserialize(JSONELEMENT element)
        {
            if (element.ValueKind == JsonValueKind.Null) return null;
            if (element.ValueKind == JsonValueKind.False) return false;
            if (element.ValueKind == JsonValueKind.True) return true;
            if (element.ValueKind == JsonValueKind.String) return element.GetString();
            if (element.ValueKind == JsonValueKind.Number) return element.GetDouble();
            if (element.ValueKind == JsonValueKind.Array) return _JsonArray.CreateFrom(element);
            if (element.ValueKind == JsonValueKind.Object) return _JsonObject.CreateFrom(element);

            throw new NotImplementedException();
        }

        public static Object GetNode(Object current, params IConvertible[] path)
        {
            foreach (var part in path)
            {
                if (part is int index && current is IReadOnlyList<Object> array) { current = array[index]; continue; }
                if (part is string key && current is IReadOnlyDictionary<String, Object> dict) { current = dict[key]; continue; }
                throw new ArgumentException("Invalid path", nameof(path));
            }

            return current;
        }

        public static T GetValue<T>(Object current, params IConvertible[] path)
            where T : IConvertible
        {
            current = GetNode(current, path);

            if (current is IConvertible value)
            {
                return (T)Convert.ChangeType(value, typeof(T), CULTURE.InvariantCulture);
            }

            throw new ArgumentException("Invalid path", nameof(path));
        }

        public static bool IsJsonSerializable(Object value) { return IsJsonSerializable(value, out _); }

        public static bool IsJsonSerializable(Object value, out Object invalidValue)
        {
            invalidValue = null;

            if (value == null) return false;

            if (value is IConvertible cvt) return IsJsonSerializable(cvt);

            if (value is IDictionary dict)
            {
                if (dict.Count == 0) { invalidValue = value; return false; }

                foreach (DictionaryEntry entry in dict)
                {
                    if (!IsJsonSerializable(entry.Value, out invalidValue)) return false;
                }

                return true;
            }

            if (value is IReadOnlyDictionary<string, object> dictXY)
            {
                if (dictXY.Count == 0) { invalidValue = value; return false; }

                foreach (var item in dictXY.Values)
                {
                    if (!IsJsonSerializable(item, out invalidValue)) return false;
                }

                return true;
            }

            if (value is IEnumerable array)
            {
                if (!array.Cast<Object>().Any()) { invalidValue = value; return false; }

                foreach (var item in array)
                {
                    if (!IsJsonSerializable(item, out invalidValue)) return false;
                }

                return true;
            }

            invalidValue = value;

            return false;
        }

        private static bool IsJsonSerializable(IConvertible cvt)
        {
            switch (cvt.GetTypeCode())
            {
                case TypeCode.Empty:
                case TypeCode.DBNull:
                case TypeCode.Object:
                case TypeCode.DateTime:
                    return false;
            }

            if (cvt is Single scvt) return !Single.IsNaN(scvt) && !Single.IsInfinity(scvt);
            if (cvt is Double dcvt) return !Double.IsNaN(dcvt) && !Double.IsInfinity(dcvt);

            return true;
        }

        public static bool AreEqualByContent(Object x, Object y, float precission)
        {
            #pragma warning disable IDE0041 // Use 'is null' check
            if (Object.ReferenceEquals(x, y)) return true;
            if (Object.ReferenceEquals(x, null)) return false;
            if (Object.ReferenceEquals(y, null)) return false;
            #pragma warning restore IDE0041 // Use 'is null' check

            if (x is IConvertible xval && y is IConvertible yval)
            {
                return AreEqual(xval, yval, precission);
            }

            if (x is IReadOnlyList<object> xarr && y is IReadOnlyList<object> yarr)
            {
                if (xarr.Count != yarr.Count) return false;
                int c = xarr.Count;

                for (int i = 0; i < c; ++i)
                {
                    if (!AreEqualByContent(xarr[i], yarr[i], precission)) return false;
                }

                return true;
            }

            if (x is IReadOnlyDictionary<string, object> xdic && y is IReadOnlyDictionary<string, object> ydic)
            {
                if (xdic.Count != ydic.Count) return false;

                foreach (var key in xdic.Keys)
                {
                    if (!xdic.TryGetValue(key, out Object xdval)) return false;
                    if (!ydic.TryGetValue(key, out Object ydval)) return false;

                    if (!AreEqualByContent(xdval, ydval, precission)) return false;
                }

                return true;
            }

            bool isValidType(Object z)
            {
                if (z is IConvertible) return true;
                if (z is IReadOnlyList<Object>) return true;
                if (z is IReadOnlyDictionary<string, object>) return true;
                return false;
            }

            if (!isValidType(x)) throw new ArgumentException($"Invalid type: {x.GetType()}", nameof(x));
            if (!isValidType(y)) throw new ArgumentException($"Invalid type: {y.GetType()}", nameof(y));

            return false;
        }

        private static bool AreEqual(IConvertible x, IConvertible y, float precission)
        {
            var xc = x.GetTypeCode();
            var yc = y.GetTypeCode();

            if (xc == TypeCode.Decimal || yc == TypeCode.Decimal)
            {
                var xf = y.ToDecimal(CULTURE.InvariantCulture);
                var yf = y.ToDecimal(CULTURE.InvariantCulture);
                return Math.Abs((double)(xf - yf)) < precission;
            }

            if (xc == TypeCode.Double || yc == TypeCode.Double)
            {
                var xf = y.ToDouble(CULTURE.InvariantCulture);
                var yf = y.ToDouble(CULTURE.InvariantCulture);
                return Math.Abs(xf - yf) < precission;
            }

            if (xc == TypeCode.Single || yc == TypeCode.Single)
            {
                var xf = y.ToSingle(CULTURE.InvariantCulture);
                var yf = y.ToSingle(CULTURE.InvariantCulture);
                return Math.Abs(xf - yf) < precission;
            }

            if (xc == yc) return Object.Equals(x, y);

            return false;
        }

        /// <summary>
        /// Calculates the hash of a json DOM structure, without taking values into account
        /// </summary>
        /// <param name="x">the input json DOM structure</param>
        /// <returns>A hash code</returns>
        /// <remarks>
        /// Theory says that two objects that are considered equal must have the same hash.
        /// This means that we cannot use the hashes of the values because two equivalent
        /// values (int 5) and (float 5.0f)  might have different hashes.
        /// </remarks>
        public static int GetStructureHashCode(Object x)
        {
            if (x == null) return 0;

            if (x is IConvertible xval) { return 1; }

            if (x is IReadOnlyList<object> xarr)
            {
                int h = xarr.Count;

                for (int i = 0; i < xarr.Count; ++i)
                {
                    h ^= GetStructureHashCode(xarr[i]);
                    h *= 17;
                }

                return h;
            }

            if (x is IReadOnlyDictionary<string, object> xdic)
            {
                int h = xdic.Count;

                foreach (var kvp in xdic.OrderBy(item => item.Key))
                {
                    h ^= kvp.Key.GetHashCode();
                    h ^= GetStructureHashCode(kvp.Value);
                    h *= 17;
                }

                return h;
            }

            throw new ArgumentException($"Invalid type: {x.GetType()}", nameof(x));
        }
    }

    /// <summary>
    /// Represents an inmutable Json Array.
    /// </summary>
    readonly struct _JsonArray : IReadOnlyList<object>, IJsonCollection, IList
    {
        #region constructor

        public static bool TryCreate(Object value, out _JsonArray obj)
        {
            if (value is IConvertible) { obj = default; return false; }
            if (value is IDictionary) { obj = default; return false; }
            if (value is IEnumerable collection) { obj = _From(collection); return true; }

            obj = default;
            return false;
        }

        public static _JsonArray CreateFrom(JSONELEMENT array)
        {
            if (array.ValueKind != JsonValueKind.Array) throw new ArgumentException("Must be JsonValueKind.Array", nameof(array));

            Object convert(JsonElement element)
            {
                return _JsonStaticUtils.Deserialize(element);
            }

            using (var entries = array.EnumerateArray())
            {
                return _From(entries.Select(convert));
            }
        }

        private static _JsonArray _From(IEnumerable collection)
        {
            return new _JsonArray(TryClone(collection));
        }

        private _JsonArray(Array array)
        {
            _Array = array;
        }

        public object Clone() { return _From(this); }

        private static Array TryClone(IEnumerable collection)
        {
            // 1st pass: determine element type and collection size

            Type elementType = null;

            int count = 0;

            foreach (var item in collection.Cast<Object>())
            {
                if (item == null) throw new ArgumentException($"{nameof(collection)}[{count}] is null", nameof(collection));

                ++count;

                if (elementType == null)
                {
                    if (item is IConvertible) elementType = typeof(IConvertible);
                    else elementType = item.GetType();
                    continue;
                }

                // subsequent types must match elementType.
                if (!elementType.IsAssignableFrom(item.GetType()))
                {
                    throw new ArgumentException($"{nameof(collection)}[{count}] is invalid type.", nameof(collection));
                }
            }

            if (count == 0) { return Array.Empty<Object>(); }

            if (elementType.IsGenericType && elementType.GetGenericTypeDefinition() == typeof(IReadOnlyDictionary<,>))
            {
                elementType = typeof(IDictionary);
            }

            int contentType = 0;
            if (elementType == typeof(IConvertible)) contentType = 1;
            if (contentType == 0 && typeof(IDictionary).IsAssignableFrom(elementType)) contentType = 3;
            if (contentType == 0 && typeof(IEnumerable).IsAssignableFrom(elementType)) contentType = 2;

            Array container = null;

            switch (contentType)
            {
                case 1: container = Array.CreateInstance(typeof(IConvertible), count); break;
                case 2: container = Array.CreateInstance(typeof(_JsonArray), count); break;
                case 3: container = Array.CreateInstance(typeof(_JsonObject), count); break;
                default: throw new NotImplementedException();
            }

            // 2nd pass: convert and assign items.

            int idx = 0;
            foreach (var item in collection) container.SetValue(_JsonStaticUtils.Serialize(item), idx++);

            return container;
        }

        #endregion

        #region data

        private readonly Array _Array;

        #endregion

        #region properties

        public Object this[int index] => _Array.GetValue(index);

        object IList.this[int index]
        {
            get => _Array.GetValue(index);
            set => throw new NotSupportedException();
        }

        public bool IsArray => true;
        public bool IsObject => false;
        public int Count => _Array.Length;
        bool IList.IsFixedSize => true;
        bool IList.IsReadOnly => true;
        bool ICollection.IsSynchronized => false;
        object ICollection.SyncRoot => null;

        #endregion

        #region API
        public IEnumerator<Object> GetEnumerator() { return _Array.Cast<object>().GetEnumerator(); }
        IEnumerator IEnumerable.GetEnumerator() { return _Array.GetEnumerator(); }
        public bool Contains(object value) { return IndexOf(value) >= 0; }
        public int IndexOf(object value) { return Array.IndexOf(_Array, value); }
        public void CopyTo(Array array, int index) { _Array.CopyTo(array, index); }
        void IList.Clear() { throw new NotSupportedException(); }
        void IList.Insert(int index, object value) { throw new NotSupportedException(); }
        void IList.Remove(object value) { throw new NotSupportedException(); }
        void IList.RemoveAt(int index) { throw new NotSupportedException(); }
        int IList.Add(object value) { throw new NotSupportedException(); }

        #endregion
    }

    /// <summary>
    /// Represents an inmutable Json Object.
    /// </summary>
    /// <remarks>
    /// Supported by converter <see href="https://github.com/dotnet/runtime/blob/76904319b41a1dd0823daaaaae6e56769ed19ed3/src/libraries/System.Text.Json/src/System/Text/Json/Serialization/Converters/Collection/IReadOnlyDictionaryOfTKeyTValueConverter.cs"/>
    /// </remarks>
    readonly struct _JsonObject : IReadOnlyDictionary<string, object>, IDictionary, IJsonCollection
    {
        #region constructor

        public static bool TryCreate(Object value, out _JsonObject obj)
        {
            if (value is IConvertible _) { obj = default; return false; }
            if (value is IDictionary dict0) { obj = new _JsonObject(_Enumerate(dict0)); return true; }
            if (value is IReadOnlyDictionary<string, Object> dict1) { obj = new _JsonObject(_Enumerate(dict1)); return true; }

            obj = default;
            return false;
        }

        public static _JsonObject CreateFrom(JSONELEMENT dict)
        {
            if (dict.ValueKind != JsonValueKind.Object) throw new ArgumentException("Must be JsonValueKind.Object", nameof(dict));

            JSONPROPERTY convert(JsonProperty property)
            {
                var value = _JsonStaticUtils.Deserialize(property.Value);
                return new JSONPROPERTY(property.Name, value);
            }

            using (var entries = dict.EnumerateObject())
            {
                return new _JsonObject(dict.EnumerateObject().Select(convert));
            }
        }

        private static IEnumerable<JSONPROPERTY> _Enumerate(IDictionary dict)
        {
            foreach (DictionaryEntry entry in dict)
            {
                if (entry.Value == null) continue;
                yield return new JSONPROPERTY(_GetKey(entry.Key), _GetValue(entry.Value));
            }
        }

        private static IEnumerable<JSONPROPERTY> _Enumerate<TKey, TValue>(IEnumerable<KeyValuePair<TKey, TValue>> dict)
        {
            foreach (var entry in dict)
            {
                if (entry.Value == null) continue;
                yield return new JSONPROPERTY(_GetKey(entry.Key), _GetValue(entry.Value));
            }
        }

        private static string _GetKey(Object key)
        {
            if (key is string skey) return skey;
            if (key is IConvertible ckey) return ckey.ToString(CULTURE.InvariantCulture);
            return key.ToString();
        }

        private static Object _GetValue(Object value)
        {
            return _JsonStaticUtils.Serialize(value);
        }

        public object Clone()
        {
            return new _JsonObject(_Dictionary);
        }

        private _JsonObject(IEnumerable<JSONPROPERTY> items)
        {
            bool _filterEmptyCollections(JSONPROPERTY p)
            {
                if (p.Value is IConvertible) return true;
                if (p.Value is IJsonCollection c) return c.Count > 0;
                throw new ArgumentException($"{p.GetType().Name} not supported.", nameof(items));
            }

            items = items.Where(_filterEmptyCollections);

            _Dictionary = new Dictionary<string, object>();

            foreach (var item in items) _Dictionary.Add(item.Key, item.Value);
        }

        #endregion

        #region data

        private readonly Dictionary<String, Object> _Dictionary;

        #endregion

        #region properties
        public object this[string key] => _Dictionary[key];
        public IEnumerable<string> Keys => _Dictionary.Keys;
        ICollection IDictionary.Keys => _Dictionary.Keys;
        public IEnumerable<object> Values => _Dictionary.Values;
        ICollection IDictionary.Values => _Dictionary.Values;
        public int Count => _Dictionary.Count;
        public bool IsArray => false;
        public bool IsObject => true;
        public bool IsFixedSize => true;
        public bool IsReadOnly => true;
        public bool IsSynchronized => false;
        public object SyncRoot => null;
        public object this[object key]
        {
            get => this[(string)key];
            set => throw new NotSupportedException();
        }

        #endregion

        #region API

        bool IDictionary.Contains(object key) { return ContainsKey((string)key); }
        public bool ContainsKey(string key) { return _Dictionary.ContainsKey(key); }
        public bool TryGetValue(string key, out object value) { return _Dictionary.TryGetValue(key, out value); }
        IEnumerator IEnumerable.GetEnumerator() { return _Dictionary.GetEnumerator(); }
        public IEnumerator<JSONPROPERTY> GetEnumerator() { return _Dictionary.AsEnumerable().GetEnumerator(); }
        IDictionaryEnumerator IDictionary.GetEnumerator() { return ((IDictionary)_Dictionary).GetEnumerator(); }
        void IDictionary.Add(object key, object value) { throw new NotSupportedException(); }
        void IDictionary.Clear() { throw new NotSupportedException(); }
        void IDictionary.Remove(object key) { throw new NotSupportedException(); }
        void ICollection.CopyTo(Array array, int index) { throw new NotImplementedException(); }

        #endregion
    }
}
