using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace SharpGLTF.Geometry.VertexTypes
{
    using JOINTWEIGHT = KeyValuePair<int, float>;

    public interface IVertexSkinning
    {
        void SetJoints(int jointSet, Vector4 joints, Vector4 weights);

        int MaxJoints { get; }

        JOINTWEIGHT GetJoint(int index);

        void SetJoint(int index, JOINTWEIGHT jw);

        void AssignFrom(IVertexSkinning vertex);

        // TODO: validation must ensure that:
        // - there's some positive weight
        // - every joint is unique
        void Validate();
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

        void IVertexSkinning.SetJoints(int jointSet, Vector4 joints, Vector4 weights)
        {
            if (jointSet == 0) { this.Joints = joints; this.Weights = weights; }
        }

        public void AssignFrom(IVertexSkinning vertex)
        {
            var c = Math.Min(this.MaxJoints, vertex.MaxJoints);

            for (int i = 0; i < c; ++i)
            {
                var jw = vertex.GetJoint(i);
                this.SetJoint(i, jw);
            }
        }

        public void Validate()
        {
            if (!Joints._IsReal()) throw new NotFiniteNumberException(nameof(Joints));
            if (!Joints.IsRound() || !Joints.IsInRange(Vector4.Zero, new Vector4(255))) throw new IndexOutOfRangeException(nameof(Joints));

            if (!Weights._IsReal()) throw new NotFiniteNumberException(nameof(Weights));
        }

        public JOINTWEIGHT GetJoint(int index)
        {
            switch (index)
            {
                case 0: return new JOINTWEIGHT((int)this.Joints.X, this.Weights.X);
                case 1: return new JOINTWEIGHT((int)this.Joints.Y, this.Weights.Y);
                case 2: return new JOINTWEIGHT((int)this.Joints.Z, this.Weights.Z);
                case 3: return new JOINTWEIGHT((int)this.Joints.W, this.Weights.W);
                default: throw new ArgumentOutOfRangeException(nameof(index));
            }
        }

        public void SetJoint(int index, JOINTWEIGHT jw)
        {
            switch (index)
            {
                case 0: { this.Joints.X = jw.Key; this.Weights.X = jw.Value; return; }
                case 1: { this.Joints.Y = jw.Key; this.Weights.Y = jw.Value; return; }
                case 2: { this.Joints.Z = jw.Key; this.Weights.Z = jw.Value; return; }
                case 3: { this.Joints.W = jw.Key; this.Weights.W = jw.Value; return; }
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

        void IVertexSkinning.SetJoints(int jointSet, Vector4 joints, Vector4 weights)
        {
            if (jointSet == 0) { this.Joints = joints; this.Weights = weights; }
        }

        public void Validate()
        {
            if (!Joints._IsReal()) throw new NotFiniteNumberException(nameof(Joints));
            if (!Joints.IsRound() || !Joints.IsInRange(Vector4.Zero, new Vector4(65535))) throw new IndexOutOfRangeException(nameof(Joints));

            if (!Weights._IsReal()) throw new NotFiniteNumberException(nameof(Weights));
        }

        public JOINTWEIGHT GetJoint(int index)
        {
            switch (index)
            {
                case 0: return new JOINTWEIGHT((int)this.Joints.X, this.Weights.X);
                case 1: return new JOINTWEIGHT((int)this.Joints.Y, this.Weights.Y);
                case 2: return new JOINTWEIGHT((int)this.Joints.Z, this.Weights.Z);
                case 3: return new JOINTWEIGHT((int)this.Joints.W, this.Weights.W);
                default: throw new ArgumentOutOfRangeException(nameof(index));
            }
        }

        public void SetJoint(int index, JOINTWEIGHT jw)
        {
            switch (index)
            {
                case 0: { this.Joints.X = jw.Key; this.Weights.X = jw.Value; return; }
                case 1: { this.Joints.Y = jw.Key; this.Weights.Y = jw.Value; return; }
                case 2: { this.Joints.Z = jw.Key; this.Weights.Z = jw.Value; return; }
                case 3: { this.Joints.W = jw.Key; this.Weights.W = jw.Value; return; }
                default: throw new ArgumentOutOfRangeException(nameof(index));
            }
        }

        public void AssignFrom(IVertexSkinning vertex)
        {
            throw new NotImplementedException();
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

        void IVertexSkinning.SetJoints(int jointSet, Vector4 joints, Vector4 weights)
        {
            if (jointSet == 0) { this.Joints0 = joints; this.Weights0 = weights; }
            if (jointSet == 1) { this.Joints1 = joints; this.Weights1 = weights; }
        }

        public void Validate()
        {
            if (!Joints0._IsReal()) throw new NotFiniteNumberException(nameof(Joints0));
            if (!Joints1._IsReal()) throw new NotFiniteNumberException(nameof(Joints1));

            if (!Joints0.IsRound() || !Joints0.IsInRange(Vector4.Zero, new Vector4(255))) throw new IndexOutOfRangeException(nameof(Joints0));
            if (!Joints1.IsRound() || !Joints1.IsInRange(Vector4.Zero, new Vector4(255))) throw new IndexOutOfRangeException(nameof(Joints1));

            if (!Weights0._IsReal()) throw new NotFiniteNumberException(nameof(Weights0));
            if (!Weights1._IsReal()) throw new NotFiniteNumberException(nameof(Weights1));
        }

        public JOINTWEIGHT GetJoint(int index)
        {
            switch (index)
            {
                case 0: return new JOINTWEIGHT((int)this.Joints0.X, this.Weights0.X);
                case 1: return new JOINTWEIGHT((int)this.Joints0.Y, this.Weights0.Y);
                case 2: return new JOINTWEIGHT((int)this.Joints0.Z, this.Weights0.Z);
                case 3: return new JOINTWEIGHT((int)this.Joints0.W, this.Weights0.W);
                case 4: return new JOINTWEIGHT((int)this.Joints1.X, this.Weights1.X);
                case 5: return new JOINTWEIGHT((int)this.Joints1.Y, this.Weights1.Y);
                case 6: return new JOINTWEIGHT((int)this.Joints1.Z, this.Weights1.Z);
                case 7: return new JOINTWEIGHT((int)this.Joints1.W, this.Weights1.W);
                default: throw new ArgumentOutOfRangeException(nameof(index));
            }
        }

        public void SetJoint(int index, JOINTWEIGHT jw)
        {
            switch (index)
            {
                case 0: { this.Joints0.X = jw.Key; this.Weights0.X = jw.Value; return; }
                case 1: { this.Joints0.Y = jw.Key; this.Weights0.Y = jw.Value; return; }
                case 2: { this.Joints0.Z = jw.Key; this.Weights0.Z = jw.Value; return; }
                case 3: { this.Joints0.W = jw.Key; this.Weights0.W = jw.Value; return; }
                case 4: { this.Joints1.X = jw.Key; this.Weights1.X = jw.Value; return; }
                case 5: { this.Joints1.Y = jw.Key; this.Weights1.Y = jw.Value; return; }
                case 6: { this.Joints1.Z = jw.Key; this.Weights1.Z = jw.Value; return; }
                case 7: { this.Joints1.W = jw.Key; this.Weights1.W = jw.Value; return; }
                default: throw new ArgumentOutOfRangeException(nameof(index));
            }
        }

        public void AssignFrom(IVertexSkinning vertex)
        {
            throw new NotImplementedException();
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

        void IVertexSkinning.SetJoints(int jointSet, Vector4 joints, Vector4 weights)
        {
            if (jointSet == 0) { this.Joints0 = joints; this.Weights0 = weights; }
            if (jointSet == 1) { this.Joints1 = joints; this.Weights1 = weights; }
        }

        public void Validate()
        {
            if (!Joints0._IsReal()) throw new NotFiniteNumberException(nameof(Joints0));
            if (!Joints1._IsReal()) throw new NotFiniteNumberException(nameof(Joints1));

            if (!Joints0.IsRound() || !Joints0.IsInRange(Vector4.Zero, new Vector4(65535))) throw new IndexOutOfRangeException(nameof(Joints0));
            if (!Joints1.IsRound() || !Joints1.IsInRange(Vector4.Zero, new Vector4(65535))) throw new IndexOutOfRangeException(nameof(Joints1));

            if (!Weights0._IsReal()) throw new NotFiniteNumberException(nameof(Weights0));
            if (!Weights1._IsReal()) throw new NotFiniteNumberException(nameof(Weights1));
        }

        public JOINTWEIGHT GetJoint(int index)
        {
            switch (index)
            {
                case 0: return new JOINTWEIGHT((int)this.Joints0.X, this.Weights0.X);
                case 1: return new JOINTWEIGHT((int)this.Joints0.Y, this.Weights0.Y);
                case 2: return new JOINTWEIGHT((int)this.Joints0.Z, this.Weights0.Z);
                case 3: return new JOINTWEIGHT((int)this.Joints0.W, this.Weights0.W);
                case 4: return new JOINTWEIGHT((int)this.Joints1.X, this.Weights1.X);
                case 5: return new JOINTWEIGHT((int)this.Joints1.Y, this.Weights1.Y);
                case 6: return new JOINTWEIGHT((int)this.Joints1.Z, this.Weights1.Z);
                case 7: return new JOINTWEIGHT((int)this.Joints1.W, this.Weights1.W);
                default: throw new ArgumentOutOfRangeException(nameof(index));
            }
        }

        public void SetJoint(int index, JOINTWEIGHT jw)
        {
            switch (index)
            {
                case 0: { this.Joints0.X = jw.Key; this.Weights0.X = jw.Value; return; }
                case 1: { this.Joints0.Y = jw.Key; this.Weights0.Y = jw.Value; return; }
                case 2: { this.Joints0.Z = jw.Key; this.Weights0.Z = jw.Value; return; }
                case 3: { this.Joints0.W = jw.Key; this.Weights0.W = jw.Value; return; }
                case 4: { this.Joints1.X = jw.Key; this.Weights1.X = jw.Value; return; }
                case 5: { this.Joints1.Y = jw.Key; this.Weights1.Y = jw.Value; return; }
                case 6: { this.Joints1.Z = jw.Key; this.Weights1.Z = jw.Value; return; }
                case 7: { this.Joints1.W = jw.Key; this.Weights1.W = jw.Value; return; }
                default: throw new ArgumentOutOfRangeException(nameof(index));
            }
        }

        public void AssignFrom(IVertexSkinning vertex)
        {
            throw new NotImplementedException();
        }

        #endregion
    }
}
