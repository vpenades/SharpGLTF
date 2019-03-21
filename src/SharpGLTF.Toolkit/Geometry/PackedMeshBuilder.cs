using System;
using System.Collections.Generic;
using System.Text;

namespace SharpGLTF.Geometry
{
    using Schema2;

    class PackedMeshBuilder<TMaterial>
    {
        public PackedMeshBuilder(string name) { _MeshName = name; }

        private readonly string _MeshName;
        private readonly List<PackedPrimitiveBuilder<TMaterial>> _Primitives = new List<PackedPrimitiveBuilder<TMaterial>>();

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
    }

    class PackedPrimitiveBuilder<TMaterial>
    {
        internal PackedPrimitiveBuilder(TMaterial material, Memory.MemoryAccessor[] vrtAccessors, Memory.MemoryAccessor idxAccessor)
        {
            _Material = material;
            _VertexAccessors = vrtAccessors;
            _IndexAccessors = idxAccessor;
        }

        private readonly TMaterial _Material;
        private readonly Memory.MemoryAccessor[] _VertexAccessors;
        private readonly Memory.MemoryAccessor _IndexAccessors;

        internal void CopyToMesh(Mesh dstMesh, Func<TMaterial, Material> materialEvaluator)
        {
            dstMesh.CreatePrimitive()
                        .WithMaterial(materialEvaluator(_Material))
                        .WithVertexAccessors(_VertexAccessors)
                        .WithIndicesAccessor(PrimitiveType.TRIANGLES, _IndexAccessors);
        }
    }
}
