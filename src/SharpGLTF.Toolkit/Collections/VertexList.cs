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

        private readonly Dictionary<VertexKey, int> _VertexCache = new Dictionary<VertexKey, int>();

        [ThreadStatic]
        private readonly T[] _VertexProbe = new T[1];

        #endregion

        #region API

        public T this[int index] => _Vertices[index];

        public int Count => _Vertices.Count;

        public IEnumerator<T> GetEnumerator() { return _Vertices.GetEnumerator(); }

        IEnumerator IEnumerable.GetEnumerator() { return _Vertices.GetEnumerator(); }

        public int Use(T v)
        {
            _VertexProbe[0] = v;

            if (_VertexCache.TryGetValue(new VertexKey(_VertexProbe, 0), out int index))
            {
                System.Diagnostics.Debug.Assert(Object.Equals(v, _Vertices[index]), "Vertex equality failed");
                return index;
            }

            index = _Vertices.Count;

            _Vertices.Add(v);

            var key = new VertexKey(_Vertices, index);

            _VertexCache[key] = index;

            return index;
        }

        public void TransformVertices(Func<T, T> transformFunc)
        {
            _VertexCache.Clear();

            for (int i = 0; i < _Vertices.Count; ++i)
            {
                _Vertices[i] = transformFunc(_Vertices[i]);

                var key = new VertexKey(_Vertices, i);

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
                dst._VertexCache[new VertexKey(dst._Vertices, idx)] = idx;
            }
        }

        #endregion

        #region types

        [System.Diagnostics.DebuggerDisplay("{_Index} {GetHashCode()}")]
        private struct VertexKey : IEquatable<VertexKey>
        {
            public VertexKey(IReadOnlyList<T> src, int idx)
            {
                _Source = src;
                _Index = idx;
            }

            private readonly IReadOnlyList<T> _Source;
            private readonly int _Index;

            public T GetValue() { return _Source[_Index]; }

            public override int GetHashCode()
            {
                return GetValue().GetHashCode();
            }

            public static bool AreEqual(VertexKey x, VertexKey y)
            {
                var xx = x.GetValue();
                var yy = y.GetValue();

                return object.Equals(xx, yy);
            }

            public override bool Equals(object obj)
            {
                return AreEqual(this, (VertexKey)obj);
            }

            public bool Equals(VertexKey other)
            {
                return AreEqual(this, other);
            }
        }

        #endregion
    }
}
