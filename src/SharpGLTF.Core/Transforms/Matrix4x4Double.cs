using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Text;

namespace SharpGLTF.Transforms
{
    #pragma warning disable SA1407 // Arithmetic expressions should declare precedence

    // stripped from https://github.com/dotnet/runtime/blob/master/src/libraries/System.Private.CoreLib/src/System/Numerics/Matrix4x4.cs

    [StructLayout(LayoutKind.Sequential)]
    [DebuggerTypeProxy(typeof(Debug._Matrix4x4DoubleProxy))]
    public struct Matrix4x4Double : IEquatable<Matrix4x4Double>
    {
        #region constants

        private static readonly Matrix4x4Double _identity = new Matrix4x4Double
        (
            1, 0, 0, 0,
            0, 1, 0, 0,
            0, 0, 1, 0,
            0, 0, 0, 1
        );

        #endregion

        #region constructors

        /// <summary>
        /// Constructs a Matrix4x4 from the given components.
        /// </summary>
        public Matrix4x4Double(double m11, double m12, double m13, double m14,
                         double m21, double m22, double m23, double m24,
                         double m31, double m32, double m33, double m34,
                         double m41, double m42, double m43, double m44)
        {
            this.M11 = m11;
            this.M12 = m12;
            this.M13 = m13;
            this.M14 = m14;

            this.M21 = m21;
            this.M22 = m22;
            this.M23 = m23;
            this.M24 = m24;

            this.M31 = m31;
            this.M32 = m32;
            this.M33 = m33;
            this.M34 = m34;

            this.M41 = m41;
            this.M42 = m42;
            this.M43 = m43;
            this.M44 = m44;
        }

        public Matrix4x4Double(Matrix4x4 other)
        {
            this.M11 = other.M11;
            this.M12 = other.M12;
            this.M13 = other.M13;
            this.M14 = other.M14;

            this.M21 = other.M21;
            this.M22 = other.M22;
            this.M23 = other.M23;
            this.M24 = other.M24;

            this.M31 = other.M31;
            this.M32 = other.M32;
            this.M33 = other.M33;
            this.M34 = other.M34;

            this.M41 = other.M41;
            this.M42 = other.M42;
            this.M43 = other.M43;
            this.M44 = other.M44;
        }

        /// <summary>
        /// Creates a translation matrix.
        /// </summary>
        /// <param name="xPosition">The amount to translate on the X-axis.</param>
        /// <param name="yPosition">The amount to translate on the Y-axis.</param>
        /// <param name="zPosition">The amount to translate on the Z-axis.</param>
        /// <returns>The translation matrix.</returns>
        public static Matrix4x4Double CreateTranslation(double xPosition, double yPosition, double zPosition)
        {
            Matrix4x4Double result;

            result.M11 = 1.0;
            result.M12 = 0.0;
            result.M13 = 0.0;
            result.M14 = 0.0;
            result.M21 = 0.0;
            result.M22 = 1.0;
            result.M23 = 0.0;
            result.M24 = 0.0;
            result.M31 = 0.0;
            result.M32 = 0.0;
            result.M33 = 1.0;
            result.M34 = 0.0;

            result.M41 = xPosition;
            result.M42 = yPosition;
            result.M43 = zPosition;
            result.M44 = 1.0f;

            return result;
        }

        /// <summary>
        /// Creates a scaling matrix.
        /// </summary>
        /// <param name="xScale">Value to scale by on the X-axis.</param>
        /// <param name="yScale">Value to scale by on the Y-axis.</param>
        /// <param name="zScale">Value to scale by on the Z-axis.</param>
        /// <returns>The scaling matrix.</returns>
        public static Matrix4x4Double CreateScale(double xScale, double yScale, double zScale)
        {
            Matrix4x4Double result;

            result.M11 = xScale;
            result.M12 = 0.0;
            result.M13 = 0.0;
            result.M14 = 0.0;
            result.M21 = 0.0;
            result.M22 = yScale;
            result.M23 = 0.0;
            result.M24 = 0.0;
            result.M31 = 0.0;
            result.M32 = 0.0;
            result.M33 = zScale;
            result.M34 = 0.0;
            result.M41 = 0.0;
            result.M42 = 0.0;
            result.M43 = 0.0;
            result.M44 = 1.0;

            return result;
        }

