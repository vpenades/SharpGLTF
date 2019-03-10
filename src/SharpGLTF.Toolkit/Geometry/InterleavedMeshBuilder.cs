using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace SharpGLTF.Geometry
{
    using Collections;
    using Schema2;

    public class InterleavedMeshBuilder<TVertex, TMaterial>
        where TVertex : struct
    {
        #region data

        private readonly VertexColumn<TVertex> _Vertices = new VertexColumn<TVertex>();
        private readonly Dictionary<TMaterial, List<int>> _Indices = new Dictionary<TMaterial, List<int>>();

        #endregion

        #region API

        public void AddPolygon(TMaterial material, params TVertex[] points)
        {
            for (int i = 2; i < points.Length; ++i)
            {
                AddTriangle(material, points[0], points[i - 1], points[i]);
            }
        }

        public void AddTriangle(TMaterial material, TVertex a, TVertex b, TVertex c)
        {
            var aa = _Vertices.Use(a);
            var bb = _Vertices.Use(b);
            var cc = _Vertices.Use(c);

            // check for degenerated triangles:
            if (aa == bb) return;
            if (aa == cc) return;
            if (bb == cc) return;

            if (!_Indices.TryGetValue(material, out List<int> indices))
            {
                indices = new List<int>();
                _Indices[material] = indices;
            }

            indices.Add(aa);
            indices.Add(bb);
            indices.Add(cc);
        }

        public void CopyToNode(Node dstNode, Func<TMaterial, Material> materialEvaluator)
        {
            dstNode.Mesh = dstNode.LogicalParent.CreateMesh();
            CopyToMesh(dstNode.Mesh, materialEvaluator);
        }

        public void CopyToMesh(Schema2.Mesh dstMesh, Func<TMaterial, Material> materialEvaluator)
        {
            var root = dstMesh.LogicalParent;

            // get vertex attributes from TVertex type using reflection
            var attributes = _GetVertexAttributes(_Vertices.Count);

            // create vertex buffer
            int byteStride = attributes[0].ByteStride;
            var vbytes = new Byte[byteStride * _Vertices.Count];

            var vbuffer = root.UseBufferView(new ArraySegment<byte>(vbytes), byteStride, BufferMode.ARRAY_BUFFER);

            // create vertex accessors
            var vertexAccessors = new Dictionary<String, Accessor>();

            foreach (var attribute in attributes)
            {
                var accessor = root.CreateAccessor(attribute.Name);

                var attributeType = _GetVertexAttributeType(attribute.Name);

                if (attributeType == typeof(Vector2)) accessor.WithVertexData(vbuffer, attribute.ByteOffset, _GetVector2Column(_Vertices, attribute.Name), attribute.Encoding, attribute.Normalized);
                if (attributeType == typeof(Vector3)) accessor.WithVertexData(vbuffer, attribute.ByteOffset, _GetVector3Column(_Vertices, attribute.Name), attribute.Encoding, attribute.Normalized);
                if (attributeType == typeof(Vector4)) accessor.WithVertexData(vbuffer, attribute.ByteOffset, _GetVector4Column(_Vertices, attribute.Name), attribute.Encoding, attribute.Normalized);

                vertexAccessors[attribute.Name.ToUpper()] = accessor;
            }

            foreach (var kvp in _Indices)
            {
                // create index buffer
                var ibytes = new Byte[4 * kvp.Value.Count];
                var ibuffer = root.UseBufferView(new ArraySegment<byte>(ibytes), 0, BufferMode.ELEMENT_ARRAY_BUFFER);

                var indices = root
                    .CreateAccessor("Indices")
                    .WithIndexData(ibuffer, 0, kvp.Value);

                // create mesh primitive
                var prim = dstMesh.CreatePrimitive();
                foreach (var va in vertexAccessors) prim.SetVertexAccessor(va.Key, va.Value);
                prim.SetIndexAccessor(indices);
                prim.DrawPrimitiveType = PrimitiveType.TRIANGLES;

                prim.Material = materialEvaluator(kvp.Key);
            }

            root.MergeImages();
            root.MergeBuffers();
        }

        #endregion

        #region core

        private Geometry.MemoryAccessInfo[] _GetVertexAttributes(int itemsCount)
        {
            var type = typeof(TVertex);

            var attributes = new List<Geometry.MemoryAccessInfo>();

            foreach (var field in type.GetFields())
            {
                var attributeName = field.Name;
                var attributeInfo = Geometry.MemoryAccessInfo.CreateDefaultElement(attributeName.ToUpper());
                attributes.Add(attributeInfo);
            }

            var array = attributes.ToArray();

            Geometry.MemoryAccessInfo.SetInterleavedInfo(array, 0, itemsCount);

            return array;
        }

        private static System.Reflection.FieldInfo _GetVertexField(string fieldName)
        {
            foreach (var field in typeof(TVertex).GetFields())
            {
                if (field.Name.ToLower() == fieldName.ToLower()) return field;
            }

            return null;
        }

        private static Type _GetVertexAttributeType(string fieldName)
        {
            return _GetVertexField(fieldName).FieldType;
        }

        private static Vector2[] _GetVector2Column(IReadOnlyList<TVertex> vertices, string fieldName)
        {
            var dst = new Vector2[vertices.Count];

            var finfo = _GetVertexField(fieldName);

            for (int i = 0; i < dst.Length; ++i)
            {
                dst[i] = (Vector2)finfo.GetValue(vertices[i]);
            }

            return dst;
        }

        private static Vector3[] _GetVector3Column(IReadOnlyList<TVertex> vertices, string fieldName)
        {
            var dst = new Vector3[vertices.Count];

            var finfo = _GetVertexField(fieldName);

            for (int i = 0; i < dst.Length; ++i)
            {
                dst[i] = (Vector3)finfo.GetValue(vertices[i]);
            }

            return dst;
        }

        private static Vector4[] _GetVector4Column(IReadOnlyList<TVertex> vertices, string fieldName)
        {
            var dst = new Vector4[vertices.Count];

            var finfo = _GetVertexField(fieldName);

            for (int i = 0; i < dst.Length; ++i)
            {
                dst[i] = (Vector4)finfo.GetValue(vertices[i]);
            }

            return dst;
        }

        #endregion
    }
}
