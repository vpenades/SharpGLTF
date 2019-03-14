using System;
using System.Collections;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

using static System.FormattableString;

namespace SharpGLTF
{
    using VERTEX = Geometry.VertexTypes.StaticPositionNormal;
    using MATERIAL = Vector4;

    /// <summary>
    /// Tiny wavefront object writer
    /// </summary>
    class WavefrontWriter
    {
        #region data

        private readonly Geometry.InterleavedMeshBuilder<VERTEX, MATERIAL> _Mesh = new Geometry.InterleavedMeshBuilder<VERTEX, MATERIAL>();
        
        #endregion

        #region API        

        public void AddTriangle(Vector3 a, Vector3 b, Vector3 c)
        {
            var aa = new Geometry.VertexTypes.StaticPositionNormal
            {
                Position = a
            };

            var bb = new Geometry.VertexTypes.StaticPositionNormal
            {
                Position = b
            };

            var cc = new Geometry.VertexTypes.StaticPositionNormal
            {
                Position = c
            };

            _Mesh.AddTriangle(Vector4.One, aa, bb, cc);
        }

        public override string ToString()
        {
            var sb = new StringBuilder();

            sb.AppendLine();

            foreach (var v in _Mesh.Vertices)
            {
                sb.AppendLine(Invariant($"v {v.Position.X} {v.Position.Y} {v.Position.Z}"));
            }

            sb.AppendLine();

            sb.AppendLine("g default");

            foreach(var m in _Mesh.Materials)
            {
                var triangles = _Mesh.GetTriangles(m);

                foreach (var t in triangles)
                {
                    sb.AppendLine(Invariant($"f {t.Item1 + 1} {t.Item2 + 1} {t.Item3 + 1}"));
                }
            }

            return sb.ToString();
        }

        #endregion
    }
}
