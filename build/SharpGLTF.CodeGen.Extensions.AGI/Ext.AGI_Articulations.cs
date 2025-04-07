using System;
using System.Collections.Generic;
using System.Text;

using SharpGLTF.SchemaReflection;

namespace SharpGLTF
{
    class AgiArticulationsExtension : SchemaProcessor
    {
        public override string GetTargetProject() { return AgiExtensions.AgiProjectDirectory; }
        private static string RootSchemaUri => AgiExtensions.VendorExtensionPath("AGI_articulations", "glTF.AGI_articulations.schema.json");
        private static string NodeSchemaUri => AgiExtensions.VendorExtensionPath("AGI_articulations", "node.AGI_articulations.schema.json");

        public override IEnumerable<(string, SchemaType.Context)> ReadSchema()
        {
            yield return ("ext.AgiRootArticulations.g", ProcessRoot());
            yield return ("ext.AgiNodeArticulations.g", ProcessNode());
        }        

        private static SchemaType.Context ProcessRoot()
        {
            var ctx = SchemaProcessing.LoadExtensionSchemaContext(RootSchemaUri);

            ctx.FindClass("Articulation")
                .GetField("pointingVector")
                .SetDataType(typeof(System.Numerics.Vector3), true)
                .SetItemsRange(0);            

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
            newEmitter.SetRuntimeName("AGI_articulations glTF extension", "AgiRootArticulations", AgiExtensions.AgiNameSpace);
            newEmitter.SetRuntimeName("AGI_articulations glTF Node extension", "AgiNodeArticulations", AgiExtensions.AgiNameSpace);
            newEmitter.SetRuntimeName("Articulation", "AgiArticulation", AgiExtensions.AgiNameSpace);
            newEmitter.SetRuntimeName("Articulation Stage", "AgiArticulationStage", AgiExtensions.AgiNameSpace);
            newEmitter.SetRuntimeName("uniformScale-xRotate-xScale-xTranslate-yRotate-yScale-yTranslate-zRotate-zScale-zTranslate", "AgiArticulationTransformType", AgiExtensions.AgiNameSpace);

            newEmitter.SetFieldToChildrenList(ctx, "AGI_articulations glTF extension", "articulations");
            newEmitter.SetFieldToChildrenList(ctx, "Articulation", "stages");
            newEmitter.SetFieldToChildrenList(ctx, "AGI_stk_metadata glTF extension", "solarPanelGroups");
        }
    }
}
