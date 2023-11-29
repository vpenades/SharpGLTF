using SharpGLTF.CodeGen;
using SharpGLTF.SchemaReflection;
using System.Collections.Generic;

namespace SharpGLTF
{
    class ExtStructuralMetadataExtension : SchemaProcessor
    {
        public override string GetTargetProject() { return Constants.CesiumProjectDirectory; }

        private static string RootSchemaUri => Constants.CustomExtensionsPath("EXT_structural_metadata", "EXT_structural_metadata.schema.json");

        public override void PrepareTypes(CSharpEmitter newEmitter, SchemaType.Context ctx)
        {
            //newEmitter.SetRuntimeName("EXT_mesh_features glTF Mesh Primitive extension", "MeshExtMeshFeatures");
            //newEmitter.SetRuntimeName("Feature ID in EXT_mesh_features", "MeshExtMeshFeatureID");
            //newEmitter.SetRuntimeName(ExtensionFeatureIdTextureName, "MeshExtMeshFeatureIDTexture");
        }

        public override IEnumerable<(string TargetFileName, SchemaType.Context Schema)> Process()
        {
            yield return ("Ext.CESIUM_ext_structural_metadata.g", ProcessRoot());
        }

        private static SchemaType.Context ProcessRoot()
        {
            var ctx = SchemaProcessing.LoadSchemaContext(RootSchemaUri);
            ctx.IgnoredByCodeEmitter("glTF Property");
            ctx.IgnoredByCodeEmitter("glTF Child of Root Property");
            ctx.IgnoredByCodeEmitter("Texture Info");
            return ctx;
        }
    }
}
