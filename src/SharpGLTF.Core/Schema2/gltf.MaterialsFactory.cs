using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

using SharpGLTF.Collections;
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
            this.RemoveExtensions<MaterialDiffuseTransmission>();
            this.RemoveExtensions<MaterialVolume>();
            this.RemoveExtensions<MaterialAnisotropy>();
            this.RemoveExtensions<MaterialIridescence>();

            this.RemoveExtensions<MaterialPBRSpecularGlossiness>();            
        }

        /// <summary>
        /// Initializes this <see cref="Material"/> instance with Unlit attributes.
        /// </summary>
        public void InitializeUnlit()
        {
            SetProperty(this, ref _pbrMetallicRoughness, new MaterialPBRMetallicRoughness());

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

            ClearExtensions();

            SetProperty(this, ref _pbrMetallicRoughness, new MaterialPBRMetallicRoughness());            

            foreach (var extn in extensionNames)
            {
                if (extn == "Sheen") this.UseExtension<MaterialSheen>();
                if (extn == "Volume") this.UseExtension<MaterialVolume>();
                if (extn == "Specular") this.UseExtension<MaterialSpecular>();
                if (extn == "ClearCoat") this.UseExtension<MaterialClearCoat>();                
                if (extn == "Anisotropy") this.UseExtension<MaterialAnisotropy>();
                if (extn == "Iridescence") this.UseExtension<MaterialIridescence>();
                if (extn == "Transmission") this.UseExtension<MaterialTransmission>();
                if (extn == "DiffuseTransmission") this.UseExtension<MaterialDiffuseTransmission>();
            }
        }

        /// <summary>
        /// Initializes this <see cref="Material"/> instance with PBR Specular Glossiness attributes.
        /// </summary>
        /// <param name="useFallback">true to add a PBRMetallicRoughness fallback material.</param>
        public void InitializePBRSpecularGlossiness(bool useFallback = false)
        {
            ClearExtensions();

            if (useFallback)
            {
                var value = this._pbrMetallicRoughness ??= new MaterialPBRMetallicRoughness();
                SetProperty(this, ref _pbrMetallicRoughness, value);                
            }
            else
            {
                SetProperty(this, ref _pbrMetallicRoughness, (MaterialPBRMetallicRoughness)null);
            }
            
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

            channels = this.GetExtension<MaterialDiffuseTransmission>()?.GetChannels(this);
            if (channels != null) { foreach (var c in channels) yield return c; }

            channels = this.GetExtension<MaterialSheen>()?.GetChannels(this);
            if (channels != null) { foreach (var c in channels) yield return c; }

            channels = this.GetExtension<MaterialSpecular>()?.GetChannels(this);
            if (channels != null) { foreach (var c in channels) yield return c; }

            channels = this.GetExtension<MaterialVolume>()?.GetChannels(this);
            if (channels != null) { foreach (var c in channels) yield return c; }

            channels = this.GetExtension<MaterialIridescence>()?.GetChannels(this);
            if (channels != null) { foreach (var c in channels) yield return c; }

            channels = this.GetExtension<MaterialAnisotropy>()?.GetChannels(this);
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

            yield return new MaterialChannel(this, "Normal", new _MaterialTexture(_GetNormalTexture), normalParam);
            yield return new MaterialChannel(this, "Occlusion", new _MaterialTexture(_GetOcclusionTexture), occlusionParam);
            yield return new MaterialChannel(this, "Emissive", new _MaterialTexture(_GetEmissiveTexture), emissiveFactorParam, MaterialEmissiveStrength.GetParameter(this));
        }

        private MaterialNormalTextureInfo _GetNormalTexture(bool create)
        {
            if (create && _normalTexture == null) SetProperty(this, ref _normalTexture, new MaterialNormalTextureInfo());
            return _normalTexture;
        }

        private MaterialOcclusionTextureInfo _GetOcclusionTexture(bool create)
        {
            if (create && _occlusionTexture == null) SetProperty(this, ref _occlusionTexture , new MaterialOcclusionTextureInfo());
            return _occlusionTexture;
        }

        private TextureInfo _GetEmissiveTexture(bool create)
        {
            if (create && _emissiveTexture == null) SetProperty(this, ref _emissiveTexture, new TextureInfo());
            return _emissiveTexture;
        }

        #endregion
    }

    internal sealed partial class MaterialPBRMetallicRoughness : IChildOf<Material>
    {
        #region hierarchy

        Material IChildOf<Material>.LogicalParent => _Parent;

        void IChildOf<Material>.SetLogicalParent(Material parent)
        {
            _Parent = parent;
        }

        private Material _Parent;

        #endregion

        #region properties

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

        #endregion

        #region API
        
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

        public IEnumerable<MaterialChannel> GetChannels(Material material)
        {
            var baseTexture = new _MaterialTexture(() => _baseColorTexture, () => SetProperty(this, ref _baseColorTexture, new TextureInfo()));
            var colorParam = new _MaterialParameter<Vector4>(_MaterialParameterKey.RGBA, _baseColorFactorDefault, () => Color, v => Color = v);
            yield return new MaterialChannel(material, "BaseColor", baseTexture, colorParam);

            var metallicRoighnessTexture = new _MaterialTexture(() => _metallicRoughnessTexture, () => SetProperty(this, ref _metallicRoughnessTexture, new TextureInfo()));
            var metallicParam = new _MaterialParameter<float>(_MaterialParameterKey.MetallicFactor, (float)_metallicFactorDefault, () => MetallicFactor, v => MetallicFactor = v);
            var roughnessParam = new _MaterialParameter<float>(_MaterialParameterKey.RoughnessFactor, (float)_roughnessFactorDefault, () => RoughnessFactor, v => RoughnessFactor = v);
            yield return new MaterialChannel(material, "MetallicRoughness", metallicRoighnessTexture, metallicParam, roughnessParam);
        }        

        #endregion
    }

    /// <summary>
    /// <see cref="MaterialPBRSpecularGlossiness"/> has been deprecated by khronos in favour of <see cref="MaterialPBRMetallicRoughness"/> + <see cref="MaterialSpecular"/>
    /// </summary>
    internal sealed partial class MaterialPBRSpecularGlossiness
    {
        #pragma warning disable CA1801 // Review unused parameters
        internal MaterialPBRSpecularGlossiness(Material material) { }
        #pragma warning restore CA1801 // Review unused parameters
        
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
            var diffuseTexture = new _MaterialTexture(() => _diffuseTexture, () => SetProperty(this, ref _diffuseTexture, new TextureInfo()));
            var diffuseParam = new _MaterialParameter<Vector4>(_MaterialParameterKey.RGBA, _diffuseFactorDefault, () => DiffuseFactor, v => DiffuseFactor = v);
            yield return new MaterialChannel(material, "Diffuse", diffuseTexture, diffuseParam);

            var glossinessTexture = new _MaterialTexture(() => _specularGlossinessTexture, () => SetProperty(this, ref _specularGlossinessTexture, new TextureInfo()));            
            var specularParam = new _MaterialParameter<Vector3>(_MaterialParameterKey.SpecularFactor, _specularFactorDefault, () => SpecularFactor, v => SpecularFactor = v);
            var glossinessParam = new _MaterialParameter<float>(_MaterialParameterKey.GlossinessFactor, (float)_glossinessFactorDefault, () => GlossinessFactor, v => GlossinessFactor = v);            
            yield return new MaterialChannel(material, "SpecularGlossiness", glossinessTexture, specularParam, glossinessParam);
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
            if (create && _clearcoatTexture == null) SetProperty(this, ref _clearcoatTexture, new TextureInfo());
            return _clearcoatTexture;
        }

        private TextureInfo _GetClearCoatRoughnessTexture(bool create)
        {
            if (create && _clearcoatRoughnessTexture == null) SetProperty(this, ref _clearcoatRoughnessTexture, new TextureInfo());
            return _clearcoatRoughnessTexture;
        }

        private MaterialNormalTextureInfo _GetClearCoatNormalTexture(bool create)
        {
            if (create && _clearcoatNormalTexture == null) SetProperty(this, ref _clearcoatNormalTexture, new MaterialNormalTextureInfo());
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
            var clearCoatTexture = new _MaterialTexture(() => _clearcoatTexture, () => SetProperty(this, ref _clearcoatTexture, new TextureInfo()));
            var clearCoatParam = new _MaterialParameter<float>(_MaterialParameterKey.ClearCoatFactor, (float)_clearcoatFactorDefault, () => ClearCoatFactor, v => ClearCoatFactor = v);
            yield return new MaterialChannel(material, "ClearCoat", clearCoatTexture, clearCoatParam);

            var roughnessTexture = new _MaterialTexture(() => _clearcoatRoughnessTexture, () => SetProperty(this, ref _clearcoatRoughnessTexture, new TextureInfo()));
            var roughnessParam = new _MaterialParameter<float>(_MaterialParameterKey.RoughnessFactor, (float)_clearcoatRoughnessFactorDefault, () => RoughnessFactor, v => RoughnessFactor = v);
            yield return new MaterialChannel(material, "ClearCoatRoughness", roughnessTexture, roughnessParam);

            var normScaleParam = new _MaterialParameter<float>(_MaterialParameterKey.NormalScale,
                MaterialNormalTextureInfo.ScaleDefault,
                () => _GetClearCoatNormalTexture(false)?.Scale ?? MaterialNormalTextureInfo.ScaleDefault,
                v => _GetClearCoatNormalTexture(true).Scale = v);            
            yield return new MaterialChannel(material, "ClearCoatNormal", new _MaterialTexture(_GetClearCoatNormalTexture), normScaleParam);
        }
    }

    internal sealed partial class MaterialTransmission
    {
        #pragma warning disable CA1801 // Review unused parameters
        internal MaterialTransmission(Material material) { }
        #pragma warning restore CA1801 // Review unused parameters
        
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
            var transmissionTexture = new _MaterialTexture(() => _transmissionTexture, () => SetProperty(this, ref _transmissionTexture, new TextureInfo()));
            var transmissionParam = new _MaterialParameter<float>(_MaterialParameterKey.TransmissionFactor, (float)_transmissionFactorDefault, () => TransmissionFactor, v => TransmissionFactor = v);

            yield return new MaterialChannel(material, "Transmission", transmissionTexture, transmissionParam);
        }
    }
    
    internal sealed partial class MaterialSheen
    {
        #pragma warning disable CA1801 // Review unused parameters
        internal MaterialSheen(Material material) { }
        #pragma warning restore CA1801 // Review unused parameters
        
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
            var colorTexture = new _MaterialTexture(() => _sheenColorTexture, () => SetProperty(this, ref _sheenColorTexture, new TextureInfo()));
            var colorParam = new _MaterialParameter<Vector3>(_MaterialParameterKey.RGB, _sheenColorFactorDefault, () => ColorFactor, v => ColorFactor = v);
            yield return new MaterialChannel(material, "SheenColor", colorTexture, colorParam);

            var roughnessTexture = new _MaterialTexture(() => _sheenRoughnessTexture, () => SetProperty(this, ref _sheenRoughnessTexture, new TextureInfo()));
            var roughnessParam = new _MaterialParameter<float>(_MaterialParameterKey.RoughnessFactor, _sheenRoughnessFactorDefault, () => RoughnessFactor, v => RoughnessFactor = v);            
            yield return new MaterialChannel(material, "SheenRoughness", roughnessTexture, roughnessParam);
        }
    }

    /// <remarks>
    /// Mapped straight away to <see cref="Material.IndexOfRefraction"/>
    /// </remarks>    
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

    /// <remarks>
    /// Mapped straight away to <see cref="Material.Dispersion"/>
    /// </remarks>    
    internal sealed partial class MaterialDispersion
    {
        #pragma warning disable CA1801 // Review unused parameters
        internal MaterialDispersion(Material material) { }
        #pragma warning restore CA1801 // Review unused parameters

        protected override void OnValidateContent(ValidationContext validate)
        {
            base.OnValidateContent(validate);
            
            if (_dispersion < _dispersionMinimum) throw new ArgumentOutOfRangeException(nameof(Dispersion));
        }

        public static float DefaultDispersion => (float)_dispersionDefault;

        public float Dispersion
        {
            get => (float)(this._dispersion ?? _dispersionDefault);
            set => this._dispersion = Math.Max(_dispersionMinimum, value).AsNullable(_dispersionDefault);
        }
    }

    internal sealed partial class MaterialSpecular
    {
        #pragma warning disable CA1801 // Review unused parameters
        internal MaterialSpecular(Material material) { }
        #pragma warning restore CA1801 // Review unused parameters
        
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
            var colorTexture = new _MaterialTexture(() => _specularColorTexture, () => SetProperty(this, ref _specularColorTexture, new TextureInfo()));
            var colorParam = new _MaterialParameter<Vector3>(_MaterialParameterKey.RGB, _specularColorFactorDefault, () => SpecularColor, v => SpecularColor = v);
            yield return new MaterialChannel(material, "SpecularColor", colorTexture, colorParam);

            var factorTexture = new _MaterialTexture(() => _specularTexture, () => SetProperty(this, ref _specularTexture, new TextureInfo()));
            var factorParam = new _MaterialParameter<float>(_MaterialParameterKey.SpecularFactor, (float)_specularFactorDefault, () => SpecularFactor, v => SpecularFactor = v);            
            yield return new MaterialChannel(material, "SpecularFactor", factorTexture, factorParam);
        }
    }

    internal sealed partial class MaterialVolume
    {
        #pragma warning disable CA1801 // Review unused parameters
        internal MaterialVolume(Material material) { }
        #pragma warning restore CA1801 // Review unused parameters
        
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

        public float ThicknessFactor
        {
            get => (float)_thicknessFactor.AsValue(_thicknessFactorDefault);
            set => _thicknessFactor = ((double)value).AsNullable(_thicknessFactorDefault, _thicknessFactorMinimum, double.MaxValue);
        }

        public Vector3 AttenuationColor
        {
            get => _attenuationColor.AsValue(_attenuationColorDefault);
            set => _attenuationColor = value.AsNullable(_attenuationColorDefault);
        }

        public float AttenuationDistance
        {
            get => (float)_attenuationDistance.AsValue(float.MaxValue);
            set => _attenuationDistance = ((double)value).AsNullable(double.MaxValue, _attenuationDistanceExclusiveMinimum, double.MaxValue);
        }

        public IEnumerable<MaterialChannel> GetChannels(Material material)
        {
            var thicknessTexture = new _MaterialTexture(() => _thicknessTexture, () => SetProperty(this, ref _thicknessTexture, new TextureInfo()));
            var thicknessParam = new _MaterialParameter<float>(_MaterialParameterKey.ThicknessFactor, (float)_thicknessFactorDefault, () => ThicknessFactor, v => ThicknessFactor = v);
            yield return new MaterialChannel(material, "VolumeThickness", thicknessTexture, thicknessParam);

            var attColorParam = new _MaterialParameter<Vector3>(_MaterialParameterKey.RGB, _attenuationColorDefault, () => AttenuationColor, v => AttenuationColor = v);
            var attDistParam = new _MaterialParameter<float>(_MaterialParameterKey.AttenuationDistance, 0, () => AttenuationDistance, v => AttenuationDistance = v);            
            yield return new MaterialChannel(material, "VolumeAttenuation", default, attColorParam, attDistParam);
        }
    }

    /// <remarks>
    /// Mapped to the Emissive channel as an additional parameter
    /// </remarks>    
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
            set => this._emissiveStrength = ((double)value).AsNullable(_emissiveStrengthDefault, _emissiveStrengthMinimum, double.MaxValue);
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
            var iridesTexture = new _MaterialTexture(() => _iridescenceTexture, () => SetProperty(this, ref _iridescenceTexture, new TextureInfo()));
            var factor = new _MaterialParameter<float>(_MaterialParameterKey.IridescenceFactor, (float)_iridescenceFactorDefault, () => IridescenceFactor, v => IridescenceFactor = v);
            var idxRef = new _MaterialParameter<float>(_MaterialParameterKey.IndexOfRefraction, (float)_iridescenceIorDefault, () => IridescenceIndexOfRefraction, v => IridescenceIndexOfRefraction = v);
            yield return new MaterialChannel(material, "Iridescence", iridesTexture, factor, idxRef);

            var thicknessTexture = new _MaterialTexture(() => _iridescenceThicknessTexture, () => SetProperty(this, ref _iridescenceThicknessTexture, new TextureInfo()));
            var thkMin = new _MaterialParameter<float>(_MaterialParameterKey.Minimum, (float)_iridescenceThicknessMinimumDefault, () => IridescenceThicknessMinimum, v => IridescenceThicknessMinimum = v);
            var thkMax = new _MaterialParameter<float>(_MaterialParameterKey.Maximum, (float)_iridescenceThicknessMaximumDefault, () => IridescenceThicknessMaximum, v => IridescenceThicknessMaximum = v);
            yield return new MaterialChannel(material, "IridescenceThickness", thicknessTexture, thkMin, thkMax);
        }
    }

    internal sealed partial class MaterialAnisotropy
    {
        #pragma warning disable CA1801 // Review unused parameters
        internal MaterialAnisotropy(Material material) { }
        #pragma warning restore CA1801 // Review unused parameters
        
        protected override void OnValidateContent(ValidationContext validate)
        {
            base.OnValidateContent(validate);

            if (_anisotropyStrength.HasValue)
            {
                Guard.MustBeBetweenOrEqualTo(_anisotropyStrength.Value, _anisotropyStrengthMinimum, _anisotropyStrengthMaximum, nameof(_anisotropyStrength));
            }
        }

        public float AnisotropyStrength
        {
            get => (float)this._anisotropyStrength.AsValue(_anisotropyStrengthDefault);
            set => this._anisotropyStrength = ((double)value).AsNullable((float)_anisotropyStrengthDefault, _anisotropyStrengthMinimum, _anisotropyStrengthMaximum);
        }

        public float AnisotropyRotation
        {
            get => (float)this._anisotropyRotation.AsValue(_anisotropyRotationDefault);
            set => this._anisotropyRotation = value.AsNullable((float)_anisotropyRotationDefault);
        }

        public IEnumerable<MaterialChannel> GetChannels(Material material)
        {
            var anisoTexture = new _MaterialTexture(() => _anisotropyTexture, () => SetProperty(this, ref _anisotropyTexture, new TextureInfo()));
            var strength = new _MaterialParameter<float>(_MaterialParameterKey.AnisotropyStrength, (float)_anisotropyStrengthDefault, () => AnisotropyStrength, v => AnisotropyStrength = v);
            var rotation = new _MaterialParameter<float>(_MaterialParameterKey.AnisotropyRotation, (float)_anisotropyRotationDefault, () => AnisotropyRotation, v => AnisotropyRotation = v);
            yield return new MaterialChannel(material, "Anisotropy", anisoTexture, strength, rotation);            
        }
    }

    internal sealed partial class MaterialDiffuseTransmission
    {
        #pragma warning disable CA1801 // Review unused parameters
        internal MaterialDiffuseTransmission(Material material) { }
        #pragma warning restore CA1801 // Review unused parameters
        
        protected override void OnValidateContent(ValidationContext validate)
        {
            base.OnValidateContent(validate);

            if (_diffuseTransmissionFactor.HasValue)
            {
                Guard.MustBeBetweenOrEqualTo(_diffuseTransmissionFactor.Value, _diffuseTransmissionFactorMinimum, _diffuseTransmissionFactorMaximum, nameof(_diffuseTransmissionFactor));
            }
        }

        public float DiffuseTransmissionFactor
        {
            get => (float)this._diffuseTransmissionFactor.AsValue(_diffuseTransmissionFactorDefault);
            set => this._diffuseTransmissionFactor = ((double)value).AsNullable(_diffuseTransmissionFactorDefault, _diffuseTransmissionFactorMinimum, _diffuseTransmissionFactorMaximum);
        }

        public Vector3 DiffuseTransmissionColorFactor
        {
            get => this._diffuseTransmissionColorFactor.AsValue(_diffuseTransmissionColorFactorDefault);
            set => this._diffuseTransmissionColorFactor = value.AsNullable(_diffuseTransmissionColorFactorDefault);
        }        

        public IEnumerable<MaterialChannel> GetChannels(Material material)
        {
            var factorTexture = new _MaterialTexture(() => _diffuseTransmissionTexture, () => SetProperty(this, ref _diffuseTransmissionTexture, new TextureInfo()));
            var factorParameter = new _MaterialParameter<float>(_MaterialParameterKey.DiffuseTransmissionFactor, (float)_diffuseTransmissionFactorDefault, () => DiffuseTransmissionFactor, v => DiffuseTransmissionFactor = v);            
            yield return new MaterialChannel(material, "DiffuseTransmissionFactor", factorTexture, factorParameter);

            var colorTexture = new _MaterialTexture(() => _diffuseTransmissionColorTexture, () => SetProperty(this, ref _diffuseTransmissionColorTexture, new TextureInfo()));
            var colorParameter = new _MaterialParameter<Vector3>(_MaterialParameterKey.RGB, _diffuseTransmissionColorFactorDefault, () => DiffuseTransmissionColorFactor, v => DiffuseTransmissionColorFactor = v);            
            yield return new MaterialChannel(material, "DiffuseTransmissionColor", colorTexture, colorParameter);
        }
    }
}
