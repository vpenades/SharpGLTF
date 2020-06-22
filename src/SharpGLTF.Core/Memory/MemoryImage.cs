using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using BYTES = System.ArraySegment<System.Byte>;

namespace SharpGLTF.Memory
{
    /// <summary>
    /// Represents an image file stored as an in-memory byte array
    /// </summary>
    [System.Diagnostics.DebuggerDisplay("{DisplayText,nq}")]
    public readonly struct MemoryImage : IEquatable<MemoryImage>
    {
        #region constants

        const string EMBEDDED_OCTET_STREAM = "data:application/octet-stream";
        const string EMBEDDED_GLTF_BUFFER = "data:application/gltf-buffer";
        const string EMBEDDED_JPEG_BUFFER = "data:image/jpeg";
        const string EMBEDDED_PNG_BUFFER = "data:image/png";
        const string EMBEDDED_DDS_BUFFER = "data:image/vnd-ms.dds";
        const string EMBEDDED_WEBP_BUFFER = "data:image/webp";

        const string MIME_PNG = "image/png";
        const string MIME_JPG = "image/jpeg";
        const string MIME_DDS = "image/vnd-ms.dds";
        const string MIME_WEBP = "image/webp";

        /// <summary>
        /// Represents a 4x4 white PNG image.
        /// </summary>
        private const string DEFAULT_PNG_IMAGE = "iVBORw0KGgoAAAANSUhEUgAAABAAAAAQCAIAAACQkWg2AAAACXBIWXMAAA7DAAAOwwHHb6hkAAAAHXpUWHRUaXRsZQAACJlzSU1LLM0pCUmtKCktSgUAKVIFt/VCuZ8AAAAoelRYdEF1dGhvcgAACJkLy0xOzStJVQhIzUtMSS1WcCzKTc1Lzy8BAG89CQyAoFAQAAAANElEQVQoz2O8cuUKAwxoa2vD2VevXsUqzsRAIqC9Bsb///8TdDey+CD0Awsx7h6NB5prAADPsx0VAB8VRQAAAABJRU5ErkJggg==";

        internal static Byte[] DefaultPngImage => Convert.FromBase64String(DEFAULT_PNG_IMAGE);

        internal static readonly string[] _EmbeddedHeaders =
                { EMBEDDED_OCTET_STREAM,
                EMBEDDED_GLTF_BUFFER,
                EMBEDDED_JPEG_BUFFER,
                EMBEDDED_PNG_BUFFER,
                EMBEDDED_DDS_BUFFER,
                EMBEDDED_WEBP_BUFFER
                };

        public static MemoryImage Empty => default;

        private const string GuardError_MustBeValidImage = "Must be a valid image: Png, Jpg, etc...";

        #endregion

        #region constructor

        public static implicit operator MemoryImage(BYTES image) { return new MemoryImage(image); }

        public static implicit operator MemoryImage(Byte[] image) { return new MemoryImage(image); }

        public static implicit operator MemoryImage(string filePath) { return new MemoryImage(filePath); }

        public MemoryImage(BYTES image)
        {
            Guard.IsTrue(_IsImage(image), nameof(image), GuardError_MustBeValidImage);

            _Image = image;
            _SourcePathHint = null;
        }

        public MemoryImage(Byte[] image)
        {
            if (image != null) Guard.IsTrue(_IsImage(image), nameof(image), GuardError_MustBeValidImage);

            _Image = image == null ? default : new BYTES(image);
            _SourcePathHint = null;
        }

        public MemoryImage(string filePath)
        {
            if (string.IsNullOrEmpty(filePath))
            {
                _Image = default;
                _SourcePathHint = null;
            }
            else
            {
                filePath = System.IO.Path.GetFullPath(filePath);

                var data = System.IO.File.ReadAllBytes(filePath);

                Guard.IsTrue(_IsImage(data), nameof(filePath), GuardError_MustBeValidImage);

                _Image = new BYTES(data);
                _SourcePathHint = filePath;
            }
        }

        internal MemoryImage(BYTES image, string filePath)
            : this(image)
        {
            _SourcePathHint = filePath;
        }

        internal MemoryImage(Byte[] image, string filePath)
            : this(image)
        {
            _SourcePathHint = filePath;
        }

        #endregion

        #region data

        private readonly BYTES _Image;

        [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)]
        private readonly String _SourcePathHint;

        public override int GetHashCode()
        {
            // since this object stores the file of an image,
            // using the file size as a hash is as good as anything else

            return _Image.Count.GetHashCode();
        }

        public static bool AreEqual(MemoryImage a, MemoryImage b)
        {
            // This compares the actual image contents, which means
            // that _SourcePathHint must not be taken into account.
            // For example, comparing two images from different paths
            // that represent the same image byte by byte should
            // return true.

            if (a.GetHashCode() != b.GetHashCode()) return false;
            if (a._Image.Equals(b._Image)) return true;
            return a._Image.AsSpan().SequenceEqual(b._Image);
        }