        /// <summary>
        /// Creates a rotation matrix from the given Quaternion rotation value.
        /// </summary>
        /// <param name="quaternion">The source Quaternion.</param>
        /// <returns>The rotation matrix.</returns>
        public static Matrix4x4Double CreateFromQuaternion(Quaternion quaternion)
        {
            Matrix4x4Double result;

            double qX = quaternion.X;
            double qY = quaternion.Y;
            double qZ = quaternion.Z;
            double qW = quaternion.W;

            double xx = qX * qX;
            double yy = qY * qY;
            double zz = qZ * qZ;

            double xy = qX * qY;
            double wz = qZ * qW;
            double xz = qZ * qX;
            double wy = qY * qW;
            double yz = qY * qZ;
            double wx = qX * qW;

            result.M11 = 1.0 - (2.0 * (yy + zz));
            result.M12 = 2.0 * (xy + wz);
            result.M13 = 2.0 * (xz - wy);
            result.M14 = 0.0;
            result.M21 = 2.0 * (xy - wz);
            result.M22 = 1.0 - (2.0 * (zz + xx));
            result.M23 = 2.0 * (yz + wx);
            result.M24 = 0.0;
            result.M31 = 2.0 * (xz + wy);
            result.M32 = 2.0 * (yz - wx);
            result.M33 = 1.0 - (2.0 * (yy + xx));
            result.M34 = 0.0;
            result.M41 = 0.0;
            result.M42 = 0.0;
            result.M43 = 0.0;
            result.M44 = 1.0;

            return result;
        }

        /// <summary>
        /// Converts a <see cref="Matrix4x4Double"/> to a <see cref="Matrix4x4"/>
        /// </summary>
        /// <param name="mat">The matrix to convert.</param>
        /// <remarks>
        /// Since converting From double precission to single precission
        /// implies a loss of precission, this conversion is explicit, so
        /// developers will be aware of when the precission loss is happening.
        /// </remarks>
        public static explicit operator Matrix4x4(Matrix4x4Double mat)
        {
            return new Matrix4x4
                (
                (float)mat.M11,
                (float)mat.M12,
                (float)mat.M13,
                (float)mat.M14,
                (float)mat.M21,
                (float)mat.M22,
                (float)mat.M23,
                (float)mat.M24,
                (float)mat.M31,
                (float)mat.M32,
                (float)mat.M33,
                (float)mat.M34,
                (float)mat.M41,
                (float)mat.M42,
                (float)mat.M43,
                (float)mat.M44
                );
        }

        /// <summary>
        /// converts from <see cref="Matrix4x4"/> to <see cref="Matrix4x4Double"/>
        /// </summary>
        /// <param name="mat">The matrix to convert.</param>
        public static implicit operator Matrix4x4Double(Matrix4x4 mat)
        {
            return new Matrix4x4Double
                (
                mat.M11,
                mat.M12,
                mat.M13,
                mat.M14,
                mat.M21,
                mat.M22,
                mat.M23,
                mat.M24,
                mat.M31,
                mat.M32,
                mat.M33,
                mat.M34,
                mat.M41,
                mat.M42,
                mat.M43,
                mat.M44
                );
        }

        #endregion

        #region Public Fields

        /// <summary>
        /// Value at row 1, column 1 of the matrix.
        /// </summary>
        public Double M11;

        /// <summary>
        /// Value at row 1, column 2 of the matrix.
        /// </summary>
        public Double M12;

        /// <summary>
        /// Value at row 1, column 3 of the matrix.
        /// </summary>
        public Double M13;

        /// <summary>
        /// Value at row 1, column 4 of the matrix.
        /// </summary>
        public Double M14;

        /// <summary>
        /// Value at row 2, column 1 of the matrix.
        /// </summary>
        public Double M21;

        /// <summary>
        /// Value at row 2, column 2 of the matrix.
        /// </summary>
        public Double M22;

