using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace SharpGLTF.Geometry.VertexTypes
{
    public interface IVertex { }

    public struct VertexPosition : IVertex
    {
        public VertexPosition(float px, float py, float pz)
        {
            Position = new Vector3(px, py, pz);
        }

        [VertexAttribute("POSITION")]
        public Vector3 Position;
    }

    public struct VertexPositionNormal : IVertex
    {
        public VertexPositionNormal(Vector3 p, Vector3 n)
        {
            Position = p;
            Normal = Vector3.Normalize(n);
        }

        public VertexPositionNormal(float px, float py, float pz, float nx, float ny, float nz)
        {
            Position = new Vector3(px, py, pz);
            Normal = Vector3.Normalize(new Vector3(nx, ny, nz));
        }

        [VertexAttribute("POSITION")]
        public Vector3 Position;

        [VertexAttribute("NORMAL")]
        public Vector3 Normal;
    }

    public struct VertexPositionNormalColor1 : IVertex
    {
        public VertexPositionNormalColor1(Vector3 pos, Vector3 nrm, Vector4 color)
        {
            Position = pos;
            Normal = Vector3.Normalize(nrm);
            Color = color;
        }

        [VertexAttribute("POSITION")]
        public Vector3 Position;

        [VertexAttribute("NORMAL")]
        public Vector3 Normal;

        [VertexAttribute("COLOR_0")]
        public Vector4 Color;
    }

    public struct VertexPositionNormalTexture1 : IVertex
    {
        public VertexPositionNormalTexture1(Vector3 pos, Vector3 nrm, Vector2 tex)
        {
            Position = pos;
            Normal = Vector3.Normalize(nrm);
            TexCoord = tex;
        }

        [VertexAttribute("POSITION")]
        public Vector3 Position;

        [VertexAttribute("NORMAL")]
        public Vector3 Normal;

        [VertexAttribute("TEXCOORD_0")]
        public Vector2 TexCoord;
    }
}
