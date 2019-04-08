using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace SharpGLTF.Geometry.VertexTypes
{
    /// <summary>
    /// Represents a a Node Joint index and its weight in a skinning system.
    /// </summary>
    [System.Diagnostics.DebuggerDisplay("{Joint} = {Weight}")]
    public struct JointWeightPair : IComparable<JointWeightPair>
    {
        public JointWeightPair(int joint, float weight)
        {
            this.Joint = joint;
            this.Weight = weight;
            if (Weight == 0) Joint = 0;
        }

        public int Joint;
        public float Weight;

        public int CompareTo(JointWeightPair other)
        {
            var a = this.Weight.CompareTo(other.Weight);
            if (a != 0) return a;

            return this.Joint.CompareTo(other.Joint);
        }

        internal static void InPlaceReverseBubbleSort(Span<JointWeightPair> span)
        {
            for (int i = 1; i < span.Length; ++i)
            {
                bool completed = true;

                for (int j = 0; j < span.Length - 1; ++j)
                {
                    if (span[j].CompareTo(span[j + 1]) < 0)
                    {
                        var tmp = span[j];
                        span[j] = span[j + 1];
                        span[j + 1 ] = tmp;
                        completed = false;
                    }
                }

                if (completed) return;
            }
        }

        /// <summary>
        /// Calculates the scale to use on the first <paramref name="count"/> weights.
        /// </summary>
        /// <param name="span">A collection of <see cref="JointWeightPair"/>.</param>
        /// <param name="count">The number of items to take from the beginning of <paramref name="span"/>.</param>
        /// <returns>A Scale factor.</returns>
        internal static float CalculateScaleFor(Span<JointWeightPair> span, int count)
        {
            System.Diagnostics.Debug.Assert(count < span.Length, nameof(count));

            float ww = 0;

            int i = 0;

            while (i < count) { ww += span[i++].Weight; }

            float w = ww;

            while (i < span.Length) { ww += span[i++].Weight; }

            return ww / w;
        }
    }

    public interface IVertexSkinning
    {
        int MaxJoints { get; }

        // TODO: validation must ensure that:
        // - there's some positive weight
        // - every joint is unique
        // - joints are sorted by weight
        // - 0 weight joints point to joint 0
        void Validate();

        JointWeightPair GetJoint(int index);

        void SetJoint(int index, int joint, float weight);
    }

    /// <summary>
    /// Defines a Vertex attribute with up to 256 bone joints and 4 weights.
    /// </summary>
    public struct VertexJoints8x4 : IVertexSkinning
    {
        #region constructors

        public VertexJoints8x4(int jointIndex)
        {
            Joints = new Vector4(jointIndex);
            Weights = Vector4.UnitX;
        }

        public VertexJoints8x4(int jointIndex1, int jointIndex2)
        {
            Joints = new Vector4(jointIndex1, jointIndex2, 0, 0);
            Weights = new Vector4(0.5f, 0.5f, 0, 0);
        }

        #endregion

        #region data

        [VertexAttribute("JOINTS_0", Schema2.EncodingType.UNSIGNED_BYTE, false)]
        public Vector4 Joints;

        [VertexAttribute("WEIGHTS_0", Schema2.EncodingType.UNSIGNED_BYTE, true)]
        public Vector4 Weights;

        public int MaxJoints => 4;

        #endregion

        #region API

        public void Validate()
        {
            if (!Joints._IsReal()) throw new NotFiniteNumberException(nameof(Joints));
            if (!Joints.IsRound() || !Joints.IsInRange(Vector4.Zero, new Vector4(255))) throw new IndexOutOfRangeException(nameof(Joints));

            if (!Weights._IsReal()) throw new NotFiniteNumberException(nameof(Weights));
        }

        public JointWeightPair GetJoint(int index)
        {
            switch (index)
            {
                case 0: return new JointWeightPair((int)this.Joints.X, this.Weights.X);
                case 1: return new JointWeightPair((int)this.Joints.Y, this.Weights.Y);
                case 2: return new JointWeightPair((int)this.Joints.Z, this.Weights.Z);
                case 3: return new JointWeightPair((int)this.Joints.W, this.Weights.W);
                default: throw new ArgumentOutOfRangeException(nameof(index));
            }
        }

        public void SetJoint(int index, int joint, float weight)
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

        #endregion
    }

    /// <summary>
    /// Defines a Vertex attribute with up to 65535 bone joints and 4 weights.
    /// </summary>
    public struct VertexJoints16x4 : IVertexSkinning
    {
        #region constructors

        public VertexJoints16x4(int jointIndex)
        {
            Joints = new Vector4(jointIndex);
            Weights = Vector4.UnitX;
        }

        public VertexJoints16x4(int jointIndex1, int jointIndex2)
        {
            Joints = new Vector4(jointIndex1, jointIndex2, 0, 0);
            Weights = new Vector4(0.5f, 0.5f, 0, 0);
        }

        #endregion

        #region data

        [VertexAttribute("JOINTS_0", Schema2.EncodingType.UNSIGNED_SHORT, false)]
        public Vector4 Joints;

        [VertexAttribute("WEIGHTS_0", Schema2.EncodingType.UNSIGNED_BYTE, true)]
        public Vector4 Weights;

        public int MaxJoints => 4;

        #endregion

        #region API

        public void Validate()
        {
            if (!Joints._IsReal()) throw new NotFiniteNumberException(nameof(Joints));
            if (!Joints.IsRound() || !Joints.IsInRange(Vector4.Zero, new Vector4(65535))) throw new IndexOutOfRangeException(nameof(Joints));

            if (!Weights._IsReal()) throw new NotFiniteNumberException(nameof(Weights));
        }

        public JointWeightPair GetJoint(int index)
        {
            switch (index)
            {
                case 0: return new JointWeightPair((int)this.Joints.X, this.Weights.X);
                case 1: return new JointWeightPair((int)this.Joints.Y, this.Weights.Y);
                case 2: return new JointWeightPair((int)this.Joints.Z, this.Weights.Z);
                case 3: return new JointWeightPair((int)this.Joints.W, this.Weights.W);
                default: throw new ArgumentOutOfRangeException(nameof(index));
            }
        }

        public void SetJoint(int index, int joint, float weight)
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

        #endregion
    }

    /// <summary>
    /// Defines a Vertex attribute with up to 256 bone joints and 8 weights.
    /// </summary>
    public struct VertexJoints8x8 : IVertexSkinning
    {
        #region constructors

        public VertexJoints8x8(int jointIndex)
        {
            Joints0 = new Vector4(jointIndex);
            Joints1 = new Vector4(jointIndex);
            Weights0 = Vector4.UnitX;
            Weights1 = Vector4.Zero;
        }

        public VertexJoints8x8(int jointIndex1, int jointIndex2)
        {
            Joints0 = new Vector4(jointIndex1, jointIndex2, 0, 0);
            Joints1 = Vector4.Zero;
            Weights0 = new Vector4(0.5f, 0.5f, 0, 0);
            Weights1 = Vector4.Zero;
        }

        #endregion

        #region data

        [VertexAttribute("JOINTS_0", Schema2.EncodingType.UNSIGNED_BYTE, false)]
        public Vector4 Joints0;

        [VertexAttribute("JOINTS_1", Schema2.EncodingType.UNSIGNED_BYTE, false)]
        public Vector4 Joints1;

        [VertexAttribute("WEIGHTS_0", Schema2.EncodingType.UNSIGNED_BYTE, true)]
        public Vector4 Weights0;

        [VertexAttribute("WEIGHTS_1", Schema2.EncodingType.UNSIGNED_BYTE, true)]
        public Vector4 Weights1;

        public int MaxJoints => 8;

        #endregion

        #region API

        public void Validate()
        {
            if (!Joints0._IsReal()) throw new NotFiniteNumberException(nameof(Joints0));
            if (!Joints1._IsReal()) throw new NotFiniteNumberException(nameof(Joints1));

            if (!Joints0.IsRound() || !Joints0.IsInRange(Vector4.Zero, new Vector4(255))) throw new IndexOutOfRangeException(nameof(Joints0));
            if (!Joints1.IsRound() || !Joints1.IsInRange(Vector4.Zero, new Vector4(255))) throw new IndexOutOfRangeException(nameof(Joints1));

            if (!Weights0._IsReal()) throw new NotFiniteNumberException(nameof(Weights0));
            if (!Weights1._IsReal()) throw new NotFiniteNumberException(nameof(Weights1));
        }

        public JointWeightPair GetJoint(int index)
        {
            switch (index)
            {
                case 0: return new JointWeightPair((int)this.Joints0.X, this.Weights0.X);
                case 1: return new JointWeightPair((int)this.Joints0.Y, this.Weights0.Y);
                case 2: return new JointWeightPair((int)this.Joints0.Z, this.Weights0.Z);
                case 3: return new JointWeightPair((int)this.Joints0.W, this.Weights0.W);
                case 4: return new JointWeightPair((int)this.Joints1.X, this.Weights1.X);
                case 5: return new JointWeightPair((int)this.Joints1.Y, this.Weights1.Y);
                case 6: return new JointWeightPair((int)this.Joints1.Z, this.Weights1.Z);
                case 7: return new JointWeightPair((int)this.Joints1.W, this.Weights1.W);
                default: throw new ArgumentOutOfRangeException(nameof(index));
            }
        }

        public void SetJoint(int index, int joint, float weight)
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

    /// <summary>
    /// Defines a Vertex attribute with up to 65535 bone joints and 8 weights.
    /// </summary>
    public struct VertexJoints16x8 : IVertexSkinning
    {
        #region constructors

        public VertexJoints16x8(int jointIndex)
        {
            Joints0 = new Vector4(jointIndex);
            Joints1 = new Vector4(jointIndex);
            Weights0 = Vector4.UnitX;
            Weights1 = Vector4.Zero;
        }

        public VertexJoints16x8(int jointIndex1, int jointIndex2)
        {
            Joints0 = new Vector4(jointIndex1, jointIndex2, 0, 0);
            Joints1 = Vector4.Zero;
            Weights0 = new Vector4(0.5f, 0.5f, 0, 0);
            Weights1 = Vector4.Zero;
        }

        #endregion

        #region data

        [VertexAttribute("JOINTS_0", Schema2.EncodingType.UNSIGNED_SHORT, false)]
        public Vector4 Joints0;

        [VertexAttribute("JOINTS_1", Schema2.EncodingType.UNSIGNED_SHORT, false)]
        public Vector4 Joints1;

        [VertexAttribute("WEIGHTS_0", Schema2.EncodingType.UNSIGNED_BYTE, true)]
        public Vector4 Weights0;

        [VertexAttribute("WEIGHTS_1", Schema2.EncodingType.UNSIGNED_BYTE, true)]
        public Vector4 Weights1;

        public int MaxJoints => 8;

        #endregion

        #region API

        public void Validate()
        {
            if (!Joints0._IsReal()) throw new NotFiniteNumberException(nameof(Joints0));
            if (!Joints1._IsReal()) throw new NotFiniteNumberException(nameof(Joints1));

            if (!Joints0.IsRound() || !Joints0.IsInRange(Vector4.Zero, new Vector4(65535))) throw new IndexOutOfRangeException(nameof(Joints0));
            if (!Joints1.IsRound() || !Joints1.IsInRange(Vector4.Zero, new Vector4(65535))) throw new IndexOutOfRangeException(nameof(Joints1));

            if (!Weights0._IsReal()) throw new NotFiniteNumberException(nameof(Weights0));
            if (!Weights1._IsReal()) throw new NotFiniteNumberException(nameof(Weights1));
        }

        public JointWeightPair GetJoint(int index)
        {
            switch (index)
            {
                case 0: return new JointWeightPair((int)this.Joints0.X, this.Weights0.X);
                case 1: return new JointWeightPair((int)this.Joints0.Y, this.Weights0.Y);
                case 2: return new JointWeightPair((int)this.Joints0.Z, this.Weights0.Z);
                case 3: return new JointWeightPair((int)this.Joints0.W, this.Weights0.W);
                case 4: return new JointWeightPair((int)this.Joints1.X, this.Weights1.X);
                case 5: return new JointWeightPair((int)this.Joints1.Y, this.Weights1.Y);
                case 6: return new JointWeightPair((int)this.Joints1.Z, this.Weights1.Z);
                case 7: return new JointWeightPair((int)this.Joints1.W, this.Weights1.W);
                default: throw new ArgumentOutOfRangeException(nameof(index));
            }
        }

        public void SetJoint(int index, int joint, float weight)
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
