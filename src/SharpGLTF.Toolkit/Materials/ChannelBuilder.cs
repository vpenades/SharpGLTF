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

        /// <summary>
        /// Gets or sets the <see cref="ChannelBuilder"/> paramenter.
        /// Its meaning depends on <see cref="Key"/>.
        /// </summary>
        public Vector4 Parameter { get; set; }

        public TextureBuilder Texture { get; private set; }

        public static bool AreEqual(ChannelBuilder a, ChannelBuilder b)
        {
            #pragma warning disable IDE0041 // Use 'is null' check
            if (Object.ReferenceEquals(a, b)) return true;
            if (Object.ReferenceEquals(a, null)) return false;
            if (Object.ReferenceEquals(b, null)) return false;
            #pragma warning restore IDE0041 // Use 'is null' check

            if (!Object.ReferenceEquals(a._Parent, b._Parent)) return false;

            if (a._Key != b._Key) return false;

            if (a.Parameter != b.Parameter) return false;

            if (!TextureBuilder.AreEqual(a.Texture, b.Texture)) return false;

            return true;
        }

        public static int GetContentHashCode(ChannelBuilder x)
        {
            if (x == null) return 0;

            var h = x._Key.GetHashCode();

            h ^= x.Parameter.GetHashCode();

            h ^= TextureBuilder.GetContentHashCode(x.Texture);

            return h;
        }

        #endregion

        #region properties

        /// <summary>
        /// Gets the <see cref="ChannelBuilder"/> name. It must be a name of <see cref="KnownChannels"/>.
        /// </summary>
        public String Key => _Key;

        public static IEqualityComparer<ChannelBuilder> ContentComparer => _ContentComparer.Default;

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

        #region Support types

        sealed class _ContentComparer : IEqualityComparer<ChannelBuilder>
        {
            public static readonly _ContentComparer Default = new _ContentComparer();

            public bool Equals(ChannelBuilder x, ChannelBuilder y)
            {
                return ChannelBuilder.AreEqual(x, y);
            }

            public int GetHashCode(ChannelBuilder obj)
            {
                return ChannelBuilder.GetContentHashCode(obj);
            }
        }

        #endregion
    }

}
