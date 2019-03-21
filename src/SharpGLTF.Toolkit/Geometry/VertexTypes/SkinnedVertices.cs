using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace SharpGLTF.Geometry.VertexTypes
{
    public interface IJoints { }

    public struct VertexJoints0 : IJoints
    {
    }

    public struct VertexJoints4 : IJoints
    {
        public VertexJoints4(int jointIndex)
        {
            Joints_0 = new Vector4(jointIndex);
            Weights_0 = Vector4.UnitX;
        }

        public VertexJoints4(int jointIndex1, int jointIndex2)
        {
            Joints_0 = new Vector4(jointIndex1, jointIndex2, 0, 0);
            Weights_0 = new Vector4(0.5f, 0.5f, 0, 0);
        }

        [VertexAttribute("JOINTS_0", Schema2.EncodingType.UNSIGNED_BYTE, false)]
        public Vector4 Joints_0;

        [VertexAttribute("WEIGHTS_0", Schema2.EncodingType.UNSIGNED_BYTE, true)]
        public Vector4 Weights_0;
    }

    public struct VertexJoints8 : IJoints
    {
        public VertexJoints8(int jointIndex)
        {
            Joints_0 = new Vector4(jointIndex);
            Joints_1 = new Vector4(jointIndex);
            Weights_0 = Vector4.UnitX;
            Weights_1 = Vector4.Zero;
        }

        public VertexJoints8(int jointIndex1, int jointIndex2)
        {
            Joints_0 = new Vector4(jointIndex1, jointIndex2, 0, 0);
            Joints_1 = Vector4.Zero;
            Weights_0 = new Vector4(0.5f, 0.5f, 0, 0);
            Weights_1 = Vector4.Zero;
        }

        [VertexAttribute("JOINTS_0", Schema2.EncodingType.UNSIGNED_BYTE, false)]
        public Vector4 Joints_0;

        [VertexAttribute("JOINTS_1", Schema2.EncodingType.UNSIGNED_BYTE, false)]
        public Vector4 Joints_1;

        [VertexAttribute("WEIGHTS_0", Schema2.EncodingType.UNSIGNED_BYTE, true)]
        public Vector4 Weights_0;

        [VertexAttribute("WEIGHTS_1", Schema2.EncodingType.UNSIGNED_BYTE, true)]
        public Vector4 Weights_1;
    }
}
