using System;
using System.Collections.Generic;
using System.Linq;

namespace SharpGLTF.Schema2
{
    partial class CESIUM_primitive_outlineglTFprimitiveextension
    {
        private MeshPrimitive meshPrimitive;
        internal CESIUM_primitive_outlineglTFprimitiveextension(MeshPrimitive meshPrimitive)
        {
            this.meshPrimitive = meshPrimitive;
        }

        public int? Indices
        {
            get => _indices;
            set => _indices = value;
        }

        protected override void OnValidateContent(Validation.ValidationContext validate)
        {
            var outlineAccessor = meshPrimitive.LogicalParent.LogicalParent.LogicalAccessors[(int)_indices];
            var isValid = CesiumToolkit.ValidateCesiumOutlineIndices(outlineAccessor, meshPrimitive);
            validate.IsTrue(nameof(_indices), isValid, "Mismatch between accesor indices and MeshPrimitive indices");

            base.OnValidateContent(validate);
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
        public static Accessor CreateCesiumOutlineAccessor(ModelRoot model, IReadOnlyList<uint> outlines, string name="Cesium outlines")
        {
            var outlineBytes = new List<byte>();

            foreach (var bytes in from outline in outlines
                                  let bytes = BitConverter.GetBytes(outline).ToList()
                                  select bytes)
            {
                outlineBytes.AddRange(bytes);
            }

            var buffer = model.UseBufferView(outlineBytes.ToArray());
            var accessor = model.CreateAccessor(name);
            accessor.SetData(buffer, 0, outlineBytes.Count / 4, DimensionType.SCALAR, EncodingType.UNSIGNED_INT, false);
            return accessor;
        }

        /// <summary>
        /// Checks if all the indices of the Cesium outline accessor are within the range of in the MeshPrimitive indices
        /// </summary>
        /// <param name="accessor">Cesium outline accessor</param>
        /// <param name="meshPrimitive">MeshPrimitive with the CESIUM_primitive_outline extension</param>
        /// <returns>true all indices are available, false indices are missing </returns>
        internal static bool ValidateCesiumOutlineIndices(Accessor accessor, MeshPrimitive meshPrimitive)
        {
            var cesiumOutlineExtension = meshPrimitive.GetExtension<CESIUM_primitive_outlineglTFprimitiveextension>();
            if (cesiumOutlineExtension != null)
            {
                var accessorIndices = accessor.AsIndicesArray();
                var meshPrimitiveIndices = meshPrimitive.GetIndices();
                var maxIndice = meshPrimitiveIndices.Max();

                foreach (var _ in from accessorIndice in accessorIndices
                                  let contains = accessorIndice <= maxIndice
                                  where !contains
                                  select new { })
                {
                    return false;
                }
            }
            return true;
        }
    }
}
