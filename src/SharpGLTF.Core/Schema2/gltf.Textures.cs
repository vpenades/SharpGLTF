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

        public TextureSampler Sampler
        {
            get => _sampler.HasValue ? LogicalParent.LogicalTextureSamplers[_sampler.Value] : null;
            set
            {
                if (value != null) Guard.MustShareLogicalParent(this, value, nameof(value));
                _sampler = value?.LogicalIndex;
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

            var xnbimg = this.GetExtension<TextureXNB>()?.Image;
            if (xnbimg != null) return xnbimg;

            var webpimg = this.GetExtension<TextureWEBP>()?.Image;
            if (webpimg != null) return webpimg;

            var ktximg = this.GetExtension<TextureKTX2>()?.Image;
            if (ktximg != null) return ktximg;

            var astcimg = this.GetExtension<TextureASTC>()?.Image;
            if (astcimg != null) return astcimg;

            return _source.HasValue ? LogicalParent.LogicalImages[_source.Value] : null;
        }

        private Image _GetFallbackImage()
        {
            var img = _source.HasValue ? LogicalParent.LogicalImages[_source.Value] : null;

            // if the default image is provided by _GetPrimaryImage() we don't need to return anything here.
            return _GetPrimaryImage() == img ? null : img;
        }

        public void SetImage(Image primaryImage)
        {
            Guard.NotNull(primaryImage, nameof(primaryImage));
            Guard.MustShareLogicalParent(this, primaryImage, nameof(primaryImage));

            ClearImages();

            if (primaryImage.Content.IsExtendedFormat)
            {                
                _SetExtendedImage(primaryImage);
            }
            else
            {                
                _source = primaryImage.LogicalIndex;
            }            
        }

        public void SetImages(Image primaryImage, Image fallbackImage)
        {
            Guard.NotNull(fallbackImage, nameof(fallbackImage));            
            Guard.MustShareLogicalParent(this, fallbackImage, nameof(fallbackImage));            
            Guard.IsTrue(fallbackImage.Content.IsJpg || fallbackImage.Content.IsPng, nameof(fallbackImage), "Fallback image must be PNG or JPEG");

            _SetExtendedImage(primaryImage);
            _source = fallbackImage.LogicalIndex;
        }

        private void _SetExtendedImage(Image extendedImage)
        {
            Guard.NotNull(extendedImage, nameof(extendedImage));
            Guard.MustShareLogicalParent(this, extendedImage, nameof(extendedImage));
            Guard.IsTrue(extendedImage.Content.IsExtendedFormat, "Primary image must be DDS, WEBP, KTX2 or XNB");

            if (extendedImage.Content.IsDds) { _UseDDSTexture().Image = extendedImage; return; }
            if (extendedImage.Content.IsXnb) { _UseXNBTexture().Image = extendedImage; return; }
            if (extendedImage.Content.IsWebp) { _UseWEBPTexture().Image = extendedImage; return; }
            if (extendedImage.Content.IsKtx2)
            {
                // ToDo: if the Ktx2 image uses ASTC compression we could do:
                // _UseASTCTexture().Image = extendedImage; return;

                _UseKTX2Texture().Image = extendedImage;
                return;
            }

            throw new NotImplementedException("Unknown image format");
        }

        public void ClearImages()
        {
            _source = null;
            this.RemoveExtensions<TextureDDS>();
            this.RemoveExtensions<TextureXNB>();
            this.RemoveExtensions<TextureWEBP>();
            this.RemoveExtensions<TextureKTX2>();
            this.RemoveExtensions<TextureASTC>();
        }

        private TextureDDS _UseDDSTexture()
        {
            this.RemoveExtensions<TextureWEBP>();
            this.RemoveExtensions<TextureXNB>();
            this.RemoveExtensions<TextureKTX2>();
            this.RemoveExtensions<TextureASTC>();
            return this.UseExtension<TextureDDS>();
        }

        private TextureXNB _UseXNBTexture()
        {
            this.RemoveExtensions<TextureDDS>();
            this.RemoveExtensions<TextureWEBP>();
            this.RemoveExtensions<TextureKTX2>();
            this.RemoveExtensions<TextureASTC>();
            return this.UseExtension<TextureXNB>();
        }

        private TextureWEBP _UseWEBPTexture()
        {
            this.RemoveExtensions<TextureDDS>();
            this.RemoveExtensions<TextureXNB>();
            this.RemoveExtensions<TextureKTX2>();
            this.RemoveExtensions<TextureASTC>();
            return this.UseExtension<TextureWEBP>();
        }

        private TextureKTX2 _UseKTX2Texture()
        {
            this.RemoveExtensions<TextureDDS>();
            this.RemoveExtensions<TextureXNB>();
            this.RemoveExtensions<TextureWEBP>();
            this.RemoveExtensions<TextureASTC>();
            return this.UseExtension<TextureKTX2>();
        }

        private TextureASTC _UseASTCTexture()
        {
            this.RemoveExtensions<TextureDDS>();
            this.RemoveExtensions<TextureXNB>();
            this.RemoveExtensions<TextureWEBP>();
            this.RemoveExtensions<TextureKTX2>();
            return this.UseExtension<TextureASTC>();
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

    #pragma warning disable GLTF1001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

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

        public override bool CheckIsRequiredExtension(ExtraProperties extensionOwner)
        {
            if (extensionOwner is not Texture tex) return false;

            return tex.FallbackImage == null;
        }
    }

    partial class TextureXNB
    {
        internal TextureXNB(Texture parent)
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

        public override bool CheckIsRequiredExtension(ExtraProperties extensionOwner)
        {
            if (extensionOwner is not Texture tex) return false;

            return tex.FallbackImage == null;
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

        public override bool CheckIsRequiredExtension(ExtraProperties extensionOwner)
        {
            if (extensionOwner is not Texture tex) return false;

            return tex.FallbackImage == null;
        }
    }

    partial class TextureKTX2
    {
        internal TextureKTX2(Texture parent)
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
                    Guard.IsTrue(value.Content.IsKtx2, nameof(value));
                }

                _source = value?.LogicalIndex;
            }
        }

        public override bool CheckIsRequiredExtension(ExtraProperties extensionOwner)
        {
            if (extensionOwner is not Texture tex) return false;

            return tex.FallbackImage == null;
        }
    }

    partial class TextureASTC
    {
        internal TextureASTC(Texture parent)
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
                    Guard.IsTrue(value.Content.IsKtx2, nameof(value));
                }

                _source = value?.LogicalIndex;
            }
        }

        public override bool CheckIsRequiredExtension(ExtraProperties extensionOwner)
        {
            if (extensionOwner is not Texture tex) return false;

            return tex.FallbackImage == null;
        }
    }

    #pragma warning restore GLTF1001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

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
        /// Gets the texture minification filter.
        /// </summary>
        /// <remarks>
        /// If value is Default, it must be interpreted by the runtime as "best fit"
        /// </remarks>
        public TEXMIPMAP MinFilter => _minFilter.AsValue(TEXMIPMAP.DEFAULT);

        /// <summary>
        /// Gets the texture magnification filter.
        /// </summary>
        /// <remarks>
        /// If value is Default, it must be interpreted by the runtime as "best fit"
        /// </remarks>
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

        public static bool AreEqualByContent(TextureSampler x, TextureSampler y)
        {
            #pragma warning disable IDE0041 // Use 'is null' check
            if (Object.ReferenceEquals(x, y)) return true;
            if (Object.ReferenceEquals(x, null)) return false;
            if (Object.ReferenceEquals(y, null)) return false;
            #pragma warning restore IDE0041 // Use 'is null' check

            if (x._minFilter != y._minFilter) return false;
            if (x._magFilter != y._magFilter) return false;
            if (x._wrapS != y._wrapS) return false;
            if (x._wrapT != y._wrapT) return false;

            return true;
        }

        internal bool IsEqualTo(TEXMIPMAP min, TEXLERP mag, TEXWRAP ws, TEXWRAP wt)
        {
            if (this._minFilter != min) return false;
            if (this._magFilter != mag) return false;
            if (this._wrapS != ws) return false;
            if (this._wrapT != wt) return false;

            return true;
        }

        public int GetContentHashCode()
        {
            var h = 0;
            h ^= this.MinFilter.GetHashCode();
            h ^= this.MagFilter.GetHashCode();
            h ^= this.WrapS.GetHashCode();
            h ^= this.WrapT.GetHashCode();
            return h;
        }

        #endregion

        #region nested types

        public static IEqualityComparer<TextureSampler> ContentComparer { get; private set; } = new _ContentComparer();

        private class _ContentComparer : IEqualityComparer<TextureSampler>
        {
            public bool Equals(TextureSampler x, TextureSampler y)
            {
                return AreEqualByContent(x, y);
            }

            public int GetHashCode(TextureSampler obj)
            {
                return obj == null ? 0 : obj.GetContentHashCode();
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
                if (s.IsEqualTo(min, mag, ws, wt)) return s;
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

            Guard.MustShareLogicalParent(this, "this", primary, nameof(primary));
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

            System.Diagnostics.Debug.Assert((primary != null) == (tex.PrimaryImage != null), "primary image incorrectly set");
            System.Diagnostics.Debug.Assert((fallback != null) == (tex.FallbackImage != null), "fallback image incorrectly set");

            return tex;
        }
    }
}
