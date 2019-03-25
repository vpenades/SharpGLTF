using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;

namespace SharpGLTF.Geometry
{
    /// <summary>
    /// Represents an utility class to help build meshes by adding primitives associated with a given material.
    /// </summary>
    /// <typeparam name="TMaterial">The material type used by this <see cref="PrimitiveBuilder{TMaterial, TvP, TvM, TvJ}"/> instance.</typeparam>
    /// <typeparam name="TvP">
    /// The vertex fragment type with Position, Normal and Tangent.
    /// Valid types are:
    /// <see cref="VertexTypes.VertexPosition"/>,
    /// <see cref="VertexTypes.VertexPositionNormal"/>,
    /// <see cref="VertexTypes.VertexPositionNormalTangent"/>.
    /// </typeparam>
    /// <typeparam name="TvM">
    /// The vertex fragment type with Colors and Texture Coordinates.
    /// Valid types are:
    /// <see cref="VertexTypes.VertexEmpty"/>,
    /// <see cref="VertexTypes.VertexColor1"/>,
    /// <see cref="VertexTypes.VertexTexture1"/>,
    /// <see cref="VertexTypes.VertexColor1Texture1"/>.
    /// </typeparam>
    public class MeshBuilder<TMaterial, TvP, TvM> : MeshBuilder<TMaterial, TvP, TvM, VertexTypes.VertexEmpty>
        where TvP : struct, VertexTypes.IVertexPosition
        where TvM : struct, VertexTypes.IVertexMaterial
    {
        public MeshBuilder(string name = null)
            : base(name) { }
    }

    /// <summary>
    /// Represents an utility class to help build meshes by adding primitives associated with a given material.
    /// </summary>
    /// <typeparam name="TMaterial">The material type used by this <see cref="PrimitiveBuilder{TMaterial, TvP, TvM, TvJ}"/> instance.</typeparam>
    /// <typeparam name="TvP">
    /// The vertex fragment type with Position, Normal and Tangent.
    /// Valid types are:
    /// <see cref="VertexTypes.VertexPosition"/>,
    /// <see cref="VertexTypes.VertexPositionNormal"/>,
    /// <see cref="VertexTypes.VertexPositionNormalTangent"/>.
    /// </typeparam>
    public class MeshBuilder<TMaterial, TvP> : MeshBuilder<TMaterial, TvP, VertexTypes.VertexEmpty, VertexTypes.VertexEmpty>
       where TvP : struct, VertexTypes.IVertexPosition
    {
        public MeshBuilder(string name = null)
            : base(name) { }
    }
}
