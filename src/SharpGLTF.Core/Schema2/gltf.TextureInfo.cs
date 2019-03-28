using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace SharpGLTF.Schema2
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

        public TextureTransform Transform
        {
            get => this.GetExtension<TextureTransform>();
        }

        public virtual Single Factor
        {
            get { return 1; }
            set { }
        }

        #endregion

        #region API

        public void SetTransform(int texCoord, Vector2 offset, Vector2 scale, float rotation)
        {
            var xform = new TextureTransform(this)
            {
                TextureCoordinate = texCoord,
                Offset = offset,
                Scale = scale,
                Rotation = rotation
            };

            if (xform.IsDefault) this.RemoveExtensions<TextureTransform>();
            else this.SetExtension(xform);
        }

        #endregion
    }

    [System.Diagnostics.DebuggerDisplay("Normal Texture[{LogicalIndex}] {Name}")]
    internal sealed partial class MaterialNormalTextureInfo
    {
        #region properties

        public override Single Factor
        {
            get => (Single)this._scale.AsValue(_scaleDefault);
            set => this._scale = ((Double)value).AsNullable(_scaleDefault);
        }

        #endregion
    }

    [System.Diagnostics.DebuggerDisplay("Occlusion Texture[{LogicalIndex}] {Name}")]
    internal sealed partial class MaterialOcclusionTextureInfo
    {
        #region properties

        public override Single Factor
        {
            get => (Single)this._strength.AsValue(_strengthDefault);
            set => this._strength = ((Double)value).AsNullable(_strengthDefault, _strengthMinimum, _strengthMaximum);
        }

        #endregion
    }
}
