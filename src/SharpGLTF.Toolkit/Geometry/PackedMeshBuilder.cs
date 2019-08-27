using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;

using SharpGLTF.Schema2;

namespace SharpGLTF.Geometry
{
    /// <summary>
    /// Used internally to convert a <see cref="MeshBuilder{TMaterial, TvP, TvM, TvS}"/>
    /// to <see cref="Schema2.Mesh"/>.
    /// </summary>
    /// <typeparam name="TMaterial">A material key to split primitives by material.</typeparam>
    class PackedMeshBuilder<TMaterial>
    {
        #region lifecycle

        internal static IEnumerable<PackedMeshBuilder<TMaterial>> PackMeshesColumnVertices(IEnumerable<IMeshBuilder<TMaterial>> meshBuilders)
        {
            try
            {
                foreach (var m in meshBuilders) m.Validate();
            }
            catch (Exception ex)
            {
                throw new ArgumentException(ex.Message, nameof(meshBuilders), ex);
            }

            var vertexBuffers = new Dictionary<string, PackedBuffer>();
            var indexBuffer = new PackedBuffer();
            var indexEncoding = meshBuilders.GetOptimalIndexEncoding();

            var dstMeshes = new List<PackedMeshBuilder<TMaterial>>();

            foreach (var srcMesh in meshBuilders)
            {
                var dstMesh = new PackedMeshBuilder<TMaterial>(srcMesh.Name);

                foreach (var srcPrim in srcMesh.Primitives)
                {
                    if (srcPrim.Vertices.Count == 0) continue;

                    var attributeNames = VertexTypes.VertexUtils
                        .GetVertexAttributes(srcPrim.Vertices[0], srcPrim.Vertices.Count)
                        .Select(item => item.Name)
                        .ToList();

                    var vAccessors = new List<Memory.MemoryAccessor>();

                    foreach (var an in attributeNames)
                    {
                        var vAccessor = VertexTypes.VertexUtils.CreateVertexMemoryAccessors(srcPrim.Vertices, an);
                        if (vAccessor == null) continue;

                        vAccessors.Add(vAccessor);

                        if (!vertexBuffers.TryGetValue(an, out PackedBuffer packed))
                        {
                            vertexBuffers[an] = packed = new PackedBuffer();
                        }

                        packed.AddAccessors(vAccessor);
                    }

                    var iAccessor = VertexTypes.VertexUtils.CreateIndexMemoryAccessor(srcPrim.GetIndices(), indexEncoding);
                    if (iAccessor != null) indexBuffer.AddAccessors(iAccessor);

                    dstMesh.AddPrimitive(srcPrim.Material, srcPrim.VerticesPerPrimitive, vAccessors.ToArray(), iAccessor);
                }

                dstMeshes.Add(dstMesh);
            }

            foreach (var vb in vertexBuffers.Values) vb.MergeBuffers();
            indexBuffer.MergeBuffers();

            return dstMeshes;
        }

        internal static IEnumerable<PackedMeshBuilder<TMaterial>> PackMeshesRowVertices(IEnumerable<IMeshBuilder<TMaterial>> meshBuilders)
        {
            try
            {
                foreach (var m in meshBuilders) m.Validate();
            }
            catch (Exception ex)
            {
                throw new ArgumentException(ex.Message, nameof(meshBuilders), ex);
            }

            var vertexBuffer = new PackedBuffer();
            var indexBuffer = new PackedBuffer();
            var indexEncoding = meshBuilders.GetOptimalIndexEncoding();

            var dstMeshes = new List<PackedMeshBuilder<TMaterial>>();

            foreach (var srcMesh in meshBuilders)
            {
                var dstMesh = new PackedMeshBuilder<TMaterial>(srcMesh.Name);

                foreach (var srcPrim in srcMesh.Primitives)
                {
                    var vAccessors = VertexTypes.VertexUtils.CreateVertexMemoryAccessors(srcPrim.Vertices);
                    if (vAccessors == null) continue;
                    vertexBuffer.AddAccessors(vAccessors);

                    var iAccessor = VertexTypes.VertexUtils.CreateIndexMemoryAccessor(srcPrim.GetIndices(), indexEncoding);
                    if (iAccessor != null) indexBuffer.AddAccessors(iAccessor);

                    dstMesh.AddPrimitive(srcPrim.Material, srcPrim.VerticesPerPrimitive, vAccessors, iAccessor);
                }

                dstMeshes.Add(dstMesh);
            }

            vertexBuffer.MergeBuffers();
            indexBuffer.MergeBuffers();

            return dstMeshes;
        }

