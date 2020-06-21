using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Text;

using IMAGEFILE = SharpGLTF.Memory.MemoryImage;

namespace SharpGLTF.Materials
{
    [System.Diagnostics.DebuggerDisplay("{_DebuggerDisplay(),nq}")]
    public class MaterialBuilder
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

        private static readonly KnownChannel[] _UnlitChannels = new[] { KnownChannel.BaseColor };

        private static readonly KnownChannel[] _MetRouChannels = new[]
        {
            KnownChannel.BaseColor,
            KnownChannel.MetallicRoughness,
            KnownChannel.Normal,
            KnownChannel.Occlusion,
            KnownChannel.Emissive,
            KnownChannel.ClearCoat,
            KnownChannel.ClearCoatNormal,
            KnownChannel.ClearCoatRoughness
        };

        private static readonly KnownChannel[] _SpeGloChannels = new[]
        {
            KnownChannel.Diffuse,
            KnownChannel.SpecularGlossiness,
            KnownChannel.Normal,
            KnownChannel.Occlusion,
            KnownChannel.Emissive,
            // KnownChannel.ClearCoat,
            // KnownChannel.ClearCoatNormal,
            // KnownChannel.ClearCoatRoughness
        };

        #endregion

        #region lifecycle

        public MaterialBuilder(string name = null)
        {
            Name = name;
        }

        public static MaterialBuilder CreateDefault()
        {
            return new MaterialBuilder("Default");
        }

        public MaterialBuilder Clone() { return new MaterialBuilder(this); }

