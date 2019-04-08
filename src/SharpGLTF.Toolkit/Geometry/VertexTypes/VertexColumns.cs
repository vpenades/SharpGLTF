using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace SharpGLTF.Geometry.VertexTypes
{
    /// <summary>
    /// Represents a vertex buffer, where every vertex attribute is represented as a vector column.
    /// </summary>
    public class VertexColumns
    {
        #region columns

        public IList<Vector3> Positions { get; set; }
        public IList<Vector3> Normals { get; set; }
        public IList<Vector4> Tangents { get; set; }

        public IList<Vector4> Colors0 { get; set; }
        public IList<Vector4> Colors1 { get; set; }

        public IList<Vector2> Textures0 { get; set; }
        public IList<Vector2> Textures1 { get; set; }

        public IList<Vector4> Joints0 { get; set; }
        public IList<Vector4> Joints1 { get; set; }

        public IList<Vector4> Weights0 { get; set; }
        public IList<Vector4> Weights1 { get; set; }

        #endregion

        #region API

        public void SetNormals(IReadOnlyDictionary<Vector3, Vector3> normalsMap)
        {
            var data = new Byte[12 * Positions.Count];

            Normals = new Memory.Vector3Array(data, 0, Positions.Count, 0);

            for (int i = 0; i < Normals.Count; ++i)
            {
                Normals[i] = normalsMap[Positions[i]];
            }
        }

        public TvP GetPositionFragment<TvP>(int index)
            where TvP : struct, IVertexPosition
        {
            var pnt = default(TvP);

            if (Positions != null) pnt.SetPosition(Positions[index]);
            if (Normals != null) pnt.SetNormal(Normals[index]);
            if (Tangents != null) pnt.SetTangent(Tangents[index]);

            return pnt;
        }

        public TvM GetMaterialFragment<TvM>(int index)
            where TvM : struct, IVertexMaterial
        {
            var cctt = default(TvM);

            if (Colors0 != null && cctt.MaxColors > 0) cctt.SetColor(0, Colors0[index]);
            if (Colors1 != null && cctt.MaxColors > 1) cctt.SetColor(1, Colors1[index]);

            if (Textures0 != null && cctt.MaxTextures > 0) cctt.SetTexCoord(0, Textures0[index]);
            if (Textures1 != null && cctt.MaxTextures > 1) cctt.SetTexCoord(1, Textures1[index]);

            return cctt;
        }

        public TvS GetSkinningFragment<TvS>(int index)
            where TvS : struct, IVertexSkinning
        {
            var jjjj = default(TvS);

            if (Joints0 != null && Weights0 != null)
            {
                var j = Joints0[index];
                var w = Weights0[index];

                jjjj.SetJoint(0, (int)j.X, w.X);
                jjjj.SetJoint(1, (int)j.Y, w.Y);
                jjjj.SetJoint(2, (int)j.Z, w.Z);
                jjjj.SetJoint(3, (int)j.W, w.W);
            }

            if (Joints1 != null && Weights1 != null)
            {
                var j = Joints1[index];
                var w = Weights1[index];

                jjjj.SetJoint(4, (int)j.X, w.X);
                jjjj.SetJoint(5, (int)j.Y, w.Y);
                jjjj.SetJoint(6, (int)j.Z, w.Z);
                jjjj.SetJoint(7, (int)j.W, w.W);
            }

            return jjjj;
        }

        #endregion
    }
}