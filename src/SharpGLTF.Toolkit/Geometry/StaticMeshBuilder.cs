using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;

namespace SharpGLTF.Geometry
{
    public class MeshBuilder<TMaterial, TVertex, TValues> : MeshBuilder<TMaterial, TVertex, TValues, VertexTypes.VertexEmpty>
        where TVertex : struct, VertexTypes.IVertexPosition
        where TValues : struct, VertexTypes.IVertexMaterial
    {
        public MeshBuilder(string name = null)
            : base(name) { }
    }

    public class MeshBuilder<TMaterial, TVertex> : MeshBuilder<TMaterial, TVertex, VertexTypes.VertexEmpty, VertexTypes.VertexEmpty>
       where TVertex : struct, VertexTypes.IVertexPosition
    {
        public MeshBuilder(string name = null)
            : base(name) { }
    }
}
