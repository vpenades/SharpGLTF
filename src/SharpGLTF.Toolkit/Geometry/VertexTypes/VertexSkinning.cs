using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;
using SharpGLTF.Transforms;

using ENCODING = SharpGLTF.Schema2.EncodingType;

namespace SharpGLTF.Geometry.VertexTypes
{
    /// <summary>
    /// Represents the interface that must be implemented by a skinning vertex fragment.
    /// </summary>
    /// <remarks>
    /// Implemented by:
    /// <list type="table">
    /// <item><see cref="VertexEmpty"/></item>
    /// <item><see cref="VertexJoints4"/></item>
    /// <item><see cref="VertexJoints8"/></item>
    /// </list>
    /// </remarks>
    public interface IVertexSkinning
    {
        /// <summary>
        /// Gets the Number of valid joints supported.<br/>Typical values are 0, 4 or 8.
        /// </summary>
        int MaxBindings { get; }

        /// <summary>
        /// Gets a joint-weight pair.
        /// </summary>
        /// <param name="index">An index from 0 to <see cref="MaxBindings"/> exclusive.</param>
        /// <returns>The joint-weight pair.</returns>
        (int Index, float Weight) GetBinding(int index);

        /// <summary>
        /// Sets the packed joints-weights.
        /// <para><b>⚠️ USE ONLY ON UNBOXED VALUES ⚠️</b></para>
        /// </summary>
        /// <param name="bindings">The packed joints-weights.</param>
        void SetBindings(in SparseWeight8 bindings);

        /// <summary>
        /// Sets the packed joints-weights.
        /// <para><b>⚠️ USE ONLY ON UNBOXED VALUES ⚠️</b></para>
        /// </summary>
        /// <param name="bindings">the list of joint indices and weights.</param>
        void SetBindings(params (int Index, float Weight)[] bindings);

        /// <summary>
        /// Gets the packed joints-weights.
        /// </summary>
        /// <returns>The packed joints-weights.</returns>
        SparseWeight8 GetBindings();

        /// <summary>
        /// Gets the indices of the first 4 joints.
        /// </summary>
        Vector4 JointsLow { get; }

        /// <summary>
        /// Gets the indices of the next 4 joints, if supported.
        /// </summary>
        Vector4 JointsHigh { get; }

        /// <summary>
        /// Gets the weights of the first 4 joints.
        /// </summary>
        Vector4 WeightsLow { get; }

        /// <summary>
        /// Gets the weights of the next 4 joints, if supported.
        /// </summary>
        Vector4 WeightsHigh { get; }
    }

    /// <summary>
    /// Defines a Vertex attribute with up to 65535 bone joints and 4 weights.
    /// </summary>
    [System.Diagnostics.DebuggerDisplay("{_GetDebuggerDisplay(),nq}")]
    public struct VertexJoints4 : IVertexSkinning, IEquatable<VertexJoints4>
    {
        #region debug

        private string _GetDebuggerDisplay() => VertexUtils._GetDebuggerDisplay(this);

        #endregion

        #region constructors

        public VertexJoints4(int jointIndex)
        {
            Joints = new Vector4(jointIndex, 0, 0, 0);
            Weights = Vector4.UnitX;
        }

        public VertexJoints4(params (int JointIndex, float Weight)[] bindings)
            : this( SparseWeight8.Create(bindings) ) { }

        public VertexJoints4(in SparseWeight8 weights)
        {
            var ordered = SparseWeight8.OrderedByWeight(weights);

            Joints = new Vector4(ordered.Index0, ordered.Index1, ordered.Index2, ordered.Index3);
            Weights = new Vector4(ordered.Weight0, ordered.Weight1, ordered.Weight2, ordered.Weight3);

            // renormalize
            var w = Vector4.Dot(Weights, Vector4.One);
            if (w != 0 && w != 1) Weights /= w;

            // we must be sure that pairs are sorted by weight
            System.Diagnostics.Debug.Assert(Weights.X >= Weights.Y);
            System.Diagnostics.Debug.Assert(Weights.Y >= Weights.Z);
            System.Diagnostics.Debug.Assert(Weights.Z >= Weights.W);
        }

        #endregion

        #region data

        /// <summary>
        /// Stores the indices of the 4 joints.
        /// </summary>
        /// <remarks>
        /// <para><b>⚠️ AVOID SETTING THIS VALUE DIRECTLY ⚠️</b></para>
        /// Consider using the constructor, or setter methods like<see cref="SetBindings(in SparseWeight8)"/> instead of setting this value directly.
        /// </remarks>
        [VertexAttribute("JOINTS_0", ENCODING.UNSIGNED_SHORT, false)]
        public Vector4 Joints;

        /// <summary>
        /// Stores the weights of the 4 joints.
        /// </summary>
        /// <remarks>
        /// <para><b>⚠️ AVOID SETTING THIS VALUE DIRECTLY ⚠️</b></para>
        /// Consider using the constructor, or setter methods like <see cref="SetBindings(in SparseWeight8)"/> instead of setting this value directly.
        /// </remarks>
        [VertexAttribute("WEIGHTS_0")]
        public Vector4 Weights;

