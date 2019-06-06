using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace SharpGLTF.Geometry.VertexTypes
{
    public interface IVertexGeometry
    {
        void Validate();

        Vector3 GetPosition();
        Boolean TryGetNormal(out Vector3 normal);
        Boolean TryGetTangent(out Vector4 tangent);

        void SetPosition(Vector3 position);
        void SetNormal(Vector3 normal);
        void SetTangent(Vector4 tangent);

        void ApplyTransform(Matrix4x4 xform);
    }

    /// <summary>
    /// Defines a Vertex attribute with a Position.
    /// </summary>
    [System.Diagnostics.DebuggerDisplay("𝐏:{Position}")]
    public struct VertexPosition : IVertexGeometry
    {
        #region constructors

        public VertexPosition(Vector3 position)
        {
            this.Position = position;
        }

        public VertexPosition(float px, float py, float pz)
        {
            this.Position = new Vector3(px, py, pz);
        }

        public VertexPosition(IVertexGeometry src)
        {
            this.Position = src.GetPosition();
        }

        public static implicit operator VertexPosition(Vector3 position)
        {
            return new VertexPosition(position);
        }

        #endregion

        #region data

        [VertexAttribute("POSITION")]
        public Vector3 Position;

        #endregion

        #region API

        void IVertexGeometry.SetPosition(Vector3 position) { this.Position = position; }

        void IVertexGeometry.SetNormal(Vector3 normal) { }

        void IVertexGeometry.SetTangent(Vector4 tangent) { }

        public Vector3 GetPosition() { return this.Position; }

        public bool TryGetNormal(out Vector3 normal) { normal = default; return false; }

        public bool TryGetTangent(out Vector4 tangent) { tangent = default; return false; }

        public void ApplyTransform(Matrix4x4 xform)
        {
            Position = Vector3.Transform(Position, xform);
        }

        public void Validate() { FragmentPreprocessors.ValidateVertexGeometry(this); }

        #endregion
    }

    /// <summary>
    /// Defines a Vertex attribute with a Position and a Normal.
    /// </summary>
    [System.Diagnostics.DebuggerDisplay("𝐏:{Position} 𝚴:{Normal}")]
    public struct VertexPositionNormal : IVertexGeometry
    {
        #region constructors

        public VertexPositionNormal(Vector3 p, Vector3 n)
        {
            this.Position = p;
            this.Normal = n;
        }

        public VertexPositionNormal(float px, float py, float pz, float nx, float ny, float nz)
        {
            this.Position = new Vector3(px, py, pz);
            this.Normal = new Vector3(nx, ny, nz);
        }

        public VertexPositionNormal(IVertexGeometry src)
        {
            this.Position = src.GetPosition();
            src.TryGetNormal(out this.Normal);
        }

        #endregion

        #region data

        [VertexAttribute("POSITION")]
        public Vector3 Position;

        [VertexAttribute("NORMAL")]
        public Vector3 Normal;

        #endregion

        #region API

        void IVertexGeometry.SetPosition(Vector3 position) { this.Position = position; }

        void IVertexGeometry.SetNormal(Vector3 normal) { this.Normal = normal; }

        void IVertexGeometry.SetTangent(Vector4 tangent) { }

        public Vector3 GetPosition() { return this.Position; }

        public bool TryGetNormal(out Vector3 normal) { normal = this.Normal; return true; }

        public bool TryGetTangent(out Vector4 tangent) { tangent = default; return false; }

        public void ApplyTransform(Matrix4x4 xform)
        {
            Position = Vector3.Transform(Position, xform);
            Normal = Vector3.Normalize(Vector3.TransformNormal(Normal, xform));
        }

        public void Validate() { FragmentPreprocessors.ValidateVertexGeometry(this); }

        #endregion
    }

    /// <summary>
    /// Defines a Vertex attribute with a Position, a Normal and a Tangent.
    /// </summary>
    [System.Diagnostics.DebuggerDisplay("𝐏:{Position} 𝚴:{Normal} 𝚻:{Tangent}")]
    public struct VertexPositionNormalTangent : IVertexGeometry
    {
        #region constructors

        public VertexPositionNormalTangent(Vector3 p, Vector3 n, Vector4 t)
        {
            this.Position = p;
            this.Normal = n;
            this.Tangent = t;
        }

        public VertexPositionNormalTangent(IVertexGeometry src)
        {
            this.Position = src.GetPosition();
            src.TryGetNormal(out this.Normal);
            src.TryGetTangent(out this.Tangent);
        }

        #endregion

        #region data

        [VertexAttribute("POSITION")]
        public Vector3 Position;

        [VertexAttribute("NORMAL")]
        public Vector3 Normal;

        [VertexAttribute("TANGENT")]
        public Vector4 Tangent;

        #endregion

        #region API

        void IVertexGeometry.SetPosition(Vector3 position) { this.Position = position; }

        void IVertexGeometry.SetNormal(Vector3 normal) { this.Normal = normal; }

        void IVertexGeometry.SetTangent(Vector4 tangent) { this.Tangent = tangent; }

        public Vector3 GetPosition() { return this.Position; }

        public bool TryGetNormal(out Vector3 normal) { normal = this.Normal; return true; }

        public bool TryGetTangent(out Vector4 tangent) { tangent = this.Tangent; return true; }

        public void ApplyTransform(Matrix4x4 xform)
        {
            Position = Vector3.Transform(Position, xform);
            Normal = Vector3.Normalize(Vector3.TransformNormal(Normal, xform));

            // TODO: not sure if this is correct, must be checked. Most probably, if the xform handedness if negative, Tangent.W must be reversed.
            var txyz = Vector3.Normalize(Vector3.TransformNormal(new Vector3(Tangent.X, Tangent.Y, Tangent.Z), xform));
            Tangent = new Vector4(txyz, Tangent.W);
        }

        public void Validate() { FragmentPreprocessors.ValidateVertexGeometry(this); }

        #endregion
    }
}
