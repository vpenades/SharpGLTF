using System;
using System.Collections.Generic;
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
        /// field, and the <see cref="Image._uri"/> is nullified.
        /// When writing a gltf file with external images, the <see cref="Image._uri"/> is
        /// briefly reassigned so the JSON can be serialized correctly.
        /// After serialization <see cref="Image._uri"/> is set back to null.
        /// </remarks>
        private Byte[] _ExternalImageContent;

        #endregion

        #region properties

        /// <summary>
        /// Gets the zero-based index of this <see cref="Image"/> at <see cref="ModelRoot.LogicalImages"/>
        /// </summary>
        public int LogicalIndex => this.LogicalParent.LogicalImages.IndexOfReference(this);

        public bool IsPng => string.IsNullOrWhiteSpace(_mimeType) ? false : _mimeType.Contains("png");
        public bool IsJpeg => string.IsNullOrWhiteSpace(_mimeType) ? false : _mimeType.Contains("jpg") | _mimeType.Contains("jpeg");

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

        public ArraySegment<Byte> GetImageContent()
        {
            if (_ExternalImageContent != null) return new ArraySegment<byte>(_ExternalImageContent);

            if (this._bufferView.HasValue)
            {
                var bv = this.LogicalParent.LogicalBufferViews[this._bufferView.Value];

                return bv.Content;
            }

            throw new InvalidOperationException();
        }

        public Image WithExternalFile(string filePath)
        {
            var content = System.IO.File.ReadAllBytes(filePath);
            return WithExternalContent(content);
        }

        public Image WithExternalContent(Byte[] content)
        {
            if (_IsPng(content)) _mimeType = MIMEPNG; // these strings might be wrong
            if (_IsJpeg(content)) _mimeType = MIMEJPEG; // these strings might be wrong

            this._uri = null;
            this._bufferView = null;
            this._ExternalImageContent = content;

            return this;
        }

        public void UseBufferViewContainer()
        {
            if (this._ExternalImageContent == null) return;

            // transfer the external image content to a buffer.
            this._bufferView = this.LogicalParent
                .UseBufferView(this._ExternalImageContent)
                .LogicalIndex;

            this._uri = null;
            this._ExternalImageContent = null;
        }

        #endregion

        #region binary read

        internal void _ResolveUri(AssetReader externalReferenceSolver)
        {
            if (!String.IsNullOrWhiteSpace(_uri))
            {
                _ExternalImageContent = _LoadImageUnchecked(_uri, externalReferenceSolver);
            }

            _uri = null; // When _Data is not empty, clear URI
        }

        private static Byte[] _LoadImageUnchecked(string uri, AssetReader externalReferenceSolver)
        {
            return uri._TryParseBase64Unchecked(EMBEDDEDGLTFBUFFER)
                ?? uri._TryParseBase64Unchecked(EMBEDDEDOCTETSTREAM)
                ?? uri._TryParseBase64Unchecked(EMBEDDEDJPEGBUFFER)
                ?? uri._TryParseBase64Unchecked(EMBEDDEDPNGBUFFER)
                ?? externalReferenceSolver?.Invoke(uri);
        }

        #endregion

        #region binary write

        internal void _EmbedAssets()
        {
            if (_ExternalImageContent != null)
            {
                var mimeContent = Convert.ToBase64String(_ExternalImageContent, Base64FormattingOptions.None);

                if (_IsPng(_ExternalImageContent))
                {
                    _mimeType = MIMEPNG;
                    _uri = EMBEDDEDPNGBUFFER + mimeContent;
                    return;
                }

                if (_IsJpeg(_ExternalImageContent))
                {
                    _mimeType = MIMEJPEG;
                    _uri = EMBEDDEDJPEGBUFFER + mimeContent;
                    return;
                }

                throw new NotImplementedException();
            }
        }

        internal void _WriteExternalAssets(string uri, AssetWriter writer)
        {
            if (_ExternalImageContent != null)
            {
                if (this._mimeType.Contains("png")) uri += ".png";
                if (this._mimeType.Contains("jpg")) uri += ".jpg";
                if (this._mimeType.Contains("jpeg")) uri += ".jpg";

                this._uri = uri;
                writer(uri, _ExternalImageContent);
            }
        }

        internal void _ClearAfterWrite() { this._uri = null; }

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
    }
}
