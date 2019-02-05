using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;

namespace glTF2Sharp.Schema2
{
    [System.Diagnostics.DebuggerDisplay("Texture[{LogicalIndex}] {Name}")]
    internal partial class TextureInfo
    {
        #region properties

        internal int _LogicalTextureIndex
        {
            get => _index;
            set => _index = value;
        }

        public int TextureSet
        {
            get => _texCoord ?? _texCoordDefault;
            set => _texCoord = value.AsNullable(_texCoordDefault, _texCoordMinimum, int.MaxValue);
        }

        #endregion
    }

    [System.Diagnostics.DebuggerDisplay("Normal Texture[{LogicalIndex}] {Name}")]
    internal partial class MaterialNormalTextureInfo
    {
        #region properties

        public Double Scale
        {
            get => this._scale.AsValue(_scaleDefault);
            set => this._scale = value.AsNullable(_scaleDefault);
        }

        #endregion
    }

    [System.Diagnostics.DebuggerDisplay("Occlusion Texture[{LogicalIndex}] {Name}")]
    internal partial class MaterialOcclusionTextureInfo
    {
        #region properties

        public Double Strength
        {
            get => this._strength ?? _strengthDefault;
            set => this._strength = value.AsNullable(_strengthDefault, _strengthMinimum, _strengthMaximum);
        }

        #endregion
    }

    [System.Diagnostics.DebuggerDisplay("Texture[{LogicalIndex}] {Name}")]
    public partial class Texture
    {
        #region properties

        public int LogicalIndex => this.LogicalParent._LogicalTextures.IndexOfReference(this);

        public Sampler Sampler
        {
            get => _sampler.HasValue ? LogicalParent._LogicalSamplers[_sampler.Value] : null;
            set => _sampler = value == null ? null : (int?)LogicalParent._UseLogicalSampler(value);
        }

        public Image Source
        {
            get => _source.HasValue ? LogicalParent._LogicalImages[_source.Value] : null;
            set => _source = value == null ? null : (int?)LogicalParent._UseLogicalImage(value);
        }

        #endregion
    }

    [System.Diagnostics.DebuggerDisplay("Sampler[{LogicalIndex}] {Name}")]
    public partial class Sampler
    {
        #region lifecycle

        internal Sampler() { }

        public Sampler(TextureInterpolationMode mag, TextureMipMapMode min, TextureWrapMode ws, TextureWrapMode wt)
        {
            _magFilter = mag;
            _minFilter = min;
            _wrapS = ws;
            _wrapT = wt;

        }

        #endregion

        #region properties

        public int LogicalIndex => this.LogicalParent._LogicalSamplers.IndexOfReference(this);

        public TextureInterpolationMode MagFilter => _magFilter ?? TextureInterpolationMode.LINEAR;

        public TextureMipMapMode MinFilter => _minFilter ?? TextureMipMapMode.LINEAR;

        public TextureWrapMode WrapS => _wrapS ?? _wrapSDefault;

        public TextureWrapMode WrapT => _wrapT ?? _wrapSDefault;

        #endregion
    }

    [System.Diagnostics.DebuggerDisplay("Image[{LogicalIndex}] {Name}")]
    public partial class Image
    {
        #region Base64 constants

        const string EMBEDDEDOCTETSTREAM = "data:application/octet-stream;base64,";
        const string EMBEDDEDGLTFBUFFER = "data:application/gltf-buffer;base64,";
        const string EMBEDDEDJPEGBUFFER = "data:image/jpeg;base64,";
        const string EMBEDDEDPNGBUFFER = "data:image/png;base64,";

        const string MIMEPNG = "image/png";
        const string MIMEJPEG = "image/jpeg";

        #endregion

        #region data        

        // this is the actual compressed image in PNG or JPEG, -NOT- the pixels data.
        private Byte[] _ExternalImageContent;

        #endregion

        #region properties

        public int LogicalIndex => this.LogicalParent._LogicalImages.IndexOfReference(this);

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

                return bv.Data;
            }

            throw new InvalidOperationException();
        }

        public Image SetExternalContent(Byte[] data)
        {
            if (_IsPng(data)) _mimeType = MIMEPNG; // these strings might be wrong
            if (_IsJpeg(data)) _mimeType = MIMEJPEG; // these strings might be wrong

            this._uri = null;
            this._bufferView = null;
            this._ExternalImageContent = data;

            return this;
        }

        public void UseBufferViewContainer()
        {
            if (this._ExternalImageContent == null) return;

            var b = this.LogicalParent.CreateBuffer(this._ExternalImageContent);
            var bv = this.LogicalParent.CreateBufferView(b, this._ExternalImageContent.Length);

            this._uri = null;
            this._bufferView = bv.LogicalIndex;

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
        internal int _UseLogicalImage(Image image)
        {
            Guard.NotNull(image, nameof(image));

            return _images.Use(image);
        }

        internal int _UseLogicalSampler(Sampler sampler)
        {
            Guard.NotNull(sampler, nameof(sampler));

            return _samplers.Use(sampler);
        }

        internal Image _AddLogicalImage()
        {
            var img = new Image();

            _images.Add(img);

            return img;
        }
        
        public Sampler UseLogicalSampler(TextureInterpolationMode mag, TextureMipMapMode min, TextureWrapMode ws, TextureWrapMode wt)
        {
            foreach (var s in this._samplers)
            {
                if (s.MagFilter == mag && s.MinFilter == min && s.WrapS == ws && s.WrapT == wt) return s;
            }

            var ss = new Sampler(mag, min, ws, wt);

            this._samplers.Add(ss);

            return ss;
        }

        public Texture UseLogicalTexture(Image image, Sampler sampler)
        {
            if (image == null) return null;

            if (image != null) Guard.MustShareLogicalParent(this, image,nameof(image));
            if (sampler != null) Guard.MustShareLogicalParent(this, sampler, nameof(sampler));

            var tex = _textures.FirstOrDefault(item => item.Source == image && item.Sampler == sampler);
            if (tex != null) return tex;

            tex = new Texture();
            _textures.Add(tex);

            tex.Source = image;
            tex.Sampler = sampler;

            return tex;
        }

        internal T UseTextureInfo<T>(Image image, Sampler sampler, int textureSet) where T:TextureInfo, new()
        {
            var tex = UseLogicalTexture(image, sampler);
            if (tex == null) return null;

            return new T
            {
                _LogicalTextureIndex = tex.LogicalIndex,
                TextureSet = textureSet
            };
        }
    }

}
