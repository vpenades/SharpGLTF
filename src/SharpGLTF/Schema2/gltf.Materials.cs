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

        /// <summary>
        /// Gets the zero-based index of this <see cref="Material"/> at <see cref="ModelRoot.LogicalMaterials"/>
        /// </summary>
        public int LogicalIndex => this.LogicalParent.LogicalMaterials.IndexOfReference(this);

        /// <summary>
        /// Gets or sets the <see cref="AlphaMode"/> of this <see cref="Material"/> instance.
        /// </summary>
        public AlphaMode Alpha
        {
            get => _alphaMode.AsValue(_alphaModeDefault);
            set => _alphaMode = value.AsNullable(_alphaModeDefault);
        }

        /// <summary>
        /// Gets or sets the <see cref="AlphaCutoff"/> of this <see cref="Material"/> instance.
        /// </summary>
        public Double AlphaCutoff
        {
            get => _alphaCutoff.AsValue(_alphaCutoffDefault);
            set => _alphaCutoff = value.AsNullable(_alphaCutoffDefault, _alphaCutoffMinimum, double.MaxValue);
        }

        /// <summary>
        /// Gets or sets a value indicating whether this <see cref="Material"/> will render as Double Sided.
        /// </summary>
        public Boolean DoubleSided
        {
            get => _doubleSided.AsValue(_doubleSidedDefault);
            set => _doubleSided = value.AsNullable(_doubleSidedDefault);
        }

        /// <summary>
        /// Gets a value indicating whether this <see cref="Material"/> instance has Unlit extension.
        /// </summary>
        public Boolean Unlit => this.GetExtension<MaterialUnlit>() != null;

        /// <summary>
        /// Gets a collection of <see cref="MaterialChannelView"/> elements available in this <see cref="Material"/> instance.
        /// </summary>
        public IEnumerable<MaterialChannelView> Channels => _GetChannels();

        #endregion

        #region API

        /// <inheritdoc />
        protected override IEnumerable<ExtraProperties> GetLogicalChildren()
        {
            return base.GetLogicalChildren().Concat(_normalTexture, _emissiveTexture, _occlusionTexture, _pbrMetallicRoughness);
        }

        /// <summary>
        /// Finds an instance of <see cref="MaterialChannelView"/>
        /// </summary>
        /// <param name="key">the channel key</param>
        /// <returns>A <see cref="MaterialChannelView"/> instance, or null if <paramref name="key"/> does not exist.</returns>
        public MaterialChannelView FindChannel(string key)
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

        public int Set => _TextureInfoGetter?.Invoke(false)?.TextureSet ?? 0;

        public Image Image => Texture?.Source;

        public TextureSampler Sampler => Texture?.Sampler;

        public Vector4 Factor => _FactorGetter();

        public TextureTransform Transform => _TextureInfoGetter?.Invoke(false)?.Transform;

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
