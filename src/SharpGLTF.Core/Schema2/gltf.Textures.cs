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

        public Image Image
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
        /// <param name="min">A value of <see cref="TEXMIPMAP"/>.</param>
        /// <param name="mag">A value of <see cref="TEXLERP"/>.</param>
        /// <param name="ws">The <see cref="TEXWRAP"/> in the S axis.</param>
        /// <param name="wt">The <see cref="TEXWRAP"/> in the T axis.</param>
        /// <returns>A <see cref="TextureSampler"/> instance, or null if all the arguments are default values.</returns>
        public TextureSampler UseSampler(TEXMIPMAP min, TEXLERP mag, TEXWRAP ws, TEXWRAP wt)
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
        /// <param name="image">The source <see cref="Image"/>.</param>
        /// <param name="sampler">The source <see cref="TextureSampler"/>.</param>
        /// <returns>A <see cref="Texture"/> instance.</returns>
        public Texture UseTexture(Image image, TextureSampler sampler)
        {
            if (image == null) return null;

            if (image != null) Guard.MustShareLogicalParent(this, image, nameof(image));
            if (sampler != null) Guard.MustShareLogicalParent(this, sampler, nameof(sampler));

            var tex = _textures.FirstOrDefault(item => item.Image == image && item.Sampler == sampler);
            if (tex != null) return tex;

            tex = new Texture();
            _textures.Add(tex);

            tex.Image = image;
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
                TextureCoordinate = textureSet
            };
        }
    }
}
