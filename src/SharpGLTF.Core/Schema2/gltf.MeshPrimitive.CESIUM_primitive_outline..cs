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
        public void SetCesiumOutline(int? indices)
        {
            if (indices == null) { RemoveExtensions<CESIUM_primitive_outlineglTFprimitiveextension>(); return; }
            var ext = UseExtension<CESIUM_primitive_outlineglTFprimitiveextension>();
            ext.Indices = indices;
        }
    }
}
