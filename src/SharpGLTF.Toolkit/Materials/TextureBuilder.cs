using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace SharpGLTF.Materials
{
    using BYTES = ArraySegment<Byte>;

    using TEXLERP = Schema2.TextureInterpolationFilter;
    using TEXMIPMAP = Schema2.TextureMipMapFilter;
    using TEXWRAP = Schema2.TextureWrapMode;

    [System.Diagnostics.DebuggerDisplay("Texture {CoordinateSet} {MinFilter} {MagFilter} {WrapS} {WrapT} {Rotation} {Offset} {Scale}")]
    public class TextureBuilder
    {
        #region lifecycle

        internal TextureBuilder(ChannelBuilder parent)
        {
            Guard.NotNull(parent, nameof(parent));

            _Parent = parent;
        }

        #endregion

        #region data

        private readonly ChannelBuilder _Parent;

        private BYTES _PrimaryImageContent;
        private BYTES _FallbackImageContent;

        #endregion

        #region properties

        public int CoordinateSet { get; set; } = 0;

        public BYTES PrimaryImageContent
        {
            get => _PrimaryImageContent;
            set => WithImage(value);
        }

        public BYTES FallbackImageContent
        {
            get => _FallbackImageContent;
            set => WithFallbackImage(value);
        }

        public TEXMIPMAP MinFilter { get; set; } = TEXMIPMAP.DEFAULT;

        public TEXLERP MagFilter { get; set; } = TEXLERP.DEFAULT;

        public TEXWRAP WrapS { get; set; } = TEXWRAP.REPEAT;

        public TEXWRAP WrapT { get; set; } = TEXWRAP.REPEAT;

        #endregion

        #region API

        public TextureBuilder WithCoordinateSet(int cset) { CoordinateSet = cset; return this; }

        public TextureBuilder WithImage(string imagePath)
        {
            var primary = System.IO.File
                .ReadAllBytes(imagePath)
                .Slice(0);

            return WithImage(primary);
        }

        public TextureBuilder WithImage(BYTES image)
        {
            if (image.Count > 0)
            {
                Guard.IsTrue(image._IsJpgImage() || image._IsPngImage() || image._IsDdsImage(), nameof(image), "Must be JPG, PNG or DDS");
            }
            else
            {
                image = default;
            }

            _PrimaryImageContent = image;
            return this;
        }

        public TextureBuilder WithFallbackImage(string imagePath)
        {
            var primary = System.IO.File
                .ReadAllBytes(imagePath)
                .Slice(0);

            return WithFallbackImage(primary);
        }

        public TextureBuilder WithFallbackImage(BYTES image)
        {
            if (image.Count > 0)
            {
                Guard.IsTrue(image._IsJpgImage() || image._IsPngImage(), nameof(image), "Must be JPG, PNG");
            }
            else
            {
                image = default;
            }

            _FallbackImageContent = image;
            return this;
        }

        public TextureBuilder WithSampler(TEXWRAP ws, TEXWRAP wt, TEXMIPMAP min = TEXMIPMAP.DEFAULT, TEXLERP mag = TEXLERP.DEFAULT)
        {
            this.WrapS = ws;
            this.WrapT = wt;

            this.MinFilter = min;
            this.MagFilter = mag;

            return this;
        }

        #endregion
    }
}
