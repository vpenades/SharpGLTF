using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;

using SharpGLTF.Geometry.VertexTypes;

namespace SharpGLTF.Geometry
{
    /// <summary>
    /// Represents an utility class to help build meshes by adding primitives associated with a given material.
    /// </summary>
    /// <typeparam name="TMaterial">The material type used by this <see cref="PrimitiveBuilder{TMaterial, TvP, TvM, TvS}"/> instance.</typeparam>
    public interface IMeshBuilder<TMaterial>
    {
        /// <summary>
        /// Gets or sets the display text name, or null.
        /// <para><b>⚠️ DO NOT USE AS AN OBJECT ID ⚠️</b> see remarks.</para>
        /// </summary>
        /// <remarks>
        /// glTF does not define any rule for object names.<br/>
        /// This means that names can be null or non unique.<br/>
        /// So don't use <see cref="Name"/> for anything other than object name display.<br/>
        /// If you need to reference objects by some ID, use lookup tables instead.
        /// </remarks>
        string Name { get; set; }

        /// <summary>
        /// Gets or sets the custom data of this object.
        /// </summary>
        IO.JsonContent Extras { get; set; }

        /// <summary>
        /// Gets a value indicating whether this mesh does not contain any geometry.
        /// </summary>
        Boolean IsEmpty { get; }

        /// <summary>
        /// Gets a collection of materials used by this mesh.
        /// </summary>
        /// <remarks>
        /// The materials are taken directly from the <see cref="Primitives"/>.
        /// </remarks>
        IEnumerable<TMaterial> Materials { get; }

        /// <summary>
        /// Gets a collection of primitives used by this mesh.
        /// </summary>
        IReadOnlyCollection<IPrimitiveReader<TMaterial>> Primitives { get; }

        /// <summary>
        /// Creates, or uses an existing primitive using <paramref name="material"/>.
        /// </summary>
        /// <param name="material">The material used by the primitive.</param>
        /// <param name="primitiveVertexCount">
        /// Defines the primitive type.<br/>
        /// <list type="number">
        /// <item>Points</item>
        /// <item>Lines</item>
        /// <item>Triangles (Default)</item>
        /// </list>
        /// </param>
        /// <returns>An instance of <see cref="IPrimitiveBuilder"/>.</returns>
        IPrimitiveBuilder UsePrimitive(TMaterial material, int primitiveVertexCount = 3);

        IMorphTargetBuilder UseMorphTarget(int index);

        IMeshBuilder<TMaterial> Clone(Func<TMaterial, TMaterial> materialCloneCallback = null);

        void Validate();
    }

    static class MeshBuilderToolkit
    {
        public static VertexBuilder<VertexGeometryDelta, VertexMaterialDelta, VertexEmpty>[] GetMorphTargetVertices(this IPrimitiveMorphTargetReader morphTarget, int vertexCount)
        {
            var c = new VertexBuilder<VertexGeometryDelta, VertexMaterialDelta, VertexEmpty>[vertexCount];

            for (int i = 0; i < vertexCount; ++i)
            {
                c[i] = morphTarget.GetVertexDelta(i);
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
                .Select(item => item.GetSkinning().GetBindings().MaxIndex);

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
            if (primitive == null) return true;
            if (primitive.Points.Count > 0) return false;
            if (primitive.Lines.Count > 0) return false;
            if (primitive.Triangles.Count > 0) return false;
            return true;
        }

        public static bool IsEmpty<TMaterial>(this IMeshBuilder<TMaterial> mesh)
        {
            if (mesh == null) return true;
            if (mesh.Primitives.Count == 0) return true;
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
