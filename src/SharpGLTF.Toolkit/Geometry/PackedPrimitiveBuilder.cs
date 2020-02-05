using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using SharpGLTF.Schema2;

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
        private Memory.MemoryAccessor[] _VertexAccessors;

        private Memory.MemoryAccessor _IndexAccessors;

        private readonly List<Memory.MemoryAccessor[]> _MorphTargets = new List<Memory.MemoryAccessor[]>();

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

            var vAccessors = new List<Memory.MemoryAccessor>();
            GuardAll.MustBeEqualTo(vAccessors.Select(item => item.Attribute.ByteOffset), 0, nameof(vAccessors));
            GuardAll.MustBeEqualTo(vAccessors.Select(item => item.Attribute.ByteStride), 0, nameof(vAccessors));

            foreach (var an in attributeNames)
            {
                var vAccessor = VertexTypes.VertexUtils.CreateVertexMemoryAccessor(srcPrim.Vertices, an, vertexEncoding);
                if (vAccessor == null) continue;

                vAccessors.Add(vAccessor);
            }

            _VertexAccessors = vAccessors.ToArray();

            Memory.MemoryAccessor.SanitizeVertexAttributes(_VertexAccessors);
        }

        public void SetIndices(IPrimitiveReader<TMaterial> srcPrim, EncodingType encoding)
        {
            Guard.NotNull(srcPrim, nameof(srcPrim));

            var iAccessor = VertexTypes.VertexUtils.CreateIndexMemoryAccessor(srcPrim.GetIndices(), encoding);

            if (_VerticesPerPrimitive == 1) Guard.MustBeNull(iAccessor, nameof(srcPrim));
            else Guard.NotNull(iAccessor, nameof(iAccessor));

            _IndexAccessors = iAccessor;
        }

        public void SetMorphTargets(IPrimitiveReader<TMaterial> srcPrim, PackedEncoding vertexEncodings)
        {
            bool hasPositions = _VertexAccessors.Any(item => item.Attribute.Name == "POSITION");
            bool hasNormals = _VertexAccessors.Any(item => item.Attribute.Name == "NORMAL");
            bool hasTangents = _VertexAccessors.Any(item => item.Attribute.Name == "TANGENT");

            if (!hasPositions) throw new InvalidOperationException("Set vertices before morph targets.");

            for (int i = 0; i < srcPrim.MorphTargets.Count; ++i)
            {
                var mtv = srcPrim.MorphTargets[i].GetMorphTargetVertices(srcPrim.Vertices.Count);

                var pAccessor = VertexTypes.VertexUtils.CreateVertexMemoryAccessor(mtv, "POSITIONDELTA", vertexEncodings);

                var nAccessor = !hasNormals ? null : VertexTypes.VertexUtils.CreateVertexMemoryAccessor(mtv, "NORMALDELTA", vertexEncodings);

                var tAccessor = !hasTangents ? null : VertexTypes.VertexUtils.CreateVertexMemoryAccessor(mtv, "TANGENTDELTA", vertexEncodings);

                AddMorphTarget(pAccessor, nAccessor, tAccessor);
            }
        }

        private void AddMorphTarget(params Memory.MemoryAccessor[] morphTarget)
        {
            morphTarget = morphTarget.Where(item => item != null).ToArray();

            foreach (var accessor in morphTarget)
            {
                if (accessor.Attribute.Dimensions != DimensionType.VEC3) throw new InvalidOperationException();
            }

            morphTarget = morphTarget
                .Select(item => _RemoveDelta(item))
                .ToArray();

            _MorphTargets.Add(morphTarget);
        }

        private static Memory.MemoryAccessor _RemoveDelta(Memory.MemoryAccessor accessor)
        {
            var name = accessor.Attribute.Name;
            if (!name.EndsWith("DELTA", StringComparison.Ordinal)) throw new InvalidOperationException();

            name = name.Replace("DELTA", string.Empty);

            var attr = accessor.Attribute;
            attr.Name = name;

            return new SharpGLTF.Memory.MemoryAccessor(accessor.Data, attr);
        }

        internal void CopyToMesh(Mesh dstMesh, Converter<TMaterial, Material> materialEvaluator)
        {
            if (_VerticesPerPrimitive < 1 || _VerticesPerPrimitive > 3) return;

            if (_VerticesPerPrimitive == 1)
            {
                var dstPrim = dstMesh.CreatePrimitive()
                        .WithMaterial(materialEvaluator(_Material))
                        .WithVertexAccessors(_VertexAccessors)
                        .WithIndicesAutomatic(PrimitiveType.POINTS);

                CopyMorphTargets(dstPrim);

                return;
            }
            else
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

        private static void _MergeSequentialVertices(IEnumerable<Memory.MemoryAccessor[]> primitives)
        {
            var vertexBuffers = new Dictionary<string, PackedBuffer>();

            foreach (var vvv in primitives)
            {
                foreach (var v in vvv)
                {
                    var k = v.Attribute.Name;

                    if (!vertexBuffers.TryGetValue(k, out PackedBuffer packed))
                    {
                        vertexBuffers[k] = packed = new PackedBuffer();
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
