using System;
using System.Collections.Generic;
using System.Linq;

using Microsoft.Xna.Framework;

namespace SharpGLTF.Runtime
{
    static class _Extensions
    {
        public static Vector4 ToXna(this System.Numerics.Vector4 v)
        {
            return new Vector4(v.X, v.Y, v.Z, v.W);
        }

        public static Matrix ToXna(this System.Numerics.Matrix4x4 m)
        {
            return new Matrix
                (
                m.M11, m.M12, m.M13, m.M14,
                m.M21, m.M22, m.M23, m.M24,
                m.M31, m.M32, m.M33, m.M34,
                m.M41, m.M42, m.M43, m.M44
                );
        }
    }
}
