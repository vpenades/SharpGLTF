using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;

namespace SharpGLTF.Materials
{
    [System.Diagnostics.DebuggerDisplay("{Name} {ShaderStyle}")]
    public class MaterialBuilder
    {
        #region lifecycle

        public MaterialBuilder(string name = null)
        {
            Name = name;
        }

        public static MaterialBuilder CreateDefault()
        {
            return new MaterialBuilder("Default");
        }

        #endregion

        #region data

        public const string SHADERUNLIT = "Unlit";
        public const string SHADERPBRMETALLICROUGHNESS = "PBRMetallicRoughness";
        public const string SHADERPBRSPECULARGLOSSINESS = "PBRSpecularGlossiness";

        private readonly List<ChannelBuilder> _Channels = new List<ChannelBuilder>();

        private MaterialBuilder _CompatibilityFallbackMaterial;

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

        public String ShaderStyle { get; set; } = SHADERPBRMETALLICROUGHNESS;

        public static bool AreEqual(MaterialBuilder x, MaterialBuilder y)
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
            if (x.ShaderStyle != y.ShaderStyle) return false;

            if (!AreEqual(x._CompatibilityFallbackMaterial, y._CompatibilityFallbackMaterial)) return false;

            // gather all unique channel keys used by both materials.

            var channelKeys = x._Channels
                .Concat(y._Channels)
                .Select(item => item.Key)
                .Distinct();

            foreach (var ckey in channelKeys)
            {
                var xc = x.GetChannel(ckey);
                var yc = y.GetChannel(ckey);

                if (!ChannelBuilder.AreEqual(xc, yc)) return false;
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

        #endregion

        #region API

        /// <summary>
        /// Sets <see cref="MaterialBuilder.ShaderStyle"/> to use <see cref="SHADERUNLIT"/>.
        /// </summary>
        /// <returns>This <see cref="MaterialBuilder"/>.</returns>
        public MaterialBuilder WithUnlitShader() { return WithShader(SHADERUNLIT); }

        /// <summary>
        /// Sets <see cref="MaterialBuilder.ShaderStyle"/> to use <see cref="SHADERPBRMETALLICROUGHNESS"/>.
        /// </summary>
        /// <returns>This <see cref="MaterialBuilder"/>.</returns>
        public MaterialBuilder WithMetallicRoughnessShader() { return WithShader(SHADERPBRMETALLICROUGHNESS); }

        /// <summary>
        /// Sets <see cref="MaterialBuilder.ShaderStyle"/> to use <see cref="SHADERPBRSPECULARGLOSSINESS"/>.
        /// </summary>
        /// <returns>This <see cref="MaterialBuilder"/>.</returns>
        public MaterialBuilder WithSpecularGlossinessShader() { return WithShader(SHADERPBRSPECULARGLOSSINESS); }

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
        public MaterialBuilder WithShader(string shader)
        {
            Guard.NotNullOrEmpty(shader, nameof(shader));
            Guard.IsTrue(shader == SHADERUNLIT || shader == SHADERPBRMETALLICROUGHNESS || shader == SHADERPBRSPECULARGLOSSINESS, nameof(shader));
            ShaderStyle = shader;
            return this;
        }

        public ChannelBuilder GetChannel(KnownChannels channelKey)
        {
            return GetChannel(channelKey.ToString());
        }

        public ChannelBuilder GetChannel(string channelKey)
        {
            Guard.NotNullOrEmpty(channelKey, nameof(channelKey));

            channelKey = channelKey.ToLowerInvariant();

            return _Channels.FirstOrDefault(item => string.Equals(channelKey, item.Key, StringComparison.OrdinalIgnoreCase));
        }

        public ChannelBuilder UseChannel(KnownChannels channelKey)
        {
            return UseChannel(channelKey.ToString());
        }

        public ChannelBuilder UseChannel(string channelKey)
        {
            var ch = GetChannel(channelKey);
            if (ch != null) return ch;

            ch = new ChannelBuilder(this, channelKey);
            _Channels.Add(ch);

            return ch;
        }

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

        public MaterialBuilder WithChannelParam(KnownChannels channelKey, Vector4 parameter)
        {
            this.UseChannel(channelKey).Parameter = parameter;

            return this;
        }

        public MaterialBuilder WithChannelParam(string channelKey, Vector4 parameter)
        {
            this.UseChannel(channelKey).Parameter = parameter;

            return this;
        }

        public MaterialBuilder WithChannelImage(KnownChannels channelKey, string primaryImagePath)
        {
            this.UseChannel(channelKey)
                .UseTexture()
                .WithPrimaryImage(primaryImagePath);

            return this;
        }

        public MaterialBuilder WithChannelImage(string channelKey, string primaryImagePath)
        {
            this.UseChannel(channelKey)
                .UseTexture()
                .WithPrimaryImage(primaryImagePath);

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

        internal void ValidateForSchema2()
        {
            if (this.ShaderStyle == SHADERPBRSPECULARGLOSSINESS)
            {
                if (this.CompatibilityFallback != null)
                {
                    Guard.MustBeNull(this.CompatibilityFallback.CompatibilityFallback, nameof(this.CompatibilityFallback.CompatibilityFallback));

                    Guard.IsTrue(this.CompatibilityFallback.ShaderStyle == SHADERPBRMETALLICROUGHNESS, nameof(CompatibilityFallback.ShaderStyle));
                }
            }
            else
            {
                Guard.MustBeNull(this.CompatibilityFallback, nameof(CompatibilityFallback));
            }
        }

        public MaterialBuilder WithNormal(string imageFilePath, float scale = 1)
        {
            WithChannelImage("Normal", imageFilePath);
            WithChannelParam("Normal", new Vector4(scale, 0, 0, 0));

            return this;
        }

        public MaterialBuilder WithOcclusion(string imageFilePath, float strength = 1)
        {
            WithChannelImage("Occlusion", imageFilePath);
            WithChannelParam("Occlusion", new Vector4(strength, 0, 0, 0));

            return this;
        }

        public MaterialBuilder WithEmissive(string imageFilePath, Vector3 emissiveFactor)
        {
            WithChannelImage("Emissive", imageFilePath);
            WithChannelParam("Emissive", new Vector4(emissiveFactor, 0));

            return this;
        }

        public MaterialBuilder WithEmissive(string imageFilePath) { return WithChannelImage("Emissive", imageFilePath); }

        public MaterialBuilder WithEmissive(Vector3 emissiveFactor) { return WithChannelParam("Emissive", new Vector4(emissiveFactor, 0)); }

        #endregion

        #region support types

        sealed class _ContentComparer : IEqualityComparer<MaterialBuilder>
        {
            public static readonly _ContentComparer Default = new _ContentComparer();

            public bool Equals(MaterialBuilder x, MaterialBuilder y)
            {
                return MaterialBuilder.AreEqual(x, y);
            }

            public int GetHashCode(MaterialBuilder obj)
            {
                return MaterialBuilder.GetContentHashCode(obj);
            }
        }

        #endregion
    }
}
