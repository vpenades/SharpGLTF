using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Transactions;

using SharpGLTF.Geometry;
using SharpGLTF.Materials;
using SharpGLTF.Schema2;

namespace SharpGLTF
{
    public static class PlotlyExtensions
    {
        public static Plotly.Box<Plotly.Types.ITracesProperty> ToTrace(this Schema2.Scene scene)
        {
            return PlotlyFactory.ToTrace(scene.EvaluateTriangles(), GetMaterialColor);
        }

        public static Plotly.Box<Plotly.Types.ITracesProperty> ToTrace(this Schema2.Mesh mesh)
        {
            return PlotlyFactory.ToTrace(mesh.EvaluateTriangles(), GetMaterialColor);
        }
        
        public static Plotly.Box<Plotly.Types.ITracesProperty> ToTrace(this IMeshBuilder<MaterialBuilder> mesh)
        {
            IEnumerable<(IVertexBuilder, IVertexBuilder, IVertexBuilder, MaterialBuilder)> _enumTris()
            {
                foreach (var p in mesh.Primitives)
                {
                    foreach (var (A, B, C) in p.Triangles)
                    {
                        var va = p.Vertices[A];
                        var vb = p.Vertices[B];
                        var vc = p.Vertices[C];

                        yield return (va, vb, vc, p.Material);
                    }
                }
            }

            return PlotlyFactory.ToTrace(_enumTris(), GetMaterialColor);
        }

        private static int GetMaterialColor(Schema2.Material material)
        {
            var mb = new MaterialBuilder();
            material.CopyTo(mb);
            return GetMaterialColor(mb);
        }

        private static int GetMaterialColor(MaterialBuilder material)
        {
            var color = (material.GetChannel(KnownChannel.BaseColor) ?? material.GetChannel(KnownChannel.Diffuse))?.Parameter ?? Vector4.One * 0.8f;

            color *= 255;

            var ccc = color.X * 65536 + color.Y * 256 + color.Z;

            return (int)ccc;
        }
    }
}
