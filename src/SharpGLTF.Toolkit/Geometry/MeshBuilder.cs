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
    /// <see cref="VertexJoints4"/>,
    /// <see cref="VertexJoints8"/>.
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

        IMeshBuilder<TMaterial> IMeshBuilder<TMaterial>.Clone(Func<TMaterial, TMaterial> materialCloneCallback)
        {
            return new MeshBuilder<TMaterial, TvG, TvM, TvS>(this, materialCloneCallback);
        }

        public MeshBuilder<TMaterial, TvG, TvM, TvS> Clone(Func<TMaterial, TMaterial> materialCloneCallback = null)
        {
            return new MeshBuilder<TMaterial, TvG, TvM, TvS>(this, materialCloneCallback);
        }

        private MeshBuilder(MeshBuilder<TMaterial, TvG, TvM, TvS> other, Func<TMaterial, TMaterial> materialCloneCallback = null)
        {
            Guard.NotNull(other, nameof(other));

            this.Name = other.Name;
            this._VertexPreprocessor = other._VertexPreprocessor;

            foreach (var kvp in other._Primitives)
            {
                var material = kvp.Key.Material;

                if (materialCloneCallback != null)
                {
                    material = materialCloneCallback(material);
                    if (material == null) continue;
                }

                var key = (material, kvp.Key.PrimType);

                if (_Primitives.TryGetValue(key, out PrimitiveBuilder<TMaterial, TvG, TvM, TvS> existing))
                {
                    existing.AddPrimitive(kvp.Value, null);
                }
                else
                {
                    _Primitives[key] = kvp.Value.Clone(this, material);
                }
            }
        }

        #endregion

        #region data

        [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)]
        private readonly Dictionary<(TMaterial Material, int PrimType), PrimitiveBuilder<TMaterial, TvG, TvM, TvS>> _Primitives = new Dictionary<(TMaterial, int), PrimitiveBuilder<TMaterial, TvG, TvM, TvS>>();

        [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)]
        private VertexPreprocessor<TvG, TvM, TvS> _VertexPreprocessor;

        #endregion

        #region properties

        public string Name { get; set; }

        public VertexPreprocessor<TvG, TvM, TvS> VertexPreprocessor
        {
            get => _VertexPreprocessor;
            set => _VertexPreprocessor = value;
        }

        public IEnumerable<TMaterial> Materials => _Primitives.Keys.Select(item => item.Material).Distinct();

        public IReadOnlyCollection<PrimitiveBuilder<TMaterial, TvG, TvM, TvS>> Primitives => _Primitives.Values;

        IReadOnlyCollection<IPrimitiveReader<TMaterial>> IMeshBuilder<TMaterial>.Primitives => _Primitives.Values;

        #endregion

        #region API

        public MorphTargetBuilder<TMaterial, TvG, TvS, TvM> UseMorphTarget(int index)
        {
            return new MorphTargetBuilder<TMaterial, TvG, TvS, TvM>(this, index);
        }

        IMorphTargetBuilder IMeshBuilder<TMaterial>.UseMorphTarget(int index)
        {
            return UseMorphTarget(index);
        }

        private PrimitiveBuilder<TMaterial, TvG, TvM, TvS> _UsePrimitive((TMaterial Material, int PrimType) key)
        {
            if (!_Primitives.TryGetValue(key, out PrimitiveBuilder<TMaterial, TvG, TvM, TvS> primitive))
            {
                if (key.PrimType == 1) primitive = new PointsPrimitiveBuilder<TMaterial, TvG, TvM, TvS>(this, key.Material);
                if (key.PrimType == 2) primitive = new LinesPrimitiveBuilder<TMaterial, TvG, TvM, TvS>(this, key.Material);
                if (key.PrimType == 3) primitive = new TrianglesPrimitiveBuilder<TMaterial, TvG, TvM, TvS>(this, key.Material);

                Guard.NotNull(primitive, nameof(key));

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
    /// <see cref="VertexJoints4"/>,
    /// <see cref="VertexJoints8"/>.
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
