using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using System.Numerics;

using SharpGLTF.Collections;
using SharpGLTF.Geometry.VertexTypes;

namespace SharpGLTF.Geometry
{
    public interface IMeshBuilder<TMaterial>
    {
        string Name { get; set; }

        IReadOnlyCollection<TMaterial> Materials { get; }

        IPrimitiveBuilder UsePrimitive(TMaterial material);

        IPrimitive<TMaterial> GetPrimitive(TMaterial material);

        void Validate();
    }

    /// <summary>
    /// Represents an utility class to help build meshes by adding primitives associated with a given material.
    /// </summary>
    /// <typeparam name="TMaterial">The material type used by this <see cref="PrimitiveBuilder{TMaterial, TvP, TvM, TvS}"/> instance.</typeparam>
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
    public class MeshBuilder<TMaterial, TvP, TvM, TvS> : IMeshBuilder<TMaterial>
        where TvP : struct, IVertexGeometry
        where TvM : struct, IVertexMaterial
        where TvS : struct, IVertexSkinning
    {
        #region lifecycle

        public MeshBuilder(string name = null)
        {
            this.Name = name;
        }

        #endregion

        #region data

        private readonly Dictionary<TMaterial, PrimitiveBuilder<TMaterial, TvP, TvM, TvS>> _Primitives = new Dictionary<TMaterial, PrimitiveBuilder<TMaterial, TvP, TvM, TvS>>();

        #endregion

        #region properties

        public Boolean StrictMode { get; set; }

        public string Name { get; set; }

        public IReadOnlyCollection<TMaterial> Materials => _Primitives.Keys;

        public IReadOnlyCollection<PrimitiveBuilder<TMaterial, TvP, TvM, TvS>> Primitives => _Primitives.Values;

        string IMeshBuilder<TMaterial>.Name { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        IReadOnlyCollection<TMaterial> IMeshBuilder<TMaterial>.Materials => throw new NotImplementedException();

        #endregion

        #region API

        private PrimitiveBuilder<TMaterial, TvP, TvM, TvS> _UsePrimitive(TMaterial material)
        {
            if (!_Primitives.TryGetValue(material, out PrimitiveBuilder<TMaterial, TvP, TvM, TvS> primitive))
            {
                primitive = new PrimitiveBuilder<TMaterial, TvP, TvM, TvS>(this, material, StrictMode);
                _Primitives[material] = primitive;
            }

            return primitive;
        }

        public PrimitiveBuilder<TMaterial, TvP, TvM, TvS> UsePrimitive(TMaterial material) { return _UsePrimitive(material); }

        IPrimitiveBuilder IMeshBuilder<TMaterial>.UsePrimitive(TMaterial material) { return _UsePrimitive(material); }

        IPrimitive<TMaterial> IMeshBuilder<TMaterial>.GetPrimitive(TMaterial material)
        {
            if (_Primitives.TryGetValue(material, out PrimitiveBuilder<TMaterial, TvP, TvM, TvS> primitive))
            {
                return primitive;
            }

            return null;
        }

        public void AddMesh(MeshBuilder<TMaterial, TvP, TvM, TvS> mesh, Matrix4x4 transform)
        {
            foreach (var p in mesh.Primitives)
            {
                UsePrimitive(p.Material).AddPrimitive(p, transform);
            }
        }

        public void Validate()
        {
            foreach (var p in _Primitives.Values)
            {
                p.Validate();
            }
        }

        #endregion
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
    /// <typeparam name="TvS">
    /// The vertex fragment type with Skin Joint Weights.
    /// Valid types are:
    /// <see cref="VertexEmpty"/>,
    /// <see cref="VertexJoints8x4"/>,
    /// <see cref="VertexJoints8x8"/>,
    /// <see cref="VertexJoints16x4"/>,
    /// <see cref="VertexJoints16x8"/>.
    /// </typeparam>
    public class MeshBuilder<TvP, TvM, TvS> : MeshBuilder<Materials.MaterialBuilder, TvP, TvM, TvS>
        where TvP : struct, IVertexGeometry
        where TvM : struct, IVertexMaterial
        where TvS : struct, IVertexSkinning
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
        where TvP : struct, IVertexGeometry
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
        where TvP : struct, IVertexGeometry
    {
        public MeshBuilder(string name = null)
            : base(name) { }
    }
}
