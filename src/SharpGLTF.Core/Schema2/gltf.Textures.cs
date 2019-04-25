using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;

namespace SharpGLTF.Schema2
{
    using TEXLERP = TextureInterpolationFilter;
    using TEXMIPMAP = TextureMipMapFilter;
    using TEXWRAP = TextureWrapMode;

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

        [Obsolete("Use PrimaryImage")]
        public Image Image
        {
            get => PrimaryImage;
            set
            {
                if (value == null) ClearImages();
                else SetImage(value);
            }
        }

        public Image PrimaryImage => _GetPrimaryImage();

        public Image FallbackImage => _GetFallbackImage();

        #endregion

        #region API

        private Image _GetPrimaryImage()
        {
            var ddstex = this.GetExtension<MSFTTextureDDS>();
            if (ddstex != null) return ddstex.Image;

            return _source.HasValue? LogicalParent.LogicalImages[_source.Value] : null;
        }

        private Image _GetFallbackImage()
        {
            var ddstex = this.GetExtension<MSFTTextureDDS>();
            if (ddstex == null) return null;

            return _source.HasValue ? LogicalParent.LogicalImages[_source.Value] : null;
        }

        public void ClearImages()
        {
            _source = null;
            this.RemoveExtensions<MSFTTextureDDS>();
        }

        public void SetImage(Image primaryImage)
        {
            Guard.MustShareLogicalParent(this, primaryImage, nameof(primaryImage));

            if (primaryImage.IsDds)
            {
                _source = null;
                _UseDDSTexture().Image = primaryImage;
            }
            else
            {
                this.RemoveExtensions<MSFTTextureDDS>();
                _source = primaryImage.LogicalIndex;
            }
        }

        public void SetImages(Image primaryImage, Image fallbackImage)
        {
            Guard.MustShareLogicalParent(this, primaryImage, nameof(primaryImage));
            Guard.MustShareLogicalParent(this, fallbackImage, nameof(fallbackImage));
            Guard.IsTrue(primaryImage.IsDds, "primary image must be DDS");
            Guard.IsTrue(fallbackImage.IsJpeg || fallbackImage.IsPng, nameof(fallbackImage), "fallback image must be PNG or JPEG");

            _UseDDSTexture().Image = primaryImage;

            _source = fallbackImage.LogicalIndex;
        }

        private MSFTTextureDDS _UseDDSTexture()
        {
            var primary = this.GetExtension<MSFTTextureDDS>();
            if (primary == null) { primary = new MSFTTextureDDS(this); this.SetExtension(primary); }

            return primary;
        }

        internal bool _IsEqualentTo(Image primary, Image fallback, TextureSampler sampler)
        {
            if (primary != this.PrimaryImage) return false;
            if (fallback != this.FallbackImage) return false;
            if (sampler != this.Sampler) return false;

            return true;
        }

