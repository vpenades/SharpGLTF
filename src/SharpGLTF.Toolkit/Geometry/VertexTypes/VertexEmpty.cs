using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using SharpGLTF.Transforms;

namespace SharpGLTF.Geometry.VertexTypes
{
    /// <summary>
    /// Represents an empty vertex attribute that can be used as an
    /// empty <see cref="IVertexMaterial"/> or empty <see cref="IVertexSkinning"/>
    /// in a <see cref="VertexBuilder{TvG, TvM, TvS}"/> structure.
    /// </summary>
    [System.Diagnostics.DebuggerDisplay("Empty")]
    public readonly struct VertexEmpty : IVertexMaterial, IVertexSkinning
    {
        #region constructor
        public void Validate() { }

        #endregion

        #region data

        public override bool Equals(object obj) { return obj is VertexEmpty; }
        public bool Equals(VertexEmpty other) { return true; }
        public static bool operator ==(in VertexEmpty a, in VertexEmpty b) { return true; }
        public static bool operator !=(in VertexEmpty a, in VertexEmpty b) { return false; }
        public override int GetHashCode() { return 0; }

        #endregion

        #region properties

        public int MaxBindings => 0;

        public int MaxColors => 0;

        public int MaxTextCoords => 0;

        [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)]
        Vector4 IVertexSkinning.JointsLow => Vector4.Zero;
        [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)]
        Vector4 IVertexSkinning.JointsHigh => Vector4.Zero;
        [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)]
        Vector4 IVertexSkinning.WeightsLow => Vector4.Zero;
        [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)]
        Vector4 IVertexSkinning.WeightsHigh => Vector4.Zero;

        #endregion

        #region API

        void IVertexMaterial.SetColor(int index, Vector4 color) { throw new ArgumentOutOfRangeException(nameof(index)); }

        void IVertexMaterial.SetTexCoord(int index, Vector2 coord) { throw new ArgumentOutOfRangeException(nameof(index)); }

        Vector4 IVertexMaterial.GetColor(int index) { throw new ArgumentOutOfRangeException(nameof(index)); }

        Vector2 IVertexMaterial.GetTexCoord(int index) { throw new ArgumentOutOfRangeException(nameof(index)); }

        public SparseWeight8 GetWeights() { return default; }

        public void SetWeights(in SparseWeight8 weights) { throw new NotSupportedException(); }

        void IVertexSkinning.SetJointBinding(int index, int joint, float weight) { throw new ArgumentOutOfRangeException(nameof(index)); }

        (int, float) IVertexSkinning.GetJointBinding(int index) { throw new ArgumentOutOfRangeException(nameof(index)); }

        public object GetCustomAttribute(string attributeName) { return null; }

        #endregion
    }
}
