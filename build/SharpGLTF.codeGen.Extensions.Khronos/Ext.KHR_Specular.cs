using System;
using System.Collections.Generic;
using System.Text;

using SharpGLTF.SchemaReflection;

namespace SharpGLTF
{
    class SpecularExtension : SchemaProcessor
    {
        private static string SchemaUri => KhronosExtensions.KhronosExtensionPath("KHR_materials_specular", "material.KHR_materials_specular.schema.json");

        private const string ExtensionRootClassName = "KHR_materials_specular glTF Material Extension";

        public override IEnumerable<(string, SchemaType.Context)> ReadSchema()
        {
            var ctx = SchemaProcessing.LoadExtensionSchemaContext(SchemaUri);

            ctx.FindClass(ExtensionRootClassName)
                .GetField("specularColorFactor")
                .SetDataType(typeof(System.Numerics.Vector3), true)
                .SetDefaultValue("Vector3.One")
                .SetItemsRange(0);

            yield return ("ext.pbrSpecular.g", ctx);
        }

        public override void PrepareTypes(CodeGen.CSharpEmitter newEmitter, SchemaType.Context ctx)
        {
            newEmitter.SetRuntimeName(ExtensionRootClassName, "MaterialSpecular");
        }
    }
}
