using System;
using System.Numerics;

namespace SharpGLTF.Schema2
{
    [System.Diagnostics.DebuggerDisplay("LogicalTexture[{_LogicalTextureIndex}]")]
    internal partial class TextureInfo
    {
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

        public TextureTransform Transform
        {
            get => this.GetExtension<TextureTransform>();
        }

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

        #endregion
    }

    [System.Diagnostics.DebuggerDisplay("Normal LogicalTexture[{_LogicalTextureIndex}] x {Scale}")]
    internal sealed partial class MaterialNormalTextureInfo
    {
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
