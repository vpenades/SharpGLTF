using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace SharpGLTF.Transforms
{
    public static class Matrix4x4Factory
    {
        #region diagnostics

        [Flags]
        public enum MatrixCheck
        {
            None = 0,

            /// <summary>
            /// All members of the matrix must be finite numbers.
            /// </summary>
            Finite = 1,

            /// <summary>
            /// the matrix must not be all zeros.
            /// </summary>
            NonZero = 2,

            /// <summary>
            /// The matrix must be the identity matrix.
            /// </summary>
            Identity = 4,

            /// <summary>
            /// The forth column of the matrix must be defined with EXACT values: [0,0,0,1].
            /// See <see href="https://www.khronos.org/registry/glTF/specs/2.0/glTF-2.0.html#skins-overview">glTF spec</see>
            /// </summary>
            IdentityColumn4 = 8,

            /// <summary>
            /// The matrix must be invertible as in <see cref="Matrix4x4.Invert(Matrix4x4, out Matrix4x4)"/>.
            /// </summary>
            Invertible = 16,

            /// <summary>
            /// The matrix must be decomposable as in <see cref="Matrix4x4.Decompose(Matrix4x4, out Vector3, out Quaternion, out Vector3)"/>.
            /// </summary>
            Decomposable = 32,

            /// <summary>
            /// The matrix must have a positive determinant.
            /// </summary>
            PositiveDeterminant = 64,

            /// <summary>
            /// A local matrix must be invertible and decomposable to Scale-Rotation-Translation.
            /// </summary>
            LocalTransform = NonZero | Invertible | Decomposable | IdentityColumn4,

            /// <remarks>
            /// A world matrix can be built from a concatenation of local tranforms.<br/>
            /// Which means it can be a squeezed matrix, and not decomposable.
            /// </remarks>
            WorldTransform = NonZero | Invertible | IdentityColumn4,

            /// <summary>
            /// Since an inverse bind matrix is built from the inverse of a WorldMatrix,
            /// the same rules apply.
            /// </summary>
            InverseBindMatrix = WorldTransform,
        }

        private static MatrixCheck _Validate(in Matrix4x4 matrix, MatrixCheck check, float tolerance = 0)
        {
            if (!matrix._IsFinite()) return MatrixCheck.Finite;

            if (check.HasFlag(MatrixCheck.NonZero) && matrix == default) return MatrixCheck.NonZero;
            if (check.HasFlag(MatrixCheck.Identity) && matrix != Matrix4x4.Identity) return MatrixCheck.Identity;
            if (check.HasFlag(MatrixCheck.IdentityColumn4))
            {
                // Acording to gltf schema
                // "The fourth row of each matrix MUST be set to [0.0, 0.0, 0.0, 1.0]."
                // https://www.khronos.org/registry/glTF/specs/2.0/glTF-2.0.html#skins-overview
                if (matrix.M14 != 0) return MatrixCheck.IdentityColumn4;
                if (matrix.M24 != 0) return MatrixCheck.IdentityColumn4;
                if (matrix.M34 != 0) return MatrixCheck.IdentityColumn4;
                if (tolerance == 0)
                {
                    if (matrix.M44 != 1) return MatrixCheck.IdentityColumn4;
                }
                else
                {
                    if (Math.Abs(matrix.M44 - 1) > tolerance) return MatrixCheck.IdentityColumn4;
                }
            }

            if (check.HasFlag(MatrixCheck.Invertible) && !Matrix4x4.Invert(matrix, out _)) return MatrixCheck.Invertible;
            if (check.HasFlag(MatrixCheck.Decomposable) && !Matrix4x4.Decompose(matrix, out _, out _, out _)) return MatrixCheck.Decomposable;
            if (check.HasFlag(MatrixCheck.PositiveDeterminant) && matrix.GetDeterminant() <= 0) return MatrixCheck.PositiveDeterminant;

            return MatrixCheck.None;
        }

        public static bool IsValid(in Matrix4x4 matrix, MatrixCheck check, float tolerance = 0)
        {
            return _Validate(matrix, check, tolerance) == MatrixCheck.None;
        }

        [System.Diagnostics.DebuggerStepThrough]
        public static void GuardMatrix(string argName, Matrix4x4 matrix, MatrixCheck check, float tolerance = 0)
        {
            var result = _Validate(matrix, check, tolerance);

            if (result != MatrixCheck.None)
            {
                throw new ArgumentException($"Invalid Matrix. Fail: {result}", argName);
            }
        }

        #endregion

        #region API

        public static Matrix4x4 CreateFromRows(Vector3 rowX, Vector3 rowY, Vector3 rowZ)
        {
            return new Matrix4x4
                (
                rowX.X, rowX.Y, rowX.Z, 0,
                rowY.X, rowY.Y, rowY.Z, 0,
                rowZ.X, rowZ.Y, rowZ.Z, 0,
                0, 0, 0, 1
                );
        }

        public static Matrix4x4 CreateFromRows(Vector3 rowX, Vector3 rowY, Vector3 rowZ, Vector3 translation)
        {
            return new Matrix4x4
                (
                rowX.X, rowX.Y, rowX.Z, 0,
                rowY.X, rowY.Y, rowY.Z, 0,
                rowZ.X, rowZ.Y, rowZ.Z, 0,
                translation.X, translation.Y, translation.Z, 1
                );
        }

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
            GuardMatrix(nameof(parentWorld), parentWorld, MatrixCheck.WorldTransform);
            GuardMatrix(nameof(childLocal), childLocal, MatrixCheck.LocalTransform);

            return childLocal * parentWorld;
        }
        
        public static Matrix4x4 WorldToLocal(in Matrix4x4 parentWorld, in Matrix4x4 childWorld)
        {
            GuardMatrix(nameof(parentWorld), parentWorld, MatrixCheck.WorldTransform);
            GuardMatrix(nameof(childWorld), childWorld, MatrixCheck.WorldTransform);            

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

        #endregion
    }
}
