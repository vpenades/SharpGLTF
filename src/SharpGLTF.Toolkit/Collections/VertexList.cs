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
        #region data

        private readonly List<T> _Vertices = new List<T>();

        private readonly Dictionary<IVertexKey, int> _VertexCache = new Dictionary<IVertexKey, int>(_KeyComparer.Instance);

        #endregion

        #region API

        public T this[int index] => _Vertices[index];

        public int Count => _Vertices.Count;

        public IEnumerator<T> GetEnumerator() { return _Vertices.GetEnumerator(); }

        IEnumerator IEnumerable.GetEnumerator() { return _Vertices.GetEnumerator(); }

        public int Use(T v)
        {
            if (_VertexCache.TryGetValue(new QueryKey(v), out int index))
            {
                System.Diagnostics.Debug.Assert(Object.Equals(v, _Vertices[index]), "Vertex equality failed");
                return index;
            }

            index = _Vertices.Count;

            _Vertices.Add(v);

            var key = new StoredKey(_Vertices, index);

            _VertexCache[key] = index;

            return index;
        }

        public void TransformVertices(Func<T, T> transformFunc)
        {
            _VertexCache.Clear();

            for (int i = 0; i < _Vertices.Count; ++i)
            {
                _Vertices[i] = transformFunc(_Vertices[i]);

                var key = new StoredKey(_Vertices, i);

                _VertexCache[key] = i;
            }
        }

        public void CopyTo(VertexList<T> dst)
        {
            for (int i = 0; i < this._Vertices.Count; ++i)
            {
                var v = this._Vertices[i];

                var idx = dst._Vertices.Count;
                dst._Vertices.Add(v);
                dst._VertexCache[new StoredKey(dst._Vertices, idx)] = idx;
            }
        }

        #endregion

        #region types

        interface IVertexKey
        {
            T GetValue();
        }

        sealed class _KeyComparer : IEqualityComparer<IVertexKey>
        {
            static _KeyComparer() { }
            private _KeyComparer() { }

            private static readonly _KeyComparer _Instance = new _KeyComparer();
            public static _KeyComparer Instance => _Instance;

            public bool Equals(IVertexKey x, IVertexKey y)
            {
                var xx = x.GetValue();
                var yy = y.GetValue();

                return object.Equals(xx, yy);
            }

            public int GetHashCode(IVertexKey obj)
            {
                return obj
                    .GetValue()
                    .GetHashCode();
            }
        }

        [System.Diagnostics.DebuggerDisplay("{GetValue()} {GetHashCode()}")]
        private readonly struct QueryKey : IVertexKey
        {
            public QueryKey(T value) { _Value = value; }

            private readonly T _Value;

            public T GetValue() { return _Value; }
        }

        [System.Diagnostics.DebuggerDisplay("{GetValue()} {GetHashCode()}")]
        private readonly struct StoredKey : IVertexKey
        {
            public StoredKey(IReadOnlyList<T> src, int idx)
            {
                _Source = src;
                _Index = idx;
            }

            private readonly IReadOnlyList<T> _Source;
            private readonly int _Index;

            public T GetValue() { return _Source[_Index]; }
        }

        #endregion
    }
}
