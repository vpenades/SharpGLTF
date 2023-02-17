using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

using SharpGLTF.Validation;

namespace SharpGLTF.Schema2
{
    public partial class Material
    {
        #region factory

        internal void ClearExtensions()
        {
            this.RemoveExtensions<MaterialIOR>();
            this.RemoveExtensions<MaterialUnlit>();
            this.RemoveExtensions<MaterialSheen>();
            this.RemoveExtensions<MaterialClearCoat>();
            this.RemoveExtensions<MaterialSpecular>();
            this.RemoveExtensions<MaterialTransmission>();
            this.RemoveExtensions<MaterialVolume>();
            this.RemoveExtensions<MaterialPBRSpecularGlossiness>();
        }

        /// <summary>
        /// Initializes this <see cref="Material"/> instance with Unlit attributes.
        /// </summary>
        public void InitializeUnlit()
        {
            if (this._pbrMetallicRoughness == null) this._pbrMetallicRoughness = new MaterialPBRMetallicRoughness();

            ClearExtensions();
            this.UseExtension<MaterialUnlit>();
        }

        /// <summary>
        /// Initializes this <see cref="Material"/> instance with PBR Metallic Roughness attributes.
        /// </summary>
        /// <param name="extensionNames">
        /// Extension names.
        /// Current valid names are: "ClearCoat", "Transmission", "Sheen"
        /// </param>
        public void InitializePBRMetallicRoughness(params string[] extensionNames)
        {
            Guard.NotNull(extensionNames, nameof(extensionNames));

            if (this._pbrMetallicRoughness == null) this._pbrMetallicRoughness = new MaterialPBRMetallicRoughness();

            ClearExtensions();

            foreach (var extn in extensionNames)
            {
                if (extn == "Sheen") this.UseExtension<MaterialSheen>();
                if (extn == "Specular") this.UseExtension<MaterialSpecular>();
                if (extn == "ClearCoat") this.UseExtension<MaterialClearCoat>();
                if (extn == "Transmission") this.UseExtension<MaterialTransmission>();
                if (extn == "Volume") this.UseExtension<MaterialVolume>();
            }
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

            ClearExtensions();
            this.UseExtension<MaterialPBRSpecularGlossiness>();
        }

        #endregion

        #region channels API

        private IEnumerable<MaterialChannel> _GetChannels()
        {
            var channels = _pbrMetallicRoughness?.GetChannels(this);
            if (channels != null) { foreach (var c in channels) yield return c; }

            channels = this.GetExtension<MaterialPBRSpecularGlossiness>()?.GetChannels(this);
            if (channels != null) { foreach (var c in channels) yield return c; }

            channels = this.GetExtension<MaterialClearCoat>()?.GetChannels(this);
            if (channels != null) { foreach (var c in channels) yield return c; }

            channels = this.GetExtension<MaterialTransmission>()?.GetChannels(this);
            if (channels != null) { foreach (var c in channels) yield return c; }

            channels = this.GetExtension<MaterialSheen>()?.GetChannels(this);
            if (channels != null) { foreach (var c in channels) yield return c; }

            channels = this.GetExtension<MaterialSpecular>()?.GetChannels(this);
            if (channels != null) { foreach (var c in channels) yield return c; }

            channels = this.GetExtension<MaterialVolume>()?.GetChannels(this);
            if (channels != null) { foreach (var c in channels) yield return c; }

            channels = this.GetExtension<MaterialIridescence>()?.GetChannels(this);
            if (channels != null) { foreach (var c in channels) yield return c; }

            var normalParam = new _MaterialParameter<float>(
                _MaterialParameterKey.NormalScale,
                MaterialNormalTextureInfo.ScaleDefault,
                () => _GetNormalTexture(false)?.Scale ?? MaterialNormalTextureInfo.ScaleDefault,
                value => _GetNormalTexture(true).Scale = value);

            var occlusionParam = new _MaterialParameter<float>(
                _MaterialParameterKey.OcclusionStrength,
                MaterialOcclusionTextureInfo.StrengthDefault,
                () => _GetOcclusionTexture(false)?.Strength ?? MaterialOcclusionTextureInfo.StrengthDefault,
                value => _GetOcclusionTexture(true).Strength = value);

            var emissiveFactorParam = new _MaterialParameter<Vector3>(
                _MaterialParameterKey.RGB,
                _emissiveFactorDefault,
                () => this._emissiveFactor.AsValue(_emissiveFactorDefault),
                value => this._emissiveFactor = value.AsNullable(_emissiveFactorDefault, Vector3.Zero, Vector3.One));

            yield return new MaterialChannel(this, "Normal", _GetNormalTexture, normalParam);
            yield return new MaterialChannel(this, "Occlusion", _GetOcclusionTexture, occlusionParam);
            yield return new MaterialChannel(this, "Emissive", _GetEmissiveTexture, emissiveFactorParam, MaterialEmissiveStrength.GetParameter(this));
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
            return base.GetLogicalChildren().ConcatElements(_baseColorTexture, _metallicRoughnessTexture);
        }

