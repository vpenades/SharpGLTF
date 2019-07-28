using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using SharpGLTF.Transforms;

namespace SharpGLTF.Geometry.VertexTypes
{
    [System.Diagnostics.DebuggerDisplay("Empty")]
    public struct VertexEmpty : IVertexMaterial, IVertexSkinning
    {
        public void Validate() { }

        public int MaxBindings => 0;

        public int MaxColors => 0;

        public int MaxTextCoords => 0;

        void IVertexMaterial.SetColor(int setIndex, Vector4 color) { }

        void IVertexMaterial.SetTexCoord(int setIndex, Vector2 coord) { }

        Vector4 IVertexMaterial.GetColor(int index) { throw new NotSupportedException(); }

        Vector2 IVertexMaterial.GetTexCoord(int index) { throw new NotSupportedException(); }

        void IVertexSkinning.SetJointBinding(int index, int joint, float weight) { }

        JointBinding IVertexSkinning.GetJointBinding(int index) { throw new NotSupportedException(); }

        public void SetWeights(in SparseWeight8 weights) { }

        public IEnumerable<JointBinding> JointBindings => Enumerable.Empty<JointBinding>();

        public Vector4 JointsLow => Vector4.Zero;

        public Vector4 JointsHigh => Vector4.Zero;

        public Vector4 WeightsLow => Vector4.Zero;

        public Vector4 Weightshigh => Vector4.Zero;

        public SparseWeight8 SparseWeights => default(SparseWeight8);
    }
}
