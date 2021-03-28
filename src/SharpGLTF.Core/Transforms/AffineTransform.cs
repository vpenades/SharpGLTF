using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace SharpGLTF.Transforms
{
    /// <summary>
    /// Represents an affine transform in 3D space, with two exclusive representantions:<br/>
    /// <list type="bullet">
    /// <item>
    /// When <see cref="IsMatrix"/> is true, A 4x3 Matrix. which is publicly<br/>
    /// exposed as a <see cref="Matrix4x4"/> matrix.
    /// </item>
    /// <item>
    /// When <see cref="IsDecomposed"/> is true, A decomposed transform defined by:<br/>
    /// <see cref="Vector3"/> Scale.<br/>
    /// <see cref="Quaternion"/> Rotation.<br/>
    /// <see cref="Vector3"/> Translation.
    /// </item>
    /// </list>
    /// </summary>
    /// <remarks>
    /// <para>
    /// Depending on how <see cref="AffineTransform"/> structures are created, the underlaying<br/>
    /// fields must be interprested as a Matrix4x3 or a decomposed sequence of Scale, Rotation and<br/>
    /// Translation.
    /// </para>
    /// <para>
    /// This approach allows <see cref="AffineTransform"/> to preserve the source data, avoiding loosing<br/>
    /// precission when decomposing a matrix, or creating a matrix from a SRT transform.
    /// </para>
    /// <para>
    /// Decomposing matrices is tricky because not all valid matrices can be decomposed; in particular<br/>
    /// squewed matrices will fail to decompose. See <see href="https://github.com/vpenades/SharpGLTF/issues/41"/>.
    /// </para>
    /// </remarks>
    [System.Diagnostics.DebuggerDisplay("AffineTransform 𝐒:{Scale} 𝐑:{Rotation} 𝚻:{Translation}")]
    public readonly struct AffineTransform
    {
        #region constants

        private const string _CannotDecomposeMessage = "Matrix is invalid or skewed.";

        public static readonly AffineTransform Identity = new AffineTransform(null, null, null);

        #endregion

        #region factories

        public static implicit operator AffineTransform(Matrix4x4 matrix) { return new AffineTransform(matrix); }

        public static AffineTransform CreateDecomposed(Matrix4x4 matrix)
        {
            if (!Matrix4x4.Decompose(matrix, out var s, out var r, out var t)) throw new ArgumentException("Can't decompose", nameof(matrix));

            return new AffineTransform(s, r, t);
        }

        public static AffineTransform CreateFromAny(Matrix4x4? matrix, Vector3? scale, Quaternion? rotation, Vector3? translation)
        {
            if (matrix.HasValue)
            {
                return new AffineTransform(matrix.Value);
            }
            else
            {
                return new AffineTransform(scale, rotation, translation);
            }
        }

        public AffineTransform WithScale(Vector3 scale)
        {
            return new AffineTransform(scale, this.Rotation, this.Translation);
        }

        public AffineTransform WithRotation(Quaternion rotation)
        {
            return new AffineTransform(this.Scale, rotation, this.Translation);
        }

        public AffineTransform WithTranslation(Vector3 translation)
        {
            return new AffineTransform(this.Scale, this.Rotation, translation);
        }

        #endregion

        #region constructors

        public AffineTransform(Matrix4x4 matrix)
        {
            if (matrix.M14 != 0) throw new ArgumentException(nameof(matrix));
            if (matrix.M24 != 0) throw new ArgumentException(nameof(matrix));
            if (matrix.M34 != 0) throw new ArgumentException(nameof(matrix));
            if (matrix.M44 != 1) throw new ArgumentException(nameof(matrix));

            _Representation = 0;

            _M11 = matrix.M11;
            _M12 = matrix.M12;
            _M13 = matrix.M13;

            _M21 = matrix.M21;
            _M22 = matrix.M22;
            _M23 = matrix.M23;

            _M31 = matrix.M31;
            _M32 = matrix.M32;
            _M33 = matrix.M33;

            _Translation = matrix.Translation;
        }

        public AffineTransform(Vector3? scale, Quaternion? rotation, Vector3? translation)
            : this(scale ?? Vector3.One, rotation ?? Quaternion.Identity, translation ?? Vector3.Zero)
        { }

        public AffineTransform(Vector3 scale, Quaternion rotation, Vector3 translation)
        {
            _Representation = 1;

            _M11 = scale.X;
            _M12 = scale.Y;
            _M13 = scale.Z;

            _M21 = rotation.X;
            _M22 = rotation.Y;
            _M23 = rotation.Z;
            _M31 = rotation.W;
            _M32 = 0;
            _M33 = 0;

            this._Translation = translation;
        }

        #endregion

        #region data

        /// <summary>
        /// Determines the underlaying representation:<br/>
        /// 0 - Fields must be interpreted as a Matrix4x3.<br/>
        /// 1 - Fields must be interpreted as a Scale, Rotation and Translation sequence.
        /// </summary>
        [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)]
        private readonly Int32 _Representation;

        /// <summary>
        /// Matrix:  M11<br/>
        /// Decomposed: Scale.X
        /// </summary>
        [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)]
        private readonly float _M11;

        /// <summary>
        /// Matrix:  M12<br/>
        /// Decomposed: Scale.Y
        /// </summary>
        [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)]
        private readonly float _M12;

        /// <summary>
        /// Matrix:  M13<br/>
        /// Decomposed: Scale.Z
        /// </summary>
        [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)]
        private readonly float _M13;

        /// <summary>
        /// Matrix:  M21<br/>
        /// Decomposed: Rotation.X
        /// </summary>
        [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)]
        private readonly float _M21;

        /// <summary>
        /// Matrix:  M22<br/>
        /// Decomposed: Rotation.Y
        /// </summary>
        [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)]
        private readonly float _M22;

        /// <summary>
        /// Matrix:  M23<br/>
        /// Decomposed: Rotation.Z
        /// </summary>
        [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)]
        private readonly float _M23;

        /// <summary>
        /// Matrix:  M31<br/>
        /// Decomposed: Rotation.W
        /// </summary>
        [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)]
        private readonly float _M31;

        /// <summary>
        /// Matrix:  M32<br/>
        /// Decomposed: unused
        /// </summary>
        [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)]
        private readonly float _M32;

        /// <summary>
        /// Matrix:  M32<br/>
        /// Decomposed: unused
        /// </summary>
        [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)]
        private readonly float _M33;

        [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)]
        private readonly Vector3 _Translation;

        #endregion

        #region properties
        public bool IsMatrix => _Representation == 0;
        public bool IsDecomposed => _Representation == 1;

        /// <summary>
        /// Gets the scale.
        /// </summary>
        /// <exception cref="InvalidOperationException">
        /// When the internal representation is a Matrix and cannot be decomposed.
        /// </exception>
        public Vector3 Scale => _GetScale();

        /// <summary>
        /// Gets the rotation.
        /// </summary>
        /// <exception cref="InvalidOperationException">
        /// When the internal representation is a Matrix and cannot be decomposed.
        /// </exception>
        public Quaternion Rotation => _GetRotation();

        /// <summary>
        /// Gets the translation
        /// </summary>
        public Vector3 Translation => _Translation;

        /// <summary>
        /// Gets the <see cref="Matrix4x4"/> transform of the current <see cref="AffineTransform"/>
        /// </summary>
        public Matrix4x4 Matrix => _GetMatrix();

        public bool IsValid
        {
            get
            {
                if (!Translation._IsFinite()) return false;

                if (!_M11._IsFinite()) return false;
                if (!_M12._IsFinite()) return false;
                if (!_M13._IsFinite()) return false;

                if (!_M21._IsFinite()) return false;
                if (!_M22._IsFinite()) return false;
                if (!_M23._IsFinite()) return false;

                if (!_M31._IsFinite()) return false;
                if (!_M32._IsFinite()) return false;
                if (!_M33._IsFinite()) return false;

                return true;
            }
        }

        public bool IsIdentity
        {
            get
            {
                if (Translation != Vector3.Zero) return false;

                if (IsDecomposed)
                {
                    // scale
                    if (_M11 != 1) return false;
                    if (_M12 != 1) return false;
                    if (_M13 != 1) return false;

                    // rotation
                    if (_M21 != 0) return false;
                    if (_M22 != 0) return false;
                    if (_M23 != 0) return false;
                    if (_M31 != 1) return false;
                }
                else
                {
                    // row 1
                    if (_M11 != 1) return false;
                    if (_M12 != 0) return false;
                    if (_M13 != 0) return false;

                    // row 2
                    if (_M21 != 0) return false;
                    if (_M22 != 1) return false;
                    if (_M23 != 0) return false;

                    // row 3
                    if (_M31 != 0) return false;
                    if (_M32 != 0) return false;
                    if (_M33 != 1) return false;
                }

                return true;
            }
        }

        #endregion

        #region API

        private Matrix4x4 _GetMatrix()
        {
            if (IsMatrix)
            {
                return new Matrix4x4
                (
                    _M11, _M12, _M13, 0,
                    _M21, _M22, _M23, 0,
                    _M31, _M32, _M33, 0,
                    _Translation.X, _Translation.Y, _Translation.Z, 1
                );
            }

            var m = Matrix4x4.CreateScale(this.Scale) * Matrix4x4.CreateFromQuaternion(this.Rotation.Sanitized());
            m.Translation = this.Translation;
            return m;
        }

        private Vector3 _GetScale()
        {
            if (IsDecomposed) return new Vector3(_M11, _M12, _M13);
            if (Matrix4x4.Decompose(_GetMatrix(), out var scale, out _, out _)) return scale;
            throw new InvalidOperationException(_CannotDecomposeMessage);
        }

        private Quaternion _GetRotation()
        {
            if (IsDecomposed) return new Quaternion(_M21, _M22, _M23, _M31);
            if (Matrix4x4.Decompose(_GetMatrix(), out _, out var rotation, out _)) return rotation;
            throw new InvalidOperationException(_CannotDecomposeMessage);
        }

        public AffineTransform GetDecomposed()
        {
            if (IsDecomposed) return this;
            if (!Matrix4x4.Decompose(Matrix, out var s, out var r, out var t)) throw new InvalidOperationException(_CannotDecomposeMessage);
            return new AffineTransform(s, r, t);
        }

        public static AffineTransform Blend(ReadOnlySpan<AffineTransform> transforms, ReadOnlySpan<float> weights)
        {
            var s = Vector3.Zero;
            var r = default(Quaternion);
            var t = Vector3.Zero;

            for (int i = 0; i < transforms.Length; ++i)
            {
                var w = weights[i];

                s += transforms[i].Scale * w;
                r += transforms[i].Rotation * w;
                t += transforms[i].Translation * w;
            }

            r = Quaternion.Normalize(r);

            return new  AffineTransform(s, r, t);
        }

        public static AffineTransform operator *(in AffineTransform a, in AffineTransform b)
        {
            return Multiply(a, b);
        }

        /// <summary>
        /// Multiplies <paramref name="a"/> by <paramref name="b"/>.
        /// </summary>
        /// <param name="a">The left transform.</param>
        /// <param name="b">The right transform.</param>
        /// <returns>
        /// <para>A new <see cref="AffineTransform"/> structure.</para>
        /// <para>
        /// The returned value will use a decomposed<br/>
        /// representation it these two conditions are met:<br/>
        /// <list type="number">
        /// <item>Arguments <paramref name="a"/> and <paramref name="b"/> are also in decomposed form.</item>
        /// <item>The result of the operation is decomposable.</item>
        /// </list>
        /// Otherwise the returned value will use a Matrix representation.
        /// </para>
        /// </returns>
        public static AffineTransform Multiply(in AffineTransform a, in AffineTransform b)
        {
            // if any of the two operators is a matrix, perform a matrix multiplication.
            if (a.IsMatrix || b.IsMatrix)
            {
                return new AffineTransform(a.Matrix * b.Matrix);
            }

            // if the B operator has an uneven scale AND a rotation, performa a matrix multiplication
            // which produces a squeezed matrix and cannot be decomposed.

            var sb = b.Scale;

            if (!(sb.X == sb.Y && sb.X == sb.Z) && b.Rotation != Quaternion.Identity)
            {
                return new AffineTransform(a.Matrix * b.Matrix);
            }

            // we're safe to make a decomposed multiplication

            var s = _Vector3Transform(b.Scale * _Vector3Transform(a.Scale, a.Rotation), Quaternion.Inverse(a.Rotation));

            var r = Quaternion.Multiply(b.Rotation, a.Rotation);

            var t
                = b.Translation
                + _Vector3Transform(a.Translation * b.Scale, b.Rotation);

            return new AffineTransform(s, r, t);
        }

        /// <summary>
        /// This method is equivalent to System.Numerics.Vector3.Transform(Vector3 v, Quaternion q)
        /// </summary>
        /// <param name="v">The vector to transform</param>
        /// <param name="q">The transform rotation</param>
        /// <returns>The rotated vector</returns>
        private static Vector3 _Vector3Transform(Vector3 v, Quaternion q)
        {
            // Extract the vector part of the quaternion
            var u = new Vector3(q.X, q.Y, q.Z);

            // Extract the scalar part of the quaternion
            var s = q.W;

            // Do the math
            return (2.0f * Vector3.Dot(u, v) * u)
                + (((s * s) - Vector3.Dot(u, u)) * v)
                + (2.0f * s * Vector3.Cross(u, v));
        }

        #endregion
    }
}
