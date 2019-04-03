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

    /// <summary>
    /// Defines a Vertex attribute with a Color material and two Texture Coordinates.
    /// </summary>
    public struct VertexColor1Texture2 : IVertexMaterial
    {
        public VertexColor1Texture2(Vector4 color, Vector2 tex0, Vector2 tex1)
        {
            Color = color;
            TexCoord0 = tex0;
            TexCoord1 = tex1;
        }

        [VertexAttribute("COLOR_0", Schema2.EncodingType.UNSIGNED_BYTE, true)]
        public Vector4 Color;

        [VertexAttribute("TEXCOORD_0")]
        public Vector2 TexCoord0;

        [VertexAttribute("TEXCOORD_1")]
        public Vector2 TexCoord1;

        void IVertexMaterial.SetColor(int setIndex, Vector4 color) { if (setIndex == 0) this.Color = color; }

        void IVertexMaterial.SetTexCoord(int setIndex, Vector2 coord)
        {
            if (setIndex == 0) this.TexCoord0 = coord;
            if (setIndex == 1) this.TexCoord1 = coord;
        }

        public void Validate()
        {
            if (!Color._IsReal()) throw new NotFiniteNumberException(nameof(Color));
            if (!Color.IsInRange(Vector4.Zero, Vector4.One)) throw new IndexOutOfRangeException(nameof(Color));

            if (!TexCoord0._IsReal()) throw new NotFiniteNumberException(nameof(TexCoord0));
            if (!TexCoord1._IsReal()) throw new NotFiniteNumberException(nameof(TexCoord1));
        }
    }

    /// <summary>
    /// Defines a Vertex attribute with a Color material and two Texture Coordinates.
    /// </summary>
    public struct VertexColor2Texture2 : IVertexMaterial
    {
        public VertexColor2Texture2(Vector4 color0, Vector4 color1, Vector2 tex0, Vector2 tex1)
        {
            Color0 = color0;
            Color1 = color1;
            TexCoord0 = tex0;
            TexCoord1 = tex1;
        }

        [VertexAttribute("COLOR_0", Schema2.EncodingType.UNSIGNED_BYTE, true)]
        public Vector4 Color0;

        [VertexAttribute("COLOR_1", Schema2.EncodingType.UNSIGNED_BYTE, true)]
        public Vector4 Color1;

        [VertexAttribute("TEXCOORD_0")]
        public Vector2 TexCoord0;

        [VertexAttribute("TEXCOORD_1")]
        public Vector2 TexCoord1;

        void IVertexMaterial.SetColor(int setIndex, Vector4 color)
        {
            if (setIndex == 0) this.Color0 = color;
            if (setIndex == 1) this.Color1 = color;
        }

        void IVertexMaterial.SetTexCoord(int setIndex, Vector2 coord)
        {
            if (setIndex == 0) this.TexCoord0 = coord;
            if (setIndex == 1) this.TexCoord1 = coord;
        }

        public void Validate()
        {
            if (!Color0._IsReal()) throw new NotFiniteNumberException(nameof(Color0));
            if (!Color0.IsInRange(Vector4.Zero, Vector4.One)) throw new IndexOutOfRangeException(nameof(Color0));

            if (!Color1._IsReal()) throw new NotFiniteNumberException(nameof(Color1));
            if (!Color1.IsInRange(Vector4.Zero, Vector4.One)) throw new IndexOutOfRangeException(nameof(Color1));

            if (!TexCoord0._IsReal()) throw new NotFiniteNumberException(nameof(TexCoord0));
            if (!TexCoord1._IsReal()) throw new NotFiniteNumberException(nameof(TexCoord1));
        }
    }
}
