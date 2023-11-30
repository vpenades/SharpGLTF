using SharpGLTF.CodeGen;
using SharpGLTF.SchemaReflection;
using System.Collections.Generic;

namespace SharpGLTF
{
    class ExtInstanceFeaturesExtension : SchemaProcessor
    {
        public override string GetTargetProject() { return Constants.CesiumProjectDirectory; }

        private static string NodeSchemaUri => Constants.CustomExtensionsPath("EXT_instance_features", "node.EXT_instance_features.schema.json");

        public override void PrepareTypes(CSharpEmitter newEmitter, SchemaType.Context ctx)
        {
            newEmitter.SetRuntimeName("EXT_instance_features glTF Node extension", "MeshExtInstanceFeatures");
            newEmitter.SetRuntimeName("Feature ID in EXT_instance_features", "MeshExtInstanceFeatureID");
        }

        public override IEnumerable<(string TargetFileName, SchemaType.Context Schema)> Process()
        {
            yield return ("Ext.CESIUM_ext_instance_features.g", ProcessNode());
        }

        private static SchemaType.Context ProcessNode()
        {
            var ctx = SchemaProcessing.LoadSchemaContext(NodeSchemaUri);
            ctx.IgnoredByCodeEmitter("glTF Property");
            ctx.IgnoredByCodeEmitter("glTF Child of Root Property");
            ctx.IgnoredByCodeEmitter("Texture Info");
            return ctx;
        }
    }
}
