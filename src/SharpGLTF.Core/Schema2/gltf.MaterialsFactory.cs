using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;

namespace SharpGLTF.Schema2
{
    public partial class Material
    {
        #region API

        /// <summary>
        /// Initializes this <see cref="Material"/> instance with PBR Metallic Roughness attributes.
        /// </summary>
        public void InitializePBRMetallicRoughness()
        {
            this._pbrMetallicRoughness = new MaterialPBRMetallicRoughness();

            this.RemoveExtensions<MaterialPBRSpecularGlossiness>();
            this.RemoveExtensions<MaterialUnlit>();
        }

        /// <summary>
        /// Initializes this <see cref="Material"/> instance with PBR Specular Glossiness attributes.
        /// </summary>
        public void InitializePBRSpecularGlossiness()
        {
            this.RemoveExtensions<MaterialUnlit>();
            this.SetExtension(new MaterialPBRSpecularGlossiness(this));
        }

        /// <summary>
        /// Initializes this <see cref="Material"/> instance with Unlit attributes.
        /// </summary>
        public void InitializeUnlit()
        {
            this.RemoveExtensions<MaterialPBRSpecularGlossiness>();
            this.SetExtension(new MaterialUnlit(this));
        }

        private IEnumerable<MaterialChannelView> _GetChannels()
        {
            if (_pbrMetallicRoughness != null)
            {
                var channels = _pbrMetallicRoughness.GetChannels(this);
                foreach (var c in channels) yield return c;
            }

            var pbrSpecGloss = this.GetExtension<MaterialPBRSpecularGlossiness>();
            if (pbrSpecGloss != null)
            {
                var channels = pbrSpecGloss.GetChannels(this);
                foreach (var c in channels) yield return c;
            }

            yield return new MaterialChannelView
                (
                this,
                "Normal",
                _GetNormalTexture,
                () => _GetNormalTexture(false) == null ? Vector4.One : new Vector4(1, 1, 1, _GetNormalTexture(false).Factor),
                value => _GetNormalTexture(true).Factor = value.W
                );

            yield return new MaterialChannelView
                (
                this,
                "Occlusion",
                _GetOcclusionTexture,
                () => _GetOcclusionTexture(false) == null ? Vector4.One : new Vector4(1, 1, 1, _GetOcclusionTexture(false).Factor),
                value => _GetOcclusionTexture(true).Factor = value.W
                );

            yield return new MaterialChannelView
                (
                this,
                "Emissive",
                _GetEmissiveTexture,
                () => { var rgb = _emissiveFactor.AsValue(_emissiveFactorDefault); return new Vector4(rgb, 1); },
                value => _emissiveFactor = new Vector3(value.X, value.Y, value.Z).AsNullable(_emissiveFactorDefault, Vector3.Zero, Vector3.One)
                );
        }

        #endregion
    }

    public partial class ModelRoot
    {
        /// <summary>
        /// Creates a new <see cref="Material"/> instance and adds it to <see cref="ModelRoot.LogicalMaterials"/>.
        /// </summary>
        /// <param name="name">The name of the instance.</param>
        /// <returns>A <see cref="Material"/> instance.</returns>
        public Material CreateMaterial(string name = null)
        {
            var mat = new Material();
            mat.Name = name;

            _materials.Add(mat);

            return mat;
        }
    }

    internal sealed partial class MaterialPBRMetallicRoughness
    {
        /// <inheritdoc />
        protected override IEnumerable<ExtraProperties> GetLogicalChildren()
        {
            return base.GetLogicalChildren().ConcatItems(_baseColorTexture, _metallicRoughnessTexture);
        }

        private TextureInfo _GetBaseTexture(bool create)
        {
            if (create && _baseColorTexture == null) _baseColorTexture = new TextureInfo();
            return _baseColorTexture;
        }

        private TextureInfo _GetMetallicTexture(bool create)
        {
            if (create && _metallicRoughnessTexture == null) _metallicRoughnessTexture = new TextureInfo();
            return _metallicRoughnessTexture;
        }

        public IEnumerable<MaterialChannelView> GetChannels(Material material)
        {
            yield return new MaterialChannelView
                (
                material,
                "BaseColor",
                _GetBaseTexture,
                () => _baseColorFactor.AsValue(_baseColorFactorDefault),
                value => _baseColorFactor = value.AsNullable(_baseColorFactorDefault)
                );

            yield return new MaterialChannelView
                (
                material,
                "Metallic",
                _GetMetallicTexture,
                () => new Vector4(1, 1, 1, (float)(_metallicFactor ?? _metallicFactorDefault)),
                value => _metallicFactor = ((double)value.W).AsNullable(_metallicFactorDefault, _metallicFactorMaximum, _metallicFactorMaximum)
                );

            yield return new MaterialChannelView
                (
                material,
                "Roughness",
                null,
                () => new Vector4(1, 1, 1, (float)(_roughnessFactor ?? _roughnessFactorDefault)),
                value => _roughnessFactor = ((double)value.W).AsNullable(_roughnessFactorDefault, _roughnessFactorMinimum, _roughnessFactorMaximum)
                );
        }
    }

    internal sealed partial class MaterialPBRSpecularGlossiness
    {
        internal MaterialPBRSpecularGlossiness(Material material) { }

        /// <inheritdoc />
        protected override IEnumerable<ExtraProperties> GetLogicalChildren()
        {
            return base.GetLogicalChildren().ConcatItems(_diffuseTexture, _specularGlossinessTexture);
        }

        private TextureInfo _GetDiffuseTexture(bool create)
        {
            if (create && _diffuseTexture == null) _diffuseTexture = new TextureInfo();
            return _diffuseTexture;
        }

        private TextureInfo _GetGlossinessTexture(bool create)
        {
            if (create && _specularGlossinessTexture == null) _specularGlossinessTexture = new TextureInfo();
            return _specularGlossinessTexture;
        }

        public IEnumerable<MaterialChannelView> GetChannels(Material material)
        {
            yield return new MaterialChannelView
                (
                material,
                "Diffuse",
                _GetDiffuseTexture,
                () => _diffuseFactor.AsValue(_diffuseFactorDefault),
                value => _diffuseFactor = value.AsNullable(_diffuseFactorDefault)
                );

            yield return new MaterialChannelView
                (
                material,
                "Glossiness",
                _GetGlossinessTexture,
                () => new Vector4(1, 1, 1, (float)_glossinessFactor.AsValue(_glossinessFactorDefault)),
                value => _glossinessFactor = ((double)value.W).AsNullable(_glossinessFactorDefault, _glossinessFactorMinimum, _glossinessFactorMaximum)
                );

            yield return new MaterialChannelView
                (
                material,
                "Specular",
                null,
                () => { var rgb = _specularFactor.AsValue(_specularFactorDefault); return new Vector4(rgb, 1); },
                value => _specularFactor = new Vector3(value.X, value.Y, value.Z).AsNullable(_specularFactorDefault)
                );
        }
    }

    internal sealed partial class MaterialUnlit
    {
        internal MaterialUnlit(Material material) { }
    }
}
