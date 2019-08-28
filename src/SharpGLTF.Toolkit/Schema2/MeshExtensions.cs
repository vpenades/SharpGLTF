using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;
using System.Linq;

using SharpGLTF.Memory;
using SharpGLTF.Geometry;
using SharpGLTF.Geometry.VertexTypes;

using MESHXFORM = SharpGLTF.Transforms.IGeometryTransform;

namespace SharpGLTF.Schema2
{
    public static partial class Schema2Toolkit
    {
        #region meshes

        public static Mesh CreateMesh(this ModelRoot root, IMeshBuilder<Materials.MaterialBuilder> mesh)
        {
            return root.CreateMeshes(mesh)[0];
        }

        public static Mesh CreateMesh<TMaterial>(this ModelRoot root, Func<TMaterial, Material> materialEvaluator, IMeshBuilder<TMaterial> mesh)
        {
            return root.CreateMeshes<TMaterial>(materialEvaluator, mesh)[0];
        }

        public static IReadOnlyList<Mesh> CreateMeshes(this ModelRoot root, params IMeshBuilder<Materials.MaterialBuilder>[] meshBuilders)
        {
            // until this point, even if the multiple material instances used by the meshes have the same content definition,
            // we must handling material equality by its object reference and nothing else, because Materials.MaterialBuilder
            // is a mutable object, and we cannot guarantee two material instances will keep having the same content.

            // it is at this point where we can coalesce materials with the same content.

            // TODO: in order to coalesce MaterialBuilder instances with same content
            // an IMeshBuilder could wrap the incoming mesh, and merge primitives with shared meshes.

            // MaterialBuilder instances can be grouped by their content, so we use this dictionary
            // to reduce the number of equal materials. This is specially useful for the default material.

            var materials = new Dictionary<Materials.MaterialBuilder, Material>(Materials.MaterialBuilder.ContentComparer);

            Material matFactory(Materials.MaterialBuilder srcMat)
            {
                if (materials.TryGetValue(srcMat, out Schema2.Material dstMat)) return dstMat;
                return materials[srcMat] = root.CreateMaterial(srcMat);
            }

            return root.CreateMeshes(matFactory, meshBuilders);
        }

        public static IReadOnlyList<Mesh> CreateMeshes<TMaterial>(this ModelRoot root, Func<TMaterial, Material> materialEvaluator, params IMeshBuilder<TMaterial>[] meshBuilders)
        {
            Guard.NotNull(root, nameof(root));
            Guard.NotNull(materialEvaluator, nameof(materialEvaluator));
            Guard.NotNull(meshBuilders, nameof(meshBuilders));

            return root.CreateMeshes(materialEvaluator, true, meshBuilders);
        }

