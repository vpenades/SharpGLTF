using System;
using System.Linq;
using System.Numerics;

namespace SharpGLTF.Geometry.VertexTypes
{
    static partial class VertexUtils
    {
        public static (Type BuilderType, Func<IVertexBuilder> BuilderFactory) GetVertexBuilderType(params string[] vertexAttributes)
        {
            var hasNormals = vertexAttributes.Contains("NORMAL");
            var hasTangents = vertexAttributes.Contains("TANGENT");

            var colors = 0;
            if (vertexAttributes.Contains("COLOR_0")) colors = Math.Max(colors, 1);
            if (vertexAttributes.Contains("COLOR_1")) colors = Math.Max(colors, 2);
            if (vertexAttributes.Contains("COLOR_2")) colors = Math.Max(colors, 3);
            if (vertexAttributes.Contains("COLOR_3")) colors = Math.Max(colors, 4);
            if (vertexAttributes.Contains("COLOR_4")) colors = Math.Max(colors, 5);
            if (vertexAttributes.Contains("COLOR_5")) colors = Math.Max(colors, 6);
            if (vertexAttributes.Contains("COLOR_6")) colors = Math.Max(colors, 7);
            if (vertexAttributes.Contains("COLOR_7")) colors = Math.Max(colors, 8);

            var uvcoords = 0;
            if (vertexAttributes.Contains("TEXCOORD_0")) uvcoords = Math.Max(uvcoords, 1);
            if (vertexAttributes.Contains("TEXCOORD_1")) uvcoords = Math.Max(uvcoords, 2);
            if (vertexAttributes.Contains("TEXCOORD_2")) uvcoords = Math.Max(uvcoords, 3);
            if (vertexAttributes.Contains("TEXCOORD_3")) uvcoords = Math.Max(uvcoords, 4);
            if (vertexAttributes.Contains("TEXCOORD_4")) uvcoords = Math.Max(uvcoords, 5);
            if (vertexAttributes.Contains("TEXCOORD_5")) uvcoords = Math.Max(uvcoords, 6);
            if (vertexAttributes.Contains("TEXCOORD_6")) uvcoords = Math.Max(uvcoords, 7);
            if (vertexAttributes.Contains("TEXCOORD_7")) uvcoords = Math.Max(uvcoords, 8);

            var joints = vertexAttributes.Contains("JOINTS_0") && vertexAttributes.Contains("WEIGHTS_0") ? 4 : 0;
            joints = vertexAttributes.Contains("JOINTS_1") && vertexAttributes.Contains("WEIGHTS_1") ? 8 : joints;

            // WARNING: not all Color x Texture higher values have been defined yet.

            return GetVertexBuilderType(hasNormals, hasTangents, colors, uvcoords, joints);
        }        

        public static TvP ConvertToGeometry<TvP>(this IVertexGeometry src)
            where TvP : struct, IVertexGeometry
        {
            if (src is TvP srcTyped) return srcTyped;

            var dst = default(TvP);

            dst.SetPosition(src.GetPosition());
            if (src.TryGetNormal(out Vector3 nrm)) dst.SetNormal(nrm);
            if (src.TryGetTangent(out Vector4 tgt)) dst.SetTangent(tgt);

            return dst;
        }

        public static TvM ConvertToMaterial<TvM>(this IVertexMaterial src)
            where TvM : struct, IVertexMaterial
        {
            if (src is TvM srcTyped) return srcTyped;

            var dst = default(TvM);

            int i = 0;

            while (i < Math.Min(src.MaxColors, dst.MaxColors))
            {
                dst.SetColor(i, src.GetColor(i));
                ++i;
            }

            while (i < dst.MaxColors)
            {
                dst.SetColor(i, Vector4.One);
                ++i;
            }

            i = 0;

            while (i < Math.Min(src.MaxTextCoords, dst.MaxTextCoords))
            {
                dst.SetTexCoord(i, src.GetTexCoord(i));
                ++i;
            }

            while (i < dst.MaxTextCoords)
            {
                dst.SetTexCoord(i, Vector2.Zero);
                ++i;
            }

            if (src is IVertexCustom srcx && dst is IVertexCustom dstx)
            {
                foreach (var key in dstx.CustomAttributes)
                {
                    if (srcx.TryGetCustomAttribute(key, out object val))
                    {
                        dstx.SetCustomAttribute(key, val);
                    }
                }

                dst = (TvM)dstx; // unbox;
            }

            return dst;
        }

        public static TvS ConvertToSkinning<TvS>(this IVertexSkinning src)
            where TvS : struct, IVertexSkinning
        {
            if (src is TvS srcTyped) return srcTyped;
            var srcWeights = src.MaxBindings > 0 ? src.GetBindings() : default;

            var dst = default(TvS);
            if (dst.MaxBindings > 0) dst.SetBindings(srcWeights);

            return dst;
        }
    }
}
