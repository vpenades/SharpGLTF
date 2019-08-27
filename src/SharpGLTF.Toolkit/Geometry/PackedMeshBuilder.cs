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

        internal static IEnumerable<PackedMeshBuilder<TMaterial>> CreatePackedMeshes(IEnumerable<IMeshBuilder<TMaterial>> meshBuilders, bool prefferStrided)
        {
            try
            {
                foreach (var m in meshBuilders) m.Validate();
            }
            catch (Exception ex)
            {
                throw new ArgumentException(ex.Message, nameof(meshBuilders), ex);
            }

            var indexEncoding = meshBuilders.GetOptimalIndexEncoding();

            foreach (var srcMesh in meshBuilders)
            {
                var dstMesh = new PackedMeshBuilder<TMaterial>(srcMesh.Name);

                foreach (var srcPrim in srcMesh.Primitives)
                {
                    if (srcPrim.Vertices.Count == 0) continue;

                    var dstPrim = dstMesh.AddPrimitive(srcPrim.Material, srcPrim.VerticesPerPrimitive);

                    bool useStrided = prefferStrided;
                    if (srcPrim.MorphTargets.TargetsCount > 0) useStrided = false;

                    if (useStrided) dstPrim.SetStridedVertices(srcPrim);
                    else dstPrim.SetStreamedVertices(srcPrim);

                    dstPrim.SetIndices(srcPrim, indexEncoding);
                    dstPrim.SetMorphTargets(srcPrim);
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

        public PackedPrimitiveBuilder<TMaterial> AddPrimitive(TMaterial material, int primitiveVertexCount)
        {
            var p = new PackedPrimitiveBuilder<TMaterial>(material, primitiveVertexCount);
            _Primitives.Add(p);

            return p;
        }

        public Mesh CreateSchema2Mesh(ModelRoot root, Func<TMaterial, Material> materialEvaluator)
        {
            if (_Primitives.Count == 0) return null;

            var dstMesh = root.CreateMesh(_MeshName);

            foreach (var p in _Primitives)
            {
                p.CopyToMesh(dstMesh, materialEvaluator);
            }

            // TODO: set default morph target weights.

            return dstMesh;
        }

        public static void MergeBuffers(IEnumerable<PackedMeshBuilder<TMaterial>> meshes)
        {
            PackedPrimitiveBuilder<TMaterial>.MergeBuffers(meshes.SelectMany(m => m._Primitives));
        }

        #endregion
    }
}
