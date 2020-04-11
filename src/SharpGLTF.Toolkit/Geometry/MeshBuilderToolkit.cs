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

        IMorphTargetBuilder UseMorphTarget(int index);

        IMeshBuilder<TMaterial> Clone(Func<TMaterial, TMaterial> materialCloneCallback = null);

        void Validate();
    }

    static class MeshBuilderToolkit
    {
        public static VertexBuilder<VertexGeometryDelta, VertexEmpty, VertexEmpty>[] GetMorphTargetVertices(this IPrimitiveMorphTargetReader morphTarget, int vertexCount)
        {
            var c = new VertexBuilder<VertexGeometryDelta, VertexEmpty, VertexEmpty>[vertexCount];

            for (int i = 0; i < vertexCount; ++i)
            {
                var delta = morphTarget.GetVertexDelta(i);

                c[i] = new VertexBuilder<VertexGeometryDelta, VertexEmpty, VertexEmpty>(delta);
            }

            return c;
        }

        public static Schema2.EncodingType GetOptimalIndexEncoding<TMaterial>(this IEnumerable<IMeshBuilder<TMaterial>> meshes)
        {
            var indices = meshes
                .SelectMany(item => item.Primitives)
                .Where(item => item.VerticesPerPrimitive >= 2) // points will never use index buffers
                .SelectMany(prim => prim.GetIndices());

            var maxIndex = indices.Any() ? indices.Max() : 0;

            return maxIndex < 65535 ? Schema2.EncodingType.UNSIGNED_SHORT : Schema2.EncodingType.UNSIGNED_INT;
        }

        public static Schema2.EncodingType GetOptimalJointEncoding<TMaterial>(this IEnumerable<IMeshBuilder<TMaterial>> meshes)
        {
            var indices = meshes
                .SelectMany(item => item.Primitives)
                .SelectMany(item => item.Vertices)
                .Select(item => item.GetSkinning().GetWeights().MaxIndex);

            var maxIndex = indices.Any() ? indices.Max() : 0;

            return maxIndex < 256 ? Schema2.EncodingType.UNSIGNED_BYTE : Schema2.EncodingType.UNSIGNED_SHORT;
        }

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
                    var a = prim.Vertices[tri.A].GetGeometry().GetPosition();
                    var b = prim.Vertices[tri.B].GetGeometry().GetPosition();
                    var c = prim.Vertices[tri.C].GetGeometry().GetPosition();
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

        public static bool IsEmpty<TMaterial>(this IPrimitiveReader<TMaterial> primitive)
        {
            if (primitive.Points.Count > 0) return false;
            if (primitive.Lines.Count > 0) return false;
            if (primitive.Triangles.Count > 0) return false;
            return true;
        }

        public static bool IsEmpty<TMaterial>(this IMeshBuilder<TMaterial> mesh)
        {
            return mesh.Primitives.All(prim => prim.IsEmpty());
        }

        /// <summary>
        /// Given a set of 4 points defining a quadrangle, it determines which
        /// is the optimal diagonal to choose to reprensent the quadrangle as two triangles.
        /// </summary>
        /// <param name="a">The first vertex.</param>
        /// <param name="b">The second vertex.</param>
        /// <param name="c">The third vertex.</param>
        /// <param name="d">The fourth vertex.</param>
        /// <returns>True if two triangles are (A,B,C),(A,C,D) , false if two triangles are (B,C,D),(B,D,A).</returns>
        public static bool GetQuadrangleDiagonal(Vector3 a, Vector3 b, Vector3 c, Vector3 d)
        {
            var area1 = Vector3.Cross(a - b, c - b).Length() + Vector3.Cross(a - d, c - d).Length();
            var area2 = Vector3.Cross(b - a, d - a).Length() + Vector3.Cross(b - c, d - c).Length();

            return area1 <= area2;
        }
    }
}
