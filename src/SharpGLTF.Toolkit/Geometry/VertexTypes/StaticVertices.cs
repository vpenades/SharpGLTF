using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace SharpGLTF.Geometry.VertexTypes
{
    public struct StaticPositionNormal
    {
        public StaticPositionNormal(float px, float py, float pz, float nx, float ny, float nz)
        {
            Position = new Vector3(px, py, pz);
            Normal = Vector3.Normalize(new Vector3(nx, ny, nz));
        }

        public Vector3 Position;
        public Vector3 Normal;
    }
}
