using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

using static System.FormattableString;

namespace SharpGLTF
{
    /// <summary>
    /// Tiny wavefront object writer
    /// </summary>
    class WavefrontWriter
    {
        #region data

        private readonly List<Vector3> _Vertices = new List<Vector3>();
        private readonly List<(int, int, int)> _Indices = new List<(int, int, int)>();
        private readonly Dictionary<Vector3, int> _VertexCache = new Dictionary<Vector3, int>();

        #endregion

        #region API

        private int _UseVertex(Vector3 v)
        {
            if (_VertexCache.TryGetValue(v, out int index)) return index;

            index = _Vertices.Count;

            _Vertices.Add(v);
            _VertexCache[v] = index;

            return index;
        }

        public void AddTriangle(Vector3 a, Vector3 b, Vector3 c)
        {
            var aa = _UseVertex(a);
            var bb = _UseVertex(b);
            var cc = _UseVertex(c);

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

            foreach (var v in _Vertices)
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
