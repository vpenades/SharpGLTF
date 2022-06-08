using System;
using System.Collections.Generic;
using System.Numerics;
using System.Runtime.Serialization;
using System.Text;

namespace SharpGLTF.Geometry.VertexTypes
{
    /// <summary>
    /// Represents the interface that must be implemented by a geometry vertex fragment.
    /// </summary>
    /// <remarks>
    /// Implemented by:
    /// <list type="table">
    /// <item><see cref="VertexPosition"/></item>
    /// <item><see cref="VertexPositionNormal"/></item>
    /// <item><see cref="VertexPositionNormalTangent"/></item>
    /// <item><see cref="VertexGeometryDelta"/></item>
    /// </list>
    /// </remarks>
    public interface IVertexGeometry
    {
        /// <summary>
        /// Gets the position of the vertex.
        /// </summary>
        /// <returns>A <see cref="Vector3"/> position.</returns>
        Vector3 GetPosition();

        /// <summary>
        /// Tries to get the normal of the vertex.
        /// </summary>
        /// <param name="normal">A <see cref="Vector3"/> normal.</param>
        /// <returns>True if the normal exists.</returns>
        Boolean TryGetNormal(out Vector3 normal);

        /// <summary>
        /// Tries to get the tangent of the vertex.
        /// </summary>
        /// <param name="tangent">A <see cref="Vector4"/> tangent.</param>
        /// <returns>True if the tangent exists.</returns>
        Boolean TryGetTangent(out Vector4 tangent);

        /// <summary>
        /// Sets the position of the vertex.
        /// <para><b>⚠️ USE ONLY ON UNBOXED VALUES ⚠️</b></para>
        /// </summary>
        /// <param name="position">A <see cref="Vector3"/> position.</param>
        void SetPosition(in Vector3 position);

        /// <summary>
        /// Sets the normal of the vertex.
        /// <para><b>⚠️ USE ONLY ON UNBOXED VALUES ⚠️</b></para>
        /// </summary>
        /// <param name="normal">A <see cref="Vector3"/> normal.</param>
        void SetNormal(in Vector3 normal);

        /// <summary>
        /// Sets the tangent of the vertex.
        /// <para><b>⚠️ USE ONLY ON UNBOXED VALUES ⚠️</b></para>
        /// </summary>
        /// <param name="tangent">A <see cref="Vector4"/> tangent.</param>
        void SetTangent(in Vector4 tangent);

        /// <summary>
        /// Applies a transform to the position, the normal and the tangent of this vertex.
        /// <para><b>⚠️ USE ONLY ON UNBOXED VALUES ⚠️</b></para>
        /// </summary>
        /// <param name="xform">a valid <see cref="Matrix4x4"/> transform.</param>
        void ApplyTransform(in Matrix4x4 xform);

        /// <summary>
        /// calculates the difference between this vertex and <paramref name="baseValue"/>
        /// </summary>
        /// <param name="baseValue">The other vertex.</param>
        /// <returns>The <see cref="VertexGeometryDelta"/> value to subtract.</returns>
        VertexGeometryDelta Subtract(IVertexGeometry baseValue);

        /// <summary>
        /// Adds a vertex delta to this value.
        /// <para><b>⚠️ USE ONLY ON UNBOXED VALUES ⚠️</b></para>
        /// </summary>
        /// <param name="delta">The <see cref="VertexGeometryDelta"/> value to add.</param>
        void Add(in VertexGeometryDelta delta);
    }

    /// <summary>
    /// Defines a Vertex attribute with a Position.
    /// </summary>
    [System.Diagnostics.DebuggerDisplay("{_GetDebuggerDisplay(),nq}")]
    public struct VertexPosition : IVertexGeometry, IEquatable<VertexPosition>
    {
        #region debug

        private string _GetDebuggerDisplay() => VertexUtils._GetDebuggerDisplay(this);

        #endregion

