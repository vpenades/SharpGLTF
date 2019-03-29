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
        public Single AlphaCutoff
        {
            get => (Single)_alphaCutoff.AsValue(_alphaCutoffDefault);
            set => _alphaCutoff = ((Double)value).AsNullable(_alphaCutoffDefault, _alphaCutoffMinimum, double.MaxValue);
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
        /// Gets a collection of <see cref="MaterialChannel"/> elements available in this <see cref="Material"/> instance.
        /// </summary>
        public IEnumerable<MaterialChannel> Channels => _GetChannels();

        #endregion

        #region API

        /// <inheritdoc />
        protected override IEnumerable<ExtraProperties> GetLogicalChildren()
        {
            return base.GetLogicalChildren().ConcatItems(_normalTexture, _emissiveTexture, _occlusionTexture, _pbrMetallicRoughness);
        }

        /// <summary>
        /// Finds an instance of <see cref="MaterialChannel"/>
        /// </summary>
        /// <param name="channelKey">the channel key</param>
        /// <returns>A <see cref="MaterialChannel"/> structure. or null if it does not exist</returns>
        public MaterialChannel? FindChannel(string channelKey)
        {
            foreach (var ch in Channels)
            {
                if (ch.Key.Equals(channelKey, StringComparison.OrdinalIgnoreCase)) return ch;
            }

            return null;
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
}
