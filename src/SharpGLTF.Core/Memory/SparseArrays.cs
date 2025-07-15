using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace SharpGLTF.Memory
{
    /// <summary>
    /// Special accessor to wrap over a base accessor and a sparse accessor
    /// </summary>
    /// <typeparam name="T">An unmanage structure type.</typeparam>
    [System.Diagnostics.DebuggerDisplay("Sparse {typeof(T).Name} Accessor {Count}")]
    public sealed class SparseArray<T> : IAccessorArray<T>
        where T : unmanaged
    {
        #region lifecycle

        public SparseArray(IReadOnlyList<T> denseValues, IReadOnlyList<T> sparseValues, IReadOnlyList<uint> sparseKeys)
        {
            Guard.NotNull(denseValues, nameof(denseValues));
            Guard.NotNull(sparseValues, nameof(sparseValues));
            Guard.NotNull(sparseKeys, nameof(sparseKeys));            
            Guard.MustBeEqualTo(sparseKeys.Count, sparseValues.Count, nameof(sparseKeys.Count));
            Guard.MustBeLessThanOrEqualTo(sparseKeys.Count, denseValues.Count, nameof(sparseKeys.Count));

            _DenseItems = denseValues;
            _SparseItems = sparseValues;

            // expand indices for fast access
            _SparseIndices = new Dictionary<int, int>();
            for (int val = 0; val < sparseKeys.Count; ++val)
            {
                var key = (int)sparseKeys[val];

                if (key >= denseValues.Count)
                {
                    throw new ArgumentOutOfRangeException(nameof(sparseKeys));
                }

                _SparseIndices[key] = val;
            }
        }

        #endregion

        #region data

        [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)]
        private readonly IReadOnlyList<T> _DenseItems;

        [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)]
        private readonly IReadOnlyList<T> _SparseItems;

        [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)]
        private readonly Dictionary<int, int> _SparseIndices;

        [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.RootHidden)]
        private T[] _DebugItems => this.ToArray();

        #endregion

        #region API

        [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)]
        public int Count => _DenseItems.Count;

        public bool IsReadOnly => true;

        public T this[int index]
        {
            get => _SparseIndices.TryGetValue(index, out int topIndex) ? _SparseItems[topIndex] : _DenseItems[index];
            set => throw new NotSupportedException("Collection is read only.");
        }

        public IEnumerator<T> GetEnumerator() { return new EncodedArrayEnumerator<T>(this); }

        IEnumerator IEnumerable.GetEnumerator() { return new EncodedArrayEnumerator<T>(this); }

        public bool Contains(T item) { return IndexOf(item) >= 0; }

        public int IndexOf(T item) { return this._FirstIndexOf(item); }

        public void CopyTo(T[] array, int arrayIndex) { Guard.NotNull(array, nameof(array)); this._CopyTo(array, arrayIndex); }

        void IList<T>.Insert(int index, T item) { throw new NotSupportedException(); }

        void IList<T>.RemoveAt(int index) { throw new NotSupportedException(); }

        void ICollection<T>.Add(T item) { throw new NotSupportedException(); }

        void ICollection<T>.Clear() { throw new NotSupportedException(); }

        bool ICollection<T>.Remove(T item) { throw new NotSupportedException(); }

        #endregion
    }
}
