using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Versioning;
using System.Text;

using BYTES = System.ArraySegment<System.Byte>;
using LAZYBYTES = System.Lazy<System.ArraySegment<System.Byte>>;

namespace SharpGLTF.Memory
{
    /// <summary>
    /// Represents an image file stored as an in-memory byte array
    /// </summary>
    [System.Diagnostics.DebuggerDisplay("{ToDebuggerDisplay(),nq}")]
    public readonly struct MemoryImage : IEquatable<MemoryImage>
    {
        #region diagnostics

        public string ToDebuggerDisplay()
        {
            if (!string.IsNullOrWhiteSpace(_SourcePathHint)) return System.IO.Path.GetFileName(_SourcePathHint);

            if (IsEmpty) return "Empty";

            var name = _GetFormat()?.DisplayName ?? "Unknown";            

            return $"{name} {_Image.Count}ᴮʸᵗᵉˢ";
        }

        #endregion

        #region constants

        internal static MemoryImage DefaultPngImage => PngFormatInfo.DefaultPngImage;

        private static readonly PngFormatInfo _PngFormat = new PngFormatInfo();
        private static readonly JpgFormatInfo _JpgFormat = new JpgFormatInfo();
        private static readonly DdsFormatInfo _DdsFormat = new DdsFormatInfo();
        private static readonly XnbFormatInfo _XnbFormat = new XnbFormatInfo();
        private static readonly WebpFormatInfo _WebpFormat = new WebpFormatInfo();
        private static readonly Ktx2FormatInfo _Ktx2Format = new Ktx2FormatInfo();

        private static IEnumerable<ImageFormatInfo> _Formats
        {
            get
            {
                yield return _PngFormat;
                yield return _JpgFormat;
                yield return _DdsFormat;
                yield return _XnbFormat;
                yield return _WebpFormat;
                yield return _Ktx2Format;
            }
        }        

        internal static readonly string[] _EmbeddedHeaders = _Formats.Select(item => item.GetMime(true)).Distinct().ToArray();        

        private const string GuardError_MustBeValidImage = "Must be a valid image: Png, Jpg, etc...";

        #endregion

        #region constructor

        public static implicit operator MemoryImage(BYTES image) { return new MemoryImage(image); }

        public static implicit operator MemoryImage(Byte[] image) { return new MemoryImage(image); }

        public static implicit operator MemoryImage(string filePath) { return new MemoryImage(filePath); }

        #if NET10_0 // https://github.com/dotnet/docs/issues/47602
        public static bool TryParseMime64(Uri mime64content, out MemoryImage image)
        {
            return TryParseMime64(mime64content?.OriginalString, out image);
        }
        #endif

        /// <summary>
        /// Tries to parse a Mime64 string to <see cref="MemoryImage"/>
        /// </summary>
        /// <param name="mime64content">The Mime64 string source.</param>
        /// <param name="image">if decoding succeeds, it will contain the image file.</param>
        /// <returns>true if decoding succeeded.</returns>
        /// <remarks>
        /// The string must be haedered with a mime prefix like:     "data:image/png;base64,"
        /// </remarks>
        public static bool TryParseMime64(string mime64content, out MemoryImage image)
        {
            if (string.IsNullOrWhiteSpace(mime64content)) { image = default; return false; }

            var data = mime64content.TryParseBase64Unchecked(_EmbeddedHeaders);
            if (data == null) { image = default; return false; }

            foreach(var fmtInfo in _Formats)
            {
                var embeddedMime = fmtInfo.GetMime(true);
                if (mime64content.StartsWith(embeddedMime, StringComparison.Ordinal) && !fmtInfo.IsFormat(data)) throw new ArgumentException($"Invalid {fmtInfo.DisplayName} Content", nameof(mime64content));
            }

            image = data;
            return true;
        }

        public MemoryImage(BYTES image) 
            : this(_ToLazy(image), null) { }

        public MemoryImage(Byte[] image)
            : this(_ToLazy(image), null) { }

        public MemoryImage(Func<BYTES> factory)
            : this(new LAZYBYTES(factory), null) { }

        public MemoryImage(string filePath)
        {
            if (string.IsNullOrEmpty(filePath))
            {
                _LazyImage = _ToLazy(default(BYTES));
                _SourcePathHint = null;
            }
            else
            {
                filePath = System.IO.Path.GetFullPath(filePath);

                var data = System.IO.File.ReadAllBytes(filePath);

                Guard.IsTrue(_IsImage(data), nameof(filePath), GuardError_MustBeValidImage);

                _LazyImage = _ToLazy(data);
                _SourcePathHint = filePath;
            }
        }        

        internal MemoryImage(Byte[] image, string filePath)
            : this(_ToLazy(image), filePath) { }

        internal MemoryImage(BYTES image, string filePath)
            : this(_ToLazy(image), filePath) { }

        internal MemoryImage(MemoryImage image, string filePath)
        {
            _LazyImage = image._LazyImage;
            _SourcePathHint = filePath ?? image._SourcePathHint;
        }

        internal MemoryImage(LAZYBYTES image, string filePath)
        {
            _LazyImage = image;
            _SourcePathHint = filePath;
        }        

        private static LAZYBYTES _ToLazy(Byte[] bytes)
        {
            bytes ??= Array.Empty<byte>();
            return _ToLazy(new BYTES(bytes));
        }
        private static LAZYBYTES _ToLazy(BYTES bytes)
        {
            #if !NETSTANDARD2_0
            return new LAZYBYTES(bytes);
            #else
            return new LAZYBYTES(() => bytes);            
            #endif
        }

        #endregion

        #region data

        private readonly LAZYBYTES _LazyImage;

        #if !NETSTANDARD2_0
        private BYTES _Image => _LazyImage == null ? BYTES.Empty : _LazyImage.Value;
        #else
        private BYTES _Image => _LazyImage == null ? new BYTES(Array.Empty<byte>()) : _LazyImage.Value;
        #endif

        /// <remarks>
        /// This field must NOT be used for equality checks, it has the same face value as a code comment.
        /// </remarks>
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

        public static MemoryImage Empty => default;

        public bool IsEmpty => _Image.Count == 0;

        /// <summary>
        /// Gets the file bytes of the image.
        /// </summary>
        public ReadOnlyMemory<Byte> Content => _Image;

        /// <summary>
        /// Gets the source path of this image, or <b>null</b>.
        /// <para><b>⚠️ DO NOT USE AS AN OBJECT ID ⚠️</b> see remarks.</para>
        /// </summary>
        /// <remarks>
        /// <para>
        /// Not all images are expected to have a source path.<br/>
        /// Specifically images embedded in a GLB file or encoded with BASE64
        /// will not have any source path at all.<br/>
        /// So if your code depends on images having a path, it might crash
        /// on gltf files with embedded images.
        /// </para>
        /// <para>
        /// If SourcePath has a value, it may be relative or absolute depending on the read context.
        /// </para>
        /// </remarks>
        public string SourcePath => _SourcePathHint;

        /// <summary>
        /// Gets a value indicating whether this object represents a valid PNG image.
        /// </summary>
        public bool IsPng => _PngFormat.IsFormat(_Image);

        /// <summary>
        /// Gets a value indicating whether this object represents a valid JPG image.
        /// </summary>
        public bool IsJpg => _JpgFormat.IsFormat(_Image);

        /// <summary>
        /// Gets a value indicating whether this object represents a valid DDS image.
        /// </summary>
        public bool IsDds => _DdsFormat.IsFormat(_Image);

        /// <summary>
        /// Gets a value indicating whether this object represents a valid WEBP image.
        /// </summary>
        public bool IsWebp => _WebpFormat.IsFormat(_Image);

        /// <summary>
        /// Gets a value indicating whether this object represents a valid KTX2 image.
        /// </summary>
        public bool IsKtx2 => _Ktx2Format.IsFormat(_Image);

        /// <summary>
        /// Gets a value indicating whether this object represents a valid XNA XNB image.
        /// </summary>
        public bool IsXnb => _XnbFormat.IsFormat(_Image);

        /// <summary>
        /// Gets a value indicating whether this object represents an image backed by a glTF extension.
        /// </summary>
        public bool IsExtendedFormat => _GetFormat()?.RequiresExtension ?? false;

        /// <summary>
        /// Gets a value indicating whether this object represents a valid image.
        /// </summary>
        public bool IsValid
        {
            get
            {
                try { _Verify(this, string.Empty); return true; }
                catch { return false; }
            }
        }

        /// <summary>
        /// Gets the most appropriate extension string for this image.
        /// </summary>
        public string FileExtension => IsEmpty ? null : _UseFormat().Extension;

        /// <summary>
        /// Gets the most appropriate Mime type string for this image.
        /// </summary>
        public string MimeType => IsEmpty ? null : _UseFormat().GetMime(false);
        
        /// <summary>
        /// If the given path ends with an image extension, it removes the extension.
        /// </summary>
        /// <param name="path">A file path</param>
        /// <returns>a trimmed path if it had an image extension, or the original path.</returns>
        public static string TrimImageExtension(string path)
        {
            if (string.IsNullOrWhiteSpace(path)) return path;

            var extensions = _Formats
                .Select(item => "." + item.Extension)
                .Distinct();

            foreach (var ext in extensions)
            {
                if (path.EndsWith(ext, StringComparison.OrdinalIgnoreCase)) return path.Substring(0, path.Length - ext.Length);
            }

            return path;
        }

        #endregion

        #region API        

        /// <summary>
        /// Opens the image file for reading its contents
        /// </summary>
        /// <returns>A read only <see cref="System.IO.Stream"/>.</returns>
        public System.IO.Stream Open()
        {
            if (IsEmpty) return null;
            return new System.IO.MemoryStream(_Image.Array, _Image.Offset, _Image.Count, false);
        }

        /// <summary>
        /// Saves the image stored in this <see cref="MemoryImage"/> to a file.
        /// </summary>
        /// <param name="filePath">A destination file path, with an extension matching <see cref="FileExtension"/></param>
        public void SaveToFile(string filePath)
        {
            if (IsEmpty) return;

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
        /// identifies an image of a specific type.
        /// </summary>
        /// <param name="format">A string representing the format: png, jpg, dds...</param>
        /// <returns>True if this image is of the given type.</returns>
        public bool IsImageOfType(string format)
        {
            Guard.NotNullOrEmpty(format, nameof(format));

            var fmt = _GetFormat();
            return fmt != null && fmt.Extension == format;
        }

        #endregion

        #region internals

        /// <summary>
        /// Gets the internal buffer.
        /// </summary>
        /// <returns>An array buffer.</returns>
        internal BYTES _GetBuffer() { return _Image; }

        private ImageFormatInfo _UseFormat()
        {
            return _GetFormat() ?? throw new InvalidOperationException("Image format not recognized.");
        }

        private ImageFormatInfo _GetFormat()
        {
            var img = _Image;
            return _Formats.FirstOrDefault(item => item.IsFormat(img));
        }

        internal static void _Verify(MemoryImage image, string paramName)
        {
            image._UseFormat().Verify(image._Image, paramName);
        }

        private static bool _IsImage(IReadOnlyList<Byte> data)
        {
            if (data == null || data.Count == 0) return false;

            return _Formats.Any(fmt => fmt.IsFormat(data));
        }

        /// <summary>
        /// Returns this image file, enconded as a Mime64 string.
        /// </summary>
        /// <param name="withPrefix">true to prefix the string with a header.</param>
        /// <returns>A mime64 string.</returns>
        internal string ToMime64(bool withPrefix = true)
        {
            if (!_IsImage(_Image)) return null;

            var mimeContent = string.Empty;

            if (withPrefix) mimeContent = _UseFormat().GetMime(true) + ";base64,";

            return mimeContent + Convert.ToBase64String(_Image.Array, _Image.Offset, _Image.Count, Base64FormattingOptions.None);
        }

        #endregion
    }    
}
