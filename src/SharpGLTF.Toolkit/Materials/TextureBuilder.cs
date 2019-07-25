using System;
using System.Collections.Generic;
using System.Linq;
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

        public int CoordinateSet { get; set; } = 0;

        public TEXMIPMAP MinFilter { get; set; } = TEXMIPMAP.DEFAULT;

        public TEXLERP MagFilter { get; set; } = TEXLERP.DEFAULT;

        public TEXWRAP WrapS { get; set; } = TEXWRAP.REPEAT;

        public TEXWRAP WrapT { get; set; } = TEXWRAP.REPEAT;

        private TextureTransformBuilder _Transform;

        public static bool AreEqual(TextureBuilder a, TextureBuilder b)
        {
            #pragma warning disable IDE0041 // Use 'is null' check
            if (Object.ReferenceEquals(a, b)) return true;
            if (Object.ReferenceEquals(a, null)) return false;
            if (Object.ReferenceEquals(b, null)) return false;
            #pragma warning restore IDE0041 // Use 'is null' check

            if (!Object.ReferenceEquals(a._Parent, b._Parent)) return false;

            if (a.CoordinateSet != b.CoordinateSet) return false;

            if (a.MinFilter != b.MinFilter) return false;
            if (a.MagFilter != b.MagFilter) return false;
            if (a.WrapS != b.WrapS) return false;
            if (a.WrapT != b.WrapT) return false;

            if (!_AreArraysContentEqual(a._PrimaryImageContent, b._PrimaryImageContent)) return false;
            if (!_AreArraysContentEqual(a._FallbackImageContent, b._FallbackImageContent)) return false;

            if (TextureTransformBuilder.AreEqual(a._Transform, b._Transform)) return false;

            return true;
        }

        private static bool _AreArraysContentEqual(BYTES a, BYTES b)
        {
            if (a.Equals(b)) return true;

            return Enumerable.SequenceEqual(a, b);
        }

        public static int GetContentHashCode(TextureBuilder x)
        {
            if (x == null) return 0;

            var h = x.CoordinateSet.GetHashCode();
            h ^= x.MinFilter.GetHashCode();
            h ^= x.MagFilter.GetHashCode();
            h ^= x.WrapS.GetHashCode();
            h ^= x.WrapT.GetHashCode();

            h ^= x._PrimaryImageContent.GetContentHashCode(16);
            h ^= x._FallbackImageContent.GetContentHashCode(16);

            return h;
        }

        #endregion

        #region properties

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

        public TextureTransformBuilder Transform => _Transform;

        public static IEqualityComparer<TextureBuilder> ContentComparer => _ContentComparer.Default;

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
                Guard.IsTrue(image._IsImage(), nameof(image), "Must be JPG, PNG, DDS or WEBP");
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
                Guard.IsTrue(image._IsJpgImage() || image._IsPngImage(), nameof(image), "Must be JPG or PNG");
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

        #region support types

        sealed class _ContentComparer : IEqualityComparer<TextureBuilder>
        {
            public static readonly _ContentComparer Default = new _ContentComparer();

            public bool Equals(TextureBuilder x, TextureBuilder y)
            {
                return TextureBuilder.AreEqual(x, y);
            }

            public int GetHashCode(TextureBuilder obj)
            {
                return TextureBuilder.GetContentHashCode(obj);
            }
        }

        #endregion
    }

    public class TextureTransformBuilder
    {
        #region lifecycle

        internal TextureTransformBuilder(Vector2 offset, Vector2 scale, float rotation = 0, int? coordSetOverride = null)
        {
            this.Offset = offset;
            this.Scale = scale;
            this.Rotation = rotation;
            this.CoordinateSetOverride = coordSetOverride;
        }

        #endregion

        #region data

        public Vector2 Offset { get; set; }

        public Vector2 Scale { get; set; } = Vector2.One;

        public Single Rotation { get; set; }

        /// <summary>
        /// Gets or sets the Texture Coordinate Set that will override <see cref="TextureBuilder.CoordinateSet"/>
        /// If <see cref="Schema2.TextureTransform"/> is supported by the client.
        /// </summary>
        public int? CoordinateSetOverride { get; set; }

        public static bool AreEqual(TextureTransformBuilder a, TextureTransformBuilder b)
        {
            #pragma warning disable IDE0041 // Use 'is null' check
            if (Object.ReferenceEquals(a, b)) return true;
            if (Object.ReferenceEquals(a, null)) return false;
            if (Object.ReferenceEquals(b, null)) return false;
            #pragma warning restore IDE0041 // Use 'is null' check

            if (a.Offset != b.Offset) return false;
            if (a.Scale != b.Scale) return false;
            if (a.Rotation != b.Rotation) return false;

            if (a.CoordinateSetOverride != b.CoordinateSetOverride) return false;

            return true;
        }

        #endregion

        #region properties

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

        #endregion
    }
}
