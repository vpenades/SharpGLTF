using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;

namespace SharpGLTF.Materials
{
    [System.Diagnostics.DebuggerDisplay("{_GetDebuggerDisplay(),nq}")]
    public class ChannelBuilder
    {
        #region debug

        private string _GetDebuggerDisplay()
        {
            var txt = Key.ToString();

            var hasParam = false;

            if (Parameter != _GetDefaultParameter(_Key))
            {
                hasParam = true;

                var rgb = $"𝐑 {Parameter.X} 𝐆 {Parameter.Y} 𝐁 {Parameter.Z}";
                var rgba = $"{rgb} 𝐀 {Parameter.W}";

                switch (Key)
                {
                    case KnownChannel.Normal:
                    case KnownChannel.ClearCoatNormal:
                    case KnownChannel.Occlusion:
                        txt += $" {Parameter.X}"; break;

                    case KnownChannel.Emissive:
                        txt += $" ({rgb})"; break;

                    case KnownChannel.Diffuse:
                    case KnownChannel.BaseColor:
                        txt += $" ({rgba})"; break;

                    case KnownChannel.MetallicRoughness:
                        txt += $" 𝐌 {Parameter.X} 𝐑 {Parameter.Y}"; break;

                    case KnownChannel.SpecularGlossiness:
                        txt += $" 𝐒 ({rgb}) 𝐆 {Parameter.Y}"; break;

                    default:
                        txt += $" {Parameter}"; break;
                }
            }

            var tex = GetValidTexture();
            if (tex != null)
            {
                if (hasParam) txt += " ×";
                txt += $" {tex.PrimaryImage.DisplayText}";
            }

            return txt;
        }

        #endregion

        #region lifecycle

        internal ChannelBuilder(MaterialBuilder parent, KnownChannel key)
        {
            Guard.NotNull(parent, nameof(parent));

            _Parent = parent;
            _Key = key;

            SetDefaultParameter();
        }

        #endregion

        #region data

        [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)]
        private readonly MaterialBuilder _Parent;

        [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)]
        private readonly KnownChannel _Key;

        /// <summary>
        /// Gets or sets the <see cref="ChannelBuilder"/> paramenter.
        /// Its meaning depends on <see cref="Key"/>.
        /// </summary>
        public Vector4 Parameter { get; set; }

        public TextureBuilder Texture { get; private set; }

        public static bool AreEqualByContent(ChannelBuilder a, ChannelBuilder b)
        {
            #pragma warning disable IDE0041 // Use 'is null' check
            if (Object.ReferenceEquals(a, b)) return true;
            if (Object.ReferenceEquals(a, null)) return false;
            if (Object.ReferenceEquals(b, null)) return false;
            #pragma warning restore IDE0041 // Use 'is null' check

            if (a._Key != b._Key) return false;

            if (a.Parameter != b.Parameter) return false;

            if (!TextureBuilder.AreEqualByContent(a.Texture, b.Texture)) return false;

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
        /// Gets the <see cref="ChannelBuilder"/> name. It must be a name of <see cref="KnownChannel"/>.
        /// </summary>
        public KnownChannel Key => _Key;

        [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)]
        public static IEqualityComparer<ChannelBuilder> ContentComparer => _ContentComparer.Default;

        #endregion

        #region API

        public TextureBuilder GetValidTexture()
        {
            if (Texture == null) return null;
            if (Texture.PrimaryImage.IsEmpty) return null;
            return Texture;
        }

        internal void CopyTo(ChannelBuilder other)
        {
            other.Parameter = this.Parameter;

            if (this.Texture == null)
            {
                RemoveTexture();
            }
            else
            {
                this.Texture.CopyTo(other.UseTexture());
            }
        }

        public void SetDefaultParameter()
        {
            this.Parameter = _GetDefaultParameter(_Key);
        }

        private static Vector4 _GetDefaultParameter(KnownChannel key)
        {
            switch (key)
            {
                case KnownChannel.Emissive: return Vector4.Zero;

                case KnownChannel.Normal:
                case KnownChannel.ClearCoatNormal:
                case KnownChannel.Occlusion:
                    return  Vector4.UnitX;

                case KnownChannel.BaseColor:
                case KnownChannel.Diffuse:
                    return Vector4.One;

                case KnownChannel.MetallicRoughness: return new Vector4(1, 1, 0, 0);
                case KnownChannel.SpecularGlossiness: return Vector4.One;

                case KnownChannel.ClearCoat: return Vector4.Zero;
                case KnownChannel.ClearCoatRoughness: return Vector4.Zero;

                default: throw new NotImplementedException();
            }
        }

        public TextureBuilder UseTexture()
        {
            if (Texture == null) Texture = new TextureBuilder(this);
            return Texture;
        }

        public void RemoveTexture() { Texture = null; }

        #endregion

        #region Nested types

        sealed class _ContentComparer : IEqualityComparer<ChannelBuilder>
        {
            public static readonly _ContentComparer Default = new _ContentComparer();

            public bool Equals(ChannelBuilder x, ChannelBuilder y)
            {
                return AreEqualByContent(x, y);
            }

            public int GetHashCode(ChannelBuilder obj)
            {
                return GetContentHashCode(obj);
            }
        }

        #endregion
    }
}
