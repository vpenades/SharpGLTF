using System;
using System.Numerics;

namespace SharpGLTF.Schema2
{
    /// <remarks>
    /// Derived classes:<br/>
    /// - <see cref="MaterialNormalTextureInfo"/><br/>
    /// - <see cref="MaterialOcclusionTextureInfo"/>
    /// </remarks>
    [System.Diagnostics.DebuggerDisplay("LogicalTexture[{_LogicalTextureIndex}]")]
    internal partial class TextureInfo
    {
        #region lifecycle

        public TextureInfo() { }

        public TextureInfo(TextureInfo other)
        {
            if (other == null) throw new ArgumentNullException(nameof(other));

            _index = other._index;
            _texCoord = other._texCoord;

            this.Extras = other.Extras;

            // TODO: should copy all extensions, not only TextureTransform.

            var otherXform = other.GetExtension<TextureTransform>();

            if (otherXform != null && !otherXform.IsDefault)
            {
                var thisXform = other.UseExtension<TextureTransform>();
                otherXform.CopyTo(thisXform);
            }
        }

        #endregion

        #region properties

        internal int _LogicalTextureIndex
        {
            get => _index;
            set => _index = value;
        }

        /// <summary>
        /// Gets or sets the index of texture's TEXCOORD_[index] attribute used for texture coordinate mapping.
        /// </summary>
        public int TextureCoordinate
        {
            get => _texCoord.AsValue(_texCoordDefault);
            set => _texCoord = value.AsNullable(_texCoordDefault, _texCoordMinimum, int.MaxValue);
        }

        public TextureTransform Transform => this.GetExtension<TextureTransform>();

        #endregion

        #region API

        public void SetTransform(Vector2 offset, Vector2 scale, float rotation, int? texCoordOverride = null)
        {
            var xform = new TextureTransform(this)
            {
                TextureCoordinateOverride = texCoordOverride,
                Offset = offset,
                Scale = scale,
                Rotation = rotation
            };

            if (xform.IsDefault) this.RemoveExtensions<TextureTransform>();
            else this.SetExtension(xform);
        }

        #endregion

        #region Validation

        protected override void OnValidateReferences(Validation.ValidationContext validate)
        {
            validate.IsNullOrIndex("Index", _index, validate.Root.LogicalTextures);

            base.OnValidateReferences(validate);
        }

        #endregion
    }

    [System.Diagnostics.DebuggerDisplay("TextureTransform {Offset} {Scale} {Rotation} {TextureCoordinate}")]
    public sealed partial class TextureTransform
    {
        #region lifecycle

        #pragma warning disable CA1801 // Review unused parameters
        internal TextureTransform(TextureInfo parent) { }
        #pragma warning restore CA1801 // Review unused parameters

        #endregion

        #region properties

        public Vector2 Offset
        {
            get => _offset.AsValue(_offsetDefault);
            set => _offset = value.AsNullable(_offsetDefault);
        }

        public Vector2 Scale
        {
            get => _scale.AsValue(_scaleDefault);
            set => _scale = value.AsNullable(_scaleDefault);
        }

        public float Rotation
        {
            get => (float)_rotation.AsValue(_rotationDefault);
            set => _rotation = ((double)value).AsNullable(_rotationDefault);
        }

        /// <summary>
        /// Gets or sets a value that overrides <see cref="TextureInfo.TextureCoordinate"/> if supplied, and if this extension is supported.
        /// </summary>
        public int? TextureCoordinateOverride
        {
            get => _texCoord;
            set => _texCoord = value;
        }

        internal bool IsDefault
        {
            get
            {
                if (_texCoord.HasValue) return false;
                if (_offset.HasValue) return false;
                if (_scale.HasValue) return false;
                if (_rotation.HasValue) return false;
                return true;
            }
        }

        public Matrix3x2 Matrix
        {
            get
            {
                var s = Matrix3x2.CreateScale(Scale);
                var r = Matrix3x2.CreateRotation(-Rotation);
                var t = Matrix3x2.CreateTranslation(Offset);

                return s * r * t;
            }
        }

        #endregion

        #region API

        internal void CopyTo(TextureTransform other)
        {
            if (other == null) throw new ArgumentNullException(nameof(other));
            other.TextureCoordinateOverride = this.TextureCoordinateOverride;
            other.Rotation = this.Rotation;
            other.Offset = this.Offset;
            other.Scale = this.Scale;            
        }

        #endregion
    }

    [System.Diagnostics.DebuggerDisplay("Normal LogicalTexture[{_LogicalTextureIndex}] x {Scale}")]
    internal sealed partial class MaterialNormalTextureInfo
    {
        #region lifecycle

        public MaterialNormalTextureInfo() { }

        public MaterialNormalTextureInfo(MaterialNormalTextureInfo other) :base(other)
        {
            _scale = other._scale;
        }

        #endregion

        #region properties

        public static Single ScaleDefault => (float)_scaleDefault;

        public Single Scale
        {
            get => (Single)this._scale.AsValue(_scaleDefault);
            set => this._scale = ((Double)value).AsNullable(_scaleDefault);
        }

        #endregion
    }

    [System.Diagnostics.DebuggerDisplay("Occlusion LogicalTexture[{_LogicalTextureIndex}] x {Strength}")]
    internal sealed partial class MaterialOcclusionTextureInfo
    {
        #region lifecycle

        public MaterialOcclusionTextureInfo() { }

        public MaterialOcclusionTextureInfo(MaterialOcclusionTextureInfo other) : base(other)
        {
            _strength = other._strength;
        }

        #endregion

        #region properties

        public static Single StrengthDefault => (float)_strengthDefault;

        public Single Strength
        {
            get => (Single)this._strength.AsValue(_strengthDefault);
            set => this._strength = ((Double)value).AsNullable(_strengthDefault, _strengthMinimum, _strengthMaximum);
        }

        #endregion
    }
}