        #endregion
    }

    partial class MSFTTextureDDS
    {
        internal MSFTTextureDDS(Texture parent)
        {
            _Parent = parent;
        }

        private readonly Texture _Parent;

        public Image Image
        {
            get => _source.HasValue ? _Parent.LogicalParent.LogicalImages[_source.Value] : null;
            set
            {
                if (value != null) Guard.MustShareLogicalParent(_Parent, value, nameof(value));
                _source = value?.LogicalIndex;
            }
        }
    }

    [System.Diagnostics.DebuggerDisplay("TextureSampler[{LogicalIndex}] {Name}")]
    public sealed partial class TextureSampler
    {
        #region lifecycle

        internal TextureSampler() { }

        internal TextureSampler(TEXMIPMAP min, TEXLERP mag, TEXWRAP ws, TEXWRAP wt)
        {
            _magFilter = mag.AsNullable(TEXLERP.DEFAULT);
            _minFilter = min.AsNullable(TEXMIPMAP.DEFAULT);
            _wrapS = ws.AsNullable(_wrapSDefault);
            _wrapT = wt.AsNullable(_wrapTDefault);
        }

        #endregion

        #region properties

        /// <summary>
        /// Gets the zero-based index of this <see cref="TextureSampler"/> at <see cref="ModelRoot.LogicalTextureSamplers"/>
        /// </summary>
        public int LogicalIndex => this.LogicalParent.LogicalTextureSamplers.IndexOfReference(this);

        public TEXMIPMAP MinFilter => _minFilter.AsValue(TEXMIPMAP.DEFAULT);

        public TEXLERP MagFilter => _magFilter.AsValue(TEXLERP.DEFAULT);

        public TEXWRAP WrapS => _wrapS.AsValue(_wrapSDefault);

        public TEXWRAP WrapT => _wrapT.AsValue(_wrapTDefault);

        #endregion

        #region API

        internal static bool IsDefault(TEXMIPMAP min, TEXLERP mag, TEXWRAP ws, TEXWRAP wt)
        {
            if (min != TEXMIPMAP.DEFAULT) return false;
            if (mag != TEXLERP.DEFAULT) return false;
            if (ws != _wrapSDefault) return false;
            if (wt != _wrapTDefault) return false;
            return true;
        }

        #endregion
    }

    public partial class ModelRoot
    {
        /// <summary>
        /// Creates or reuses a <see cref="TextureSampler"/> instance
        /// at <see cref="ModelRoot.LogicalTextureSamplers"/>.
        /// </summary>
        /// <param name="ws">The <see cref="TEXWRAP"/> in the S axis.</param>
        /// <param name="wt">The <see cref="TEXWRAP"/> in the T axis.</param>
        /// <param name="min">A value of <see cref="TEXMIPMAP"/>.</param>
        /// <param name="mag">A value of <see cref="TEXLERP"/>.</param>
        /// <returns>A <see cref="TextureSampler"/> instance, or null if all the arguments are default values.</returns>
        public TextureSampler UseTextureSampler(TEXWRAP ws, TEXWRAP wt, TEXMIPMAP min, TEXLERP mag)
        {
            if (TextureSampler.IsDefault(min, mag, ws, wt)) return null;

            foreach (var s in this._samplers)
            {
                if (s.MinFilter == min && s.MagFilter == mag && s.WrapS == ws && s.WrapT == wt) return s;
            }

            var ss = new TextureSampler(min, mag, ws, wt);

            this._samplers.Add(ss);

            return ss;
        }

        /// <summary>
        /// Creates or reuses a <see cref="Texture"/> instance
        /// at <see cref="ModelRoot.LogicalTextures"/>.
        /// </summary>
        /// <param name="primary">The source <see cref="Image"/>.</param>
        /// <param name="sampler">The source <see cref="TextureSampler"/>.</param>
        /// <returns>A <see cref="Texture"/> instance.</returns>
        public Texture UseTexture(Image primary, TextureSampler sampler)
        {
            return UseTexture(primary, null, sampler);
        }

        /// <summary>
        /// Creates or reuses a <see cref="Texture"/> instance
        /// at <see cref="ModelRoot.LogicalTextures"/>.
        /// </summary>
        /// <param name="primary">The source <see cref="Image"/>.</param>
        /// <param name="fallback">The source <see cref="Image"/>.</param>
        /// <param name="sampler">The source <see cref="TextureSampler"/>.</param>
        /// <returns>A <see cref="Texture"/> instance.</returns>
        public Texture UseTexture(Image primary, Image fallback, TextureSampler sampler)
        {
            // if primary image is null, there's no point in creating a texture.
            if (primary == null)
            {
                Guard.MustBeNull(fallback, nameof(fallback));
                return null;
            }

            if (primary  != null) Guard.MustShareLogicalParent(this, primary, nameof(primary));
            if (fallback != null) Guard.MustShareLogicalParent(this, fallback, nameof(primary));
            if (sampler  != null) Guard.MustShareLogicalParent(this, sampler, nameof(sampler));

            // find if we have an equivalent texture
            var tex = _textures.FirstOrDefault(item => item._IsEqualentTo(primary, fallback, sampler));
            if (tex != null) return tex;

            // create a new texture
            tex = new Texture();
            _textures.Add(tex);

            if (fallback == null) tex.SetImage(primary);
            else tex.SetImages(primary, fallback);

            tex.Sampler = sampler;

            return tex;
        }
    }
}