        #region constructors

        public VertexPosition(in Vector3 position)
        {
            this.Position = position;
        }

        public VertexPosition(float px, float py, float pz)
        {
            this.Position = new Vector3(px, py, pz);
        }

        public VertexPosition(IVertexGeometry src)
        {
            Guard.NotNull(src, nameof(src));
            this.Position = src.GetPosition();
        }

        public static implicit operator VertexPosition(in Vector3 position)
        {
            return new VertexPosition(position);
        }

        #endregion

        #region data

        [VertexAttribute("POSITION")]
        public Vector3 Position;

        /// <inheritdoc/>
        public readonly override int GetHashCode() { return Position.GetHashCode(); }

        /// <inheritdoc/>
        public readonly override bool Equals(object obj) { return obj is VertexPosition other && AreEqual(this, other); }

        /// <inheritdoc/>
        public readonly bool Equals(VertexPosition other) { return AreEqual(this, other); }
        public static bool operator ==(in VertexPosition a, in VertexPosition b) { return AreEqual(a, b); }
        public static bool operator !=(in VertexPosition a, in VertexPosition b) { return !AreEqual(a, b); }
        public static bool AreEqual(in VertexPosition a, in VertexPosition b)
        {
            return a.Position == b.Position;
        }        

        #endregion

        #region API

        void IVertexGeometry.SetPosition(in Vector3 position) { this.Position = position; }

        void IVertexGeometry.SetNormal(in Vector3 normal) { }

        void IVertexGeometry.SetTangent(in Vector4 tangent) { }

        /// <inheritdoc/>
        public VertexGeometryDelta Subtract(IVertexGeometry baseValue)
        {
            return new VertexGeometryDelta((VertexPosition)baseValue, this);
        }

        /// <inheritdoc/>
        public void Add(in VertexGeometryDelta delta)
        {
            this.Position += delta.PositionDelta;
        }

        /// <inheritdoc/>
        public readonly Vector3 GetPosition() { return this.Position; }

        /// <inheritdoc/>
        public readonly bool TryGetNormal(out Vector3 normal) { normal = default; return false; }

        /// <inheritdoc/>
        public readonly bool TryGetTangent(out Vector4 tangent) { tangent = default; return false; }

        /// <inheritdoc/>
        public void ApplyTransform(in Matrix4x4 xform)
        {
            Position = Vector3.Transform(Position, xform);
        }

        #endregion
    }

    /// <summary>
    /// Defines a Vertex attribute with a Position and a Normal.
    /// </summary>
    [System.Diagnostics.DebuggerDisplay("{_GetDebuggerDisplay(),nq}")]
    public struct VertexPositionNormal : IVertexGeometry, IEquatable<VertexPositionNormal>
    {
        #region debug

        private string _GetDebuggerDisplay() => VertexUtils._GetDebuggerDisplay(this);

        #endregion

        #region constructors

        public VertexPositionNormal(in Vector3 p, in Vector3 n)
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
            Guard.NotNull(src, nameof(src));

            this.Position = src.GetPosition();
            src.TryGetNormal(out this.Normal);
        }

        public static implicit operator VertexPositionNormal(in (Vector3 Pos, Vector3 Nrm) tuple)
        {
            return new VertexPositionNormal(tuple.Pos, tuple.Nrm);
        }

        #endregion

        #region data

        [VertexAttribute("POSITION")]
        public Vector3 Position;

        [VertexAttribute("NORMAL")]
        public Vector3 Normal;

        /// <inheritdoc/>
        public readonly override int GetHashCode() { return Position.GetHashCode(); }

        /// <inheritdoc/>
        public readonly override bool Equals(object obj) { return obj is VertexPositionNormal other && AreEqual(this, other); }

