using System;
using System.Collections.Generic;
using System.Text;

namespace SharpGLTF.Geometry
{
    using System.Numerics;
    using VertexTypes;

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
    [System.Diagnostics.DebuggerDisplay("Primitive {_Material}")]
    public struct Vertex<TvP, TvM, TvS>
        where TvP : struct, IVertexGeometry
        where TvM : struct, IVertexMaterial
        where TvS : struct, IVertexSkinning
    {
        #region constructors

        public Vertex(TvP g, TvM m, TvS s)
        {
            Geometry = g;
            Material = m;
            Skinning = s;
        }

        public Vertex(TvP g, TvM m)
        {
            Geometry = g;
            Material = m;
            Skinning = default;
        }

        public Vertex(TvP g, TvS s)
        {
            Geometry = g;
            Material = default;
            Skinning = s;
        }

        public Vertex(TvP g)
        {
            Geometry = g;
            Material = default;
            Skinning = default;
        }

        public static implicit operator Vertex<TvP, TvM, TvS>((TvP, TvM, TvS) tuple)
        {
            return new Vertex<TvP, TvM, TvS>(tuple.Item1, tuple.Item2, tuple.Item3);
        }

        public static implicit operator Vertex<TvP, TvM, TvS>((TvP, TvM) tuple)
        {
            return new Vertex<TvP, TvM, TvS>(tuple.Item1, tuple.Item2);
        }

        public static implicit operator Vertex<TvP, TvM, TvS>((TvP, TvS) tuple)
        {
            return new Vertex<TvP, TvM, TvS>(tuple.Item1, tuple.Item2);
        }

        public static implicit operator Vertex<TvP, TvM, TvS>(TvP g)
        {
            return new Vertex<TvP, TvM, TvS>(g);
        }

        public Vertex<TvPP, TvMM, TvSS> CloneAs<TvPP, TvMM, TvSS>()
            where TvPP : struct, IVertexGeometry
            where TvMM : struct, IVertexMaterial
            where TvSS : struct, IVertexSkinning
        {
            var p = Geometry.CloneAs<TvPP>();
            var m = Material.CloneAs<TvMM>();
            var s = Skinning.CloneAs<TvSS>();

            return new Vertex<TvPP, TvMM, TvSS>(p, m, s);
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

        #endregion
    }
}
