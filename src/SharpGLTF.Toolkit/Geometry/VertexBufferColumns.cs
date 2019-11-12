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
    /// One of the use cases of <see cref="VertexBufferColumns"/> is to bind the different attribute
    /// columns directly to the <see cref="Schema2.Accessor"/> source feed, which means that
    /// if you modify the contents of a column that is binded directly to a model, you're
    /// modifying the model's internal data.
    /// </remarks>
    public class VertexBufferColumns
    {
        #region constants

        private const string ERR_COLUMNLEN = "Column length mismatch.";

        #endregion

        #region Data Columns

        #pragma warning disable CA2227 // Collection properties should be read only

        public IList<Vector3> Positions { get; set; }
        public IList<Vector3> Normals { get; set; }
        public IList<Vector4> Tangents { get; set; }

        public IList<Vector4> Colors0 { get; set; }
        public IList<Vector4> Colors1 { get; set; }

        public IList<Vector2> TexCoords0 { get; set; }
        public IList<Vector2> TexCoords1 { get; set; }

        public IList<Vector4> Joints0 { get; set; }
        public IList<Vector4> Joints1 { get; set; }

        public IList<Vector4> Weights0 { get; set; }
        public IList<Vector4> Weights1 { get; set; }

        #pragma warning restore CA2227 // Collection properties should be read only

        private List<VertexBufferColumns> _MorphTargets;

        public IReadOnlyList<VertexBufferColumns> MorphTargets => _MorphTargets == null ? (IReadOnlyList<VertexBufferColumns>)Array.Empty<VertexBufferColumns>() : _MorphTargets;

        #endregion

        #region API

        private static IList<T> _IsolateColumn<T>(IList<T> column)
        {
            if (column == null) return null;

            var newColumn = new T[column.Count];

            column.CopyTo(newColumn, 0);

            return newColumn;
        }

        /// <summary>
        /// Performs an in-place copy of the contents of every column,
        /// which guarantees that the columns of this <see cref="VertexBufferColumns"/>
        /// are not shared by any other object and can be modified safely.
        /// </summary>
        public void IsolateColumns()
        {
            this.Positions = _IsolateColumn(this.Positions);
            this.Normals = _IsolateColumn(this.Normals);
            this.Tangents = _IsolateColumn(this.Tangents);

            this.Colors0 = _IsolateColumn(this.Colors0);
            this.Colors1 = _IsolateColumn(this.Colors1);

            this.TexCoords0 = _IsolateColumn(this.TexCoords0);
            this.TexCoords1 = _IsolateColumn(this.TexCoords1);

            this.Joints0 = _IsolateColumn(this.Joints0);
            this.Joints1 = _IsolateColumn(this.Joints1);

            this.Weights0 = _IsolateColumn(this.Weights0);
            this.Weights1 = _IsolateColumn(this.Weights1);

            if (_MorphTargets == null) return;

            foreach (var mt in _MorphTargets) mt.IsolateColumns();
        }

        /// <summary>
        /// Applies a transform to the columns of this <see cref="VertexBufferColumns"/>
        /// </summary>
        /// <param name="transform">A Mesh transformer object</param>
        /// <remarks>
        /// This is a one time operation.
        /// Once it's applied, skinning and morphing columns are removed, since they're baked
        /// into the position, normal and tangent columns.
        /// </remarks>
        public void ApplyTransform(Transforms.IGeometryTransform transform)
        {
            Guard.NotNull(this.Positions, nameof(this.Positions), "Missing Positions column");
            if (this.Normals != null) Guard.IsTrue(this.Positions.Count == this.Normals.Count, nameof(this.Normals), ERR_COLUMNLEN);
            if (this.Tangents != null) Guard.IsTrue(this.Positions.Count == this.Tangents.Count, nameof(this.Tangents), ERR_COLUMNLEN);
            if (this.Colors0 != null) Guard.IsTrue(this.Positions.Count == this.Colors0.Count, nameof(this.Colors0), ERR_COLUMNLEN);
            if (this.Colors1 != null) Guard.IsTrue(this.Positions.Count == this.Colors1.Count, nameof(this.Colors1), ERR_COLUMNLEN);
            if (this.TexCoords0 != null) Guard.IsTrue(this.Positions.Count == this.TexCoords0.Count, nameof(this.TexCoords0), ERR_COLUMNLEN);
            if (this.TexCoords1 != null) Guard.IsTrue(this.Positions.Count == this.TexCoords1.Count, nameof(this.TexCoords1), ERR_COLUMNLEN);
            if (this.Joints0 != null) Guard.IsTrue(this.Positions.Count == this.Joints0.Count, nameof(this.Joints0), ERR_COLUMNLEN);
            if (this.Joints1 != null) Guard.IsTrue(this.Positions.Count == this.Joints1.Count, nameof(this.Joints1), ERR_COLUMNLEN);
            if (this.Weights0 != null) Guard.IsTrue(this.Positions.Count == this.Weights0.Count, nameof(this.Weights0), ERR_COLUMNLEN);
            if (this.Weights1 != null) Guard.IsTrue(this.Positions.Count == this.Weights1.Count, nameof(this.Weights1), ERR_COLUMNLEN);

            // since the attributes we want to overwrite might be binded directly to the model's buffer
            // data, and we don't want to modify the source data, we isolate the columns to be overwritten.

            this.Positions = _IsolateColumn(this.Positions);
            this.Normals = _IsolateColumn(this.Normals);
            this.Tangents = _IsolateColumn(this.Tangents);
            this.Colors0 = _IsolateColumn(this.Colors0);

            // prepare animation data, if available

            var skinning = default(Transforms.SparseWeight8);

            Vector3[] morphPositions = null;
            Vector3[] morphNormals = null;
            Vector3[] morphTangents = null;
            Vector4[] morphColors0 = null;

            if (_MorphTargets != null)
            {
                if (_MorphTargets.All(item => item.Positions != null)) morphPositions = new Vector3[this.MorphTargets.Count];
                if (_MorphTargets.All(item => item.Normals != null)) morphNormals = new Vector3[this.MorphTargets.Count];
                if (_MorphTargets.All(item => item.Tangents != null)) morphTangents = new Vector3[this.MorphTargets.Count];
                if (_MorphTargets.All(item => item.Colors0 != null)) morphColors0 = new Vector4[this.MorphTargets.Count];
            }

            // loop over every vertex

            int vcount = this.Positions.Count;

            for (int i = 0; i < vcount; ++i)
            {
                if (this.Joints0 != null)
                {
                    if (this.Joints1 != null) skinning = new Transforms.SparseWeight8(Joints0[i], Joints1[i], Weights0[i], Weights1[i]);
                    else skinning = new Transforms.SparseWeight8(Joints0[i], Weights0[i]);
                }

                if (this.Positions != null)
                {
                    _FillMorphData(morphPositions, vc => vc.Positions[i]);
                    Positions[i] = transform.TransformPosition(Positions[i], morphPositions, skinning);
                }

                if (this.Normals != null)
                {
                    _FillMorphData(morphNormals, vc => vc.Normals[i]);
                    Normals[i] = transform.TransformNormal(Normals[i], morphNormals, skinning);
                }

                if (this.Tangents != null)
                {
                    _FillMorphData(morphTangents, vc => vc.Tangents[i]);
                    Tangents[i] = transform.TransformTangent(Tangents[i], morphTangents, skinning);
                }

                if (this.Colors0 != null)
                {
                    _FillMorphData(morphColors0, vc => vc.Colors0[i]);
                    Colors0[i] = transform.MorphColors(Colors0[i], morphColors0);
                }
            }

            // we've just applied the transform,
            // so we clear animation columns since
            // they're irrelevant now.

            _MorphTargets = null;

            Joints0 = null;
            Joints1 = null;
            Weights0 = null;
            Weights1 = null;
        }

        private void _FillMorphData(Vector3[] array, Func<VertexBufferColumns, Vector3> selector)
        {
            if (array == null) return;

            for (int i = 0; i < this._MorphTargets.Count; ++i)
            {
                array[i] = selector(this._MorphTargets[i]);
            }
        }

        private void _FillMorphData(Vector3[] array, Func<VertexBufferColumns, Vector4> selector)
        {
            if (array == null) return;

            for (int i = 0; i < this._MorphTargets.Count; ++i)
            {
                var v = selector(this._MorphTargets[i]);
                array[i] = new Vector3(v.X, v.Y, v.Z);
            }
        }

        private void _FillMorphData(Vector4[] array, Func<VertexBufferColumns, Vector4> selector)
        {
            if (array == null) return;

            for (int i = 0; i < this._MorphTargets.Count; ++i)
            {
                array[i] = selector(this._MorphTargets[i]);
            }
        }

        public VertexBufferColumns AddMorphTarget()
        {
            if (_MorphTargets == null) _MorphTargets = new List<VertexBufferColumns>();
            var mt = new VertexBufferColumns();
            _MorphTargets.Add(mt);

            return mt;
        }

        #endregion

        #region API - Vertex indexing

        public Type GetCompatibleVertexType()
        {
            var hasNormals = Normals != null;
            var hasTangents = hasNormals && Tangents != null;

            int numCols = 0;
            if (Colors0 != null) numCols = 1;
            if (Colors0 != null && Colors1 != null) numCols = 2;

            int numTexs = 0;
            if (TexCoords0 != null) numTexs = 1;
            if (TexCoords0 != null && TexCoords1 != null) numTexs = 2;

            int numJoints = 0;
            if (Joints0 != null) numJoints = 4;
            if (Joints0 != null && Joints1 != null) numJoints = 8;

            return VertexUtils.GetVertexBuilderType(hasNormals, hasTangents, numCols, numTexs, numJoints);
        }

        private TvG GetVertexGeometry<TvG>(int index)
            where TvG : struct, IVertexGeometry
        {
            var g = default(TvG);

            if (Positions != null) g.SetPosition(Positions[index]);
            if (Normals != null) g.SetNormal(Normals[index]);
            if (Tangents != null) g.SetTangent(Tangents[index]);

            return g;
        }

        private TvM GetVertexMaterial<TvM>(int index)
            where TvM : struct, IVertexMaterial
        {
            var m = default(TvM);

            if (Colors0 != null && m.MaxColors > 0) m.SetColor(0, Colors0[index]);
            if (Colors1 != null && m.MaxColors > 1) m.SetColor(1, Colors1[index]);

            if (TexCoords0 != null && m.MaxTextCoords > 0) m.SetTexCoord(0, TexCoords0[index]);
            if (TexCoords1 != null && m.MaxTextCoords > 1) m.SetTexCoord(1, TexCoords1[index]);

            return m;
        }

        private TvS GetVertexSkinning<TvS>(int index)
            where TvS : struct, IVertexSkinning
        {
            var s = default(TvS);

            if (s.MaxBindings == 0) return s;

            if (Joints0 != null && Weights0 != null)
            {
                if (Joints1 != null && Weights1 != null)
                {
                    var sparse = new Transforms.SparseWeight8(Joints0[index], Joints1[index], Weights0[index], Weights1[index]);
                    s.SetWeights(sparse);
                }
                else
                {
                    var sparse = new Transforms.SparseWeight8(Joints0[index], Weights0[index]);
                    s.SetWeights(sparse);
                }
            }

            return s;
        }

        public IVertexBuilder GetVertex(Type vertexType, int index)
        {
            var g = GetVertexGeometry<VertexPositionNormalTangent>(index);
            var m = GetVertexMaterial<VertexColor2Texture2>(index);
            var s = GetVertexSkinning<VertexJoints8>(index);

            var v = (IVertexBuilder)Activator.CreateInstance(vertexType);

            v.SetGeometry(g);
            v.SetMaterial(m);
            v.SetSkinning(s);

            return v;
        }

        public VertexBuilder<TvG, TvM, VertexEmpty> GetVertex<TvG, TvM>(int index)
            where TvG : struct, IVertexGeometry
            where TvM : struct, IVertexMaterial
        {
            var g = GetVertexGeometry<TvG>(index);
            var m = GetVertexMaterial<TvM>(index);

            return new VertexBuilder<TvG, TvM, VertexEmpty>(g, m);
        }

        public VertexBuilder<TvG, TvM, TvS> GetVertex<TvG, TvM, TvS>(int index)
            where TvG : struct, IVertexGeometry
            where TvM : struct, IVertexMaterial
            where TvS : struct, IVertexSkinning
        {
            var g = GetVertexGeometry<TvG>(index);
            var m = GetVertexMaterial<TvM>(index);
            var s = GetVertexSkinning<TvS>(index);

            return new VertexBuilder<TvG, TvM, TvS>(g, m, s);
        }

        #endregion

        #region utilites

        public static void CalculateSmoothNormals(IEnumerable<(VertexBufferColumns Vertices, IEnumerable<(int A, int B, int C)> Indices)> primitives)
        {
            Guard.NotNull(primitives, nameof(primitives));

            void addDirection(Dictionary<Vector3, Vector3> dict, Vector3 pos, Vector3 dir)
            {
                if (!dir._IsFinite()) return;
                if (!dict.TryGetValue(pos, out Vector3 n)) n = Vector3.Zero;
                dict[pos] = n + dir;
            }

            // calculate

            var normalMap = new Dictionary<Vector3, Vector3>();

            foreach (var (vertices, indices) in primitives)
            {
                foreach (var (ta, tb, tc) in indices)
                {
                    var p1 = vertices.Positions[ta];
                    var p2 = vertices.Positions[tb];
                    var p3 = vertices.Positions[tc];

                    var d = Vector3.Cross(p2 - p1, p3 - p1);

                    addDirection(normalMap, p1, d);
                    addDirection(normalMap, p2, d);
                    addDirection(normalMap, p3, d);
                }
            }

            // normalize

            foreach (var pos in normalMap.Keys.ToList())
            {
                var nrm = Vector3.Normalize(normalMap[pos]);

                normalMap[pos] = nrm._IsFinite() && nrm.LengthSquared() > 0.5f ? nrm : Vector3.UnitZ;
            }

            // apply

            foreach (var (vertices, indices) in primitives)
            {
                vertices.Normals = new Vector3[vertices.Positions.Count];

                for (int i = 0; i < vertices.Positions.Count; ++i)
                {
                    if (normalMap.TryGetValue(vertices.Positions[i],out Vector3 nrm))
                    {
                        vertices.Normals[i] = nrm;
                    }
                    else
                    {
                        vertices.Normals[i] = Vector3.UnitZ;
                    }
                }
            }
        }

        public static void CalculateTangents(IEnumerable<(VertexBufferColumns Vertices, IEnumerable<(int A, int B, int C)> Indices)> primitives)
        {
            // https://gamedev.stackexchange.com/questions/128023/how-does-mikktspace-work-for-calculating-the-tangent-space-during-normal-mapping
            // https://stackoverflow.com/questions/25349350/calculating-per-vertex-tangents-for-glsl
            // https://github.com/buildaworldnet/IrrlichtBAW/wiki/How-to-Normal-Detail-Bump-Derivative-Map,-why-Mikkelsen-is-slightly-wrong-and-why-you-should-give-up-on-calculating-per-vertex-tangents
            // https://gamedev.stackexchange.com/questions/68612/how-to-compute-tangent-and-bitangent-vectors
            // https://www.marti.works/calculating-tangents-for-your-mesh/
            // https://www.html5gamedevs.com/topic/34364-gltf-support-and-mikkt-space/

            Guard.NotNull(primitives, nameof(primitives));

            void addTangent(Dictionary<(Vector3 pos, Vector3 nrm, Vector2 uv), (Vector3, Vector3)> dict, (Vector3 pos, Vector3 nrm, Vector2 uv) key, (Vector3 tu, Vector3 tv) alpha)
            {
                dict.TryGetValue(key, out (Vector3 tu, Vector3 tv) beta);

                dict[key] = (alpha.tu + beta.tu, alpha.tv + beta.tv);
            }

            // calculate

            var tangentsMap = new Dictionary<(Vector3 pos, Vector3 nrm, Vector2 uv), (Vector3 u, Vector3 v)>();

            foreach (var (vertices, indices) in primitives)
            {
                vertices.Tangents = new Vector4[vertices.Positions.Count];

                foreach (var (i1, i2, i3) in indices)
                {
                    var p1 = vertices.Positions[i1];
                    var p2 = vertices.Positions[i2];
                    var p3 = vertices.Positions[i3];

                    var n1 = vertices.Normals[i1];
                    var n2 = vertices.Normals[i2];
                    var n3 = vertices.Normals[i3];

                    var uv1 = vertices.TexCoords0[i1];
                    var uv2 = vertices.TexCoords0[i2];
                    var uv3 = vertices.TexCoords0[i3];

                    var svec = p2 - p1;
                    var tvec = p3 - p1;

                    var stex = uv2 - uv1;
                    var ttex = uv3 - uv1;

                    float s1 = stex.X;
                    float s2 = ttex.X;
                    float t1 = stex.Y;
                    float t2 = ttex.Y;

                    var r = 1.0F / ((s1 * t2) - (s2 * t1));

                    var sdir = new Vector3((t2 * svec.X) - (t1 * tvec.X), (t2 * svec.Y) - (t1 * tvec.Y), (t2 * svec.Z) - (t1 * tvec.Z) ) * r;
                    var tdir = new Vector3((s1 * tvec.X) - (s2 * svec.X), (s1 * tvec.Y) - (s2 * svec.Y), (s1 * tvec.Z) - (s2 * svec.Z) ) * r;

                    addTangent(tangentsMap, (p1, n1, uv1), (sdir, tdir));
                    addTangent(tangentsMap, (p2, n2, uv2), (sdir, tdir));
                    addTangent(tangentsMap, (p3, n3, uv3), (sdir, tdir));
                }
            }

            // normalize

            foreach (var key in tangentsMap.Keys.ToList())
            {
                var val = tangentsMap[key];

                // Gram-Schmidt orthogonalize
                val.u = Vector3.Normalize(val.u - (key.nrm * Vector3.Dot(key.nrm, val.u)));
                val.v = Vector3.Normalize(val.v - (key.nrm * Vector3.Dot(key.nrm, val.v)));

                tangentsMap[key] = val;
            }

            // apply

            foreach (var (vertices, indices) in primitives)
            {
                vertices.Tangents = new Vector4[vertices.Tangents.Count];

                for (int i = 0; i < vertices.Positions.Count; ++i)
                {
                    var p = vertices.Positions[i];
                    var n = vertices.Normals[i];
                    var t = vertices.TexCoords0[i];

                    if (tangentsMap.TryGetValue((p, n, t), out (Vector3 u, Vector3 v) tangents))
                    {
                        var handedness = Vector3.Dot(Vector3.Cross(n, tangents.u), tangents.v) < 0 ? -1.0f : 1.0f;

                        vertices.Tangents[i] = new Vector4(tangents.u, handedness);
                    }
                    else
                    {
                        vertices.Tangents[i] = new Vector4(1, 0, 0, 1);
                    }
                }
            }
        }

        #endregion
    }
}