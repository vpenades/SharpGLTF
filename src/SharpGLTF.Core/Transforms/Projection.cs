using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace SharpGLTF.Transforms
{
    /// <summary>
    /// Utility class to calculate camera matrices
    /// </summary>
    /// <see href="https://github.com/KhronosGroup/glTF/blob/master/specification/2.0/README.md#projection-matrices"/>
    public static class Projection
    {
        /// <summary>
        /// Calculates an orthographic projection matrix.
        /// </summary>
        /// <param name="xmag">Magnification in the X axis.</param>
        /// <param name="ymag">Magnification in the Y axis.</param>
        /// <param name="znear">Distance to the near pane in the Z axis.</param>
        /// <param name="zfar">Distance to the far plane in the Z axis.</param>
        /// <returns>A projection matrix</returns>
        public static Matrix4x4 CreateOrthographicMatrix(float xmag, float ymag, float znear, float zfar)
        {
            Guard.MustBeGreaterThanOrEqualTo(znear, 0, nameof(znear));
            Guard.MustBeGreaterThanOrEqualTo(zfar, 0, nameof(zfar));
            Guard.MustBeGreaterThan(zfar, znear, nameof(zfar));
            Guard.MustBeLessThan(zfar, float.PositiveInfinity, nameof(zfar));

            var d = znear - zfar;

            var x = 1 / xmag;
            var y = 1 / ymag;
            var z = 2 / d;
            var w = (zfar + znear) / d;

            return new Matrix4x4
                (
                x, 0, 0, 0,
                0, y, 0, 0,
                0, 0, z, w,
                0, 0, 0, 1
                );
        }

        /// <summary>
        /// Calculates a perspective projection matrix.
        /// </summary>
        /// <param name="aspectRatio">The aspect ratio between horizontal and vertical. (optional)</param>
        /// <param name="yfov">The vertical field of view, in radians.</param>
        /// <param name="znear">Distance to the near pane in the Z axis.</param>
        /// <param name="zfar">Distance to the far plane in the Z axis. Optionally, this value can be positive infinity</param>
        /// <returns>A projection matrix</returns>
        public static Matrix4x4 CreatePerspectiveMatrix(float aspectRatio, float yfov, float znear, float zfar = float.PositiveInfinity)
        {
            Guard.MustBeGreaterThan(aspectRatio, 0, nameof(aspectRatio));
            Guard.MustBeGreaterThan(yfov, 0, nameof(yfov));
            Guard.MustBeLessThan(yfov, (float)Math.PI, nameof(yfov));

            Guard.MustBeGreaterThanOrEqualTo(znear, 0, nameof(znear));
            Guard.MustBeGreaterThanOrEqualTo(zfar, 0, nameof(zfar));
            Guard.MustBeGreaterThan(zfar, znear, nameof(zfar));

            var v = (float)Math.Tan(0.5 * yfov);
            var h = aspectRatio * v;

            var x = 1 / h;
            var y = 1 / v;
            var z = -1f;
            var w = -2 * znear;

            if (!float.IsPositiveInfinity(zfar))
            {
                var d = znear - zfar;
                z = (zfar + znear) / d;
                w = (2f * zfar * znear) / d;
            }

            return new Matrix4x4
                (
                x, 0, 0, 0,
                0, y, 0, 0,
                0, 0, z, w,
                0, 0, -1, 0
                );
        }
    }
}
