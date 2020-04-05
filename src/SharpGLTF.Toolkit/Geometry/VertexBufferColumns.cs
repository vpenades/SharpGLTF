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

        private void _FillMorphData(Vector3[] array, Converter<VertexBufferColumns, Vector3> selector)
        {
            if (array == null) return;

            for (int i = 0; i < this._MorphTargets.Count; ++i)
            {
                array[i] = selector(this._MorphTargets[i]);
            }
        }

        private void _FillMorphData(Vector3[] array, Converter<VertexBufferColumns, Vector4> selector)
        {
            if (array == null) return;

            for (int i = 0; i < this._MorphTargets.Count; ++i)
            {
                var v = selector(this._MorphTargets[i]);
                array[i] = new Vector3(v.X, v.Y, v.Z);
            }
        }

        private void _FillMorphData(Vector4[] array, Converter<VertexBufferColumns, Vector4> selector)
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

            if (m.MaxColors > 0) m.SetColor(0, Colors0 == null ? Vector4.One : Colors0[index]);
            if (m.MaxColors > 1) m.SetColor(1, Colors1 == null ? Vector4.One : Colors1[index]);

            if (m.MaxTextCoords > 0) m.SetTexCoord(0, TexCoords0 == null ? Vector2.Zero : TexCoords0[index]);
            if (m.MaxTextCoords > 1) m.SetTexCoord(1, TexCoords1 == null ? Vector2.Zero : TexCoords1[index]);

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

        struct _NormalTangentAgent : VertexNormalsFactory.IMeshPrimitive, VertexTangentsFactory.IMeshPrimitive
        {
            public _NormalTangentAgent(VertexBufferColumns vertices, IEnumerable<(int A, int B, int C)> indices)
            {
                _Vertices = vertices;
                _Indices = indices;
            }

            private readonly VertexBufferColumns _Vertices;
            private readonly IEnumerable<(int A, int B, int C)> _Indices;

            public int VertexCount => _Vertices.Positions.Count;

            public IEnumerable<(int A, int B, int C)> GetTriangleIndices() { return _Indices; }

            public Vector3 GetVertexPosition(int idx) { return _Vertices.Positions[idx]; }

            public Vector3 GetVertexNormal(int idx) { return _Vertices.Normals[idx]; }

            public Vector2 GetVertexTexCoord(int idx) { return _Vertices.TexCoords0[idx]; }

            public void SetVertexNormal(int idx, Vector3 normal)
            {
                if (_Vertices.Normals == null) _Vertices.Normals = new Vector3[_Vertices.Positions.Count];

                _Vertices.Normals[idx] = normal;
            }

            public void SetVertexTangent(int idx, Vector4 tangent)
            {
                if (_Vertices.Tangents == null) _Vertices.Tangents = new Vector4[_Vertices.Positions.Count];

                _Vertices.Tangents[idx] = tangent;
            }
        }

        public static void CalculateSmoothNormals(IReadOnlyList<(VertexBufferColumns Vertices, IEnumerable<(int A, int B, int C)> Indices)> primitives)
        {
            Guard.NotNull(primitives, nameof(primitives));

            var agents = primitives
                .Select(item => new _NormalTangentAgent(item.Vertices, item.Indices))
                .ToList();

            VertexNormalsFactory.CalculateSmoothNormals(agents);
        }

        public static void CalculateTangents(IReadOnlyList<(VertexBufferColumns Vertices, IEnumerable<(int A, int B, int C)> Indices)> primitives)
        {
            Guard.NotNull(primitives, nameof(primitives));

            var agents = primitives
                .Select(item => new _NormalTangentAgent(item.Vertices, item.Indices))
                .ToList();

            VertexTangentsFactory.CalculateTangents(agents);
        }

        #endregion
    }
}