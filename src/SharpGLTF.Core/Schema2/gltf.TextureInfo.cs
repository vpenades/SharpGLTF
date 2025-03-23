using System;
using System.Linq;
using System.Numerics;

namespace SharpGLTF.Schema2
{
    /// <remarks>
    /// Derived classes:<br/>
    /// - <see cref="MaterialNormalTextureInfo"/><br/>
    /// - <see cref="MaterialOcclusionTextureInfo"/>
    /// </remarks>
    [System.Diagnostics.DebuggerDisplay("LogicalTexture[{_LogicalTextureIndex}]")]
    public partial class TextureInfo : Collections.IChildOf<Material>
    {
        #region lifecycle

        public TextureInfo() { }        

        void Collections.IChildOf<Material>.SetLogicalParent(Material parent)
        {
            this.LogicalParent = parent;
        }

        [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)]
        public Material LogicalParent { get; private set; }

        #endregion

        #region properties

        /// <summary>
        /// Index into <see cref="ModelRoot.LogicalTextures"/>
        /// </summary>
        public int LogicalTextureIndex
        {
            get => _index;
            internal protected set => _index = value;
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

            if (xform.IsDefault) this.RemoveExtensions<TextureTransform>(); // TODO: this may no longer be valid because KHR_animation_pointer requires the object to exist.
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

    [System.Diagnostics.DebuggerDisplay("Normal LogicalTexture[{_LogicalTextureIndex}] x {Scale}")]
    internal sealed partial class MaterialNormalTextureInfo
    {
        #region lifecycle

        public MaterialNormalTextureInfo() { }        

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
