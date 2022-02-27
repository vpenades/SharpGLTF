using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;

using TEXLERP = SharpGLTF.Schema2.TextureInterpolationFilter;
using TEXMIPMAP = SharpGLTF.Schema2.TextureMipMapFilter;
using TEXWRAP = SharpGLTF.Schema2.TextureWrapMode;

namespace SharpGLTF.Materials
{
    /// <summary>
    /// Represents the texture used by a <see cref="ChannelBuilder"/>
    /// </summary>
    [System.Diagnostics.DebuggerDisplay("{_DebuggerDisplay(),nq}")]
    public class TextureBuilder : BaseBuilder
    {
        #region Debug

        internal string _DebuggerDisplay()
        {
            var txt = "Texture ";
            if (CoordinateSet != 0) txt += $" {CoordinateSet}ˢᵉᵗ";

            if (MinFilter != TEXMIPMAP.DEFAULT) txt += $" {MinFilter}ᴹⁱⁿ";
            if (MagFilter != TEXLERP.DEFAULT) txt += $" {MagFilter}ᴹᵃᵍ";

            if (WrapS != TEXWRAP.REPEAT) txt += $" {WrapS}↔";
            if (WrapT != TEXWRAP.REPEAT) txt += $" {WrapT}↕";

            if (_PrimaryImageContent != null) txt += $" {_PrimaryImageContent.Content.ToDebuggerDisplay()}";
            if (_FallbackImageContent != null) txt += $" => {_FallbackImageContent.Content.ToDebuggerDisplay()}";

            return txt;
        }

        #endregion

        #region lifecycle

        internal TextureBuilder(ChannelBuilder parent)
        {
            Guard.NotNull(parent, nameof(parent));

            _Parent = parent;
        }

        #endregion

        #region data

        [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)]
        private readonly ChannelBuilder _Parent;

        [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)]
        private ImageBuilder _PrimaryImageContent;

        [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)]
        private ImageBuilder _FallbackImageContent;

        [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)]
        private TextureTransformBuilder _Transform;

        public int CoordinateSet { get; set; }

        public TEXMIPMAP MinFilter { get; set; } = TEXMIPMAP.DEFAULT;

        public TEXLERP MagFilter { get; set; } = TEXLERP.DEFAULT;

        public TEXWRAP WrapS { get; set; } = TEXWRAP.REPEAT;

        public TEXWRAP WrapT { get; set; } = TEXWRAP.REPEAT;

        public static bool AreEqualByContent(TextureBuilder x, TextureBuilder y)
        {
            if (x == null || y == null) return true;

            if ((x, y).AreSameReference(out bool areTheSame)) return areTheSame;

            if (!BaseBuilder.AreEqualByContent(x, y)) return false;

            if (x.CoordinateSet != y.CoordinateSet) return false;

            if (x.MinFilter != y.MinFilter) return false;
            if (x.MagFilter != y.MagFilter) return false;
            if (x.WrapS != y.WrapS) return false;
            if (x.WrapT != y.WrapT) return false;

            if (!ImageBuilder.AreEqualByContent(x._PrimaryImageContent, y._PrimaryImageContent)) return false;
            if (!ImageBuilder.AreEqualByContent(x._FallbackImageContent, y._FallbackImageContent)) return false;

            if (!TextureTransformBuilder.AreEqualByContent(x._Transform, y._Transform)) return false;

            return true;
        }

        public static int GetContentHashCode(TextureBuilder x)
        {
            if (x == null) return 0;

            var h = BaseBuilder.GetContentHashCode(x);

            h ^= x.CoordinateSet.GetHashCode();
            h ^= x.MinFilter.GetHashCode();
            h ^= x.MagFilter.GetHashCode();
            h ^= x.WrapS.GetHashCode();
            h ^= x.WrapT.GetHashCode();

            h ^= ImageBuilder.GetContentHashCode(x._PrimaryImageContent);
            h ^= ImageBuilder.GetContentHashCode(x._FallbackImageContent);

            return h;
        }

        #endregion

        #region properties

        /// <summary>
        /// Gets or sets the default image bytes to use by this <see cref="TextureBuilder"/>,
        /// Supported formats are: PNG, JPG, DDS, WEBP and KTX2
        /// </summary>
        public ImageBuilder PrimaryImage
        {
            get => _PrimaryImageContent;
            set => WithPrimaryImage(value);
        }

        /// <summary>
        /// Gets or sets the fallback image bytes to use by this <see cref="TextureBuilder"/>,
        /// Supported formats are: PNG, JPG.
        /// </summary>
        public ImageBuilder FallbackImage
        {
            get => _FallbackImageContent;
            set => WithFallbackImage(value);
        }

        public TextureTransformBuilder Transform => _Transform;

        public static IEqualityComparer<TextureBuilder> ContentComparer => _ContentComparer.Default;

        #endregion

        #region API

        internal void CopyTo(TextureBuilder other)
        {
            other.SetNameAndExtrasFrom(other);

            other._PrimaryImageContent = this._PrimaryImageContent?.Clone();
            other._FallbackImageContent = this._FallbackImageContent?.Clone();

            other.CoordinateSet = this.CoordinateSet;

            other.MinFilter = this.MinFilter;
            other.MagFilter = this.MagFilter;
            other.WrapS = this.WrapS;
            other.WrapT = this.WrapT;

            var xform = new TextureTransformBuilder(this._Transform);
            other._Transform = xform.IsDefault ? null : xform;
        }

        public TextureBuilder WithCoordinateSet(int cset) { CoordinateSet = cset; return this; }

        public TextureBuilder WithPrimaryImage(ImageBuilder image)
        {
            if (image != null)
            {
                Guard.IsTrue(ImageBuilder.IsValid(image), nameof(image), "Must be JPG, PNG, DDS, WEBP or KTX2");
            }
            else
            {
                image = default;
            }

            _PrimaryImageContent = image;
            return this;
        }

        public TextureBuilder WithFallbackImage(ImageBuilder image)
        {
            if (image != null)
            {
                Guard.IsTrue(image.Content.IsJpg || image.Content.IsPng, nameof(image), "Must be JPG or PNG");
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

        #region nested types

        sealed class _ContentComparer : IEqualityComparer<TextureBuilder>
        {
            public static readonly _ContentComparer Default = new _ContentComparer();

            public bool Equals(TextureBuilder x, TextureBuilder y)
            {
                return AreEqualByContent(x, y);
            }

            public int GetHashCode(TextureBuilder obj)
            {
                return GetContentHashCode(obj);
            }
        }

        #endregion
    }

    [System.Diagnostics.DebuggerDisplay("Transform 𝐒:{Scale} 𝐑:{Rotation} 𝚻:{Offset}")]
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

        internal TextureTransformBuilder(TextureTransformBuilder other)
        {
            this.Offset = other?.Offset ?? Vector2.Zero;
            this.Scale = other?.Scale ?? Vector2.One;
            this.Rotation = other?.Rotation ?? 0;
            this.CoordinateSetOverride = other?.CoordinateSetOverride;
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

        public static bool AreEqualByContent(TextureTransformBuilder a, TextureTransformBuilder b)
        {
            if (a != null && a.IsDefault) a = null;
            if (b != null && b.IsDefault) b = null;

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
                return true;
            }
        }

        #endregion
    }
}