        public override bool Equals(object obj) { return obj is MemoryImage other && AreEqual(this, other); }

        public bool Equals(MemoryImage other) { return AreEqual(this, other); }

        public static bool operator ==(MemoryImage left, MemoryImage right) { return AreEqual(left, right); }

        public static bool operator !=(MemoryImage left, MemoryImage right) { return !AreEqual(left, right); }

        #endregion

        #region properties

        public bool IsEmpty => _Image.Count == 0;

        /// <summary>
        /// Gets the file bytes of the image.
        /// </summary>
        public ReadOnlyMemory<Byte> Content => _Image;

        /// <summary>
        /// Gets the source path of this image, or null if the image cannot be tracked to a file path (as it is the case of embedded images)
        /// </summary>
        public string SourcePath => _SourcePathHint;

        /// <summary>
        /// Gets a value indicating whether this object represents a valid PNG image.
        /// </summary>
        public bool IsPng => _IsPngImage(_Image);

        /// <summary>
        /// Gets a value indicating whether this object represents a valid JPG image.
        /// </summary>
        public bool IsJpg => _IsJpgImage(_Image);

        /// <summary>
        /// Gets a value indicating whether this object represents a valid DDS image.
        /// </summary>
        public bool IsDds => _IsDdsImage(_Image);

        /// <summary>
        /// Gets a value indicating whether this object represents a valid WEBP image.
        /// </summary>
        public bool IsWebp => _IsWebpImage(_Image);

        /// <summary>
        /// Gets a value indicating whether this object represents a valid image.
        /// </summary>
        public bool IsValid => _IsImage(_Image);

        /// <summary>
        /// Gets the most appropriate extension string for this image.
        /// </summary>
        public string FileExtension
        {
            get
            {
                if (IsEmpty) return null;
                if (IsPng) return "png";
                if (IsJpg) return "jpg";
                if (IsDds) return "dds";
                if (IsWebp) return "webp";
                throw new NotImplementedException();
            }
        }

        /// <summary>
        /// Gets the most appropriate Mime type string for this image.
        /// </summary>
        public string MimeType
        {
            get
            {
                if (IsEmpty) return null;
                if (IsPng) return MIME_PNG;
                if (IsJpg) return MIME_JPG;
                if (IsDds) return MIME_DDS;
                if (IsWebp) return MIME_WEBP;
                throw new NotImplementedException();
            }
        }

        public string DisplayText
        {
            get {
                if (!string.IsNullOrWhiteSpace(_SourcePathHint)) return System.IO.Path.GetFileName(_SourcePathHint);

                if (IsEmpty) return "Empty";
                if (!IsValid) return $"Unknown {_Image.Count}ᴮʸᵗᵉˢ";
                if (IsJpg) return $"JPG {_Image.Count}ᴮʸᵗᵉˢ";
                if (IsPng) return $"PNG {_Image.Count}ᴮʸᵗᵉˢ";
                if (IsDds) return $"DDS {_Image.Count}ᴮʸᵗᵉˢ";
                if (IsWebp) return $"WEBP {_Image.Count}ᴮʸᵗᵉˢ";
                return "Undefined";
            }
        }

        #endregion

        #region API

        /// <summary>
        /// Opens the image file for reading its contents
        /// </summary>
        /// <returns>A read only <see cref="System.IO.Stream"/>.</returns>
        public System.IO.Stream Open()
        {
            if (_Image.Count == 0) return null;
            return new System.IO.MemoryStream(_Image.Array, _Image.Offset, _Image.Count, false);
        }

        /// <summary>
        /// Saves the image stored in this <see cref="MemoryImage"/> to a file.
        /// </summary>
        /// <param name="filePath">A destination file path, with an extension matching <see cref="FileExtension"/></param>
        public void SaveToFile(string filePath)
        {
            Guard.FilePathMustBeValid(filePath, nameof(filePath));
            Guard.IsTrue(filePath.EndsWith("." + this.FileExtension, StringComparison.OrdinalIgnoreCase), nameof(filePath), $"{nameof(filePath)} must use extension '.{this.FileExtension}'");

            using (var dst = System.IO.File.Create(filePath))
            {
                using (var src = Open())
                {
                    src.CopyTo(dst);
                }
            }
        }

        /// <summary>
        /// Gets the internal buffer.
        /// </summary>
        /// <returns>An array buffer.</returns>
        internal BYTES _GetBuffer() { return _Image; }

