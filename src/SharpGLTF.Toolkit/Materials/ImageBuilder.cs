using System;
using System.Collections.Generic;
using System.Text;

using BYTES = System.ArraySegment<System.Byte>;
using IMAGEFILE = SharpGLTF.Memory.MemoryImage;

namespace SharpGLTF.Materials
{
    /// <summary>
    /// Represents an image that can be used at <see cref="TextureBuilder.PrimaryImage"/> and <see cref="TextureBuilder.FallbackImage"/>.
    /// </summary>
    [System.Diagnostics.DebuggerDisplay("{_DebuggerDisplay(),nq}")]
    public sealed class ImageBuilder : BaseBuilder
    {
        #region Debug

        internal string _DebuggerDisplay()
        {
            var txt = "Image ";
            if (!string.IsNullOrWhiteSpace(Name)) txt += $"{Name} ";
            txt += Content.ToDebuggerDisplay();

            return txt;
        }

        #endregion

        #region lifecycle

        public static implicit operator ImageBuilder(BYTES image) { return new IMAGEFILE(image); }

        public static implicit operator ImageBuilder(Byte[] image) { return new IMAGEFILE(image); }

        public static implicit operator ImageBuilder(string filePath) { return new IMAGEFILE(filePath); }

        public static implicit operator ImageBuilder(IMAGEFILE content) { return From(content); }

        public static ImageBuilder From(IMAGEFILE content, string name = null)
        {
            return content.IsEmpty ? null : new ImageBuilder(content, name, default);
        }

        public static ImageBuilder From(IMAGEFILE content, string name, IO.JsonContent extras)
        {
            return content.IsEmpty ? null : new ImageBuilder(content, name, extras);
        }

        private ImageBuilder(IMAGEFILE content, string name, IO.JsonContent extras)
            : base(name, extras)
        {
            Content = content;
        }

        internal ImageBuilder Clone()
        {
            return new ImageBuilder(this);
        }

        private ImageBuilder(ImageBuilder other)
            : base(other)
        {
            this.Content = other.Content;
        }

        #endregion

        #region data
        public IMAGEFILE Content { get; set; }

        public static bool AreEqualByContent(ImageBuilder x, ImageBuilder y)
        {
            if ((x, y).AreSameReference(out bool areTheSame)) return areTheSame;

            if (!BaseBuilder.AreEqualByContent(x, y)) return false;

            if (!IMAGEFILE.AreEqual(x.Content, y.Content)) return false;

            return true;
        }

        public static int GetContentHashCode(ImageBuilder x)
        {
            if (x == null) return 0;

            var h = BaseBuilder.GetContentHashCode(x);

            h ^= x.Content.GetHashCode();

            return h;
        }

        #endregion

        #region API

        public static bool IsValid(ImageBuilder ib) { return ib != null && ib.Content.IsValid; }

        #endregion

    }
}
