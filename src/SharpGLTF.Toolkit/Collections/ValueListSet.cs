using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;

namespace SharpGLTF.Collections
{
    /// <summary>
    /// Represents A specialised list that requires all elements to be unique.
    /// </summary>
    /// <remarks>
    /// - This collection is based on <see cref="Dictionary{TKey, TValue}"/>
    /// - Replaces <see cref="VertexList{T}"/>
    /// - Designed to work with lists of vertices.
    ///
    /// This collection is:
    /// - like a HashSet, in the sense that every element must be unique.
    /// - like a list, because elements can be accessed by index: <see cref="this[int]"/>.
    /// - <see cref="IndexOf(in T)"/> and <see cref="Use(in T)"/> are fast as in a HashSet.
    /// </remarks>
    /// <typeparam name="T">Any value type.</typeparam>
    class ValueListSet<T> : IReadOnlyList<T>
        where T : struct
    {
        #region constructors

        public ValueListSet()
            : this(0, null) { }

        public ValueListSet(int capacity, IEqualityComparer<T> comparer = null)
        {
            if (capacity < 0) throw new ArgumentOutOfRangeException(nameof(capacity));
            if (capacity > 0) _Initialize(capacity);
            _Comparer = comparer ?? EqualityComparer<T>.Default;
        }

        #endregion

        #region data

        [DebuggerDisplay("Hash:{HashCode} Next:{Next} Value:{Value}")]
        private struct _Entry
        {
            public int HashCode;    // Lower 31 bits of hash code, -1 if unused
            public int Next;        // Index of next entry, -1 if last
            public T Value;         // Value of entry
        }

        private IEqualityComparer<T> _Comparer;

        private _Entry[] _Entries;
        private int[] _Buckets;     // indices to the last entry with the given hash.
        private int _Count;         // actual number of elements of the collection.

        private int _Version;

        #endregion

        #region properties

        public IEqualityComparer<T> Comparer => _Comparer;

        public int Count => _Count;

        public T this[int index]
        {
            get
            {
                if (index < 0 || index >= _Count) throw new ArgumentOutOfRangeException(nameof(index));
                if (_Entries[index].HashCode == -1) throw new ArgumentException(nameof(index));
                return _Entries[index].Value;
            }
        }

        public IEnumerable<int> Indices => new _IndexCollection(this);

        #endregion

        #region API

        public void Clear()
        {
            if (_Count <= 0) return;

            _Entries.AsSpan().Fill(default);
            _Buckets.AsSpan().Fill(-1);

            _Count = 0;

            _Version++;
        }

        public bool Exists(int index)
        {
            if (index < 0 || index >= _Count) return false;
            if (_Entries[index].HashCode == -1) return false;
            return true;
        }

        public int IndexOf(in T value)
        {
            return _Buckets == null ? -1 : _IndexOf(value);
        }

        public int Use(in T value)
        {
            var idx = _Buckets == null ? -1 : _IndexOf(value);
            if (idx >= 0) return idx;
            return _Insert(value);
        }

        public int Add(in T value)
        {
            if (_IndexOf(value) >= 0) throw new ArgumentException("${value} already exists", nameof(value));
            return _Insert(value);
        }

        public bool Contains(in T item) { return IndexOf(item) >= 0; }

        public void CopyTo(T[] array, int arrayIndex)
        {
            for (int i = 0; i < _Count; ++i)
            {
                var entry = _Entries[i];
                if (entry.HashCode != -1) array[arrayIndex++] = entry.Value;
            }
        }

        public void CopyTo(ValueListSet<T> dst)
        {
            if (_Count == 0) { dst.Clear(); return; }

            if (dst._Buckets == null || dst._Buckets.Length < this._Buckets.Length) dst._Buckets = new int[this._Buckets.Length];
            if (dst._Entries == null || dst._Entries.Length < this._Entries.Length) dst._Entries = new _Entry[this._Entries.Length];

            dst._Count = this._Count;

            this._Entries.AsSpan(0, _Count).CopyTo(dst._Entries);

            if (this._Comparer == dst._Comparer)
            {
                this._Buckets.AsSpan(0).CopyTo(dst._Buckets);
            }
            else
            {
                // if comparer is different, we must rebuild hashes and buckets.
                dst._Resize(dst._Count, true);
            }

            dst._Version++;
        }

        public IEnumerator<T> GetEnumerator() { return new _ValueEnumerator(this); }

        IEnumerator IEnumerable.GetEnumerator() { return new _ValueEnumerator(this); }

        public void ApplyTransform(Func<T, T> transformFunc)
        {
            for (int i = 0; i < _Count; ++i)
            {
                _Entries[i].Value = transformFunc(_Entries[i].Value);
            }

            // reconstruct hashes.
            _Resize(_Count, true);
        }

        #endregion

        #region core

        private void _Initialize(int capacity)
        {
            int size = _PrimeNumberHelpers.GetPrime(capacity);

            _Buckets = new int[size];
            _Buckets.AsSpan().Fill(-1);
            _Entries = new _Entry[size];
            _Count = 0;
        }

        private int _IndexOf(in T value)
        {
            System.Diagnostics.Debug.Assert(_Buckets != null);

            int hashCode = _Comparer.GetHashCode(value) & 0x7FFFFFFF;
            int bucket = hashCode % _Buckets.Length;

            for (int i = _Buckets[bucket]; i >= 0; i = _Entries[i].Next)
            {
                if (_Entries[i].HashCode == hashCode && _Comparer.Equals(_Entries[i].Value, value)) return i;
            }

            return -1;
        }

        private int _Insert(in T value)
        {
            if (_Buckets == null) _Initialize(0);
            if (_Count == _Entries.Length) _Grow();

            int hashCode = _Comparer.GetHashCode(value) & 0x7FFFFFFF;
            int targetBucket = hashCode % _Buckets.Length;

            int index = _Count;
            _Count++;

            _Entries[index].HashCode = hashCode;
            _Entries[index].Next = _Buckets[targetBucket];
            _Entries[index].Value = value;
            _Buckets[targetBucket] = index;
            _Version++;

            System.Diagnostics.Debug.Assert(_Entries[index].Next < index);

            return index;
        }

        private void _Grow()
        {
            int newCount = _PrimeNumberHelpers.ExpandPrime(_Count);
            System.Diagnostics.Debug.Assert(newCount > _Count);
            _Resize(newCount, false);
        }

        private void _Resize(int newSize, bool forceNewHashCodes)
        {
            if (newSize < _Entries.Length) newSize = _Entries.Length;

            Array.Resize(ref _Entries, newSize);

            if (forceNewHashCodes)
            {
                for (int index = 0; index < _Count; index++)
                {
                    if (_Entries[index].HashCode == -1) continue;

                    _Entries[index].HashCode = _Comparer.GetHashCode(_Entries[index].Value) & 0x7FFFFFFF;
                }
            }

            // reconstruct buckets & linked chain

            if (_Buckets.Length != _Entries.Length) _Buckets = new int[_Entries.Length];
            _Buckets.AsSpan().Fill(-1);

            for (int index = 0; index < _Count; index++)
            {
                if (_Entries[index].HashCode < 0) continue;

                int bucket = _Entries[index].HashCode % _Buckets.Length;
                _Entries[index].Next = _Buckets[bucket];
                _Buckets[bucket] = index;

                System.Diagnostics.Debug.Assert(_Entries[index].Next < index);
            }
        }

        #endregion

        #region nested types

        struct _ValueEnumerator : IEnumerator<T>
        {
            #region lifecycle

            internal _ValueEnumerator(ValueListSet<T> source)
            {
                _Source = source;
                _Version = source._Version;
                _Index = 0;
                _Current = default;
            }

            public void Dispose() { }

            #endregion

            #region data

            private readonly ValueListSet<T> _Source;
            private readonly int _Version;
            private int _Index;
            private T _Current;

            #endregion

            #region properties

            public T Current => _Current;
            object IEnumerator.Current => _Current;

            #endregion

            #region API

            public bool MoveNext()
            {
                if (_Version != _Source._Version) throw new InvalidOperationException("collection changed");

                // Use unsigned comparison since we set index to source.count+1 when the enumeration ends.
                // dictionary.count+1 could be negative if dictionary.count is Int32.MaxValue
                while ((uint)_Index < (uint)_Source._Count)
                {
                    if (_Source._Entries[_Index].HashCode >= 0)
                    {
                        _Current = _Source._Entries[_Index].Value;
                        _Index++;
                        return true;
                    }

                    _Index++;
                }

                _Index = _Source._Count + 1;
                _Current = default;
                return false;
            }

            void IEnumerator.Reset()
            {
                if (_Version != _Source._Version) throw new InvalidOperationException("collection changed");

                _Index = 0;
                _Current = default;
            }

            #endregion
        }

        readonly struct _IndexCollection : IEnumerable<int>
        {
            public _IndexCollection(ValueListSet<T> source) { _Source = source; }

            private readonly ValueListSet<T> _Source;
            public IEnumerator<int> GetEnumerator() { return new _IndexEnumerator(_Source); }
            IEnumerator IEnumerable.GetEnumerator() { return new _IndexEnumerator(_Source); }
        }

        struct _IndexEnumerator : IEnumerator<int>
        {
            #region lifecycle

            internal _IndexEnumerator(ValueListSet<T> source)
            {
                _Source = source;
                _Version = source._Version;
                _Index = 0;
                _Current = -1;
            }

            public void Dispose() { }

            #endregion

            #region data

            private readonly ValueListSet<T> _Source;
            private readonly int _Version;
            private int _Index;
            private int _Current;

            #endregion

            #region properties

            public int Current => _Current;
            object IEnumerator.Current => _Current;

            #endregion

            #region API

            public bool MoveNext()
            {
                if (_Version != _Source._Version) throw new InvalidOperationException("collection changed");

                // Use unsigned comparison since we set index to source.count+1 when the enumeration ends.
                // dictionary.count+1 could be negative if dictionary.count is Int32.MaxValue
                while ((uint)_Index < (uint)_Source._Count)
                {
                    if (_Source._Entries[_Index].HashCode >= 0)
                    {
                        _Current = _Index;
                        _Index++;
                        return true;
                    }

                    _Index++;
                }

                _Index = _Source._Count + 1;
                _Current = default;
                return false;
            }

            void IEnumerator.Reset()
            {
                if (_Version != _Source._Version) throw new InvalidOperationException("collection changed");

                _Index = 0;
                _Current = default;
            }

            #endregion
        }

        #endregion
    }
}