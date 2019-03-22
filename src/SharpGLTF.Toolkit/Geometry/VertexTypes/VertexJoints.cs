using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace SharpGLTF.Geometry.VertexTypes
{
    public interface IVertexJoints
    {
        void Validate();
    }

    public struct VertexJoints4 : IVertexJoints
    {
        public VertexJoints4(int jointIndex)
        {
            Joints = new Vector4(jointIndex);
            Weights = Vector4.UnitX;
        }

        public VertexJoints4(int jointIndex1, int jointIndex2)
        {
            Joints = new Vector4(jointIndex1, jointIndex2, 0, 0);
            Weights = new Vector4(0.5f, 0.5f, 0, 0);
        }

        [VertexAttribute("JOINTS_0", Schema2.EncodingType.UNSIGNED_BYTE, false)]
        public Vector4 Joints;

        [VertexAttribute("WEIGHTS_0", Schema2.EncodingType.UNSIGNED_BYTE, true)]
        public Vector4 Weights;

        public void Validate()
        {
            if (!Joints._IsReal()) throw new NotFiniteNumberException(nameof(Joints));
            if (!Weights._IsReal()) throw new NotFiniteNumberException(nameof(Weights));
        }
    }

    public struct VertexJoints8 : IVertexJoints
    {
        public VertexJoints8(int jointIndex)
        {
            Joints0 = new Vector4(jointIndex);
            Joints1 = new Vector4(jointIndex);
            Weights0 = Vector4.UnitX;
            Weights1 = Vector4.Zero;
        }

        public VertexJoints8(int jointIndex1, int jointIndex2)
        {
            Joints0 = new Vector4(jointIndex1, jointIndex2, 0, 0);
            Joints1 = Vector4.Zero;
            Weights0 = new Vector4(0.5f, 0.5f, 0, 0);
            Weights1 = Vector4.Zero;
        }

        [VertexAttribute("JOINTS_0", Schema2.EncodingType.UNSIGNED_BYTE, false)]
        public Vector4 Joints0;

        [VertexAttribute("JOINTS_1", Schema2.EncodingType.UNSIGNED_BYTE, false)]
        public Vector4 Joints1;

        [VertexAttribute("WEIGHTS_0", Schema2.EncodingType.UNSIGNED_BYTE, true)]
        public Vector4 Weights0;

        [VertexAttribute("WEIGHTS_1", Schema2.EncodingType.UNSIGNED_BYTE, true)]
        public Vector4 Weights1;

        public void Validate()
        {
            if (!Joints0._IsReal()) throw new NotFiniteNumberException(nameof(Joints0));
            if (!Joints1._IsReal()) throw new NotFiniteNumberException(nameof(Joints1));

            if (!Weights0._IsReal()) throw new NotFiniteNumberException(nameof(Weights0));
            if (!Weights1._IsReal()) throw new NotFiniteNumberException(nameof(Weights1));
        }
    }
}
