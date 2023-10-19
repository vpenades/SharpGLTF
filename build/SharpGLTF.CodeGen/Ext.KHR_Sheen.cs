using System;
using System.Collections.Generic;
using System.Text;

using SharpGLTF.SchemaReflection;

namespace SharpGLTF
{
    class SheenExtension : SchemaProcessor
    {
        private static string SchemaUri => Constants.KhronosExtensionPath("KHR_materials_sheen", "material.KHR_materials_sheen.schema.json");

        private const string ExtensionRootClassName = "KHR_materials_sheen glTF Material Extension";

        public override IEnumerable<(string, SchemaType.Context)> Process()
        {
            var ctx = SchemaProcessing.LoadSchemaContext(SchemaUri);
            ctx.IgnoredByCodeEmitter("glTF Property");
            ctx.IgnoredByCodeEmitter("glTF Child of Root Property");
            ctx.IgnoredByCodeEmitter("Texture Info");
            ctx.IgnoredByCodeEmitter("Material Normal Texture Info");


            ctx.FindClass(ExtensionRootClassName)
                .GetField("sheenColorFactor")
                .SetDataType(typeof(System.Numerics.Vector3), true)
                .SetDefaultValue("Vector3.Zero")
                .SetItemsRange(0);

            ctx.FindClass(ExtensionRootClassName)
                .GetField("sheenRoughnessFactor")
                .SetDataType(typeof(float), true)
                .SetItemsRange(0);

            yield return ("ext.Sheen.g", ctx);
        }

        public override void PrepareTypes(CodeGen.CSharpEmitter newEmitter, SchemaType.Context ctx)
        {
            newEmitter.SetRuntimeName(ExtensionRootClassName, "MaterialSheen");
        }
    }
}
