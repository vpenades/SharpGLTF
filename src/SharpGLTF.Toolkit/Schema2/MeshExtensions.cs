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

        #region evaluation

        public static IEnumerable<((TvP, TvM, TvJ), (TvP, TvM, TvJ), (TvP, TvM, TvJ), Material)> Triangulate<TvP, TvM, TvJ>(this Mesh mesh, Matrix4x4 xform)
            where TvP : struct, Geometry.VertexTypes.IVertexPosition
            where TvM : struct, Geometry.VertexTypes.IVertexMaterial
            where TvJ : struct, Geometry.VertexTypes.IVertexJoints
        {
            var normals = mesh.GetComputedNormals();

            return mesh.Primitives.SelectMany(item => item.Triangulate<TvP, TvM, TvJ>(xform, normals));
        }

        public static IEnumerable<((TvP, TvM, TvJ), (TvP, TvM, TvJ), (TvP, TvM, TvJ), Material)> Triangulate<TvP, TvM, TvJ>(this MeshPrimitive prim, Matrix4x4 xform, IReadOnlyDictionary<Vector3, Vector3> defaultNormals)
            where TvP : struct, Geometry.VertexTypes.IVertexPosition
            where TvM : struct, Geometry.VertexTypes.IVertexMaterial
            where TvJ : struct, Geometry.VertexTypes.IVertexJoints
        {
            var vertices = prim.GetVertexColumns();
            if (vertices.Normals == null && defaultNormals != null) vertices.SetNormals(defaultNormals);

            var triangles = prim.GetTriangleIndices();

            foreach (var t in triangles)
            {
                var ap = vertices.GetPositionFragment<TvP>(t.Item1);
                var bp = vertices.GetPositionFragment<TvP>(t.Item2);
                var cp = vertices.GetPositionFragment<TvP>(t.Item3);

                ap.Transform(xform);
                bp.Transform(xform);
                cp.Transform(xform);

                var am = vertices.GetMaterialFragment<TvM>(t.Item1);
                var bm = vertices.GetMaterialFragment<TvM>(t.Item2);
                var cm = vertices.GetMaterialFragment<TvM>(t.Item3);

                var aj = vertices.GetJointsFragment<TvJ>(t.Item1);
                var bj = vertices.GetJointsFragment<TvJ>(t.Item2);
                var cj = vertices.GetJointsFragment<TvJ>(t.Item3);

                yield return ((ap, am, aj), (bp, bm, bj), (cp, cm, cj), prim.Material);
            }
        }

        public static Geometry.VertexTypes.VertexColumns GetVertexColumns(this MeshPrimitive primitive)
        {
            var vertexAccessors = primitive.VertexAccessors;

            var columns = new Geometry.VertexTypes.VertexColumns();

            if (vertexAccessors.ContainsKey("POSITION")) columns.Positions = vertexAccessors["POSITION"].AsVector3Array();
            if (vertexAccessors.ContainsKey("NORMAL")) columns.Normals = vertexAccessors["NORMAL"].AsVector3Array();
            if (vertexAccessors.ContainsKey("TANGENT")) columns.Tangents = vertexAccessors["TANGENT"].AsVector4Array();

            if (vertexAccessors.ContainsKey("COLOR_0")) columns.Colors0 = vertexAccessors["COLOR_0"].AsVector4Array();
            if (vertexAccessors.ContainsKey("COLOR_1")) columns.Colors1 = vertexAccessors["COLOR_1"].AsVector4Array();

            if (vertexAccessors.ContainsKey("TEXCOORD_0")) columns.Textures0 = vertexAccessors["TEXCOORD_0"].AsVector2Array();
            if (vertexAccessors.ContainsKey("TEXCOORD_1")) columns.Textures1 = vertexAccessors["TEXCOORD_1"].AsVector2Array();

            if (vertexAccessors.ContainsKey("JOINTS_0")) columns.Joints0 = vertexAccessors["JOINTS_0"].AsVector4Array();
            if (vertexAccessors.ContainsKey("JOINTS_1")) columns.Joints1 = vertexAccessors["JOINTS_1"].AsVector4Array();

            if (vertexAccessors.ContainsKey("WEIGHTS_0")) columns.Weights0 = vertexAccessors["WEIGHTS_0"].AsVector4Array();
            if (vertexAccessors.ContainsKey("WEIGHTS_1")) columns.Weights1 = vertexAccessors["WEIGHTS_1"].AsVector4Array();

            return columns;
        }

        public static IEnumerable<(int, int, int)> GetTriangleIndices(this MeshPrimitive primitive)
        {
            if (primitive.IndexAccessor == null) return primitive.DrawPrimitiveType.GetTrianglesIndices(primitive.GetVertexAccessor("POSITION").Count);

            return primitive.DrawPrimitiveType.GetTrianglesIndices(primitive.IndexAccessor.AsIndicesArray());
        }

        /// <summary>
        /// Calculates a default set of normals for the given mesh.
        /// </summary>
        /// <param name="mesh">A <see cref="Mesh"/> instance.</param>
        /// <returns>A <see cref="Dictionary{TKey, TValue}"/> where the keys represent positions and the values represent Normals.</returns>
        public static Dictionary<Vector3, Vector3> GetComputedNormals(this Mesh mesh)
        {
            var posnrm = new Dictionary<Vector3, Vector3>();

            void addDirection(Dictionary<Vector3, Vector3> dict, Vector3 pos, Vector3 dir)
            {
                if (!dir._IsReal()) return;
                if (!dict.TryGetValue(pos, out Vector3 n)) n = Vector3.Zero;
                dict[pos] = n + dir;
            }

            foreach (var p in mesh.Primitives)
            {
                var positions = p.GetVertexAccessor("POSITION").AsVector3Array();

                foreach (var t in p.GetTriangleIndices())
                {
                    var p1 = positions[t.Item1];
                    var p2 = positions[t.Item2];
                    var p3 = positions[t.Item3];
                    var d = Vector3.Cross(p2 - p1, p3 - p1);
                    addDirection(posnrm, p1, d);
                    addDirection(posnrm, p2, d);
                    addDirection(posnrm, p3, d);
                }
            }

            foreach (var pos in posnrm.Keys.ToList())
            {
                posnrm[pos] = Vector3.Normalize(posnrm[pos]);
            }

            return posnrm;
        }

        public static void AddMesh<TMaterial, TvP, TvM, TvJ>(this Geometry.MeshBuilder<TMaterial, TvP, TvM, TvJ> meshBuilder, Mesh srcMesh, Matrix4x4 xform, Func<Material, TMaterial> materialFunc)
            where TvP : struct, Geometry.VertexTypes.IVertexPosition
            where TvM : struct, Geometry.VertexTypes.IVertexMaterial
            where TvJ : struct, Geometry.VertexTypes.IVertexJoints
        {
            var normals = srcMesh.GetComputedNormals();

            foreach (var srcPrim in srcMesh.Primitives)
            {
                var dstPrim = meshBuilder.UsePrimitive(materialFunc(srcPrim.Material));

                foreach (var tri in srcPrim.Triangulate<TvP, TvM, TvJ>(xform, normals))
                {
                    dstPrim.AddTriangle(tri.Item1, tri.Item2, tri.Item3);
                }
            }
        }

        public static void SaveAsWavefront(this ModelRoot model, string filePath)
        {
            var wf = new IO.WavefrontWriter();
            wf.AddModel(model);
            wf.WriteFiles(filePath);
        }

        #endregion
    }
}
