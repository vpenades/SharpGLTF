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
    /// <see cref="VertexJoints4"/>,
    /// <see cref="VertexJoints8"/>.
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
}
