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

        public static Mesh CreateMesh<TMaterial>(this ModelRoot root, Converter<TMaterial, Material> materialEvaluator, IMeshBuilder<TMaterial> mesh)
        {
            return root.CreateMeshes<TMaterial>(materialEvaluator, mesh)[0];
        }

        public static IReadOnlyList<Mesh> CreateMeshes(this ModelRoot root, params IMeshBuilder<Materials.MaterialBuilder>[] meshBuilders)
        {
            // until this point, even if the multiple material instances used by the meshes have the same content definition,
            // we must handle material equality by its object reference and nothing else, because Materials.MaterialBuilder
            // is a mutable object, and we cannot guarantee two material instances will keep having the same content over time.

            // * it is at this point where we can coalesce materials with the same content.

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

        public static IReadOnlyList<Mesh> CreateMeshes<TMaterial>(this ModelRoot root, Converter<TMaterial, Material> materialConverter, params IMeshBuilder<TMaterial>[] meshBuilders)
        {
            Guard.NotNull(root, nameof(root));
            Guard.NotNull(materialConverter, nameof(materialConverter));
            Guard.NotNull(meshBuilders, nameof(meshBuilders));

            return root.CreateMeshes(materialConverter, Scenes.SceneBuilderSchema2Settings.Default, meshBuilders);
        }

        public static IReadOnlyList<Mesh> CreateMeshes<TMaterial>(this ModelRoot root, Converter<TMaterial, Material> materialConverter, Scenes.SceneBuilderSchema2Settings settings, params IMeshBuilder<TMaterial>[] meshBuilders)
        {
            Guard.NotNull(root, nameof(root));
            Guard.NotNull(materialConverter, nameof(materialConverter));
            Guard.NotNull(meshBuilders, nameof(meshBuilders));
            Guard.IsTrue(meshBuilders.Length == meshBuilders.Distinct().Count(), nameof(meshBuilders), "The collection has repeated meshes.");

            foreach (var m in meshBuilders) m.Validate();

            // create a new material for every unique (byRef) material in the mesh builders.
            var mapMaterials = meshBuilders
                .SelectMany(item => item.Primitives)
                .Where(item => !item.IsEmpty())
                .Select(item => item.Material)
                .Distinct()
                .ToDictionary(m => m, m => materialConverter(m));

            // create Schema2.Mesh collections for every gathered group.

            var srcMeshes = PackedMeshBuilder<TMaterial>
                .CreatePackedMeshes(meshBuilders, settings)
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

        public static MeshPrimitive WithVertexAccessors<TvP, TvM>(this MeshPrimitive primitive, IReadOnlyList<(TvP Geo, TvM Mat)> vertices)
            where TvP : struct, IVertexGeometry
            where TvM : struct, IVertexMaterial
        {
            var xvertices = vertices
                .Select(item => new VertexBuilder<TvP, TvM, VertexEmpty>(item.Geo, item.Mat))
                .ToList();

            return primitive.WithVertexAccessors(xvertices);
        }

        public static MeshPrimitive WithVertexAccessors<TvP, TvM, TvS>(this MeshPrimitive primitive, IReadOnlyList<(TvP Geo, TvM Mat, TvS Skin)> vertices)
            where TvP : struct, IVertexGeometry
            where TvM : struct, IVertexMaterial
            where TvS : struct, IVertexSkinning
        {
            var xvertices = vertices
                .Select(item => new VertexBuilder<TvP, TvM, TvS>(item.Geo, item.Mat, item.Skin))
                .ToList();

            return primitive.WithVertexAccessors(xvertices);
        }

        public static MeshPrimitive WithVertexAccessors<TVertex>(this MeshPrimitive primitive, IReadOnlyList<TVertex> vertices)
            where TVertex : IVertexBuilder
        {
            var memAccessors = VertexUtils.CreateVertexMemoryAccessors(vertices, new PackedEncoding());

            return primitive.WithVertexAccessors(memAccessors);
        }

        public static MeshPrimitive WithVertexAccessors(this MeshPrimitive primitive, IEnumerable<MemoryAccessor> memAccessors)
        {
            Guard.NotNull(memAccessors, nameof(memAccessors));
            Guard.IsTrue(memAccessors.All(item => item != null), nameof(memAccessors));

            foreach (var va in memAccessors) primitive.WithVertexAccessor(va);

            return primitive;
        }

        public static MeshPrimitive WithVertexAccessor(this MeshPrimitive primitive, MemoryAccessor memAccessor)
        {
            Guard.NotNull(primitive, nameof(primitive));
            Guard.NotNull(memAccessor, nameof(memAccessor));

            var root = primitive.LogicalParent.LogicalParent;

            primitive.SetVertexAccessor(memAccessor.Attribute.Name, root.CreateVertexAccessor(memAccessor));

            return primitive;
        }

        public static MeshPrimitive WithIndicesAccessor(this MeshPrimitive primitive, PrimitiveType primitiveType, MemoryAccessor memAccessor)
        {
            Guard.NotNull(primitive, nameof(primitive));

            var root = primitive.LogicalParent.LogicalParent;

            var accessor = root.CreateAccessor();

            accessor.SetIndexData(memAccessor);

            primitive.DrawPrimitiveType = primitiveType;
            primitive.SetIndexAccessor(accessor);

            return primitive;
        }

        public static MeshPrimitive WithMorphTargetAccessors(this MeshPrimitive primitive, int targetIndex, IEnumerable<MemoryAccessor> memAccessors)
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

        public static IEnumerable<(IVertexBuilder A, Material Material)> EvaluatePoints(this Mesh mesh, MESHXFORM xform = null)
        {
            if (mesh == null) return Enumerable.Empty<(IVertexBuilder, Material)>();

            return mesh.Primitives.SelectMany(item => item.EvaluatePoints(xform));
        }

        public static IEnumerable<(IVertexBuilder A, Material Material)> EvaluatePoints(this MeshPrimitive prim, MESHXFORM xform = null)
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

        public static IEnumerable<(IVertexBuilder A, IVertexBuilder B, Material Material)> EvaluateLines(this Mesh mesh, MESHXFORM xform = null)
        {
            if (mesh == null) return Enumerable.Empty<(IVertexBuilder, IVertexBuilder, Material)>();

            return mesh.Primitives.SelectMany(item => item.EvaluateLines(xform));
        }

        public static IEnumerable<(IVertexBuilder A, IVertexBuilder B, Material Material)> EvaluateLines(this MeshPrimitive prim, MESHXFORM xform = null)
        {
            if (prim == null) yield break;
            if (xform != null && !xform.Visible) yield break;

            var lines = prim.GetLineIndices();
            if (!lines.Any()) yield break;

            var vertices = prim.GetVertexColumns(xform);
            var vtype = vertices.GetCompatibleVertexType();

            foreach (var (la, lb) in lines)
            {
                var va = vertices.GetVertex(vtype, la);
                var vb = vertices.GetVertex(vtype, lb);

                yield return (va, vb, prim.Material);
            }
        }

        public static IEnumerable<(IVertexBuilder A, IVertexBuilder B, IVertexBuilder C, Material Material)> EvaluateTriangles(this Mesh mesh, MESHXFORM xform = null)
        {
            if (mesh == null) return Enumerable.Empty<(IVertexBuilder, IVertexBuilder, IVertexBuilder, Material)>();

            return mesh.Primitives.SelectMany(item => item.EvaluateTriangles(xform));
        }

        public static IEnumerable<(IVertexBuilder A, IVertexBuilder B, IVertexBuilder C, Material Material)> EvaluateTriangles(this MeshPrimitive prim, MESHXFORM xform = null)
        {
            if (prim == null) yield break;
            if (xform != null && !xform.Visible) yield break;

            var triangles = prim.GetTriangleIndices();
            if (!triangles.Any()) yield break;

            var vertices = prim.GetVertexColumns(xform);
            var vtype = vertices.GetCompatibleVertexType();

            foreach (var (ta, tb, tc) in triangles)
            {
                var va = vertices.GetVertex(vtype, ta);
                var vb = vertices.GetVertex(vtype, tb);
                var vc = vertices.GetVertex(vtype, tc);

                yield return (va, vb, vc, prim.Material);
            }
        }

        public static IEnumerable<(VertexBuilder<TvG, TvM, TvS> A, VertexBuilder<TvG, TvM, TvS> B, VertexBuilder<TvG, TvM, TvS> C, Material Material)> EvaluateTriangles<TvG, TvM, TvS>(this Mesh mesh, MESHXFORM xform = null)
            where TvG : struct, IVertexGeometry
            where TvM : struct, IVertexMaterial
            where TvS : struct, IVertexSkinning
        {
            if (xform != null && !xform.Visible) mesh = null;
            if (mesh == null) return Enumerable.Empty<(VertexBuilder<TvG, TvM, TvS>, VertexBuilder<TvG, TvM, TvS>, VertexBuilder<TvG, TvM, TvS>, Material)>();

            var primitives = mesh.Primitives
                .Where(prim => prim.GetTriangleIndices().Any())
                .Select(prim => (prim.Material, prim.GetVertexColumns(xform), (IEnumerable<(int, int, int)>)prim.GetTriangleIndices().ToList()))
                .ToList();

            bool needsNormals = default(TvG).TryGetNormal(out Vector3 nrm);
            bool needsTangents = default(TvG).TryGetTangent(out Vector4 tgt);

            if (needsNormals)
            {
                var prims = primitives
                    .Where(p => p.Item2.Normals == null)
                    .Select(p => (p.Item2, p.Item3))
                    .ToList();

                if (prims.Any()) VertexBufferColumns.CalculateSmoothNormals(prims);
            }

            if (needsTangents)
            {
                var prims = primitives
                    .Where(p => p.Item2.Tangents == null && p.Item2.TexCoords0 != null)
                    .Select(p => (p.Item2, p.Item3))
                    .ToList();

                if (prims.Any()) VertexBufferColumns.CalculateTangents(prims);
            }

            return primitives.SelectMany(prim => _EvaluateTriangles<TvG, TvM, TvS>(prim.Material, prim.Item2, prim.Item3));
        }

        private static IEnumerable<(VertexBuilder<TvG, TvM, TvS> A, VertexBuilder<TvG, TvM, TvS> B, VertexBuilder<TvG, TvM, TvS> C, Material Material)> _EvaluateTriangles<TvG, TvM, TvS>(Material material, VertexBufferColumns vertices, IEnumerable<(int A, int B, int C)> indices)
            where TvG : struct, IVertexGeometry
            where TvM : struct, IVertexMaterial
            where TvS : struct, IVertexSkinning
        {
            foreach (var (ta, tb, tc) in indices)
            {
                var va = vertices.GetVertex<TvG, TvM, TvS>(ta);
                var vb = vertices.GetVertex<TvG, TvM, TvS>(tb);
                var vc = vertices.GetVertex<TvG, TvM, TvS>(tc);

                yield return (va, vb, vc, material);
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

        public static void AddMesh<TMaterial, TvG, TvM, TvS>(this MeshBuilder<TMaterial, TvG, TvM, TvS> meshBuilder, Mesh srcMesh, Converter<Material, TMaterial> materialFunc)
            where TvG : struct, IVertexGeometry
            where TvM : struct, IVertexMaterial
            where TvS : struct, IVertexSkinning
        {
            Guard.NotNull(meshBuilder, nameof(meshBuilder));
            Guard.NotNull(materialFunc, nameof(materialFunc));

            if (srcMesh == null) return;

            var materialMap = srcMesh.Primitives
                .Select(prim => prim.Material)
                .Distinct()
                .ToDictionary(mat => mat, mat => materialFunc(mat));

            Material currMat = null;
            PrimitiveBuilder<TMaterial, TvG, TvM, TvS> currPrim = null;

            foreach (var tri in srcMesh.EvaluateTriangles<TvG, TvM, TvS>())
            {
                if (currMat != tri.Material)
                {
                    currMat = tri.Material;
                    currPrim = meshBuilder.UsePrimitive(materialMap[currMat]);
                }

                currPrim.AddTriangle(tri.A, tri.B, tri.C);
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
        public static MeshBuilder<TMaterial, TvG, TvM, VertexEmpty> ToStaticMeshBuilder<TMaterial, TvG, TvM>(this Scene srcScene, Converter<Material, TMaterial> materialFunc, Animation animation, float time)
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

                mesh.UsePrimitive(material).AddTriangle(tri.A, tri.B, tri.C);
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

        public static MeshBuilder<Materials.MaterialBuilder, TvG, TvM, TvS> ToMeshBuilder<TMaterial, TvG, TvM, TvS>(this IEnumerable<(VertexBuilder<TvG, TvM, TvS> A, VertexBuilder<TvG, TvM, TvS> B, VertexBuilder<TvG, TvM, TvS> C, TMaterial Material)> triangles, Converter<TMaterial, Materials.MaterialBuilder> materialFunc)
            where TvG : struct, IVertexGeometry
            where TvM : struct, IVertexMaterial
            where TvS : struct, IVertexSkinning
        {
            var mats = new Dictionary<TMaterial, Materials.MaterialBuilder>();

            Materials.MaterialBuilder useMaterial(TMaterial srcMat)
            {
                if (mats.TryGetValue(srcMat, out Materials.MaterialBuilder dstMat)) return dstMat;

                mats[srcMat] = dstMat = materialFunc(srcMat);

                return dstMat;
            }

            var m = new MeshBuilder<Materials.MaterialBuilder, TvG, TvM, TvS>();

            foreach (var tri in triangles)
            {
                var prim = m.UsePrimitive(useMaterial(tri.Material));

                prim.AddTriangle(tri.A, tri.B, tri.C);
            }

            return m;
        }

        private static void AddPrimitiveGeometry(this IPrimitiveBuilder dstPrim, MeshPrimitive srcPrim)
        {
            Guard.NotNull(dstPrim, nameof(dstPrim));

            var vertices = srcPrim.GetVertexColumns();
            var vmap = new Dictionary<int, int>();

            foreach (var srcPoint in srcPrim.GetPointIndices())
            {
                var vrt = vertices.GetVertex(dstPrim.VertexType, srcPoint);

                var idx = dstPrim.AddPoint(vrt);

                vmap[srcPoint] = idx;
            }

            foreach (var (srcA, srcB) in srcPrim.GetLineIndices())
            {
                var vrtA = vertices.GetVertex(dstPrim.VertexType, srcA);
                var vrtB = vertices.GetVertex(dstPrim.VertexType, srcB);

                var (idxA, idxB) = dstPrim.AddLine(vrtA, vrtB);

                vmap[srcA] = idxA;
                vmap[srcB] = idxB;
            }

            foreach (var (srcA, srcB, srcC) in srcPrim.GetTriangleIndices())
            {
                var vrtA = vertices.GetVertex(dstPrim.VertexType, srcA);
                var vrtB = vertices.GetVertex(dstPrim.VertexType, srcB);
                var vrtC = vertices.GetVertex(dstPrim.VertexType, srcC);

                var (idxA, idxB, idxC) = dstPrim.AddTriangle(vrtA, vrtB, vrtC);

                vmap[srcA] = idxA;
                vmap[srcB] = idxB;
                vmap[srcC] = idxC;
            }

            for (int tidx = 0; tidx < vertices.MorphTargets.Count; ++tidx)
            {
                var srcTarget = vertices.MorphTargets[tidx];

                foreach (var kvp in vmap)
                {
                    if (kvp.Value < 0) continue;

                    var v = srcTarget.GetVertex(dstPrim.VertexType, kvp.Key);

                    dstPrim.SetVertexDelta(tidx, kvp.Value, new VertexGeometryDelta(v.GetGeometry()) );
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
