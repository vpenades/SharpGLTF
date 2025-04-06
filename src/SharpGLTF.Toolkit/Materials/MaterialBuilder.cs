using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace SharpGLTF.Materials
{
    /// <summary>
    /// Represents the root object of a material instance structure.
    /// </summary>
    [System.Diagnostics.DebuggerDisplay("{_DebuggerDisplay(),nq}")]
    public partial class MaterialBuilder : BaseBuilder, ICloneable
    {
        #region debug

        internal string _DebuggerDisplay()
        {
            var txt = "MatBuilder ";
            if (!string.IsNullOrWhiteSpace(Name)) txt += $" \"{Name}\"";

            txt += $" {_ShaderStyle}";

            if (AlphaMode == AlphaMode.BLEND) txt += " AlphaBlend";
            if (AlphaMode == AlphaMode.MASK) txt += $" AlphaMask({AlphaCutoff})";
            if (DoubleSided) txt += " DoubleSided";

            return txt;
        }

        #endregion

        #region constants

        public const string SHADERUNLIT = "Unlit";
        public const string SHADERPBRMETALLICROUGHNESS = "PBRMetallicRoughness";
        public const string SHADERPBRSPECULARGLOSSINESS = "PBRSpecularGlossiness";

        #endregion

        #region lifecycle

        public static MaterialBuilder CreateDefault()
        {
            return new MaterialBuilder("Default");
        }

        public MaterialBuilder(string name = null)
            : base(name) { }

        Object ICloneable.Clone() { return new MaterialBuilder(this); }

        public MaterialBuilder Clone() { return new MaterialBuilder(this); }

        public MaterialBuilder(MaterialBuilder other)
            : base(other)
        {
            Guard.NotNull(other, nameof(other));

            this.AlphaMode = other.AlphaMode;
            this.AlphaCutoff = other.AlphaCutoff;
            this.DoubleSided = other.DoubleSided;
            this.ShaderStyle = other.ShaderStyle;
            this.Dispersion = other.Dispersion;
            this.IndexOfRefraction = other.IndexOfRefraction;            

            this._CompatibilityFallbackMaterial = other._CompatibilityFallbackMaterial == null
                ? null
                : new MaterialBuilder(other._CompatibilityFallbackMaterial);

            foreach (var otherChannel in other._Channels)
            {
                var thisChannel = UseChannel(otherChannel.Key);

                otherChannel.CopyTo(thisChannel);
            }
        }

        #endregion

        #region data

        [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)]
        private readonly List<ChannelBuilder> _Channels = new List<ChannelBuilder>();

        [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)]
        private MaterialBuilder _CompatibilityFallbackMaterial;

        [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)]
        private string _ShaderStyle = SHADERPBRMETALLICROUGHNESS;


        public AlphaMode AlphaMode { get; set; } = AlphaMode.OPAQUE;

        public Single AlphaCutoff { get; set; } = 0.5f;

        /// <summary>
        /// Gets or sets a value indicating whether triangles must be rendered from both sides.
        /// </summary>
        public Boolean DoubleSided { get; set; }

        public String ShaderStyle
        {
            get => _ShaderStyle;
            set => _SetShader(value);
        }

        public float IndexOfRefraction { get; set; } = 1.5f;

        public float Dispersion { get; set; } = 0;

        /// <inheritdoc/>
        /// <remarks>
        /// Material builder uses default class equality, but NUnit4 does some weird trickery on equality
        /// </remarks>
        public override int GetHashCode() { return base.GetHashCode(); }

        /// <inheritdoc/>
        /// <remarks>
        /// MaterialBuilder objects use reference comparison by default.<br/>
        /// For content equality use <see cref="AreEqualByContent(MaterialBuilder, MaterialBuilder)"/>
        /// </remarks>
        public override bool Equals(object obj)
        {
            return Object.ReferenceEquals(this, obj);
        }       

        public static bool AreEqualByContent(MaterialBuilder x, MaterialBuilder y)
        {
            if ((x, y).AreSameReference(out bool areTheSame)) return areTheSame;

            if (!BaseBuilder.AreEqualByContent(x, y)) return false;

            if (x.AlphaMode != y.AlphaMode) return false;
            // AlphaCutoff only has meaning when AlphaMode = Mask
            if (x.AlphaMode == AlphaMode.MASK && x.AlphaCutoff != y.AlphaCutoff) return false;
            if (x.DoubleSided != y.DoubleSided) return false;
            if (x.Dispersion != y.Dispersion) return false;
            if (x.IndexOfRefraction != y.IndexOfRefraction) return false;            
            if (x._ShaderStyle != y._ShaderStyle) return false;

            if (!AreEqualByContent(x._CompatibilityFallbackMaterial, y._CompatibilityFallbackMaterial)) return false;

            // gather all unique channel keys used by both materials.

            var channelKeys = x._Channels
                .Concat(y._Channels)
                .Select(item => item.Key)
                .Distinct();

            foreach (var ckey in channelKeys)
            {
                var xc = x.GetChannel(ckey);
                var yc = y.GetChannel(ckey);

                if (!ChannelBuilder.AreEqualByContent(xc, yc)) return false;
            }

            return true;
        }

        public static int GetContentHashCode(MaterialBuilder x)
        {
            if (x == null) return 0;

            var h = BaseBuilder.GetContentHashCode(x);

            h ^= x.AlphaMode.GetHashCode();
            h ^= x.AlphaCutoff.GetHashCode();
            h ^= x.DoubleSided.GetHashCode();
            h ^= x.Dispersion.GetHashCode();
            h ^= x.IndexOfRefraction.GetHashCode();            
            h ^= x.ShaderStyle.GetHashCode(StringComparison.InvariantCulture);

            h ^= x._Channels
                .Select(item => ChannelBuilder.GetContentHashCode(item))
                .GetContentHashCode();

            h ^= GetContentHashCode(x._CompatibilityFallbackMaterial);

            return h;
        }

        #endregion

        #region properties

        [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.RootHidden)]
        public IReadOnlyCollection<ChannelBuilder> Channels => _Channels;

        public MaterialBuilder CompatibilityFallback
        {
            get => _CompatibilityFallbackMaterial;
            set
            {
                Guard.IsFalse(_CompatibilityFallbackMaterial == this, nameof(value), "Cannot use self as fallback material");
                _CompatibilityFallbackMaterial = value;
            }
        }

        public static IEqualityComparer<MaterialBuilder> ContentComparer => _ContentComparer.Default;

        public static IEqualityComparer<MaterialBuilder> ReferenceComparer => _ReferenceComparer.Default;

        #endregion

        #region API

        private void _SetShader(string shader)
        {
            Guard.NotNullOrEmpty(shader, nameof(shader));
            Guard.IsTrue(shader == SHADERUNLIT || shader == SHADERPBRMETALLICROUGHNESS || shader == SHADERPBRSPECULARGLOSSINESS, nameof(shader));

            _ShaderStyle = shader;

            var validChannels = _GetValidChannels();

            // remove incompatible channels.
            for (int i = _Channels.Count - 1; i >= 0; --i)
            {
                var c = _Channels[i];
                if (!validChannels.Contains(c.Key)) _Channels.RemoveAt(i);
            }
        }

        [Obsolete("Use GetChannel with KnownChannel whenever possible")]
        public ChannelBuilder GetChannel(string channelKey)
        {
            Guard.NotNullOrEmpty(channelKey, nameof(channelKey));
            var key = (KnownChannel)Enum.Parse(typeof(KnownChannel), channelKey, true);

            return GetChannel(key);
        }

        [Obsolete("Use UseChannel with KnownChannel whenever possible")]
        public ChannelBuilder UseChannel(string channelKey)
        {
            Guard.NotNullOrEmpty(channelKey, nameof(channelKey));
            var key = (KnownChannel)Enum.Parse(typeof(KnownChannel), channelKey, true);

            return UseChannel(key);
        }

        public ChannelBuilder GetChannel(KnownChannel channelKey)
        {
            return _Channels.FirstOrDefault(item => item.Key == channelKey);
        }        

        public ChannelBuilder UseChannel(KnownChannel channelKey)
        {
            Guard.IsTrue(_GetValidChannels().Contains(channelKey), nameof(channelKey));

            var ch = GetChannel(channelKey);
            if (ch != null) return ch;

            ch = new ChannelBuilder(this, channelKey);
            _Channels.Add(ch);

            return ch;
        }        

        public void RemoveChannel(KnownChannel key)
        {
            var idx = _Channels.IndexOf(item => item.Key == key);
            if (idx < 0) return;
            _Channels.RemoveAt(idx);
        }

        internal void ValidateForSchema2()
        {
            var hasClearCoat = this.GetChannel(KnownChannel.ClearCoat) != null
                || this.GetChannel(KnownChannel.ClearCoatNormal) != null
                || this.GetChannel(KnownChannel.ClearCoatRoughness) != null;

            var hasTransmission = this.GetChannel(KnownChannel.Transmission) != null;

            if (this.ShaderStyle == SHADERPBRSPECULARGLOSSINESS)
            {
                Guard.IsFalse(hasClearCoat, KnownChannel.ClearCoat.ToString(), "Clear Coat not supported for Specular Glossiness materials.");
                Guard.IsFalse(hasTransmission, KnownChannel.Transmission.ToString(), "Transmission not supported for Specular Glossiness materials.");

                if (this.CompatibilityFallback != null)
                {
                    Guard.MustBeNull(this.CompatibilityFallback.CompatibilityFallback, nameof(CompatibilityFallback));

                    Guard.MustBeEqualTo(this.Name, this.CompatibilityFallback.Name, nameof(Name));

                    Guard.IsTrue(this.CompatibilityFallback.ShaderStyle == SHADERPBRMETALLICROUGHNESS, nameof(ShaderStyle));

                    Guard.IsTrue(this.AlphaMode == this.CompatibilityFallback.AlphaMode, nameof(AlphaMode));
                    Guard.MustBeEqualTo(this.AlphaCutoff, this.CompatibilityFallback.AlphaCutoff, nameof(AlphaCutoff));
                    Guard.MustBeEqualTo(this.DoubleSided, this.CompatibilityFallback.DoubleSided, nameof(DoubleSided));
                    Guard.MustBeEqualTo(this.Dispersion, this.CompatibilityFallback.Dispersion, nameof(Dispersion));
                    Guard.MustBeEqualTo(this.IndexOfRefraction, this.CompatibilityFallback.IndexOfRefraction, nameof(IndexOfRefraction));                    

                    foreach (var chKey in new[] { KnownChannel.Normal, KnownChannel.Occlusion, KnownChannel.Emissive })
                    {
                        var primary = this.GetChannel(chKey);
                        var fallbck = this.CompatibilityFallback.GetChannel(chKey);

                        Guard.IsTrue(ChannelBuilder.AreEqualByContent(primary, fallbck), chKey.ToString(), "Primary and fallback materials must have the same channel properties");
                    }
                }
            }
            else
            {
                Guard.MustBeNull(this.CompatibilityFallback, nameof(CompatibilityFallback));
            }
        }

        #endregion

        #region API * With

        /// <summary>
        /// Sets <see cref="ShaderStyle"/>.
        /// </summary>
        /// <param name="shader">
        /// A valid shader style, which can be one of these values:
        /// <see cref="SHADERUNLIT"/>,
        /// <see cref="SHADERPBRMETALLICROUGHNESS"/>,
        /// <see cref="SHADERPBRSPECULARGLOSSINESS"/>
        /// </param>
        /// <returns>This <see cref="MaterialBuilder"/>.</returns>
        public MaterialBuilder WithShader(string shader) { _SetShader(shader); return this; }

        /// <summary>
        /// Sets <see cref="ShaderStyle"/> to use <see cref="SHADERUNLIT"/>.
        /// </summary>
        /// <returns>This <see cref="MaterialBuilder"/>.</returns>
        public MaterialBuilder WithUnlitShader() { _SetShader(SHADERUNLIT); return this; }

        /// <summary>
        /// Sets <see cref="ShaderStyle"/> to use <see cref="SHADERPBRMETALLICROUGHNESS"/>.
        /// </summary>
        /// <returns>This <see cref="MaterialBuilder"/>.</returns>
        public MaterialBuilder WithMetallicRoughnessShader() { _SetShader(SHADERPBRMETALLICROUGHNESS); return this; }

        /// <summary>
        /// Sets <see cref="ShaderStyle"/> to use <see cref="SHADERPBRSPECULARGLOSSINESS"/>.
        /// </summary>
        /// <returns>This <see cref="MaterialBuilder"/>.</returns>
        [Obsolete("SpecularGlossiness has been deprecated by Khronos")]
        public MaterialBuilder WithSpecularGlossinessShader() { _SetShader(SHADERPBRSPECULARGLOSSINESS); return this; }

        public MaterialBuilder WithAlpha(AlphaMode alphaMode = AlphaMode.OPAQUE, Single alphaCutoff = 0.5f)
        {
            this.AlphaMode = alphaMode;
            this.AlphaCutoff = alphaCutoff;

            return this;
        }

        public MaterialBuilder WithDoubleSide(bool enabled)
        {
            this.DoubleSided = enabled;

            return this;
        }

        [Obsolete("Use WithChannelParam(KnownChannel channelKey, KnownProperty propertyName, Object parameter)")]
        public MaterialBuilder WithChannelParam(KnownChannel channelKey, Vector4 parameter)
        {
            this.UseChannel(channelKey).Parameter = parameter;

            return this;
        }

        [Obsolete("Use WithChannelParam(KnownChannel channelKey, KnownProperty propertyName, Object parameter)")]
        public MaterialBuilder WithChannelParam(string channelKey, Vector4 parameter)
        {
            this.UseChannel(channelKey).Parameter = parameter;
            return this;
        }

        [Obsolete("Use WithChannelImage(KnownChannel channelKey, ImageBuilder primaryImage)")]
        public MaterialBuilder WithChannelImage(string channelKey, ImageBuilder primaryImage)
        {
            this.UseChannel(channelKey)
                .UseTexture()
                .WithPrimaryImage(primaryImage);

            return this;
        }

        public MaterialBuilder WithChannelParam(KnownChannel channelKey, KnownProperty propertyName, Object parameter)
        {
            this.UseChannel(channelKey).Parameters[propertyName] = MaterialValue.CreateFrom(parameter);
            return this;
        }        

        public MaterialBuilder WithChannelImage(KnownChannel channelKey, ImageBuilder primaryImage)
        {
            if (ImageBuilder.IsEmpty(primaryImage))
            {
                this.GetChannel(channelKey)?.RemoveTexture();
                return this;
            }

            this.UseChannel(channelKey)
                .UseTexture()
                .WithPrimaryImage(primaryImage);

            return this;
        }

        

        /// <summary>
        /// Defines a fallback <see cref="MaterialBuilder"/> instance for the current <see cref="MaterialBuilder"/>.
        /// </summary>
        /// <param name="fallback">
        /// A <see cref="MaterialBuilder"/> instance
        /// that must have a <see cref="ShaderStyle"/>
        /// of type <see cref="SHADERPBRMETALLICROUGHNESS"/></param>
        /// <returns>This <see cref="MaterialBuilder"/>.</returns>
        public MaterialBuilder WithFallback(MaterialBuilder fallback)
        {
            this.CompatibilityFallback = fallback;

            return this;
        }

        public MaterialBuilder WithMetallicRoughnessFallback(ImageBuilder baseColor, Vector4? rgba, ImageBuilder metallicRoughness, float? metallic, float? roughness)
        {
            var fallback = this
                .Clone()
                .WithMetallicRoughnessShader()
                .WithBaseColor(baseColor, rgba)
                .WithMetallicRoughness(metallicRoughness, metallic, roughness);

            return this.WithFallback(fallback);
        }

        #endregion

        #region API * With for specific channels

        public MaterialBuilder WithNormal(ImageBuilder imageFile, float scale = 1)
        {
            WithChannelImage(KnownChannel.Normal, imageFile);
            WithChannelParam(KnownChannel.Normal, KnownProperty.NormalScale, scale);
            return this;
        }

        public MaterialBuilder WithOcclusion(ImageBuilder imageFile, float strength = 1)
        {
            WithChannelImage(KnownChannel.Occlusion, imageFile);
            WithChannelParam(KnownChannel.Occlusion, KnownProperty.OcclusionStrength, strength);
            return this;
        }

        public MaterialBuilder WithEmissive(Vector3 rgb, float strength = 1)
        {
            WithChannelParam(KnownChannel.Emissive, KnownProperty.EmissiveStrength, strength);
            WithChannelParam(KnownChannel.Emissive, KnownProperty.RGB, rgb);
            return this;
        }

        public MaterialBuilder WithEmissive(ImageBuilder imageFile, Vector3? rgb = null, float strength = 1)
        {
            WithChannelImage(KnownChannel.Emissive, imageFile);
            if (rgb.HasValue) WithEmissive(rgb.Value, strength);
            return this;
        }

        public MaterialBuilder WithBaseColor(Vector4 rgba)
        {
            return WithChannelParam(KnownChannel.BaseColor, KnownProperty.RGBA, rgba);
        }

        public MaterialBuilder WithBaseColor(ImageBuilder imageFile, Vector4? rgba = null)
        {
            WithChannelImage(KnownChannel.BaseColor, imageFile);
            if (rgba.HasValue) WithBaseColor(rgba.Value);
            return this;
        }

        public MaterialBuilder WithMetallicRoughness(float? metallic = null, float? roughness = null)
        {
            if (!metallic.HasValue && !roughness.HasValue) return this;

            var channel = UseChannel(KnownChannel.MetallicRoughness);
            if (metallic.HasValue) channel.Parameters[KnownProperty.MetallicFactor] = metallic.Value;
            if (roughness.HasValue) channel.Parameters[KnownProperty.RoughnessFactor] = roughness.Value;
            return this;
        }

        public MaterialBuilder WithMetallicRoughness(ImageBuilder imageFile, float? metallic = null, float? roughness = null)
        {
            WithChannelImage(KnownChannel.MetallicRoughness, imageFile);
            WithMetallicRoughness(metallic, roughness);
            return this;
        }        

        public MaterialBuilder WithClearCoatNormal(ImageBuilder imageFile)
        {
            WithChannelImage(KnownChannel.ClearCoatNormal, imageFile);
            return this;
        }

        public MaterialBuilder WithClearCoat(ImageBuilder imageFile, float intensity)
        {
            WithChannelImage(KnownChannel.ClearCoat, imageFile);            
            WithChannelParam(KnownChannel.ClearCoat, KnownProperty.ClearCoatFactor, intensity);
            return this;
        }

        public MaterialBuilder WithClearCoatRoughness(ImageBuilder imageFile, float roughness)
        {
            WithChannelImage(KnownChannel.ClearCoatRoughness, imageFile);            
            WithChannelParam(KnownChannel.ClearCoatRoughness, KnownProperty.RoughnessFactor, roughness);
            return this;
        }

        public MaterialBuilder WithTransmission(ImageBuilder imageFile, float intensity)
        {
            WithChannelImage(KnownChannel.Transmission, imageFile);            
            WithChannelParam(KnownChannel.Transmission, KnownProperty.TransmissionFactor, intensity);
            return this;
        }

        public MaterialBuilder WithDiffuseTransmissionFactor(ImageBuilder imageFile, float factor)
        {
            WithChannelImage(KnownChannel.DiffuseTransmissionFactor, imageFile);
            WithChannelParam(KnownChannel.DiffuseTransmissionFactor, KnownProperty.DiffuseTransmissionFactor, factor);
            return this;
        }

        public MaterialBuilder WithDiffuseTransmissionColor(ImageBuilder imageFile, Vector3? rgb = null)
        {
            WithChannelImage(KnownChannel.DiffuseTransmissionColor, imageFile);
            if (rgb.HasValue) WithChannelParam(KnownChannel.DiffuseTransmissionColor, KnownProperty.RGB, rgb.Value);
            return this;
        }

        public MaterialBuilder WithSpecularColor(ImageBuilder imageFile, Vector3? rgb = null)
        {
            WithChannelImage(KnownChannel.SpecularColor, imageFile);            
            if (rgb.HasValue) WithChannelParam(KnownChannel.SpecularColor, KnownProperty.RGB, rgb.Value);
            return this;
        }

        public MaterialBuilder WithSpecularFactor(ImageBuilder imageFile, float factor)
        {
            WithChannelImage(KnownChannel.SpecularFactor, imageFile);            
            WithChannelParam(KnownChannel.SpecularFactor, KnownProperty.SpecularFactor, factor);
            return this;
        }

        public MaterialBuilder WithVolumeThickness(ImageBuilder imageFile, float factor)
        {
            WithChannelImage(KnownChannel.VolumeThickness, imageFile);            
            WithChannelParam(KnownChannel.VolumeThickness, KnownProperty.ThicknessFactor, factor);
            return this;
        }

        public MaterialBuilder WithVolumeAttenuation(Vector3 color, float distance)
        {            
            WithChannelParam(KnownChannel.VolumeAttenuation, KnownProperty.RGB, color);
            WithChannelParam(KnownChannel.VolumeAttenuation, KnownProperty.AttenuationDistance, distance);
            return this;
        }

        public MaterialBuilder WithIridescence(ImageBuilder imageFile, float factor = 0f, float ior = 1.3f)
        {
            WithChannelImage(KnownChannel.Iridescence, imageFile);            
            WithChannelParam(KnownChannel.Iridescence, KnownProperty.IridescenceFactor, factor);
            WithChannelParam(KnownChannel.Iridescence, KnownProperty.IndexOfRefraction, ior);
            return this;
        }

        public MaterialBuilder WithIridescenceThickness(ImageBuilder imageFile, float min = 100f, float max = 400f)
        {
            WithChannelImage(KnownChannel.IridescenceThickness, imageFile);
            WithChannelParam(KnownChannel.IridescenceThickness, KnownProperty.Minimum, min);
            WithChannelParam(KnownChannel.IridescenceThickness, KnownProperty.Maximum, max);
            return this;
        }

        public MaterialBuilder WithAnisotropy(ImageBuilder imageFile, float strength = 0f, float rotation = 0f)
        {
            WithChannelImage(KnownChannel.Anisotropy, imageFile);
            WithChannelParam(KnownChannel.Anisotropy, KnownProperty.AnisotropyStrength, strength);
            WithChannelParam(KnownChannel.Anisotropy, KnownProperty.AnisotropyRotation, rotation);
            return this;
        }

        #endregion

        #region API - OBSOLETE

        [Obsolete("This channel is used by KHR_materials_pbrSpecularGlossiness extension, which has been deprecated by Khronos; use WithBaseColor instead.")]
        public MaterialBuilder WithDiffuse(Vector4 rgba) { return WithChannelParam(KnownChannel.Diffuse, KnownProperty.RGBA, rgba); }

        [Obsolete("This channel is used by KHR_materials_pbrSpecularGlossiness extension, which has been deprecated by Khronos; use WithBaseColor instead.")]
        public MaterialBuilder WithDiffuse(ImageBuilder imageFile, Vector4? rgba = null)
        {
            WithChannelImage(KnownChannel.Diffuse, imageFile);
            if (rgba.HasValue) WithDiffuse(rgba.Value);
            return this;
        }

        [Obsolete("This channel is used by KHR_materials_pbrSpecularGlossiness extension, which has been deprecated by Khronos; use WithSpecularColor instead.")]
        public MaterialBuilder WithSpecularGlossiness(Vector3? specular = null, float? glossiness = null)
        {
            if (!specular.HasValue && !glossiness.HasValue) return this;

            var channel = UseChannel(KnownChannel.SpecularGlossiness);

            if (specular.HasValue) channel.Parameters[KnownProperty.SpecularFactor] = specular.Value;
            if (glossiness.HasValue) channel.Parameters[KnownProperty.GlossinessFactor] = glossiness.Value;

            return this;
        }

        [Obsolete("This channel is used by KHR_materials_pbrSpecularGlossiness extension, which has been deprecated by Khronos; use WithSpecularColor instead.")]
        public MaterialBuilder WithSpecularGlossiness(ImageBuilder imageFile, Vector3? specular = null, float? glossiness = null)
        {
            WithChannelImage(KnownChannel.SpecularGlossiness, imageFile);
            WithSpecularGlossiness(specular, glossiness);
            return this;
        }

        #endregion

        #region nested types

        sealed class _ContentComparer : IEqualityComparer<MaterialBuilder>
        {
            public static readonly _ContentComparer Default = new _ContentComparer();

            public bool Equals(MaterialBuilder x, MaterialBuilder y)
            {
                return AreEqualByContent(x, y);
            }

            public int GetHashCode(MaterialBuilder obj)
            {
                return GetContentHashCode(obj);
            }
        }

        sealed class _ReferenceComparer : IEqualityComparer<MaterialBuilder>
        {
            public static readonly _ReferenceComparer Default = new _ReferenceComparer();

            public bool Equals(MaterialBuilder x, MaterialBuilder y)
            {
                return Object.ReferenceEquals(x, y);
            }

            public int GetHashCode(MaterialBuilder obj)
            {
                return RuntimeHelpers.GetHashCode(obj);
            }
        }

        #endregion
    }
}
