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
    /// The vertex fragment type with Position, Normal and Tangent.<br/>
    /// Valid types are:<br/>
    /// <list type="table">
    /// <item><see cref="VertexPosition"/></item>
    /// <item><see cref="VertexPositionNormal"/></item>
    /// <item><see cref="VertexPositionNormalTangent"/></item>
    /// </list>
    /// </typeparam>
    /// <typeparam name="TvM">
    /// The vertex fragment type with Colors and Texture Coordinates.<br/>
    /// Valid types are:<br/>
    /// <list type="table">
    /// <item><see cref="VertexEmpty"/></item>
    /// <item><see cref="VertexColor1"/></item>
    /// <item><see cref="VertexTexture1"/></item>
    /// <item><see cref="VertexColor1Texture1"/></item>
    /// <item><see cref="VertexColor1Texture2"/></item>
    /// <item><see cref="VertexColor1Texture1"/></item>
    /// <item><see cref="VertexColor2Texture2"/></item>
    /// </list>
    /// </typeparam>
    /// <typeparam name="TvS">
    /// The vertex fragment type with Skin Joint Weights.<br/>
    /// Valid types are:<br/>
    /// <list type="table">
    /// <item><see cref="VertexEmpty"/></item>
    /// <item><see cref="VertexJoints4"/></item>
    /// <item><see cref="VertexJoints8"/></item>
    /// </list>
    /// </typeparam>
    public class MeshBuilder<TMaterial, TvG, TvM, TvS> : BaseBuilder, IMeshBuilder<TMaterial>, ICloneable
        where TvG : struct, IVertexGeometry
        where TvM : struct, IVertexMaterial
        where TvS : struct, IVertexSkinning
    {
        #region lifecycle

        public MeshBuilder(string name = null)
            : base(name)
        {
            // this is the recomended preprocesor for release/production
            _VertexPreprocessor = new VertexPreprocessor<TvG, TvM, TvS>();
            _VertexPreprocessor.SetSanitizerPreprocessors();
        }

        Object ICloneable.Clone()
        {
            return new MeshBuilder<TMaterial, TvG, TvM, TvS>(this);
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
            : base(other)
        {
            Guard.NotNull(other, nameof(other));

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

        /// <inheritdoc/>
        public bool IsEmpty => Primitives.Sum(item => item.Vertices.Count) == 0;

        public VertexPreprocessor<TvG, TvM, TvS> VertexPreprocessor
        {
            get => _VertexPreprocessor;
            set => _VertexPreprocessor = value;
        }

        /// <inheritdoc/>
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

        /// <summary>
        /// Creates, or uses an existing primitive using <paramref name="material"/>.
        /// </summary>
        /// <param name="material">The material used by the primitive.</param>
        /// <param name="primitiveVertexCount">
        /// Defines the primitive type.<br/>
        /// <list type="number">
        /// <item>Points</item>
        /// <item>Lines</item>
        /// <item>Triangles (Default)</item>
        /// </list>
        /// </param>
        /// <returns>An instance of <see cref="IPrimitiveBuilder"/>.</returns>
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

        public void AddMesh(IMeshBuilder<TMaterial> mesh, Matrix4x4 vertexTransform)
        {
            if (mesh == null) return;

            if (vertexTransform == Matrix4x4.Identity)
            {
                AddMesh<TMaterial>(mesh, null, null);
                return;
            }

            AddMesh<TMaterial>(mesh, null, v => VertexBuilder<TvG, TvM, TvS>.CreateFrom(v).TransformedBy(vertexTransform));
        }

        public void AddMesh(IMeshBuilder<TMaterial> mesh, Func<TMaterial, TMaterial> materialTransform = null, Converter<IVertexBuilder, VertexBuilder<TvG, TvM, TvS>> vertexTransform = null)
        {
            if (mesh == null) return;

            if (materialTransform == null) materialTransform = m => m;
            if (vertexTransform == null) vertexTransform = v => VertexBuilder<TvG, TvM, TvS>.CreateFrom(v);

            AddMesh<TMaterial>(mesh, materialTransform, vertexTransform);
        }

        public void AddMesh<TSourceMaterial>(IMeshBuilder<TSourceMaterial> mesh, Func<TSourceMaterial, TMaterial> materialTransform, Converter<IVertexBuilder, VertexBuilder<TvG, TvM, TvS>> vertexTransform = null)
        {
            if (mesh == null) return;

            Guard.NotNull(materialTransform, nameof(materialTransform));
            if (vertexTransform == null) vertexTransform = v => VertexBuilder<TvG, TvM, TvS>.CreateFrom(v);

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

            // TODO: remove collapsed primitives.
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

    /// <inheritdoc/>
    public class MeshBuilder<TvG, TvM, TvS> : MeshBuilder<Materials.MaterialBuilder, TvG, TvM, TvS>
        where TvG : struct, IVertexGeometry
        where TvM : struct, IVertexMaterial
        where TvS : struct, IVertexSkinning
    {
        public MeshBuilder(string name = null)
            : base(name) { }
    }

    /// <inheritdoc/>
    public class MeshBuilder<TvG, TvM> : MeshBuilder<Materials.MaterialBuilder, TvG, TvM, VertexEmpty>
        where TvG : struct, IVertexGeometry
        where TvM : struct, IVertexMaterial
    {
        public MeshBuilder(string name = null)
            : base(name) { }
    }

    /// <inheritdoc/>
    public class MeshBuilder<TvG> : MeshBuilder<Materials.MaterialBuilder, TvG, VertexEmpty, VertexEmpty>
        where TvG : struct, IVertexGeometry
    {
        public MeshBuilder(string name = null)
            : base(name) { }
    }
}
