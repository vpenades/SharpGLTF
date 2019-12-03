using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SharpGLTF.Validation;
using BYTES = System.ArraySegment<byte>;

namespace SharpGLTF.Schema2
{
    [System.Diagnostics.DebuggerDisplay("Image[{LogicalIndex}] {Name}")]
    public sealed partial class Image
    {
        #region Base64 constants

        const string EMBEDDED_OCTET_STREAM = "data:application/octet-stream;base64,";
        const string EMBEDDED_GLTF_BUFFER = "data:application/gltf-buffer;base64,";
        const string EMBEDDED_JPEG_BUFFER = "data:image/jpeg;base64,";
        const string EMBEDDED_PNG_BUFFER = "data:image/png;base64,";
        const string EMBEDDED_DDS_BUFFER = "data:image/vnd-ms.dds;base64,";
        const string EMBEDDED_WEBP_BUFFER = "data:image/webp;base64,";

        const string MIME_PNG = "image/png";
        const string MIME_JPG = "image/jpeg";
        const string MIME_DDS = "image/vnd-ms.dds";
        const string MIME_WEBP = "image/webp";

        const string DEFAULT_PNG_IMAGE = "iVBORw0KGgoAAAANSUhEUgAAABAAAAAQCAIAAACQkWg2AAAACXBIWXMAAA7DAAAOwwHHb6hkAAAAHXpUWHRUaXRsZQAACJlzSU1LLM0pCUmtKCktSgUAKVIFt/VCuZ8AAAAoelRYdEF1dGhvcgAACJkLy0xOzStJVQhIzUtMSS1WcCzKTc1Lzy8BAG89CQyAoFAQAAAANElEQVQoz2O8cuUKAwxoa2vD2VevXsUqzsRAIqC9Bsb///8TdDey+CD0Awsx7h6NB5prAADPsx0VAB8VRQAAAABJRU5ErkJggg==";

        internal static Byte[] DefaultPngImage => Convert.FromBase64String(DEFAULT_PNG_IMAGE);

        #endregion

        #region lifecycle

        internal Image() { }

        #endregion

        #region data

        /// <summary>
        /// this is the not a raw bitmap, but tha actual compressed image in PNG or JPEG.
        /// </summary>
        /// <remarks>
        /// When a model is loaded, the image file is loaded into memory and assigned to this
        /// field, and the <see cref="Image._uri"/> and <see cref="Image._mimeType"/> fields are nullified.
        /// When writing a gltf file with external images, the <see cref="Image._uri"/> and <see cref="Image._mimeType"/>
        /// fields are briefly reassigned so the JSON can be serialized correctly.
        /// After serialization <see cref="Image._uri"/> and <see cref="Image._mimeType"/> fields are set back to null.
        /// </remarks>
        private Byte[] _SatelliteImageContent;

        #endregion

        #region properties

        /// <summary>
        /// Gets the zero-based index of this <see cref="Image"/> at <see cref="ModelRoot.LogicalImages"/>
        /// </summary>
        public int LogicalIndex => this.LogicalParent.LogicalImages.IndexOfReference(this);

        /// <summary>
        /// Gets a value indicating whether the contained image is stored in a satellite file when loaded or saved.
        /// </summary>
        public bool IsSatelliteFile => _SatelliteImageContent != null;

        /// <summary>
        /// Gets a value indicating whether the contained image is a PNG image.
        /// </summary>
        public bool IsPng => GetImageContent()._IsPngImage();

        /// <summary>
        /// Gets a value indicating whether the contained image is a JPEG image.
        /// </summary>
        public bool IsJpeg => GetImageContent()._IsJpgImage();

        /// <summary>
        /// Gets a value indicating whether the contained image is a DDS image.
        /// </summary>
        public bool IsDds => GetImageContent()._IsDdsImage();

        /// <summary>
        /// Gets a value indicating whether the contained image is a WEBP image.
        /// </summary>
        public bool IsWebp => GetImageContent()._IsWebpImage();

        /// <summary>
        /// Gets the filename extension of the image that can be retrieved with <see cref="GetImageContent"/>
        /// </summary>
        public string FileExtension
        {
            get
            {
                if (IsPng) return "png";
                if (IsJpeg) return "jpg";
                if (IsDds) return "dds";
                if (IsWebp) return "webp";
                return "raw";
            }
        }

        internal int _SourceBufferViewIndex => _bufferView.AsValue(-1);

        internal bool _HasContent
        {
            get
            {
                if (_bufferView != null) return true;
                if (_SatelliteImageContent != null) return true;
                return false;
            }
        }

        #endregion

        #region API

        /// <summary>
        /// Opens the image file.
        /// </summary>
        /// <returns>A <see cref="System.IO.Stream"/> containing the image file.</returns>
        public System.IO.Stream OpenImageFile()
        {
            var content = GetImageContent();
            if (content.Count == 0) return null;

            return new System.IO.MemoryStream(content.Array, content.Offset, content.Count, false);
        }

