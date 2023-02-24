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
        // call using sample: model.LogicalMeshes[0].Primitives[0].AddCesiumOutline(4);

        public void AddCesiumOutline(int? indices)
        {
            if (indices != null) { this.RemoveExtensions<CESIUM_primitive_outlineglTFprimitiveextension>(); return; }
            var ext = new CESIUM_primitive_outlineglTFprimitiveextension(this);
            ext.Indices = indices;

            // what to do next?
            SetExtension(ext);
            UseExtension<CESIUM_primitive_outlineglTFprimitiveextension>();
        }
    }
}
