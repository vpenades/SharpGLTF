using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace SharpGLTF.Transforms
{
    /// <summary>
    /// Represents an affine transform in 3D space, defined by:
    /// - A <see cref="Vector3"/> scale.
    /// - A <see cref="Quaternion"/> rotation.
    /// - A <see cref="Vector3"/> translation.
    /// </summary>
    /// <remarks>
    /// <see cref="AffineTransform"/> cannot represent skewed matrices. This means
    /// that it can be used to represent <see cref="Schema2.Node"/> local transforms,
    /// but since chained transforms can become skewed, a world transform cannot be
    /// represented by a <see cref="AffineTransform"/>.
    /// </remarks>
    /// <see href="https://github.com/vpenades/SharpGLTF/issues/41"/>
    [System.Diagnostics.DebuggerDisplay("AffineTransform 𝐒:{Scale} 𝐑:{Rotation} 𝚻:{Translation}")]
    public struct AffineTransform
    {
        #region lifecycle

        public static implicit operator AffineTransform(Matrix4x4 matrix) { return new AffineTransform(matrix); }

        public AffineTransform(Matrix4x4 matrix)
        {
            if (!Matrix4x4.Decompose(matrix, out this.Scale, out this.Rotation, out this.Translation))
            {
                throw new ArgumentException("matrix is invalid or skewed.", nameof(matrix));
            }
        }

        public AffineTransform(Vector3? scale, Quaternion? rotation, Vector3? translation)
        {
            this.Scale = scale ?? Vector3.One;
            this.Rotation = rotation ?? Quaternion.Identity;
            this.Translation = translation ?? Vector3.Zero;
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
        public  Vector3 Translation;

        #endregion

        #region properties

        public static AffineTransform Identity => new AffineTransform { Rotation = Quaternion.Identity, Scale = Vector3.One, Translation = Vector3.Zero };

        /// <summary>
        /// Gets the <see cref="Matrix4x4"/> transform of the current <see cref="AffineTransform"/>
        /// </summary>
        public Matrix4x4 Matrix
        {
            get
            {
                var m = Matrix4x4.CreateScale(this.Scale) * Matrix4x4.CreateFromQuaternion(this.Rotation.Sanitized());
                m.Translation = this.Translation;
                return m;
            }
        }

        public bool IsValid
        {
            get
            {
                if (!Scale._IsFinite()) return false;
                if (!Rotation._IsFinite()) return false;
                if (!Translation._IsFinite()) return false;

                return true;
            }
        }

        public bool IsIdentity
        {
            get
            {
                if (Scale != Vector3.One) return false;
                if (Rotation != Quaternion.Identity) return false;
                if (Translation != Vector3.Zero) return false;
                return true;
            }
        }

        #endregion

        #region API

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

        public static AffineTransform Multiply(in AffineTransform a, in AffineTransform b)
        {
            AffineTransform r;

            r.Scale = Vector3Transform(b.Scale * Vector3Transform(a.Scale, a.Rotation), Quaternion.Inverse(a.Rotation));

            r.Rotation = Quaternion.Multiply(b.Rotation, a.Rotation);

            r.Translation
                = b.Translation
                + Vector3Transform(a.Translation * b.Scale, b.Rotation);

            return r;
        }

        /// <summary>
        /// This method is equivalent to System.Numerics.Vector3.Transform(Vector3 v, Quaternion q)
        /// </summary>
        /// <param name="v">The vector to transform</param>
        /// <param name="q">The transform rotation</param>
        /// <returns>The rotated vector</returns>
        private static Vector3 Vector3Transform(Vector3 v, Quaternion q)
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
