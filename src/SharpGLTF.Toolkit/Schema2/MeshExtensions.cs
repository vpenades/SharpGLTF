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

        public static Mesh CreateMesh<TvP, TvM, TvJ>(this ModelRoot root, Geometry.MeshBuilder<Material, TvP, TvM, TvJ> meshBuilder)
            where TvP : struct, Geometry.VertexTypes.IVertexPosition
            where TvM : struct, Geometry.VertexTypes.IVertexMaterial
            where TvJ : struct, Geometry.VertexTypes.IVertexJoints
        {
            return root.CreateMeshes(m => m, meshBuilder).First();
        }

        public static Mesh CreateMesh<TMaterial, TvP, TvM, TvJ>(this ModelRoot root, Func<TMaterial, Material> materialEvaluator, Geometry.MeshBuilder<TMaterial, TvP, TvM, TvJ> meshBuilder)
            where TvP : struct, Geometry.VertexTypes.IVertexPosition
            where TvM : struct, Geometry.VertexTypes.IVertexMaterial
            where TvJ : struct, Geometry.VertexTypes.IVertexJoints
        {
            return root.CreateMeshes(materialEvaluator, meshBuilder).First();
        }

        public static IReadOnlyList<Mesh> CreateMeshes<TvP, TvM, TvJ>(this ModelRoot root, params Geometry.MeshBuilder<Material, TvP, TvM, TvJ>[] meshBuilders)
            where TvP : struct, Geometry.VertexTypes.IVertexPosition
            where TvM : struct, Geometry.VertexTypes.IVertexMaterial
            where TvJ : struct, Geometry.VertexTypes.IVertexJoints
        {
            return root.CreateMeshes(k => k, meshBuilders);
        }

        public static IReadOnlyList<Mesh> CreateMeshes<TMaterial, TvP, TvM, TvJ>(this ModelRoot root, Func<TMaterial, Material> materialEvaluator, params Geometry.MeshBuilder<TMaterial, TvP, TvM, TvJ>[] meshBuilders)
            where TvP : struct, Geometry.VertexTypes.IVertexPosition
            where TvM : struct, Geometry.VertexTypes.IVertexMaterial
            where TvJ : struct, Geometry.VertexTypes.IVertexJoints
        {
            // create a new material for every unique material in the mesh builders.
            var mapMaterials = meshBuilders
                .SelectMany(item => item.Primitives)
                .Select(item => item.Material)
                .Distinct()
                .ToDictionary(k => k, k => materialEvaluator(k));

            // creates meshes and primitives using MemoryAccessors using a single, shared vertex and index buffer
            var srcMeshes = Geometry.PackedMeshBuilder<TMaterial>
                .PackMeshes(meshBuilders)
                .ToList();

            var dstMeshes = new List<Mesh>();

            foreach (var srcMesh in srcMeshes)
            {
                var dstMesh = srcMesh.CreateSchema2Mesh(root, m => mapMaterials[m]);

                dstMeshes.Add(dstMesh);
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
            where TVertex : struct, Geometry.VertexTypes.IVertexPosition
        {
            var xvertices = vertices.Select(item => (item, default(Geometry.VertexTypes.VertexEmpty), default(Geometry.VertexTypes.VertexEmpty))).ToList();

            return primitive.WithVertexAccessors(xvertices);
        }

        public static MeshPrimitive WithVertexAccessors<TVertex, TValues>(this MeshPrimitive primitive, IReadOnlyList<(TVertex, TValues)> vertices)
            where TVertex : struct, Geometry.VertexTypes.IVertexPosition
            where TValues : struct, Geometry.VertexTypes.IVertexMaterial
        {
            var xvertices = vertices.Select(item => (item.Item1, item.Item2, default(Geometry.VertexTypes.VertexEmpty))).ToList();

            return primitive.WithVertexAccessors(xvertices);
        }

        public static MeshPrimitive WithVertexAccessors<TVertex, TValues, TJoints>(this MeshPrimitive primitive, IReadOnlyList<(TVertex, TValues, TJoints)> vertices)
            where TVertex : struct, Geometry.VertexTypes.IVertexPosition
            where TValues : struct, Geometry.VertexTypes.IVertexMaterial
            where TJoints : struct, Geometry.VertexTypes.IVertexJoints
        {
            var memAccessors = Geometry.VertexTypes.VertexUtils.CreateVertexMemoryAccessors(new[] { vertices }).First();

            return primitive.WithVertexAccessors(memAccessors);
        }

        public static MeshPrimitive WithVertexAccessors(this MeshPrimitive primitive, IEnumerable<Memory.MemoryAccessor> memAccessors)
        {
            foreach (var va in memAccessors) primitive.WithVertexAccessor(va);

            return primitive;
        }

        public static MeshPrimitive WithVertexAccessor(this MeshPrimitive primitive, Memory.MemoryAccessor memAccessor)
        {
            var root = primitive.LogicalParent.LogicalParent;

            primitive.SetVertexAccessor(memAccessor.Attribute.Name, root.CreateVertexAccessor(memAccessor));

            return primitive;
        }

        public static MeshPrimitive WithIndicesAccessor(this MeshPrimitive primitive, PrimitiveType primitiveType, Memory.MemoryAccessor memAccessor)
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
