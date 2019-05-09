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
            Normals = new Vector3[Positions.Count];

            for (int i = 0; i < Normals.Count; ++i)
            {
                if (normalsMap.TryGetValue(Positions[i], out Vector3 nrm))
                {
                    Normals[i] = nrm;
                }
            }
        }

        public void ApplyTransform(Transforms.ITransform transform)
        {
            var newPos = new Vector3[Positions.Count];
            var newNrm = Normals == null || Normals.Count < newPos.Length ? null : new Vector3[newPos.Length];
            var newTgt = Tangents == null || Tangents.Count < newPos.Length ? null : new Vector4[newPos.Length];

            var jw0 = Joints0 != null && Joints0.Count == newPos.Length && Weights0 != null && Weights0.Count == newPos.Length;
            var jw1 = Joints1 != null && Joints1.Count == newPos.Length && Weights1 != null && Weights1.Count == newPos.Length;
            var jjww = new (int, float)[8];

            for (int i = 0; i < newPos.Length; ++i)
            {
                if (jw0)
                {
                    var j = Joints0[i];
                    var w = Weights0[i];
                    jjww[0] = ((int)j.X, w.X);
                    jjww[1] = ((int)j.Y, w.Y);
                    jjww[2] = ((int)j.Z, w.Z);
                    jjww[3] = ((int)j.W, w.W);
                }

                if (jw1)
                {
                    var j = Joints1[i];
                    var w = Weights1[i];
                    jjww[4] = ((int)j.X, w.X);
                    jjww[5] = ((int)j.Y, w.Y);
                    jjww[6] = ((int)j.Z, w.Z);
                    jjww[7] = ((int)j.W, w.W);
                }

                newPos[i] = transform.TransformPosition(Positions[i], jjww);
                if (newNrm != null) newNrm[i] = transform.TransformNormal(Normals[i], jjww);
                if (newTgt != null) newTgt[i] = transform.TransformTangent(Tangents[i], jjww);
            }

            Positions = newPos;
            Normals = newNrm;
            Tangents = newTgt;

            Joints0 = null;
            Joints1 = null;
            Weights0 = null;
            Weights1 = null;
        }

        public TvP GetPositionFragment<TvP>(int index)
            where TvP : struct, IVertexGeometry
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

                jjjj.SetJointBinding(0, (int)j.X, w.X);
                jjjj.SetJointBinding(1, (int)j.Y, w.Y);
                jjjj.SetJointBinding(2, (int)j.Z, w.Z);
                jjjj.SetJointBinding(3, (int)j.W, w.W);
            }

            if (Joints1 != null && Weights1 != null)
            {
                var j = Joints1[index];
                var w = Weights1[index];

                jjjj.SetJointBinding(4, (int)j.X, w.X);
                jjjj.SetJointBinding(5, (int)j.Y, w.Y);
                jjjj.SetJointBinding(6, (int)j.Z, w.Z);
                jjjj.SetJointBinding(7, (int)j.W, w.W);
            }

            return jjjj;
        }

        #endregion
    }
}