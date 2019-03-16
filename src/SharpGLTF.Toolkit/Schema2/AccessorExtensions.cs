using System;
using System.Collections.Generic;
using System.Text;
using System.Numerics;

namespace SharpGLTF.Schema2
{
    using SharpGLTF.Geometry.VertexTypes;

    public static partial class Toolkit
    {
        public static IReadOnlyDictionary<string, Accessor> CreateInterleavedVertexAccessors<TVertex>(this ModelRoot root, IReadOnlyList<TVertex> vertices)
        {
            // get vertex attributes from TVertex type using reflection
            var attributes = VertexUtils.GetVertexAttributes(typeof(TVertex), vertices.Count);

            // create vertex buffer
            int byteStride = attributes[0].ByteStride;
            var vbytes = new Byte[byteStride * vertices.Count];

            var vbuffer = root.UseBufferView(new ArraySegment<byte>(vbytes), byteStride, BufferMode.ARRAY_BUFFER);

            // create vertex accessors
            var vertexAccessors = new Dictionary<String, Accessor>();

            foreach (var attribute in attributes)
            {
                var accessor = root.CreateAccessor(attribute.Name);

                var field = VertexUtils.GetVertexField(typeof(TVertex), attribute.Name);

                if (field.FieldType == typeof(Vector2)) accessor.SetVertexData(vbuffer, attribute.ByteOffset, field.GetVector2Column(vertices), attribute.Encoding, attribute.Normalized);
                if (field.FieldType == typeof(Vector3)) accessor.SetVertexData(vbuffer, attribute.ByteOffset, field.GetVector3Column(vertices), attribute.Encoding, attribute.Normalized);
                if (field.FieldType == typeof(Vector4)) accessor.SetVertexData(vbuffer, attribute.ByteOffset, field.GetVector4Column(vertices), attribute.Encoding, attribute.Normalized);

                vertexAccessors[attribute.Name] = accessor;
            }

            return vertexAccessors;
        }
    }
}
