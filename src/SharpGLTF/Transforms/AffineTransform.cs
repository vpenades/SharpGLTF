using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace SharpGLTF.Transforms
{
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

        public Quaternion Rotation;
        public Vector3 Scale;
        public Vector3 Translation; // Origin

        #endregion

        #region properties

        public Matrix4x4 Matrix
        {
            get
            {
                return Matrix4x4.CreateScale(Scale) * Matrix4x4.CreateFromQuaternion(Rotation) * Matrix4x4.CreateTranslation(Translation);
            }
        }

        #endregion

        #region API

        public static Matrix4x4 Evaluate(Matrix4x4? m, Vector3? s, Quaternion? r, Vector3? t)
        {
            if (m.HasValue) return m.Value;

            return new AffineTransform(null, s, r, t).Matrix;
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
