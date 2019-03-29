using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace SharpGLTF.Materials
{
    [System.Diagnostics.DebuggerDisplay("{_Key} {Factor}")]
    public class MaterialChannelBuilder
    {
        #region lifecycle

        internal MaterialChannelBuilder(string key) { _Key = key; }

        #endregion

        #region data

        private readonly String _Key;

        #endregion

        #region properties

        public String Key => _Key;

        public Vector4 Factor { get; set; } = Vector4.One;

        public TextureBuilder Texture { get; set; }

        #endregion

        #region API

        public TextureBuilder UseTexture()
        {
            if (Texture == null) Texture = new TextureBuilder();
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
