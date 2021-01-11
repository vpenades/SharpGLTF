using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace SharpGLTF.Collections
{
    /// <summary>
    /// Represent an ordered collection of <typeparamref name="T"/> vertices, where every vertex is unique.
    /// </summary>
    /// <typeparam name="T">A Vertex type</typeparam>
    class VertexList<T> : IReadOnlyList<T>
        where T : struct
    {
        #region lifecycle

        public VertexList()
        {
            _VertexComparer = new _KeyComparer(_Vertices);
            _VertexCache = new Dictionary<int, int>(_VertexComparer);
        }

        #endregion

        #region data

        private List<T> _Vertices = new List<T>();

        private _KeyComparer _VertexComparer;
        private Dictionary<int, int> _VertexCache;

        #endregion

        #region API

        public T this[int index] => _Vertices[index];

        public int Count => _Vertices.Count;

        public IEnumerator<T> GetEnumerator() { return _Vertices.GetEnumerator(); }

        IEnumerator IEnumerable.GetEnumerator() { return _Vertices.GetEnumerator(); }

        public int Use(in T v)
        {
            var idx = IndexOf(v);

            return idx >= 0 ? idx : _Add(v);
        }

        public int IndexOf(in T v)
        {
            _VertexComparer.QueryValue = v;

            if (_VertexCache.TryGetValue(-1, out int idx))
            {
                _VertexComparer.QueryValue = default;

                System.Diagnostics.Debug.Assert(Object.Equals(v, _Vertices[idx]), "Vertex equality failed");

                return idx;
            }

            _VertexComparer.QueryValue = default;

            return -1;
        }

        private int _Add(in T v)
        {
            int idx = _Vertices.Count;

            _Vertices.Add(v);
            _VertexCache[idx] = idx;

            System.Diagnostics.Debug.Assert(_Vertices.Count == _VertexCache.Count);

            return idx;
        }

        public void ApplyTransform(Func<T, T> transformFunc)
        {
            // although our "apparent" dictionary keys and values remain the same
            // we must reconstruct the VertexCache to regenerate the hashes.
            _VertexCache.Clear();

            for (int i = 0; i < _Vertices.Count; ++i)
            {
                _Vertices[i] = transformFunc(_Vertices[i]);
                _VertexCache[i] = i;
            }
        }

        public void CopyTo(VertexList<T> dst) { dst._Set(this); }

        private void _Set(VertexList<T> src)
        {
            _Vertices = new List<T>(src._Vertices);
            _VertexComparer = new _KeyComparer(_Vertices);
            _VertexCache = new Dictionary<int, int>(src._VertexCache, _VertexComparer);
        }

        #endregion

        #region nested types

        sealed class _KeyComparer : IEqualityComparer<int>
        {
            public _KeyComparer(IReadOnlyList<T> items) { _Items = items; }

            private readonly IReadOnlyList<T> _Items;

            public T QueryValue { get; set; }

            public bool Equals(int x, int y)
            {
                var xx = x < 0 ? QueryValue : _Items[x];
                var yy = y < 0 ? QueryValue : _Items[y];

                return object.Equals(xx, yy);
            }

            public int GetHashCode(int idx) { return (idx < 0 ? QueryValue : _Items[idx]).GetHashCode(); }
        }

        #endregion
    }
}
