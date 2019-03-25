using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace SharpGLTF.Transforms
{
    // https://github.com/KhronosGroup/glTF-Validator/issues/33

    /// <summary>
    /// Represents an affine transform in 3D space, defined by a <see cref="Quaternion"/> rotation, a <see cref="Vector3"/> scale and a <see cref="Vector3"/> translation.
    /// </summary>
    public struct AffineTransform
    {
        #region lifecycle

        internal AffineTransform(Matrix4x4? m, Vector3? s, Quaternion? r, Vector3? t)
        {
            if (m.HasValue)
            {
                Matrix4x4.Decompose(m.Value, out Scale, out Rotation, out Translation);
            }
            else
            {
                Rotation = r ?? Quaternion.Identity;
                Scale = s ?? Vector3.One;
                Translation = t ?? Vector3.Zero;
            }
        }

        #endregion

        #region data

        /// <summary>
        /// Rotation
        /// </summary>
        public Quaternion Rotation;

        /// <summary>
        /// Scale
        /// </summary>
        public Vector3 Scale;

        /// <summary>
        /// Translation
        /// </summary>
        public Vector3 Translation;

        #endregion

        #region properties

        /// <summary>
        /// Gets the <see cref="Matrix"/> transform of the current <see cref="AffineTransform"/>
        /// </summary>
        public Matrix4x4 Matrix
        {
            get
            {
                return
                    Matrix4x4.CreateScale(Scale)
                    *
                    Matrix4x4.CreateFromQuaternion(Rotation.Sanitized())
                    *
                    Matrix4x4.CreateTranslation(Translation);
            }
        }

        #endregion

        #region API

        /// <summary>
        /// Evaluates a <see cref="Matrix4x4"/> transform based on the available parameters.
        /// </summary>
        /// <param name="transform">A <see cref="Matrix4x4"/> instance, or null.</param>
        /// <param name="scale">A <see cref="Vector3"/> instance, or null.</param>
        /// <param name="rotation">A <see cref="Quaternion"/> instance, or null.</param>
        /// <param name="translation">A <see cref="Vector3"/> instance, or null.</param>
        /// <returns>A <see cref="Matrix4x4"/> transform.</returns>
        public static Matrix4x4 Evaluate(Matrix4x4? transform, Vector3? scale, Quaternion? rotation, Vector3? translation)
        {
            if (transform.HasValue) return transform.Value;

            return new AffineTransform(null, scale, rotation, translation).Matrix;
        }

        public static Matrix4x4 LocalToWorld(Matrix4x4 parentWorld, Matrix4x4 childLocal)
        {
            return childLocal * parentWorld;
        }

        public static Matrix4x4 WorldToLocal(Matrix4x4 parentWorld, Matrix4x4 childWorld)
        {
            Matrix4x4.Invert(parentWorld, out Matrix4x4 invWorld);

            return childWorld * invWorld;
        }

        public static Matrix4x4 CreateFromRows(Vector4 x, Vector4 y, Vector4 z, Vector4 w)
        {
            return new Matrix4x4
                (
                x.X, x.Y, x.Z, x.W,
                y.X, y.Y, y.Z, y.W,
                z.X, z.Y, z.Z, z.W,
                w.X, w.Y, w.Z, w.W
                );
        }

        #endregion
    }
}
