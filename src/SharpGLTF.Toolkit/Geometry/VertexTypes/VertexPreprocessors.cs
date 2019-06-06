using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace SharpGLTF.Geometry.VertexTypes
{
    public delegate TvG? VertexGeometryPreprocessor<TvG>(TvG arg)
        where TvG : struct, IVertexGeometry;

    public delegate TvM? VertexMaterialPreprocessor<TvM>(TvM arg)
        where TvM : struct, IVertexMaterial;

    public delegate TvS? VertexSkinningPreprocessor<TvS>(TvS arg)
        where TvS : struct, IVertexSkinning;

    /// <summary>
    /// Represents a <see cref="VertexBuilder{TvG, TvM, TvS}"/> preprocessor used by <see cref="MeshBuilder{TMaterial, TvG, TvM, TvS}.VertexPreprocessor"/>
    /// </summary>
    /// <typeparam name="TvG">
    /// The vertex fragment type with Position, Normal and Tangent.
    /// Valid types are:
    /// <see cref="VertexPosition"/>,
    /// <see cref="VertexPositionNormal"/>,
    /// <see cref="VertexPositionNormalTangent"/>.
    /// </typeparam>
    /// <typeparam name="TvM">
    /// The vertex fragment type with Colors and Texture Coordinates.
    /// Valid types are:
    /// <see cref="VertexEmpty"/>,
    /// <see cref="VertexColor1"/>,
    /// <see cref="VertexTexture1"/>,
    /// <see cref="VertexColor1Texture1"/>.
    /// </typeparam>
    /// <typeparam name="TvS">
    /// The vertex fragment type with Skin Joint Weights.
    /// Valid types are:
    /// <see cref="VertexEmpty"/>,
    /// <see cref="VertexJoints8x4"/>,
    /// <see cref="VertexJoints8x8"/>,
    /// <see cref="VertexJoints16x4"/>,
    /// <see cref="VertexJoints16x8"/>.
    /// </typeparam>
    public sealed class VertexPreprocessor<TvG, TvM, TvS>
        where TvG : struct, IVertexGeometry
        where TvM : struct, IVertexMaterial
        where TvS : struct, IVertexSkinning
    {
        #region data

        private readonly List<VertexGeometryPreprocessor<TvG>> _GeometryPreprocessor = new List<VertexGeometryPreprocessor<TvG>>();
        private readonly List<VertexMaterialPreprocessor<TvM>> _MaterialPreprocessor = new List<VertexMaterialPreprocessor<TvM>>();
        private readonly List<VertexSkinningPreprocessor<TvS>> _SkinningPreprocessor = new List<VertexSkinningPreprocessor<TvS>>();

        #endregion

        #region API

        public void Clear()
        {
            _GeometryPreprocessor.Clear();
            _MaterialPreprocessor.Clear();
            _SkinningPreprocessor.Clear();
        }

        public void Append(VertexGeometryPreprocessor<TvG> func)
        {
            _GeometryPreprocessor.Add(func);
        }

        public void Append(VertexMaterialPreprocessor<TvM> func)
        {
            _MaterialPreprocessor.Add(func);
        }

        public void Append(VertexSkinningPreprocessor<TvS> func)
        {
            _SkinningPreprocessor.Add(func);
        }

        public void SetDebugPreprocessors()
        {
            Clear();
            Append(FragmentPreprocessors.ValidateVertexGeometry);
            Append(FragmentPreprocessors.ValidateVertexMaterial);
            Append(FragmentPreprocessors.ValidateVertexSkinning);
        }

        public void SetSanitizerPreprocessors()
        {
            Clear();
            Append(FragmentPreprocessors.SanitizeVertexGeometry);
            Append(FragmentPreprocessors.SanitizeVertexMaterial);
            Append(FragmentPreprocessors.SanitizeVertexSkinning);
        }

        public bool PreprocessVertex(ref VertexBuilder<TvG, TvM, TvS> vertex)
        {
            foreach (var f in _GeometryPreprocessor)
            {
                var g = f(vertex.Geometry);
                if (!g.HasValue) return false;
                vertex.Geometry = g.Value;
            }

            foreach (var f in _MaterialPreprocessor)
            {
                var m = f(vertex.Material);
                if (!m.HasValue) return false;
                vertex.Material = m.Value;
            }

            foreach (var f in _SkinningPreprocessor)
            {
                var s = f(vertex.Skinning);
                if (!s.HasValue) return false;
                vertex.Skinning = s.Value;
            }

            return true;
        }

        #endregion
    }

    /// <summary>
    /// Defines a set of vertex fragment preprocessors to be used with <see cref="VertexPreprocessor{TvG, TvM, TvS}"/>
    /// </summary>
    static class FragmentPreprocessors
    {
        /// <summary>
        /// validates a vertex geometry, throwing exceptions if found invalid
        /// </summary>
        /// <typeparam name="TvG">
        /// The vertex fragment type with Position, Normal and Tangent.
        /// Valid types are:
        /// <see cref="VertexPosition"/>,
        /// <see cref="VertexPositionNormal"/>,
        /// <see cref="VertexPositionNormalTangent"/>.
        /// </typeparam>
        /// <param name="vertex">the source <typeparamref name="TvG"/> vertex.</param>
        /// <returns>A sanitized <typeparamref name="TvG"/> vertex, or null if sanitization failed.</returns>
        /// <exception cref="ArgumentException">When the vertex is invalid.</exception>
        public static TvG? ValidateVertexGeometry<TvG>(TvG vertex)
            where TvG : struct, IVertexGeometry
        {
            var p = vertex.GetPosition();
            Guard.IsTrue(p._IsReal(), "Position", "Values are not finite.");

            if (vertex.TryGetNormal(out Vector3 n))
            {
                Guard.IsTrue(n._IsReal(), "Normal", "Values are not finite.");
                Guard.MustBeBetweenOrEqualTo(n.Length(), 0.99f, 1.01f, "Normal.Length");
            }

            if (vertex.TryGetTangent(out Vector4 t))
            {
                Guard.IsTrue(t._IsReal(), "Tangent", "Values are not finite.");
                Guard.IsTrue(t.W == 1 || t.W == -1, "Tangent.W", "Invalid value");
                Guard.MustBeBetweenOrEqualTo(new Vector3(t.X, t.Y, t.Z).Length(), 0.99f, 1.01f, "Tangent.XYZ.Length");
            }

            return vertex;
        }

        /// <summary>
        /// Sanitizes a vertex material with a best effort approach
        /// </summary>
        /// <typeparam name="TvM">
        /// The vertex fragment type with Colors and Texture Coordinates.
        /// Valid types are:
        /// <see cref="VertexEmpty"/>,
        /// <see cref="VertexColor1"/>,
        /// <see cref="VertexTexture1"/>,
        /// <see cref="VertexColor1Texture1"/>.
        /// </typeparam>
        /// <param name="vertex">the source <typeparamref name="TvM"/> vertex.</param>
        /// <returns>A sanitized <typeparamref name="TvM"/> vertex, or null if sanitization failed.</returns>
        public static TvM? ValidateVertexMaterial<TvM>(TvM vertex)
            where TvM : struct, IVertexMaterial
        {
            for (int i = 0; i < vertex.MaxColors; ++i)
            {
                var c = vertex.GetColor(i);
                Guard.IsTrue(c._IsReal(), $"Color{i}", "Values are not finite.");
                Guard.MustBeBetweenOrEqualTo(c.X, 0, 1, $"Color{i}.R");
                Guard.MustBeBetweenOrEqualTo(c.Y, 0, 1, $"Color{i}.G");
                Guard.MustBeBetweenOrEqualTo(c.Z, 0, 1, $"Color{i}.B");
                Guard.MustBeBetweenOrEqualTo(c.W, 0, 1, $"Color{i}.A");
            }

            for (int i = 0; i < vertex.MaxTextures; ++i)
            {
                var t = vertex.GetTexCoord(i);
                Guard.IsTrue(t._IsReal(), $"TexCoord{i}", "Values are not finite.");
            }

            return vertex;
        }

        /// <summary>
        /// Sanitizes a vertex skinning with a best effort approach
        /// </summary>
        /// <typeparam name="TvS">
        /// The vertex fragment type with Skin Joint Weights.
        /// Valid types are:
        /// <see cref="VertexEmpty"/>,
        /// <see cref="VertexJoints8x4"/>,
        /// <see cref="VertexJoints8x8"/>,
        /// <see cref="VertexJoints16x4"/>,
        /// <see cref="VertexJoints16x8"/>.
        /// </typeparam>
        /// <param name="vertex">the source <typeparamref name="TvS"/> vertex.</param>
        /// <returns>A sanitized <typeparamref name="TvS"/> vertex, or null if sanitization failed.</returns>
        public static TvS? ValidateVertexSkinning<TvS>(TvS vertex)
            where TvS : struct, IVertexSkinning
        {
            if (vertex.MaxBindings == 0) return vertex;

            // Apparently the consensus is that weights are required to be normalized.
            // More here: https://github.com/KhronosGroup/glTF/issues/1213

            float weightsSum = 0;

            for (int i = 0; i < vertex.MaxBindings; ++i)
            {
                var pair = vertex.GetJointBinding(i);

                Guard.MustBeGreaterThanOrEqualTo(pair.Joint, 0, $"Joint{i}");
                Guard.IsTrue(pair.Weight._IsReal(), $"Weight{i}", "Values are not finite.");
                if (pair.Weight == 0) Guard.IsTrue(pair.Joint == 0, "joints with weight zero must be set to zero");

                weightsSum += pair.Weight;
            }

            // TODO: check that joints are unique

            Guard.MustBeBetweenOrEqualTo(weightsSum, 0.99f, 1.01f, "Weights SUM");

            return vertex;
        }

        /// <summary>
        /// Sanitizes a vertex geometry with a best effort approach
        /// </summary>
        /// <typeparam name="TvG">
        /// The vertex fragment type with Position, Normal and Tangent.
        /// Valid types are:
        /// <see cref="VertexPosition"/>,
        /// <see cref="VertexPositionNormal"/>,
        /// <see cref="VertexPositionNormalTangent"/>.
        /// </typeparam>
        /// <param name="vertex">the source <typeparamref name="TvG"/> vertex.</param>
        /// <returns>A sanitized <typeparamref name="TvG"/> vertex, or null if sanitization failed.</returns>
        public static TvG? SanitizeVertexGeometry<TvG>(TvG vertex)
            where TvG : struct, IVertexGeometry
        {
            var p = vertex.GetPosition();

            if (!p._IsReal()) return null;

            if (vertex.TryGetNormal(out Vector3 n))
            {
                if (!n._IsReal()) return null;
                if (n == Vector3.Zero) n = p;
                if (n == Vector3.Zero) return null;

                var l = n.Length();
                if (l < 0.99f || l > 0.01f) vertex.SetNormal(Vector3.Normalize(n));
            }

            if (vertex.TryGetTangent(out Vector4 tw))
            {
                if (!tw._IsReal()) return null;

                var t = new Vector3(tw.X, tw.Y, tw.Z);
                if (t == Vector3.Zero) return null;

                if (tw.W > 0) tw.W = 1;
                if (tw.W < 0) tw.W = -1;

                var l = t.Length();
                if (l < 0.99f || l > 0.01f) t = Vector3.Normalize(t);

                vertex.SetTangent(new Vector4(t, tw.W));
            }

            return vertex;
        }

        /// <summary>
        /// Sanitizes a vertex material with a best effort approach
        /// </summary>
        /// <typeparam name="TvM">
        /// The vertex fragment type with Colors and Texture Coordinates.
        /// Valid types are:
        /// <see cref="VertexEmpty"/>,
        /// <see cref="VertexColor1"/>,
        /// <see cref="VertexTexture1"/>,
        /// <see cref="VertexColor1Texture1"/>.
        /// </typeparam>
        /// <param name="vertex">the source <typeparamref name="TvM"/> vertex.</param>
        /// <returns>A sanitized <typeparamref name="TvM"/> vertex, or null if sanitization failed.</returns>
        public static TvM? SanitizeVertexMaterial<TvM>(TvM vertex)
            where TvM : struct, IVertexMaterial
        {
            for (int i = 0; i < vertex.MaxColors; ++i)
            {
                var c = vertex.GetColor(i);
                if (!c._IsReal()) c = Vector4.Zero;
                c = Vector4.Min(Vector4.One, c);
                c = Vector4.Max(Vector4.Zero, c);
                vertex.SetColor(i, c);
            }

            for (int i = 0; i < vertex.MaxTextures; ++i)
            {
                var t = vertex.GetTexCoord(i);
                if (!t._IsReal()) vertex.SetTexCoord(i, Vector2.Zero);
            }

            return vertex;
        }

        /// <summary>
        /// Sanitizes a vertex skinning with a best effort approach
        /// </summary>
        /// <typeparam name="TvS">
        /// The vertex fragment type with Skin Joint Weights.
        /// Valid types are:
        /// <see cref="VertexEmpty"/>,
        /// <see cref="VertexJoints8x4"/>,
        /// <see cref="VertexJoints8x8"/>,
        /// <see cref="VertexJoints16x4"/>,
        /// <see cref="VertexJoints16x8"/>.
        /// </typeparam>
        /// <param name="vertex">the source <typeparamref name="TvS"/> vertex.</param>
        /// <returns>A sanitized <typeparamref name="TvS"/> vertex, or null if sanitization failed.</returns>
        public static TvS? SanitizeVertexSkinning<TvS>(TvS vertex)
            where TvS : struct, IVertexSkinning
        {
            if (vertex.MaxBindings == 0) return vertex;

            Span<JointBinding> pairs = stackalloc JointBinding[vertex.MaxBindings];

            // Apparently the consensus is that weights are required to be normalized.
            // More here: https://github.com/KhronosGroup/glTF/issues/1213

            float weightsSum = 0;

            for (int i = 0; i < pairs.Length; ++i)
            {
                var pair = vertex.GetJointBinding(i);

                pairs[i] = pair.Weight == 0 ? default : pair;

                weightsSum += pair.Weight;
            }

            // TODO: check that joints are unique, and if not, do a merge.

            if (weightsSum == 0) weightsSum = 1;

            JointBinding.InPlaceReverseBubbleSort(pairs);

            for (int i = 0; i < pairs.Length; ++i)
            {
                vertex.SetJointBinding(i, pairs[i].Joint, pairs[i].Weight / weightsSum);
            }

            return vertex;
        }
    }
}
