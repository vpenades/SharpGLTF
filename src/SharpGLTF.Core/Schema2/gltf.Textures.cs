using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;

namespace SharpGLTF.Schema2
{
    using SharpGLTF.Validation;
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
            var ddsimg = this.GetExtension<TextureDDS>()?.Image;
            if (ddsimg != null) return ddsimg;

            var wbpimg = this.GetExtension<TextureWEBP>()?.Image;
            if (wbpimg != null) return wbpimg;

            return _source.HasValue ? LogicalParent.LogicalImages[_source.Value] : null;
        }

        private Image _GetFallbackImage()
        {
            var img = _source.HasValue ? LogicalParent.LogicalImages[_source.Value] : null;

            // if the default image is provided by _GetPrimaryImage() we don't need to return anything here.
            return _GetPrimaryImage() == img ? null : img;
        }

        public void ClearImages()
        {
            _source = null;
            this.RemoveExtensions<TextureDDS>();
            this.RemoveExtensions<TextureWEBP>();
        }

        public void SetImage(Image primaryImage)
        {
            Guard.NotNull(primaryImage, nameof(primaryImage));
            Guard.MustShareLogicalParent(this, primaryImage, nameof(primaryImage));

            if (primaryImage.Content.IsDds || primaryImage.Content.IsWebp)
            {
                var fallback = LogicalParent.UseImage(Memory.MemoryImage.DefaultPngImage.Slice(0));
                SetImages(primaryImage, fallback);
            }
            else
            {
                ClearImages();
                _source = primaryImage.LogicalIndex;
            }
        }

        public void SetImages(Image primaryImage, Image fallbackImage)
        {
            Guard.NotNull(primaryImage, nameof(primaryImage));
            Guard.NotNull(fallbackImage, nameof(fallbackImage));
            Guard.MustShareLogicalParent(this, primaryImage, nameof(primaryImage));
            Guard.MustShareLogicalParent(this, fallbackImage, nameof(fallbackImage));
            Guard.IsTrue(primaryImage.Content.IsDds || primaryImage.Content.IsWebp, "Primary image must be DDS or WEBP");
            Guard.IsTrue(fallbackImage.Content.IsJpg || fallbackImage.Content.IsPng, nameof(fallbackImage), "Fallback image must be PNG or JPEG");

            ClearImages();

            if (primaryImage.Content.IsDds)
            {
                _UseDDSTexture().Image = primaryImage;
            }

            if (primaryImage.Content.IsWebp)
            {
                _UseWEBPTexture().Image = primaryImage;
            }

            _source = fallbackImage.LogicalIndex;
        }

        private TextureDDS _UseDDSTexture()
        {
            var primary = this.GetExtension<TextureDDS>();
            if (primary == null) { primary = new TextureDDS(this); this.SetExtension(primary); }

            return primary;
        }

        private TextureWEBP _UseWEBPTexture()
        {
            var primary = this.GetExtension<TextureWEBP>();
            if (primary == null) { primary = new TextureWEBP(this); this.SetExtension(primary); }

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

        #region Validation

        protected override void OnValidateReferences(ValidationContext validate)
        {
            base.OnValidateReferences(validate);

            validate
                .IsNullOrIndex("Source", _source, this.LogicalParent.LogicalImages)
                .IsNullOrIndex("Sampler", _sampler, this.LogicalParent.LogicalTextureSamplers);
        }

        #endregion
    }

    partial class TextureDDS
    {
        internal TextureDDS(Texture parent)
        {
            _Parent = parent;
        }

        private readonly Texture _Parent;

        public Image Image
        {
            get => _source.HasValue ? _Parent.LogicalParent.LogicalImages[_source.Value] : null;
            set
            {
                if (value != null)
                {
                    Guard.MustShareLogicalParent(_Parent, value, nameof(value));
                    Guard.IsTrue(value.Content.IsDds, nameof(value));
                }

                _source = value?.LogicalIndex;
            }
        }
    }

    partial class TextureWEBP
    {
        internal TextureWEBP(Texture parent)
        {
            _Parent = parent;
        }

        private readonly Texture _Parent;

        public Image Image
        {
            get => _source.HasValue ? _Parent.LogicalParent.LogicalImages[_source.Value] : null;
            set
            {
                if (value != null)
                {
                    Guard.MustShareLogicalParent(_Parent, value, nameof(value));
                    Guard.IsTrue(value.Content.IsWebp, nameof(value));
                }

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
        public Texture UseTexture(Image primary, TextureSampler sampler = null)
        {
            // if primary image is null, there's no point in creating a texture.
            if (primary == null) return null;

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
        public Texture UseTexture(Image primary, Image fallback, TextureSampler sampler = null)
        {
            // if primary image is null, there's no point in creating a texture.
            if (primary == null)
            {
                Guard.MustBeNull(fallback, nameof(fallback));
                return null;
            }

            if (primary  != null) Guard.MustShareLogicalParent(this, "this", primary, nameof(primary));
            if (fallback != null) Guard.MustShareLogicalParent(this, "this", fallback, nameof(primary));
            if (sampler  != null) Guard.MustShareLogicalParent(this, "this", sampler, nameof(sampler));

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