        /// <summary>
        /// Value at row 2, column 3 of the matrix.
        /// </summary>
        public Double M23;

        /// <summary>
        /// Value at row 2, column 4 of the matrix.
        /// </summary>
        public Double M24;

        /// <summary>
        /// Value at row 3, column 1 of the matrix.
        /// </summary>
        public Double M31;

        /// <summary>
        /// Value at row 3, column 2 of the matrix.
        /// </summary>
        public Double M32;

        /// <summary>
        /// Value at row 3, column 3 of the matrix.
        /// </summary>
        public Double M33;

        /// <summary>
        /// Value at row 3, column 4 of the matrix.
        /// </summary>
        public Double M34;

        /// <summary>
        /// Value at row 4, column 1 of the matrix.
        /// </summary>
        public Double M41;

        /// <summary>
        /// Value at row 4, column 2 of the matrix.
        /// </summary>
        public Double M42;

        /// <summary>
        /// Value at row 4, column 3 of the matrix.
        /// </summary>
        public Double M43;

        /// <summary>
        /// Value at row 4, column 4 of the matrix.
        /// </summary>
        public Double M44;

        /// <summary>
        /// Returns the hash code for this instance.
        /// </summary>
        /// <returns>The hash code.</returns>
        public override int GetHashCode()
        {
            unchecked
            {
                return M11.GetHashCode() + M12.GetHashCode() + M13.GetHashCode() + M14.GetHashCode() +
                       M21.GetHashCode() + M22.GetHashCode() + M23.GetHashCode() + M24.GetHashCode() +
                       M31.GetHashCode() + M32.GetHashCode() + M33.GetHashCode() + M34.GetHashCode() +
                       M41.GetHashCode() + M42.GetHashCode() + M43.GetHashCode() + M44.GetHashCode();
            }
        }

        /// <summary>
        /// Returns a boolean indicating whether the given two matrices are equal.
        /// </summary>
        /// <param name="value1">The first matrix to compare.</param>
        /// <param name="value2">The second matrix to compare.</param>
        /// <returns>True if the given matrices are equal; False otherwise.</returns>
        public static bool operator ==(Matrix4x4Double value1, Matrix4x4Double value2)
        {
            return (value1.M11 == value2.M11 && value1.M22 == value2.M22 && value1.M33 == value2.M33 && value1.M44 == value2.M44 && // Check diagonal element first for early out.
                    value1.M12 == value2.M12 && value1.M13 == value2.M13 && value1.M14 == value2.M14 && value1.M21 == value2.M21 &&
                    value1.M23 == value2.M23 && value1.M24 == value2.M24 && value1.M31 == value2.M31 && value1.M32 == value2.M32 &&
                    value1.M34 == value2.M34 && value1.M41 == value2.M41 && value1.M42 == value2.M42 && value1.M43 == value2.M43);
        }

        /// <summary>
        /// Returns a boolean indicating whether the given two matrices are not equal.
        /// </summary>
        /// <param name="value1">The first matrix to compare.</param>
        /// <param name="value2">The second matrix to compare.</param>
        /// <returns>True if the given matrices are not equal; False if they are equal.</returns>
        public static bool operator !=(Matrix4x4Double value1, Matrix4x4Double value2)
        {
            return (value1.M11 != value2.M11 || value1.M12 != value2.M12 || value1.M13 != value2.M13 || value1.M14 != value2.M14 ||
                    value1.M21 != value2.M21 || value1.M22 != value2.M22 || value1.M23 != value2.M23 || value1.M24 != value2.M24 ||
                    value1.M31 != value2.M31 || value1.M32 != value2.M32 || value1.M33 != value2.M33 || value1.M34 != value2.M34 ||
                    value1.M41 != value2.M41 || value1.M42 != value2.M42 || value1.M43 != value2.M43 || value1.M44 != value2.M44);
        }

        /// <summary>
        /// Returns a boolean indicating whether this matrix instance is equal to the other given matrix.
        /// </summary>
        /// <param name="other">The matrix to compare this instance to.</param>
        /// <returns>True if the matrices are equal; False otherwise.</returns>
        public bool Equals(Matrix4x4Double other) => this == other;

