using System;
using System.Collections.Generic;
using System.Text;
using System.Numerics;

namespace SharpGLTF.Schema2
{
    using SharpGLTF.Geometry.VertexTypes;

    public static partial class Toolkit
    {
        public static IReadOnlyDictionary<string, Accessor> CreateStaticVertexAccessors<TVertex>(this ModelRoot root, IReadOnlyList<TVertex> vertices)
            where TVertex : struct
        {
            // get vertex attributes from TVertex type using reflection
            var attributes = VertexUtils.GetVertexAttributes(typeof(TVertex), vertices.Count);

            // create vertex buffer
            int byteStride = attributes[0].ByteStride;
            var vbytes = new Byte[byteStride * vertices.Count];
            var vbuffer = root.UseBufferView(new ArraySegment<byte>(vbytes), byteStride, BufferMode.ARRAY_BUFFER);

            return _CreateAccessors(root, vbuffer, attributes, 0, vertices);
        }

        internal static IReadOnlyDictionary<string, Accessor> _CreateAccessors<TVertex>(ModelRoot root, BufferView vbuffer, Geometry.MemoryAccessInfo[] attributes, int vertexStart, IReadOnlyList<TVertex> vertices)
        {
            attributes = Geometry.MemoryAccessInfo.Slice(attributes, vertexStart, vertices.Count);

            // create vertex accessors
            var vertexAccessors = new Dictionary<String, Accessor>();

            foreach (var attribute in attributes)
            {
                var field = VertexUtils.GetVertexField(typeof(TVertex), attribute.Name);

                vertexAccessors[attribute.Name] = _CreateAccessor(root, vbuffer, attribute, vertices, field);
            }

            return vertexAccessors;
        }

        public static IReadOnlyDictionary<string, Accessor> CreateSkinedVertexAccessors<TVertex, TSkin>(this ModelRoot root, IReadOnlyList<(TVertex, TSkin)> vertices)
        {
            // get vertex attributes from TVertex and TSkin types using reflection
            var attributes = VertexUtils.GetVertexAttributes(typeof(TVertex), typeof(TSkin), vertices.Count);

            // create vertex buffer
            int byteStride = attributes[0].ByteStride;
            var vbytes = new Byte[byteStride * vertices.Count];
            var vbuffer = root.UseBufferView(new ArraySegment<byte>(vbytes), byteStride, BufferMode.ARRAY_BUFFER);

            // create vertex accessors
            var vertexAccessors = new Dictionary<String, Accessor>();

            foreach (var attribute in attributes)
            {
                var vfield = VertexUtils.GetVertexField(typeof(TVertex), attribute.Name);
                var sfield = VertexUtils.GetVertexField(typeof(TSkin), attribute.Name);

                if (vfield != null)
                {
                    vertexAccessors[attribute.Name] = root._CreateAccessor(vbuffer, attribute, vertices, vfield, false);
                }
                else if (sfield != null)
                {
                    vertexAccessors[attribute.Name] = root._CreateAccessor(vbuffer, attribute, vertices, sfield, true);
                }
            }

            return vertexAccessors;
        }

        private static Accessor _CreateAccessor<TVertex>(this ModelRoot root, BufferView vbuffer, Geometry.MemoryAccessInfo attribute, IReadOnlyList<TVertex> vertices, System.Reflection.FieldInfo field)
        {
            var accessor = root.CreateAccessor(attribute.Name);
            if (field.FieldType == typeof(Vector2)) accessor.SetVertexData(vbuffer, attribute.ByteOffset, field.GetVector2Column(vertices), attribute.Encoding, attribute.Normalized);
            if (field.FieldType == typeof(Vector3)) accessor.SetVertexData(vbuffer, attribute.ByteOffset, field.GetVector3Column(vertices), attribute.Encoding, attribute.Normalized);
            if (field.FieldType == typeof(Vector4)) accessor.SetVertexData(vbuffer, attribute.ByteOffset, field.GetVector4Column(vertices), attribute.Encoding, attribute.Normalized);
            return accessor;
        }

        private static Accessor _CreateAccessor<TVertex, TSkin>(this ModelRoot root, BufferView vbuffer, Geometry.MemoryAccessInfo attribute, IReadOnlyList<(TVertex, TSkin)> vertices, System.Reflection.FieldInfo field, bool vertexOrSkin)
        {
            var accessor = root.CreateAccessor(attribute.Name);
            if (field.FieldType == typeof(Vector2)) accessor.SetVertexData(vbuffer, attribute.ByteOffset, field.GetVector2Column(vertices, vertexOrSkin), attribute.Encoding, attribute.Normalized);
            if (field.FieldType == typeof(Vector3)) accessor.SetVertexData(vbuffer, attribute.ByteOffset, field.GetVector3Column(vertices, vertexOrSkin), attribute.Encoding, attribute.Normalized);
            if (field.FieldType == typeof(Vector4)) accessor.SetVertexData(vbuffer, attribute.ByteOffset, field.GetVector4Column(vertices, vertexOrSkin), attribute.Encoding, attribute.Normalized);
            return accessor;
        }
    }
}
