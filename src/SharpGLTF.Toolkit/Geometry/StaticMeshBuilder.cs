using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;

namespace SharpGLTF.Geometry
{
    using Collections;

    public class StaticMeshBuilder<TMaterial, TVertex> : SkinnedMeshBuilder<TMaterial, TVertex, VertexTypes.VertexJoints0>
        where TVertex : struct, VertexTypes.IVertex
    {
        public StaticMeshBuilder(string name = null) : base(name) { }

        public new void AddPolygon(TMaterial material, params TVertex[] points)
        {
            for (int i = 2; i < points.Length; ++i)
            {
                AddTriangle(material, points[0], points[i - 1], points[i]);
            }
        }

        public new void AddTriangle(TMaterial material, TVertex a, TVertex b, TVertex c)
        {
            AddTriangle(material, (a, default), (b, default), (c, default));
        }
    }
}
