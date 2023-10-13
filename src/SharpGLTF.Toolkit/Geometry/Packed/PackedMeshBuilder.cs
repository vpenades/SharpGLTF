using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;

using SharpGLTF.Schema2;

using JSONEXTRAS = System.Text.Json.Nodes.JsonNode;

namespace SharpGLTF.Geometry
{
    /// <summary>
    /// Used internally to convert a <see cref="MeshBuilder{TMaterial, TvP, TvM, TvS}"/>
    /// to <see cref="Schema2.Mesh"/>.
    /// </summary>
    /// <typeparam name="TMaterial">A material key to split primitives by material.</typeparam>
    class PackedMeshBuilder<TMaterial> : BaseBuilder
    {
        #region lifecycle

        /// <summary>
        /// Converts a collection of <see cref="IMeshBuilder{TMaterial}"/> meshes into a collection of <see cref="PackedMeshBuilder{TMaterial}"/> meshes,
        /// ensuring that the resources are shared across all meshes.
        /// </summary>
        /// <param name="meshBuilders">A collection of <see cref="IMeshBuilder{TMaterial}"/> meshes.</param>
        /// <param name="settings">Mesh packaging settings.</param>
        /// <returns>A collectio of <see cref="PackedMeshBuilder{TMaterial}"/> meshes.</returns>
        internal static IEnumerable<PackedMeshBuilder<TMaterial>> CreatePackedMeshes(IEnumerable<IMeshBuilder<TMaterial>> meshBuilders, Scenes.SceneBuilderSchema2Settings settings)
        {
            try
            {
                foreach (var m in meshBuilders) m.Validate();
            }
            catch (Exception ex)
            {
                throw new ArgumentException(ex.Message, nameof(meshBuilders), ex);
            }

            var vertexEncodings = new PackedEncoding();
            vertexEncodings.JointsEncoding = meshBuilders.GetOptimalJointEncoding();
            vertexEncodings.WeightsEncoding = settings.CompactVertexWeights ? EncodingType.UNSIGNED_SHORT : EncodingType.FLOAT;

            var indexEncoding = meshBuilders.GetOptimalIndexEncoding();

            foreach (var srcMesh in meshBuilders)
            {
                // Gather all the primitives of the mesh

                var srcPrims = srcMesh
                    .Primitives
                    .Where(item => item.Vertices.Count > 0)
                    .ToList();                

                // identify morph target attributes in use                

                var morphTargetsAttributes = new HashSet<string>();

                foreach (var srcPrim in srcPrims)
                {
                    srcPrim._GatherMorphTargetAttributes(morphTargetsAttributes);                    
                }

                // adjust vertex encoding

                if (morphTargetsAttributes.Count > 0)
                {
                    // if any primitive has morph targets, it is better not to use strided vertex buffers.
                    settings.UseStridedBuffers = false;
                }

                bool hasColorMorph = morphTargetsAttributes.Contains("COLOR_0DELTA")
                    || morphTargetsAttributes.Contains("COLOR_1DELTA")
                    || morphTargetsAttributes.Contains("COLOR_2DELTA")
                    || morphTargetsAttributes.Contains("COLOR_3DELTA");

                // if any primitive has color morphing, we need to ensure the vertex
                // color attribute encoding is FLOAT to allow negative delta values.

                vertexEncodings.ColorEncoding = hasColorMorph
                    ? EncodingType.FLOAT
                    : (EncodingType?)null;

                // Create a packed mesh

                var dstMesh = new PackedMeshBuilder<TMaterial>(srcMesh.Name, srcMesh.Extras);

                foreach (var srcPrim in srcPrims)
                {
                    var dstPrim = dstMesh.AddPrimitive(srcPrim.Material, srcPrim.VerticesPerPrimitive);

                    if (settings.UseStridedBuffers) dstPrim.SetStridedVertices(srcPrim, vertexEncodings);
                    else dstPrim.SetStreamedVertices(srcPrim, vertexEncodings);

                    dstPrim.SetIndices(srcPrim, indexEncoding);

                    if (morphTargetsAttributes.Count > 0) dstPrim.SetMorphTargets(srcPrim, vertexEncodings, morphTargetsAttributes);
                }

                yield return dstMesh;
            }
        }

        private PackedMeshBuilder(string name, JSONEXTRAS extras)
            : base(name, extras) { }

        #endregion

        #region data

        private readonly List<PackedPrimitiveBuilder<TMaterial>> _Primitives = new List<PackedPrimitiveBuilder<TMaterial>>();

        #endregion

        #region API

        public PackedPrimitiveBuilder<TMaterial> AddPrimitive(TMaterial material, int primitiveVertexCount)
        {
            var p = new PackedPrimitiveBuilder<TMaterial>(material, primitiveVertexCount);
            _Primitives.Add(p);

            return p;
        }

        public Mesh CreateSchema2Mesh(ModelRoot root, Converter<TMaterial, Material> materialEvaluator)
        {
            if (_Primitives.Count == 0) return null;

            var dstMesh = root.CreateMesh();
            this.TryCopyNameAndExtrasTo(dstMesh);

            foreach (var p in _Primitives)
            {
                p.CopyToMesh(dstMesh, materialEvaluator);
            }

            dstMesh.SetMorphWeights(null);

            return dstMesh;
        }

        public static void MergeBuffers(IEnumerable<PackedMeshBuilder<TMaterial>> meshes)
        {
            PackedPrimitiveBuilder<TMaterial>.MergeBuffers(meshes.SelectMany(m => m._Primitives));
        }

        #endregion
    }
}
