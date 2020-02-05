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
        /// Returns the in-memory representation of the image file.
        /// </summary>
        public Memory.MemoryImage MemoryImage => new Memory.MemoryImage(GetImageContent());

        /// <summary>
        /// Gets a value indicating whether the contained image is a PNG image.
        /// </summary>
        [Obsolete("Use MemoryImage property")]
        public bool IsPng => this.MemoryImage.IsPng;

        /// <summary>
        /// Gets a value indicating whether the contained image is a JPEG image.
        /// </summary>
        [Obsolete("Use MemoryImage property")]
        public bool IsJpeg => this.MemoryImage.IsJpg;

        /// <summary>
        /// Gets a value indicating whether the contained image is a DDS image.
        /// </summary>
        [Obsolete("Use MemoryImage property")]
        public bool IsDds => this.MemoryImage.IsDds;

        /// <summary>
        /// Gets a value indicating whether the contained image is a WEBP image.
        /// </summary>
        [Obsolete("Use MemoryImage property")]
        public bool IsWebp => this.MemoryImage.IsWebp;

        /// <summary>
        /// Gets the filename extension of the image that can be retrieved with <see cref="GetImageContent"/>
        /// </summary>
        [Obsolete("Use MemoryImage property")]
        public string FileExtension => this.MemoryImage.FileExtension;

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
        [Obsolete("Use MemoryImage property")]
        public System.IO.Stream OpenImageFile()
        {
            return this.MemoryImage.Open();
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

            var imimg = new Memory.MemoryImage(content);
            if (!imimg.IsValid) throw new ArgumentException($"{nameof(content)} must be a PNG, JPG, DDS or WEBP image", nameof(content));

            string imageType = imimg.MimeType;

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

        internal void _ResolveUri(IO.ReadContext context)
        {
            if (String.IsNullOrWhiteSpace(_uri)) return;

            var data = Memory.MemoryImage.TryParseBytes(_uri);

            if (data == null)
            {
                var bytes = context.ReadAllBytesToEnd(_uri);

                // let's try to avoid making a copy if it's not neccesary.
                if (bytes.Offset == 0 && bytes.Array.Length == bytes.Count) data = bytes.Array;
                else data = bytes.ToArray();
            }

            _SatelliteImageContent = data;
            _uri = null;
            _mimeType = null;
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

            var imimg = new Memory.MemoryImage(_SatelliteImageContent);
            _mimeType = imimg.MimeType;
            _uri = imimg.ToMime64();
        }

        /// <summary>
        /// Called internally by the serializer when the image content is to be written as an external file
        /// </summary>
        /// <param name="writer">The satellite asset writer</param>
        /// <param name="satelliteUri">A local satellite URI</param>
        internal void _WriteToSatellite(IO.WriteContext writer, string satelliteUri)
        {
            if (_SatelliteImageContent == null)
            {
                _WriteAsBufferView();
                return;
            }

            _mimeType = null;

            var imimg = new Memory.MemoryImage(_SatelliteImageContent);
            if (!imimg.IsValid) throw new InvalidOperationException();

            _uri = System.IO.Path.ChangeExtension(satelliteUri, imimg.FileExtension);
            writer.WriteAllBytesToEnd(_uri, imimg.GetBuffer());
        }

        private void _WriteAsBufferView()
        {
            Guard.IsTrue(_bufferView.HasValue, nameof(_bufferView));

            var imimg = this.MemoryImage;
            if (!imimg.IsValid) throw new InvalidOperationException();

            _mimeType = imimg.MimeType;
        }

        /// <summary>
        /// Called by the serializer immediatelly after
        /// calling <see cref="_WriteToSatellite(IO.WriteContext, string)"/>
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
        /// and appends it to <see cref="ModelRoot.LogicalImages"/>.
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
            Guard.IsTrue(new Memory.MemoryImage(imageContent).IsValid, nameof(imageContent), $"{nameof(imageContent)} must be a valid image byte stream.");

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
