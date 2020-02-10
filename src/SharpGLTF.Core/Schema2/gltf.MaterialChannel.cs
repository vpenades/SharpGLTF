using System;
using System.Numerics;

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
    public readonly struct MaterialChannel
    {
        #region lifecycle

        internal MaterialChannel(Material m, String key, Func<Boolean, TextureInfo> texInfo, Single defval, Func<Single> cgetter, Action<Single> csetter)
            : this(m, key, texInfo)
        {
            Guard.NotNull(cgetter, nameof(cgetter));
            Guard.NotNull(csetter, nameof(csetter));

            _ParameterDefVal = new Vector4(defval, 0, 0, 0);
            _ParameterGetter = () => new Vector4(cgetter(), 0, 0, 0);
            _ParameterSetter = value => csetter(value.X);
        }

        internal MaterialChannel(Material m, String key, Func<Boolean, TextureInfo> texInfo, Vector4 defval, Func<Vector4> cgetter, Action<Vector4> csetter)
            : this(m, key, texInfo)
        {
            Guard.NotNull(cgetter, nameof(cgetter));
            Guard.NotNull(csetter, nameof(csetter));

            _ParameterDefVal = defval;
            _ParameterGetter = cgetter;
            _ParameterSetter = csetter;
        }

        private MaterialChannel(Material m, String key, Func<Boolean, TextureInfo> texInfo)
        {
            Guard.NotNull(m, nameof(m));
            Guard.NotNullOrEmpty(key, nameof(key));

            Guard.NotNull(texInfo, nameof(texInfo));

            _Key = key;
            _Material = m;

            _TextureInfo = texInfo;

            _ParameterDefVal = Vector4.Zero;
            _ParameterGetter = null;
            _ParameterSetter = null;
        }

        #endregion

        #region data

        private readonly String _Key;
        private readonly Material _Material;

        private readonly Func<Boolean, TextureInfo> _TextureInfo;

        private readonly Vector4 _ParameterDefVal;
        private readonly Func<Vector4> _ParameterGetter;
        private readonly Action<Vector4> _ParameterSetter;

        public override int GetHashCode()
        {
            if (_Key == null) return 0;

            return _Key.GetHashCode() ^ _Material.GetHashCode();
        }

        #endregion

        #region properties

        public Material LogicalParent => _Material;

        public String Key => _Key;

        public Boolean HasDefaultContent => _CheckHasDefaultContent();

        /// <summary>
        /// Gets or sets the <see cref="Vector4"/> parameter of this channel.
        /// The meaning of the <see cref="Vector4.X"/>, <see cref="Vector4.Y"/>. <see cref="Vector4.Z"/> and <see cref="Vector4.W"/>
        /// depend on the type of channel.
        /// </summary>
        public Vector4 Parameter
        {
            get => _ParameterGetter();
            set => _ParameterSetter(value);
        }

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
            Image primaryImg,
            Image fallbackImg = null,
            TextureWrapMode ws = TextureWrapMode.REPEAT,
            TextureWrapMode wt = TextureWrapMode.REPEAT,
            TextureMipMapFilter min = TextureMipMapFilter.DEFAULT,
            TextureInterpolationFilter mag = TextureInterpolationFilter.DEFAULT)
        {
            if (primaryImg == null) return; // in theory, we should completely remove the TextureInfo

            Guard.NotNull(_Material, nameof(_Material));

            var sampler = _Material.LogicalParent.UseTextureSampler(ws, wt, min, mag);
            var texture = _Material.LogicalParent.UseTexture(primaryImg, fallbackImg, sampler);

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

        private bool _CheckHasDefaultContent()
        {
            if (this.Parameter != _ParameterDefVal) return false;
            if (this.Texture != null) return false;

            // there's no point in keep checking because if there's no texture, all other elements are irrelevant.

            return true;
        }

        #endregion
    }
}
