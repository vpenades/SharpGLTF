using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace SharpGLTF.Schema2
{
    /// <summary>
    /// Represents a material sub-channel, which usually contains a texture.
    /// </summary>
    /// <remarks>
    /// This structure is not part of the gltf schema,
    /// but wraps several components of the material
    /// to have an homogeneous and easy to use API.
    /// </remarks>
    [System.Diagnostics.DebuggerDisplay("Channel {_Key}")]
    public struct MaterialChannel
    {
        #region lifecycle

        internal MaterialChannel(Material m, String key, Func<Boolean, TextureInfo> texInfo)
        {
            Guard.NotNull(m, nameof(m));
            Guard.NotNullOrEmpty(key, nameof(key));

            Guard.NotNull(texInfo, nameof(texInfo));

            _Key = key;
            _Material = m;

            _TextureInfo = texInfo;

            _ColorGetter = () => Vector4.One;
            _ColorSetter = val => { };

            _AmountGetter = () => texInfo(false)?.Amount ?? 1;
            _AmountSetter = val => texInfo(true).Amount = val;

            IsTextureAmountSupported = true;
            IsColorSupported = false;
        }

        internal MaterialChannel(Material m, String key, Func<Boolean, TextureInfo> texInfo, Func<Single> agetter, Action<Single> asetter)
        {
            Guard.NotNull(m, nameof(m));
            Guard.NotNullOrEmpty(key, nameof(key));

            Guard.NotNull(texInfo, nameof(texInfo));
            Guard.NotNull(agetter, nameof(agetter));
            Guard.NotNull(asetter, nameof(asetter));

            _Key = key;
            _Material = m;

            _TextureInfo = texInfo;

            _ColorGetter = () => Vector4.One;
            _ColorSetter = val => { };

            _AmountGetter = agetter;
            _AmountSetter = asetter;

            IsTextureAmountSupported = true;
            IsColorSupported = false;
        }

        internal MaterialChannel(Material m, String key, Func<Boolean, TextureInfo> texInfo, Func<Vector4> cgetter, Action<Vector4> csetter)
        {
            Guard.NotNull(m, nameof(m));
            Guard.NotNullOrEmpty(key, nameof(key));

            Guard.NotNull(texInfo, nameof(texInfo));

            Guard.NotNull(cgetter, nameof(cgetter));
            Guard.NotNull(csetter, nameof(csetter));

            _Key = key;
            _Material = m;

            _TextureInfo = texInfo;

            _ColorGetter = cgetter;
            _ColorSetter = csetter;

            _AmountGetter = () => texInfo(false)?.Amount ?? 1;
            _AmountSetter = val => texInfo(true).Amount = val;

            IsTextureAmountSupported = false;
            IsColorSupported = true;
        }

        internal MaterialChannel(Material m, String key, Func<Single> agetter, Action<Single> asetter)
        {
            Guard.NotNull(m, nameof(m));
            Guard.NotNullOrEmpty(key, nameof(key));

            Guard.NotNull(agetter, nameof(agetter));
            Guard.NotNull(asetter, nameof(asetter));

            _Key = key;
            _Material = m;

            _TextureInfo = null;

            _ColorGetter = () => Vector4.One;
            _ColorSetter = val => { };

            _AmountGetter = agetter;
            _AmountSetter = asetter;

            IsTextureAmountSupported = true;
            IsColorSupported = false;
        }

        internal MaterialChannel(Material m, String key, Func<Vector4> cgetter, Action<Vector4> csetter)
        {
            Guard.NotNull(m, nameof(m));
            Guard.NotNullOrEmpty(key, nameof(key));

            Guard.NotNull(cgetter, nameof(cgetter));
            Guard.NotNull(csetter, nameof(csetter));

            _Key = key;
            _Material = m;

            _TextureInfo = null;

            _ColorGetter = cgetter;
            _ColorSetter = csetter;

            _AmountGetter = () => 1;
            _AmountSetter = val => { };

            IsTextureAmountSupported = false;
            IsColorSupported = true;
        }

        #endregion

        #region data

        private readonly String _Key;
        private readonly Material _Material;

        private readonly Func<Boolean, TextureInfo> _TextureInfo;

        private readonly Func<Single> _AmountGetter;
        private readonly Action<Single> _AmountSetter;

        private readonly Func<Vector4> _ColorGetter;
        private readonly Action<Vector4> _ColorSetter;

        #endregion

        #region properties

        public Material LogicalParent => _Material;

        public String Key => _Key;

        /// <summary>
        /// Gets a value indicating whether this channel supports a Color factor.
        /// </summary>
        public bool IsColorSupported { get; private set; }

        public Vector4 Color
        {
            get => _ColorGetter();
            set => _ColorSetter(value);
        }

        /// <summary>
        /// Gets a value indicating whether this channel supports textures.
        /// </summary>
        public bool IsTextureSupported => _TextureInfo != null;

        /// <summary>
        /// Gets the <see cref="Texture"/> instance used by this Material, or null.
        /// </summary>
        public Texture Texture => _GetTexture();

        /// <summary>
        /// Gets the index of texture's TEXCOORD_[index] attribute used for texture coordinate mapping.
        /// </summary>
        public int TextureCoordinate => _TextureInfo(false)?.TextureCoordinate ?? 0;

        public TextureTransform TextureTransform => _TextureInfo(false)?.Transform;

        public TextureSampler TextureSampler => Texture?.Sampler;

        /// <summary>
        /// Gets a value indicating whether this channel supports texture amount factor.
        /// </summary>
        public bool IsTextureAmountSupported { get; private set; }

        /// <summary>
        /// Gets or sets the Texture weight in the final shader.
        /// </summary>
        /// <remarks>
        /// Not all channels support this property.
        /// </remarks>
        public Single TextureAmount
        {
            get => _AmountGetter();
            set => _AmountSetter(value);
        }

        #endregion

        #region API

        private Texture _GetTexture()
        {
            var texInfo = _TextureInfo?.Invoke(false);
            if (texInfo == null) return null;

            return _Material.LogicalParent.LogicalTextures[texInfo._LogicalTextureIndex];
        }

        public void SetTexture(
            int texCoord,
            Image texImg,
            TextureMipMapFilter min = TextureMipMapFilter.DEFAULT,
            TextureInterpolationFilter mag = TextureInterpolationFilter.DEFAULT,
            TextureWrapMode ws = TextureWrapMode.REPEAT,
            TextureWrapMode wt = TextureWrapMode.REPEAT)
        {
            if (texImg == null) return; // in theory, we should completely remove the TextureInfo

            if (_Material == null) throw new InvalidOperationException();

            var sampler = _Material.LogicalParent.UseSampler(min, mag, ws, wt);
            var texture = _Material.LogicalParent.UseTexture(texImg, sampler);

            SetTexture(texCoord, texture);
        }

        public void SetTexture(int texSet, Texture tex)
        {
            Guard.NotNull(tex, nameof(tex));
            Guard.MustShareLogicalParent(_Material, tex, nameof(tex));

            if (_TextureInfo == null) throw new InvalidOperationException();

            var texInfo = _TextureInfo(true);

            texInfo.TextureCoordinate = texSet;
            texInfo._LogicalTextureIndex = tex.LogicalIndex;
        }

        public void SetTransform(Vector2 offset, Vector2 scale, float rotation = 0, int? texCoordOverride = null)
        {
            if (_TextureInfo == null) throw new InvalidOperationException();

            var texInfo = _TextureInfo(true);

            texInfo.SetTransform(offset, scale, rotation, texCoordOverride);
        }

        #endregion
    }
}
