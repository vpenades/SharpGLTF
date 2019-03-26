using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace SharpGLTF.Geometry.VertexTypes
{
    public interface IVertexMaterial
    {
        void SetColor(int setIndex, Vector4 color);
        void SetTexCoord(int setIndex, Vector2 coord);

        void Validate();
    }

    /// <summary>
    /// Defines a Vertex attribute with a Color material.
    /// </summary>
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

        void IVertexMaterial.SetColor(int setIndex, Vector4 color) { if (setIndex == 0) this.Color = color; }

        void IVertexMaterial.SetTexCoord(int setIndex, Vector2 coord) { }

        public void Validate()
        {
            if (!Color._IsReal()) throw new NotFiniteNumberException(nameof(Color));
            if (!Color.IsInRange(Vector4.Zero, Vector4.One)) throw new IndexOutOfRangeException(nameof(Color));
        }
    }

    /// <summary>
    /// Defines a Vertex attribute with a Texture Coordinate.
    /// </summary>
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

        void IVertexMaterial.SetColor(int setIndex, Vector4 color) { }

        void IVertexMaterial.SetTexCoord(int setIndex, Vector2 coord) { if (setIndex == 0) this.TexCoord = coord; }

        public void Validate()
        {
            if (!TexCoord._IsReal()) throw new NotFiniteNumberException(nameof(TexCoord));
        }
    }

    /// <summary>
    /// Defines a Vertex attribute with a Color material and a Texture Coordinate.
    /// </summary>
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

        void IVertexMaterial.SetColor(int setIndex, Vector4 color) { if (setIndex == 0) this.Color = color; }

        void IVertexMaterial.SetTexCoord(int setIndex, Vector2 coord) { if (setIndex == 0) this.TexCoord = coord; }

        public void Validate()
        {
            if (!Color._IsReal()) throw new NotFiniteNumberException(nameof(Color));
            if (!Color.IsInRange(Vector4.Zero, Vector4.One)) throw new IndexOutOfRangeException(nameof(Color));

            if (!TexCoord._IsReal()) throw new NotFiniteNumberException(nameof(TexCoord));
        }
    }
}
