using System;
using System.Collections.Generic;
using System.Text;

using SharpGLTF.SchemaReflection;

namespace SharpGLTF
{
    class LightsPunctualExtension : SchemaProcessor
    {
        private static string RootSchemaUri => Constants.KhronosExtensionPath("KHR_lights_punctual", "glTF.KHR_lights_punctual.schema.json");
        private static string NodeSchemaUri => Constants.KhronosExtensionPath("KHR_lights_punctual", "node.KHR_lights_punctual.schema.json");

        private const string ExtensionRootClassName = "KHR_lights_punctual glTF Document Extension";
        private const string ExtensionNodeClassName = "KHR_lights_punctual glTF Node Extension";
        private const string ExtensionLightClassName = "KHR_lights_punctual Light Properties";
        private const string ExtensionLightSpotClassName = "KHR_lights_punctual Light Spot Properties";

        public override IEnumerable<(string, SchemaType.Context)> Process()
        {            
            yield return ("ext.ModelLightsPunctual.g", ProcessModel());
            yield return ("ext.NodeLightsPunctual.g", ProcessNode());
        }
        private static SchemaType.Context ProcessModel()
        {
            var ctx = SchemaProcessing.LoadSchemaContext(RootSchemaUri);
            ctx.IgnoredByCodeEmitter("glTF Property");
            ctx.IgnoredByCodeEmitter("glTF Child of Root Property");

            ctx.FindClass(ExtensionLightClassName)
                .GetField("color")
                .SetDataType(typeof(System.Numerics.Vector3), true)
                .SetDefaultValue("Vector3.One")
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
            newEmitter.SetRuntimeName(ExtensionLightClassName, "PunctualLight");
            newEmitter.SetRuntimeName(ExtensionLightSpotClassName, "PunctualLightSpot");
            newEmitter.SetRuntimeName(ExtensionRootClassName, "_ModelPunctualLights");
            newEmitter.SetRuntimeName(ExtensionNodeClassName, "_NodePunctualLight");
        }
    }
}
