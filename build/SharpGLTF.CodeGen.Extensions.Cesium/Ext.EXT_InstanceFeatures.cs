using SharpGLTF.CodeGen;
using SharpGLTF.SchemaReflection;
using System.Collections.Generic;

namespace SharpGLTF
{
    class ExtInstanceFeaturesExtension : SchemaProcessor
    {
        public override string GetTargetProject() { return CesiumExtensions.CesiumProjectDirectory; }

        private static string NodeSchemaUri => CesiumExtensions.CustomExtensionsPath("EXT_instance_features", "node.EXT_instance_features.schema.json");

        public override void PrepareTypes(CSharpEmitter newEmitter, SchemaType.Context ctx)
        {            
            newEmitter.SetRuntimeName("EXT_instance_features glTF Node extension", "MeshExtInstanceFeatures", CesiumExtensions.CesiumNameSpace);
            newEmitter.SetRuntimeName("Feature ID in EXT_instance_features", "MeshExtInstanceFeatureID", CesiumExtensions.CesiumNameSpace);         

            newEmitter.SetFieldToChildrenList(ctx, "EXT_instance_features glTF Node extension", "featureIds");
        }

        public override IEnumerable<(string TargetFileName, SchemaType.Context Schema)> ReadSchema()
        {
            yield return ("Ext.CESIUM_ext_instance_features.g", ProcessNode());
        }

        private static SchemaType.Context ProcessNode()
        {
            var ctx = SchemaProcessing.LoadExtensionSchemaContext(NodeSchemaUri);
            return ctx;
        }
    }
}