        /// <summary>
        /// Returns a boolean indicating whether the given Object is equal to this matrix instance.
        /// </summary>
        /// <param name="obj">The Object to compare against.</param>
        /// <returns>True if the Object is equal to this matrix; False otherwise.</returns>
        public override bool Equals(object obj) => (obj is Matrix4x4Double other) && (this == other);

        #endregion Public Fields

        #region properties

        /// <summary>
        /// Returns the multiplicative identity matrix.
        /// </summary>
        public static Matrix4x4Double Identity
        {
            get { return _identity; }
        }

        /// <summary>
        /// Gets or sets the translation component of this matrix.
        /// </summary>
        public (Double x, Double y, Double z) Translation
        {
            get
            {
                return (M41, M42, M43);
            }
            set
            {
                M41 = value.x;
                M42 = value.y;
                M43 = value.z;
            }
        }

        #endregion

        #region methods

        /// <summary>
        /// Attempts to calculate the inverse of the given matrix. If successful, result will contain the inverted matrix.
        /// </summary>
        /// <param name="matrix">The source matrix to invert.</param>
        /// <param name="result">If successful, contains the inverted matrix.</param>
        /// <returns>True if the source matrix could be inverted; False otherwise.</returns>
        public static bool Invert(Matrix4x4Double matrix, out Matrix4x4Double result)
        {
            //                                       -1
            // If you have matrix M, inverse Matrix M   can compute
            //
            //     -1       1
            //    M   = --------- A
            //            det(M)
            //
            // A is adjugate (adjoint) of M, where,
            //
            //      T
            // A = C
            //
            // C is Cofactor matrix of M, where,
            //           i + j
            // C   = (-1)      * det(M  )
            //  ij                    ij
            //
            //     [ a b c d ]
            // M = [ e f g h ]
            //     [ i j k l ]
            //     [ m n o p ]
            //
            // First Row
            //           2 | f g h |
            // C   = (-1)  | j k l | = + ( f ( kp - lo ) - g ( jp - ln ) + h ( jo - kn ) )
            //  11         | n o p |
            //
            //           3 | e g h |
            // C   = (-1)  | i k l | = - ( e ( kp - lo ) - g ( ip - lm ) + h ( io - km ) )
            //  12         | m o p |
            //
            //           4 | e f h |
            // C   = (-1)  | i j l | = + ( e ( jp - ln ) - f ( ip - lm ) + h ( in - jm ) )
            //  13         | m n p |
            //
            //           5 | e f g |
            // C   = (-1)  | i j k | = - ( e ( jo - kn ) - f ( io - km ) + g ( in - jm ) )
            //  14         | m n o |
            //
            // Second Row
            //           3 | b c d |
            // C   = (-1)  | j k l | = - ( b ( kp - lo ) - c ( jp - ln ) + d ( jo - kn ) )
            //  21         | n o p |
            //
            //           4 | a c d |
            // C   = (-1)  | i k l | = + ( a ( kp - lo ) - c ( ip - lm ) + d ( io - km ) )
            //  22         | m o p |
            //
            //           5 | a b d |
            // C   = (-1)  | i j l | = - ( a ( jp - ln ) - b ( ip - lm ) + d ( in - jm ) )
            //  23         | m n p |
            //
            //           6 | a b c |
            // C   = (-1)  | i j k | = + ( a ( jo - kn ) - b ( io - km ) + c ( in - jm ) )
            //  24         | m n o |
            //
            // Third Row
            //           4 | b c d |
            // C   = (-1)  | f g h | = + ( b ( gp - ho ) - c ( fp - hn ) + d ( fo - gn ) )
            //  31         | n o p |
            //
            //           5 | a c d |
            // C   = (-1)  | e g h | = - ( a ( gp - ho ) - c ( ep - hm ) + d ( eo - gm ) )
            //  32         | m o p |
            //
            //           6 | a b d |
            // C   = (-1)  | e f h | = + ( a ( fp - hn ) - b ( ep - hm ) + d ( en - fm ) )
            //  33         | m n p |
            //
            //           7 | a b c |
            // C   = (-1)  | e f g | = - ( a ( fo - gn ) - b ( eo - gm ) + c ( en - fm ) )
            //  34         | m n o |
            //
            // Fourth Row
            //           5 | b c d |
            // C   = (-1)  | f g h | = - ( b ( gl - hk ) - c ( fl - hj ) + d ( fk - gj ) )
            //  41         | j k l |
            //
            //           6 | a c d |
            // C   = (-1)  | e g h | = + ( a ( gl - hk ) - c ( el - hi ) + d ( ek - gi ) )
            //  42         | i k l |
            //
            //           7 | a b d |
            // C   = (-1)  | e f h | = - ( a ( fl - hj ) - b ( el - hi ) + d ( ej - fi ) )
            //  43         | i j l |
            //
            //           8 | a b c |
            // C   = (-1)  | e f g | = + ( a ( fk - gj ) - b ( ek - gi ) + c ( ej - fi ) )
            //  44         | i j k |
            //
            // Cost of operation
            // 53 adds, 104 muls, and 1 div.
            double a = matrix.M11, b = matrix.M12, c = matrix.M13, d = matrix.M14;
            double e = matrix.M21, f = matrix.M22, g = matrix.M23, h = matrix.M24;
            double i = matrix.M31, j = matrix.M32, k = matrix.M33, l = matrix.M34;
            double m = matrix.M41, n = matrix.M42, o = matrix.M43, p = matrix.M44;

            double kp_lo = (k * p) - (l * o);
            double jp_ln = (j * p) - (l * n);
            double jo_kn = (j * o) - (k * n);
            double ip_lm = (i * p) - (l * m);
            double io_km = (i * o) - (k * m);
            double in_jm = (i * n) - (j * m);

            double a11 = +(f * kp_lo - g * jp_ln + h * jo_kn);
            double a12 = -(e * kp_lo - g * ip_lm + h * io_km);
            double a13 = +(e * jp_ln - f * ip_lm + h * in_jm);
            double a14 = -(e * jo_kn - f * io_km + g * in_jm);

            double det = (a * a11) + (b * a12) + (c * a13) + (d * a14);

            if (Math.Abs(det) < double.Epsilon)
            {
                result = new Matrix4x4Double(double.NaN, double.NaN, double.NaN, double.NaN,
                                       double.NaN, double.NaN, double.NaN, double.NaN,
                                       double.NaN, double.NaN, double.NaN, double.NaN,
                                       double.NaN, double.NaN, double.NaN, double.NaN);
                return false;
            }

            double invDet = 1.0 / det;

            result.M11 = a11 * invDet;
            result.M21 = a12 * invDet;
            result.M31 = a13 * invDet;
            result.M41 = a14 * invDet;

            result.M12 = -(b * kp_lo - c * jp_ln + d * jo_kn) * invDet;
            result.M22 = +(a * kp_lo - c * ip_lm + d * io_km) * invDet;
            result.M32 = -(a * jp_ln - b * ip_lm + d * in_jm) * invDet;
            result.M42 = +(a * jo_kn - b * io_km + c * in_jm) * invDet;

            double gp_ho = (g * p) - (h * o);
            double fp_hn = (f * p) - (h * n);
            double fo_gn = (f * o) - (g * n);
            double ep_hm = (e * p) - (h * m);
            double eo_gm = (e * o) - (g * m);
            double en_fm = (e * n) - (f * m);

            result.M13 = +(b * gp_ho - c * fp_hn + d * fo_gn) * invDet;
            result.M23 = -(a * gp_ho - c * ep_hm + d * eo_gm) * invDet;
            result.M33 = +(a * fp_hn - b * ep_hm + d * en_fm) * invDet;
            result.M43 = -(a * fo_gn - b * eo_gm + c * en_fm) * invDet;

            double gl_hk = (g * l) - (h * k);
            double fl_hj = (f * l) - (h * j);
            double fk_gj = (f * k) - (g * j);
            double el_hi = (e * l) - (h * i);
            double ek_gi = (e * k) - (g * i);
            double ej_fi = (e * j) - (f * i);

            result.M14 = -(b * gl_hk - c * fl_hj + d * fk_gj) * invDet;
            result.M24 = +(a * gl_hk - c * el_hi + d * ek_gi) * invDet;
            result.M34 = -(a * fl_hj - b * el_hi + d * ej_fi) * invDet;
            result.M44 = +(a * fk_gj - b * ek_gi + c * ej_fi) * invDet;

            return true;
        }

