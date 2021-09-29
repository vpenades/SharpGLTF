using System;
using System.Collections.Generic;
using System.Linq;
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
            if (this._pbrMetallicRoughness == null) this._pbrMetallicRoughness = new MaterialPBRMetallicRoughness();

            ClearExtensions();

            foreach (var extn in extensionNames)
            {
                if (extn == "Sheen") this.UseExtension<MaterialSheen>();
                if (extn == "Specular") this.UseExtension<MaterialSpecular>();
                if (extn == "ClearCoat") this.UseExtension<MaterialClearCoat>();
                if (extn == "Transmission") this.UseExtension<MaterialTransmission>();
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

            var transmission = this.GetExtension<MaterialTransmission>();
            if (transmission != null)
            {
                var channels = transmission.GetChannels(this);
                foreach (var c in channels) yield return c;
            }

            var sheen = this.GetExtension<MaterialSheen>();
            if (sheen != null)
            {
                var channels = sheen.GetChannels(this);
                foreach (var c in channels) yield return c;
            }

            var specular = this.GetExtension<MaterialSpecular>();
            if (specular != null)
            {
                var channels = specular.GetChannels(this);
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
            return base.GetLogicalChildren().ConcatElements(_baseColorTexture, _metallicRoughnessTexture);
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
    }

    internal sealed partial class MaterialPBRSpecularGlossiness
    {
        #pragma warning disable CA1801 // Review unused parameters
        internal MaterialPBRSpecularGlossiness(Material material) { }
        #pragma warning restore CA1801 // Review unused parameters

        protected override IEnumerable<ExtraProperties> GetLogicalChildren()
        {
            return base.GetLogicalChildren().ConcatElements(_diffuseTexture, _specularGlossinessTexture);
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
                () => _diffuseFactor.AsValue(_diffuseFactorDefault),
                value => _diffuseFactor = value.AsNullable(_diffuseFactorDefault)
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
                value => this._clearcoatFactor = value.AsNullable((float)_clearcoatFactorDefault)
                );

            yield return new MaterialChannel
                (
                material, "ClearCoatRoughness",
                _GetClearCoatRoughnessTexture,
                (float)_clearcoatRoughnessFactorDefault,
                () => (float)this._clearcoatRoughnessFactor.AsValue(_clearcoatRoughnessFactorDefault),
                value => this._clearcoatRoughnessFactor = value.AsNullable((float)_clearcoatRoughnessFactorDefault)
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

        public IEnumerable<MaterialChannel> GetChannels(Material material)
        {
            yield return new MaterialChannel
                (
                material, "Transmission",
                _GetTransmissionTexture,
                (float)_transmissionFactorDefault,
                () => (float)this._transmissionFactor.AsValue(_transmissionFactorDefault),
                value => this._transmissionFactor = value.AsNullable((float)_transmissionFactorDefault)
                );
        }

        private TextureInfo _GetTransmissionTexture(bool create)
        {
            if (create && _transmissionTexture == null) _transmissionTexture = new TextureInfo();
            return _transmissionTexture;
        }

        protected override void OnValidateContent(ValidationContext validate)
        {
            base.OnValidateContent(validate);

            if (_transmissionFactor.HasValue)
            {
                Guard.MustBeBetweenOrEqualTo(_transmissionFactor.Value, _transmissionFactorMinimum, _transmissionFactorMaximum, nameof(_transmissionFactor));
            }
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

        public IEnumerable<MaterialChannel> GetChannels(Material material)
        {
            yield return new MaterialChannel
                (
                material, "SheenColor",
                _GetSheenColorTexture,
                _sheenColorFactorDefault,
                () => _sheenColorFactor.AsValue(_sheenColorFactorDefault),
                value => this._sheenColorFactor = value.AsNullable(_sheenColorFactorDefault)
                );

            yield return new MaterialChannel
                (
                material, "SheenRoughness",
                _GetSheenRoughnessTexture,
                _sheenRoughnessFactorDefault,
                () => _sheenRoughnessFactor.AsValue(_sheenRoughnessFactorDefault),
                value => this._sheenRoughnessFactor = value.AsNullable(_sheenRoughnessFactorDefault)
                );
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
    }

    internal sealed partial class MaterialIOR
    {
        #pragma warning disable CA1801 // Review unused parameters
        internal MaterialIOR(Material material) { }
        #pragma warning restore CA1801 // Review unused parameters

        public static float DefaultIndexOfRefraction => (float)_iorDefault;

        public float IndexOfRefraction
        {
            get => (float)(this._ior ?? _iorDefault);
            set => this._ior = ((double)value).AsNullable(_iorDefault);
        }

        protected override void OnValidateContent(ValidationContext validate)
        {
            base.OnValidateContent(validate);

            if (_ior == 0) return; // a value of 0 is allowed by the spec as a special value
            if (_ior < 1) throw new ArgumentOutOfRangeException(nameof(IndexOfRefraction));
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

        public static float SpecularFactorDefault => (float)_specularFactorDefault;

        public float SpecularFactor
        {
            get => (float)_specularFactor.AsValue(_specularFactorDefault);
            set => _specularFactor = ((double)value).AsNullable(_specularFactorDefault, _specularFactorMinimum, _specularFactorMaximum);
        }

        public IEnumerable<MaterialChannel> GetChannels(Material material)
        {
            yield return new MaterialChannel
                (
                material,
                "SpecularColor",
                _GetSpecularColorTexture,
                _specularColorFactorDefault,
                () => this.SpecularColor,
                value => this.SpecularColor = value
                );

            yield return new MaterialChannel
                (
                material,
                "SpecularFactor",
                _GetSpecularFactorTexture,
                SpecularFactorDefault,
                () => this.SpecularFactor,
                value => this.SpecularFactor = value
                );
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
    }
}
