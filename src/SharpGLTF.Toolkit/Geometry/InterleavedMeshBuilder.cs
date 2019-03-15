using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace SharpGLTF.Geometry
{
    using Collections;
    using Schema2;
    using VertexTypes;

    public class InterleavedMeshBuilder<TVertex, TMaterial>
        where TVertex : struct
    {
        #region data

        private readonly VertexColumn<TVertex> _Vertices = new VertexColumn<TVertex>();
        private readonly Dictionary<TMaterial, List<int>> _Indices = new Dictionary<TMaterial, List<int>>();

        #endregion

        #region properties

        public IReadOnlyList<TVertex> Vertices => _Vertices;

        public IReadOnlyCollection<TMaterial> Materials => _Indices.Keys;

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

        public IEnumerable<(int, int, int)> GetTriangles(TMaterial material)
        {
            if (!_Indices.TryGetValue(material, out List<int> indices)) yield break;

            for (int i = 2; i < indices.Count; i += 3)
            {
                yield return (indices[i - 2], indices[i - 1], indices[i]);
            }
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
            var attributes = VertexUtils.GetVertexAttributes(typeof(TVertex), _Vertices.Count);

            // create vertex buffer
            int byteStride = attributes[0].ByteStride;
            var vbytes = new Byte[byteStride * _Vertices.Count];

            var vbuffer = root.UseBufferView(new ArraySegment<byte>(vbytes), byteStride, BufferMode.ARRAY_BUFFER);

            // create vertex accessors
            var vertexAccessors = new Dictionary<String, Accessor>();

            foreach (var attribute in attributes)
            {
                var accessor = root.CreateAccessor(attribute.Name);

                var field = VertexUtils.GetVertexField(typeof(TVertex), attribute.Name);

                if (field.FieldType == typeof(Vector2)) accessor.SetVertexData(vbuffer, attribute.ByteOffset, field.GetVector2Column(_Vertices), attribute.Encoding, attribute.Normalized);
                if (field.FieldType == typeof(Vector3)) accessor.SetVertexData(vbuffer, attribute.ByteOffset, field.GetVector3Column(_Vertices), attribute.Encoding, attribute.Normalized);
                if (field.FieldType == typeof(Vector4)) accessor.SetVertexData(vbuffer, attribute.ByteOffset, field.GetVector4Column(_Vertices), attribute.Encoding, attribute.Normalized);

                vertexAccessors[attribute.Name] = accessor;
            }

            foreach (var kvp in _Indices)
            {
                // create index buffer
                var ibytes = new Byte[4 * kvp.Value.Count];
                var ibuffer = root.UseBufferView(new ArraySegment<byte>(ibytes), 0, BufferMode.ELEMENT_ARRAY_BUFFER);

                var indices = root
                    .CreateAccessor("Indices");

                indices.SetIndexData(ibuffer, 0, kvp.Value);

                // create mesh primitive
                var prim = dstMesh.CreatePrimitive();
                foreach (var va in vertexAccessors) prim.SetVertexAccessor(va.Key, va.Value);
                prim.SetIndexAccessor(indices);
                prim.DrawPrimitiveType = PrimitiveType.TRIANGLES;

                prim.Material = materialEvaluator(kvp.Key);
            }
        }

        #endregion
    }
}