        protected override void OnValidateContent(ValidationContext validate)
        {
            base.OnValidateContent(validate);

            if (_baseColorFactor.HasValue)
            {
                Guard.MustBeBetweenOrEqualTo(_baseColorFactor.Value.X, 0, 1, nameof(_baseColorFactor));
                Guard.MustBeBetweenOrEqualTo(_baseColorFactor.Value.Y, 0, 1, nameof(_baseColorFactor));
                Guard.MustBeBetweenOrEqualTo(_baseColorFactor.Value.Z, 0, 1, nameof(_baseColorFactor));
                Guard.MustBeBetweenOrEqualTo(_baseColorFactor.Value.W, 0, 1, nameof(_baseColorFactor));
            }

            if (_metallicFactor.HasValue)
            {
                Guard.MustBeBetweenOrEqualTo(_metallicFactor.Value, _metallicFactorMinimum, _metallicFactorMaximum, nameof(_metallicFactor));
            }
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

        public float MetallicFactor
        {
            get => (float)_metallicFactor.AsValue(_metallicFactorDefault);
            set => _metallicFactor = ((double)value).AsNullable(_metallicFactorDefault, _metallicFactorMinimum, _metallicFactorMaximum);
        }

        public float RoughnessFactor
        {
            get => (float)_roughnessFactor.AsValue(_roughnessFactorDefault);
            set => _roughnessFactor = ((double)value).AsNullable(_roughnessFactorDefault, _roughnessFactorMinimum, _roughnessFactorMaximum);
        }

        public IEnumerable<MaterialChannel> GetChannels(Material material)
        {
            var colorParam = new _MaterialParameter<Vector4>(_MaterialParameterKey.RGBA, _baseColorFactorDefault, () => Color, v => Color = v);
            var metallicParam = new _MaterialParameter<float>(_MaterialParameterKey.MetallicFactor, (float)_metallicFactorDefault, () => MetallicFactor, v => MetallicFactor = v);
            var roughnessParam = new _MaterialParameter<float>(_MaterialParameterKey.RoughnessFactor, (float)_roughnessFactorDefault, () => RoughnessFactor, v => RoughnessFactor = v);

            yield return new MaterialChannel(material, "BaseColor", _GetBaseTexture, colorParam);
            yield return new MaterialChannel(material, "MetallicRoughness", _GetMetallicTexture, metallicParam, roughnessParam);
        }
    }

    /// <summary>
    /// <see cref="MaterialPBRSpecularGlossiness"/> has been deprecated by khronos in favour of <see cref="MaterialPBRMetallicRoughness"/> + <see cref="MaterialSpecular"/>
    /// </summary>
    internal sealed partial class MaterialPBRSpecularGlossiness
    {
        #pragma warning disable CA1801 // Review unused parameters
        internal MaterialPBRSpecularGlossiness(Material material) { }
        #pragma warning restore CA1801 // Review unused parameters

        protected override IEnumerable<ExtraProperties> GetLogicalChildren()
        {
            return base.GetLogicalChildren().ConcatElements(_diffuseTexture, _specularGlossinessTexture);
        }

