using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace SharpGLTF.Geometry.VertexTypes
{
    public struct VertexEmpty : IVertexMaterial, IVertexSkinning
    {
        public void Validate() { }

        public int MaxJoints => 0;

        public int MaxColors => 0;

        public int MaxTextures => 0;

        void IVertexMaterial.SetColor(int setIndex, Vector4 color) { }

        void IVertexMaterial.SetTexCoord(int setIndex, Vector2 coord) { }

        Vector4 IVertexMaterial.GetColor(int index) { throw new NotSupportedException(); }

        Vector2 IVertexMaterial.GetTexCoord(int index) { throw new NotSupportedException(); }

        void IVertexSkinning.SetJoint(int index, int joint, float weight) { }

        JointWeightPair IVertexSkinning.GetJoint(int index) { throw new NotSupportedException(); }
    }
}
