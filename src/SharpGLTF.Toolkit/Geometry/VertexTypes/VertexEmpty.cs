using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using SharpGLTF.Transforms;

namespace SharpGLTF.Geometry.VertexTypes
{
    [System.Diagnostics.DebuggerDisplay("Empty")]
    public readonly struct VertexEmpty : IVertexMaterial, IVertexSkinning
    {
        public void Validate() { }

        public int MaxBindings => 0;

        public int MaxColors => 0;

        public int MaxTextCoords => 0;

        void IVertexMaterial.SetColor(int index, Vector4 color) { throw new ArgumentOutOfRangeException(nameof(index)); }

        void IVertexMaterial.SetTexCoord(int index, Vector2 coord) { throw new ArgumentOutOfRangeException(nameof(index)); }

        Vector4 IVertexMaterial.GetColor(int index) { throw new ArgumentOutOfRangeException(nameof(index)); }

        Vector2 IVertexMaterial.GetTexCoord(int index) { throw new ArgumentOutOfRangeException(nameof(index)); }

        public SparseWeight8 GetWeights() { return default; }

        public void SetWeights(in SparseWeight8 weights) { throw new NotSupportedException(); }

        void IVertexSkinning.SetJointBinding(int index, int joint, float weight) { throw new ArgumentOutOfRangeException(nameof(index)); }

        (int, float) IVertexSkinning.GetJointBinding(int index) { throw new ArgumentOutOfRangeException(nameof(index)); }

        public object GetCustomAttribute(string attributeName) { return null; }

        [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)]
        Vector4 IVertexSkinning.JointsLow => Vector4.Zero;
        [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)]
        Vector4 IVertexSkinning.JointsHigh => Vector4.Zero;
        [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)]
        Vector4 IVertexSkinning.WeightsLow => Vector4.Zero;
        [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)]
        Vector4 IVertexSkinning.WeightsHigh => Vector4.Zero;
    }
}
