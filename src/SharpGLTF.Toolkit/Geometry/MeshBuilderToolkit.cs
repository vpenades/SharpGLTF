using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;

using SharpGLTF.Geometry.VertexTypes;

namespace SharpGLTF.Geometry
{
    public interface IMeshBuilder<TMaterial>
    {
        string Name { get; set; }

        IEnumerable<TMaterial> Materials { get; }

        IReadOnlyCollection<IPrimitiveReader<TMaterial>> Primitives { get; }

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
            var tvg = VertexUtils.GetVertexGeometryType(vertexAttributes);
            var tvm = VertexUtils.GetVertexMaterialType(vertexAttributes);
            var tvs = VertexUtils.GetVertexSkinningType(vertexAttributes);

            var meshType = typeof(MeshBuilder<,,,>);

            meshType = meshType.MakeGenericType(materialType, tvg, tvm, tvs);
            return meshType;
        }

        public static IReadOnlyDictionary<Vector3, Vector3> CalculateSmoothNormals<TMaterial>(this IMeshBuilder<TMaterial> srcMesh)
        {
            var posnrm = new Dictionary<Vector3, Vector3>();

            void addDirection(Dictionary<Vector3, Vector3> dict, Vector3 pos, Vector3 dir)
            {
                if (!dir._IsFinite()) return;
                if (!dict.TryGetValue(pos, out Vector3 n)) n = Vector3.Zero;
                dict[pos] = n + dir;
            }

            foreach (var prim in srcMesh.Primitives)
            {
                foreach (var tri in prim.Triangles)
                {
                    var a = prim.Vertices[tri.Item1].GetGeometry().GetPosition();
                    var b = prim.Vertices[tri.Item1].GetGeometry().GetPosition();
                    var c = prim.Vertices[tri.Item1].GetGeometry().GetPosition();
                    var d = Vector3.Cross(b - a, c - a);
                    addDirection(posnrm, a, d);
                    addDirection(posnrm, b, d);
                    addDirection(posnrm, c, d);
                }
            }

            foreach (var pos in posnrm.Keys.ToList())
            {
                posnrm[pos] = Vector3.Normalize(posnrm[pos]);
            }

            return posnrm;
        }
    }
}
