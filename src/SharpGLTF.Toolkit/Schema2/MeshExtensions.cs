using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;
using System.Linq;

namespace SharpGLTF.Schema2
{
    using Memory;

    public static partial class Toolkit
    {
        #region meshes

        public static Mesh CreateMesh<TVertex, TSkin>(this ModelRoot root, Geometry.SkinnedMeshBuilder<Material, TVertex, TSkin> meshBuilder)
            where TVertex : struct, Geometry.VertexTypes.IVertex
            where TSkin : struct, Geometry.VertexTypes.IVertexJoints
        {
            return root.CreateMeshes(m => m, meshBuilder).First();
        }

        public static Mesh CreateMesh<TMaterial, TVertex, TSkin>(this ModelRoot root, Func<TMaterial, Material> materialEvaluator, Geometry.SkinnedMeshBuilder<TMaterial, TVertex, TSkin> meshBuilder)
            where TVertex : struct, Geometry.VertexTypes.IVertex
            where TSkin : struct, Geometry.VertexTypes.IVertexJoints
        {
            return root.CreateMeshes(materialEvaluator, meshBuilder).First();
        }

        public static Mesh[] CreateMeshes<TMaterial, TVertex, TSkin>(this ModelRoot root, Func<TMaterial, Material> materialEvaluator, params Geometry.SkinnedMeshBuilder<TMaterial, TVertex, TSkin>[] meshBuilders)
            where TVertex : struct, Geometry.VertexTypes.IVertex
            where TSkin : struct, Geometry.VertexTypes.IVertexJoints
        {
            // create a new schema material for every unique material in the mesh builders.
            var mapMaterials = meshBuilders
                .SelectMany(item => item.Primitives)
                .Select(item => item.Material)
                .Distinct()
                .ToDictionary(k => k, k => materialEvaluator(k));

            // creates meshes and primitives using MemoryAccessors using a single, shared vertex and index buffer
            var srcMeshes = Geometry.SkinnedMeshBuilder<TMaterial, TVertex, TSkin>
                .MergeBuffers(meshBuilders)
                .ToList();

            var dstMeshes = meshBuilders
                .Select(item => root.CreateMesh(item.Name))
                .ToArray();

            for (int i = 0; i < dstMeshes.Length; ++i)
            {
                var dstMesh = dstMeshes[i];
                var srcMesh = srcMeshes[i];

                foreach (var srcPrim in srcMesh)
                {
                    dstMesh.CreatePrimitive()
                        .WithMaterial( mapMaterials[srcPrim.Item1] )
                        .WithVertexAccessors(srcPrim.Item2)
                        .WithIndicesAccessor(PrimitiveType.TRIANGLES, srcPrim.Item3);
                }
            }

            return dstMeshes;
        }

        #endregion

        #region accessors

        public static MeshPrimitive WithVertexAccessor(this MeshPrimitive primitive, string attribute, IReadOnlyList<Single> values)
        {
            var root = primitive.LogicalParent.LogicalParent;

            // create a vertex buffer and fill it
            var view = root.UseBufferView(new Byte[4 * values.Count], 0, null, 0, BufferMode.ARRAY_BUFFER);
            var array = new ScalarArray(view.Content);
            array.FillFrom(0, values);

            var accessor = root.CreateAccessor();
            primitive.SetVertexAccessor(attribute, accessor);

            accessor.SetVertexData(view, 0, values.Count, DimensionType.SCALAR, EncodingType.FLOAT, false);

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
            primitive.SetVertexAccessor(attribute, accessor);

            accessor.SetVertexData(view, 0, values.Count, DimensionType.VEC2, EncodingType.FLOAT, false);

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

        public static MeshPrimitive WithVertexAccessors<TVertex>(this MeshPrimitive primitive, IReadOnlyList<TVertex> vertices)
            where TVertex : struct, Geometry.VertexTypes.IVertex
        {
            var xvertices = vertices.Select(item => (item, default(Geometry.VertexTypes.VertexJoints0))).ToList();

            return primitive.WithVertexAccessors(xvertices);
        }

        public static MeshPrimitive WithVertexAccessors<TVertex, TSkin>(this MeshPrimitive primitive, IReadOnlyList<(TVertex, TSkin)> vertices)
            where TVertex : struct, Geometry.VertexTypes.IVertex
            where TSkin : struct, Geometry.VertexTypes.IVertexJoints
        {
            var memAccessors = Geometry.VertexTypes.VertexUtils.CreateVertexMemoryAccessors(new[] { vertices }).First();

            return primitive.WithVertexAccessors(memAccessors);
        }

        public static MeshPrimitive WithVertexAccessors(this MeshPrimitive primitive, IEnumerable<Geometry.MemoryAccessor> memAccessors)
        {
            foreach (var va in memAccessors) primitive.WithVertexAccessor(va);

            return primitive;
        }

        public static MeshPrimitive WithVertexAccessor(this MeshPrimitive primitive, Geometry.MemoryAccessor memAccessor)
        {
            var root = primitive.LogicalParent.LogicalParent;

            primitive.SetVertexAccessor(memAccessor.Attribute.Name, root.CreateVertexAccessor(memAccessor));

            return primitive;
        }

        public static MeshPrimitive WithIndicesAccessor(this MeshPrimitive primitive, PrimitiveType primitiveType, Geometry.MemoryAccessor memAccessor)
        {
            var root = primitive.LogicalParent.LogicalParent;

            var accessor = root.CreateAccessor();

            accessor.SetIndexData(memAccessor);

            primitive.DrawPrimitiveType = primitiveType;
            primitive.SetIndexAccessor(accessor);

            return primitive;
        }

        #endregion

        #region material

        public static MeshPrimitive WithMaterial(this MeshPrimitive primitive, Material material)
        {
            primitive.Material = material;
            return primitive;
        }

        #endregion
    }
}
