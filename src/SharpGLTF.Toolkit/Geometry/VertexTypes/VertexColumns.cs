using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace SharpGLTF.Geometry.VertexTypes
{
    class VertexColumns
    {
        #region lifecycle

        public VertexColumns(IReadOnlyDictionary<string, Accessor> vertexAccessors)
        {
            if (vertexAccessors.ContainsKey("POSITION")) Positions = vertexAccessors["POSITION"].AsVector3Array();
            if (vertexAccessors.ContainsKey("NORMAL")) Normals = vertexAccessors["NORMAL"].AsVector3Array();
            if (vertexAccessors.ContainsKey("TANGENT")) Tangents = vertexAccessors["TANGENT"].AsVector4Array();

            if (vertexAccessors.ContainsKey("COLOR_0")) Colors0 = vertexAccessors["COLOR_0"].AsVector4Array();
            if (vertexAccessors.ContainsKey("COLOR_1")) Colors1 = vertexAccessors["COLOR_1"].AsVector4Array();

            if (vertexAccessors.ContainsKey("TEXCOORD_0")) Textures0 = vertexAccessors["TEXCOORD_0"].AsVector2Array();
            if (vertexAccessors.ContainsKey("TEXCOORD_1")) Textures1 = vertexAccessors["TEXCOORD_1"].AsVector2Array();

            if (vertexAccessors.ContainsKey("JOINTS_0")) Joints0 = vertexAccessors["JOINTS_0"].AsVector4Array();
            if (vertexAccessors.ContainsKey("JOINTS_1")) Joints1 = vertexAccessors["JOINTS_1"].AsVector4Array();

            if (vertexAccessors.ContainsKey("WEIGHTS_0")) Weights0 = vertexAccessors["WEIGHTS_0"].AsVector4Array();
            if (vertexAccessors.ContainsKey("WEIGHTS_1")) Weights1 = vertexAccessors["WEIGHTS_1"].AsVector4Array();
        }

        #endregion

        #region columns

        public Memory.IEncodedArray<Vector3> Positions { get; private set; }
        public Memory.IEncodedArray<Vector3> Normals { get; private set; }
        public Memory.IEncodedArray<Vector4> Tangents { get; private set; }

        public Memory.IEncodedArray<Vector4> Colors0 { get; private set; }
        public Memory.IEncodedArray<Vector4> Colors1 { get; private set; }

        public Memory.IEncodedArray<Vector2> Textures0 { get; private set; }
        public Memory.IEncodedArray<Vector2> Textures1 { get; private set; }

        public Memory.IEncodedArray<Vector4> Joints0 { get; private set; }
        public Memory.IEncodedArray<Vector4> Joints1 { get; private set; }

        public Memory.IEncodedArray<Vector4> Weights0 { get; private set; }
        public Memory.IEncodedArray<Vector4> Weights1 { get; private set; }

        #endregion
    }
}