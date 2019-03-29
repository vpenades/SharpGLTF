using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;

namespace SharpGLTF.Geometry
{
    using VertexTypes;

    /// <summary>
    /// Represents an utility class to help build meshes by adding primitives associated with a given material.
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
    /// <typeparam name="TvJ">
    /// The vertex fragment type with Skin Joint Weights.
    /// Valid types are:
    /// <see cref="VertexEmpty"/>,
    /// <see cref="VertexJoints8x4"/>,
    /// <see cref="VertexJoints8x8"/>,
    /// <see cref="VertexJoints16x4"/>,
    /// <see cref="VertexJoints16x8"/>.
    /// </typeparam>
    public class MeshBuilder<TvP, TvM, TvJ> : MeshBuilder<Materials.MaterialBuilder,TvP,TvM,TvJ>
        where TvP : struct, IVertexPosition
        where TvM : struct, IVertexMaterial
        where TvJ : struct, IVertexJoints
    {
        public MeshBuilder(string name = null)
            : base(name) { }
    }

    /// <summary>
    /// Represents an utility class to help build meshes by adding primitives associated with a given material.
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
    public class MeshBuilder<TvP, TvM> : MeshBuilder<Materials.MaterialBuilder, TvP, TvM, VertexEmpty>
        where TvP : struct, IVertexPosition
        where TvM : struct, IVertexMaterial
    {
        public MeshBuilder(string name = null)
            : base(name) { }
    }

    /// <summary>
    /// Represents an utility class to help build meshes by adding primitives associated with a given material.
    /// </summary>
    /// <typeparam name="TvP">
    /// The vertex fragment type with Position, Normal and Tangent.
    /// Valid types are:
    /// <see cref="VertexPosition"/>,
    /// <see cref="VertexPositionNormal"/>,
    /// <see cref="VertexPositionNormalTangent"/>.
    /// </typeparam>
    public class MeshBuilder<TvP> : MeshBuilder<Materials.MaterialBuilder, TvP, VertexEmpty, VertexEmpty>
        where TvP : struct, IVertexPosition
    {
        public MeshBuilder(string name = null)
            : base(name) { }
    }
}
