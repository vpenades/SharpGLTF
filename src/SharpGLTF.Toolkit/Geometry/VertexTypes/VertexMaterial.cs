using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace SharpGLTF.Geometry.VertexTypes
{
    public interface IVertexMaterial
    {
        void Validate();
    }

    public struct VertexColor1 : IVertexMaterial
    {
        public VertexColor1(Vector4 color)
        {
            Color = color;
        }

        public static implicit operator VertexColor1(Vector4 color)
        {
            return new VertexColor1(color);
        }

        [VertexAttribute("COLOR_0", Schema2.EncodingType.UNSIGNED_BYTE, true)]
        public Vector4 Color;

        public void Validate()
        {
            if (!Color._IsReal()) throw new NotFiniteNumberException(nameof(Color));
        }
    }

    public struct VertexTexture1 : IVertexMaterial
    {
        public VertexTexture1(Vector2 uv)
        {
            TexCoord = uv;
        }

        public static implicit operator VertexTexture1(Vector2 uv)
        {
            return new VertexTexture1(uv);
        }

        [VertexAttribute("TEXCOORD_0")]
        public Vector2 TexCoord;

        public void Validate()
        {
            if (!TexCoord._IsReal()) throw new NotFiniteNumberException(nameof(TexCoord));
        }
    }

    public struct VertexColor1Texture1 : IVertexMaterial
    {
        public VertexColor1Texture1(Vector4 color, Vector2 tex)
        {
            Color = color;
            TexCoord = tex;
        }

        [VertexAttribute("COLOR_0", Schema2.EncodingType.UNSIGNED_BYTE, true)]
        public Vector4 Color;

        [VertexAttribute("TEXCOORD_0")]
        public Vector2 TexCoord;

        public void Validate()
        {
            if (!Color._IsReal()) throw new NotFiniteNumberException(nameof(Color));
            if (!TexCoord._IsReal()) throw new NotFiniteNumberException(nameof(TexCoord));
        }
    }
}
