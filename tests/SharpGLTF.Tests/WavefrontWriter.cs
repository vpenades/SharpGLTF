using System;
using System.Collections;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

using static System.FormattableString;

namespace SharpGLTF
{
    using VERTEX = Geometry.VertexTypes.VertexPositionNormal;
    using MATERIAL = Vector4;

    /// <summary>
    /// Tiny wavefront object writer
    /// </summary>
    class WavefrontWriter
    {
        #region data

        private readonly Geometry.StaticMeshBuilder<VERTEX, MATERIAL> _Mesh = new Geometry.StaticMeshBuilder<VERTEX, MATERIAL>();
        
        #endregion

        #region API        

        public void AddTriangle(Vector3 a, Vector3 b, Vector3 c)
        {
            var aa = new VERTEX { Position = a };
            var bb = new VERTEX { Position = b };
            var cc = new VERTEX { Position = c };

            _Mesh.AddTriangle(Vector4.One, aa, bb, cc);
        }

        public override string ToString()
        {
            var sb = new StringBuilder();

            sb.AppendLine();

            foreach (var p in _Mesh.Primitives)
            {
                foreach (var v in p.Vertices)
                {
                    sb.AppendLine(Invariant($"v {v.Position.X} {v.Position.Y} {v.Position.Z}"));
                }
            }

            sb.AppendLine();

            sb.AppendLine("g default");

            var baseVertexIndex = 1;

            foreach(var p in _Mesh.Primitives)
            {
                foreach (var t in p.Triangles)
                {
                    sb.AppendLine(Invariant($"f {t.Item1 + baseVertexIndex} {t.Item2 + baseVertexIndex} {t.Item3 + baseVertexIndex}"));
                }

                baseVertexIndex += p.Vertices.Count;
            }

            return sb.ToString();
        }

        #endregion
    }
}
