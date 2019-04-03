using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace SharpGLTF.Geometry.VertexTypes
{
    public interface IVertexSkinning
    {
        void SetJoints(int jointSet, Vector4 joints, Vector4 weights);

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

        #endregion

        #region API

        void IVertexSkinning.SetJoints(int jointSet, Vector4 joints, Vector4 weights)
        {
            if (jointSet == 0) { this.Joints = joints; this.Weights = weights; }
        }

        public void Validate()
        {
            if (!Joints._IsReal()) throw new NotFiniteNumberException(nameof(Joints));
            if (!Joints.IsRound() || !Joints.IsInRange(Vector4.Zero, new Vector4(255))) throw new IndexOutOfRangeException(nameof(Joints));

            if (!Weights._IsReal()) throw new NotFiniteNumberException(nameof(Weights));
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

        #endregion
    }
}
