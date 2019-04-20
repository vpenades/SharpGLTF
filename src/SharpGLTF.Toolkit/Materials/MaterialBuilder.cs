using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;

namespace SharpGLTF.Materials
{
    using ALPHA = Schema2.AlphaMode;

    [System.Diagnostics.DebuggerDisplay("{Name} {ShaderStyle}")]
    public class MaterialBuilder
    {
        #region lifecycle

        public MaterialBuilder(string name = null)
        {
            Name = name;
        }

        #endregion

        #region data

        public const string SHADERUNLIT = "Unlit";
        public const string SHADERPBRMETALLICROUGHNESS = "PBRMetallicRoughness";
        public const string SHADERPBRSPECULARGLOSSINESS = "PBRSpecularGlossiness";

        private readonly List<ChannelBuilder> _Channels = new List<ChannelBuilder>();

        private MaterialBuilder _CompatibilityFallbackMaterial;

        #endregion

        #region properties

        public string Name { get; set; }

        public IReadOnlyCollection<ChannelBuilder> Channels => _Channels;

        public ALPHA AlphaMode { get; set; } = ALPHA.OPAQUE;

        public Single AlphaCutoff { get; set; } = 0.5f;

        public Boolean DoubleSided { get; set; } = false;

        public String ShaderStyle { get; set; } = SHADERPBRMETALLICROUGHNESS;

        public MaterialBuilder CompatibilityFallback
        {
            get => _CompatibilityFallbackMaterial;
            set
            {
                if (_CompatibilityFallbackMaterial == this) throw new ArgumentException(nameof(value));
                _CompatibilityFallbackMaterial = value;
            }
        }

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

            channelKey = channelKey.ToLower();

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

        public MaterialBuilder WithAlpha(ALPHA alphaMode = ALPHA.OPAQUE, Single alphaCutoff = 0.5f)
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

        public MaterialBuilder WithChannelImage(KnownChannels channelKey, string imageFilePath)
        {
            this.UseChannel(channelKey)
                .UseTexture()
                .WithImage(imageFilePath);

            return this;
        }

        public MaterialBuilder WithChannelImage(string channelKey, string imageFilePath)
        {
            this.UseChannel(channelKey)
                .UseTexture()
                .WithImage(imageFilePath);

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
    }
}
