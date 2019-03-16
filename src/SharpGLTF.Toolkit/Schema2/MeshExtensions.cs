using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace SharpGLTF.Schema2
{
    using Memory;

    public static partial class Toolkit
    {
        public static Mesh CreateMesh<TVertex>(this ModelRoot root, string name, Geometry.InterleavedMeshBuilder<TVertex, Material> meshBuilder)
            where TVertex : struct
        {
            return root.CreateMesh<TVertex, Material>(name, meshBuilder, k => k);
        }

        public static Mesh CreateMesh<TVertex, TMaterial>(this ModelRoot root, string name, Geometry.InterleavedMeshBuilder<TVertex, TMaterial> meshBuilder, Func<TMaterial,Material> materialEvaluator)
            where TVertex : struct
        {
            var dstMesh = root.CreateMesh(name);

            // create vertex accessors
            var vertexAccessors = root.CreateInterleavedVertexAccessors(meshBuilder.Vertices);

            foreach (var mkey in meshBuilder.Materials)
            {
                var indices = meshBuilder.GetIndices(mkey);

                // create index buffer
                var ibytes = new Byte[4 * indices.Count];
                var ibuffer = root.UseBufferView(new ArraySegment<byte>(ibytes), 0, BufferMode.ELEMENT_ARRAY_BUFFER);

                var indicesAccessor = root
                    .CreateAccessor("Indices");

                indicesAccessor.SetIndexData(ibuffer, 0, indices);

                // create mesh primitive
                var prim = dstMesh.CreatePrimitive();
                foreach (var va in vertexAccessors) prim.SetVertexAccessor(va.Key, va.Value);
                prim.SetIndexAccessor(indicesAccessor);
                prim.DrawPrimitiveType = PrimitiveType.TRIANGLES;

                prim.Material = materialEvaluator(mkey);
            }

            return dstMesh;
        }

        public static MeshPrimitive WithVertexAccessors<TVertex>(this MeshPrimitive primitive, IReadOnlyList<TVertex> vertices)
        {
            var accessors = primitive.LogicalParent.LogicalParent.CreateInterleavedVertexAccessors(vertices);

            foreach (var va in accessors) primitive.SetVertexAccessor(va.Key, va.Value);

            return primitive;
        }

        public static MeshPrimitive WithVertexAccessor(this MeshPrimitive primitive, string attribute, IReadOnlyList<Vector2> values)
        {
            var root = primitive.LogicalParent.LogicalParent;

            // create a vertex buffer and fill it
            var view = root.UseBufferView(new Byte[8 * values.Count], 0, null, 0, BufferMode.ARRAY_BUFFER);
            var array = new Vector2Array(view.Content);
            array.FillFrom(0, values);

            var accessor = root.CreateAccessor();

            accessor.SetVertexData(view, 0, values.Count, DimensionType.VEC2, EncodingType.FLOAT, false);

            primitive.SetVertexAccessor(attribute, accessor);

            return primitive;
        }

        public static MeshPrimitive WithVertexAccessor(this MeshPrimitive primitive, string attribute, IReadOnlyList<Vector3> values)
        {
            var root = primitive.LogicalParent.LogicalParent;

            // create a vertex buffer and fill it
            var view = root.UseBufferView(new Byte[12 * values.Count], 0, null, 0, BufferMode.ARRAY_BUFFER);
            var array = new Vector3Array(view.Content);
            array.FillFrom(0, values);

            var accessor = root.CreateAccessor();

            accessor.SetVertexData(view, 0, values.Count, DimensionType.VEC3, EncodingType.FLOAT, false);

            primitive.SetVertexAccessor(attribute, accessor);

            return primitive;
        }

        public static MeshPrimitive WithVertexAccessor(this MeshPrimitive primitive, string attribute, IReadOnlyList<Vector4> values)
        {
            var root = primitive.LogicalParent.LogicalParent;

            // create a vertex buffer and fill it
            var view = root.UseBufferView(new Byte[16 * values.Count], 0, null, 0, BufferMode.ARRAY_BUFFER);
            var array = new Vector4Array(view.Content);
            array.FillFrom(0, values);

            var accessor = root.CreateAccessor();

            accessor.SetVertexData(view, 0, values.Count, DimensionType.VEC4, EncodingType.FLOAT, false);

            primitive.SetVertexAccessor(attribute, accessor);

            return primitive;
        }

        public static MeshPrimitive WithIndicesAutomatic(this MeshPrimitive primitive, PrimitiveType primitiveType)
        {
            var root = primitive.LogicalParent.LogicalParent;

            primitive.DrawPrimitiveType = primitiveType;
            primitive.SetIndexAccessor(null);

            return primitive;
        }

        public static MeshPrimitive WithIndicesAccessor(this MeshPrimitive primitive, PrimitiveType primitiveType, IReadOnlyList<Int32> values)
        {
            var root = primitive.LogicalParent.LogicalParent;

            // create an index buffer and fill it
            var view = root.UseBufferView(new Byte[4 * values.Count], 0, null, 0, BufferMode.ELEMENT_ARRAY_BUFFER);
            var array = new IntegerArray(view.Content);
            array.FillFrom(0, values);

            var accessor = root.CreateAccessor();

            accessor.SetIndexData(view, 0, values.Count, IndexEncodingType.UNSIGNED_INT);

            primitive.DrawPrimitiveType = primitiveType;
            primitive.SetIndexAccessor(accessor);

            return primitive;
        }

        public static MeshPrimitive WithMaterial(this MeshPrimitive primitive, Material material)
        {
            primitive.Material = material;
            return primitive;
        }
    }
}
