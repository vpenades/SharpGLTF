using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

using SharpGLTF.Memory;

namespace SharpGLTF.Geometry.VertexTypes
{
    static class VertexUtils
    {
        #region vertex builder

        public static Type GetVertexGeometryType(params string[] vertexAttributes)
        {
            var t = typeof(VertexPosition);
            if (vertexAttributes.Contains("NORMAL")) t = typeof(VertexPositionNormal);
            if (vertexAttributes.Contains("TANGENT")) t = typeof(VertexPositionNormalTangent);
            return t;
        }

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
                if (uvcoords == 1) return typeof(VertexColor2Texture2);
                if (uvcoords >= 2) return typeof(VertexColor2Texture2);
            }

            return typeof(VertexEmpty);
        }

        public static Type GetVertexSkinningType(params string[] vertexAttributes)
        {
            var joints = vertexAttributes.Contains("JOINTS_0") && vertexAttributes.Contains("WEIGHTS_0") ? 4 : 0;
            joints = vertexAttributes.Contains("JOINTS_1") && vertexAttributes.Contains("WEIGHTS_1") ? 8 : joints;

            if (joints == 4) return typeof(VertexJoints4);
            if (joints == 8) return typeof(VertexJoints8);

            return typeof(VertexEmpty);
        }

        public static Type GetVertexBuilderType(params string[] vertexAttributes)
        {
            var tvg = GetVertexGeometryType(vertexAttributes);
            var tvm = GetVertexMaterialType(vertexAttributes);
            var tvs = GetVertexSkinningType(vertexAttributes);

            var vtype = typeof(VertexBuilder<,,>);

            return vtype.MakeGenericType(tvg, tvm, tvs);
        }

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

        public static bool SanitizeVertex<TvG>(this TvG inVertex, out TvG outVertex)
            where TvG : struct, IVertexGeometry
        {
            outVertex = inVertex;

            var p = inVertex.GetPosition();

            if (!p._IsFinite()) return false;

            if (inVertex.TryGetNormal(out Vector3 n))
            {
                if (!n._IsFinite()) return false;
                if (n == Vector3.Zero) n = p;
                if (n == Vector3.Zero) return false;

                var l = n.Length();
                if (l < 0.99f || l > 0.01f) outVertex.SetNormal(Vector3.Normalize(n));
            }

            if (inVertex.TryGetTangent(out Vector4 tw))
            {
                if (!tw._IsFinite()) return false;

                var t = new Vector3(tw.X, tw.Y, tw.Z);
                if (t == Vector3.Zero) return false;

                if (tw.W > 0) tw.W = 1;
                if (tw.W < 0) tw.W = -1;

                var l = t.Length();
                if (l < 0.99f || l > 0.01f) t = Vector3.Normalize(t);

                outVertex.SetTangent(new Vector4(t, tw.W));
            }

            return true;
        }

        public static TvP ConvertToGeometry<TvP>(this IVertexGeometry src)
            where TvP : struct, IVertexGeometry
        {
            if (src.GetType() == typeof(TvP)) return (TvP)src;

            var dst = default(TvP);

            dst.SetPosition(src.GetPosition());
            if (src.TryGetNormal(out Vector3 nrm)) dst.SetNormal(nrm);
            if (src.TryGetTangent(out Vector4 tgt)) dst.SetTangent(tgt);

            return dst;
        }

        public static TvM ConvertToMaterial<TvM>(this IVertexMaterial src)
            where TvM : struct, IVertexMaterial
        {
            if (src.GetType() == typeof(TvM)) return (TvM)src;

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

            while (i < dst.MaxColors)
            {
                dst.SetTexCoord(i, Vector2.Zero);
                ++i;
            }

            return dst;
        }

        public static TvS ConvertToSkinning<TvS>(this IVertexSkinning src)
            where TvS : struct, IVertexSkinning
        {
            if (src.GetType() == typeof(TvS)) return (TvS)src;

            var sparse = src.MaxBindings > 0 ? src.GetWeights() : default;

            var dst = default(TvS);

            if (dst.MaxBindings > 0) dst.SetWeights(sparse);

            return dst;
        }

        #endregion

        #region memory buffers API

        public static MemoryAccessor CreateVertexMemoryAccessor<TVertex>(this IReadOnlyList<TVertex> vertices, string attributeName, Schema2.EncodingType jointEncoding)
            where TVertex : IVertexBuilder
        {
            if (vertices == null || vertices.Count == 0) return null;

            // determine the vertex attributes from the first vertex.
            var attributes = GetVertexAttributes(vertices[0], vertices.Count, jointEncoding);

            var attribute = attributes.FirstOrDefault(item => item.Name == attributeName);
            if (attribute.Name == null) return null;
            attribute.ByteOffset = 0;
            attribute.ByteStride = 0;

            // create a buffer
            var vbuffer = new ArraySegment<byte>(new Byte[attribute.PaddedByteLength * vertices.Count]);

            // fill the buffer with the vertex attributes.
            var accessor = new MemoryAccessor(vbuffer, attribute);

            accessor.FillAccessor(vertices);

            return accessor;
        }

        public static MemoryAccessor[] CreateVertexMemoryAccessors<TVertex>(this IReadOnlyList<TVertex> vertices, Schema2.EncodingType jointEncoding)
            where TVertex : IVertexBuilder
        {
            if (vertices == null || vertices.Count == 0) return null;

            // determine the vertex attributes from the first vertex.
            var attributes = GetVertexAttributes(vertices[0], vertices.Count, jointEncoding);

            // create a buffer
            int byteStride = attributes[0].ByteStride;
            var vbuffer = new ArraySegment<byte>(new Byte[byteStride * vertices.Count]);

            // fill the buffer with the vertex attributes.
            var accessors = MemoryAccessInfo
                .Slice(attributes, 0, vertices.Count)
                .Select(item => new MemoryAccessor(vbuffer, item))
                .ToArray();

            foreach (var accessor in accessors)
            {
                accessor.FillAccessor(vertices);
            }

            return accessors;
        }

        private static void FillAccessor<TVertex>(this MemoryAccessor dstAccessor, IReadOnlyList<TVertex> srcVertices)
            where TVertex : IVertexBuilder
        {
            var columnFunc = _GetVertexBuilderAttributeFunc(dstAccessor.Attribute.Name);

            if (dstAccessor.Attribute.Dimensions == Schema2.DimensionType.SCALAR) dstAccessor.AsScalarArray().Fill(srcVertices._GetColumn<TVertex, Single>(columnFunc));
            if (dstAccessor.Attribute.Dimensions == Schema2.DimensionType.VEC2) dstAccessor.AsVector2Array().Fill(srcVertices._GetColumn<TVertex, Vector2>(columnFunc));
            if (dstAccessor.Attribute.Dimensions == Schema2.DimensionType.VEC3) dstAccessor.AsVector3Array().Fill(srcVertices._GetColumn<TVertex, Vector3>(columnFunc));
            if (dstAccessor.Attribute.Dimensions == Schema2.DimensionType.VEC4) dstAccessor.AsVector4Array().Fill(srcVertices._GetColumn<TVertex, Vector4>(columnFunc));
        }

        public static MemoryAccessor CreateIndexMemoryAccessor(this IReadOnlyList<Int32> indices, Schema2.EncodingType encoding)
        {
            if (indices == null || indices.Count == 0) return null;

            var attribute = new MemoryAccessInfo("INDEX", 0, indices.Count, 0, Schema2.DimensionType.SCALAR, encoding);

            // create buffer
            var ibytes = new Byte[encoding.ByteLength() * indices.Count];
            var ibuffer = new ArraySegment<byte>(ibytes);

            // fill the buffer with indices.
            var accessor = new MemoryAccessor(ibuffer, attribute.Slice(0, indices.Count));

            accessor.AsIntegerArray().Fill(indices);

            return accessor;
        }

        public static MemoryAccessInfo[] GetVertexAttributes(this IVertexBuilder firstVertex, int vertexCount, Schema2.EncodingType jointEncoding)
        {
            var tvg = firstVertex.GetGeometry().GetType();
            var tvm = firstVertex.GetMaterial().GetType();
            var tvs = firstVertex.GetSkinning().GetType();

            var attributes = new List<MemoryAccessInfo>();

            foreach (var finfo in tvg.GetFields())
            {
                var attribute = _GetMemoryAccessInfo(finfo);
                if (attribute.HasValue) attributes.Add(attribute.Value);
            }

            foreach (var finfo in tvm.GetFields())
            {
                var attribute = _GetMemoryAccessInfo(finfo);
                if (attribute.HasValue) attributes.Add(attribute.Value);
            }

            foreach (var finfo in tvs.GetFields())
            {
                var attribute = _GetMemoryAccessInfo(finfo);
                if (attribute.HasValue)
                {
                    var a = attribute.Value;
                    if (a.Name.StartsWith("JOINTS_", StringComparison.OrdinalIgnoreCase)) a.Encoding = jointEncoding;

                    attributes.Add(a);
                }
            }

            var array = attributes.ToArray();

            MemoryAccessInfo.SetInterleavedInfo(array, 0, vertexCount);

            return array;
        }

        private static MemoryAccessInfo? _GetMemoryAccessInfo(System.Reflection.FieldInfo finfo)
        {
            var attribute = finfo.GetCustomAttributes(true)
                    .OfType<VertexAttributeAttribute>()
                    .FirstOrDefault();

            if (attribute == null) return null;

            var dimensions = (Schema2.DimensionType?)null;

            if (finfo.FieldType == typeof(Single)) dimensions = Schema2.DimensionType.SCALAR;
            if (finfo.FieldType == typeof(Vector2)) dimensions = Schema2.DimensionType.VEC2;
            if (finfo.FieldType == typeof(Vector3)) dimensions = Schema2.DimensionType.VEC3;
            if (finfo.FieldType == typeof(Vector4)) dimensions = Schema2.DimensionType.VEC4;
            if (finfo.FieldType == typeof(Quaternion)) dimensions = Schema2.DimensionType.VEC4;
            if (finfo.FieldType == typeof(Matrix4x4)) dimensions = Schema2.DimensionType.MAT4;

            if (dimensions == null) throw new ArgumentException($"invalid type {finfo.FieldType}");

            return new MemoryAccessInfo(attribute.Name, 0, 0, 0, dimensions.Value, attribute.Encoding, attribute.Normalized);
        }

        private static Func<IVertexBuilder, Object> _GetVertexBuilderAttributeFunc(string attributeName)
        {
            if (attributeName == "POSITION") return v => v.GetGeometry().GetPosition();
            if (attributeName == "NORMAL") return v => { return v.GetGeometry().TryGetNormal(out Vector3 n) ? n : Vector3.Zero; };
            if (attributeName == "TANGENT") return v => { return v.GetGeometry().TryGetTangent(out Vector4 t) ? t : Vector4.Zero; };

            if (attributeName == "POSITIONDELTA") return v => v.GetGeometry().GetPosition();
            if (attributeName == "NORMALDELTA") return v => { return v.GetGeometry().TryGetNormal(out Vector3 n) ? n : Vector3.Zero; };
            if (attributeName == "TANGENTDELTA") return v => { return v.GetGeometry().TryGetTangent(out Vector4 t) ? new Vector3(t.X, t.Y, t.Z) : Vector3.Zero; };

            if (attributeName == "COLOR_0") return v => { var m = v.GetMaterial(); return m.MaxColors <= 0 ? Vector4.One : m.GetColor(0); };
            if (attributeName == "COLOR_1") return v => { var m = v.GetMaterial(); return m.MaxColors <= 1 ? Vector4.One : m.GetColor(1); };
            if (attributeName == "COLOR_2") return v => { var m = v.GetMaterial(); return m.MaxColors <= 2 ? Vector4.One : m.GetColor(2); };
            if (attributeName == "COLOR_3") return v => { var m = v.GetMaterial(); return m.MaxColors <= 3 ? Vector4.One : m.GetColor(3); };

            if (attributeName == "TEXCOORD_0") return v => { var m = v.GetMaterial(); return m.MaxTextCoords <= 0 ? Vector2.Zero : m.GetTexCoord(0); };
            if (attributeName == "TEXCOORD_1") return v => { var m = v.GetMaterial(); return m.MaxTextCoords <= 1 ? Vector2.Zero : m.GetTexCoord(1); };
            if (attributeName == "TEXCOORD_2") return v => { var m = v.GetMaterial(); return m.MaxTextCoords <= 2 ? Vector2.Zero : m.GetTexCoord(2); };
            if (attributeName == "TEXCOORD_3") return v => { var m = v.GetMaterial(); return m.MaxTextCoords <= 3 ? Vector2.Zero : m.GetTexCoord(3); };

            if (attributeName == "JOINTS_0") return v => v.GetSkinning().JointsLow;
            if (attributeName == "JOINTS_1") return v => v.GetSkinning().JointsHigh;

            if (attributeName == "WEIGHTS_0") return v => v.GetSkinning().WeightsLow;
            if (attributeName == "WEIGHTS_1") return v => v.GetSkinning().WeightsHigh;

            return v => v.GetMaterial().GetCustomAttribute(attributeName);
        }

        private static TColumn[] _GetColumn<TVertex, TColumn>(this IReadOnlyList<TVertex> vertices, Func<IVertexBuilder, Object> func)
            where TVertex : IVertexBuilder
        {
            var dst = new TColumn[vertices.Count];

            for (int i = 0; i < dst.Length; ++i)
            {
                var v = vertices[i];

                dst[i] = (TColumn)func(v);
            }

            return dst;
        }

        #endregion
    }
}
