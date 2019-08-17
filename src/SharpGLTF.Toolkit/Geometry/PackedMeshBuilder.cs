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
        /// <param name="meshBuilders">A collection of <see cref="MeshBuilder{TMaterial, TvP, TvM, TvS}"/> instances.</param>
        /// <returns>A collection of <see cref="PackedMeshBuilder{TMaterial}"/> instances.</returns>
        internal static IEnumerable<PackedMeshBuilder<TMaterial>> PackMeshes(IEnumerable<IMeshBuilder<TMaterial>> meshBuilders)
        {
            try
            {
                foreach (var m in meshBuilders) m.Validate();
            }
            catch (Exception ex)
            {
                throw new ArgumentException(ex.Message, nameof(meshBuilders), ex);
            }

            var meshPrimitives = meshBuilders
                .SelectMany(item => item.Primitives)
                .Where(item => !item.IsEmpty());

            if (!meshPrimitives.Any())
            {
                foreach (var mb in meshBuilders) yield return new PackedMeshBuilder<TMaterial>(mb.Name);
                yield break;
            }

            var vertexBlocks = VertexTypes.VertexUtils
                .CreateVertexMemoryAccessors( meshPrimitives.Select(item => item.Vertices) )
                .ToList();

            var indexBlocks = VertexTypes.VertexUtils
                .CreateIndexMemoryAccessors( meshPrimitives.Select(item => item.Indices) )
                .ToList();

            if (vertexBlocks.Count != indexBlocks.Count) throw new InvalidOperationException("Vertex and index blocks count mismatch");

            int idx = 0;

            foreach (var meshBuilder in meshBuilders)
            {
                var dstMesh = new PackedMeshBuilder<TMaterial>(meshBuilder.Name);

                foreach (var primitiveBuilder in meshBuilder.Primitives)
                {
                    if (primitiveBuilder.IsEmpty()) continue;

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