        /// <inheritdoc/>
        public readonly int MaxBindings => 4;

        /// <inheritdoc/>
        public readonly override int GetHashCode() { return Joints.GetHashCode(); }

        /// <inheritdoc/>
        public readonly override bool Equals(object obj) { return obj is VertexJoints4 other && AreEqual(this, other); }

        /// <inheritdoc/>
        public readonly bool Equals(VertexJoints4 other) { return AreEqual(this, other); }
        public static bool operator ==(in VertexJoints4 a, in VertexJoints4 b) { return AreEqual(a, b); }
        public static bool operator !=(in VertexJoints4 a, in VertexJoints4 b) { return !AreEqual(a, b); }
        public static bool AreEqual(in VertexJoints4 a, in VertexJoints4 b)
        {
            // technically we should compare index-weights pairs,
            // but it's expensive, and these values are expected
            // to be already sorted by weight, unless filled manually.

            return a.Joints == b.Joints && a.Weights == b.Weights;
        }        

        #endregion

        #region properties

        [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)]
        Vector4 IVertexSkinning.JointsLow => this.Joints;
        [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)]
        Vector4 IVertexSkinning.JointsHigh => Vector4.Zero;
        [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)]
        Vector4 IVertexSkinning.WeightsLow => this.Weights;
        [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)]
        Vector4 IVertexSkinning.WeightsHigh => Vector4.Zero;

        #endregion

        #region API

        /// <inheritdoc/>
        public readonly SparseWeight8 GetBindings() { return SparseWeight8.Create(this.Joints, this.Weights); }

        /// <inheritdoc/>
        public void SetBindings(in SparseWeight8 bindings) { this = new VertexJoints4(bindings); }

        /// <inheritdoc/>
        public void SetBindings(params (int Index, float Weight)[] bindings) { this = new VertexJoints4(bindings); }

        /// <inheritdoc/>
        public readonly (int Index, float Weight) GetBinding(int index)
        {
            switch (index)
            {
                case 0: return ((int)this.Joints.X, this.Weights.X);
                case 1: return ((int)this.Joints.Y, this.Weights.Y);
                case 2: return ((int)this.Joints.Z, this.Weights.Z);
                case 3: return ((int)this.Joints.W, this.Weights.W);
                default: throw new ArgumentOutOfRangeException(nameof(index));
            }
        }

        #endregion
    }

    /// <summary>
    /// Defines a Vertex attribute with up to 65535 bone joints and 8 weights.
    /// </summary>
    [System.Diagnostics.DebuggerDisplay("{_GetDebuggerDisplay(),nq}")]
    public struct VertexJoints8 : IVertexSkinning, IEquatable<VertexJoints8>
    {
        #region debug

        private readonly string _GetDebuggerDisplay() => VertexUtils._GetDebuggerDisplay(this);

        #endregion

        #region constructors

        public VertexJoints8(int jointIndex)
        {
            Joints0 = new Vector4(jointIndex, 0, 0, 0);
            Joints1 = Vector4.Zero;
            Weights0 = Vector4.UnitX;
            Weights1 = Vector4.Zero;
        }

        public VertexJoints8(params (int JointIndex, float Weight)[] bindings)
            : this(SparseWeight8.Create(bindings)) { }

        public VertexJoints8(in SparseWeight8 weights)
        {
            var ordered = SparseWeight8.OrderedByWeight(weights);

            Joints0 = new Vector4(ordered.Index0, ordered.Index1, ordered.Index2, ordered.Index3);
            Joints1 = new Vector4(ordered.Index4, ordered.Index5, ordered.Index6, ordered.Index7);
            Weights0 = new Vector4(ordered.Weight0, ordered.Weight1, ordered.Weight2, ordered.Weight3);
            Weights1 = new Vector4(ordered.Weight4, ordered.Weight5, ordered.Weight6, ordered.Weight7);

            // renormalize
            var w = Vector4.Dot(Weights0, Vector4.One) + Vector4.Dot(Weights1, Vector4.One);
            if (w != 0 && w != 1) { Weights0 /= w; Weights1 /= w; }

            // we must be sure that pairs are sorted by weight
            System.Diagnostics.Debug.Assert(Weights0.X >= Weights0.Y);
            System.Diagnostics.Debug.Assert(Weights0.Y >= Weights0.Z);
            System.Diagnostics.Debug.Assert(Weights0.Z >= Weights0.W);
            System.Diagnostics.Debug.Assert(Weights0.W >= Weights1.X);
            System.Diagnostics.Debug.Assert(Weights1.X >= Weights1.Y);
            System.Diagnostics.Debug.Assert(Weights1.Y >= Weights1.Z);
            System.Diagnostics.Debug.Assert(Weights1.Z >= Weights1.W);
        }

        #endregion

        #region data

        /// <summary>
        /// Stores the indices of the first 4 joints.
        /// </summary>
        /// <remarks>
        /// <para><b>⚠️ AVOID SETTING THIS VALUE DIRECTLY ⚠️</b></para>
        /// Consider using the constructor, or setter methods like <see cref="SetBindings(in SparseWeight8)"/> instead of setting this value directly.
        /// </remarks>
        [VertexAttribute("JOINTS_0", ENCODING.UNSIGNED_SHORT, false)]
        public Vector4 Joints0;

        /// <summary>
        /// Stores the indices of the next 4 joints.
        /// </summary>
        /// <remarks>
        /// <para><b>⚠️ AVOID SETTING THIS VALUE DIRECTLY ⚠️</b></para>
        /// Consider using the constructor, or setter methods like <see cref="SetBindings(in SparseWeight8)"/> instead of setting this value directly.
        /// </remarks>
        [VertexAttribute("JOINTS_1", ENCODING.UNSIGNED_SHORT, false)]
        public Vector4 Joints1;

        /// <summary>
        /// Stores the weights of the first 4 joints.
        /// </summary>
        /// <remarks>
        /// <para><b>⚠️ AVOID SETTING THESE VALUES DIRECTLY ⚠️</b></para>
        /// Consider using the constructor, or setter methods like <see cref="SetBindings(in SparseWeight8)"/> instead of setting this value directly.
        /// </remarks>
        [VertexAttribute("WEIGHTS_0")]
        public Vector4 Weights0;

        /// <summary>
        /// Stores the weights of the next 4 joints.
        /// </summary>
        /// <remarks>
        /// <para><b>⚠️ AVOID SETTING THESE VALUES DIRECTLY ⚠️</b></para>
        /// Consider using the constructor, or setter methods like <see cref="SetBindings(in SparseWeight8)"/> instead of setting this value directly.
        /// </remarks>
        [VertexAttribute("WEIGHTS_1")]
        public Vector4 Weights1;

        /// <inheritdoc/>
        public readonly int MaxBindings => 8;

        /// <inheritdoc/>
        public readonly override int GetHashCode() { return Joints0.GetHashCode(); }

        /// <inheritdoc/>
        public readonly override bool Equals(object obj) { return obj is VertexJoints8 other && AreEqual(this, other); }

        /// <inheritdoc/>
        public readonly bool Equals(VertexJoints8 other) { return AreEqual(this, other); }
        public static bool operator ==(in VertexJoints8 a, in VertexJoints8 b) { return AreEqual(a, b); }
        public static bool operator !=(in VertexJoints8 a, in VertexJoints8 b) { return !AreEqual(a, b); }
        public static bool AreEqual(in VertexJoints8 a, in VertexJoints8 b)
        {
            // technically we should compare index-weights pairs,
            // but it's expensive, and these values are expected
            // to be already sorted by weight, unless filled manually.

            return a.Joints0 == b.Joints0 && a.Joints1 == b.Joints1
                && a.Weights0 == b.Weights0 && a.Weights1 == b.Weights1;
        }

        #endregion

        #region properties

        [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)]
        readonly Vector4 IVertexSkinning.JointsLow => this.Joints0;
        [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)]
        readonly Vector4 IVertexSkinning.JointsHigh => this.Joints1;
        [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)]
        readonly Vector4 IVertexSkinning.WeightsLow => this.Weights0;
        [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)]
        readonly Vector4 IVertexSkinning.WeightsHigh => this.Weights1;

        #endregion

        #region API

        /// <inheritdoc/>
        public readonly SparseWeight8 GetBindings() { return SparseWeight8.CreateUnchecked(this.Joints0, this.Joints1, this.Weights0, this.Weights1); }

        /// <inheritdoc/>
        public void SetBindings(in SparseWeight8 weights) { this = new VertexJoints8(weights); }

        /// <inheritdoc/>
        public void SetBindings(params (int Index, float Weight)[] bindings) { this = new VertexJoints8(bindings); }

        /// <inheritdoc/>
        public readonly (int Index, float Weight) GetBinding(int index)
        {
            switch (index)
            {
                case 0: return ((int)this.Joints0.X, this.Weights0.X);
                case 1: return ((int)this.Joints0.Y, this.Weights0.Y);
                case 2: return ((int)this.Joints0.Z, this.Weights0.Z);
                case 3: return ((int)this.Joints0.W, this.Weights0.W);
                case 4: return ((int)this.Joints1.X, this.Weights1.X);
                case 5: return ((int)this.Joints1.Y, this.Weights1.Y);
                case 6: return ((int)this.Joints1.Z, this.Weights1.Z);
                case 7: return ((int)this.Joints1.W, this.Weights1.W);
                default: throw new ArgumentOutOfRangeException(nameof(index));
            }
        }

        #endregion
    }
}
