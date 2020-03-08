using System;
using System.Collections.Generic;
using System.Text;

namespace SharpGLTF.Schema2
{
    [System.Diagnostics.DebuggerDisplay("Camera[{LogicalIndex}] {Name} {_type}")]
    public sealed partial class Camera
    {
        #region lifecycle

        internal Camera() { }

        #endregion

        #region properties

        /// <summary>
        /// Gets the zero-based index of this <see cref="Camera"/> at <see cref="ModelRoot.LogicalCameras"/>
        /// </summary>
        public int LogicalIndex => this.LogicalParent.LogicalCameras.IndexOfReference(this);

        public ICamera Settings => GetCamera();

        /// <summary>
        /// Gets the projection matrix for the current <see cref="Settings"/>
        /// </summary>
        public System.Numerics.Matrix4x4 Matrix => GetCamera().Matrix;

        #endregion

        #region API

        protected override IEnumerable<ExtraProperties> GetLogicalChildren()
        {
            return base.GetLogicalChildren().ConcatItems(_orthographic, _perspective);
        }

        internal ICamera GetCamera()
        {
            if (this._orthographic != null && this._perspective != null)
            {
                switch (this._type)
                {
                    case CameraType.orthographic: return this._orthographic;
                    case CameraType.perspective: return this._perspective;
                    default: throw new NotImplementedException();
                }
            }

            if (this._orthographic != null) return this._orthographic;
            if (this._perspective != null) return this._perspective;

            return null;
        }

        /// <summary>
        /// Configures this <see cref="Camera"/> to use Orthographic projection.
        /// </summary>
        /// <param name="xmag">Magnification in the X axis.</param>
        /// <param name="ymag">Magnification in the Y axis.</param>
        /// <param name="znear">Distance to the near pane in the Z axis.</param>
        /// <param name="zfar">Distance to the far plane in the Z axis.</param>
        public void SetOrthographicMode(float xmag, float ymag, float znear, float zfar)
        {
            CameraOrthographic.VerifyParameters(xmag, ymag, znear, zfar);

            this._perspective = null;
            this._orthographic = new CameraOrthographic(xmag, ymag, znear, zfar);

            this._type = CameraType.orthographic;
        }

        /// <summary>
        /// Configures this <see cref="Camera"/> to use perspective projection.
        /// </summary>
        /// <param name="aspectRatio">The aspect ratio between horizontal and vertical. (optional)</param>
        /// <param name="yfov">The vertical field of view, in radians.</param>
        /// <param name="znear">Distance to the near pane in the Z axis.</param>
        /// <param name="zfar">Distance to the far plane in the Z axis.</param>
        public void SetPerspectiveMode(float? aspectRatio, float yfov, float znear, float zfar)
        {
            CameraPerspective.VerifyParameters(aspectRatio, yfov, znear, zfar);

            this._orthographic = null;
            this._perspective = new CameraPerspective(aspectRatio, yfov, znear, zfar);

            this._type = CameraType.perspective;
        }

        #endregion

        #region validation

        protected override void OnValidateReferences(Validation.ValidationContext validate)
        {
            if (_type == CameraType.perspective)
            {
                validate.IsDefined("perspective", _perspective);
                validate.IsUndefined("orthographic", _orthographic);
            }

            if (_type == CameraType.orthographic)
            {
                validate.IsUndefined("perspective", _perspective);
                validate.IsDefined("orthographic", _orthographic);
            }

            base.OnValidateReferences(validate);
        }

        #endregion
    }

    /// <summary>
    /// Common interface for <see cref="CameraOrthographic"/> and <see cref="CameraPerspective"/>.
    /// </summary>
    public interface ICamera
    {
        bool IsOrthographic { get; }
        bool IsPerspective { get; }

        System.Numerics.Matrix4x4 Matrix { get; }
    }

    [System.Diagnostics.DebuggerDisplay("Orthographic ({XMag},{YMag})  {ZNear} < {ZFar}")]
    public sealed partial class CameraOrthographic : ICamera
    {
        #region lifecycle

        internal CameraOrthographic() { }

        internal CameraOrthographic(float xmag, float ymag, float znear, float zfar)
        {
            this._xmag = xmag;
            this._ymag = ymag;
            this._znear = znear;
            this._zfar = zfar;
        }

        #endregion

        #region properties

        public bool IsPerspective => false;

        public bool IsOrthographic => true;

        /// <summary>
        /// Gets the magnification factor in the X axis
        /// </summary>
        public float XMag => (float)_xmag;

        /// <summary>
        /// Gets the magnification factor in the Y axis
        /// </summary>
        public float YMag => (float)_ymag;

        /// <summary>
        /// Gets the near plane distance in the Z axis.
        /// </summary>
        public float ZNear => (float)_znear;

