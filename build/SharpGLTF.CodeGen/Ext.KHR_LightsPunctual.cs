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

            ctx.FindClass("light")
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
            newEmitter.SetRuntimeName("light", "PunctualLight");
            newEmitter.SetRuntimeName("light/spot", "PunctualLightSpot");
            newEmitter.SetRuntimeName("KHR_lights_punctual glTF extension", "_ModelPunctualLights");
            newEmitter.SetRuntimeName("KHR_lights_punctual node extension", "_NodePunctualLight");
        }
    }
}
