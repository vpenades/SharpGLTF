using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

using SharpGLTF.Memory;

using DIMENSIONS = SharpGLTF.Schema2.DimensionType;
using ENCODING = SharpGLTF.Schema2.EncodingType;

namespace SharpGLTF.Geometry.VertexTypes
{
    static partial class VertexUtils
    {
        public static MemoryAccessor CreateVertexMemoryAccessor<TVertex>(this IReadOnlyList<TVertex> vertices, string attributeName, PackedEncoding vertexEncoding)
            where TVertex : IVertexBuilder
        {
            if (vertices == null || vertices.Count == 0) return null;

            vertexEncoding.AdjustJointEncoding(vertices);

            // determine the vertex attributes from the first vertex.
            var attributes = GetVertexAttributes(vertices[0], vertices.Count, vertexEncoding);

            var attribute = attributes.FirstOrDefault(item => item.Name == attributeName);
            if (attribute.Name == null) return null;
            attribute.ByteOffset = 0;
            attribute.ByteStride = 0;

            // create a buffer
            var vbuffer = new ArraySegment<byte>(new Byte[attribute.StepByteLength * vertices.Count]);

            // fill the buffer with the vertex attributes.
            var accessor = new MemoryAccessor(vbuffer, attribute);

            accessor.FillAccessor(vertices);

            return accessor;
        }

        public static MemoryAccessor[] CreateVertexMemoryAccessors<TVertex>(this IReadOnlyList<TVertex> vertices, PackedEncoding vertexEncoding)
            where TVertex : IVertexBuilder
        {
            if (vertices == null || vertices.Count == 0) return null;

            vertexEncoding.AdjustJointEncoding(vertices);

            // determine the vertex attributes from the first vertex.
            var attributes = GetVertexAttributes(vertices[0], vertices.Count, vertexEncoding);

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

            MemoryAccessor.SanitizeVertexAttributes(accessors);

            return accessors;
        }

        private static void FillAccessor<TVertex>(this MemoryAccessor dstAccessor, IReadOnlyList<TVertex> srcVertices)
            where TVertex : IVertexBuilder
        {
            var columnFunc = _GetVertexBuilderAttributeFunc(dstAccessor.Attribute.Name);

            if (dstAccessor.Attribute.Dimensions == DIMENSIONS.SCALAR) dstAccessor.AsScalarArray().Fill(srcVertices._GetColumn<TVertex, Single>(columnFunc));
            if (dstAccessor.Attribute.Dimensions == DIMENSIONS.VEC2) dstAccessor.AsVector2Array().Fill(srcVertices._GetColumn<TVertex, Vector2>(columnFunc));
            if (dstAccessor.Attribute.Dimensions == DIMENSIONS.VEC3) dstAccessor.AsVector3Array().Fill(srcVertices._GetColumn<TVertex, Vector3>(columnFunc));
            if (dstAccessor.Attribute.Dimensions == DIMENSIONS.VEC4) dstAccessor.AsVector4Array().Fill(srcVertices._GetColumn<TVertex, Vector4>(columnFunc));
        }

        public static MemoryAccessor CreateIndexMemoryAccessor(this IReadOnlyList<Int32> indices, ENCODING indexEncoding)
        {
            if (indices == null || indices.Count == 0) return null;

            var attribute = new MemoryAccessInfo("INDEX", 0, indices.Count, 0, DIMENSIONS.SCALAR, indexEncoding);

            // create buffer
            var ibytes = new Byte[indexEncoding.ByteLength() * indices.Count];
            var ibuffer = new ArraySegment<byte>(ibytes);

            // fill the buffer with indices.
            var accessor = new MemoryAccessor(ibuffer, attribute.Slice(0, indices.Count));

            accessor.AsIntegerArray().Fill(indices);

            return accessor;
        }

        public static MemoryAccessInfo[] GetVertexAttributes(this IVertexBuilder firstVertex, int vertexCount, PackedEncoding vertexEncoding)
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
                if (attribute.HasValue)
                {
                    var a = attribute.Value;
                    if (a.Name.StartsWith("COLOR_", StringComparison.OrdinalIgnoreCase))
                    {
                        if (vertexEncoding.ColorEncoding.HasValue)
                        {
                            a.Encoding = vertexEncoding.ColorEncoding.Value;
                            a.Normalized = a.Encoding != ENCODING.FLOAT;
                        }
                    }

                    attributes.Add(a);
                }
            }

