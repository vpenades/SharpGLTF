using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using SharpGLTF.Schema2;

using MACCESSOR = SharpGLTF.Memory.MemoryAccessor;

namespace SharpGLTF.Geometry
{
    sealed class PackedPrimitiveBuilder<TMaterial>
    {
        #region lifecycle

        public PackedPrimitiveBuilder(TMaterial material, int primitiveVertexCount)
        {
            Guard.MustBeBetweenOrEqualTo(primitiveVertexCount, 1, 3, nameof(primitiveVertexCount));

            _Material = material;
            _VerticesPerPrimitive = primitiveVertexCount;
        }

        #endregion

        #region data

        private readonly TMaterial _Material;
        private readonly int _VerticesPerPrimitive;

        private Type _StridedVertexType;

        private MACCESSOR[] _VertexAccessors;

        private MACCESSOR _IndexAccessors;

        private readonly List<MACCESSOR[]> _MorphTargets = new List<MACCESSOR[]>();

        #endregion

        #region API

        public void SetStridedVertices(IPrimitiveReader<TMaterial> srcPrim, PackedEncoding vertexEncoding)
        {
            Guard.NotNull(srcPrim, nameof(srcPrim));

            var vAccessors = VertexTypes.VertexUtils.CreateVertexMemoryAccessors(srcPrim.Vertices, vertexEncoding);

            Guard.NotNull(vAccessors, nameof(srcPrim));

            _StridedVertexType = srcPrim.VertexType;
            _VertexAccessors = vAccessors;
        }

        public void SetStreamedVertices(IPrimitiveReader<TMaterial> srcPrim, PackedEncoding vertexEncoding)
        {
            Guard.NotNull(srcPrim, nameof(srcPrim));

            var attributeNames = VertexTypes.VertexUtils
                        .GetVertexAttributes(srcPrim.Vertices[0], srcPrim.Vertices.Count, vertexEncoding)
                        .Select(item => item.Name)
                        .ToList();

            var vAccessors = new List<MACCESSOR>();
            GuardAll.MustBeEqualTo(vAccessors.Select(item => item.Attribute.ByteOffset), 0, nameof(vAccessors));
            GuardAll.MustBeEqualTo(vAccessors.Select(item => item.Attribute.ByteStride), 0, nameof(vAccessors));

            foreach (var an in attributeNames)
            {
                var vAccessor = VertexTypes.VertexUtils.CreateVertexMemoryAccessor(srcPrim.Vertices, an, vertexEncoding);
                if (vAccessor == null) continue;

                vAccessors.Add(vAccessor);
            }

            _VertexAccessors = vAccessors.ToArray();

            MACCESSOR.SanitizeVertexAttributes(_VertexAccessors);
        }

        public void SetIndices(IPrimitiveReader<TMaterial> srcPrim, EncodingType encoding)
        {
            Guard.NotNull(srcPrim, nameof(srcPrim));

            var iAccessor = VertexTypes.VertexUtils.CreateIndexMemoryAccessor(srcPrim.GetIndices(), encoding);

            if (_VerticesPerPrimitive == 1) Guard.MustBeNull(iAccessor, nameof(srcPrim));
            else Guard.NotNull(iAccessor, nameof(iAccessor));

            _IndexAccessors = iAccessor;
        }        

        public void SetMorphTargets(IPrimitiveReader<TMaterial> srcPrim, PackedEncoding vertexEncodings, ISet<string> morphTargetAttributes)
        {
            bool hasPositions = _VertexAccessors.Any(item => item.Attribute.Name == "POSITION");
            bool hasNormals = _VertexAccessors.Any(item => item.Attribute.Name == "NORMAL");
            bool hasTangents = _VertexAccessors.Any(item => item.Attribute.Name == "TANGENT");

            bool hasColors0 = _VertexAccessors.Any(item => item.Attribute.Name == "COLOR_0");
            bool hasColors1 = _VertexAccessors.Any(item => item.Attribute.Name == "COLOR_1");

            bool hasTexCoords0 = _VertexAccessors.Any(item => item.Attribute.Name == "TEXCOORD_0");
            bool hasTexCoords1 = _VertexAccessors.Any(item => item.Attribute.Name == "TEXCOORD_1");
            bool hasTexCoords2 = _VertexAccessors.Any(item => item.Attribute.Name == "TEXCOORD_2");
            bool hasTexCoords3 = _VertexAccessors.Any(item => item.Attribute.Name == "TEXCOORD_3");

            for (int i = 0; i < srcPrim.MorphTargets.Count; ++i)
            {
                var (pAccessor,nAccessor,tAccessor,c0Accessor, c1Accessor, uv0Accessor, uv1Accessor, uv2Accessor, uv3Accessor) = srcPrim._GetMorphTargetAccessors(i, vertexEncodings, morphTargetAttributes);

                if (!hasPositions) pAccessor = null;
                if (!hasNormals) nAccessor = null;
                if (!hasTangents) tAccessor = null;
                if (!hasColors0) c0Accessor = null;
                if (!hasColors1) c1Accessor = null;
                if (!hasTexCoords0) uv0Accessor = null;
                if (!hasTexCoords1) uv1Accessor = null;
                if (!hasTexCoords2) uv2Accessor = null;
                if (!hasTexCoords3) uv3Accessor = null;

                AddMorphTarget(pAccessor, nAccessor, tAccessor, c0Accessor, c1Accessor, uv0Accessor, uv1Accessor, uv2Accessor, uv3Accessor);
            }
        }

