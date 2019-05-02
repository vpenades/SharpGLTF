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

        /// <summary>
        /// Converts a collection of <see cref="MeshBuilder{TMaterial, TvP, TvM, TvS}"/>
        /// to a collection of <see cref="PackedMeshBuilder{TMaterial}"/>, trying to use
        /// a single vertex buffer and a single index buffer shared by all meshes.
        /// </summary>
        /// <typeparam name="TvP">The vertex fragment type with Position, Normal and Tangent.</typeparam>
        /// <typeparam name="TvM">The vertex fragment type with Colors and Texture Coordinates.</typeparam>
        /// <typeparam name="TvS">The vertex fragment type with Skin Joint Weights.</typeparam>
        /// <param name="meshBuilders">A collection of <see cref="MeshBuilder{TMaterial, TvP, TvM, TvS}"/> instances.</param>
        /// <returns>A collection of <see cref="PackedMeshBuilder{TMaterial}"/> instances.</returns>
        internal static IEnumerable<PackedMeshBuilder<TMaterial>> PackMeshes<TvP, TvM, TvS>(IEnumerable<MeshBuilder<TMaterial, TvP, TvM, TvS>> meshBuilders)
            where TvP : struct, VertexTypes.IVertexGeometry
            where TvM : struct, VertexTypes.IVertexMaterial
            where TvS : struct, VertexTypes.IVertexSkinning
        {
            try
            {
                foreach (var m in meshBuilders) m.Validate();
            }
            catch (Exception ex)
            {
                throw new ArgumentException(ex.Message, nameof(meshBuilders), ex);
            }

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
                    dstMesh.AddPrimitive(primitiveBuilder.Material, primitiveBuilder.VerticesPerPrimitive, vertexBlocks[idx], indexBlocks[idx]);

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

        public void AddPrimitive(TMaterial material, int primitiveVertexCount, Memory.MemoryAccessor[] vrtAccessors, Memory.MemoryAccessor idxAccessor)
        {
            var p = new PackedPrimitiveBuilder<TMaterial>(material, primitiveVertexCount, vrtAccessors, idxAccessor);
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

        internal PackedPrimitiveBuilder(TMaterial material, int primitiveVertexCount, Memory.MemoryAccessor[] vrtAccessors, Memory.MemoryAccessor idxAccessor)
        {
            _Material = material;
            _VerticesPerPrimitive = primitiveVertexCount;
            _VertexAccessors = vrtAccessors;
            _IndexAccessors = idxAccessor;
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
