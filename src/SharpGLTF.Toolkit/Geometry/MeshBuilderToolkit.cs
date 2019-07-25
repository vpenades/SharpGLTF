using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SharpGLTF.Geometry
{
    public interface IMeshBuilder<TMaterial>
    {
        string Name { get; set; }

        IEnumerable<TMaterial> Materials { get; }

        IReadOnlyCollection<IPrimitive<TMaterial>> Primitives { get; }

        IPrimitiveBuilder UsePrimitive(TMaterial material, int primitiveVertexCount = 3);

        void Validate();
    }

    static class MeshBuilderToolkit
    {
        public static IMeshBuilder<TMaterial> CreateMeshBuilderFromVertexAttributes<TMaterial>(params string[] vertexAttributes)
        {
            Type meshType = GetMeshBuilderType(typeof(TMaterial), vertexAttributes);

            var mesh = Activator.CreateInstance(meshType, string.Empty);

            return mesh as IMeshBuilder<TMaterial>;
        }

        public static Type GetMeshBuilderType(Type materialType, string[] vertexAttributes)
        {
            var tvg = VertexTypes.VertexUtils.GetVertexGeometryType(vertexAttributes);
            var tvm = VertexTypes.VertexUtils.GetVertexMaterialType(vertexAttributes);
            var tvs = VertexTypes.VertexUtils.GetVertexSkinningType(vertexAttributes);

            var meshType = typeof(MeshBuilder<,,,>);

            meshType = meshType.MakeGenericType(materialType, tvg, tvm, tvs);
            return meshType;
        }
    }
}
