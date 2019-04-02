using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace SharpGLTF.Materials
{
    [System.Diagnostics.DebuggerDisplay("{_Key} {Amount}")]
    public class MaterialChannelBuilder
    {
        #region lifecycle

        internal MaterialChannelBuilder(MaterialBuilder parent, string key) { _Parent = parent; _Key = key; }

        #endregion

        #region data

        private readonly MaterialBuilder _Parent;

        private readonly String _Key;

        #endregion

        #region properties

        public String Key => _Key;

        public Single Amount { get; set; } = 1;

        public Vector4 Color { get; set; } = Vector4.One;

        public TextureBuilder Texture { get; private set; }

        #endregion

        #region API

        public TextureBuilder UseTexture()
        {
            if (Texture == null) Texture = new TextureBuilder(this);
            return Texture;
        }

        #endregion
    }

    public enum KnownChannels
    {
        Normal,
        Occlusion,
        Emissive,

        BaseColor,
        Metallic,
        Roughness,

        Diffuse,
        Specular,
        Glosiness,
    }
}
