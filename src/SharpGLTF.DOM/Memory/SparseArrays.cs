using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SharpGLTF.Memory
{
    /// <summary>
    /// Special accessor to wrap over a base accessor and a sparse accessor
    /// </summary>
    /// <typeparam name="T"></typeparam>
    [System.Diagnostics.DebuggerDisplay("Sparse {typeof(T).Name} Accessor {Count}")]
    public struct SparseArray<T> : IEncodedArray<T>
        where T : unmanaged
    {
        #region lifecycle

        public SparseArray(IEncodedArray<T> bottom, IEncodedArray<T> top, IntegerArray topMapping)
        {
            _BottomItems = bottom;
            _TopItems = top;
            _Mapping = new Dictionary<int, int>();

            for (int val = 0; val < topMapping.Count; ++val)
            {
                var key = (int)topMapping[val];
                _Mapping[key] = val;
            }
        }

        #endregion

        #region data

        [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)]
        private readonly IEncodedArray<T> _BottomItems;

        [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)]
        private readonly IEncodedArray<T> _TopItems;

        [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)]
        private readonly Dictionary<int, int> _Mapping;

        [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.RootHidden)]
        private T[] _DebugItems => this.ToArray();

        #endregion

        #region API

        [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)]
        public int Count => _BottomItems.Count;

        public T this[int index]
        {
            get => _Mapping.TryGetValue(index, out int topIndex) ? _TopItems[topIndex] : _BottomItems[index];
            set
            {
                if (_Mapping.TryGetValue(index, out int topIndex)) _TopItems[topIndex] = value;
            }
        }

        public void CopyTo(ArraySegment<T> dst) { EncodedArrayUtils.Copy(this, dst); }

        public (T, T) GetBounds()
        {
            throw new NotImplementedException();
        }

        public IEnumerator<T> GetEnumerator() { return new EncodedArrayEnumerator<T>(this); }

        IEnumerator IEnumerable.GetEnumerator() { return new EncodedArrayEnumerator<T>(this); }

        #endregion
    }
}
