using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

using FIELDINFO = SharpGLTF.Reflection.FieldInfo;

namespace SharpGLTF.Collections
{
    /// <summary>
    /// An Specialisation of <see cref="Dictionary{TKey, TValue}"/>, which interconnects the dictionary items with the parent of the collection.
    /// </summary>    
    [System.Diagnostics.DebuggerDisplay("{Count}")]
    public sealed class ChildrenDictionary<T, TParent>
        : IReadOnlyDictionary<string, T>
        , IDictionary<string, T>
        , Reflection.IReflectionObject
        where T : class, IChildOfDictionary<TParent>
        where TParent : class
    {
        #region lifecycle

        public ChildrenDictionary(TParent parent)
        {
            Guard.NotNull(parent, nameof(parent));
            _Parent = parent;
        }

        #endregion

        #region data

        [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)]
        private readonly TParent _Parent;

        [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.RootHidden)]
        private Dictionary<string, T> _Collection;

        #endregion

        #region Properties

        IEnumerable<string> IReadOnlyDictionary<string, T>.Keys => this.Keys;
        public ICollection<string> Keys => _Collection == null ? Array.Empty<string>() : (ICollection<string>)_Collection.Keys;

        IEnumerable<T> IReadOnlyDictionary<string, T>.Values => this.Values;
        public ICollection<T> Values => _Collection == null ? Array.Empty<T>() : (ICollection<T>)_Collection.Values;        

        public int Count => _Collection == null ? 0 : _Collection.Count;        

        public bool IsReadOnly => false;

        #endregion

        #region API

        public T this[string key]
        {
            get => TryGetValue(key, out var value) ? value : throw new KeyNotFoundException();
            set => Add(key, value);
        }

        public void Clear()
        {
            if (_Collection == null) return;

            foreach (var item in _Collection) // orphan all children
            {
                item.Value.SetLogicalParent(null, null);
                _AssertItem(item.Value, null);
            }

            _Collection = null;
        }
        
        public void Add(string key, T value)
        {
            _VerifyIsOrphan(value);

            _Collection ??= new Dictionary<string, T>();

            Remove(key);

            if (value == null) return;

            value.SetLogicalParent(_Parent, key);
            _AssertItem(value, key);

            _Collection[key] = value;            
        }

        public bool Remove(string key)
        {
            if (_Collection == null) return false;

            if (!_Collection.TryGetValue(key, out var oldValue)) return false;            

            // orphan the current child
            oldValue?.SetLogicalParent(null, null);

            var r = _Collection.Remove(key);

            if (_Collection.Count == 0) _Collection = null;

            return r;
        }        

        public bool ContainsKey(string key)
        {
            if (_Collection == null) return false;
            return _Collection.ContainsKey(key);
        }

        public bool TryGetValue(string key, out T value)
        {
            if (_Collection == null) { value = default; return false; }
            return _Collection.TryGetValue(key, out value);
        }

        public IEnumerator<KeyValuePair<string, T>> GetEnumerator()
        {
            var c = _Collection ?? Enumerable.Empty<KeyValuePair<string, T>>();
            return c.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            var c = _Collection ?? Enumerable.Empty<KeyValuePair<string, T>>();
            return c.GetEnumerator();
        }

        private static void _VerifyIsOrphan(T item)
        {
            Guard.NotNull(item, nameof(item));
            Guard.MustBeNull(item.LogicalParent, nameof(item.LogicalParent));
            Guard.MustBeNull(item.LogicalKey, nameof(item.LogicalKey));
        }

        [Conditional("DEBUG")]
        private void _AssertItem(T item, string key)
        {
            System.Diagnostics.Debug.Assert(item.LogicalKey == key);

            var parent = key != null ? _Parent : null;

            System.Diagnostics.Debug.Assert(item.LogicalParent == parent);
        }        

        public void Add(KeyValuePair<string, T> item)
        {
            Add(item.Key, item.Value);
        }

        public bool Contains(KeyValuePair<string, T> item)
        {
            return ContainsKey(item.Key);
        }

        public bool Remove(KeyValuePair<string, T> item)
        {
            return Remove(item.Key);
        }

        public void CopyTo(KeyValuePair<string, T>[] array, int arrayIndex)
        {
            if (_Collection == null) return;
            foreach(var kvp in _Collection)
            {
                array[arrayIndex++] = kvp;
            }
        }

        #endregion

        #region API . Reflection

        public IEnumerable<FIELDINFO> GetFields()
        {
            return this.Select(kvp => FIELDINFO.From(kvp.Key, this, dict => dict[kvp.Key]));
        }

        public bool TryGetField(string name, out FIELDINFO value)
        {
            if (this.TryGetValue(name, out var val))
            {
                value = FIELDINFO.From(name, this, dict => dict[name]);
                return true;
            }

            value = default;
            return false;
        }

        #endregion
    }
}
