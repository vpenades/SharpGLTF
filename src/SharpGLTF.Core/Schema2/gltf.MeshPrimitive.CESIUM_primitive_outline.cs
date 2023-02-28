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
            Guard.IsTrue(accessor.Dimensions== DimensionType.SCALAR, nameof(accessor));

            var ext = UseExtension<CESIUM_primitive_outlineglTFprimitiveextension>();
            ext.Indices = accessor.LogicalIndex;
        }
    }

    public static class CesiumOutline
    {
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
    }
}
