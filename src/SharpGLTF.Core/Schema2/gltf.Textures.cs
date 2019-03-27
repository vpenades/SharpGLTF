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
        }

        #endregion

        #region API

        public void SetTransform(int texCoord, Vector2 offset, Vector2 scale, float rotation)
        {
            var xform = new TextureTransform(this)
            {
                TextureCoordinate = texCoord,
                Offset = offset,
                Scale = scale,
                Rotation = rotation
            };

            if (xform.IsDefault) this.RemoveExtensions<TextureTransform>();
            else this.SetExtension(xform);
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

        public TextureSampler Sampler
        {
            get => _sampler.HasValue ? LogicalParent.LogicalTextureSamplers[_sampler.Value] : null;
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

    [System.Diagnostics.DebuggerDisplay("TextureSampler[{LogicalIndex}] {Name}")]
    public sealed partial class TextureSampler
    {
        #region lifecycle

        internal TextureSampler() { }

        internal TextureSampler(TextureInterpolationMode mag, TextureMipMapMode min, TextureWrapMode ws, TextureWrapMode wt)
        {
            _magFilter = mag;
            _minFilter = min;
            _wrapS = ws;
            _wrapT = wt;
        }

        #endregion

        #region properties

        /// <summary>
        /// Gets the zero-based index of this <see cref="TextureSampler"/> at <see cref="ModelRoot.LogicalTextureSamplers"/>
        /// </summary>
        public int LogicalIndex => this.LogicalParent.LogicalTextureSamplers.IndexOfReference(this);

        public TextureInterpolationMode MagFilter => _magFilter.AsValue(TextureInterpolationMode.LINEAR);

        public TextureMipMapMode MinFilter => _minFilter.AsValue(TextureMipMapMode.LINEAR);

        public TextureWrapMode WrapS => _wrapS.AsValue(_wrapSDefault);

        public TextureWrapMode WrapT => _wrapT.AsValue(_wrapTDefault);

        #endregion
    }

    [System.Diagnostics.DebuggerDisplay("TextureTransform {TextureCoordinate} {Offset} {Scale} {Rotation}")]
    public partial class TextureTransform
    {
        #region lifecycle

        internal TextureTransform(TextureInfo parent) { }

        #endregion

        #region properties

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
            set => _texCoord = value.AsNullable(_texCoordMinimum, _texCoordMinimum, int.MaxValue);
        }

        internal bool IsDefault
        {
            get
            {
                if (_texCoord.HasValue) return false;
                if (_offset.HasValue) return false;
                if (_scale.HasValue) return false;
                if (_rotation.HasValue) return false;
                return true;
            }
        }

        #endregion
    }

    public partial class ModelRoot
    {
        /// <summary>
        /// Creates or reuses a <see cref="TextureSampler"/> instance
        /// at <see cref="ModelRoot.LogicalTextureSamplers"/>.
        /// </summary>
        /// <param name="mag">A value of <see cref="TextureInterpolationMode"/>.</param>
        /// <param name="min">A value of <see cref="TextureMipMapMode"/>.</param>
        /// <param name="ws">The <see cref="TextureWrapMode"/> in the S axis.</param>
        /// <param name="wt">The <see cref="TextureWrapMode"/> in the T axis.</param>
        /// <returns>A <see cref="TextureSampler"/> instance.</returns>
        public TextureSampler UseSampler(TextureInterpolationMode mag, TextureMipMapMode min, TextureWrapMode ws, TextureWrapMode wt)
        {
            foreach (var s in this._samplers)
            {
                if (s.MagFilter == mag && s.MinFilter == min && s.WrapS == ws && s.WrapT == wt) return s;
            }

            var ss = new TextureSampler(mag, min, ws, wt);

            this._samplers.Add(ss);

            return ss;
        }

        /// <summary>
        /// Creates or reuses a <see cref="Texture"/> instance
        /// at <see cref="ModelRoot.LogicalTextures"/>.
        /// </summary>
        /// <param name="image">The source <see cref="Image"/>.</param>
        /// <param name="sampler">The source <see cref="TextureSampler"/>.</param>
        /// <returns>A <see cref="Texture"/> instance.</returns>
        public Texture UseTexture(Image image, TextureSampler sampler)
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

        internal T _UseTextureInfo<T>(Image image, TextureSampler sampler, int textureSet)
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
