using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace SharpGLTF.Materials
{
    [System.Diagnostics.DebuggerDisplay("{Key} {Parameter}")]
    public class MaterialChannelBuilder
    {
        #region lifecycle

        internal MaterialChannelBuilder(MaterialBuilder parent, string key)
        {
            _Parent = parent; _Key = key;

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

        #endregion

        #region data

        private readonly MaterialBuilder _Parent;

        private readonly String _Key;

        #endregion

        #region properties

        public String Key => _Key;

        public Vector4 Parameter { get; set; }

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
        MetallicRoughness,

        Diffuse,
        SpecularGlossiness,
    }
}
