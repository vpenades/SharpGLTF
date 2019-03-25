using System;
using System.Collections;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

using static System.FormattableString;

namespace SharpGLTF
{
    using POSITION = Geometry.VertexTypes.VertexPositionNormal;
    using TEXCOORD = Geometry.VertexTypes.VertexEmpty;
    using MATERIAL = Vector4;

    /// <summary>
    /// Tiny wavefront object writer
    /// </summary>
    class WavefrontWriter
    {
        #region data

        private readonly Geometry.MeshBuilder<MATERIAL, POSITION, TEXCOORD> _Mesh = new Geometry.MeshBuilder<MATERIAL, POSITION, TEXCOORD>();
        
        #endregion

        #region API        

        public void AddTriangle(Vector3 a, Vector3 b, Vector3 c)
        {
            var aa = new POSITION { Position = a };
            var bb = new POSITION { Position = b };
            var cc = new POSITION { Position = c };

            _Mesh.UsePrimitive(Vector4.One).AddTriangle(aa, bb, cc);
        }

        public override string ToString()
        {
            var sb = new StringBuilder();

            sb.AppendLine();

            foreach (var p in _Mesh.Primitives)
            {
                foreach (var v in p.Vertices)
                {
                    sb.AppendLine(Invariant($"v {v.Item1.Position.X} {v.Item1.Position.Y} {v.Item1.Position.Z}"));
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