        /// <summary>
        /// Multiplies a matrix by another matrix.
        /// </summary>
        /// <param name="value1">The first source matrix.</param>
        /// <param name="value2">The second source matrix.</param>
        /// <returns>The result of the multiplication.</returns>
        public static Matrix4x4Double Multiply(Matrix4x4Double value1, Matrix4x4Double value2) => value1 * value2;

        /// <summary>
        /// Multiplies a matrix by another matrix.
        /// </summary>
        /// <param name="value1">The first source matrix.</param>
        /// <param name="value2">The second source matrix.</param>
        /// <returns>The result of the multiplication.</returns>
        public static Matrix4x4Double operator *(Matrix4x4Double value1, Matrix4x4Double value2)
        {
            Matrix4x4Double m;

            // First row

            m.M11 = value1.M11 * value2.M11 + value1.M12 * value2.M21 + value1.M13 * value2.M31 + value1.M14 * value2.M41;
            m.M12 = value1.M11 * value2.M12 + value1.M12 * value2.M22 + value1.M13 * value2.M32 + value1.M14 * value2.M42;
            m.M13 = value1.M11 * value2.M13 + value1.M12 * value2.M23 + value1.M13 * value2.M33 + value1.M14 * value2.M43;
            m.M14 = value1.M11 * value2.M14 + value1.M12 * value2.M24 + value1.M13 * value2.M34 + value1.M14 * value2.M44;

            // Second row
            m.M21 = value1.M21 * value2.M11 + value1.M22 * value2.M21 + value1.M23 * value2.M31 + value1.M24 * value2.M41;
            m.M22 = value1.M21 * value2.M12 + value1.M22 * value2.M22 + value1.M23 * value2.M32 + value1.M24 * value2.M42;
            m.M23 = value1.M21 * value2.M13 + value1.M22 * value2.M23 + value1.M23 * value2.M33 + value1.M24 * value2.M43;
            m.M24 = value1.M21 * value2.M14 + value1.M22 * value2.M24 + value1.M23 * value2.M34 + value1.M24 * value2.M44;

            // Third row
            m.M31 = value1.M31 * value2.M11 + value1.M32 * value2.M21 + value1.M33 * value2.M31 + value1.M34 * value2.M41;
            m.M32 = value1.M31 * value2.M12 + value1.M32 * value2.M22 + value1.M33 * value2.M32 + value1.M34 * value2.M42;
            m.M33 = value1.M31 * value2.M13 + value1.M32 * value2.M23 + value1.M33 * value2.M33 + value1.M34 * value2.M43;
            m.M34 = value1.M31 * value2.M14 + value1.M32 * value2.M24 + value1.M33 * value2.M34 + value1.M34 * value2.M44;

            // Fourth row
            m.M41 = value1.M41 * value2.M11 + value1.M42 * value2.M21 + value1.M43 * value2.M31 + value1.M44 * value2.M41;
            m.M42 = value1.M41 * value2.M12 + value1.M42 * value2.M22 + value1.M43 * value2.M32 + value1.M44 * value2.M42;
            m.M43 = value1.M41 * value2.M13 + value1.M42 * value2.M23 + value1.M43 * value2.M33 + value1.M44 * value2.M43;
            m.M44 = value1.M41 * value2.M14 + value1.M42 * value2.M24 + value1.M43 * value2.M34 + value1.M44 * value2.M44;

            return m;
        }

        #endregion
    }

    #pragma warning restore SA1407 // Arithmetic expressions should declare precedence
}
