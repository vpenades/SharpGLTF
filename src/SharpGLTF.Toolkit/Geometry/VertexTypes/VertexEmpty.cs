using System;
using System.Collections.Generic;
using System.Text;

namespace SharpGLTF.Geometry.VertexTypes
{
    public struct VertexEmpty : IVertexMaterial, IVertexJoints
    {
        public void Validate() { }
    }
}
