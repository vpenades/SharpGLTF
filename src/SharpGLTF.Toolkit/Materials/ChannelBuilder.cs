using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;

namespace SharpGLTF.Materials
{
    [System.Diagnostics.DebuggerDisplay("{Key} {Parameter}")]
    public class ChannelBuilder
    {
        #region lifecycle

        internal ChannelBuilder(MaterialBuilder parent, string key)
        {
            Guard.NotNull(parent, nameof(parent));
            Guard.NotNullOrEmpty(key, nameof(key));
            Guard.IsTrue(Enum.GetNames(typeof(KnownChannels)).Contains(key), nameof(key), $"{nameof(key)} must be a name of {nameof(KnownChannels)}.");

            _Parent = parent;
            _Key = key;

            SetDefaultParameter();
        }

        #endregion

        #region data

        private readonly MaterialBuilder _Parent;

        private readonly String _Key;

        #endregion

        #region properties

        /// <summary>
        /// Gets the <see cref="ChannelBuilder"/> name. It must be a name of <see cref="KnownChannels"/>.
        /// </summary>
        public String Key => _Key;

        /// <summary>
        /// Gets or sets the <see cref="ChannelBuilder"/> paramenter.
        /// Its meaning depends on <see cref="Key"/>.
        /// </summary>
        public Vector4 Parameter { get; set; }

        public TextureBuilder Texture { get; private set; }

        #endregion

        #region API

        public void SetDefaultParameter()
        {
            switch (_Key)
            {
                case "Emissive": Parameter = Vector4.Zero; break;

                case "Normal":
                case "Occlusion":
                    Parameter = new Vector4(1, 0, 0, 0); break;

                case "BaseColor":
                case "Diffuse":
                    Parameter = Vector4.One; break;

                case "MetalicRoughness": Parameter = new Vector4(1, 1, 0, 0); break;

                case "SpecularGlossiness": Parameter = Vector4.One; break;
            }
        }

        public TextureBuilder UseTexture()
        {
            if (Texture == null) Texture = new TextureBuilder(this);
            return Texture;
        }

        public void RemoveTexture() { Texture = null; }

        #endregion
    }

    public enum KnownChannels
    {
        Normal,
        Occlusion,
        Emissive,

        BaseColor,
        MetallicRoughness,

        Diffuse,
        SpecularGlossiness,
    }
}
