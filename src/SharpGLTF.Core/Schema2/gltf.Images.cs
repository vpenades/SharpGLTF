using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

using SharpGLTF.Validation;
using BYTES = System.ArraySegment<byte>;

namespace SharpGLTF.Schema2
{
    [System.Diagnostics.DebuggerDisplay("{_DebuggerDisplay(),nq}")]
    public sealed partial class Image
    {
        #region debug

        internal string _DebuggerDisplay()
        {
            return $"Image[{LogicalIndex}] {Name} = {Content.ToDebuggerDisplay()}";
        }

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
        private Memory.MemoryImage? _SatelliteContent;

        #endregion

        #region properties

        /// <summary>
        /// Gets or sets the in-memory representation of the image file.
        /// </summary>
        public Memory.MemoryImage Content
        {
            get => GetSatelliteContent();
            set => SetSatelliteContent(value);
        }

        /// <summary>
        /// When set to a FileName or a relative File Path,
        /// it will be used as the file name of the texture being written.<br/>
        /// When null, a default file name will be used.
        /// </summary>
        /// <remarks>        
        /// <para>
        /// if not sure about the image extension, using .* as extension will replace
        /// the extension with the appropiate one at the time of writing.
        /// </para>
        /// <para>
        /// For more advanced scenarios, you can also use: <see cref="WriteSettings.ImageWriteCallback"/>
        /// </para>
        /// </remarks>
        public String AlternateWriteFileName { get; set; }

        internal int _SourceBufferViewIndex => _bufferView.AsValue(-1);

        internal bool _HasContent
        {
            get
            {
                if (_bufferView != null) return true;
                return _SatelliteContent?.IsValid ?? false;
            }
        }

        #endregion

        #region API

        /// <summary>
        /// Retrieves the image file as a segment of bytes.
        /// </summary>
        /// <returns>A <see cref="BYTES"/> segment containing the image file, which can be a PNG, JPG, DDS, WEBP or KTX2 format.</returns>
        private Memory.MemoryImage GetSatelliteContent()
        {
            // the image is stored locally in a temporary buffer
            if (_SatelliteContent.HasValue) return _SatelliteContent.Value;

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
        /// Initializes this <see cref="Image"/> with an image stored in a <see cref="Byte"/> array.
        /// </summary>
        /// <param name="content">A <see cref="Byte"/> array containing a PNG or JPEG image.</param>
        private void SetSatelliteContent(Memory.MemoryImage content)
        {
            Memory.MemoryImage._Verify(content, nameof(content));

            _DiscardContent();

            this._SatelliteContent = content;
        }

        /// <summary>
        /// If the image is stored externaly as an image file,
        /// it creates a new BufferView and stores the image in the BufferView.
        /// </summary>
        internal void TransferToInternalBuffer()
        {
            if (!this._SatelliteContent.HasValue) return;

            // transfer the external image content to a buffer.
            this._bufferView = this.LogicalParent
                .UseBufferView(this._SatelliteContent.Value._GetBuffer())
                .LogicalIndex;

            this._uri = null;
            this._mimeType = null;
            this._SatelliteContent = default;
        }

        #endregion

        #region binary read

        internal void _ResolveUri(ReadContext context)
        {
            // No uri to decode.
            if (String.IsNullOrWhiteSpace(_uri)) return;

            // Try decode Base64 embedded image.
            if (Memory.MemoryImage.TryParseMime64(_uri, out var memImage))
            {
                _SatelliteContent = memImage;
            }

            // Then it's a regular URI
            else
            {
                // try resolve the full path
                if (context.TryGetFullPath(_uri, out string fullPath))
                {
                    _SatelliteContent = fullPath;
                }

                // full path could not be resolved, use direct load instead.
                else
                {
                    _SatelliteContent = context.ReadAllBytesToEnd(_uri);
                }
            }

            _uri = null;
            _mimeType = null;
        }

        internal void _DiscardContent()
        {
            this._uri = null;
            this._mimeType = null;
            this._bufferView = null;
            this._SatelliteContent = null;
        }

        #endregion

        #region binary write

        /// <summary>
        /// Called internally by the serializer when the image content is to be embedded into the JSON document.
        /// </summary>
        internal void _WriteToInternal()
        {
            if (!_SatelliteContent.HasValue) { _WriteAsBufferView(); return; }

            var imimg = _SatelliteContent.Value;
            Memory.MemoryImage._Verify(imimg, nameof(imimg));

            _uri = imimg.ToMime64();
            _mimeType = imimg.MimeType;
        }

        /// <summary>
        /// Called internally by the serializer when the image content is to be written as an external file
        /// </summary>
        /// <param name="writer">The satellite asset writer</param>
        /// <param name="satelliteUri">A local satellite URI</param>
        internal void _WriteToSatellite(WriteContext writer, string satelliteUri)
        {
            if (!_SatelliteContent.HasValue)
            {
                _WriteAsBufferView();
                return;
            }

            var imimg = _SatelliteContent.Value;
            Memory.MemoryImage._Verify(imimg, nameof(imimg));

            if (string.IsNullOrWhiteSpace(AlternateWriteFileName))
            {
                satelliteUri = System.IO.Path.ChangeExtension(satelliteUri, imimg.FileExtension);
            }
            else
            {
                satelliteUri = AlternateWriteFileName;
                if (satelliteUri.EndsWith(".*"))
                {
                    satelliteUri = System.IO.Path.ChangeExtension(satelliteUri, imimg.FileExtension);
                }

                if (System.IO.Path.IsPathRooted(satelliteUri)) throw new InvalidOperationException(nameof(AlternateWriteFileName) + " must be a relative path");
            }            

            satelliteUri = writer.WriteImage(satelliteUri, imimg);

            satelliteUri = satelliteUri.Replace("\\", "/");
            _uri = Uri.EscapeUriString(satelliteUri);
            _mimeType = null;
        }

        private void _WriteAsBufferView()
        {
            Guard.IsTrue(_bufferView.HasValue, nameof(_bufferView));

            var imimg = this.Content;
            Memory.MemoryImage._Verify(imimg, nameof(imimg));

            _uri = null;
            _mimeType = imimg.MimeType;
        }

        /// <summary>
        /// Called by the serializer immediatelly after
        /// calling <see cref="_WriteToSatellite(Schema2.WriteContext, string)"/>
        /// or <see cref="_WriteToInternal"/>
        /// </summary>
        internal void _ClearAfterWrite()
        {
            _uri = null;
            _mimeType = null;
        }

        #endregion

        #region Validation

        protected override void OnValidateReferences(Validation.ValidationContext validate)
        {
            base.OnValidateReferences(validate);

            validate
                .IsNullOrValidURI(nameof(_uri), this._uri, Memory.MemoryImage._EmbeddedHeaders)
                .IsNullOrIndex("BufferView", _bufferView, validate.Root.LogicalBufferViews);
        }

        protected override void OnValidateContent(ValidationContext validate)
        {
            if (this._bufferView.HasValue)
            {
                var bv = validate.Root.LogicalBufferViews[this._bufferView ?? 0];

                validate.IsTrue("BufferView", bv.IsDataBuffer, "is a GPU target.");
            }

            Memory.MemoryImage._Verify(Content, nameof(Content));
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
        public Image UseImage(Memory.MemoryImage imageContent)
        {
            Memory.MemoryImage._Verify(imageContent, nameof(imageContent));

            // If we find an image with the same content, let's reuse it.
            foreach (var img in this.LogicalImages)
            {
                if (img.Content.Equals(imageContent)) return img;
            }

            var image = this.CreateImage();
            image.Content = imageContent;
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
