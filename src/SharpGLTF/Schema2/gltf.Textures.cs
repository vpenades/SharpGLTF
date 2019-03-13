using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;

namespace SharpGLTF.Schema2
{
    [System.Diagnostics.DebuggerDisplay("Texture[{LogicalIndex}] {Name}")]
    internal partial class TextureInfo
    {
        #region properties

        internal int _LogicalTextureIndex
        {
            get => _index;
            set => _index = value;
        }

        public int TextureSet
        {
            get => _texCoord ?? _texCoordDefault;
            set => _texCoord = value.AsNullable(_texCoordDefault, _texCoordMinimum, int.MaxValue);
        }

        public TextureTransform Transform
        {
            get => this.GetExtension<TextureTransform>();
            set
            {
                if (value == null) { this.RemoveExtensions<TextureTransform>(); return; }

                this.SetExtension(value);
            }
        }

        #endregion
    }

    [System.Diagnostics.DebuggerDisplay("Normal Texture[{LogicalIndex}] {Name}")]
    internal sealed partial class MaterialNormalTextureInfo
    {
        #region properties

        public Double Scale
        {
            get => this._scale.AsValue(_scaleDefault);
            set => this._scale = value.AsNullable(_scaleDefault);
        }

        #endregion
    }

    [System.Diagnostics.DebuggerDisplay("Occlusion Texture[{LogicalIndex}] {Name}")]
    internal sealed partial class MaterialOcclusionTextureInfo
    {
        #region properties

        public Double Strength
        {
            get => this._strength ?? _strengthDefault;
            set => this._strength = value.AsNullable(_strengthDefault, _strengthMinimum, _strengthMaximum);
        }

        #endregion
    }

    [System.Diagnostics.DebuggerDisplay("Texture[{LogicalIndex}] {Name}")]
    public sealed partial class Texture
    {
        #region lifecycle

        internal Texture() { }

        #endregion

        #region properties

        /// <summary>
        /// Gets the zero-based index of this <see cref="Texture"/> at <see cref="ModelRoot.LogicalTextures"/>
        /// </summary>
        public int LogicalIndex => this.LogicalParent.LogicalTextures.IndexOfReference(this);

        public Sampler Sampler
        {
            get => _sampler.HasValue ? LogicalParent.LogicalSamplers[_sampler.Value] : null;
            set
            {
                if (value != null) Guard.MustShareLogicalParent(this, value, nameof(value));
                _sampler = value?.LogicalIndex;
            }
        }

        public Image Source
        {
            get => _source.HasValue ? LogicalParent.LogicalImages[_source.Value] : null;
            set
            {
                if (value != null) Guard.MustShareLogicalParent(this, value, nameof(value));
                _source = value?.LogicalIndex;
            }
        }

        #endregion
    }

    [System.Diagnostics.DebuggerDisplay("Sampler[{LogicalIndex}] {Name}")]
    public sealed partial class Sampler
    {
        #region lifecycle

        internal Sampler() { }

        internal Sampler(TextureInterpolationMode mag, TextureMipMapMode min, TextureWrapMode ws, TextureWrapMode wt)
        {
            _magFilter = mag;
            _minFilter = min;
            _wrapS = ws;
            _wrapT = wt;
        }

        #endregion

        #region properties

        /// <summary>
        /// Gets the zero-based index of this <see cref="Sampler"/> at <see cref="ModelRoot.LogicalSamplers"/>
        /// </summary>
        public int LogicalIndex => this.LogicalParent.LogicalSamplers.IndexOfReference(this);

        public TextureInterpolationMode MagFilter => _magFilter ?? TextureInterpolationMode.LINEAR;

        public TextureMipMapMode MinFilter => _minFilter ?? TextureMipMapMode.LINEAR;

        public TextureWrapMode WrapS => _wrapS ?? _wrapSDefault;

        public TextureWrapMode WrapT => _wrapT ?? _wrapSDefault;

        #endregion
    }

    public partial class TextureTransform
    {
        internal TextureTransform(TextureInfo parent) { }

        public Vector2 Offset
        {
            get => _offset.AsValue(_offsetDefault);
            set => _offset = value.AsNullable(_offsetDefault);
        }

        public Vector2 Scale
        {
            get => _scale.AsValue(_scaleDefault);
            set => _scale = value.AsNullable(_scaleDefault);
        }

        public float Rotation
        {
            get => (float)_rotation.AsValue(_rotationDefault);
            set => _rotation = ((double)value).AsNullable(_rotationDefault);
        }

        public int TextureCoordinate
        {
            get => _texCoord.AsValue(_texCoordMinimum);
            set => _texCoord = value.AsNullable(_texCoordMinimum);
        }
    }

    public partial class ModelRoot
    {
        /// <summary>
        /// Creates or reuses a <see cref="Sampler"/> instance
        /// at <see cref="ModelRoot.LogicalSamplers"/>.
        /// </summary>
        /// <param name="mag">A value of <see cref="TextureInterpolationMode"/>.</param>
        /// <param name="min">A value of <see cref="TextureMipMapMode"/>.</param>
        /// <param name="ws">The <see cref="TextureWrapMode"/> in the S axis.</param>
        /// <param name="wt">The <see cref="TextureWrapMode"/> in the T axis.</param>
        /// <returns>A <see cref="Sampler"/> instance.</returns>
        public Sampler UseSampler(TextureInterpolationMode mag, TextureMipMapMode min, TextureWrapMode ws, TextureWrapMode wt)
        {
            foreach (var s in this._samplers)
            {
                if (s.MagFilter == mag && s.MinFilter == min && s.WrapS == ws && s.WrapT == wt) return s;
            }

            var ss = new Sampler(mag, min, ws, wt);

            this._samplers.Add(ss);

            return ss;
        }

        /// <summary>
        /// Creates or reuses a <see cref="Texture"/> instance
        /// at <see cref="ModelRoot.LogicalTextures"/>.
        /// </summary>
        /// <param name="image">The source <see cref="Image"/>.</param>
        /// <param name="sampler">The source <see cref="Sampler"/>.</param>
        /// <returns>A <see cref="Texture"/> instance.</returns>
        public Texture UseTexture(Image image, Sampler sampler)
        {
            if (image == null) return null;

            if (image != null) Guard.MustShareLogicalParent(this, image, nameof(image));
            if (sampler != null) Guard.MustShareLogicalParent(this, sampler, nameof(sampler));

            var tex = _textures.FirstOrDefault(item => item.Source == image && item.Sampler == sampler);
            if (tex != null) return tex;

            tex = new Texture();
            _textures.Add(tex);

            tex.Source = image;
            tex.Sampler = sampler;

            return tex;
        }

        internal T _UseTextureInfo<T>(Image image, Sampler sampler, int textureSet)
            where T : TextureInfo, new()
        {
            var tex = UseTexture(image, sampler);
            if (tex == null) return null;

            return new T
            {
                _LogicalTextureIndex = tex.LogicalIndex,
                TextureSet = textureSet
            };
        }
    }
}
