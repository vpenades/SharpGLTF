using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

using BYTES = System.ArraySegment<byte>;

using TEXLERP = SharpGLTF.Schema2.TextureInterpolationFilter;
using TEXMIPMAP = SharpGLTF.Schema2.TextureMipMapFilter;
using TEXWRAP = SharpGLTF.Schema2.TextureWrapMode;

namespace SharpGLTF.Materials
{
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

        private TextureTransformBuilder _Transform;

        #endregion

        #region properties

        public int CoordinateSet { get; set; } = 0;

        /// <summary>
        /// Gets or sets the default image bytes to use by this <see cref="TextureBuilder"/>,
        /// Supported formats are: PNG, JPG, DDS and WEBP
        /// </summary>
        public BYTES PrimaryImageContent
        {
            get => _PrimaryImageContent;
            set => WithImage(value);
        }

        /// <summary>
        /// Gets or sets the fallback image bytes to use by this <see cref="TextureBuilder"/>,
        /// Supported formats are: PNG, JPG.
        /// </summary>
        public BYTES FallbackImageContent
        {
            get => _FallbackImageContent;
            set => WithFallbackImage(value);
        }

        public TEXMIPMAP MinFilter { get; set; } = TEXMIPMAP.DEFAULT;

        public TEXLERP MagFilter { get; set; } = TEXLERP.DEFAULT;

        public TEXWRAP WrapS { get; set; } = TEXWRAP.REPEAT;

        public TEXWRAP WrapT { get; set; } = TEXWRAP.REPEAT;

        public TextureTransformBuilder Transform => _Transform;

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
                Guard.IsTrue(image._IsJpgImage() || image._IsPngImage() || image._IsDdsImage() || image._IsWebpImage(), nameof(image), "Must be JPG, PNG, DDS or WEBP");
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

        public TextureBuilder WithTransform(Single offsetX, Single offsetY, Single scaleX = 1, Single scaleY = 1, Single rotation = 0, int? coordSetOverride = null)
        {
            return WithTransform(new Vector2(offsetX, offsetY), new Vector2(scaleX, scaleY), rotation, coordSetOverride);
        }

        public TextureBuilder WithTransform(Vector2 offset, Vector2 scale, Single rotation = 0, int? coordSetOverride = null)
        {
            var xform = new TextureTransformBuilder(offset, scale, rotation, coordSetOverride);

            _Transform = xform.IsDefault ? null : xform;

            return this;
        }

        #endregion
    }

    public class TextureTransformBuilder
    {
        internal TextureTransformBuilder(Vector2 offset, Vector2 scale, float rotation = 0, int? coordSetOverride = null)
        {
            this.Offset = offset;
            this.Scale = scale;
            this.Rotation = rotation;
            this.CoordinateSetOverride = coordSetOverride;
        }

        public Vector2 Offset { get; set; }

        public Vector2 Scale { get; set; } = Vector2.One;

        public Single Rotation { get; set; }

        /// <summary>
        /// Gets or sets the Texture Coordinate Set that will override <see cref="TextureBuilder.CoordinateSet"/>
        /// If <see cref="Schema2.TextureTransform"/> is supported by the client.
        /// </summary>
        public int? CoordinateSetOverride { get; set; }

        internal bool IsDefault
        {
            get
            {
                if (Offset != Vector2.Zero) return false;
                if (Scale != Vector2.One) return false;
                if (Rotation != 0) return false;
                if (CoordinateSetOverride.HasValue) return false;
                return false;
            }
        }

    }
}
