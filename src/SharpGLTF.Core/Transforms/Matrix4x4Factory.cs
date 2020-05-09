using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace SharpGLTF.Transforms
{
    public static class Matrix4x4Factory
    {
        /// <summary>
        /// Evaluates a <see cref="Matrix4x4"/> transform based on the available parameters.
        /// </summary>
        /// <param name="transform">A <see cref="Matrix4x4"/> instance, or null.</param>
        /// <param name="scale">A <see cref="Vector3"/> instance, or null.</param>
        /// <param name="rotation">A <see cref="Quaternion"/> instance, or null.</param>
        /// <param name="translation">A <see cref="Vector3"/> instance, or null.</param>
        /// <returns>A <see cref="Matrix4x4"/> transform.</returns>
        public static Matrix4x4 CreateFrom(Matrix4x4? transform, Vector3? scale, Quaternion? rotation, Vector3? translation)
        {
            if (transform.HasValue)
            {
                // scale, rotation and translation should be null at this point.
                return transform.Value;
            }

            return new AffineTransform(scale, rotation, translation).Matrix;
        }

        public static Matrix4x4 LocalToWorld(in Matrix4x4 parentWorld, in Matrix4x4 childLocal)
        {
            return childLocal * parentWorld;
        }

        public static Matrix4x4 WorldToLocal(in Matrix4x4 parentWorld, in Matrix4x4 childWorld)
        {
            return childWorld * parentWorld.Inverse();
        }

        /// <summary>
        /// Normalizes the axis of the given matrix, to make it orthogonal.
        /// </summary>
        /// <param name="xform">The <see cref="Matrix4x4"/> to normalize.</param>
        public static void NormalizeMatrix(ref Matrix4x4 xform)
        {
            var vx = new Vector3(xform.M11, xform.M12, xform.M13);
            var vy = new Vector3(xform.M21, xform.M22, xform.M23);
            var vz = new Vector3(xform.M31, xform.M32, xform.M33);

            var lx = vx.Length();
            var ly = vy.Length();
            var lz = vz.Length();

            // normalize axis vectors
            vx /= lx;
            vy /= ly;
            vz /= lz;

            // determine the skew of each axis (the smaller, the more orthogonal the axis is)
            var kxy = Math.Abs(Vector3.Dot(vx, vy));
            var kxz = Math.Abs(Vector3.Dot(vx, vz));
            var kyz = Math.Abs(Vector3.Dot(vy, vz));

            var kx = kxy + kxz;
            var ky = kxy + kyz;
            var kz = kxz + kyz;

            // we will use the axis with less skew as our fixed pivot.

            // axis X as pivot
            if (kx < ky && kx < kz)
            {
                if (ky < kz)
                {
                    vz = Vector3.Cross(vx, vy);
                    vy = Vector3.Cross(vz, vx);
                }
                else
                {
                    vy = Vector3.Cross(vz, vx);
                    vz = Vector3.Cross(vx, vy);
                }
            }

            // axis Y as pivot
            else if (ky < kx && ky < kz)
            {
                if (kx < kz)
                {
                    vz = Vector3.Cross(vx, vy);
                    vx = Vector3.Cross(vy, vz);
                }
                else
                {
                    vx = Vector3.Cross(vy, vz);
                    vz = Vector3.Cross(vx, vy);
                }
            }

            // axis z as pivot
            else
            {
                if (kx < ky)
                {
                    vy = Vector3.Cross(vz, vx);
                    vx = Vector3.Cross(vy, vz);
                }
                else
                {
                    vx = Vector3.Cross(vy, vz);
                    vy = Vector3.Cross(vz, vx);
                }
            }

            // restore axes original lengths
            vx *= lx;
            vy *= ly;
            vz *= lz;

            xform.M11 = vx.X;
            xform.M12 = vx.Y;
            xform.M13 = vx.Z;

            xform.M21 = vy.X;
            xform.M22 = vy.Y;
            xform.M23 = vy.Z;

            xform.M31 = vz.X;
            xform.M32 = vz.Y;
            xform.M33 = vz.Z;
        }
    }
}
