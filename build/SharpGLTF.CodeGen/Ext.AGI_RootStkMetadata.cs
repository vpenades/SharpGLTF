using System;
using System.Collections.Generic;
using System.Text;

using SharpGLTF.SchemaReflection;

namespace SharpGLTF
{
    class AgiStkMetadataExtension : SchemaProcessor
    {
        private static string RootSchemaUri => Constants.VendorExtensionPath("AGI_stk_metadata", "glTF.AGI_stk_metadata.schema.json");
        private static string NodeSchemaUri => Constants.VendorExtensionPath("AGI_stk_metadata", "node.AGI_stk_metadata.schema.json");

        public override IEnumerable<(string, SchemaType.Context)> Process()
        {
            yield return ("ext.AgiRootStkMetadata.g", ProcessRoot());
            yield return ("ext.AgiNodeStkMetadata.g", ProcessNode());
        }        

        private static SchemaType.Context ProcessRoot()
        {
            var ctx = SchemaProcessing.LoadSchemaContext(RootSchemaUri);
            ctx.IgnoredByCodeEmitter("glTF Property");
            ctx.IgnoredByCodeEmitter("glTF Child of Root Property");
            return ctx;
        }

        private static SchemaType.Context ProcessNode()
        {
            var ctx = SchemaProcessing.LoadSchemaContext(NodeSchemaUri);
            ctx.IgnoredByCodeEmitter("glTF Property");
            ctx.IgnoredByCodeEmitter("glTF Child of Root Property");
            return ctx;
        }

        public override void PrepareTypes(CodeGen.CSharpEmitter newEmitter, SchemaType.Context ctx)
        {
            newEmitter.SetRuntimeName("AGI_stk_metadata glTF extension", "AgiRootStkMetadata");
            newEmitter.SetRuntimeName("AGI_stk_metadata glTF Node extension", "AgiNodeStkMetadata");
            newEmitter.SetRuntimeName("Solar Panel Group", "AgiStkSolarPanelGroup");
        }        
    }
}
