using System;
using System.Collections.Generic;
using System.Linq;

using Microsoft.Xna.Framework;

namespace SharpGLTF.Runtime
{
    static class _Extensions
    {
        public static Vector2 ToXna(this System.Numerics.Vector2 v)
        {
            return new Vector2(v.X, v.Y);
        }

        public static Vector3 ToXna(this System.Numerics.Vector3 v)
        {
            return new Vector3(v.X, v.Y, v.Z);
        }

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

        public static void FixTextureSampler(this Schema2.ModelRoot root)
        {
            // SharpGLTF 1.0.0-Alpha10 has an issue with TextureSamplers, it's fixed in newer versions

            foreach(var t in root.LogicalTextures)
            {
                if (t.Sampler == null)
                {
                    var sampler = root.UseTextureSampler
                        (
                        Schema2.TextureWrapMode.REPEAT,
                        Schema2.TextureWrapMode.REPEAT,
                        Schema2.TextureMipMapFilter.DEFAULT,
                        Schema2.TextureInterpolationFilter.LINEAR
                        );

                    t.Sampler = sampler;
                }
            }
        }

        public static BoundingSphere CreateBoundingSphere(this Schema2.Mesh mesh)
        {
            var points = mesh
                .Primitives
                .Select(item => item.GetVertexAccessor("POSITION"))
                .Where(item => item != null)
                .SelectMany(item => item.AsVector3Array())
                .Select(item => item.ToXna());

            return BoundingSphere.CreateFromPoints(points);
        }
    }
}
