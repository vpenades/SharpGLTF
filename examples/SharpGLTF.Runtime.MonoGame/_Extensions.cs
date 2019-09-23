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

        public static void FixTextureSampler(this SharpGLTF.Schema2.ModelRoot root)
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

        public static int GetPrimitiveVertexSize(this Schema2.PrimitiveType ptype)
        {
            switch (ptype)
            {
                case Schema2.PrimitiveType.POINTS: return 1;
                case Schema2.PrimitiveType.LINES: return 2;
                case Schema2.PrimitiveType.LINE_LOOP: return 2;
                case Schema2.PrimitiveType.LINE_STRIP: return 2;
                case Schema2.PrimitiveType.TRIANGLES: return 3;
                case Schema2.PrimitiveType.TRIANGLE_FAN: return 3;
                case Schema2.PrimitiveType.TRIANGLE_STRIP: return 3;
                default: throw new NotImplementedException();
            }
        }

        public static IEnumerable<(int, int, int)> GetTriangleIndices(this Schema2.MeshPrimitive primitive)
        {
            if (primitive == null || primitive.DrawPrimitiveType.GetPrimitiveVertexSize() != 3) return Enumerable.Empty<(int, int, int)>();

            if (primitive.IndexAccessor == null) return primitive.DrawPrimitiveType.GetTrianglesIndices(primitive.GetVertexAccessor("POSITION").Count);

            return primitive.DrawPrimitiveType.GetTrianglesIndices(primitive.IndexAccessor.AsIndicesArray());
        }

        public static IEnumerable<(int, int, int)> GetTrianglesIndices(this Schema2.PrimitiveType ptype, int vertexCount)
        {
            return ptype.GetTrianglesIndices(Enumerable.Range(0, vertexCount).Select(item => (UInt32)item));
        }

        public static IEnumerable<(int, int, int)> GetTrianglesIndices(this Schema2.PrimitiveType ptype, IEnumerable<UInt32> sourceIndices)
        {
            switch (ptype)
            {
                case Schema2.PrimitiveType.TRIANGLES:
                    {
                        using (var ptr = sourceIndices.GetEnumerator())
                        {
                            while (true)
                            {
                                if (!ptr.MoveNext()) break;
                                var a = ptr.Current;
                                if (!ptr.MoveNext()) break;
                                var b = ptr.Current;
                                if (!ptr.MoveNext()) break;
                                var c = ptr.Current;

                                if (!_IsDegenerated(a, b, c)) yield return ((int)a, (int)b, (int)c);
                            }
                        }

                        break;
                    }

                case Schema2.PrimitiveType.TRIANGLE_FAN:
                    {
                        using (var ptr = sourceIndices.GetEnumerator())
                        {
                            if (!ptr.MoveNext()) break;
                            var a = ptr.Current;
                            if (!ptr.MoveNext()) break;
                            var b = ptr.Current;

                            while (true)
                            {
                                if (!ptr.MoveNext()) break;
                                var c = ptr.Current;

                                if (!_IsDegenerated(a, b, c)) yield return ((int)a, (int)b, (int)c);

                                b = c;
                            }
                        }

                        break;
                    }

                case Schema2.PrimitiveType.TRIANGLE_STRIP:
                    {
                        using (var ptr = sourceIndices.GetEnumerator())
                        {
                            if (!ptr.MoveNext()) break;
                            var a = ptr.Current;
                            if (!ptr.MoveNext()) break;
                            var b = ptr.Current;

                            bool reversed = false;

                            while (true)
                            {
                                if (!ptr.MoveNext()) break;
                                var c = ptr.Current;

                                if (!_IsDegenerated(a, b, c))
                                {
                                    if (reversed) yield return ((int)b, (int)a, (int)c);
                                    else yield return ((int)a, (int)b, (int)c);
                                }

                                a = b;
                                b = c;
                                reversed = !reversed;
                            }
                        }

                        break;
                    }

                default: throw new NotImplementedException();
            }            
        }

        static bool _IsDegenerated(uint a, uint b, uint c)
        {
            if (a == b) return true;
            if (a == c) return true;
            if (b == c) return true;
            return false;
        }


    }
}
