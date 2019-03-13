using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;

namespace SharpGLTF.Geometry.VertexTypes
{
    public static class VertexUtils
    {
        public static System.Reflection.FieldInfo GetVertexField(Type type, string attributeName)
        {
            foreach (var finfo in type.GetFields())
            {
                var attribute = _GetAccessor(finfo);

                if (attribute.HasValue)
                {
                    if (attribute.Value.Name == attributeName) return finfo;
                }
            }

            return null;
        }

        public static MemoryAccessInfo[] GetVertexAttributes(Type type, int itemsCount)
        {
            var attributes = new List<MemoryAccessInfo>();

            foreach (var finfo in type.GetFields())
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

        public static Single[] GetScalarColumn<TVertex>(this System.Reflection.FieldInfo finfo, IReadOnlyList<TVertex> vertices)
        {
            var dst = new Single[vertices.Count];

            for (int i = 0; i < dst.Length; ++i)
            {
                dst[i] = (Single)finfo.GetValue(vertices[i]);
            }

            return dst;
        }

        public static Vector2[] GetVector2Column<TVertex>(this System.Reflection.FieldInfo finfo, IReadOnlyList<TVertex> vertices)
        {
            var dst = new Vector2[vertices.Count];

            for (int i = 0; i < dst.Length; ++i)
            {
                dst[i] = (Vector2)finfo.GetValue(vertices[i]);
            }

            return dst;
        }

        public static Vector3[] GetVector3Column<TVertex>(this System.Reflection.FieldInfo finfo, IReadOnlyList<TVertex> vertices)
        {
            var dst = new Vector3[vertices.Count];

            for (int i = 0; i < dst.Length; ++i)
            {
                dst[i] = (Vector3)finfo.GetValue(vertices[i]);
            }

            return dst;
        }

        public static Vector4[] GetVector4Column<TVertex>(this System.Reflection.FieldInfo finfo, IReadOnlyList<TVertex> vertices)
        {
            var dst = new Vector4[vertices.Count];

            for (int i = 0; i < dst.Length; ++i)
            {
                dst[i] = (Vector4)finfo.GetValue(vertices[i]);
            }

            return dst;
        }
    }
}
