using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;
using System.Linq;

using SharpGLTF.Geometry.VertexTypes;

namespace SharpGLTF.Geometry
{
    /// <summary>
    /// Represents a vertex buffer, where every vertex attribute is represented as a vector column.
    /// </summary>
    /// <remarks>
    /// One of the use cases of <see cref="VertexColumns"/> is to bind the different attribute
    /// columns directly to the <see cref="Schema2.Accessor"/> source feed, which means that
    /// if you modify the contents of a column that is binded directly to a model, you're
    /// modifying the model's internal data.
    /// </remarks>
    public class VertexColumns
    {
        #region Data Columns

#pragma warning disable CA2227 // Collection properties should be read only

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

        public class MorphTarget
        {
            public IList<Vector3> Positions { get; set; }
            public IList<Vector3> Normals { get; set; }
            public IList<Vector3> Tangents { get; set; }
        }

#pragma warning restore CA2227 // Collection properties should be read only

        private List<MorphTarget> _MorphTargets;

        private static readonly IReadOnlyList<MorphTarget> _EmptyMorphTargets = new MorphTarget[0];

        public IReadOnlyList<MorphTarget> MorphTargets => _MorphTargets == null ? _EmptyMorphTargets : _MorphTargets;

        #endregion

        #region API

        /// <summary>
        /// Creates an internal copy of the <see cref="Positions"/> columns,
        /// which ensures the column data is not shared with other objects
        /// </summary>
        public void IsolatePositions()
        {
            if (this.Positions == null) return;

            var newPositions = new Vector3[this.Positions.Count];

            this.Positions.CopyTo(newPositions, 0);

            this.Positions = newPositions;
        }

        /// <summary>
        /// Creates an internal copy of the <see cref="Normals"/> columns,
        /// which ensures the column data is not shared with other objects
        /// </summary>
        public void IsolateNormals()
        {
            if (this.Normals == null) return;

            var newNormals = new Vector3[this.Normals.Count];

            this.Normals.CopyTo(newNormals, 0);

            this.Normals = newNormals;
        }

        /// <summary>
        /// Creates an internal copy of the <see cref="Tangents"/> columns,
        /// which ensures the column data is not shared with other objects
        /// </summary>
        public void IsolateTangents()
        {
            if (this.Tangents == null) return;

            var newTangents = new Vector4[this.Tangents.Count];

            this.Tangents.CopyTo(newTangents, 0);

            this.Tangents = newTangents;
        }

        public void ApplyNormals(IReadOnlyDictionary<Vector3, Vector3> normalsMap)
        {
            IsolateNormals();

            if (this.Normals == null) this.Normals = new Vector3[Positions.Count];

            for (int i = 0; i < Normals.Count; ++i)
            {
                if (normalsMap.TryGetValue(this.Positions[i], out Vector3 nrm))
                {
                    this.Normals[i] = nrm;
                }
            }
        }

