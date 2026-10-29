namespace SharpGLTF.Schema2
{
    using Tiles3D;

    /// <summary>
    /// Extension methods for 3DTiles glTF Extensions
    /// </summary>
    public static partial class Tiles3DExtensions
    {
        private static bool _3DTilesRegistered;

        /// <summary>
        /// This method most be called once at application's startup to register the extensions.
        /// </summary>
        public static void RegisterExtensions()
        {
            if (_3DTilesRegistered) return;

            _3DTilesRegistered = true;

            ExtensionsFactory.RegisterExtension<MeshPrimitive, CesiumPrimitiveOutline>("CESIUM_primitive_outline", p=> new CesiumPrimitiveOutline(p));
            ExtensionsFactory.RegisterExtension<Node, MeshExtInstanceFeatures>("EXT_instance_features", p => new MeshExtInstanceFeatures(p));
            ExtensionsFactory.RegisterExtension<MeshPrimitive, MeshExtMeshFeatures>("EXT_mesh_features", p => new MeshExtMeshFeatures(p));
            ExtensionsFactory.RegisterExtension<ModelRoot, EXTStructuralMetadataRoot>("EXT_structural_metadata", p => new EXTStructuralMetadataRoot(p));
            ExtensionsFactory.RegisterExtension<MeshPrimitive, ExtStructuralMetadataMeshPrimitive>("EXT_structural_metadata", p => new ExtStructuralMetadataMeshPrimitive(p));

            _Register3DTiles21Extensions();
        }

        /// <summary>
        /// Registers the extensions of the glTF 2.1 3D Tiles vendor extensions (3d-tiles-2.0 branch).
        /// </summary>
        /// <remarks>
        /// 3DTILES_shape_* extensions are not registered because they extend the glTF 2.1 "shape" object, which is not supported by the core library.
        /// EXT_node_visibility_* extensions extend KHR_node_visibility, so they are registered against <see cref="ExtraProperties"/>.
        /// </remarks>
        private static void _Register3DTiles21Extensions()
        {
            ExtensionsFactory.RegisterExtension<Node, ExtGeoreferenceNode>(ExtGeoreferenceNode.SCHEMANAME, p => new ExtGeoreferenceNode());

            ExtensionsFactory.RegisterExtension<ModelRoot, ExtGeospatialCrs>(ExtGeospatialCrs.SCHEMANAME, p => new ExtGeospatialCrs());
            ExtensionsFactory.RegisterExtension<ExtGeospatialCrs, ExtGeospatialCrsWkid>(ExtGeospatialCrsWkid.SCHEMANAME, p => new ExtGeospatialCrsWkid());
            ExtensionsFactory.RegisterExtension<ExtGeospatialCrs, ExtGeospatialCrsWkt2>(ExtGeospatialCrsWkt2.SCHEMANAME, p => new ExtGeospatialCrsWkt2());

            ExtensionsFactory.RegisterExtension<ExtraProperties, ExtNodeVisibilityVolume>(ExtNodeVisibilityVolume.SCHEMANAME, p => new ExtNodeVisibilityVolume());
            ExtensionsFactory.RegisterExtension<ExtraProperties, ExtNodeVisibilityConditions>(ExtNodeVisibilityConditions.SCHEMANAME, p => new ExtNodeVisibilityConditions());
            ExtensionsFactory.RegisterExtension<ModelRoot, ExtNodeVisibilityConditionsRoot>(ExtNodeVisibilityConditionsRoot.SCHEMANAME, p => new ExtNodeVisibilityConditionsRoot());

            ExtensionsFactory.RegisterExtension<Node, ExtVoxelsNode>(ExtVoxelsNode.SCHEMANAME, p => new ExtVoxelsNode());

            ExtensionsFactory.RegisterExtension<ModelRoot, Tiles3DHorizonOcclusionPoint>(Tiles3DHorizonOcclusionPoint.SCHEMANAME, p => new Tiles3DHorizonOcclusionPoint());
            ExtensionsFactory.RegisterExtension<Node, Tiles3DImplicitTiling>(Tiles3DImplicitTiling.SCHEMANAME, p => new Tiles3DImplicitTiling());
            ExtensionsFactory.RegisterExtension<ModelRoot, Tiles3DLayersRoot>(Tiles3DLayersRoot.SCHEMANAME, p => new Tiles3DLayersRoot());
            ExtensionsFactory.RegisterExtension<Node, Tiles3DLayersNode>(Tiles3DLayersNode.SCHEMANAME, p => new Tiles3DLayersNode());
            ExtensionsFactory.RegisterExtension<ModelRoot, Tiles3DSubtree>(Tiles3DSubtree.SCHEMANAME, p => new Tiles3DSubtree());
            ExtensionsFactory.RegisterExtension<ModelRoot, Tiles3DTilesetRoot>(Tiles3DTilesetRoot.SCHEMANAME, p => new Tiles3DTilesetRoot());
            ExtensionsFactory.RegisterExtension<Node, Tiles3DTilesetNode>(Tiles3DTilesetNode.SCHEMANAME, p => new Tiles3DTilesetNode());
            ExtensionsFactory.RegisterExtension<ModelRoot, Tiles3DTilesetVectors>(Tiles3DTilesetVectors.SCHEMANAME, p => new Tiles3DTilesetVectors());
            ExtensionsFactory.RegisterExtension<ModelRoot, Tiles3DTilesetVoxels>(Tiles3DTilesetVoxels.SCHEMANAME, p => new Tiles3DTilesetVoxels());
        }
    }
}
