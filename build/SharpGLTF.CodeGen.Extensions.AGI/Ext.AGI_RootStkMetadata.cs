using System;
using System.Collections.Generic;
using System.Text;

using SharpGLTF.SchemaReflection;

namespace SharpGLTF
{
    class AgiStkMetadataExtension : SchemaProcessor
    {
        public override string GetTargetProject() { return AgiExtensions.AgiProjectDirectory; }
        private static string RootSchemaUri => AgiExtensions.VendorExtensionPath("AGI_stk_metadata", "glTF.AGI_stk_metadata.schema.json");
        private static string NodeSchemaUri => AgiExtensions.VendorExtensionPath("AGI_stk_metadata", "node.AGI_stk_metadata.schema.json");

        public override IEnumerable<(string, SchemaType.Context)> ReadSchema()
        {
            yield return ("ext.AgiRootStkMetadata.g", ProcessRoot());
            yield return ("ext.AgiNodeStkMetadata.g", ProcessNode());
        }        

        private static SchemaType.Context ProcessRoot()
        {
            var ctx = SchemaProcessing.LoadExtensionSchemaContext(RootSchemaUri);
            return ctx;
        }

        private static SchemaType.Context ProcessNode()
        {
            var ctx = SchemaProcessing.LoadExtensionSchemaContext(NodeSchemaUri);            
            return ctx;
        }

        public override void PrepareTypes(CodeGen.CSharpEmitter newEmitter, SchemaType.Context ctx)
        {
            newEmitter.SetRuntimeName("AGI_stk_metadata glTF extension", "AgiRootStkMetadata", AgiExtensions.AgiNameSpace);
            newEmitter.SetRuntimeName("AGI_stk_metadata glTF Node extension", "AgiNodeStkMetadata", AgiExtensions.AgiNameSpace);
            newEmitter.SetRuntimeName("Solar Panel Group", "AgiStkSolarPanelGroup", AgiExtensions.AgiNameSpace);
        }        
    }
}