        /// <summary>
        /// Retrieves the image file as a segment of bytes.
        /// </summary>
        /// <returns>A <see cref="BYTES"/> segment containing the image file, which can be a PNG, JPG, DDS or WEBP format.</returns>
        public BYTES GetImageContent()
        {
            // the image is stored locally in a temporary buffer
            if (_SatelliteImageContent != null) return new BYTES(_SatelliteImageContent);

            // the image is stored in a BufferView
            if (this._bufferView.HasValue)
            {
                var bv = this.LogicalParent.LogicalBufferViews[this._bufferView.Value];

                return bv.Content;
            }

            // TODO: if external images have not been loaded into _ExternalImageContent
            // and ModelRoot was loaded from file and stored the load path, use the _uri
            // to load the model.

            throw new InvalidOperationException();
        }

        /// <summary>
        /// Initializes this <see cref="Image"/> with an image loaded from a file.
        /// </summary>
        /// <param name="filePath">A valid path to an image file.</param>
        public void SetSatelliteFile(string filePath)
        {
            var content = System.IO.File.ReadAllBytes(filePath);
            SetSatelliteContent(content);
        }

        /// <summary>
        /// Initializes this <see cref="Image"/> with an image stored in a <see cref="Byte"/> array.
        /// </summary>
        /// <param name="content">A <see cref="Byte"/> array containing a PNG or JPEG image.</param>
        public void SetSatelliteContent(Byte[] content)
        {
            Guard.NotNull(content, nameof(content));

            string imageType = null;

            if (content._IsPngImage()) imageType = MIME_PNG;
            if (content._IsJpgImage()) imageType = MIME_JPG;
            if (content._IsDdsImage()) imageType = MIME_DDS;
            if (content._IsWebpImage()) imageType = MIME_WEBP;

            if (imageType == null) throw new ArgumentException($"{nameof(content)} must be a PNG, JPG, DDS or WEBP image", nameof(content));

            _DiscardContent();

            this._SatelliteImageContent = content;
        }

        /// <summary>
        /// If the image is stored externaly as an image file,
        /// it creates a new BufferView and stores the image in the BufferView.
        /// </summary>
        public void TransferToInternalBuffer()
        {
            if (this._SatelliteImageContent == null) return;

            // transfer the external image content to a buffer.
            this._bufferView = this.LogicalParent
                .UseBufferView(this._SatelliteImageContent)
                .LogicalIndex;

            this._uri = null;
            this._mimeType = null;
            this._SatelliteImageContent = null;
        }

        #endregion

        #region binary read

        internal void _ResolveUri(ReadContext context)
        {
            if (!String.IsNullOrWhiteSpace(_uri))
            {
                var data = _LoadImageUnchecked(context, _uri);

                _SatelliteImageContent = data;
                _uri = null;
                _mimeType = null;
            }
        }

        private static Byte[] _LoadImageUnchecked(ReadContext context, string uri)
        {
            return uri._TryParseBase64Unchecked(EMBEDDED_GLTF_BUFFER)
                ?? uri._TryParseBase64Unchecked(EMBEDDED_OCTET_STREAM)
                ?? uri._TryParseBase64Unchecked(EMBEDDED_JPEG_BUFFER)
                ?? uri._TryParseBase64Unchecked(EMBEDDED_PNG_BUFFER)
                ?? uri._TryParseBase64Unchecked(EMBEDDED_DDS_BUFFER)
                ?? context.ReadBytes(uri).ToArray();
        }

        internal void _DiscardContent()
        {
            this._uri = null;
            this._mimeType = null;
            this._bufferView = null;
            this._SatelliteImageContent = null;
        }

        #endregion

        #region binary write

        /// <summary>
        /// Called internally by the serializer when the image content is to be embedded into the JSON document.
        /// </summary>
        internal void _WriteToInternal()
        {
            if (_SatelliteImageContent == null) { _WriteAsBufferView(); return; }

            var mimeContent = Convert.ToBase64String(_SatelliteImageContent, Base64FormattingOptions.None);

            if (_SatelliteImageContent._IsPngImage())
            {
                _mimeType = MIME_PNG;
                _uri = EMBEDDED_PNG_BUFFER + mimeContent;
                return;
            }

            if (_SatelliteImageContent._IsJpgImage())
            {
                _mimeType = MIME_JPG;
                _uri = EMBEDDED_JPEG_BUFFER + mimeContent;
                return;
            }

            if (_SatelliteImageContent._IsDdsImage())
            {
                _mimeType = MIME_DDS;
                _uri = EMBEDDED_DDS_BUFFER + mimeContent;
                return;
            }

            if (_SatelliteImageContent._IsWebpImage())
            {
                _mimeType = MIME_WEBP;
                _uri = EMBEDDED_WEBP_BUFFER + mimeContent;
                return;
            }

            throw new NotImplementedException();
        }