        private PackedMeshBuilder(string name) { _MeshName = name; }

        #endregion

        #region data

        private readonly string _MeshName;

        private readonly List<PackedPrimitiveBuilder<TMaterial>> _Primitives = new List<PackedPrimitiveBuilder<TMaterial>>();

        #endregion

        #region API

        public void AddPrimitive(TMaterial material, int primitiveVertexCount, Memory.MemoryAccessor[] vrtAccessors, Memory.MemoryAccessor idxAccessor)
        {
            var p = new PackedPrimitiveBuilder<TMaterial>(material, primitiveVertexCount, vrtAccessors, idxAccessor);
            _Primitives.Add(p);
        }

        public Mesh CreateSchema2Mesh(ModelRoot root, Func<TMaterial, Material> materialEvaluator)
        {
            if (_Primitives.Count == 0) return null;

            var dstMesh = root.CreateMesh(_MeshName);

            foreach (var p in _Primitives)
            {
                p.CopyToMesh(dstMesh, materialEvaluator);
            }

            return dstMesh;
        }

        #endregion
    }

    class PackedPrimitiveBuilder<TMaterial>
    {
        #region lifecycle

        internal PackedPrimitiveBuilder(TMaterial material, int primitiveVertexCount, Memory.MemoryAccessor[] vrtAccessors, Memory.MemoryAccessor idxAccessor)
        {
            Guard.MustBeBetweenOrEqualTo(primitiveVertexCount, 1, 3, nameof(primitiveVertexCount));

            Guard.NotNull(vrtAccessors, nameof(vrtAccessors));

            if (primitiveVertexCount == 1) Guard.MustBeNull(idxAccessor, nameof(idxAccessor));
            else                           Guard.NotNull(idxAccessor, nameof(idxAccessor));

            _Material = material;
            _VerticesPerPrimitive = primitiveVertexCount;
            _VertexAccessors = vrtAccessors;
            _IndexAccessors = idxAccessor; // indices can be null for points
        }

        #endregion

        #region data

        private readonly TMaterial _Material;
        private readonly int _VerticesPerPrimitive;

        private readonly Memory.MemoryAccessor[] _VertexAccessors;

        private readonly Memory.MemoryAccessor _IndexAccessors;

        #endregion

        #region API

        internal void CopyToMesh(Mesh dstMesh, Func<TMaterial, Material> materialEvaluator)
        {
            if (_VerticesPerPrimitive < 1 || _VerticesPerPrimitive > 3) return;

            if (_VerticesPerPrimitive == 1)
            {
                dstMesh.CreatePrimitive()
                        .WithMaterial(materialEvaluator(_Material))
                        .WithVertexAccessors(_VertexAccessors)
                        .WithIndicesAutomatic(PrimitiveType.POINTS);

                return;
            }

            var pt = PrimitiveType.LINES;
            if (_VerticesPerPrimitive == 3) pt = PrimitiveType.TRIANGLES;

            dstMesh.CreatePrimitive()
                        .WithMaterial(materialEvaluator(_Material))
                        .WithVertexAccessors(_VertexAccessors)
                        .WithIndicesAccessor(pt, _IndexAccessors);
        }

        #endregion
    }
}