            foreach (var finfo in tvs.GetFields())
            {
                var attribute = _GetMemoryAccessInfo(finfo);
                if (attribute.HasValue)
                {
                    var a = attribute.Value;
                    if (a.Name.StartsWith("JOINTS_", StringComparison.OrdinalIgnoreCase)) a.Encoding = vertexEncoding.JointsEncoding.Value;
                    if (a.Name.StartsWith("WEIGHTS_", StringComparison.OrdinalIgnoreCase))
                    {
                        a.Encoding = vertexEncoding.WeightsEncoding.Value;
                        if (a.Encoding != ENCODING.FLOAT) a.Normalized = true;
                    }

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

            var dimensions = (DIMENSIONS?)null;

            if (finfo.FieldType == typeof(Single)) dimensions = DIMENSIONS.SCALAR;
            if (finfo.FieldType == typeof(Vector2)) dimensions = DIMENSIONS.VEC2;
            if (finfo.FieldType == typeof(Vector3)) dimensions = DIMENSIONS.VEC3;
            if (finfo.FieldType == typeof(Vector4)) dimensions = DIMENSIONS.VEC4;
            if (finfo.FieldType == typeof(Quaternion)) dimensions = DIMENSIONS.VEC4;
            if (finfo.FieldType == typeof(Matrix4x4)) dimensions = DIMENSIONS.MAT4;

            if (dimensions == null) throw new ArgumentException($"invalid type {finfo.FieldType}");

            return new MemoryAccessInfo(attribute.Name, 0, 0, 0, dimensions.Value, attribute.Encoding, attribute.Normalized);
        }

        private static Converter<IVertexBuilder, Object> _GetVertexBuilderAttributeFunc(string attributeName)
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

            if (attributeName == "COLOR_0DELTA") return v => { var m = v.GetMaterial(); return m.MaxColors <= 0 ? Vector4.Zero : m.GetColor(0); };
            if (attributeName == "COLOR_1DELTA") return v => { var m = v.GetMaterial(); return m.MaxColors <= 1 ? Vector4.Zero : m.GetColor(1); };

            if (attributeName == "TEXCOORD_0") return v => { var m = v.GetMaterial(); return m.MaxTextCoords <= 0 ? Vector2.Zero : m.GetTexCoord(0); };
            if (attributeName == "TEXCOORD_1") return v => { var m = v.GetMaterial(); return m.MaxTextCoords <= 1 ? Vector2.Zero : m.GetTexCoord(1); };
            if (attributeName == "TEXCOORD_2") return v => { var m = v.GetMaterial(); return m.MaxTextCoords <= 2 ? Vector2.Zero : m.GetTexCoord(2); };
            if (attributeName == "TEXCOORD_3") return v => { var m = v.GetMaterial(); return m.MaxTextCoords <= 3 ? Vector2.Zero : m.GetTexCoord(3); };

            if (attributeName == "TEXCOORD_0DELTA") return v => { var m = v.GetMaterial(); return m.MaxTextCoords <= 0 ? Vector2.Zero : m.GetTexCoord(0); };
            if (attributeName == "TEXCOORD_1DELTA") return v => { var m = v.GetMaterial(); return m.MaxTextCoords <= 1 ? Vector2.Zero : m.GetTexCoord(1); };

            if (attributeName == "JOINTS_0") return v => v.GetSkinning().JointsLow;
            if (attributeName == "JOINTS_1") return v => v.GetSkinning().JointsHigh;

            if (attributeName == "WEIGHTS_0") return v => v.GetSkinning().WeightsLow;
            if (attributeName == "WEIGHTS_1") return v => v.GetSkinning().WeightsHigh;

            return v => _GetVertexBuilderCustomAttributeFunc(v.GetMaterial(), attributeName);
        }

        private static object _GetVertexBuilderCustomAttributeFunc(IVertexMaterial vertex, string attributeName)
        {
            if (!(vertex is IVertexCustom customVertex)) return null;
            return customVertex.TryGetCustomAttribute(attributeName, out Object value) ? value : null;
        }

        private static TColumn[] _GetColumn<TVertex, TColumn>(this IReadOnlyList<TVertex> vertices, Converter<IVertexBuilder, Object> func)
            where TVertex : IVertexBuilder
        {
            var dst = new TColumn[vertices.Count];

            for (int i = 0; i < dst.Length; ++i)
            {
                var v = func(vertices[i]);

                dst[i] = v == null ? default : (TColumn)v;
            }

            return dst;
        }
    }
}
