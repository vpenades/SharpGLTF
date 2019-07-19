using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SharpGLTF.Collections
{
    /// <summary>
    /// Represents an empty, read-only dictionary to use as a safe replacement of NULL.
    /// </summary>
    /// <typeparam name="TKey">The type of keys in the read-only dictionary.</typeparam>
    /// <typeparam name="TValue">The type of values in the read-only dictionary.</typeparam>
    sealed class EmptyDictionary<TKey, TValue> : IReadOnlyDictionary<TKey, TValue>
    {
        #region lifecycle

        static EmptyDictionary() { }

        private EmptyDictionary() { }

        private static readonly EmptyDictionary<TKey, TValue> _Instance = new EmptyDictionary<TKey, TValue>();

        public static IReadOnlyDictionary<TKey, TValue> Instance => _Instance;

        #endregion

        #region API

        public TValue this[TKey key] => throw new KeyNotFoundException();

        public IEnumerable<TKey> Keys => Enumerable.Empty<TKey>();

        public IEnumerable<TValue> Values => Enumerable.Empty<TValue>();

        public int Count => 0;

        public bool ContainsKey(TKey key) { return false; }

        public bool TryGetValue(TKey key, out TValue value) { value = default; return false; }

        public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator() { yield break; }

        IEnumerator IEnumerable.GetEnumerator() { yield break; }

        #endregion
    }
}