        /// <summary>
        /// Called internally by the serializer when the image content is to be written as an external file
        /// </summary>
        /// <param name="writer">The satellite asset writer</param>
        /// <param name="satelliteUri">A local satellite URI</param>
        internal void _WriteToSatellite(AssetWriter writer, string satelliteUri)
        {
            if (_SatelliteImageContent == null)
            {
                _WriteAsBufferView();
                return;
            }

            if (_SatelliteImageContent._IsPngImage())
            {
                _mimeType = null;
                _uri = satelliteUri += ".png";
                writer(_uri, _SatelliteImageContent.Slice(0) );
                return;
            }

            if (_SatelliteImageContent._IsJpgImage())
            {
                _mimeType = null;
                _uri = satelliteUri += ".jpg";
                writer(_uri, _SatelliteImageContent.Slice(0) );
                return;
            }

            if (_SatelliteImageContent._IsDdsImage())
            {
                _mimeType = null;
                _uri = satelliteUri += ".dds";
                writer(_uri, _SatelliteImageContent.Slice(0) );
                return;
            }

            if (_SatelliteImageContent._IsWebpImage())
            {
                _mimeType = null;
                _uri = satelliteUri += ".webp";
                writer(_uri, _SatelliteImageContent.Slice(0));
                return;
            }

            throw new NotImplementedException();
        }

        private void _WriteAsBufferView()
        {
            Guard.IsTrue(_bufferView.HasValue, nameof(_bufferView));

            var imageContent = GetImageContent();

            if (imageContent._IsPngImage()) { _mimeType = MIME_PNG; return; }
            if (imageContent._IsJpgImage()) { _mimeType = MIME_JPG; return; }
            if (imageContent._IsDdsImage()) { _mimeType = MIME_DDS; return; }
            if (imageContent._IsWebpImage()) { _mimeType = MIME_WEBP; return; }

            throw new NotImplementedException();
        }

        /// <summary>
        /// Called by the serializer immediatelly after
        /// calling <see cref="_WriteToSatellite(AssetWriter, string)"/>
        /// or <see cref="_WriteToInternal"/>
        /// </summary>
        internal void _ClearAfterWrite()
        {
            _mimeType = null;
            _uri = null;
        }

        #endregion

        #region Validation

        protected override void OnValidateReferences(Validation.ValidationContext result)
        {
            base.OnValidateReferences(result);

            result.CheckSchemaIsValidURI("Uri", this._uri);

            result.CheckArrayIndexAccess("BufferView", _bufferView, this.LogicalParent.LogicalBufferViews);
        }

        protected override void OnValidate(ValidationContext result)
        {
            base.OnValidate(result);

            if (this._bufferView.HasValue)
            {
                var bv = result.Root.LogicalBufferViews[this._bufferView ?? 0];
                if (!bv.IsDataBuffer) result.AddLinkError(("BufferView", this._bufferView), "is a GPU target.");
            }
        }

        #endregion
    }

    public partial class ModelRoot
    {
        /// <summary>
        /// Creates a new <see cref="Image"/> instance.
        /// and adds it to <see cref="ModelRoot.LogicalImages"/>.
        /// </summary>
        /// <param name="name">The name of the instance.</param>
        /// <returns>A <see cref="Image"/> instance.</returns>
        public Image CreateImage(string name = null)
        {
            var image = new Image();
            image.Name = name;

            this._images.Add(image);

            return image;
        }

        /// <summary>
        /// Creates or reuses a <see cref="Image"/> instance.
        /// </summary>
        /// <param name="imageContent">An image encoded in PNG, JPEG or DDS</param>
        /// <returns>A <see cref="Image"/> instance.</returns>
        public Image UseImage(BYTES imageContent)
        {
            Guard.NotNullOrEmpty(imageContent, nameof(imageContent));
            Guard.IsTrue(imageContent._IsImage(), nameof(imageContent), $"{nameof(imageContent)} must be a valid image byte stream.");

            foreach (var img in this.LogicalImages)
            {
                var existingContent = img.GetImageContent();
                if (Enumerable.SequenceEqual(existingContent, imageContent)) return img;
            }

            var image = this.CreateImage();
            image.SetSatelliteContent(imageContent.ToArray());
            return image;
        }

        /// <summary>
        /// Transfers all the <see cref="ModelRoot.LogicalImages"/> content into <see cref="BufferView"/> instances
        /// </summary>
        /// <remarks>
        /// Images can be stored in three different ways:
        /// - As satellite files.
        /// - Embedded as MIME64 into the JSON document
        /// - Referenced with <see cref="BufferView"/>
        ///
        /// This call ensures all images will be internalized as <see cref="BufferView"/> instances.
        ///
        /// This action cannot be reversed.
        /// </remarks>
        public void MergeImages()
        {
            foreach (var img in this._images) img.TransferToInternalBuffer();
        }
    }
}