        /// <inheritdoc/>
        public readonly bool Equals(VertexPositionNormal other) { return AreEqual(this, other); }
        public static bool operator ==(in VertexPositionNormal a, in VertexPositionNormal b) { return AreEqual(a, b); }
        public static bool operator !=(in VertexPositionNormal a, in VertexPositionNormal b) { return !AreEqual(a, b); }
        public static bool AreEqual(in VertexPositionNormal a, in VertexPositionNormal b)
        {
            return a.Position == b.Position && a.Normal == b.Normal;
        }        

        #endregion

        #region API

        void IVertexGeometry.SetPosition(in Vector3 position) { this.Position = position; }

        void IVertexGeometry.SetNormal(in Vector3 normal) { this.Normal = normal; }

        void IVertexGeometry.SetTangent(in Vector4 tangent) { }

        /// <inheritdoc/>
        public VertexGeometryDelta Subtract(IVertexGeometry baseValue)
        {
            return new VertexGeometryDelta((VertexPositionNormal)baseValue, this);
        }

        /// <inheritdoc/>
        public void Add(in VertexGeometryDelta delta)
        {
            this.Position += delta.PositionDelta;
            this.Normal += delta.NormalDelta;
        }

        /// <inheritdoc/>
        public readonly Vector3 GetPosition() { return this.Position; }

        /// <inheritdoc/>
        public readonly bool TryGetNormal(out Vector3 normal) { normal = this.Normal; return true; }

        /// <inheritdoc/>
        public readonly bool TryGetTangent(out Vector4 tangent) { tangent = default; return false; }

        /// <inheritdoc/>
        public void ApplyTransform(in Matrix4x4 xform)
        {
            Position = Vector3.Transform(Position, xform);
            Normal = Vector3.Normalize(Vector3.TransformNormal(Normal, xform));
        }

