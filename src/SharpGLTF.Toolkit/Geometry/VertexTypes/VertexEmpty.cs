using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace SharpGLTF.Geometry.VertexTypes
{
    public struct VertexEmpty : IVertexMaterial, IVertexSkinning
    {
        public int MaxJoints => 0;

        void IVertexMaterial.SetColor(int setIndex, Vector4 color) { }

        void IVertexMaterial.SetTexCoord(int setIndex, Vector2 coord) { }

        void IVertexSkinning.SetJoints(int jointSet, Vector4 joints, Vector4 weights) { }

        public void Validate() { }

        public KeyValuePair<int, float> GetJoint(int index) { return default; }

        public void SetJoint(int index, KeyValuePair<int, float> jw) { }

        public void AssignFrom(IVertexSkinning vertex) { }
    }
}
