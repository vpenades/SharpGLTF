using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace SharpGLTF.Geometry.VertexTypes
{
    public interface IVertexPosition
    {
        void Validate();
    }

    public struct VertexPosition : IVertexPosition
    {
        public VertexPosition(Vector3 position)
        {
            this.Position = position;
        }

        public VertexPosition(float px, float py, float pz)
        {
            this.Position = new Vector3(px, py, pz);
        }

        public static implicit operator VertexPosition(Vector3 position)
        {
            return new VertexPosition(position);
        }

        [VertexAttribute("POSITION")]
        public Vector3 Position;

        public void Validate()
        {
            if (!Position._IsReal()) throw new NotFiniteNumberException(nameof(Position));
        }
    }

    public struct VertexPositionNormal : IVertexPosition
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

        public void Validate()
        {
            if (!Position._IsReal()) throw new NotFiniteNumberException(nameof(Position));
            if (!Normal._IsReal()) throw new NotFiniteNumberException(nameof(Normal));
        }
    }

    public struct VertexPositionNormalTangent : IVertexPosition
    {
        public VertexPositionNormalTangent(Vector3 p, Vector3 n, Vector4 t)
        {
            Position = p;
            Normal = Vector3.Normalize(n);
            Tangent = t;
        }

        [VertexAttribute("POSITION")]
        public Vector3 Position;

        [VertexAttribute("NORMAL")]
        public Vector3 Normal;

        [VertexAttribute("TANGENT")]
        public Vector4 Tangent;

        public void Validate()
        {
            if (!Position._IsReal()) throw new NotFiniteNumberException(nameof(Position));
            if (!Normal._IsReal()) throw new NotFiniteNumberException(nameof(Normal));
            if (!Tangent._IsReal()) throw new NotFiniteNumberException(nameof(Tangent));
        }
    }
}