        private void AddMorphTarget(params MACCESSOR[] morphTarget)
        {
            MACCESSOR _removeDeltaSuffix(MACCESSOR accessor)
            {
                var name = accessor.Attribute.Name;
                if (!name.EndsWith("DELTA", StringComparison.Ordinal)) throw new InvalidOperationException();

                name = name.Replace("DELTA", string.Empty);

                var attr = accessor.Attribute;
                attr.Name = name;

                return new Memory.MemoryAccessor(accessor.Data, attr);
            }

            morphTarget = morphTarget
                .Where(item => item != null)
                .Select(item => _removeDeltaSuffix(item))
                .ToArray();

            _MorphTargets.Add(morphTarget);
        }        

        internal void CopyToMesh(Mesh dstMesh, Converter<TMaterial, Material> materialEvaluator)
        {
            if (_VerticesPerPrimitive < 1 || _VerticesPerPrimitive > 3) return;            

            // points
            if (_VerticesPerPrimitive == 1)
            {
                var dstPrim = dstMesh.CreatePrimitive()
                        .WithMaterial(materialEvaluator(_Material))
                        .WithVertexAccessors(_VertexAccessors)
                        .WithIndicesAutomatic(PrimitiveType.POINTS);

                CopyMorphTargets(dstPrim);

                return;
            }
            else // lines or triangles
            {
                var pt = PrimitiveType.LINES;
                if (_VerticesPerPrimitive == 3) pt = PrimitiveType.TRIANGLES;

                var dstPrim = dstMesh.CreatePrimitive()
                            .WithMaterial(materialEvaluator(_Material))
                            .WithVertexAccessors(_VertexAccessors)
                            .WithIndicesAccessor(pt, _IndexAccessors);

                CopyMorphTargets(dstPrim);
            }
        }

        private void CopyMorphTargets(MeshPrimitive dstPrim)
        {
            for (int i = 0; i < _MorphTargets.Count; ++i)
            {
                if (_MorphTargets[i] == null || _MorphTargets[i].Length == 0) throw new InvalidOperationException("all morph targets must have at least one accessor");

                dstPrim.WithMorphTargetAccessors(i, _MorphTargets[i]);
            }
        }

        #endregion

        #region API - Buffer merging

        public static void MergeBuffers(IEnumerable<PackedPrimitiveBuilder<TMaterial>> primitives)
        {
            _MergeIndices(primitives);
            _MergeStridedVertices(primitives.Where(p => p._StridedVertexType != null));
            _MergeSequentialVertices(primitives.Where(p => p._StridedVertexType == null).Select(p => p._VertexAccessors));
            _MergeSequentialVertices(primitives.SelectMany(p => p._MorphTargets));
        }

        private static void _MergeSequentialVertices(IEnumerable<MACCESSOR[]> primitives)
        {
            var vertexBuffers = new Dictionary<(string, int), PackedBuffer>();

            foreach (var vvv in primitives)
            {
                foreach (var v in vvv)
                {
                    var k = v.Attribute.Name;
                    var s = v.Attribute.ItemByteLength;

                    if (!vertexBuffers.TryGetValue((k,s), out PackedBuffer packed))
                    {
                        vertexBuffers[(k, s)] = packed = new PackedBuffer();
                    }

                    packed.AddAccessors(v);
                }
            }

            foreach (var vb in vertexBuffers.Values) vb.MergeBuffers();
        }

        private static void _MergeStridedVertices(IEnumerable<PackedPrimitiveBuilder<TMaterial>> primitives)
        {
            var perVertexGroups = primitives
                .ToList()
                .GroupBy(item => item._StridedVertexType);

            foreach (var group in perVertexGroups)
            {
                var vertexBuffers = new PackedBuffer();

                foreach (var p in group)
                {
                    vertexBuffers.AddAccessors(p._VertexAccessors);
                }

                vertexBuffers.MergeBuffers();
            }
        }

        private static void _MergeIndices(IEnumerable<PackedPrimitiveBuilder<TMaterial>> primitives)
        {
            var indexBuffers = new PackedBuffer();

            foreach (var p in primitives)
            {
                indexBuffers.AddAccessors(p._IndexAccessors);
            }

            indexBuffers.MergeBuffers();
        }

        #endregion
    }
}
