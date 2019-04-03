using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace SharpGLTF.Collections
{
    class VertexList<T> : IReadOnlyList<T>
        where T : struct
    {
        #region data

        private readonly List<T> _Vertices = new List<T>();
        private readonly Dictionary<T, int> _VertexCache = new Dictionary<T, int>();

        #endregion

        #region API

        public T this[int index] => _Vertices[index];

        public int Count => _Vertices.Count;

        public IEnumerator<T> GetEnumerator() { return _Vertices.GetEnumerator(); }

        IEnumerator IEnumerable.GetEnumerator() { return _Vertices.GetEnumerator(); }

        public int Use(T v)
        {
            if (_VertexCache.TryGetValue(v, out int index)) return index;

            index = _Vertices.Count;

            _Vertices.Add(v);
            _VertexCache[v] = index;

            return index;
        }

        #endregion
    }
}
