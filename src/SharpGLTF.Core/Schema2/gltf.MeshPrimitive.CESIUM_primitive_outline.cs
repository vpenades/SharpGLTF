using System;
using System.Collections.Generic;
using System.Linq;

using SharpGLTF.Validation;

namespace SharpGLTF.Schema2
{
    partial class CesiumPrimitiveOutline
    {
        private MeshPrimitive meshPrimitive;
        internal CesiumPrimitiveOutline(MeshPrimitive meshPrimitive)
        {
            this.meshPrimitive = meshPrimitive;
        }        

        public Accessor Indices
        {
            get
            {
                return _indices.HasValue
                    ? meshPrimitive.LogicalParent.LogicalParent.LogicalAccessors[_indices.Value]
                    : null;
            }
            set
            {
                if (value == null) { _indices = null; return; }

                _ValidateAccessor(meshPrimitive.LogicalParent.LogicalParent, value);

                _indices = value.LogicalIndex;
            }
        }

        protected override void OnValidateReferences(ValidationContext validate)
        {
            validate.IsNullOrIndex(nameof(Indices), this._indices, meshPrimitive.LogicalParent.LogicalParent.LogicalAccessors);

            base.OnValidateReferences(validate);
        }

        protected override void OnValidateContent(Validation.ValidationContext validate)
        {
            var outlineAccessor = meshPrimitive.LogicalParent.LogicalParent.LogicalAccessors[(int)_indices];
            var isValid = _ValidateCesiumOutlineIndices(outlineAccessor, meshPrimitive);
            validate.IsTrue(nameof(_indices), isValid, "Mismatch between accesor indices and MeshPrimitive indices");

            base.OnValidateContent(validate);
        }

        internal static void _ValidateAccessor(ModelRoot model, Accessor accessor)
        {
            Guard.NotNull(accessor, nameof(accessor));
            Guard.MustShareLogicalParent(model, "this", accessor, nameof(accessor));
            Guard.IsTrue(accessor.Encoding == EncodingType.UNSIGNED_INT, nameof(accessor));
            Guard.IsTrue(accessor.Dimensions == DimensionType.SCALAR, nameof(accessor));
            Guard.IsFalse(accessor.Normalized, nameof(accessor));
        }

        /// <summary>
        /// Checks if all the indices of the Cesium outline accessor are within the range of in the MeshPrimitive indices
        /// </summary>
        /// <param name="accessor">Cesium outline accessor</param>
        /// <param name="meshPrimitive">MeshPrimitive with the CESIUM_primitive_outline extension</param>
        /// <returns>true all indices are available, false indices are missing </returns>
        private static bool _ValidateCesiumOutlineIndices(Accessor accessor, MeshPrimitive meshPrimitive)
        {
            var cesiumOutlineExtension = meshPrimitive.GetExtension<CesiumPrimitiveOutline>();
            if (cesiumOutlineExtension != null)
            {
                var accessorIndices = accessor.AsIndicesArray();
                var meshPrimitiveIndices = meshPrimitive.GetIndices();
                var maxIndex = meshPrimitiveIndices.Max();

                foreach (var _ in from accessorIndice in accessorIndices
                                  let contains = accessorIndice <= maxIndex
                                  where !contains
                                  select new { })
                {
                    return false;
                }
            }
            return true;
        }
    }

    partial class MeshPrimitive
    {
        /// <summary>
        /// Sets Cesium outline vertex indices
        /// </summary>
        /// <param name="outlines">the list of vertex indices.</param>
        /// <param name="accessorName">the name of the accessor to be created.</param>
        public void SetCesiumOutline(IReadOnlyList<uint> outlines, string accessorName = "Cesium outlines")
        {
            Guard.NotNull(outlines, nameof(outlines));

            // create and fill data

            var dstData = new Byte[outlines.Count * 4];
            var dstArray = new Memory.IntegerArray(dstData, IndexEncodingType.UNSIGNED_INT);
            for (int i = 0; i < outlines.Count; ++i) { dstArray[i] = outlines[i]; }

            var model = this.LogicalParent.LogicalParent;

            var bview = model.UseBufferView(dstData);
            var accessor = model.CreateAccessor(accessorName);

            accessor.SetData(bview, 0, dstArray.Count, DimensionType.SCALAR, EncodingType.UNSIGNED_INT, false);

            SetCesiumOutline(accessor);
        }

        public void SetCesiumOutline(Accessor accessor)
        {
            if (accessor == null) { RemoveExtensions<CesiumPrimitiveOutline>(); return; }

            CesiumPrimitiveOutline._ValidateAccessor(this.LogicalParent.LogicalParent, accessor);

            var ext = UseExtension<CesiumPrimitiveOutline>();
            ext.Indices = accessor;
        }
    }
}
