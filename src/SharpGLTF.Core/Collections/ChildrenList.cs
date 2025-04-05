using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

using FIELDINFO = SharpGLTF.Reflection.FieldInfo;

namespace SharpGLTF.Collections
{
    /// <summary>
    /// An Specialisation of <see cref="List{T}"/>, which interconnects the list items with the parent of the collection.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <typeparam name="TParent"></typeparam>
    [System.Diagnostics.DebuggerDisplay("{Count}")]
    public sealed class ChildrenList<T, TParent>
        : IList<T>, IReadOnlyList<T>
        , Reflection.IReflectionArray
        where T : class, IChildOfList<TParent>
        where TParent : class
    {
        #region lifecycle

        public ChildrenList(TParent parent)
        {
            Guard.NotNull(parent, nameof(parent));
            _Parent = parent;
        }

        #endregion

        #region data

        [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)]
        private readonly TParent _Parent;

        [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.RootHidden)]
        private List<T> _Collection;

        #endregion

        #region properties

        /// <inheritdoc/>
        public T this[int index]
        {
            get
            {
                if (_Collection == null) throw new ArgumentOutOfRangeException(nameof(index));
                return _Collection[index];
            }

            set
            {
                // new value must be an orphan
                _VerifyIsOrphan(value);

                if (_Collection == null) throw new ArgumentOutOfRangeException(nameof(index));

                if (_Collection[index] == value) return; // nothing to do

                // make the current child orphan.
                if (_Collection[index] != null)
                {
                    _Collection[index].SetLogicalParent(null, -1);
                    _AssertItem(_Collection[index], -1);
                }

                // update the collection with new child
                _Collection[index] = value;

                if (_Collection[index] != null)
                {
                    _Collection[index].SetLogicalParent(_Parent, index);
                    _AssertItem(_Collection[index], index);
                }
            }
        }

        /// <inheritdoc/>
        public int Count => _Collection == null ? 0 : _Collection.Count;

        /// <inheritdoc/>
        [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)]
        public bool IsReadOnly => false;

        #endregion

        #region API

        /// <inheritdoc/>
        public bool Contains(T item)
        {
            return _Collection?.Contains(item) ?? false;
        }

        /// <inheritdoc/>
        public int IndexOf(T item)
        {
            return _Collection?.IndexOf(item) ?? -1;
        }

        /// <inheritdoc/>
        public void CopyTo(T[] array, int arrayIndex)
        {
            if (_Collection == null) return;
            _Collection.CopyTo(array, arrayIndex);
        }

        /// <inheritdoc/>
        public void Add(T item)
        {
            _VerifyIsOrphan(item);

            _Collection ??= new List<T>();

            var idx =_Collection.Count;
            _Collection.Add(item);
            item.SetLogicalParent(_Parent, idx);
            _AssertItem(item, idx);
        }

        /// <inheritdoc/>
        public void Clear()
        {
            if (_Collection == null) return;

            foreach (var item in _Collection) // orphan all children
            {
                item.SetLogicalParent(null, -1);
                _AssertItem(item, -1);
            }

            _Collection = null;
        }

        /// <inheritdoc/>
        public void Insert(int index, T item)
        {
            _VerifyIsOrphan(item);

            _Collection ??= new List<T>();
            _Collection.Insert(index, item);

            // fix indices of upper items
            for (int i = index; i < _Collection.Count; ++i)
            {
                item = _Collection[i];
                if (item == null) continue;
                item.SetLogicalParent(_Parent, i);
                _AssertItem(item, i);
            }
        }

        /// <inheritdoc/>
        public bool Remove(T item)
        {
            var idx = IndexOf(item);
            if (idx < 0) return false;
            RemoveAt(idx);

            return true;
        }

        /// <inheritdoc/>
        public void RemoveAt(int index)
        {
            if (_Collection == null) throw new ArgumentOutOfRangeException(nameof(index));
            if (index < 0 || index >= _Collection.Count) throw new ArgumentOutOfRangeException(nameof(index));

            var item = _Collection[index];
            _Collection.RemoveAt(index);

            // orphan the current child
            item?.SetLogicalParent(null, -1);

            // fix indices of upper items
            for (int i = index; i < _Collection.Count; ++i)
            {
                item = _Collection[i];

                item.SetLogicalParent(_Parent, i);
                _AssertItem(item, i);
            }

            if (_Collection.Count == 0) _Collection = null;
        }

        /// <inheritdoc/>
        public IEnumerator<T> GetEnumerator()
        {
            return _Collection?.GetEnumerator() ?? Enumerable.Empty<T>().GetEnumerator();
        }

        /// <inheritdoc/>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return _Collection?.GetEnumerator() ?? Enumerable.Empty<T>().GetEnumerator();
        }

        private static void _VerifyIsOrphan(T item)
        {            
            Guard.NotNull(item, nameof(item));
            Guard.MustBeNull(item.LogicalParent, nameof(item.LogicalParent));
            Guard.MustBeEqualTo(-1, item.LogicalIndex, nameof(item.LogicalIndex));
        }

        [Conditional("DEBUG")]
        private void _AssertItem(T item, int index)
        {
            System.Diagnostics.Debug.Assert(item.LogicalIndex == index);

            var parent = index >= 0 ? _Parent : null;

            System.Diagnostics.Debug.Assert(item.LogicalParent == parent);            
        }

        #endregion

        #region Reflection

        IEnumerable<FIELDINFO> Reflection.IReflectionObject.GetFields()
        {
            for(int i=0; i < Count; ++i)
            {
                yield return ((Reflection.IReflectionArray)this).GetField(i);
            }
        }

        FIELDINFO Reflection.IReflectionArray.GetField(int index)
        {
            return FIELDINFO.From(index.ToString(System.Globalization.CultureInfo.InvariantCulture), this, list => list[index]);
        }

        public bool TryGetField(string name, out FIELDINFO value)
        {
            if (int.TryParse(name, System.Globalization.NumberStyles.Integer, System.Globalization.CultureInfo.InvariantCulture, out var index))
            {
                value = FIELDINFO.From(name, this, list => list[index]);
                return true;
            }

            value = default;
            return false;
        }

        #endregion
    }
}
