using System;
using System.Collections.Generic;
using System.Linq;

namespace SharpGLTF.Schema2
{
    partial class CESIUM_primitive_outlineglTFprimitiveextension
    {
        internal CESIUM_primitive_outlineglTFprimitiveextension(MeshPrimitive meshPrimitive) { }

        public int? Indices
        {
            get => _indices;
            set => _indices = value;
        }
    }
    partial class MeshPrimitive
    {
        public void SetCesiumOutline(Accessor accessor)
        {
            if (accessor == null) { RemoveExtensions<CESIUM_primitive_outlineglTFprimitiveextension>(); return; }

            Guard.NotNull(accessor, nameof(accessor));
            Guard.MustShareLogicalParent(LogicalParent.LogicalParent, "this", accessor, nameof(accessor));
            Guard.IsTrue(accessor.Encoding == EncodingType.UNSIGNED_INT, nameof(accessor));
            Guard.IsTrue(accessor.Dimensions == DimensionType.SCALAR, nameof(accessor));

            var ext = UseExtension<CESIUM_primitive_outlineglTFprimitiveextension>();
            ext.Indices = accessor.LogicalIndex;
        }
    }

    public static class CesiumToolkit
    {
        /// <summary>
        /// Creates an accessor to store Cesium outline vertex indices
        /// </summary>
        /// <param name="model"></param>
        /// <param name="outlines"></param>
        /// <returns></returns>
        public static Accessor CreateCesiumOutlineAccessor(ModelRoot model, IReadOnlyList<uint> outlines)
        {
            var outlineBytes = new List<byte>();

            foreach (var outline in outlines)
            {
                var bytes = BitConverter.GetBytes(outline).ToList();
                outlineBytes.AddRange(bytes);
            }
            var buffer = model.UseBufferView(outlineBytes.ToArray());
            var accessor = model.CreateAccessor("Cesium outlines");
            accessor.SetData(buffer, 0, outlineBytes.Count / 4, DimensionType.SCALAR, EncodingType.UNSIGNED_INT, false);
            return accessor;
        }

        /// <summary>
        /// Checks if all the indices in the Cesium Outline Extension are existing in the primitive indices 
        /// </summary>
        /// <param name="model"></param>
        /// <returns>boolean, true is valid false is not valid</returns>
        public static bool ValidateCesiumOutlineExtension(ModelRoot model)
        {
            foreach (var mesh in model.LogicalMeshes)
            {
                foreach (var meshPrimitive in mesh.Primitives)
                {
                    var cesiumOutlineExtension = meshPrimitive.GetExtension<CESIUM_primitive_outlineglTFprimitiveextension>();
                    if (cesiumOutlineExtension != null)
                    {
                        var accessor = model.LogicalAccessors[(int)cesiumOutlineExtension.Indices];
                        var accessorIndices = accessor.AsIndicesArray();
                        var meshPrimitiveIndices = meshPrimitive.GetIndices();
                        foreach (var accessorIndice in accessorIndices)
                        {
                            var contains = meshPrimitiveIndices.Contains(accessorIndice);
                            if (!contains)
                            {
                                return false;
                            }
                        }
                    }
                }
            }
            return true;
        }
    }
}
