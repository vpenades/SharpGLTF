using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using System.Numerics;

using SharpGLTF.Collections;
using SharpGLTF.Geometry.VertexTypes;

namespace SharpGLTF.Geometry
{
    /// <summary>
    /// Represents an utility class to help build meshes by adding primitives associated with a given material.
    /// </summary>
    /// <typeparam name="TMaterial">The material type used by this <see cref="PrimitiveBuilder{TMaterial, TvP, TvM, TvS}"/> instance.</typeparam>
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
    /// <see cref="VertexColor1Texture2"/>.
    /// <see cref="VertexColor2Texture2"/>.
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
    public class MeshBuilder<TMaterial, TvG, TvM, TvS> : IMeshBuilder<TMaterial>
        where TvG : struct, IVertexGeometry
        where TvM : struct, IVertexMaterial
        where TvS : struct, IVertexSkinning
    {
        #region lifecycle

        public MeshBuilder(string name = null)
        {
            this.Name = name;

            // this is the recomended preprocesor for release/production
            _VertexPreprocessor = new VertexPreprocessor<TvG, TvM, TvS>();
            _VertexPreprocessor.SetSanitizerPreprocessors();
        }

        #endregion

        #region data

        private readonly Dictionary<(TMaterial, int), PrimitiveBuilder<TMaterial, TvG, TvM, TvS>> _Primitives = new Dictionary<(TMaterial, int), PrimitiveBuilder<TMaterial, TvG, TvM, TvS>>();

        private VertexPreprocessor<TvG, TvM, TvS> _VertexPreprocessor;

        #endregion

        #region properties

        public string Name { get; set; }

        public VertexPreprocessor<TvG, TvM, TvS> VertexPreprocessor
        {
            get => _VertexPreprocessor;
            set => _VertexPreprocessor = value;
        }

        public IEnumerable<TMaterial> Materials => _Primitives.Keys.Select(item => item.Item1).Distinct();

        public IReadOnlyCollection<PrimitiveBuilder<TMaterial, TvG, TvM, TvS>> Primitives => _Primitives.Values;

        IReadOnlyCollection<IPrimitiveReader<TMaterial>> IMeshBuilder<TMaterial>.Primitives => _Primitives.Values;

        #endregion

        #region API

        private PrimitiveBuilder<TMaterial, TvG, TvM, TvS> _UsePrimitive((TMaterial, int) key)
        {
            if (!_Primitives.TryGetValue(key, out PrimitiveBuilder<TMaterial, TvG, TvM, TvS> primitive))
            {
                primitive = new PrimitiveBuilder<TMaterial, TvG, TvM, TvS>(this, key.Item1, key.Item2);
                _Primitives[key] = primitive;
            }

            return primitive;
        }

        public PrimitiveBuilder<TMaterial, TvG, TvM, TvS> UsePrimitive(TMaterial material, int primitiveVertexCount = 3)
        {
            Guard.NotNull(material, nameof(material));
            Guard.MustBeBetweenOrEqualTo(primitiveVertexCount, 1, 3, nameof(primitiveVertexCount));

            return _UsePrimitive((material, primitiveVertexCount));
        }

        IPrimitiveBuilder IMeshBuilder<TMaterial>.UsePrimitive(TMaterial material, int primitiveVertexCount)
        {
            Guard.NotNull(material, nameof(material));
            Guard.MustBeBetweenOrEqualTo(primitiveVertexCount, 1, 3, nameof(primitiveVertexCount));

            return _UsePrimitive((material, primitiveVertexCount));
        }

        public void AddMesh(MeshBuilder<TMaterial, TvG, TvM, TvS> mesh, Func<TMaterial, TMaterial> materialTransform, Func<VertexBuilder<TvG, TvM, TvS>, VertexBuilder<TvG, TvM, TvS>> vertexTransform)
        {
            if (mesh == null) return;
            Guard.NotNull(materialTransform, nameof(materialTransform));

            foreach (var p in mesh.Primitives)
            {
                var materialKey = materialTransform(p.Material);

                UsePrimitive(materialKey).AddPrimitive(p, vertexTransform);
            }
        }

        /// <summary>
        /// Transforms all the points of all the <see cref="PrimitiveBuilder{TMaterial, TvP, TvM, TvS}"/>
        /// of the this <see cref="MeshBuilder{TMaterial, TvP, TvM, TvS}"/> using the given lambfa function.
        /// </summary>
        /// <param name="vertexTransform">A lambda function to transform <see cref="VertexBuilder{TvP, TvM, TvS}"/> vertices.</param>
        public void TransformVertices(Func<VertexBuilder<TvG, TvM, TvS>, VertexBuilder<TvG, TvM, TvS>> vertexTransform)
        {
            foreach (var p in Primitives) p.TransformVertices(vertexTransform);
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
    /// <see cref="VertexColor1Texture2"/>.
    /// <see cref="VertexColor2Texture2"/>.
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
    public class MeshBuilder<TvG, TvM, TvS> : MeshBuilder<Materials.MaterialBuilder, TvG, TvM, TvS>
        where TvG : struct, IVertexGeometry
        where TvM : struct, IVertexMaterial
        where TvS : struct, IVertexSkinning
    {
        public MeshBuilder(string name = null)
            : base(name) { }
    }

    /// <summary>
    /// Represents an utility class to help build meshes by adding primitives associated with a given material.
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
    /// <see cref="VertexColor1Texture2"/>.
    /// <see cref="VertexColor2Texture2"/>.
    /// </typeparam>
    public class MeshBuilder<TvG, TvM> : MeshBuilder<Materials.MaterialBuilder, TvG, TvM, VertexEmpty>
        where TvG : struct, IVertexGeometry
        where TvM : struct, IVertexMaterial
    {
        public MeshBuilder(string name = null)
            : base(name) { }
    }

    /// <summary>
    /// Represents an utility class to help build meshes by adding primitives associated with a given material.
    /// </summary>
    /// <typeparam name="TvG">
    /// The vertex fragment type with Position, Normal and Tangent.
    /// Valid types are:
    /// <see cref="VertexPosition"/>,
    /// <see cref="VertexPositionNormal"/>,
    /// <see cref="VertexPositionNormalTangent"/>.
    /// </typeparam>
    public class MeshBuilder<TvG> : MeshBuilder<Materials.MaterialBuilder, TvG, VertexEmpty, VertexEmpty>
        where TvG : struct, IVertexGeometry
    {
        public MeshBuilder(string name = null)
            : base(name) { }
    }
}
