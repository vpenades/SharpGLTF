using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

using SharpGLTF.Geometry.VertexTypes;

namespace SharpGLTF.Geometry
{
    [System.Diagnostics.DebuggerDisplay("𝐂:{Color} 𝐔𝐕:{TexCoord} _CUSTOM_1:{CustomId}")]
    public struct VertexColor1Texture1Custom1 : IVertexMaterial
    {
        #region constructors

        public VertexColor1Texture1Custom1(Vector4 color, Vector2 tex, Single customId)
        {
            Color = color;
            TexCoord = tex;
            CustomId = customId;
        }

        public VertexColor1Texture1Custom1(IVertexMaterial src)
        {
            this.Color = src.MaxColors > 0 ? src.GetColor(0) : Vector4.One;
            this.TexCoord = src.MaxTextCoords > 0 ? src.GetTexCoord(0) : Vector2.Zero;

            if (src is VertexColor1Texture1Custom1 custom)
            {
                this.CustomId = custom.CustomId;
            }
            else
            {
                this.CustomId = 0;
            }
        }

        public static implicit operator VertexColor1Texture1Custom1((Vector4 color, Vector2 tex, Single customId) tuple)
        {
            return new VertexColor1Texture1Custom1(tuple.color, tuple.tex, tuple.customId);
        }

        #endregion

        #region data

        public const string CUSTOMATTRIBUTENAME = "_CUSTOM_1";

        [VertexAttribute(CUSTOMATTRIBUTENAME, Schema2.EncodingType.FLOAT, false)]
        public Single CustomId;

        [VertexAttribute("COLOR_0", Schema2.EncodingType.UNSIGNED_BYTE, true)]
        public Vector4 Color;

        [VertexAttribute("TEXCOORD_0")]
        public Vector2 TexCoord;

        public int MaxColors => 1;

        public int MaxTextCoords => 1;

        #endregion

        #region API

        void IVertexMaterial.SetColor(int setIndex, Vector4 color) { if (setIndex == 0) this.Color = color; }

        void IVertexMaterial.SetTexCoord(int setIndex, Vector2 coord) { if (setIndex == 0) this.TexCoord = coord; }

        public Vector4 GetColor(int index)
        {
            if (index != 0) throw new ArgumentOutOfRangeException(nameof(index));
            return Color;
        }

        public Vector2 GetTexCoord(int index)
        {
            if (index != 0) throw new ArgumentOutOfRangeException(nameof(index));
            return TexCoord;
        }

        public void Validate() { FragmentPreprocessors.ValidateVertexMaterial(this); }

        public object GetCustomAttribute(string attributeName)
        {
            return attributeName == CUSTOMATTRIBUTENAME ? (Object)CustomId : null;
        }

        #endregion
    }
}