        public void ApplyTransform(Transforms.ITransform transform)
        {
            // since positions, normals and tangents might be binded directly to the model's buffer data,
            // and we don't want to modify the source data, we create a copy of the columns.

            IsolatePositions();
            IsolateNormals();
            IsolateTangents();

            // prepare morph data, if available

            Vector3[] morphPositions = null;
            Vector3[] morphNormals = null;
            Vector3[] morphTangents = null;

            if (_MorphTargets != null)
            {
                if (_MorphTargets.All(item => item.Positions != null)) morphPositions = new Vector3[this.MorphTargets.Count];
                if (_MorphTargets.All(item => item.Normals != null)) morphNormals = new Vector3[this.MorphTargets.Count];
                if (_MorphTargets.All(item => item.Tangents != null)) morphTangents = new Vector3[this.MorphTargets.Count];
            }

            // prepare skinning data, if available

            var jw0 = Joints0 != null && Weights0 != null;
            var jw1 = Joints1 != null && Weights1 != null;

            var jwjwjwjw = new (int, float)[(jw0 ? 4 : 0) + (jw1 ? 4 : 0)];

            int vcount = Positions.Count;

            for (int i = 0; i < vcount; ++i)
            {
                if (jw0)
                {
                    var j = Joints0[i];
                    var w = Weights0[i];
                    jwjwjwjw[0] = ((int)j.X, w.X);
                    jwjwjwjw[1] = ((int)j.Y, w.Y);
                    jwjwjwjw[2] = ((int)j.Z, w.Z);
                    jwjwjwjw[3] = ((int)j.W, w.W);
                }

                if (jw1)
                {
                    var j = Joints1[i];
                    var w = Weights1[i];
                    jwjwjwjw[4] = ((int)j.X, w.X);
                    jwjwjwjw[5] = ((int)j.Y, w.Y);
                    jwjwjwjw[6] = ((int)j.Z, w.Z);
                    jwjwjwjw[7] = ((int)j.W, w.W);
                }

                if (Positions != null)
                {
                    _FillMorphData(morphPositions, vc => vc.Positions[i]);
                    Positions[i] = transform.TransformPosition(Positions[i], morphPositions, jwjwjwjw);
                }

                if (Normals != null)
                {
                    _FillMorphData(morphNormals, vc => vc.Normals[i]);
                    Normals[i] = transform.TransformNormal(Normals[i], morphNormals, jwjwjwjw);
                }

                if (Tangents != null)
                {
                    _FillMorphData(morphTangents, vc => vc.Tangents[i]);
                    Tangents[i] = transform.TransformTangent(Tangents[i], morphTangents, jwjwjwjw);
                }
            }

            // we've just applied the transform,
            // so we no longer need these columns.

            _MorphTargets = null;

            Joints0 = null;
            Joints1 = null;
            Weights0 = null;
            Weights1 = null;
        }

        private void _FillMorphData(Vector3[] array, Func<MorphTarget, Vector3> selector)
        {
            if (array == null) return;

            for (int i = 0; i < this._MorphTargets.Count; ++i)
            {
                array[i] = selector(this._MorphTargets[i]);
            }
        }

        public TvG GetVertexGeometry<TvG>(int index)
            where TvG : struct, IVertexGeometry
        {
            var pnt = default(TvG);

            if (Positions != null) pnt.SetPosition(Positions[index]);
            if (Normals != null) pnt.SetNormal(Normals[index]);
            if (Tangents != null) pnt.SetTangent(Tangents[index]);

            return pnt;
        }

        public TvM GetVertexMaterial<TvM>(int index)
            where TvM : struct, IVertexMaterial
        {
            var cctt = default(TvM);

            if (Colors0 != null && cctt.MaxColors > 0) cctt.SetColor(0, Colors0[index]);
            if (Colors1 != null && cctt.MaxColors > 1) cctt.SetColor(1, Colors1[index]);

            if (Textures0 != null && cctt.MaxTextCoords > 0) cctt.SetTexCoord(0, Textures0[index]);
            if (Textures1 != null && cctt.MaxTextCoords > 1) cctt.SetTexCoord(1, Textures1[index]);

            return cctt;
        }

        public TvS GetVertexSkinning<TvS>(int index)
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

        public VertexBuilder<TvG, TvM, VertexEmpty> GetVertex<TvG, TvM>(int index)
            where TvG : struct, IVertexGeometry
            where TvM : struct, IVertexMaterial
        {
            return new VertexBuilder<TvG, TvM, VertexEmpty>
                (
                GetVertexGeometry<TvG>(index),
                GetVertexMaterial<TvM>(index)
                );
        }

        public VertexBuilder<TvG, TvM, TvS> GetVertex<TvG, TvM, TvS>(int index)
            where TvG : struct, IVertexGeometry
            where TvM : struct, IVertexMaterial
            where TvS : struct, IVertexSkinning
        {
            return new VertexBuilder<TvG, TvM, TvS>
                (
                GetVertexGeometry<TvG>(index),
                GetVertexMaterial<TvM>(index),
                GetVertexSkinning<TvS>(index)
                );
        }

        public MorphTarget AddMorphTarget()
        {
            if (_MorphTargets == null) _MorphTargets = new List<MorphTarget>();
            var mt = new MorphTarget();
            _MorphTargets.Add(mt);

            return mt;
        }

        #endregion
    }
}