
using System;
using System.Collections.Generic;
using System.Text;

using SharpGLTF.Geometry;
using SharpGLTF.Geometry.VertexTypes;

namespace SharpGLTF
{
    static class ToolkitUtils
    {
        public static void AddConvexPolygon<TMaterial, TvG, TvM, TvS>(this PrimitiveBuilder<TMaterial, TvG, TvM, TvS> primitive, params VertexBuilder<TvG, TvM, TvS>[] vertices)
        where TvG : struct, IVertexGeometry
        where TvM : struct, IVertexMaterial
        where TvS : struct, IVertexSkinning
        {
            for (int i = 2; i < vertices.Length; ++i)
            {
                var a = vertices[0];
                var b = vertices[i - 1];
                var c = vertices[i];

                primitive.AddTriangle(a, b, c);
            }
        }
    }
}