        /// <summary>
        /// Gets the far plane distance in the Z axis.
        /// </summary>
        public float ZFar => (float)_zfar;

        /// <summary>
        /// Gets the projection matrix for the current settings
        /// </summary>
        public System.Numerics.Matrix4x4 Matrix => Transforms.Projection.CreateOrthographicMatrix(XMag, YMag, ZNear, ZFar);

        #endregion

        #region API

        public static void VerifyParameters(float xmag, float ymag, float znear, float zfar)
        {
            Guard.MustBeGreaterThanOrEqualTo(znear, (float)_znearMinimum, nameof(znear));
            Guard.MustBeGreaterThanOrEqualTo(zfar, (float)_zfarMinimum, nameof(zfar));
            Guard.MustBeGreaterThan(zfar, znear, nameof(zfar));
            Guard.MustBeLessThan(zfar, float.PositiveInfinity, nameof(zfar));

            // these are considered warnings
            // Guard.MustBeGreaterThan(xmag, 0, nameof(xmag));
            // Guard.MustBeGreaterThan(ymag, 0, nameof(ymag));
        }

        #endregion

        #region validation

        protected override void OnValidateContent(Validation.ValidationContext validate)
        {
            validate.That(() => VerifyParameters(this.XMag, this.YMag, this.ZNear, this.ZFar));

            base.OnValidateContent(validate);
        }

        #endregion
    }

    [System.Diagnostics.DebuggerDisplay("Perspective {AspectRatio} {VerticalFOV}   {ZNear} < {ZFar}")]
    public sealed partial class CameraPerspective : ICamera
    {
        #region lifecycle

        internal CameraPerspective() { }

        internal CameraPerspective(float? aspectRatio, float yfov, float znear, float zfar)
        {
            VerifyParameters(aspectRatio, yfov, znear, zfar);

            this._aspectRatio = aspectRatio ?? null;
            this._yfov = yfov;
            this._znear = znear;
            this._zfar = float.IsPositiveInfinity(zfar) ? (double?)null : (double)zfar;
        }

        #endregion

        #region properties

        public bool IsOrthographic => false;

        public bool IsPerspective => true;

        /// <summary>
        /// Gets the aspect ratio between horizontal window size and vertical window size.
        /// </summary>
        public float? AspectRatio => (float?)(this._aspectRatio ?? null);

        /// <summary>
        /// Gets the vertical field of view, in radians
        /// </summary>
        public float VerticalFOV => (float)this._yfov;

        /// <summary>
        /// Gets the near plane distance in the Z axis.
        /// </summary>
        public float ZNear => (float)_znear;

        /// <summary>
        /// Gets the far plane distance in the Z axis.
        /// </summary>
        /// <remarks>
        /// This value can be a finite value, or positive infinity.
        /// </remarks>
        public float ZFar => this._zfar.HasValue ? (float)_zfar.Value : float.PositiveInfinity;

        /// <summary>
        /// Gets the projection matrix for the current settings
        /// </summary>
        public System.Numerics.Matrix4x4 Matrix => Transforms.Projection.CreateOrthographicMatrix(AspectRatio.AsValue(1), VerticalFOV, ZNear, ZFar);

        #endregion

        #region API

        public static void VerifyParameters(float? aspectRatio, float yfov, float znear, float zfar = float.PositiveInfinity)
        {
            Guard.MustBeGreaterThanOrEqualTo(aspectRatio.AsValue(1), (float)_aspectRatioMinimum, nameof(aspectRatio));
            Guard.MustBeGreaterThan(yfov, (float)_yfovMinimum, nameof(yfov));
            // Guard.MustBeLessThan(yfov, (float)Math.PI, nameof(yfov));

            Guard.MustBeGreaterThanOrEqualTo(znear, (float)_znearMinimum, nameof(znear));
            Guard.MustBeGreaterThanOrEqualTo(zfar, (float)_zfarMinimum, nameof(zfar));
            Guard.MustBeGreaterThan(zfar, znear, nameof(zfar));
        }

        #endregion

        #region validation

        protected override void OnValidateContent(Validation.ValidationContext validate)
        {
            validate.That(() => VerifyParameters(this.AspectRatio, this.VerticalFOV, this.ZNear, this.ZFar));

            base.OnValidateContent(validate);
        }

        #endregion
    }

    public partial class ModelRoot
    {
        /// <summary>
        /// Creates a new <see cref="Camera"/> instance.
        /// and appends it to <see cref="ModelRoot.LogicalCameras"/>.
        /// </summary>
        /// <param name="name">The name of the instance.</param>
        /// <returns>A <see cref="Camera"/> instance.</returns>
        public Camera CreateCamera(string name = null)
        {
            var camera = new Camera
            {
                Name = name
            };

            _cameras.Add(camera);

            return camera;
        }
    }
}
