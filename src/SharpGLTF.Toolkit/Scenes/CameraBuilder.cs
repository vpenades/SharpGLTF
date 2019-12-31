using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace SharpGLTF.Scenes
{
    public abstract class CameraBuilder
    {
        #region lifecycle

        public abstract CameraBuilder Clone();

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

        public abstract Matrix4x4 Matrix { get; }

        #endregion

        #region Nested types

        #pragma warning disable CA1034 // Nested types should not be visible

        [System.Diagnostics.DebuggerDisplay("Orthographic ({XMag},{YMag})  {ZNear} < {ZFar}")]
        public sealed class Orthographic : CameraBuilder
        {
            #region lifecycle

            public Orthographic(float xmag, float ymag, float znear, float zfar)
            {
                this.XMag = xmag;
                this.YMag = ymag;
                this.ZNear = znear;
                this.ZFar = zfar;
            }

            internal Orthographic(Schema2.CameraOrthographic ortho)
            {
                this.XMag = ortho.XMag;
                this.YMag = ortho.YMag;
                this.ZNear = ortho.ZNear;
                this.ZFar = ortho.ZFar;
            }

            public override CameraBuilder Clone()
            {
                return new Orthographic(this);
            }

            internal Orthographic(Orthographic ortho)
            {
                this.XMag = ortho.XMag;
                this.YMag = ortho.YMag;
                this.ZNear = ortho.ZNear;
                this.ZFar = ortho.ZFar;
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

            /// <summary>
            /// Gets the projection matrix for the current settings
            /// </summary>
            public override Matrix4x4 Matrix => Transforms.Projection.CreateOrthographicMatrix(XMag, YMag, ZNear, ZFar);

            #endregion
        }

        [System.Diagnostics.DebuggerDisplay("Perspective {AspectRatio} {VerticalFOV}   {ZNear} < {ZFar}")]
        public sealed partial class Perspective : CameraBuilder
        {
            #region lifecycle

            public Perspective(float? aspectRatio, float fovy, float znear, float zfar = float.PositiveInfinity)
            {
                this.AspectRatio = aspectRatio;
                this.VerticalFOV = fovy;
                this.ZNear = znear;
                this.ZFar = zfar;
            }

            internal Perspective(Schema2.CameraPerspective persp)
            {
                this.AspectRatio = persp.AspectRatio;
                this.VerticalFOV = persp.VerticalFOV;
                this.ZNear = persp.ZNear;
                this.ZFar = persp.ZFar;
            }

            public override CameraBuilder Clone()
            {
                return new Perspective(this);
            }

            internal Perspective(Perspective persp)
            {
                this.AspectRatio = persp.AspectRatio;
                this.VerticalFOV = persp.VerticalFOV;
                this.ZNear = persp.ZNear;
                this.ZFar = persp.ZFar;
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

            /// <summary>
            /// Gets the projection matrix for the current settings
            /// </summary>
            public override Matrix4x4 Matrix => Transforms.Projection.CreateOrthographicMatrix(AspectRatio ?? 1, VerticalFOV, ZNear, ZFar);

            #endregion
        }

        #pragma warning restore CA1034 // Nested types should not be visible

        #endregion
    }
}