        #endregion
    }

    /// <summary>
    /// Defines a Vertex attribute with a Position, a Normal and a Tangent.
    /// </summary>
    [System.Diagnostics.DebuggerDisplay("{_GetDebuggerDisplay(),nq}")]
    public struct VertexPositionNormalTangent : IVertexGeometry, IEquatable<VertexPositionNormalTangent>
    {
        #region debug

        private readonly string _GetDebuggerDisplay() => VertexUtils._GetDebuggerDisplay(this);

        #endregion

        #region constructors

        public VertexPositionNormalTangent(in Vector3 p, in Vector3 n, in Vector4 t)
        {
            this.Position = p;
            this.Normal = n;
            this.Tangent = t;
        }

        public VertexPositionNormalTangent(IVertexGeometry src)
        {
            Guard.NotNull(src, nameof(src));

            this.Position = src.GetPosition();
            src.TryGetNormal(out this.Normal);
            src.TryGetTangent(out this.Tangent);
        }

        public static implicit operator VertexPositionNormalTangent(in (Vector3 Pos, Vector3 Nrm, Vector4 Tgt) tuple)
        {
            return new VertexPositionNormalTangent(tuple.Pos, tuple.Nrm, tuple.Tgt);
        }

        #endregion

        #region data

        [VertexAttribute("POSITION")]
        public Vector3 Position;

        [VertexAttribute("NORMAL")]
        public Vector3 Normal;

        [VertexAttribute("TANGENT")]
        public Vector4 Tangent;

        /// <inheritdoc/>
        public readonly override int GetHashCode() { return Position.GetHashCode(); }

        /// <inheritdoc/>
        public readonly override bool Equals(object obj) { return obj is VertexPositionNormalTangent other && AreEqual(this, other); }

        /// <inheritdoc/>
        public readonly bool Equals(VertexPositionNormalTangent other) { return AreEqual(this, other); }
        public static bool operator ==(in VertexPositionNormalTangent a, in VertexPositionNormalTangent b) { return AreEqual(a, b); }
        public static bool operator !=(in VertexPositionNormalTangent a, in VertexPositionNormalTangent b) { return !AreEqual(a, b); }
        public static bool AreEqual(in VertexPositionNormalTangent a, in VertexPositionNormalTangent b)
        {
            return a.Position == b.Position && a.Normal == b.Normal && a.Tangent == b.Tangent;
        }        

        #endregion

        #region API

        void IVertexGeometry.SetPosition(in Vector3 position) { this.Position = position; }

        void IVertexGeometry.SetNormal(in Vector3 normal) { this.Normal = normal; }

        void IVertexGeometry.SetTangent(in Vector4 tangent) { this.Tangent = tangent; }

        /// <inheritdoc/>
        public readonly VertexGeometryDelta Subtract(IVertexGeometry baseValue)
        {
            return new VertexGeometryDelta((VertexPositionNormalTangent)baseValue, this);
        }

        /// <inheritdoc/>
        public void Add(in VertexGeometryDelta delta)
        {
            this.Position += delta.PositionDelta;
            this.Normal += delta.NormalDelta;
            this.Tangent += new Vector4(delta.TangentDelta, 0);
        }

        /// <inheritdoc/>
        public readonly Vector3 GetPosition() { return this.Position; }

        /// <inheritdoc/>
        public readonly bool TryGetNormal(out Vector3 normal) { normal = this.Normal; return true; }

        /// <inheritdoc/>
        public readonly bool TryGetTangent(out Vector4 tangent) { tangent = this.Tangent; return true; }

        /// <inheritdoc/>
        public void ApplyTransform(in Matrix4x4 xform)
        {
            Position = Vector3.Transform(Position, xform);
            Normal = Vector3.Normalize(Vector3.TransformNormal(Normal, xform));

            // TODO: not sure if this is correct, must be checked. Most probably, if the xform handedness if negative, Tangent.W must be reversed.
            var txyz = Vector3.Normalize(Vector3.TransformNormal(new Vector3(Tangent.X, Tangent.Y, Tangent.Z), xform));
            Tangent = new Vector4(txyz, Tangent.W);
        }

        #endregion
    }

    /// <summary>
    /// Defines a Vertex attribute with a Position, a Normal and a Tangent.
    /// </summary>
    [System.Diagnostics.DebuggerDisplay("{_GetDebuggerDisplay(),nq}")]
    public struct VertexGeometryDelta : IVertexGeometry, IEquatable<VertexGeometryDelta>
    {
        #region debug

        private string _GetDebuggerDisplay() => $"Δ𝐏:{PositionDelta} Δ𝚴:{NormalDelta} Δ𝚻:{TangentDelta}";

        #endregion

        #region constructors

        public static implicit operator VertexGeometryDelta(in Vector3 position)
        {
            return new VertexGeometryDelta(position, Vector3.Zero, Vector3.Zero);
        }

        public static implicit operator VertexGeometryDelta(in (Vector3 Pos, Vector3 Nrm) tuple)
        {
            return new VertexGeometryDelta(tuple.Pos, tuple.Nrm, Vector3.Zero);
        }

        public static implicit operator VertexGeometryDelta(in (Vector3 Pos, Vector3 Nrm, Vector3 tgt) tuple)
        {
            return new VertexGeometryDelta(tuple.Pos, tuple.Nrm, tuple.tgt);
        }

        public VertexGeometryDelta(IVertexGeometry src)
        {
            Guard.NotNull(src, nameof(src));

            this.PositionDelta = src.GetPosition();
            src.TryGetNormal(out this.NormalDelta);
            src.TryGetTangent(out Vector4 t);

            this.TangentDelta = new Vector3(t.X, t.Y, t.Z);
        }

        public VertexGeometryDelta(in Vector3 p, in Vector3 n, in Vector3 t)
        {
            this.PositionDelta = p;
            this.NormalDelta = n;
            this.TangentDelta = t;
        }

        internal VertexGeometryDelta(in VertexPosition rootVal, in VertexPosition morphVal)
        {
            PositionDelta = morphVal.Position - rootVal.Position;
            NormalDelta = Vector3.Zero;
            TangentDelta = Vector3.Zero;
        }

        internal VertexGeometryDelta(in VertexPositionNormal rootVal, in VertexPositionNormal morphVal)
        {
            PositionDelta = morphVal.Position - rootVal.Position;
            NormalDelta = morphVal.Normal - rootVal.Normal;
            TangentDelta = Vector3.Zero;
        }

        internal VertexGeometryDelta(in VertexPositionNormalTangent rootVal, in VertexPositionNormalTangent morphVal)
        {
            PositionDelta = morphVal.Position - rootVal.Position;
            NormalDelta = morphVal.Normal - rootVal.Normal;

            var dt = morphVal.Tangent - rootVal.Tangent;
            TangentDelta = new Vector3(dt.X, dt.Y, dt.Z);
        }

        internal VertexGeometryDelta(in VertexGeometryDelta rootVal, in VertexGeometryDelta morphVal)
        {
            PositionDelta = morphVal.PositionDelta - rootVal.PositionDelta;
            NormalDelta = morphVal.NormalDelta - rootVal.NormalDelta;
            TangentDelta = morphVal.TangentDelta - rootVal.TangentDelta;
        }

        #endregion

        #region data

        [VertexAttribute("POSITIONDELTA")]
        public Vector3 PositionDelta;

        [VertexAttribute("NORMALDELTA")]
        public Vector3 NormalDelta;

        [VertexAttribute("TANGENTDELTA")]
        public Vector3 TangentDelta;

        /// <inheritdoc/>
        public readonly override int GetHashCode() { return PositionDelta.GetHashCode(); }

        /// <inheritdoc/>
        public readonly override bool Equals(object obj) { return obj is VertexGeometryDelta other && AreEqual(this, other); }

        /// <inheritdoc/>
        public readonly bool Equals(VertexGeometryDelta other) { return AreEqual(this, other); }
        public static bool operator ==(in VertexGeometryDelta a, in VertexGeometryDelta b) { return AreEqual(a, b); }
        public static bool operator !=(in VertexGeometryDelta a, in VertexGeometryDelta b) { return !AreEqual(a, b); }
        public static bool AreEqual(in VertexGeometryDelta a, in VertexGeometryDelta b)
        {
            return a.PositionDelta == b.PositionDelta && a.NormalDelta == b.NormalDelta && a.TangentDelta == b.TangentDelta;
        }        

        #endregion

        #region API

        void IVertexGeometry.SetPosition(in Vector3 position) { this.PositionDelta = position; }

        void IVertexGeometry.SetNormal(in Vector3 normal) { this.NormalDelta = normal; }

        void IVertexGeometry.SetTangent(in Vector4 tangent) { this.TangentDelta = new Vector3(tangent.X, tangent.Y, tangent.Z); }

        /// <inheritdoc/>
        public readonly Vector3 GetPosition() { return this.PositionDelta; }

        /// <inheritdoc/>
        public readonly bool TryGetNormal(out Vector3 normal) { normal = this.NormalDelta; return true; }

        /// <inheritdoc/>
        public readonly bool TryGetTangent(out Vector4 tangent) { tangent = new Vector4(this.TangentDelta, 0); return true; }

        /// <inheritdoc/>
        public void ApplyTransform(in Matrix4x4 xform) { throw new NotSupportedException(); }

        /// <inheritdoc/>
        public VertexGeometryDelta Subtract(IVertexGeometry baseValue)
        {
            return new VertexGeometryDelta((VertexGeometryDelta)baseValue, this);
        }

        /// <inheritdoc/>
        public void Add(in VertexGeometryDelta delta)
        {
            this.PositionDelta += delta.PositionDelta;
            this.NormalDelta += delta.NormalDelta;
            this.TangentDelta += delta.TangentDelta;
        }

        #endregion
    }
}