        public MaterialBuilder(MaterialBuilder other)
        {
            Guard.NotNull(other, nameof(other));

            this.Name = other.Name;
            this.AlphaMode = other.AlphaMode;
            this.AlphaCutoff = other.AlphaCutoff;
            this.DoubleSided = other.DoubleSided;
            this.ShaderStyle = other.ShaderStyle;

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

        /// <summary>
        /// Gets or sets the name of this <see cref="MaterialBuilder"/> instance.
        /// </summary>
        public string Name { get; set; }

        public AlphaMode AlphaMode { get; set; } = AlphaMode.OPAQUE;

        public Single AlphaCutoff { get; set; } = 0.5f;

        /// <summary>
        /// Gets or sets a value indicating whether triangles must be rendered from both sides.
        /// </summary>
        public Boolean DoubleSided { get; set; } = false;

        public String ShaderStyle
        {
            get => _ShaderStyle;
            set => _SetShader(value);
        }

        public static bool AreEqualByContent(MaterialBuilder x, MaterialBuilder y)
        {
            #pragma warning disable IDE0041 // Use 'is null' check
            if (Object.ReferenceEquals(x, y)) return true;
            if (Object.ReferenceEquals(x, null)) return false;
            if (Object.ReferenceEquals(y, null)) return false;
            #pragma warning restore IDE0041 // Use 'is null' check

            // Although .Name is not strictly a material property,
            // it identifies a specific material during Runtime that
            // might be relevant and needs to be preserved.
            // If an author needs materials to be merged, it's better
            // to keep the Name as null, or to use a common name like "Default".

            if (x.Name != y.Name) return false;
            if (x.AlphaMode != y.AlphaMode) return false;
            if (x.AlphaCutoff != y.AlphaCutoff) return false;
            if (x.DoubleSided != y.DoubleSided) return false;
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

            var h = x.Name == null ? 0 : x.Name.GetHashCode();

            h ^= x.AlphaMode.GetHashCode();
            h ^= x.AlphaCutoff.GetHashCode();
            h ^= x.DoubleSided.GetHashCode();
            h ^= x.ShaderStyle.GetHashCode();

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

        private IReadOnlyList<KnownChannel> _GetValidChannels()
        {
            switch (ShaderStyle)
            {
                case SHADERUNLIT: return _UnlitChannels;
                case SHADERPBRMETALLICROUGHNESS: return _MetRouChannels;
                case SHADERPBRSPECULARGLOSSINESS: return _SpeGloChannels;
                default: throw new NotImplementedException();
            }
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

        public ChannelBuilder GetChannel(string channelKey)
        {
            Guard.NotNullOrEmpty(channelKey, nameof(channelKey));
            var key = (KnownChannel)Enum.Parse(typeof(KnownChannel), channelKey, true);

            return GetChannel(key);
        }

        public ChannelBuilder UseChannel(string channelKey)
        {
            Guard.NotNullOrEmpty(channelKey, nameof(channelKey));
            var key = (KnownChannel)Enum.Parse(typeof(KnownChannel), channelKey, true);

            return UseChannel(key);
        }

        public void RemoveChannel(KnownChannel key)
        {
            var idx = _Channels.IndexOf(item => item.Key == key);
            if (idx < 0) return;
            _Channels.RemoveAt(idx);
        }

        internal void ValidateForSchema2()
        {
            var hasClearCoat = this.GetChannel("ClearCoat") != null
                || this.GetChannel("ClearCoatRoughness") != null
                || this.GetChannel("ClearCoatNormal") != null;

            if (this.ShaderStyle == SHADERPBRSPECULARGLOSSINESS)
            {
                Guard.IsFalse(hasClearCoat, KnownChannel.ClearCoat.ToString(), "Clear Coat not supported for Specular Glossiness materials.");

                if (this.CompatibilityFallback != null)
                {
                    Guard.MustBeNull(this.CompatibilityFallback.CompatibilityFallback, nameof(CompatibilityFallback));

                    Guard.MustBeEqualTo(this.Name, this.CompatibilityFallback.Name, nameof(Name));

                    Guard.IsTrue(this.CompatibilityFallback.ShaderStyle == SHADERPBRMETALLICROUGHNESS, nameof(ShaderStyle));

                    Guard.IsTrue(this.AlphaMode == this.CompatibilityFallback.AlphaMode, nameof(AlphaMode));
                    Guard.MustBeEqualTo(this.AlphaCutoff, this.CompatibilityFallback.AlphaCutoff, nameof(AlphaCutoff));
                    Guard.MustBeEqualTo(this.DoubleSided, this.CompatibilityFallback.DoubleSided, nameof(DoubleSided));

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
        /// Sets <see cref="MaterialBuilder.ShaderStyle"/>.
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
        /// Sets <see cref="MaterialBuilder.ShaderStyle"/> to use <see cref="SHADERUNLIT"/>.
        /// </summary>
        /// <returns>This <see cref="MaterialBuilder"/>.</returns>
        public MaterialBuilder WithUnlitShader() { _SetShader(SHADERUNLIT); return this; }

        /// <summary>
        /// Sets <see cref="MaterialBuilder.ShaderStyle"/> to use <see cref="SHADERPBRMETALLICROUGHNESS"/>.
        /// </summary>
        /// <returns>This <see cref="MaterialBuilder"/>.</returns>
        public MaterialBuilder WithMetallicRoughnessShader() { _SetShader(SHADERPBRMETALLICROUGHNESS); return this; }

        /// <summary>
        /// Sets <see cref="MaterialBuilder.ShaderStyle"/> to use <see cref="SHADERPBRSPECULARGLOSSINESS"/>.
        /// </summary>
        /// <returns>This <see cref="MaterialBuilder"/>.</returns>
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

        public MaterialBuilder WithChannelParam(KnownChannel channelKey, Vector4 parameter)
        {
            this.UseChannel(channelKey).Parameter = parameter;

            return this;
        }

        public MaterialBuilder WithChannelImage(KnownChannel channelKey, IMAGEFILE primaryImage)
        {
            if (primaryImage.IsEmpty)
            {
                this.GetChannel(channelKey)?.RemoveTexture();
                return this;
            }

            this.UseChannel(channelKey)
                .UseTexture()
                .WithPrimaryImage(primaryImage);

            return this;
        }

        public MaterialBuilder WithChannelParam(string channelKey, Vector4 parameter)
        {
            this.UseChannel(channelKey).Parameter = parameter;
            return this;
        }

        public MaterialBuilder WithChannelImage(string channelKey, IMAGEFILE primaryImage)
        {
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
        /// that must have a <see cref="MaterialBuilder.ShaderStyle"/>
        /// of type <see cref="SHADERPBRMETALLICROUGHNESS"/></param>
        /// <returns>This <see cref="MaterialBuilder"/>.</returns>
        public MaterialBuilder WithFallback(MaterialBuilder fallback)
        {
            this.CompatibilityFallback = fallback;

            return this;
        }

        public MaterialBuilder WithMetallicRoughnessFallback(IMAGEFILE baseColor, Vector4? rgba, IMAGEFILE metallicRoughness, float? metallic, float? roughness)
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

        public MaterialBuilder WithNormal(IMAGEFILE imageFile, float scale = 1)
        {
            WithChannelImage(KnownChannel.Normal, imageFile);
            WithChannelParam(KnownChannel.Normal, new Vector4(scale, 0, 0, 0));
            return this;
        }

        public MaterialBuilder WithOcclusion(IMAGEFILE imageFile, float strength = 1)
        {
            WithChannelImage(KnownChannel.Occlusion, imageFile);
            WithChannelParam(KnownChannel.Occlusion, new Vector4(strength, 0, 0, 0));
            return this;
        }

        public MaterialBuilder WithEmissive(Vector3 rgb) { return WithChannelParam(KnownChannel.Emissive, new Vector4(rgb, 1)); }

        public MaterialBuilder WithEmissive(IMAGEFILE imageFile, Vector3? rgb = null)
        {
            WithChannelImage(KnownChannel.Emissive, imageFile);
            if (rgb.HasValue) WithEmissive(rgb.Value);
            return this;
        }

        public MaterialBuilder WithBaseColor(Vector4 rgba) { return WithChannelParam(KnownChannel.BaseColor, rgba); }

        public MaterialBuilder WithBaseColor(IMAGEFILE imageFile, Vector4? rgba = null)
        {
            WithChannelImage(KnownChannel.BaseColor, imageFile);
            if (rgba.HasValue) WithBaseColor(rgba.Value);
            return this;
        }

        public MaterialBuilder WithMetallicRoughness(float? metallic = null, float? roughness = null)
        {
            if (!metallic.HasValue && !roughness.HasValue) return this;

            var channel = UseChannel(KnownChannel.MetallicRoughness);
            var val = channel.Parameter;
            if (metallic.HasValue) val.X = metallic.Value;
            if (roughness.HasValue) val.Y = roughness.Value;
            channel.Parameter = val;

            return this;
        }

        public MaterialBuilder WithMetallicRoughness(IMAGEFILE imageFile, float? metallic = null, float? roughness = null)
        {
            WithChannelImage(KnownChannel.MetallicRoughness, imageFile);
            WithMetallicRoughness(metallic, roughness);
            return this;
        }

        public MaterialBuilder WithDiffuse(Vector4 rgba) { return WithChannelParam(KnownChannel.Diffuse, rgba); }

        public MaterialBuilder WithDiffuse(IMAGEFILE imageFile, Vector4? rgba = null)
        {
            WithChannelImage(KnownChannel.Diffuse, imageFile);
            if (rgba.HasValue) WithDiffuse(rgba.Value);
            return this;
        }

        public MaterialBuilder WithSpecularGlossiness(Vector3? specular = null, float? glossiness = null)
        {
            if (!specular.HasValue && !glossiness.HasValue) return this;

            var channel = UseChannel(KnownChannel.SpecularGlossiness);

            var val = channel.Parameter;
            if (specular.HasValue)
            {
                val.X = specular.Value.X;
                val.Y = specular.Value.Y;
                val.Z = specular.Value.Z;
            }

            if (glossiness.HasValue) val.W = glossiness.Value;

            channel.Parameter = val;

            return this;
        }

        public MaterialBuilder WithSpecularGlossiness(IMAGEFILE imageFile, Vector3? specular = null, float? glossiness = null)
        {
            WithChannelImage(KnownChannel.SpecularGlossiness, imageFile);
            WithSpecularGlossiness(specular, glossiness);
            return this;
        }

        public MaterialBuilder WithClearCoatNormal(IMAGEFILE imageFile)
        {
            WithChannelImage(KnownChannel.ClearCoatNormal, imageFile);
            return this;
        }

        public MaterialBuilder WithClearCoat(IMAGEFILE imageFile, float intensity)
        {
            WithChannelImage(KnownChannel.ClearCoat, imageFile);
            WithChannelParam(KnownChannel.ClearCoat, new Vector4(intensity, 0, 0, 0));
            return this;
        }

        public MaterialBuilder WithClearCoatRoughness(IMAGEFILE imageFile, float roughness)
        {
            WithChannelImage(KnownChannel.ClearCoatRoughness, imageFile);
            WithChannelParam(KnownChannel.ClearCoatRoughness, new Vector4(roughness, 0, 0, 0));
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
