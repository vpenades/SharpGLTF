namespace SharpGLTF.Schema2
{
    /// <summary>
    /// Extension methods for 3DTiles glTF Extensions
    /// </summary>
    public static partial class ThreeDTilesExtensions
    {
        private static bool _3DTilesRegistered;

        /// <summary>
        /// This method most be called once at application's startup to register the extensions.
        /// </summary>
        public static void RegisterExtensions()
        {
            if (_3DTilesRegistered) return;

            _3DTilesRegistered = true;

            ExtensionsFactory.RegisterExtension<MeshPrimitive, CesiumPrimitiveOutline>("CESIUM_primitive_outline");
            ExtensionsFactory.RegisterExtension<Node, MeshExtInstanceFeatures>("EXT_instance_features");
            ExtensionsFactory.RegisterExtension<MeshPrimitive, MeshExtMeshFeatures>("EXT_mesh_features");
            ExtensionsFactory.RegisterExtension<ModelRoot, EXTStructuralMetadataRoot>("EXT_structural_metadata");
            ExtensionsFactory.RegisterExtension<MeshPrimitive, ExtStructuralMetadataMeshPrimitive>("EXT_structural_metadata");
        }
    }
}