        protected override void OnValidateContent(ValidationContext validate)
        {
            base.OnValidateContent(validate);

            if (_specularFactor.HasValue)
            {
                Guard.MustBeBetweenOrEqualTo(_specularFactor.Value.X, 0, 1, nameof(_specularFactor));
                Guard.MustBeBetweenOrEqualTo(_specularFactor.Value.Y, 0, 1, nameof(_specularFactor));
                Guard.MustBeBetweenOrEqualTo(_specularFactor.Value.Z, 0, 1, nameof(_specularFactor));
            }

            if (_glossinessFactor.HasValue)
            {
                Guard.MustBeBetweenOrEqualTo(_glossinessFactor.Value, _glossinessFactorMinimum, _glossinessFactorMaximum, nameof(_glossinessFactor));
            }
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

        public Vector4 DiffuseFactor
        {
            get => _diffuseFactor.AsValue(_diffuseFactorDefault);
            set => _diffuseFactor = _diffuseFactor = value.AsNullable(_diffuseFactorDefault);
        }

        public Vector3 SpecularFactor
        {
            get => _specularFactor.AsValue(_specularFactorDefault);
            set => _specularFactor = value.AsNullable(_specularFactorDefault);
        }

        public float GlossinessFactor
        {
            get => (float)_glossinessFactor.AsValue(_glossinessFactorDefault);
            set => _glossinessFactor = ((double)value).AsNullable(_glossinessFactorDefault, _glossinessFactorMinimum, _glossinessFactorMaximum);
        }

        public IEnumerable<MaterialChannel> GetChannels(Material material)
        {
            var diffuseParam = new _MaterialParameter<Vector4>(_MaterialParameterKey.RGBA, _diffuseFactorDefault, () => DiffuseFactor, v => DiffuseFactor = v);
            var specularParam = new _MaterialParameter<Vector3>(_MaterialParameterKey.SpecularFactor, _specularFactorDefault, () => SpecularFactor, v => SpecularFactor = v);
            var glossinessParam = new _MaterialParameter<float>(_MaterialParameterKey.GlossinessFactor, (float)_glossinessFactorDefault, () => GlossinessFactor, v => GlossinessFactor = v);

            yield return new MaterialChannel(material, "Diffuse", _GetDiffuseTexture, diffuseParam);
            yield return new MaterialChannel(material, "SpecularGlossiness", _GetGlossinessTexture, specularParam, glossinessParam);
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
            return base.GetLogicalChildren().ConcatElements(_clearcoatTexture, _clearcoatRoughnessTexture, _clearcoatNormalTexture);
        }

        protected override void OnValidateContent(ValidationContext validate)
        {
            base.OnValidateContent(validate);

            if (_clearcoatFactor.HasValue)
            {
                Guard.MustBeBetweenOrEqualTo(_clearcoatFactor.Value, _clearcoatFactorMinimum, _clearcoatFactorMaximum, nameof(_clearcoatFactor));
            }

            if (_clearcoatRoughnessFactor.HasValue)
            {
                Guard.MustBeBetweenOrEqualTo(_clearcoatRoughnessFactor.Value, _clearcoatRoughnessFactorMinimum, _clearcoatRoughnessFactorMaximum, nameof(_clearcoatRoughnessFactor));
            }
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

        public float ClearCoatFactor
        {
            get => (float)this._clearcoatFactor.AsValue(_clearcoatFactorDefault);
            set => this._clearcoatFactor = value.AsNullable((float)_clearcoatFactorDefault);
        }

        public float RoughnessFactor
        {
            get => (float)this._clearcoatRoughnessFactor.AsValue(_clearcoatRoughnessFactorDefault);
            set => this._clearcoatRoughnessFactor = value.AsNullable((float)_clearcoatRoughnessFactorDefault);
        }

        public IEnumerable<MaterialChannel> GetChannels(Material material)
        {
            var clearCoatParam = new _MaterialParameter<float>(_MaterialParameterKey.ClearCoatFactor, (float)_clearcoatFactorDefault, () => ClearCoatFactor, v => ClearCoatFactor = v);
            var roughnessParam = new _MaterialParameter<float>(_MaterialParameterKey.RoughnessFactor, (float)_clearcoatRoughnessFactorDefault, () => RoughnessFactor, v => RoughnessFactor = v);
            var normScaleParam = new _MaterialParameter<float>(_MaterialParameterKey.NormalScale,
                MaterialNormalTextureInfo.ScaleDefault,
                () => _GetClearCoatNormalTexture(false)?.Scale ?? MaterialNormalTextureInfo.ScaleDefault,
                v => _GetClearCoatNormalTexture(true).Scale = v);

            yield return new MaterialChannel(material, "ClearCoat", _GetClearCoatTexture, clearCoatParam);
            yield return new MaterialChannel(material, "ClearCoatRoughness", _GetClearCoatRoughnessTexture, roughnessParam);
            yield return new MaterialChannel(material, "ClearCoatNormal", _GetClearCoatNormalTexture, normScaleParam);
        }
    }

    internal sealed partial class MaterialTransmission
    {
        #pragma warning disable CA1801 // Review unused parameters
        internal MaterialTransmission(Material material) { }
        #pragma warning restore CA1801 // Review unused parameters

        protected override IEnumerable<ExtraProperties> GetLogicalChildren()
        {
            return base.GetLogicalChildren().ConcatElements(_transmissionTexture);
        }

        protected override void OnValidateContent(ValidationContext validate)
        {
            base.OnValidateContent(validate);

            if (_transmissionFactor.HasValue)
            {
                Guard.MustBeBetweenOrEqualTo(_transmissionFactor.Value, _transmissionFactorMinimum, _transmissionFactorMaximum, nameof(_transmissionFactor));
            }
        }

        public float TransmissionFactor
        {
            get => (float)this._transmissionFactor.AsValue(_transmissionFactorDefault);
            set => this._transmissionFactor = value.AsNullable((float)_transmissionFactorDefault);
        }

        public IEnumerable<MaterialChannel> GetChannels(Material material)
        {
            var transmissionParam = new _MaterialParameter<float>(_MaterialParameterKey.TransmissionFactor, (float)_transmissionFactorDefault, () => TransmissionFactor, v => TransmissionFactor = v);

            yield return new MaterialChannel(material, "Transmission", _GetTransmissionTexture, transmissionParam);
        }

        private TextureInfo _GetTransmissionTexture(bool create)
        {
            if (create && _transmissionTexture == null) _transmissionTexture = new TextureInfo();
            return _transmissionTexture;
        }
    }

    internal sealed partial class MaterialSheen
    {
        #pragma warning disable CA1801 // Review unused parameters
        internal MaterialSheen(Material material) { }
        #pragma warning restore CA1801 // Review unused parameters

        protected override IEnumerable<ExtraProperties> GetLogicalChildren()
        {
            return base.GetLogicalChildren().ConcatElements(_sheenColorTexture, _sheenRoughnessTexture);
        }

        protected override void OnValidateContent(ValidationContext validate)
        {
            base.OnValidateContent(validate);

            if (_sheenColorFactor.HasValue)
            {
                Guard.MustBeBetweenOrEqualTo(_sheenColorFactor.Value.X, 0, 1, nameof(_sheenColorFactor));
                Guard.MustBeBetweenOrEqualTo(_sheenColorFactor.Value.Y, 0, 1, nameof(_sheenColorFactor));
                Guard.MustBeBetweenOrEqualTo(_sheenColorFactor.Value.Z, 0, 1, nameof(_sheenColorFactor));
            }

            if (_sheenRoughnessFactor.HasValue)
            {
                Guard.MustBeBetweenOrEqualTo(_sheenRoughnessFactor.Value, _sheenRoughnessFactorMinimum, _sheenRoughnessFactorMaximum, nameof(_sheenRoughnessFactor));
            }
        }

        public Vector3 ColorFactor
        {
            get => _sheenColorFactor.AsValue(_sheenColorFactorDefault);
            set => this._sheenColorFactor = value.AsNullable(_sheenColorFactorDefault);
        }

        public float RoughnessFactor
        {
            get => _sheenRoughnessFactor.AsValue(_sheenRoughnessFactorDefault);
            set => this._sheenRoughnessFactor = value.AsNullable(_sheenRoughnessFactorDefault);
        }

        public IEnumerable<MaterialChannel> GetChannels(Material material)
        {
            var colorParam = new _MaterialParameter<Vector3>(_MaterialParameterKey.RGB, _sheenColorFactorDefault, () => ColorFactor, v => ColorFactor = v);
            var roughnessParam = new _MaterialParameter<float>(_MaterialParameterKey.RoughnessFactor, _sheenRoughnessFactorDefault, () => RoughnessFactor, v => RoughnessFactor = v);

            yield return new MaterialChannel(material, "SheenColor", _GetSheenColorTexture, colorParam);
            yield return new MaterialChannel(material, "SheenRoughness", _GetSheenRoughnessTexture, roughnessParam);
        }

        private TextureInfo _GetSheenColorTexture(bool create)
        {
            if (create && _sheenColorTexture == null) _sheenColorTexture = new TextureInfo();
            return _sheenColorTexture;
        }

        private TextureInfo _GetSheenRoughnessTexture(bool create)
        {
            if (create && _sheenRoughnessTexture == null) _sheenRoughnessTexture = new TextureInfo();
            return _sheenRoughnessTexture;
        }
    }

    internal sealed partial class MaterialIOR
    {
        #pragma warning disable CA1801 // Review unused parameters
        internal MaterialIOR(Material material) { }
        #pragma warning restore CA1801 // Review unused parameters

        protected override void OnValidateContent(ValidationContext validate)
        {
            base.OnValidateContent(validate);

            if (_ior == 0) return; // a value of 0 is allowed by the spec as a special value
            if (_ior < 1) throw new ArgumentOutOfRangeException(nameof(IndexOfRefraction));
        }

        public static float DefaultIndexOfRefraction => (float)_iorDefault;

        public float IndexOfRefraction
        {
            get => (float)(this._ior ?? _iorDefault);
            set => this._ior = ((double)value).AsNullable(_iorDefault);
        }
    }

    internal sealed partial class MaterialSpecular
    {
        #pragma warning disable CA1801 // Review unused parameters
        internal MaterialSpecular(Material material) { }
        #pragma warning restore CA1801 // Review unused parameters

        protected override IEnumerable<ExtraProperties> GetLogicalChildren()
        {
            return base.GetLogicalChildren().ConcatElements(_specularColorTexture, _specularTexture);
        }

        protected override void OnValidateContent(ValidationContext validate)
        {
            base.OnValidateContent(validate);

            if (_specularColorFactor.HasValue)
            {
                Guard.MustBeBetweenOrEqualTo(_specularColorFactor.Value.X, 0, float.MaxValue, nameof(_specularColorFactor));
                Guard.MustBeBetweenOrEqualTo(_specularColorFactor.Value.Y, 0, float.MaxValue, nameof(_specularColorFactor));
                Guard.MustBeBetweenOrEqualTo(_specularColorFactor.Value.Z, 0, float.MaxValue, nameof(_specularColorFactor));
            }

            if (_specularFactor.HasValue)
            {
                Guard.MustBeBetweenOrEqualTo(_specularFactor.Value, _specularFactorMinimum, _specularFactorMaximum, nameof(_specularFactor));
            }
        }

        private TextureInfo _GetSpecularColorTexture(bool create)
        {
            if (create && _specularColorTexture == null) _specularColorTexture = new TextureInfo();
            return _specularColorTexture;
        }

        private TextureInfo _GetSpecularFactorTexture(bool create)
        {
            if (create && _specularTexture == null) _specularTexture = new TextureInfo();
            return _specularTexture;
        }

        public Vector3 SpecularColor
        {
            get => _specularColorFactor.AsValue(_specularColorFactorDefault);
            set => _specularColorFactor = value.AsNullable(_specularColorFactorDefault);
        }

        public float SpecularFactor
        {
            get => (float)_specularFactor.AsValue(_specularFactorDefault);
            set => _specularFactor = ((double)value).AsNullable(_specularFactorDefault, _specularFactorMinimum, _specularFactorMaximum);
        }

        public IEnumerable<MaterialChannel> GetChannels(Material material)
        {
            var colorParam = new _MaterialParameter<Vector3>(_MaterialParameterKey.RGB, _specularColorFactorDefault, () => SpecularColor, v => SpecularColor = v);
            var factorParam = new _MaterialParameter<float>(_MaterialParameterKey.SpecularFactor, (float)_specularFactorDefault, () => SpecularFactor, v => SpecularFactor = v);

            yield return new MaterialChannel(material, "SpecularColor", _GetSpecularColorTexture, colorParam);
            yield return new MaterialChannel(material, "SpecularFactor", _GetSpecularFactorTexture, factorParam);
        }
    }

    internal sealed partial class MaterialVolume
    {
        #pragma warning disable CA1801 // Review unused parameters
        internal MaterialVolume(Material material) { }
        #pragma warning restore CA1801 // Review unused parameters

        protected override IEnumerable<ExtraProperties> GetLogicalChildren()
        {
            return base.GetLogicalChildren().ConcatElements(_thicknessTexture);
        }

        protected override void OnValidateContent(ValidationContext validate)
        {
            base.OnValidateContent(validate);

            if (_attenuationColor.HasValue)
            {
                Guard.MustBeBetweenOrEqualTo(_attenuationColor.Value.X, 0, float.MaxValue, nameof(_attenuationColor));
                Guard.MustBeBetweenOrEqualTo(_attenuationColor.Value.Y, 0, float.MaxValue, nameof(_attenuationColor));
                Guard.MustBeBetweenOrEqualTo(_attenuationColor.Value.Z, 0, float.MaxValue, nameof(_attenuationColor));
            }

            if (_thicknessFactor.HasValue)
            {
                Guard.MustBeBetweenOrEqualTo(_thicknessFactor.Value, _thicknessFactorMinimum, float.MaxValue, nameof(_thicknessFactor));
            }
        }

        private TextureInfo _GetThicknessTexture(bool create)
        {
            if (create && _thicknessTexture == null) _thicknessTexture = new TextureInfo();
            return _thicknessTexture;
        }

        public float ThicknessFactor
        {
            get => (float)_thicknessFactor.AsValue(_thicknessFactorDefault);
            set => _thicknessFactor = ((double)value).AsNullable(_thicknessFactorDefault);
        }

        public Vector3 AttenuationColor
        {
            get => _attenuationColor.AsValue(_attenuationColorDefault);
            set => _attenuationColor = value.AsNullable(_attenuationColorDefault);
        }

        public float AttenuationDistance
        {
            get => (float)_attenuationDistance.AsValue(0);
            set => _attenuationDistance = value > _attenuationDistanceExclusiveMinimum ? value : throw new ArgumentOutOfRangeException(nameof(value));
        }

        public IEnumerable<MaterialChannel> GetChannels(Material material)
        {
            var thicknessParam = new _MaterialParameter<float>(_MaterialParameterKey.ThicknessFactor, (float)_thicknessFactorDefault, () => ThicknessFactor, v => ThicknessFactor = v);
            var attColorParam = new _MaterialParameter<Vector3>(_MaterialParameterKey.RGB, _attenuationColorDefault, () => AttenuationColor, v => AttenuationColor = v);
            var attDistParam = new _MaterialParameter<float>(_MaterialParameterKey.AttenuationDistance, 0, () => AttenuationDistance, v => AttenuationDistance = v);

            yield return new MaterialChannel(material, "VolumeThickness", _GetThicknessTexture, thicknessParam);
            yield return new MaterialChannel(material, "VolumeAttenuation", onCreate => null, attColorParam, attDistParam);
        }
    }

    internal sealed partial class MaterialEmissiveStrength
    {
        #pragma warning disable CA1801 // Review unused parameters
        internal MaterialEmissiveStrength(Material material) { }
        #pragma warning restore CA1801 // Review unused parameters

        protected override void OnValidateContent(ValidationContext validate)
        {
            base.OnValidateContent(validate);

            if (_emissiveStrength < _emissiveStrengthMinimum) throw new ArgumentOutOfRangeException(nameof(EmissiveStrength)); 
        }

        public const float DefaultEmissiveStrength = (float)_emissiveStrengthDefault;

        public float EmissiveStrength
        {
            get => (float)(this._emissiveStrength ?? _emissiveStrengthDefault);
            set => this._emissiveStrength = ((double)value).AsNullable(_emissiveStrengthDefault);
        }

        public static _MaterialParameter<float> GetParameter(Material material)
        {
            float _getter() { return material.GetExtension<MaterialEmissiveStrength>()?.EmissiveStrength ?? 1; }

            void _setter(float value)
            {
                value = Math.Max((float)_emissiveStrengthMinimum, value);

                if (value == DefaultEmissiveStrength) { material.RemoveExtensions<MaterialEmissiveStrength>(); }
                else { material.UseExtension<MaterialEmissiveStrength>().EmissiveStrength = value; }
            }

            return new _MaterialParameter<float>(_MaterialParameterKey.EmissiveStrength, DefaultEmissiveStrength, _getter, _setter);
        }
    }


    internal sealed partial class MaterialIridescence
    {
        #pragma warning disable CA1801 // Review unused parameters
        internal MaterialIridescence(Material material) { }
        #pragma warning restore CA1801 // Review unused parameters

        protected override IEnumerable<ExtraProperties> GetLogicalChildren()
        {
            return base.GetLogicalChildren().ConcatElements(_iridescenceTexture, _iridescenceThicknessTexture);
        }

        protected override void OnValidateContent(ValidationContext validate)
        {
            base.OnValidateContent(validate);            

            if (_iridescenceFactor.HasValue)
            {
                Guard.MustBeBetweenOrEqualTo(_iridescenceFactor.Value, _iridescenceFactorMinimum, _iridescenceFactorMaximum, nameof(_iridescenceFactor));
            }

            if (_iridescenceIor.HasValue)
            {
                Guard.MustBeBetweenOrEqualTo(_iridescenceIor.Value, _iridescenceIorMinimum, double.MaxValue, nameof(_iridescenceIor));
            }

            var thickMin = _iridescenceThicknessMinimum ?? _iridescenceThicknessMinimumDefault;
            var thickMax = _iridescenceThicknessMaximum ?? _iridescenceThicknessMaximumDefault;

            Guard.MustBeBetweenOrEqualTo(thickMin, _iridescenceThicknessMinimumMinimum, thickMax, nameof(_iridescenceThicknessMinimum));
            Guard.MustBeBetweenOrEqualTo(thickMax, thickMin, double.MaxValue, nameof(_iridescenceThicknessMaximum));

            
        }

        private TextureInfo _GetIridescenceTexture(bool create)
        {
            if (create && _iridescenceTexture == null) _iridescenceTexture = new TextureInfo();
            return _iridescenceTexture;
        }

        private TextureInfo _GetIridescenceThicknessTexture(bool create)
        {
            if (create && _iridescenceThicknessTexture == null) _iridescenceThicknessTexture = new TextureInfo();
            return _iridescenceThicknessTexture;
        }

        public float IridescenceFactor
        {
            get => (float)_iridescenceFactor.AsValue(_iridescenceFactorDefault);
            set => _iridescenceFactor = ((double)value).AsNullable(_iridescenceFactorDefault, _iridescenceFactorMinimum, _iridescenceFactorMaximum);
        }

        public float IridescenceIndexOfRefraction
        {
            get => (float)_iridescenceIor.AsValue(_iridescenceIorDefault);
            set => _iridescenceIor = ((double)value).AsNullable(_iridescenceIorDefault, _iridescenceIorMinimum, double.MaxValue);
        }

        public float IridescenceThicknessMinimum
        {
            get => (float)_iridescenceThicknessMinimum.AsValue(_iridescenceThicknessMinimumDefault);
            set => _iridescenceThicknessMinimum = ((double)value).AsNullable(_iridescenceThicknessMinimumDefault, _iridescenceThicknessMinimumMinimum, double.MaxValue);
        }

        public float IridescenceThicknessMaximum
        {
            get => (float)_iridescenceThicknessMaximum.AsValue(_iridescenceThicknessMaximumDefault);
            set => _iridescenceThicknessMaximum = ((double)value).AsNullable(_iridescenceThicknessMaximumDefault, _iridescenceThicknessMaximumMinimum, double.MaxValue);
        }

        public IEnumerable<MaterialChannel> GetChannels(Material material)
        {
            var factor = new _MaterialParameter<float>(_MaterialParameterKey.IridescenceFactor, (float)_iridescenceFactorDefault, () => IridescenceFactor, v => IridescenceFactor = v);
            var idxRef = new _MaterialParameter<float>(_MaterialParameterKey.IndexOfRefraction, (float)_iridescenceIorDefault, () => IridescenceIndexOfRefraction, v => IridescenceIndexOfRefraction = v);

            yield return new MaterialChannel(material, "Iridescence", _GetIridescenceTexture, factor, idxRef);

            var thkMin = new _MaterialParameter<float>(_MaterialParameterKey.Minimum, (float)_iridescenceThicknessMinimumDefault, () => IridescenceThicknessMinimum, v => IridescenceThicknessMinimum = v);
            var thkMax = new _MaterialParameter<float>(_MaterialParameterKey.Maximum, (float)_iridescenceThicknessMaximumDefault, () => IridescenceThicknessMaximum, v => IridescenceThicknessMaximum = v);

            yield return new MaterialChannel(material, "IridescenceThickness", _GetIridescenceThicknessTexture, thkMin, thkMax);
        }
    }
}
