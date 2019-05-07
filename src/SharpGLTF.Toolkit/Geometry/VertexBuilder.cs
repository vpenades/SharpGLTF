using System;
using System.Collections.Generic;
using System.Text;
using System.Numerics;

using SharpGLTF.Geometry.VertexTypes;

namespace SharpGLTF.Geometry
{
    /// <summary>
    /// Represents an individual vertex object.
    /// </summary>
    /// <typeparam name="TvP">
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
    [System.Diagnostics.DebuggerDisplay("Vertex {Geometry} {Material} {Skinning}")]
    public struct VertexBuilder<TvP, TvM, TvS>
        where TvP : struct, IVertexGeometry
        where TvM : struct, IVertexMaterial
        where TvS : struct, IVertexSkinning
    {
        #region constructors

        public VertexBuilder(TvP g, TvM m, TvS s)
        {
            Geometry = g;
            Material = m;
            Skinning = s;
        }

        public VertexBuilder(TvP g, TvM m, params (int, float)[] bindings)
        {
            Geometry = g;
            Material = m;
            Skinning = default;

            for (int i = 0; i < bindings.Length; ++i)
            {
                Skinning.SetBoneBinding(i, bindings[i].Item1, bindings[i].Item2);
            }
        }

        public VertexBuilder(TvP g, TvM m)
        {
            Geometry = g;
            Material = m;
            Skinning = default;
        }

        public VertexBuilder(TvP g, TvS s)
        {
            Geometry = g;
            Material = default;
            Skinning = s;
        }

        public VertexBuilder(TvP g)
        {
            Geometry = g;
            Material = default;
            Skinning = default;
        }

        public static implicit operator VertexBuilder<TvP, TvM, TvS>((TvP, TvM, TvS) tuple)
        {
            return new VertexBuilder<TvP, TvM, TvS>(tuple.Item1, tuple.Item2, tuple.Item3);
        }

        public static implicit operator VertexBuilder<TvP, TvM, TvS>((TvP, TvM) tuple)
        {
            return new VertexBuilder<TvP, TvM, TvS>(tuple.Item1, tuple.Item2);
        }

        public static implicit operator VertexBuilder<TvP, TvM, TvS>((TvP, TvS) tuple)
        {
            return new VertexBuilder<TvP, TvM, TvS>(tuple.Item1, tuple.Item2);
        }

        public static implicit operator VertexBuilder<TvP, TvM, TvS>(TvP g)
        {
            return new VertexBuilder<TvP, TvM, TvS>(g);
        }

        #endregion

        #region data

        public TvP Geometry;
        public TvM Material;
        public TvS Skinning;

        #endregion

        #region properties

        public Vector3 Position
        {
            get => Geometry.GetPosition();
            set => Geometry.SetPosition(value);
        }

        #endregion

        #region API

        public void Validate()
        {
            Geometry.Validate();
            Material.Validate();
            Skinning.Validate();
        }

        public VertexBuilder<TvPP, TvMM, TvSS> ConvertTo<TvPP, TvMM, TvSS>()
            where TvPP : struct, IVertexGeometry
            where TvMM : struct, IVertexMaterial
            where TvSS : struct, IVertexSkinning
        {
            var p = Geometry.ConvertTo<TvPP>();
            var m = Material.ConvertTo<TvMM>();
            var s = Skinning.ConvertTo<TvSS>();

            return new VertexBuilder<TvPP, TvMM, TvSS>(p, m, s);
        }

        public static MeshBuilder<TMaterial, TvP, TvM, TvS> CreateCompatibleMesh<TMaterial>(string name = null)
        {
            return new MeshBuilder<TMaterial, TvP, TvM, TvS>(name);
        }

        public static MeshBuilder<TvP, TvM, TvS> CreateCompatibleMesh(string name = null)
        {
            return new MeshBuilder<TvP, TvM, TvS>(name);
        }

        #endregion
    }
}
