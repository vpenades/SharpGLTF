using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace SharpGLTF.Scenes
{
    /// <summary>
    /// Represents an camera object.
    /// </summary>
    /// <remarks>
    /// Derived types are:<br/>
    /// - <see cref="Orthographic"/><br/>
    /// - <see cref="Perspective"/><br/>
    /// </remarks>
    public abstract class CameraBuilder : BaseBuilder
    {
        #region lifecycle

        public abstract CameraBuilder Clone();

        protected CameraBuilder(float znear, float zfar)
        {
            this.ZNear = znear;
            this.ZFar = zfar;
        }

        protected CameraBuilder(CameraBuilder other)
            : base(other)
        {
            Guard.NotNull(other, nameof(other));

            this.ZNear = other.ZNear;
            this.ZFar = other.ZFar;
        }

        #endregion

        #region properties

        public static Vector3 LocalDirection => -Vector3.UnitZ;

        /// <summary>
        /// Gets or sets the near plane distance in the Z axis.
        /// </summary>
        public float ZNear { get; set; }

        /// <summary>
        /// Gets or sets the far plane distance in the Z axis.
        /// </summary>
        public float ZFar { get; set; }

        /// <summary>
        /// Gets a value indicating whether the camera parameters are correct.
        /// </summary>
        public bool IsValid
        {
            get
            {
                try { GetMatrix(); return true; }
                catch { return false; }
            }
        }

        /// <summary>
        /// Gets the projection matrix for the camera parameters.
        /// </summary>
        public Matrix4x4 Matrix => GetMatrix();

        #endregion

        #region API

        protected abstract Matrix4x4 GetMatrix();

        #endregion

        #region Nested types

        #pragma warning disable CA1034 // Nested types should not be visible

        /// <inheritdoc/>
        [System.Diagnostics.DebuggerDisplay("CameraBuilder.Orthographic ({XMag},{YMag})  {ZNear} < {ZFar}")]
        public sealed class Orthographic : CameraBuilder
        {
            #region lifecycle

            public Orthographic(float xmag, float ymag, float znear, float zfar)
                : base(znear, zfar)
            {
                this.XMag = xmag;
                this.YMag = ymag;
            }

            internal Orthographic(Schema2.CameraOrthographic ortho)
                : base(ortho.ZFar, ortho.ZFar)
            {
                this.XMag = ortho.XMag;
                this.YMag = ortho.YMag;
            }

            public override CameraBuilder Clone()
            {
                return new Orthographic(this);
            }

            private Orthographic(Orthographic ortho)
                : base(ortho)
            {
                this.XMag = ortho.XMag;
                this.YMag = ortho.YMag;
            }

            #endregion

            #region properties

            /// <summary>
            /// Gets or sets the magnification factor in the X axis
            /// </summary>
            public float XMag { get; set; }

            /// <summary>
            /// Gets or sets the magnification factor in the Y axis
            /// </summary>
            public float YMag { get; set; }

            #endregion

            #region API

            protected override Matrix4x4 GetMatrix()
            {
                return Transforms.Projection.CreateOrthographicMatrix(XMag, YMag, ZNear, ZFar);
            }

            #endregion
        }

        /// <inheritdoc/>
        [System.Diagnostics.DebuggerDisplay("CameraBuilder.Perspective {AspectRatio} {VerticalFOV}   {ZNear} < {ZFar}")]
        public sealed partial class Perspective : CameraBuilder
        {
            #region lifecycle

            public Perspective(float? aspectRatio, float fovy, float znear, float zfar = float.PositiveInfinity)
                : base(znear, zfar)
            {
                this.AspectRatio = aspectRatio;
                this.VerticalFOV = fovy;
            }

            internal Perspective(Schema2.CameraPerspective persp)
                : base(persp.ZNear, persp.ZFar)
            {
                this.AspectRatio = persp.AspectRatio;
                this.VerticalFOV = persp.VerticalFOV;
            }

            public override CameraBuilder Clone()
            {
                return new Perspective(this);
            }

            private Perspective(Perspective persp)
                : base(persp)
            {
                this.AspectRatio = persp.AspectRatio;
                this.VerticalFOV = persp.VerticalFOV;
            }

            #endregion

            #region properties

            /// <summary>
            /// Gets or sets the aspect ratio between horizontal window size and vertical window size.
            /// </summary>
            public float? AspectRatio { get; set; }

            /// <summary>
            /// Gets or sets the vertical field of view, in radians
            /// </summary>
            public float VerticalFOV { get; set; }

            #endregion

            #region API
            protected override Matrix4x4 GetMatrix()
            {
                return Transforms.Projection.CreateOrthographicMatrix(AspectRatio ?? 1, VerticalFOV, ZNear, ZFar);
            }

            #endregion
        }

        #pragma warning restore CA1034 // Nested types should not be visible

        #endregion
    }
}
