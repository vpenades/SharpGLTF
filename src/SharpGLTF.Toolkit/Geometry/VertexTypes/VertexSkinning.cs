using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;
using SharpGLTF.Transforms;

using ENCODING = SharpGLTF.Schema2.EncodingType;

namespace SharpGLTF.Geometry.VertexTypes
{
    public interface IVertexSkinning
    {
        int MaxBindings { get; }

        void Validate();

        (int Index, float Weight) GetJointBinding(int index);
        void SetJointBinding(int index, int joint, float weight);

        void SetWeights(in SparseWeight8 weights);

        SparseWeight8 GetWeights();

        Vector4 JointsLow { get; }
        Vector4 JointsHigh { get; }

        Vector4 WeightsLow { get; }
        Vector4 WeightsHigh { get; }
    }

    /// <summary>
    /// Defines a Vertex attribute with up to 65535 bone joints and 4 weights.
    /// </summary>
    public struct VertexJoints4 : IVertexSkinning
    {
        #region constructors

        public VertexJoints4(int jointIndex)
        {
            Joints = new Vector4(jointIndex, 0, 0, 0);
            Weights = Vector4.UnitX;
        }

        public VertexJoints4(params (int, float)[] bindings)
            : this( SparseWeight8.Create(bindings) ) { }

        public VertexJoints4(in SparseWeight8 weights)
        {
            var w4 = SparseWeight8.OrderedByWeight(weights);

            Joints = new Vector4(w4.Index0, w4.Index1, w4.Index2, w4.Index3);
            Weights = new Vector4(w4.Weight0, w4.Weight1, w4.Weight2, w4.Weight3);

            // renormalize
            var w = Vector4.Dot(Weights, Vector4.One);
            if (w != 0 && w != 1) Weights /= w;
        }

        #endregion

        #region data

        [VertexAttribute("JOINTS_0", ENCODING.UNSIGNED_SHORT, false)]
        public Vector4 Joints;

        [VertexAttribute("WEIGHTS_0")]
        public Vector4 Weights;

        public int MaxBindings => 4;

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

        public void Validate() { FragmentPreprocessors.ValidateVertexSkinning(this); }

        public SparseWeight8 GetWeights() { return new SparseWeight8(this.Joints, this.Weights); }

        public void SetWeights(in SparseWeight8 weights) { this = new VertexJoints4(weights); }

        public (int, float) GetJointBinding(int index)
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

        public void SetJointBinding(int index, int joint, float weight)
        {
            switch (index)
            {
                case 0: { this.Joints.X = joint; this.Weights.X = weight; return; }
                case 1: { this.Joints.Y = joint; this.Weights.Y = weight; return; }
                case 2: { this.Joints.Z = joint; this.Weights.Z = weight; return; }
                case 3: { this.Joints.W = joint; this.Weights.W = weight; return; }
                default: throw new ArgumentOutOfRangeException(nameof(index));
            }
        }

        public void InPlaceSort()
        {
            var sparse = new SparseWeight8(this.Joints, this.Weights);
            this = new VertexJoints4(sparse);
        }

        #endregion
    }

    /// <summary>
    /// Defines a Vertex attribute with up to 65535 bone joints and 8 weights.
    /// </summary>
    public struct VertexJoints8 : IVertexSkinning
    {
        #region constructors

        public VertexJoints8(int jointIndex)
        {
            Joints0 = new Vector4(jointIndex, 0, 0, 0);
            Joints1 = Vector4.Zero;
            Weights0 = Vector4.UnitX;
            Weights1 = Vector4.Zero;
        }

        public VertexJoints8(params (int, float)[] bindings)
            : this(SparseWeight8.Create(bindings)) { }

        public VertexJoints8(in SparseWeight8 weights)
        {
            var w8 = SparseWeight8.OrderedByWeight(weights);

            Joints0 = new Vector4(w8.Index0, w8.Index1, w8.Index2, w8.Index3);
            Joints1 = new Vector4(w8.Index4, w8.Index5, w8.Index6, w8.Index7);
            Weights0 = new Vector4(w8.Weight0, w8.Weight1, w8.Weight2, w8.Weight3);
            Weights1 = new Vector4(w8.Weight4, w8.Weight5, w8.Weight6, w8.Weight7);

            // renormalize
            var w = Vector4.Dot(Weights0, Vector4.One) + Vector4.Dot(Weights1, Vector4.One);
            if (w != 0 && w != 1) { Weights0 /= w; Weights1 /= w; }
        }

        #endregion

        #region data

        [VertexAttribute("JOINTS_0", ENCODING.UNSIGNED_SHORT, false)]
        public Vector4 Joints0;

        [VertexAttribute("JOINTS_1", ENCODING.UNSIGNED_SHORT, false)]
        public Vector4 Joints1;

        [VertexAttribute("WEIGHTS_0")]
        public Vector4 Weights0;

        [VertexAttribute("WEIGHTS_1")]
        public Vector4 Weights1;

        public int MaxBindings => 8;

        #endregion

        #region properties

        [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)]
        Vector4 IVertexSkinning.JointsLow => this.Joints0;
        [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)]
        Vector4 IVertexSkinning.JointsHigh => this.Joints1;
        [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)]
        Vector4 IVertexSkinning.WeightsLow => this.Weights0;
        [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)]
        Vector4 IVertexSkinning.WeightsHigh => this.Joints1;

        #endregion

        #region API

        public void Validate() { FragmentPreprocessors.ValidateVertexSkinning(this); }

        public SparseWeight8 GetWeights() { return new SparseWeight8(this.Joints0, this.Joints1, this.Weights0, this.Weights1); }

        public void SetWeights(in SparseWeight8 weights) { this = new VertexJoints8(weights); }

        public (int Index, float Weight) GetJointBinding(int index)
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

        public void SetJointBinding(int index, int joint, float weight)
        {
            switch (index)
            {
                case 0: { this.Joints0.X = joint; this.Weights0.X = weight; return; }
                case 1: { this.Joints0.Y = joint; this.Weights0.Y = weight; return; }
                case 2: { this.Joints0.Z = joint; this.Weights0.Z = weight; return; }
                case 3: { this.Joints0.W = joint; this.Weights0.W = weight; return; }
                case 4: { this.Joints1.X = joint; this.Weights1.X = weight; return; }
                case 5: { this.Joints1.Y = joint; this.Weights1.Y = weight; return; }
                case 6: { this.Joints1.Z = joint; this.Weights1.Z = weight; return; }
                case 7: { this.Joints1.W = joint; this.Weights1.W = weight; return; }
                default: throw new ArgumentOutOfRangeException(nameof(index));
            }
        }

        #endregion
    }
}
