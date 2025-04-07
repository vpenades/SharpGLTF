using System;
using System.Collections.Generic;
using System.Text;

using SharpGLTF.SchemaReflection;

namespace SharpGLTF
{
    class IorExtension : SchemaProcessor
    {
        private static string SchemaUri => KhronosExtensions.KhronosExtensionPath("KHR_materials_ior", "material.KHR_materials_ior.schema.json");

        private const string ExtensionRootClassName = "KHR_materials_ior glTF Material Extension";

        public override IEnumerable<(string, SchemaType.Context)> ReadSchema()
        {
            var ctx = SchemaProcessing.LoadExtensionSchemaContext(SchemaUri);

            yield return ("ext.Ior.g", ctx);
        }

        public override void PrepareTypes(CodeGen.CSharpEmitter newEmitter, SchemaType.Context ctx)
        {
            newEmitter.SetRuntimeName(ExtensionRootClassName, "MaterialIOR");
        }
    }
}
