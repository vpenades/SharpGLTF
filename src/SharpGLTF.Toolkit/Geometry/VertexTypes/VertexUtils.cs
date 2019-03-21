using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;

namespace SharpGLTF.Geometry.VertexTypes
{
    using Memory;

    public static class VertexUtils
    {
        public static IEnumerable<MemoryAccessor[]> CreateVertexMemoryAccessors<TVertex, TJoints>(this IEnumerable<IReadOnlyList<(TVertex, TJoints)>> vertexBlocks)
            where TVertex : struct, IVertex
            where TJoints : struct, IJoints
        {
            // total number of vertices
            var totalCount = vertexBlocks.Sum(item => item.Count);

            // vertex attributes
            var attributes = GetVertexAttributes(typeof(TVertex), typeof(TJoints), totalCount);

            // create master vertex buffer
            int byteStride = attributes[0].ByteStride;
            var vbuffer = new ArraySegment<byte>( new Byte[byteStride * totalCount] );

            var baseVertexIndex = 0;

            foreach (var block in vertexBlocks)
            {
                var accessors = MemoryAccessInfo
                    .Slice(attributes, baseVertexIndex, block.Count)
                    .Select(item => new MemoryAccessor(vbuffer, item))
                    .ToArray();

                foreach (var accessor in accessors)
                {
                    bool isSkin = false;
                    var finfo = GetVertexField(typeof(TVertex), accessor.Attribute.Name);
                    if (finfo == null)
                    {
                        finfo = GetVertexField(typeof(TJoints), accessor.Attribute.Name);
                        isSkin = true;
                    }

                    if (accessor.Attribute.Dimensions == Schema2.DimensionType.SCALAR) accessor.Fill(GetScalarColumn(finfo, block, isSkin));
                    if (accessor.Attribute.Dimensions == Schema2.DimensionType.VEC2) accessor.Fill(GetVector2Column(finfo, block, isSkin));
                    if (accessor.Attribute.Dimensions == Schema2.DimensionType.VEC3) accessor.Fill(GetVector3Column(finfo, block, isSkin));
                    if (accessor.Attribute.Dimensions == Schema2.DimensionType.VEC4) accessor.Fill(GetVector4Column(finfo, block, isSkin));
                }

                yield return accessors;

                baseVertexIndex += block.Count;
            }
        }

        public static IEnumerable<MemoryAccessor> CreateIndexMemoryAccessors(this IEnumerable<IReadOnlyList<Int32>> indexBlocks)
        {
            // get attributes
            var totalCount = indexBlocks.Sum(item => item.Count);
            var attribute = new MemoryAccessInfo("INDEX", 0, totalCount, 0, Schema2.DimensionType.SCALAR, Schema2.EncodingType.UNSIGNED_INT);

            // create master index buffer
            var ibytes = new Byte[4 * totalCount];
            var ibuffer = new ArraySegment<byte>(ibytes);

            var baseIndicesIndex = 0;

            foreach (var block in indexBlocks)
            {
                var accessor = new MemoryAccessor(ibuffer, attribute.Slice(baseIndicesIndex, block.Count));

                accessor.Fill(block);

                yield return accessor;

                baseIndicesIndex += block.Count;
            }
        }

        private static System.Reflection.FieldInfo GetVertexField(Type vertexType, string attributeName)
        {
            foreach (var finfo in vertexType.GetFields())
            {
                var attribute = _GetMemoryAccessInfo(finfo);

                if (attribute.HasValue)
                {
                    if (attribute.Value.Name == attributeName) return finfo;
                }
            }

            return null;
        }

        private static MemoryAccessInfo[] GetVertexAttributes(Type vertexType, Type jointsType, int itemsCount)
        {
            var attributes = new List<MemoryAccessInfo>();

            foreach (var finfo in vertexType.GetFields())
            {
                var attribute = _GetMemoryAccessInfo(finfo);
                if (attribute.HasValue) attributes.Add(attribute.Value);
            }

            foreach (var finfo in jointsType.GetFields())
            {
                var attribute = _GetMemoryAccessInfo(finfo);
                if (attribute.HasValue) attributes.Add(attribute.Value);
            }

            var array = attributes.ToArray();

            MemoryAccessInfo.SetInterleavedInfo(array, 0, itemsCount);

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

        internal static Single[] GetScalarColumn<TVertex, TJoints>(this System.Reflection.FieldInfo finfo, IReadOnlyList<(TVertex, TJoints)> vertices, bool vertexOrSkin)
        {
            var dst = new Single[vertices.Count];

            for (int i = 0; i < dst.Length; ++i)
            {
                var v = vertices[i];

                dst[i] = vertexOrSkin ? (Single)finfo.GetValue(v.Item2) : (Single)finfo.GetValue(v.Item1);
            }

            return dst;
        }

        internal static Vector2[] GetVector2Column<TVertex, TJoints>(this System.Reflection.FieldInfo finfo, IReadOnlyList<(TVertex, TJoints)> vertices, bool vertexOrSkin)
        {
            var dst = new Vector2[vertices.Count];

            for (int i = 0; i < dst.Length; ++i)
            {
                var v = vertices[i];

                dst[i] = vertexOrSkin ? (Vector2)finfo.GetValue(v.Item2) : (Vector2)finfo.GetValue(v.Item1);
            }

            return dst;
        }

        internal static Vector3[] GetVector3Column<TVertex, TJoints>(this System.Reflection.FieldInfo finfo, IReadOnlyList<(TVertex, TJoints)> vertices, bool vertexOrSkin)
        {
            var dst = new Vector3[vertices.Count];

            for (int i = 0; i < dst.Length; ++i)
            {
                var v = vertices[i];

                dst[i] = vertexOrSkin ? (Vector3)finfo.GetValue(v.Item2) : (Vector3)finfo.GetValue(v.Item1);
            }

            return dst;
        }

        internal static Vector4[] GetVector4Column<TVertex, TJoints>(this System.Reflection.FieldInfo finfo, IReadOnlyList<(TVertex, TJoints)> vertices, bool vertexOrSkin)
        {
            var dst = new Vector4[vertices.Count];

            for (int i = 0; i < dst.Length; ++i)
            {
                var v = vertices[i];

                dst[i] = vertexOrSkin ? (Vector4)finfo.GetValue(v.Item2) : (Vector4)finfo.GetValue(v.Item1);
            }

            return dst;
        }
    }
}