        /// <summary>
        /// Returns this image file, enconded as a Mime64 string.
        /// </summary>
        /// <param name="withPrefix">true to prefix the string with a header.</param>
        /// <returns>A mime64 string.</returns>
        internal string ToMime64(bool withPrefix = true)
        {
            if (!this.IsValid) return null;

            var mimeContent = string.Empty;
            if (withPrefix)
            {
                if (this.IsPng) mimeContent = EMBEDDED_PNG_BUFFER;
                if (this.IsJpg) mimeContent = EMBEDDED_JPEG_BUFFER;
                if (this.IsDds) mimeContent = EMBEDDED_DDS_BUFFER;
                if (this.IsWebp) mimeContent = EMBEDDED_WEBP_BUFFER;

                mimeContent += ";base64,";
            }

            return mimeContent + Convert.ToBase64String(_Image.Array, _Image.Offset, _Image.Count, Base64FormattingOptions.None);
        }

        /// <summary>
        /// Tries to parse a Mime64 string to a Byte array.
        /// </summary>
        /// <param name="mime64content">The Mime64 string source.</param>
        /// <param name="data">if decoding succeeds, it will contain the decoded data</param>
        /// <returns>true if decoding succeeded.</returns>
        internal static bool TryParseMime64(string mime64content, out Byte[] data)
        {
            if (mime64content == null) { data = null; return false; }

            data = mime64content.TryParseBase64Unchecked(_EmbeddedHeaders);
            if (data == null) return false;

            if (mime64content.StartsWith(EMBEDDED_PNG_BUFFER, StringComparison.Ordinal) && !_IsPngImage(data)) throw new ArgumentException("Invalid PNG Content", nameof(mime64content));
            if (mime64content.StartsWith(EMBEDDED_JPEG_BUFFER, StringComparison.Ordinal) && !_IsJpgImage(data)) throw new ArgumentException("Invalid JPG Content", nameof(mime64content));
            if (mime64content.StartsWith(EMBEDDED_DDS_BUFFER, StringComparison.Ordinal) && !_IsDdsImage(data)) throw new ArgumentException("Invalid DDS Content", nameof(mime64content));
            if (mime64content.StartsWith(EMBEDDED_WEBP_BUFFER, StringComparison.Ordinal) && !_IsWebpImage(data)) throw new ArgumentException("Invalid WEBP Content", nameof(mime64content));

            return true;
        }

        /// <summary>
        /// identifies an image of a specific type.
        /// </summary>
        /// <param name="format">A string representing the format: png, jpg, dds...</param>
        /// <returns>True if this image is of the given type.</returns>
        public bool IsImageOfType(string format)
        {
            Guard.NotNullOrEmpty(format, nameof(format));

            if (!IsValid) return false;

            if (format.EndsWith("png", StringComparison.OrdinalIgnoreCase)) return IsPng;
            if (format.EndsWith("jpg", StringComparison.OrdinalIgnoreCase)) return IsJpg;
            if (format.EndsWith("jpeg", StringComparison.OrdinalIgnoreCase)) return IsJpg;
            if (format.EndsWith("dds", StringComparison.OrdinalIgnoreCase)) return IsDds;
            if (format.EndsWith("webp", StringComparison.OrdinalIgnoreCase)) return IsWebp;

            return false;
        }

        #endregion

        #region internals

        private static bool _IsPngImage(IReadOnlyList<Byte> data)
        {
            if (data[0] != 0x89) return false;
            if (data[1] != 0x50) return false;
            if (data[2] != 0x4e) return false;
            if (data[3] != 0x47) return false;

            return true;
        }

        private static bool _IsJpgImage(IReadOnlyList<Byte> data)
        {
            if (data[0] != 0xff) return false;
            if (data[1] != 0xd8) return false;

            return true;
        }

        private static bool _IsDdsImage(IReadOnlyList<Byte> data)
        {
            if (data[0] != 0x44) return false;
            if (data[1] != 0x44) return false;
            if (data[2] != 0x53) return false;
            if (data[3] != 0x20) return false;
            return true;
        }

        private static bool _IsWebpImage(IReadOnlyList<Byte> data)
        {
            // RIFF
            if (data[0] != 0x52) return false;
            if (data[1] != 0x49) return false;
            if (data[2] != 0x46) return false;
            if (data[3] != 0x46) return false;

            // WEBP
            if (data[8] != 0x57) return false;
            if (data[9] != 0x45) return false;
            if (data[10] != 0x42) return false;
            if (data[11] != 0x50) return false;

            return true;
        }

        private static bool _IsImage(IReadOnlyList<Byte> data)
        {
            if (data == null) return false;
            if (data.Count < 12) return false;

            if (_IsDdsImage(data)) return true;
            if (_IsJpgImage(data)) return true;
            if (_IsPngImage(data)) return true;
            if (_IsWebpImage(data)) return true;

            return false;
        }

        #endregion
    }
}
