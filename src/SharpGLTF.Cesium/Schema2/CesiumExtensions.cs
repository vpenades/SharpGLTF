namespace SharpGLTF.Schema2
{
    /// <summary>
    /// Extension methods for Cesium glTF Extensions
    /// </summary>
    public static partial class CesiumExtensions
    {
        private static bool _CesiumRegistered;

        /// <summary>
        /// This method most be called once at application's startup to register the extensions.
        /// </summary>
        public static void RegisterExtensions()
        {
            if (_CesiumRegistered) return;

            _CesiumRegistered = true;

            ExtensionsFactory.RegisterExtension<MeshPrimitive, CesiumPrimitiveOutline>("CESIUM_primitive_outline");
            ExtensionsFactory.RegisterExtension<Node, MeshExtInstanceFeatures>("EXT_instance_features");
            ExtensionsFactory.RegisterExtension<MeshPrimitive, MeshExtMeshFeatures>("EXT_mesh_features");
            ExtensionsFactory.RegisterExtension<ModelRoot, EXTStructuralMetadataRoot>("EXT_structural_metadata");

            // todo: register the rest of the extensions
            // ExtensionsFactory.RegisterExtension<MeshPrimitive, EXTStructuralMetadataMeshPrimitive>("EXT_structural_metadata");
        }
    }
}