        public static IReadOnlyList<Mesh> CreateMeshes<TMaterial>(this ModelRoot root, Func<TMaterial, Material> materialEvaluator, bool strided, params IMeshBuilder<TMaterial>[] meshBuilders)
        {
            Guard.NotNull(root, nameof(root));
            Guard.NotNull(materialEvaluator, nameof(materialEvaluator));
            Guard.NotNull(meshBuilders, nameof(meshBuilders));
            Guard.IsTrue(meshBuilders.Length == meshBuilders.Distinct().Count(), nameof(meshBuilders), "The collection has repeated meshes.");

            foreach (var m in meshBuilders) m.Validate();

            // create a new material for every unique (byRef) material in the mesh builders.
            var mapMaterials = meshBuilders
                .SelectMany(item => item.Primitives)
                .Where(item => !item.IsEmpty())
                .Select(item => item.Material)
                .Distinct()
                .ToDictionary(m => m, m => materialEvaluator(m));

            // create Schema2.Mesh collections for every gathered group.

            var srcMeshes = PackedMeshBuilder<TMaterial>
                .CreatePackedMeshes(meshBuilders, strided)
                .ToList();

            PackedMeshBuilder<TMaterial>.MergeBuffers(srcMeshes);

            // create schema2 meshes

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

        public static MeshPrimitive WithIndicesAutomatic(this MeshPrimitive primitive, PrimitiveType primitiveType)
        {
            Guard.NotNull(primitive, nameof(primitive));

            var root = primitive.LogicalParent.LogicalParent;

            primitive.DrawPrimitiveType = primitiveType;
            primitive.SetIndexAccessor(null);

            return primitive;
        }

        public static MeshPrimitive WithIndicesAccessor(this MeshPrimitive primitive, PrimitiveType primitiveType, IReadOnlyList<Int32> values)
        {
            Guard.NotNull(primitive, nameof(primitive));
            Guard.NotNull(values, nameof(values));

            var root = primitive.LogicalParent.LogicalParent;

            // create an index buffer and fill it
            var view = root.UseBufferView(new Byte[4 * values.Count], 0, null, 0, BufferMode.ELEMENT_ARRAY_BUFFER);
            var array = new IntegerArray(view.Content);
            array.Fill(values);

            var accessor = root.CreateAccessor();

            accessor.SetIndexData(view, 0, values.Count, IndexEncodingType.UNSIGNED_INT);

            primitive.DrawPrimitiveType = primitiveType;
            primitive.SetIndexAccessor(accessor);

            return primitive;
        }

        public static MeshPrimitive WithVertexAccessor(this MeshPrimitive primitive, string attribute, IReadOnlyList<Single> values)
        {
            Guard.NotNull(primitive, nameof(primitive));
            Guard.NotNull(values, nameof(values));

            var root = primitive.LogicalParent.LogicalParent;

            // create a vertex buffer and fill it
            var view = root.UseBufferView(new Byte[4 * values.Count], 0, null, 0, BufferMode.ARRAY_BUFFER);
            var array = new ScalarArray(view.Content);
            array.Fill(values);

            var accessor = root.CreateAccessor();
            primitive.SetVertexAccessor(attribute, accessor);

            accessor.SetVertexData(view, 0, values.Count, DimensionType.SCALAR, EncodingType.FLOAT, false);

            return primitive;
        }

        public static MeshPrimitive WithVertexAccessor(this MeshPrimitive primitive, string attribute, IReadOnlyList<Vector2> values)
        {
            Guard.NotNull(primitive, nameof(primitive));
            Guard.NotNull(values, nameof(values));

            var root = primitive.LogicalParent.LogicalParent;

            // create a vertex buffer and fill it
            var view = root.UseBufferView(new Byte[8 * values.Count], 0, null, 0, BufferMode.ARRAY_BUFFER);
            var array = new Vector2Array(view.Content);
            array.Fill(values);

            var accessor = root.CreateAccessor();
            primitive.SetVertexAccessor(attribute, accessor);

            accessor.SetVertexData(view, 0, values.Count, DimensionType.VEC2, EncodingType.FLOAT, false);

            return primitive;
        }

        public static MeshPrimitive WithVertexAccessor(this MeshPrimitive primitive, string attribute, IReadOnlyList<Vector3> values)
        {
            Guard.NotNull(primitive, nameof(primitive));
            Guard.NotNull(values, nameof(values));

            var root = primitive.LogicalParent.LogicalParent;

            // create a vertex buffer and fill it
            var view = root.UseBufferView(new Byte[12 * values.Count], 0, null, 0, BufferMode.ARRAY_BUFFER);
            var array = new Vector3Array(view.Content);
            array.Fill(values);

            var accessor = root.CreateAccessor();

            accessor.SetVertexData(view, 0, values.Count, DimensionType.VEC3, EncodingType.FLOAT, false);

            primitive.SetVertexAccessor(attribute, accessor);

            return primitive;
        }

        public static MeshPrimitive WithVertexAccessor(this MeshPrimitive primitive, string attribute, IReadOnlyList<Vector4> values)
        {
            Guard.NotNull(primitive, nameof(primitive));
            Guard.NotNull(values, nameof(values));

            var root = primitive.LogicalParent.LogicalParent;

            // create a vertex buffer and fill it
            var view = root.UseBufferView(new Byte[16 * values.Count], 0, null, 0, BufferMode.ARRAY_BUFFER);
            var array = new Vector4Array(view.Content);
            array.Fill(values);

            var accessor = root.CreateAccessor();

            accessor.SetVertexData(view, 0, values.Count, DimensionType.VEC4, EncodingType.FLOAT, false);

            primitive.SetVertexAccessor(attribute, accessor);

            return primitive;
        }

        public static MeshPrimitive WithVertexAccessors(this MeshPrimitive primitive, IReadOnlyList<VertexPosition> vertices)
        {
            var xvertices = vertices
                .Select(item => new VertexBuilder<VertexPosition, VertexEmpty, VertexEmpty>(item))
                .ToList();

            return primitive.WithVertexAccessors(xvertices);
        }

        public static MeshPrimitive WithVertexAccessors(this MeshPrimitive primitive, IReadOnlyList<VertexPositionNormal> vertices)
        {
            var xvertices = vertices
                .Select(item => new VertexBuilder<VertexPositionNormal, VertexEmpty, VertexEmpty>(item))
                .ToList();

            return primitive.WithVertexAccessors(xvertices);
        }

        public static MeshPrimitive WithVertexAccessors<TvP, TvM>(this MeshPrimitive primitive, IReadOnlyList<(TvP, TvM)> vertices)
            where TvP : struct, IVertexGeometry
            where TvM : struct, IVertexMaterial
        {
            var xvertices = vertices
                .Select(item => new VertexBuilder<TvP, TvM, VertexEmpty>(item.Item1, item.Item2))
                .ToList();

            return primitive.WithVertexAccessors(xvertices);
        }

        public static MeshPrimitive WithVertexAccessors<TvP, TvM, TvS>(this MeshPrimitive primitive, IReadOnlyList<(TvP, TvM, TvS)> vertices)
            where TvP : struct, IVertexGeometry
            where TvM : struct, IVertexMaterial
            where TvS : struct, IVertexSkinning
        {
            var xvertices = vertices
                .Select(item => new VertexBuilder<TvP, TvM, TvS>(item.Item1, item.Item2, item.Item3))
                .ToList();

            return primitive.WithVertexAccessors(xvertices);
        }

        public static MeshPrimitive WithVertexAccessors<TVertex>(this MeshPrimitive primitive, IReadOnlyList<TVertex> vertices)
            where TVertex : IVertexBuilder
        {
            var memAccessors = VertexUtils.CreateVertexMemoryAccessors( vertices );

            return primitive.WithVertexAccessors(memAccessors);
        }

        public static MeshPrimitive WithVertexAccessors(this MeshPrimitive primitive, IEnumerable<Memory.MemoryAccessor> memAccessors)
        {
            Guard.NotNull(memAccessors, nameof(memAccessors));
            Guard.IsTrue(memAccessors.All(item => item != null), nameof(memAccessors));

            foreach (var va in memAccessors) primitive.WithVertexAccessor(va);

            return primitive;
        }

        public static MeshPrimitive WithVertexAccessor(this MeshPrimitive primitive, Memory.MemoryAccessor memAccessor)
        {
            Guard.NotNull(primitive, nameof(primitive));
            Guard.NotNull(memAccessor, nameof(memAccessor));

            var root = primitive.LogicalParent.LogicalParent;

            primitive.SetVertexAccessor(memAccessor.Attribute.Name, root.CreateVertexAccessor(memAccessor));

            return primitive;
        }

        public static MeshPrimitive WithIndicesAccessor(this MeshPrimitive primitive, PrimitiveType primitiveType, Memory.MemoryAccessor memAccessor)
        {
            Guard.NotNull(primitive, nameof(primitive));

            var root = primitive.LogicalParent.LogicalParent;

            var accessor = root.CreateAccessor();

            accessor.SetIndexData(memAccessor);

            primitive.DrawPrimitiveType = primitiveType;
            primitive.SetIndexAccessor(accessor);

            return primitive;
        }

        public static MeshPrimitive WithMorphTargetAccessors(this MeshPrimitive primitive, int targetIndex, IEnumerable<Memory.MemoryAccessor> memAccessors)
        {
            Guard.NotNull(primitive, nameof(primitive));
            Guard.MustBeGreaterThanOrEqualTo(targetIndex, 0, nameof(targetIndex));
            Guard.NotNull(memAccessors, nameof(memAccessors));

            var root = primitive.LogicalParent.LogicalParent;

            var accessors = memAccessors.ToDictionary(item => item.Attribute.Name, item => root.CreateVertexAccessor(item));

            primitive.SetMorphTargetAccessors(targetIndex, accessors);

            return primitive;
        }

        #endregion

        #region material

        public static MeshPrimitive WithMaterial(this MeshPrimitive primitive, Material material)
        {
            Guard.NotNull(primitive, nameof(primitive));

            primitive.Material = material;
            return primitive;
        }

        #endregion

        #region evaluation

        public static int GetPrimitiveVertexSize(this PrimitiveType ptype)
        {
            switch (ptype)
            {
                case PrimitiveType.POINTS: return 1;
                case PrimitiveType.LINES: return 2;
                case PrimitiveType.LINE_LOOP: return 2;
                case PrimitiveType.LINE_STRIP: return 2;
                case PrimitiveType.TRIANGLES: return 3;
                case PrimitiveType.TRIANGLE_FAN: return 3;
                case PrimitiveType.TRIANGLE_STRIP: return 3;
                default: throw new NotImplementedException();
            }
        }

        public static IEnumerable<int> GetPointIndices(this MeshPrimitive primitive)
        {
            if (primitive == null || primitive.DrawPrimitiveType.GetPrimitiveVertexSize() != 1) return Enumerable.Empty<int>();

            if (primitive.IndexAccessor == null) return Enumerable.Range(0, primitive.GetVertexAccessor("POSITION").Count);

            return primitive.IndexAccessor.AsIndicesArray().Select(item => (int)item);
        }

        public static IEnumerable<(int, int)> GetLineIndices(this MeshPrimitive primitive)
        {
            if (primitive == null || primitive.DrawPrimitiveType.GetPrimitiveVertexSize() != 2) return Enumerable.Empty<(int, int)>();

            if (primitive.IndexAccessor == null) return primitive.DrawPrimitiveType.GetLinesIndices(primitive.GetVertexAccessor("POSITION").Count);

            return primitive.DrawPrimitiveType.GetLinesIndices(primitive.IndexAccessor.AsIndicesArray());
        }

        public static IEnumerable<(int, int, int)> GetTriangleIndices(this MeshPrimitive primitive)
        {
            if (primitive == null || primitive.DrawPrimitiveType.GetPrimitiveVertexSize() != 3) return Enumerable.Empty<(int, int, int)>();

            if (primitive.IndexAccessor == null) return primitive.DrawPrimitiveType.GetTrianglesIndices(primitive.GetVertexAccessor("POSITION").Count);

            return primitive.DrawPrimitiveType.GetTrianglesIndices(primitive.IndexAccessor.AsIndicesArray());
        }

        public static IEnumerable<(IVertexBuilder, Material)> EvaluatePoints(this Mesh mesh, MESHXFORM xform = null)
        {
            if (mesh == null) return Enumerable.Empty<(IVertexBuilder, Material)>();

            return mesh.Primitives.SelectMany(item => item.EvaluatePoints(xform));
        }

        public static IEnumerable<(IVertexBuilder, Material)> EvaluatePoints(this MeshPrimitive prim, MESHXFORM xform = null)
        {
            if (prim == null) yield break;
            if (xform != null && !xform.Visible) yield break;

            var points = prim.GetPointIndices();
            if (!points.Any()) yield break;

            var vertices = prim.GetVertexColumns(xform);
            var vtype = vertices.GetCompatibleVertexType();

            foreach (var t in points)
            {
                var a = vertices.GetVertex(vtype, t);

                yield return (a, prim.Material);
            }
        }

        public static IEnumerable<(IVertexBuilder, IVertexBuilder, Material)> EvaluateLines(this Mesh mesh, MESHXFORM xform = null)
        {
            if (mesh == null) return Enumerable.Empty<(IVertexBuilder, IVertexBuilder, Material)>();

            return mesh.Primitives.SelectMany(item => item.EvaluateLines(xform));
        }

        public static IEnumerable<(IVertexBuilder, IVertexBuilder, Material)> EvaluateLines(this MeshPrimitive prim, MESHXFORM xform = null)
        {
            if (prim == null) yield break;
            if (xform != null && !xform.Visible) yield break;

            var lines = prim.GetLineIndices();
            if (!lines.Any()) yield break;

            var vertices = prim.GetVertexColumns(xform);
            var vtype = vertices.GetCompatibleVertexType();

            foreach (var t in lines)
            {
                var a = vertices.GetVertex(vtype, t.Item1);
                var b = vertices.GetVertex(vtype, t.Item2);

                yield return (a, b, prim.Material);
            }
        }

        public static IEnumerable<(IVertexBuilder, IVertexBuilder, IVertexBuilder, Material)> EvaluateTriangles(this Mesh mesh, MESHXFORM xform = null)
        {
            if (mesh == null) return Enumerable.Empty<(IVertexBuilder, IVertexBuilder, IVertexBuilder, Material)>();

            return mesh.Primitives.SelectMany(item => item.EvaluateTriangles(xform));
        }

        public static IEnumerable<(IVertexBuilder, IVertexBuilder, IVertexBuilder, Material)> EvaluateTriangles(this MeshPrimitive prim, MESHXFORM xform = null)
        {
            if (prim == null) yield break;
            if (xform != null && !xform.Visible) yield break;

            var triangles = prim.GetTriangleIndices();
            if (!triangles.Any()) yield break;

            var vertices = prim.GetVertexColumns(xform);
            var vtype = vertices.GetCompatibleVertexType();

            foreach (var t in triangles)
            {
                var a = vertices.GetVertex(vtype, t.Item1);
                var b = vertices.GetVertex(vtype, t.Item2);
                var c = vertices.GetVertex(vtype, t.Item3);

                yield return (a, b, c, prim.Material);
            }
        }

        public static IEnumerable<(VertexBuilder<TvG, TvM, TvS>, VertexBuilder<TvG, TvM, TvS>, VertexBuilder<TvG, TvM, TvS>, Material)> EvaluateTriangles<TvG, TvM, TvS>(this Mesh mesh, MESHXFORM xform = null)
            where TvG : struct, IVertexGeometry
            where TvM : struct, IVertexMaterial
            where TvS : struct, IVertexSkinning
        {
            if (mesh == null) return Enumerable.Empty<(VertexBuilder<TvG, TvM, TvS>, VertexBuilder<TvG, TvM, TvS>, VertexBuilder<TvG, TvM, TvS>, Material)>();

            return mesh.Primitives.SelectMany(item => item.EvaluateTriangles<TvG, TvM, TvS>(xform));
        }

        public static IEnumerable<(VertexBuilder<TvG, TvM, TvS>, VertexBuilder<TvG, TvM, TvS>, VertexBuilder<TvG, TvM, TvS>, Material)> EvaluateTriangles<TvG, TvM, TvS>(this MeshPrimitive prim, MESHXFORM xform = null)
            where TvG : struct, IVertexGeometry
            where TvM : struct, IVertexMaterial
            where TvS : struct, IVertexSkinning
        {
            if (prim == null) yield break;
            if (xform != null && !xform.Visible) yield break;

            var triangles = prim.GetTriangleIndices();
            if (!triangles.Any()) yield break;

            var vertices = prim.GetVertexColumns(xform);

            bool hasNormals = vertices.Normals != null;

            foreach (var t in triangles)
            {
                var a = vertices.GetVertex<TvG, TvM, TvS>(t.Item1);
                var b = vertices.GetVertex<TvG, TvM, TvS>(t.Item2);
                var c = vertices.GetVertex<TvG, TvM, TvS>(t.Item3);

                if (!hasNormals)
                {
                    var n = Vector3.Cross(b.Position - a.Position, c.Position - a.Position);
                    n = Vector3.Normalize(n);
                    a.Geometry.SetNormal(n);
                    b.Geometry.SetNormal(n);
                    c.Geometry.SetNormal(n);
                }

                yield return (a, b, c, prim.Material);
            }
        }

        #endregion

        #region mesh conversion

        public static VertexBufferColumns GetVertexColumns(this MeshPrimitive primitive, MESHXFORM xform = null)
        {
            Guard.NotNull(primitive, nameof(primitive));

            var columns = new VertexBufferColumns();

            _Initialize(primitive.VertexAccessors, columns);

            for (int i = 0; i < primitive.MorphTargetsCount; ++i)
            {
                var morphTarget = primitive.GetMorphTargetAccessors(i);
                _Initialize(morphTarget, columns.AddMorphTarget());
            }

            if (xform != null) columns.ApplyTransform(xform);

            return columns;
        }

        private static void _Initialize(IReadOnlyDictionary<string, Accessor> vertexAccessors, VertexBufferColumns dstColumns)
        {
            if (vertexAccessors.ContainsKey("POSITION")) dstColumns.Positions = vertexAccessors["POSITION"].AsVector3Array();
            if (vertexAccessors.ContainsKey("NORMAL")) dstColumns.Normals = vertexAccessors["NORMAL"].AsVector3Array();
            if (vertexAccessors.ContainsKey("TANGENT")) dstColumns.Tangents = vertexAccessors["TANGENT"].AsColorArray(0);

            if (vertexAccessors.ContainsKey("COLOR_0")) dstColumns.Colors0 = vertexAccessors["COLOR_0"].AsColorArray(1);
            if (vertexAccessors.ContainsKey("COLOR_1")) dstColumns.Colors1 = vertexAccessors["COLOR_1"].AsColorArray(1);

            if (vertexAccessors.ContainsKey("TEXCOORD_0")) dstColumns.TexCoords0 = vertexAccessors["TEXCOORD_0"].AsVector2Array();
            if (vertexAccessors.ContainsKey("TEXCOORD_1")) dstColumns.TexCoords1 = vertexAccessors["TEXCOORD_1"].AsVector2Array();

            if (vertexAccessors.ContainsKey("JOINTS_0")) dstColumns.Joints0 = vertexAccessors["JOINTS_0"].AsVector4Array();
            if (vertexAccessors.ContainsKey("JOINTS_1")) dstColumns.Joints1 = vertexAccessors["JOINTS_1"].AsVector4Array();

            if (vertexAccessors.ContainsKey("WEIGHTS_0")) dstColumns.Weights0 = vertexAccessors["WEIGHTS_0"].AsVector4Array();
            if (vertexAccessors.ContainsKey("WEIGHTS_1")) dstColumns.Weights1 = vertexAccessors["WEIGHTS_1"].AsVector4Array();
        }

        /// <summary>
        /// Calculates a default set of normals for the given mesh.
        /// </summary>
        /// <param name="mesh">A <see cref="Mesh"/> instance.</param>
        /// <returns>A <see cref="Dictionary{TKey, TValue}"/> where the keys represent positions and the values represent Normals.</returns>
        public static Dictionary<Vector3, Vector3> GetComputedNormals(this Mesh mesh)
        {
            if (mesh == null) return null;

            var posnrm = new Dictionary<Vector3, Vector3>();

            void addDirection(Dictionary<Vector3, Vector3> dict, Vector3 pos, Vector3 dir)
            {
                if (!dir._IsFinite()) return;
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

        public static void AddMesh<TMaterial, TvG, TvM, TvS>(this MeshBuilder<TMaterial, TvG, TvM, TvS> meshBuilder, Mesh srcMesh, Func<Material, TMaterial> materialFunc)
            where TvG : struct, IVertexGeometry
            where TvM : struct, IVertexMaterial
            where TvS : struct, IVertexSkinning
        {
            Guard.NotNull(meshBuilder, nameof(meshBuilder));
            Guard.NotNull(materialFunc, nameof(materialFunc));

            if (srcMesh == null) return;

            foreach (var srcPrim in srcMesh.Primitives)
            {
                if (srcPrim != null) continue;

                var dstMat = materialFunc(srcPrim.Material);
                var dstPrim = meshBuilder.UsePrimitive(dstMat);

                foreach (var tri in srcPrim.EvaluateTriangles<TvG, TvM, TvS>(null))
                {
                    dstPrim.AddTriangle(tri.Item1, tri.Item2, tri.Item3);
                }
            }
        }

        /// <summary>
        /// Evaluates the current <paramref name="srcScene"/> at a given <paramref name="animation"/> and <paramref name="time"/>
        /// and creates a static <see cref="MeshBuilder{TMaterial, TvG, TvM, TvS}"/>
        /// </summary>
        /// <typeparam name="TMaterial">Any material type</typeparam>
        /// <typeparam name="TvG">A subtype of <see cref="IVertexGeometry"/></typeparam>
        /// <typeparam name="TvM">A subtype of <see cref="IVertexMaterial"/></typeparam>
        /// <param name="srcScene">The source <see cref="Scene"/> to evaluate.</param>
        /// <param name="materialFunc">A function to convert <see cref="Material"/> into <typeparamref name="TMaterial"/>.</param>
        /// <param name="animation">The source <see cref="Animation"/> to evaluate.</param>
        /// <param name="time">A time point, in seconds, within <paramref name="animation"/>.</param>
        /// <returns>A new <see cref="MeshBuilder{TMaterial, TvG, TvM, TvS}"/> containing the evaluated geometry.</returns>
        public static MeshBuilder<TMaterial, TvG, TvM, VertexEmpty> ToStaticMeshBuilder<TMaterial, TvG, TvM>(this Scene srcScene, Func<Material, TMaterial> materialFunc, Animation animation, float time)
            where TvG : struct, IVertexGeometry
            where TvM : struct, IVertexMaterial
        {
            var mesh = new MeshBuilder<TMaterial, TvG, TvM, VertexEmpty>();

            if (srcScene == null) return mesh;

            if (animation != null) Guard.MustShareLogicalParent(srcScene, animation, nameof(animation));

            Guard.NotNull(materialFunc, nameof(materialFunc));

            foreach (var tri in srcScene.EvaluateTriangles<VertexPositionNormal, VertexColor1Texture1>(animation, time))
            {
                var material = materialFunc(tri.Item4);

                mesh.UsePrimitive(material).AddTriangle(tri.Item1, tri.Item2, tri.Item3);
            }

            return mesh;
        }

        public static MeshBuilder<Materials.MaterialBuilder, TvG, TvM, VertexEmpty> ToStaticMeshBuilder<TvG, TvM>(this Scene srcScene, Animation animation, float time)
            where TvG : struct, IVertexGeometry
            where TvM : struct, IVertexMaterial
        {
            var materials = new Dictionary<Material, Materials.MaterialBuilder>();

            Materials.MaterialBuilder convertMaterial(Material srcMaterial)
            {
                if (materials.TryGetValue(srcMaterial, out Materials.MaterialBuilder dstMaterial)) return dstMaterial;

                dstMaterial = new Materials.MaterialBuilder();
                srcMaterial.CopyTo(dstMaterial);

                // if we find an exiting match, we will use it instead.
                var oldMaterial = materials.Values.FirstOrDefault(item => Materials.MaterialBuilder.AreEqualByContent(dstMaterial, item));
                if (oldMaterial != null) dstMaterial = oldMaterial;

                return materials[srcMaterial] = dstMaterial;
            }

            return srcScene.ToStaticMeshBuilder<Materials.MaterialBuilder, TvG, TvM>(convertMaterial, animation, time);
        }

        public static IMeshBuilder<Materials.MaterialBuilder> ToMeshBuilder(this Mesh srcMesh)
        {
            if (srcMesh == null) return null;

            var vertexAttributes = srcMesh.Primitives
                .SelectMany(item => item.VertexAccessors.Keys)
                .Distinct()
                .ToArray();

            var dstMesh = MeshBuilderToolkit.CreateMeshBuilderFromVertexAttributes<Materials.MaterialBuilder>(vertexAttributes);
            dstMesh.Name = srcMesh.Name;

            Materials.MaterialBuilder defMat = null;

            var dstMaterials = new Dictionary<Material, Materials.MaterialBuilder>();

            IPrimitiveBuilder GetPrimitive(Material srcMaterial, int vcount)
            {
                IPrimitiveBuilder dstPrim = null;

                if (srcMaterial == null)
                {
                    if (defMat == null) defMat = Materials.MaterialBuilder.CreateDefault();
                    dstPrim = dstMesh.UsePrimitive(defMat, vcount);
                }
                else
                {
                    if (!dstMaterials.TryGetValue(srcMaterial, out Materials.MaterialBuilder dstMat))
                    {
                        dstMat = new Materials.MaterialBuilder();
                        srcMaterial.CopyTo(dstMat);
                        dstMaterials[srcMaterial] = dstMat;
                    }

                    dstPrim = dstMesh.UsePrimitive(dstMat, vcount);
                }

                return dstPrim;
            }

            foreach (var srcPrim in srcMesh.Primitives)
            {
                int vcount = 0;
                if (srcPrim.GetPointIndices().Any()) vcount = 1;
                if (srcPrim.GetLineIndices().Any()) vcount = 2;
                if (srcPrim.GetTriangleIndices().Any()) vcount = 3;

                var dstPrim = GetPrimitive(srcPrim.Material, vcount);

                dstPrim.AddPrimitiveGeometry(srcPrim);
            }

            return dstMesh;
        }

        private static void AddPrimitiveGeometry(this IPrimitiveBuilder dstPrim, MeshPrimitive srcPrim)
        {
            Guard.NotNull(dstPrim, nameof(dstPrim));

            var vertices = srcPrim.GetVertexColumns();
            var vmap = new Dictionary<int, int>();

            foreach (var srcPoint in srcPrim.GetPointIndices())
            {
                var v = vertices.GetVertex(dstPrim.VertexType, srcPoint);

                var idx = dstPrim.AddPoint(v);

                vmap[srcPoint] = idx;
            }

            foreach (var srcLine in srcPrim.GetLineIndices())
            {
                var v1 = vertices.GetVertex(dstPrim.VertexType, srcLine.Item1);
                var v2 = vertices.GetVertex(dstPrim.VertexType, srcLine.Item2);

                var indices = dstPrim.AddLine(v1, v2);

                vmap[srcLine.Item1] = indices.Item1;
                vmap[srcLine.Item2] = indices.Item2;
            }

            foreach (var srcTri in srcPrim.GetTriangleIndices())
            {
                var v1 = vertices.GetVertex(dstPrim.VertexType, srcTri.Item1);
                var v2 = vertices.GetVertex(dstPrim.VertexType, srcTri.Item2);
                var v3 = vertices.GetVertex(dstPrim.VertexType, srcTri.Item3);

                var indices = dstPrim.AddTriangle(v1, v2, v3);

                vmap[srcTri.Item1] = indices.Item1;
                vmap[srcTri.Item2] = indices.Item2;
                vmap[srcTri.Item3] = indices.Item3;
            }

            for (int tidx = 0; tidx < vertices.MorphTargets.Count; ++tidx)
            {
                var srcTarget = vertices.MorphTargets[tidx];

                foreach (var kvp in vmap)
                {
                    if (kvp.Value < 0) continue;

                    var v = srcTarget.GetVertex(dstPrim.VertexType, kvp.Key);

                    dstPrim.SetVertexDisplacement(tidx, kvp.Value, v.GetGeometry());
                }
            }
        }

        public static void SaveAsWavefront(this ModelRoot model, string filePath)
        {
            Guard.NotNull(model, nameof(model));
            Guard.NotNullOrEmpty(filePath, nameof(filePath));
            Guard.IsFalse(filePath.Any(c => char.IsWhiteSpace(c)), nameof(filePath), "Whitespace characters not allowed in filename");

            var wf = new IO.WavefrontWriter();
            wf.AddModel(model);
            wf.WriteFiles(filePath);
        }

        public static void SaveAsWavefront(this ModelRoot model, string filePath, Animation animation, float time)
        {
            Guard.NotNull(model, nameof(model));
            Guard.NotNullOrEmpty(filePath, nameof(filePath));
            Guard.IsFalse(filePath.Any(c => char.IsWhiteSpace(c)), nameof(filePath), "Whitespace characters not allowed in filename");

            var wf = new IO.WavefrontWriter();
            wf.AddModel(model, animation, time);
            wf.WriteFiles(filePath);
        }

        #endregion
    }
}
