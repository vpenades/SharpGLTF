using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SharpGLTF.Schema2
{
    [System.Diagnostics.DebuggerDisplay("Image[{LogicalIndex}] {Name}")]
    public sealed partial class Image
    {
        #region Base64 constants

        const string EMBEDDEDOCTETSTREAM = "data:application/octet-stream;base64,";
        const string EMBEDDEDGLTFBUFFER = "data:application/gltf-buffer;base64,";
        const string EMBEDDEDJPEGBUFFER = "data:image/jpeg;base64,";
        const string EMBEDDEDPNGBUFFER = "data:image/png;base64,";

        const string MIMEPNG = "image/png";
        const string MIMEJPEG = "image/jpeg";

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
        public bool IsPng => _IsPng(GetImageContent());

        /// <summary>
        /// Gets a value indicating whether the contained image is a JPEG image.
        /// </summary>
        public bool IsJpeg => _IsJpeg(GetImageContent());

        #endregion

        #region API

        private static bool _IsPng(IReadOnlyList<Byte> data)
        {
            if (data[0] != 0x89) return false;
            if (data[1] != 0x50) return false;
            if (data[2] != 0x4e) return false;
            if (data[3] != 0x47) return false;

            return true;
        }

        private static bool _IsJpeg(IReadOnlyList<Byte> data)
        {
            if (data[0] != 0xff) return false;
            if (data[1] != 0xd8) return false;

            return true;
        }

        /// <summary>
        /// Retrieves the image file as a block of bytes.
        /// </summary>
        /// <returns>A <see cref="ArraySegment{Byte}"/> block containing the image file.</returns>
        public ArraySegment<Byte> GetImageContent()
        {
            // the image is stored locally in a temporary buffer
            if (_SatelliteImageContent != null) return new ArraySegment<byte>(_SatelliteImageContent);

            /// the image is stored in a <see cref="BufferView"/>
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
        /// <returns>This <see cref="Image"/> instance.</returns>
        public Image WithSatelliteFile(string filePath)
        {
            var content = System.IO.File.ReadAllBytes(filePath);
            return WithSatelliteContent(content);
        }

        /// <summary>
        /// Initializes this <see cref="Image"/> with an image stored in a <see cref="Byte"/> array.
        /// </summary>
        /// <param name="content">A <see cref="Byte"/> array containing a PNG or JPEG image.</param>
        /// <returns>This <see cref="Image"/> instance.</returns>
        public Image WithSatelliteContent(Byte[] content)
        {
            Guard.NotNull(content, nameof(content));

            string imageType = null;

            if (_IsPng(content)) imageType = MIMEPNG; // these strings might be wrong
            if (_IsJpeg(content)) imageType = MIMEJPEG; // these strings might be wrong

            if (imageType == null) throw new ArgumentException($"{nameof(content)} must be a PNG or JPG image", nameof(content));

            this._uri = null;
            this._mimeType = null;
            this._bufferView = null;
            this._SatelliteImageContent = content;

            return this;
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

        internal void _ResolveUri(AssetReader externalReferenceSolver)
        {
            if (!String.IsNullOrWhiteSpace(_uri))
            {
                _SatelliteImageContent = _LoadImageUnchecked(externalReferenceSolver, _uri);
                _uri = null;
                _mimeType = null;
            }
        }

        private static Byte[] _LoadImageUnchecked(AssetReader externalReferenceSolver, string uri)
        {
            return uri._TryParseBase64Unchecked(EMBEDDEDGLTFBUFFER)
                ?? uri._TryParseBase64Unchecked(EMBEDDEDOCTETSTREAM)
                ?? uri._TryParseBase64Unchecked(EMBEDDEDJPEGBUFFER)
                ?? uri._TryParseBase64Unchecked(EMBEDDEDPNGBUFFER)
                ?? externalReferenceSolver.Invoke(uri).ToArray();
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

            if (_IsPng(_SatelliteImageContent))
            {
                _mimeType = MIMEPNG;
                _uri = EMBEDDEDPNGBUFFER + mimeContent;
                return;
            }

            if (_IsJpeg(_SatelliteImageContent))
            {
                _mimeType = MIMEJPEG;
                _uri = EMBEDDEDJPEGBUFFER + mimeContent;
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
            if (_SatelliteImageContent == null) { _WriteAsBufferView(); return; }

            if (_IsPng(_SatelliteImageContent))
            {
                _mimeType = null;
                _uri = satelliteUri += ".png";
                writer(_uri, new ArraySegment<byte>(_SatelliteImageContent) );
                return;
            }

            if (_IsJpeg(_SatelliteImageContent))
            {
                _mimeType = null;
                _uri = satelliteUri += ".jpg";
                writer(_uri, new ArraySegment<byte>(_SatelliteImageContent) );
                return;
            }

            throw new NotImplementedException();

        }

        private void _WriteAsBufferView()
        {
            Guard.IsTrue(this._bufferView.HasValue, nameof(this._bufferView));

            if (_IsPng(GetImageContent())) { _mimeType = MIMEPNG; return; }
            if (_IsJpeg(GetImageContent())) { _mimeType = MIMEJPEG; return; }

            throw new NotImplementedException();
        }

        /// <summary>
        /// Called by the serializer immediatelly after
        /// calling <see cref="_WriteToSatellite(AssetWriter, string)"/>
        /// or <see cref="_WriteToInternal"/>
        /// </summary>
        internal void _ClearAfterWrite()
        {
            this._mimeType = null;
            this._uri = null;
        }

        #endregion
    }

    public partial class ModelRoot
    {
        /// <summary>
        /// Creates a new <see cref="Image"/> instance
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
