using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;

namespace SharpGLTF.Geometry
{
    using Schema2;

    class PackedMeshBuilder<TMaterial>
    {
        #region lifecycle

        internal static IEnumerable<PackedMeshBuilder<TMaterial>> PackMeshes<TVertex, TJoints>(IEnumerable<SkinnedMeshBuilder<TMaterial, TVertex, TJoints>> meshBuilders)
            where TVertex : struct, VertexTypes.IVertex
            where TJoints : struct, VertexTypes.IJoints
        {
            var vertexBlocks = VertexTypes.VertexUtils.CreateVertexMemoryAccessors
                (
                meshBuilders
                .SelectMany(item => item.Primitives)
                .Select(item => item.Vertices)
                ).ToList();

            var indexBlocks = VertexTypes.VertexUtils.CreateIndexMemoryAccessors
                (
                meshBuilders
                .SelectMany(item => item.Primitives)
                .Select(item => item.Indices)
                ).ToList();

            int idx = 0;

            foreach (var meshBuilder in meshBuilders)
            {
                var dstMesh = new PackedMeshBuilder<TMaterial>(meshBuilder.Name);

                foreach (var primitiveBuilder in meshBuilder.Primitives)
                {
                    dstMesh.AddPrimitive(primitiveBuilder.Material, vertexBlocks[idx], indexBlocks[idx]);

                    ++idx;
                }

                yield return dstMesh;
            }
        }

        private PackedMeshBuilder(string name) { _MeshName = name; }

        #endregion

        #region data

        private readonly string _MeshName;
        private readonly List<PackedPrimitiveBuilder<TMaterial>> _Primitives = new List<PackedPrimitiveBuilder<TMaterial>>();

        #endregion

        #region API

        public void AddPrimitive(TMaterial material, Memory.MemoryAccessor[] vrtAccessors, Memory.MemoryAccessor idxAccessor)
        {
            var p = new PackedPrimitiveBuilder<TMaterial>(material, vrtAccessors, idxAccessor);
            _Primitives.Add(p);
        }

        public Mesh CreateSchema2Mesh(ModelRoot root, Func<TMaterial, Material> materialEvaluator)
        {
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

        internal PackedPrimitiveBuilder(TMaterial material, Memory.MemoryAccessor[] vrtAccessors, Memory.MemoryAccessor idxAccessor)
        {
            _Material = material;
            _VertexAccessors = vrtAccessors;
            _IndexAccessors = idxAccessor;
        }

        #endregion

        #region data

        private readonly TMaterial _Material;
        private readonly Memory.MemoryAccessor[] _VertexAccessors;
        private readonly Memory.MemoryAccessor _IndexAccessors;

        #endregion

        #region API

        internal void CopyToMesh(Mesh dstMesh, Func<TMaterial, Material> materialEvaluator)
        {
            dstMesh.CreatePrimitive()
                        .WithMaterial(materialEvaluator(_Material))
                        .WithVertexAccessors(_VertexAccessors)
                        .WithIndicesAccessor(PrimitiveType.TRIANGLES, _IndexAccessors);
        }

        #endregion
    }
}
