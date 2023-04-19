using System;
using System.Collections.Generic;
using System.Text;

using SharpGLTF.SchemaReflection;

namespace SharpGLTF
{
    class AgiArticulationsExtension : SchemaProcessor
    {
        private static string RootSchemaUri => Constants.VendorExtensionPath("AGI_articulations", "glTF.AGI_articulations.schema.json");
        private static string NodeSchemaUri => Constants.VendorExtensionPath("AGI_articulations", "node.AGI_articulations.schema.json");

        public override IEnumerable<(string, SchemaType.Context)> Process()
        {
            yield return ("ext.AgiRootArticulations.g", ProcessRoot());
            yield return ("ext.AgiNodeArticulations.g", ProcessNode());
        }        

        private static SchemaType.Context ProcessRoot()
        {
            var ctx = SchemaProcessing.LoadSchemaContext(RootSchemaUri);
            ctx.IgnoredByCodeEmitter("glTF Property");
            ctx.IgnoredByCodeEmitter("glTF Child of Root Property");

            ctx.FindClass("Articulation")
                .GetField("pointingVector")
                .SetDataType(typeof(System.Numerics.Vector3), true)
                .SetItemsRange(0);            

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
            newEmitter.SetRuntimeName("AGI_articulations glTF extension", "AgiRootArticulations");
            newEmitter.SetRuntimeName("AGI_articulations glTF Node extension", "AgiNodeArticulations");
            newEmitter.SetRuntimeName("Articulation", "AgiArticulation");
            newEmitter.SetRuntimeName("Articulation Stage", "AgiArticulationStage");
            newEmitter.SetRuntimeName("uniformScale-xRotate-xScale-xTranslate-yRotate-yScale-yTranslate-zRotate-zScale-zTranslate", "AgiArticulationTransformType");            

            var agiArticulationRootClass = ctx.FindClass("AGI_articulations glTF extension");
            if (agiArticulationRootClass != null)
            {
                newEmitter.SetCollectionContainer(agiArticulationRootClass.UseField("articulations"), "ChildrenList<TItem,AgiRootArticulations>");
            }

            var agiArticulationClass = ctx.FindClass("Articulation");
            if (agiArticulationClass != null)
            {
                newEmitter.SetCollectionContainer(agiArticulationClass.UseField("stages"), "ChildrenList<TItem,AgiArticulation>");
            }

            var agiStkMetadataRootClass = ctx.FindClass("AGI_stk_metadata glTF extension");
            if (agiStkMetadataRootClass != null)
            {
                newEmitter.SetCollectionContainer(agiStkMetadataRootClass.UseField("solarPanelGroups"), "ChildrenList<TItem,AgiRootStkMetadata>");
            }
        }
    }
}
