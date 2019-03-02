using System;
using System.Collections;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

using static System.FormattableString;

namespace SharpGLTF
{

    class VertexColumn<T> : IReadOnlyList<T>
        where T:struct
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

    /// <summary>
    /// Tiny wavefront object writer
    /// </summary>
    class WavefrontWriter
    {
        #region data

        private readonly VertexColumn<Vector3> _Positions = new VertexColumn<Vector3>();
        private readonly VertexColumn<Vector3> _Normals = new VertexColumn<Vector3>();

        private readonly List<(int, int, int)> _Indices = new List<(int, int, int)>();        

        #endregion

        #region API        

        public void AddTriangle(Vector3 a, Vector3 b, Vector3 c)
        {
            var aa = _Positions.Use(a);
            var bb = _Positions.Use(b);
            var cc = _Positions.Use(c);

            // check for degenerated triangles:
            if (aa == bb) return;
            if (aa == cc) return;
            if (bb == cc) return;

            _Indices.Add((aa, bb, cc));
        }

        public override string ToString()
        {
            var sb = new StringBuilder();

            sb.AppendLine();

            foreach (var v in _Positions)
            {
                sb.AppendLine(Invariant($"v {v.X} {v.Y} {v.Z}"));
            }

            sb.AppendLine();

            sb.AppendLine("g default");

            foreach (var p in _Indices)
            {
                sb.AppendLine(Invariant($"f {p.Item1 + 1} {p.Item2 + 1} {p.Item3 + 1}"));
            }

            return sb.ToString();
        }

        #endregion
    }
}
