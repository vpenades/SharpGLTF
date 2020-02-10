using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SharpGLTF.Collections
{
    /// <summary>
    /// Wraps a standard dictionary, but performs a transform in the value
    /// </summary>
    /// <typeparam name="TKey">The dictionary key type.</typeparam>
    /// <typeparam name="TValueIn">The internal value type.</typeparam>
    /// <typeparam name="TValueOut">The exposed value type.</typeparam>
    readonly struct ReadOnlyLinqDictionary<TKey, TValueIn, TValueOut> : IReadOnlyDictionary<TKey, TValueOut>
    {
        #region lifecycle

        public ReadOnlyLinqDictionary(IReadOnlyDictionary<TKey, TValueIn> dict, Converter<TValueIn, TValueOut> valConverter)
        {
            _Source = dict;
            _ValueConverter = valConverter;
        }

        #endregion

        #region data

        private readonly IReadOnlyDictionary<TKey, TValueIn> _Source;
        private readonly Converter<TValueIn, TValueOut> _ValueConverter;

        #endregion

        #region API

        public TValueOut this[TKey key] => _ValueConverter(_Source[key]);

        public IEnumerable<TKey> Keys => _Source.Keys;

        public IEnumerable<TValueOut> Values
        {
            get
            {
                var cvt = _ValueConverter;
                return _Source.Values.Select(item => cvt(item));
            }
        }

        public int Count => _Source.Count;

        public bool ContainsKey(TKey key) { return _Source.ContainsKey(key); }

        public bool TryGetValue(TKey key, out TValueOut value)
        {
            if (!_Source.TryGetValue(key, out TValueIn val))
            {
                value = default;
                return false;
            }

            value = _ValueConverter(val);
            return true;
        }

        public IEnumerator<KeyValuePair<TKey, TValueOut>> GetEnumerator()
        {
            var cvt = _ValueConverter;
            return _Source
                .Select(item => new KeyValuePair<TKey, TValueOut>(item.Key, cvt(item.Value)))
                .GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            var cvt = _ValueConverter;
            return _Source
                .Select(item => new KeyValuePair<TKey, TValueOut>(item.Key, cvt(item.Value)))
                .GetEnumerator();
        }

        #endregion
    }

    readonly struct LinqDictionary<TKey, TValueIn, TValueOut> : IDictionary<TKey, TValueOut>
    {
        #region lifecycle

        public LinqDictionary(IDictionary<TKey, TValueIn> dict, Converter<TValueOut, TValueIn> inConverter, Converter<TValueIn, TValueOut> outConverter)
        {
            _Source = dict;
            _InConverter = inConverter;
            _OutConverter = outConverter;
        }

        #endregion

        #region data

        private readonly IDictionary<TKey, TValueIn> _Source;
        private readonly Converter<TValueOut, TValueIn> _InConverter;
        private readonly Converter<TValueIn, TValueOut> _OutConverter;

        #endregion

        #region API

        public TValueOut this[TKey key]
        {
            get => _OutConverter(_Source[key]);
            set => _Source[key] = _InConverter(value);
        }

        public ICollection<TKey> Keys => _Source.Keys;

        public ICollection<TValueOut> Values
        {
            get
            {
                var cvt = _OutConverter;
                return _Source.Values.Select(item => cvt(item)).ToList();
            }
        }

        public int Count => _Source.Count;

        public bool IsReadOnly => throw new NotImplementedException();

        public bool ContainsKey(TKey key) { return _Source.ContainsKey(key); }

        public bool TryGetValue(TKey key, out TValueOut value)
        {
            if (!_Source.TryGetValue(key, out TValueIn val))
            {
                value = default;
                return false;
            }

            value = _OutConverter(val);
            return true;
        }

        public IEnumerator<KeyValuePair<TKey, TValueOut>> GetEnumerator()
        {
            var cvt = _OutConverter;
            return _Source
                .Select(item => new KeyValuePair<TKey, TValueOut>(item.Key, cvt(item.Value)))
                .GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            var cvt = _OutConverter;
            return _Source
                .Select(item => new KeyValuePair<TKey, TValueOut>(item.Key, cvt(item.Value)))
                .GetEnumerator();
        }

        public void Add(TKey key, TValueOut value)
        {
            this[key] = value;
        }

        public bool Remove(TKey key)
        {
            return _Source.Remove(key);
        }

        public void Add(KeyValuePair<TKey, TValueOut> item)
        {
            this[item.Key] = item.Value;
        }

        public void Clear()
        {
            _Source.Clear();
        }

        public bool Contains(KeyValuePair<TKey, TValueOut> item)
        {
            throw new NotImplementedException();
        }

        public void CopyTo(KeyValuePair<TKey, TValueOut>[] array, int arrayIndex)
        {
            throw new NotImplementedException();
        }

        public bool Remove(KeyValuePair<TKey, TValueOut> item)
        {
            throw new NotImplementedException();
        }

        #endregion
    }
}
