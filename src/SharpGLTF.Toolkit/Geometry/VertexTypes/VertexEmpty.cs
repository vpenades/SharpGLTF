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
    public readonly struct VertexEmpty : IVertexMaterial, IVertexSkinning, IEquatable<VertexEmpty>
    {
        #region constructor
        public void Validate() { }

        #endregion

        #region data

        public override int GetHashCode() { return 0; }
        public override bool Equals(object obj) { return obj is VertexEmpty; }
        public bool Equals(VertexEmpty other) { return true; }
        public static bool operator ==(in VertexEmpty a, in VertexEmpty b) { return true; }
        public static bool operator !=(in VertexEmpty a, in VertexEmpty b) { return false; }

        #endregion

        #region properties

        /// <inheritdoc/>
        public int MaxBindings => 0;

        /// <inheritdoc/>
        public int MaxColors => 0;

        /// <inheritdoc/>
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

        VertexMaterialDelta IVertexMaterial.Subtract(IVertexMaterial baseValue) { return VertexMaterialDelta.Zero; }

        void IVertexMaterial.Add(in VertexMaterialDelta delta) { /* do nothing */ }

        Vector4 IVertexMaterial.GetColor(int index) { throw new ArgumentOutOfRangeException(nameof(index)); }

        Vector2 IVertexMaterial.GetTexCoord(int index) { throw new ArgumentOutOfRangeException(nameof(index)); }

        /// <inheritdoc/>
        public SparseWeight8 GetBindings() { return default; }

        /// <inheritdoc/>
        public void SetBindings(in SparseWeight8 bindings) { throw new NotSupportedException(); }

        /// <inheritdoc/>
        public void SetBindings(params (int Index, float Weight)[] bindings) { throw new NotSupportedException(); }

        /// <inheritdoc/>
        (int Index, float Weight) IVertexSkinning.GetBinding(int index) { throw new ArgumentOutOfRangeException(nameof(index)); }

        #endregion
    }
}
