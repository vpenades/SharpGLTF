using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;

namespace SharpGLTF.Geometry.VertexTypes
{
    public static class VertexUtils
    {
        public static System.Reflection.FieldInfo GetVertexField(Type vertexType, string attributeName)
        {
            foreach (var finfo in vertexType.GetFields())
            {
                var attribute = _GetAccessor(finfo);

                if (attribute.HasValue)
                {
                    if (attribute.Value.Name == attributeName) return finfo;
                }
            }

            return null;
        }

        public static MemoryAccessInfo[] GetVertexAttributes(Type vertexType, int itemsCount)
        {
            var attributes = new List<MemoryAccessInfo>();

            foreach (var finfo in vertexType.GetFields())
            {
                var attribute = _GetAccessor(finfo);

                if (attribute.HasValue)
                {
                    attributes.Add(attribute.Value);
                }
            }

            var array = attributes.ToArray();

            MemoryAccessInfo.SetInterleavedInfo(array, 0, itemsCount);

            return array;
        }

        public static MemoryAccessInfo[] GetVertexAttributes(Type vertexType, Type skinType, int itemsCount)
        {
            var attributes = new List<MemoryAccessInfo>();

            foreach (var finfo in vertexType.GetFields())
            {
                var attribute = _GetAccessor(finfo);
                if (attribute.HasValue) attributes.Add(attribute.Value);
            }

            foreach (var finfo in skinType.GetFields())
            {
                var attribute = _GetAccessor(finfo);
                if (attribute.HasValue) attributes.Add(attribute.Value);
            }

            var array = attributes.ToArray();

            MemoryAccessInfo.SetInterleavedInfo(array, 0, itemsCount);

            return array;
        }

        public static IEnumerable<MemoryAccessor[]> CreateVertexMemoryAccessors<TVertex>(this IEnumerable<IReadOnlyList<TVertex>> vertexBlocks)
            where TVertex : struct
        {
            // get attributes
            var totalCount = vertexBlocks.Sum(item => item.Count);
            var attributes = GetVertexAttributes(typeof(TVertex), totalCount);

            // create master vertex buffer
            int byteStride = attributes[0].ByteStride;
            var vbytes = new Byte[byteStride * totalCount];
            var vbuffer = new ArraySegment<byte>(vbytes);

            var baseVertexIndex = 0;

            foreach (var block in vertexBlocks)
            {
                yield return MemoryAccessInfo
                    .Slice(attributes, baseVertexIndex, block.Count)
                    .Select(item => new MemoryAccessor(vbuffer, item))
                    .ToArray();

                baseVertexIndex += block.Count;
            }
        }

        public static IEnumerable<MemoryAccessor[]> CreateIndexMemoryAccessors(this IEnumerable<IReadOnlyList<Int32>> indexBlocks)
        {
            // get attributes
            var totalCount = indexBlocks.Sum(item => item.Count);

            // create master index buffer
            var vbytes = new Byte[4 * totalCount];
            var vbuffer = new ArraySegment<byte>(vbytes);

            var baseIndicesIndex = 0;

            throw new NotImplementedException();

            /*
            foreach (var block in indexBlocks)
            {
                yield return MemoryAccessInfo
                    .Slice(attributes, baseVertexIndex, block.Count)
                    .Select(item => new MemoryAccessor(vbuffer, item))
                    .ToArray();

                baseIndicesIndex += block.Count;
            }*/
        }

        private static MemoryAccessInfo? _GetAccessor(System.Reflection.FieldInfo finfo)
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

        internal static Single[] GetScalarColumn<TVertex>(this System.Reflection.FieldInfo finfo, IReadOnlyList<TVertex> vertices)
        {
            var dst = new Single[vertices.Count];

            for (int i = 0; i < dst.Length; ++i)
            {
                dst[i] = (Single)finfo.GetValue(vertices[i]);
            }

            return dst;
        }

        internal static Vector2[] GetVector2Column<TVertex>(this System.Reflection.FieldInfo finfo, IReadOnlyList<TVertex> vertices)
        {
            var dst = new Vector2[vertices.Count];

            for (int i = 0; i < dst.Length; ++i)
            {
                dst[i] = (Vector2)finfo.GetValue(vertices[i]);
            }

            return dst;
        }

        internal static Vector3[] GetVector3Column<TVertex>(this System.Reflection.FieldInfo finfo, IReadOnlyList<TVertex> vertices)
        {
            var dst = new Vector3[vertices.Count];

            for (int i = 0; i < dst.Length; ++i)
            {
                dst[i] = (Vector3)finfo.GetValue(vertices[i]);
            }

            return dst;
        }

        internal static Vector4[] GetVector4Column<TVertex>(this System.Reflection.FieldInfo finfo, IReadOnlyList<TVertex> vertices)
        {
            var dst = new Vector4[vertices.Count];

            for (int i = 0; i < dst.Length; ++i)
            {
                dst[i] = (Vector4)finfo.GetValue(vertices[i]);
            }

            return dst;
        }

        internal static Single[] GetScalarColumn<TVertex, TSkin>(this System.Reflection.FieldInfo finfo, IReadOnlyList<(TVertex, TSkin)> vertices, bool vertexOrSkin)
        {
            var dst = new Single[vertices.Count];

            for (int i = 0; i < dst.Length; ++i)
            {
                var v = vertices[i];

                dst[i] = vertexOrSkin ? (Single)finfo.GetValue(v.Item2) : (Single)finfo.GetValue(v.Item1);
            }

            return dst;
        }

        internal static Vector2[] GetVector2Column<TVertex, TSkin>(this System.Reflection.FieldInfo finfo, IReadOnlyList<(TVertex, TSkin)> vertices, bool vertexOrSkin)
        {
            var dst = new Vector2[vertices.Count];

            for (int i = 0; i < dst.Length; ++i)
            {
                var v = vertices[i];

                dst[i] = vertexOrSkin ? (Vector2)finfo.GetValue(v.Item2) : (Vector2)finfo.GetValue(v.Item1);
            }

            return dst;
        }

        internal static Vector3[] GetVector3Column<TVertex, TSkin>(this System.Reflection.FieldInfo finfo, IReadOnlyList<(TVertex, TSkin)> vertices, bool vertexOrSkin)
        {
            var dst = new Vector3[vertices.Count];

            for (int i = 0; i < dst.Length; ++i)
            {
                var v = vertices[i];

                dst[i] = vertexOrSkin ? (Vector3)finfo.GetValue(v.Item2) : (Vector3)finfo.GetValue(v.Item1);
            }

            return dst;
        }

        internal static Vector4[] GetVector4Column<TVertex, TSkin>(this System.Reflection.FieldInfo finfo, IReadOnlyList<(TVertex, TSkin)> vertices, bool vertexOrSkin)
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
