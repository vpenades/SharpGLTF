using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace SharpGLTF.Geometry.VertexTypes
{
    using Memory;

    static class VertexUtils
    {
        public static IEnumerable<MemoryAccessor[]> CreateVertexMemoryAccessors<TvP, TvM, TvJ>(this IEnumerable<IReadOnlyList<(TvP, TvM, TvJ)>> vertexBlocks)
            where TvP : struct, IVertexPosition
            where TvM : struct, IVertexMaterial
            where TvJ : struct, IVertexJoints
        {
            // total number of vertices
            var totalCount = vertexBlocks.Sum(item => item.Count);

            // vertex attributes
            var attributes = GetVertexAttributes(typeof(TvP), typeof(TvM), typeof(TvJ), totalCount);

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
                    var columnFunc = GetItemValueFunc<TvP, TvM, TvJ>(accessor.Attribute.Name);

                    if (accessor.Attribute.Dimensions == Schema2.DimensionType.SCALAR) accessor.Fill(block.GetScalarColumn(columnFunc));
                    if (accessor.Attribute.Dimensions == Schema2.DimensionType.VEC2) accessor.Fill(block.GetVector2Column(columnFunc));
                    if (accessor.Attribute.Dimensions == Schema2.DimensionType.VEC3) accessor.Fill(block.GetVector3Column(columnFunc));
                    if (accessor.Attribute.Dimensions == Schema2.DimensionType.VEC4) accessor.Fill(block.GetVector4Column(columnFunc));
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

        private static MemoryAccessInfo[] GetVertexAttributes(Type vertexType, Type valuesType, Type jointsType, int itemsCount)
        {
            var attributes = new List<MemoryAccessInfo>();

            foreach (var finfo in vertexType.GetFields())
            {
                var attribute = _GetMemoryAccessInfo(finfo);
                if (attribute.HasValue) attributes.Add(attribute.Value);
            }

            foreach (var finfo in valuesType.GetFields())
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

        private static Func<(TvP, TvM, TvJ), Object> GetItemValueFunc<TvP, TvM, TvJ>(string attributeName)
        {
            var finfo = GetVertexField(typeof(TvP), attributeName);
            if (finfo != null) return vertex => finfo.GetValue(vertex.Item1);

            finfo = GetVertexField(typeof(TvM), attributeName);
            if (finfo != null) return vertex => finfo.GetValue(vertex.Item2);

            finfo = GetVertexField(typeof(TvJ), attributeName);
            if (finfo != null) return vertex => finfo.GetValue(vertex.Item3);

            throw new NotImplementedException();
        }

        private static Single[] GetScalarColumn<TvP, TvM, TvJ>(this IReadOnlyList<(TvP, TvM, TvJ)> vertices, Func<(TvP, TvM, TvJ), Object> func)
        {
            return GetColumn<TvP, TvM, TvJ, Single>(vertices, func);
        }

        private static Vector2[] GetVector2Column<TvP, TvM, TvJ>(this IReadOnlyList<(TvP, TvM, TvJ)> vertices, Func<(TvP, TvM, TvJ), Object> func)
        {
            return GetColumn<TvP, TvM, TvJ, Vector2>(vertices, func);
        }

        private static Vector3[] GetVector3Column<TvP, TvM, TvJ>(this IReadOnlyList<(TvP, TvM, TvJ)> vertices, Func<(TvP, TvM, TvJ), Object> func)
        {
            return GetColumn<TvP, TvM, TvJ, Vector3>(vertices, func);
        }

        private static Vector4[] GetVector4Column<TvP, TvM, TvJ>(this IReadOnlyList<(TvP, TvM, TvJ)> vertices, Func<(TvP, TvM, TvJ), Object> func)
        {
            return GetColumn<TvP, TvM, TvJ, Vector4>(vertices, func);
        }

        private static TColumn[] GetColumn<TvP, TvM, TvJ, TColumn>(this IReadOnlyList<(TvP, TvM, TvJ)> vertices, Func<(TvP, TvM, TvJ), Object> func)
        {
            var dst = new TColumn[vertices.Count];

            for (int i = 0; i < dst.Length; ++i)
            {
                var v = vertices[i];

                dst[i] = (TColumn)func(v);
            }

            return dst;
        }
    }
}
