using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;

namespace SharpGLTF.Schema2
{
    [System.Diagnostics.DebuggerDisplay("Material[{LogicalIndex}] {Name}")]
    public sealed partial class Material
    {
        #region lifecycle

        internal Material() { }

        #endregion

        #region properties

        public int LogicalIndex => this.LogicalParent.LogicalMaterials.IndexOfReference(this);

        public AlphaMode Alpha
        {
            get => _alphaMode.AsValue(_alphaModeDefault);
            set => _alphaMode = value.AsNullable(_alphaModeDefault);
        }

        public Double AlphaCutoff
        {
            get => _alphaCutoff.AsValue(_alphaCutoffDefault);
            set => _alphaCutoff = value.AsNullable(_alphaCutoffDefault, _alphaCutoffMinimum, double.MaxValue);
        }

        public Boolean DoubleSided
        {
            get => _doubleSided.AsValue(_doubleSidedDefault);
            set => _doubleSided = value.AsNullable(_doubleSidedDefault);
        }

        public Boolean Unlit => this.GetExtension<MaterialUnlit_KHR>() != null;

        /// <summary>
        /// Gets a collection of channel views available for this material.
        /// </summary>
        public IEnumerable<MaterialChannelView> Channels => _GetChannels();

        #endregion

        #region API

        /// <summary>
        /// Returns an object that allows to read and write information of a given channel of the material.
        /// </summary>
        /// <param name="key">the channel key</param>
        /// <returns>the channel view</returns>
        public MaterialChannelView GetChannel(string key)
        {
            return Channels.FirstOrDefault(item => item.Key == key);
        }

        private MaterialNormalTextureInfo _GetNormalTexture(bool create)
        {
            if (create && _normalTexture == null) _normalTexture = new MaterialNormalTextureInfo();
            return _normalTexture;
        }

        private MaterialOcclusionTextureInfo _GetOcclusionTexture(bool create)
        {
            if (create && _occlusionTexture == null) _occlusionTexture = new MaterialOcclusionTextureInfo();
            return _occlusionTexture;
        }

        private TextureInfo _GetEmissiveTexture(bool create)
        {
            if (create && _emissiveTexture == null) _emissiveTexture = new TextureInfo();
            return _emissiveTexture;
        }

        #endregion
    }

    [System.Diagnostics.DebuggerDisplay("Channel {_Key}")]
    public struct MaterialChannelView
    {
        #region lifecycle

        internal MaterialChannelView(Material m, String key, Func<Boolean, TextureInfo> texInfo, Func<Vector4> fg, Action<Vector4> fs)
        {
            _Key = key;
            _Material = m;
            _TextureInfoGetter = texInfo;
            _FactorGetter = fg;
            _FactorSetter = fs;
        }

        #endregion

        #region data

        private readonly String _Key;
        private readonly Material _Material;

        private readonly Func<Boolean, TextureInfo> _TextureInfoGetter;

        private readonly Func<Vector4> _FactorGetter;
        private readonly Action<Vector4> _FactorSetter;

        #endregion

        #region properties

        public String Key => _Key;

        public Texture Texture => _TextureInfoGetter?.Invoke(false) == null ? null : _Material.LogicalParent.LogicalTextures[_TextureInfoGetter(false)._LogicalTextureIndex];

        public int Set => _TextureInfoGetter?.Invoke(false) == null ? 0 : _TextureInfoGetter(false).TextureSet;

        public Image Image => Texture?.Source;

        public Sampler Sampler => Texture?.Sampler;

        public Vector4 Factor => _FactorGetter();

        #endregion

        #region fluent API

        public void SetFactor(Vector4 value) { _FactorSetter?.Invoke(value); }

        public void SetTexture(
            int texSet,
            Image texImg,
            TextureInterpolationMode mag = TextureInterpolationMode.LINEAR,
            TextureMipMapMode min = TextureMipMapMode.LINEAR_MIPMAP_LINEAR,
            TextureWrapMode ws = TextureWrapMode.REPEAT,
            TextureWrapMode wt = TextureWrapMode.REPEAT)
        {
            if (texImg == null) return; // in theory, we should completely remove the TextureInfo

            var sampler = _Material.LogicalParent.UseSampler(mag, min, ws, wt);
            var texture = _Material.LogicalParent.UseTexture(texImg, sampler);

            SetTexture(texSet, texture);
        }

        public void SetTexture(int texSet, Texture tex)
        {
            Guard.NotNull(tex, nameof(tex));
            Guard.MustShareLogicalParent(_Material, tex, nameof(tex));

            var texInfo = _TextureInfoGetter(true);

            texInfo.TextureSet = texSet;
            texInfo._LogicalTextureIndex = tex.LogicalIndex;
        }

        #endregion
    }
}
