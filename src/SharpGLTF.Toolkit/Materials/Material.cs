using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;

namespace SharpGLTF.Materials
{
    using ALPHA = Schema2.AlphaMode;

    [System.Diagnostics.DebuggerDisplay("{Name} {Style}")]
    public class MaterialBuilder
    {
        #region lifecycle

        public MaterialBuilder(string name = null)
        {
            Name = name;
        }

        #endregion

        #region data

        private readonly List<MaterialChannelBuilder> _Channels = new List<MaterialChannelBuilder>();

        private MaterialBuilder _CompatibilityFallbackMaterial;

        #endregion

        #region properties

        public string Name { get; set; }

        public IReadOnlyCollection<MaterialChannelBuilder> Channels => _Channels;

        public ALPHA AlphaMode { get; set; } = ALPHA.OPAQUE;

        public Single AlphaCutoff { get; set; } = 0.5f;

        public Boolean DoubleSided { get; set; } = false;

        public Boolean Unlit { get; set; } = false;

        public String Style { get; set; } = "PBRMetallicRoughness";

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

        public MaterialChannelBuilder GetChannel(KnownChannels channelKey)
        {
            return GetChannel(channelKey.ToString());
        }

        public MaterialChannelBuilder GetChannel(string channelKey)
        {
            Guard.NotNullOrEmpty(channelKey, nameof(channelKey));

            channelKey = channelKey.ToLower();

            return _Channels.FirstOrDefault(item => string.Equals(channelKey, item.Key, StringComparison.OrdinalIgnoreCase));
        }

        public MaterialChannelBuilder UseChannel(KnownChannels channelKey)
        {
            return UseChannel(channelKey.ToString());
        }

        public MaterialChannelBuilder UseChannel(string channelKey)
        {
            var ch = GetChannel(channelKey);

            if (ch != null) return ch;

            ch = new MaterialChannelBuilder(channelKey);
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

        public MaterialBuilder WithChannelColor(KnownChannels channelKey, Vector4 color)
        {
            this.UseChannel(channelKey).Factor = color;
            return this;
        }

        public MaterialBuilder WithChannelColor(string channelKey, Vector4 color)
        {
            this.UseChannel(channelKey).Factor = color;
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

        #endregion
    }
}
