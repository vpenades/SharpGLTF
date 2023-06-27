using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Numerics;

namespace SharpGLTF.Geometry.VertexTypes
{
    static partial class VertexUtils
    {
        #if !NETSTANDARD
        [return: DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)]
        #endif
        public static Type GetVertexGeometryType(params string[] vertexAttributes)
        {
            var t = typeof(VertexPosition);
            if (vertexAttributes.Contains("NORMAL")) t = typeof(VertexPositionNormal);
            if (vertexAttributes.Contains("TANGENT")) t = typeof(VertexPositionNormalTangent);
            return t;
        }

        #if !NETSTANDARD
        [return: DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)]
        #endif
        public static Type GetVertexMaterialType(params string[] vertexAttributes)
        {
            var colors = vertexAttributes.Contains("COLOR_0") ? 1 : 0;
            colors = vertexAttributes.Contains("COLOR_1") ? 2 : colors;
            colors = vertexAttributes.Contains("COLOR_2") ? 3 : colors;
            colors = vertexAttributes.Contains("COLOR_3") ? 4 : colors;

            var uvcoords = vertexAttributes.Contains("TEXCOORD_0") ? 1 : 0;
            uvcoords = vertexAttributes.Contains("TEXCOORD_1") ? 2 : uvcoords;
            uvcoords = vertexAttributes.Contains("TEXCOORD_2") ? 3 : uvcoords;
            uvcoords = vertexAttributes.Contains("TEXCOORD_3") ? 4 : uvcoords;

            return GetVertexMaterialType(colors, uvcoords);
        }

        #if !NETSTANDARD
        [return: DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)]
        #endif
        public static Type GetVertexMaterialType(int colors, int uvcoords)
        {
            if (colors == 0)
            {
                if (uvcoords == 0) return typeof(VertexEmpty);
                if (uvcoords == 1) return typeof(VertexTexture1);
                if (uvcoords >= 2) return typeof(VertexTexture2);
            }

            if (colors == 1)
            {
                if (uvcoords == 0) return typeof(VertexColor1);
                if (uvcoords == 1) return typeof(VertexColor1Texture1);
                if (uvcoords >= 2) return typeof(VertexColor1Texture2);
            }

            if (colors >= 2)
            {
                if (uvcoords == 0) return typeof(VertexColor2);
                if (uvcoords == 1) return typeof(VertexColor2Texture1);
                if (uvcoords >= 2) return typeof(VertexColor2Texture2);
            }

            return typeof(VertexEmpty);
        }

        #if !NETSTANDARD
        [return: DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)]
        #endif
        public static Type GetVertexSkinningType(params string[] vertexAttributes)
        {
            var joints = vertexAttributes.Contains("JOINTS_0") && vertexAttributes.Contains("WEIGHTS_0") ? 4 : 0;
            joints = vertexAttributes.Contains("JOINTS_1") && vertexAttributes.Contains("WEIGHTS_1") ? 8 : joints;

            if (joints == 4) return typeof(VertexJoints4);
            if (joints == 8) return typeof(VertexJoints8);

            return typeof(VertexEmpty);
        }

        #if !NETSTANDARD
        [return: DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)]
        #endif
        public static Type GetVertexBuilderType(params string[] vertexAttributes)
        {
            var tvg = GetVertexGeometryType(vertexAttributes);
            var tvm = GetVertexMaterialType(vertexAttributes);
            var tvs = GetVertexSkinningType(vertexAttributes);

            var vtype = typeof(VertexBuilder<,,>);

            return vtype.MakeGenericType(tvg, tvm, tvs);
        }

        #if !NETSTANDARD
        [return: DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)]
        #endif
        public static Type GetVertexBuilderType(bool hasNormals, bool hasTangents, int numCols, int numUV, int numJoints)
        {
            var tvg = typeof(VertexPosition);
            if (hasNormals) tvg = typeof(VertexPositionNormal);
            if (hasTangents) tvg = typeof(VertexPositionNormalTangent);

            var tvm = GetVertexMaterialType(numCols, numUV);

            var tvs = typeof(VertexEmpty);
            if (numJoints == 4) tvs = typeof(VertexJoints4);
            if (numJoints >= 8) tvs = typeof(VertexJoints8);

            var vtype = typeof(VertexBuilder<,,>);

            return vtype.MakeGenericType(tvg, tvm, tvs);
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
