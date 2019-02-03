using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace glTF2Sharp.Collections
{
    /// <summary>
    /// Wraps a standard dictionary, but performs a transform in the value
    /// </summary>
    /// <typeparam name="TKey"></typeparam>
    /// <typeparam name="TValueIn"></typeparam>
    /// <typeparam name="TValueOut"></typeparam>
    struct LinqDictionary<TKey, TValueIn, TValueOut> : IReadOnlyDictionary<TKey, TValueOut>
    {
        #region lifecycle

        public LinqDictionary(IReadOnlyDictionary<TKey,TValueIn> dict, Func<TValueIn,TValueOut> valConverter)
        {
            _Source = dict;
            _ValueConverter = valConverter;
        }

        #endregion

        #region data

        private readonly IReadOnlyDictionary<TKey, TValueIn> _Source;
        private readonly Func<TValueIn, TValueOut> _ValueConverter;

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
}
