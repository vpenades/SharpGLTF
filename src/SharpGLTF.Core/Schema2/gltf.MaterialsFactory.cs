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
            if (this._pbrMetallicRoughness == null) this._pbrMetallicRoughness = new MaterialPBRMetallicRoughness();

            this.RemoveExtensions<MaterialUnlit>();
            this.RemoveExtensions<MaterialClearCoat>();
            this.RemoveExtensions<MaterialPBRSpecularGlossiness>();
        }

        /// <summary>
        /// Initializes this <see cref="Material"/> instance with PBR Metallic Roughness attributes and Clear Coat extension.
        /// </summary>
        public void InitializePBRMetallicRoughnessClearCoat()
        {
            if (this._pbrMetallicRoughness == null) this._pbrMetallicRoughness = new MaterialPBRMetallicRoughness();

            this.RemoveExtensions<MaterialUnlit>();
            this.RemoveExtensions<MaterialPBRSpecularGlossiness>();
            this.SetExtension(new MaterialClearCoat(this));
        }

        /// <summary>
        /// Initializes this <see cref="Material"/> instance with PBR Specular Glossiness attributes.
        /// </summary>
        /// <param name="useFallback">true to add a PBRMetallicRoughness fallback material.</param>
        public void InitializePBRSpecularGlossiness(bool useFallback = false)
        {
            if (useFallback)
            {
                if (this._pbrMetallicRoughness == null) this._pbrMetallicRoughness = new MaterialPBRMetallicRoughness();
            }
            else
            {
                this._pbrMetallicRoughness = null;
            }

            this.RemoveExtensions<MaterialUnlit>();
            this.RemoveExtensions<MaterialClearCoat>();
            this.SetExtension(new MaterialPBRSpecularGlossiness(this));
        }

        /// <summary>
        /// Initializes this <see cref="Material"/> instance with Unlit attributes.
        /// </summary>
        public void InitializeUnlit()
        {
            if (this._pbrMetallicRoughness == null) this._pbrMetallicRoughness = new MaterialPBRMetallicRoughness();

            this.RemoveExtensions<MaterialPBRSpecularGlossiness>();
            this.SetExtension(new MaterialUnlit(this));
        }

        private IEnumerable<MaterialChannel> _GetChannels()
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

            var clearCoat = this.GetExtension<MaterialClearCoat>();
            if (clearCoat != null)
            {
                var channels = clearCoat.GetChannels(this);
                foreach (var c in channels) yield return c;
            }

            yield return new MaterialChannel
                (
                this, "Normal",
                _GetNormalTexture,
                MaterialNormalTextureInfo.ScaleDefault,
                () => _GetNormalTexture(false)?.Scale ?? MaterialNormalTextureInfo.ScaleDefault,
                value => _GetNormalTexture(true).Scale = value
                );

            yield return new MaterialChannel
                (
                this, "Occlusion",
                _GetOcclusionTexture,
                MaterialOcclusionTextureInfo.StrengthDefault,
                () => _GetOcclusionTexture(false)?.Strength ?? MaterialOcclusionTextureInfo.StrengthDefault,
                value => _GetOcclusionTexture(true).Strength = value
                );

            yield return new MaterialChannel
                (
                this, "Emissive",
                _GetEmissiveTexture,
                Vector4.Zero,
                () => this._EmissiveColor,
                value => this._EmissiveColor = value
                );
        }

        private Vector4 _EmissiveColor
        {
            get => new Vector4(_emissiveFactor.AsValue(_emissiveFactorDefault), 1);
            set => _emissiveFactor = new Vector3(value.X, value.Y, value.Z).AsNullable(_emissiveFactorDefault, Vector3.Zero, Vector3.One);
        }

        private MaterialNormalTextureInfo _GetNormalTexture(bool create)
        {
            if (create && _normalTexture == null) _normalTexture = new MaterialNormalTextureInfo();
            return _normalTexture;
        }

        private MaterialOcclusionTextureInfo _GetOcclusionTexture(bool create)
        {
            if (create && _occlusionTexture == null) _occlusionTexture = new MaterialOcclusionTextureInfo();
            return _occlusionTexture;
        }

        private TextureInfo _GetEmissiveTexture(bool create)
        {
            if (create && _emissiveTexture == null) _emissiveTexture = new TextureInfo();
            return _emissiveTexture;
        }

        #endregion
    }

    internal sealed partial class MaterialPBRMetallicRoughness
    {
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

        public Vector4 Color
        {
            get => _baseColorFactor.AsValue(_baseColorFactorDefault);
            set => _baseColorFactor = value.AsNullable(_baseColorFactorDefault);
        }

        public static Vector4 ParameterDefault => new Vector4((float)_metallicFactorDefault, (float)_roughnessFactorDefault, 0, 0);

        public Vector4 Parameter
        {
            get
            {
                return new Vector4
                    (
                    (float)_metallicFactor.AsValue( _metallicFactorDefault),
                    (float)_roughnessFactor.AsValue(_roughnessFactorDefault),
                    0,
                    0
                    );
            }
            set
            {
                _metallicFactor  = ((double)value.X).AsNullable( _metallicFactorDefault,  _metallicFactorMinimum,  _metallicFactorMaximum);
                _roughnessFactor = ((double)value.Y).AsNullable(_roughnessFactorDefault, _roughnessFactorMinimum, _roughnessFactorMaximum);
            }
        }

        public IEnumerable<MaterialChannel> GetChannels(Material material)
        {
            yield return new MaterialChannel
                (
                material, "BaseColor",
                _GetBaseTexture,
                _baseColorFactorDefault,
                () => this.Color,
                value => this.Color = value
                );

            yield return new MaterialChannel
                (
                material,
                "MetallicRoughness",
                _GetMetallicTexture,
                ParameterDefault,
                () => this.Parameter,
                value => this.Parameter = value
                );
        }
    }

    internal sealed partial class MaterialPBRSpecularGlossiness
    {
        #pragma warning disable CA1801 // Review unused parameters
        internal MaterialPBRSpecularGlossiness(Material material) { }
        #pragma warning restore CA1801 // Review unused parameters

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

        public Vector4 Color
        {
            get => _diffuseFactor.AsValue(_diffuseFactorDefault);
            set => _diffuseFactor = value.AsNullable(_diffuseFactorDefault);
        }

        public static Vector4 ParameterDefault => new Vector4(_specularFactorDefault, (float)_glossinessFactorDefault);

        public Vector4 Parameter
        {
            get
            {
                return new Vector4
                    (
                    _specularFactor.AsValue(_specularFactorDefault),
                    (float)_glossinessFactor.AsValue(_glossinessFactorDefault)
                    );
            }
            set
            {
                _specularFactor = new Vector3(value.X, value.Y, value.Z).AsNullable(_specularFactorDefault);
                _glossinessFactor = ((double)value.W).AsNullable(_glossinessFactorDefault, _glossinessFactorMinimum, _glossinessFactorMaximum);
            }
        }

        public IEnumerable<MaterialChannel> GetChannels(Material material)
        {
            yield return new MaterialChannel
                (
                material, "Diffuse",
                _GetDiffuseTexture,
                _diffuseFactorDefault,
                () => this.Color,
                value => this.Color = value
                );

            yield return new MaterialChannel
                (
                material, "SpecularGlossiness",
                _GetGlossinessTexture,
                ParameterDefault,
                () => this.Parameter,
                value => this.Parameter = value
                );
        }
    }

    internal sealed partial class MaterialUnlit
    {
        #pragma warning disable CA1801 // Review unused parameters
        internal MaterialUnlit(Material material) { }
        #pragma warning restore CA1801 // Review unused parameters
    }

    internal sealed partial class MaterialClearCoat
    {
        #pragma warning disable CA1801 // Review unused parameters
        internal MaterialClearCoat(Material material) { }
        #pragma warning restore CA1801 // Review unused parameters

        protected override IEnumerable<ExtraProperties> GetLogicalChildren()
        {
            return base.GetLogicalChildren().ConcatItems(_clearcoatTexture, _clearcoatRoughnessTexture, _clearcoatNormalTexture);
        }

        private TextureInfo _GetClearCoatTexture(bool create)
        {
            if (create && _clearcoatTexture == null) _clearcoatTexture = new TextureInfo();
            return _clearcoatTexture;
        }

        private TextureInfo _GetClearCoatRoughnessTexture(bool create)
        {
            if (create && _clearcoatRoughnessTexture == null) _clearcoatRoughnessTexture = new TextureInfo();
            return _clearcoatRoughnessTexture;
        }

        private MaterialNormalTextureInfo _GetClearCoatNormalTexture(bool create)
        {
            if (create && _clearcoatNormalTexture == null) _clearcoatNormalTexture = new MaterialNormalTextureInfo();
            return _clearcoatNormalTexture;
        }

        public IEnumerable<MaterialChannel> GetChannels(Material material)
        {
            yield return new MaterialChannel
                (
                material, "ClearCoat",
                _GetClearCoatTexture,
                (float)_clearcoatFactorDefault,
                () => (float)this._clearcoatFactor.AsValue(_clearcoatFactorDefault),
                value => this._clearcoatFactor = value
                );

            yield return new MaterialChannel
                (
                material, "ClearCoatRoughness",
                _GetClearCoatRoughnessTexture,
                (float)_clearcoatRoughnessFactorDefault,
                () => (float)this._clearcoatRoughnessFactor.AsValue(_clearcoatRoughnessFactorDefault),
                value => this._clearcoatRoughnessFactor = value
                );

            yield return new MaterialChannel
                (
                material, "ClearCoatNormal",
                _GetClearCoatNormalTexture,
                MaterialNormalTextureInfo.ScaleDefault,
                () => _GetClearCoatNormalTexture(false)?.Scale ?? MaterialNormalTextureInfo.ScaleDefault,
                value => _GetClearCoatNormalTexture(true).Scale = value
                );
        }
    }
}
