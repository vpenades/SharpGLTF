using System;
using System.Collections.Generic;
using System.Text;

using SharpGLTF.SchemaReflection;

namespace SharpGLTF
{
    class ClearCoatExtension : SchemaProcessor
    {
        private static string SchemaUri => KhronosExtensions.KhronosExtensionPath("KHR_materials_clearcoat", "material.KHR_materials_clearcoat.schema.json");

        private const string ExtensionRootClassName = "KHR_materials_clearcoat glTF Material Extension";

        public override IEnumerable<(string, SchemaType.Context)> ReadSchema()
        {
            var ctx = SchemaProcessing.LoadExtensionSchemaContext(SchemaUri);

            yield return("ext.ClearCoat.g", ctx);
        }

        public override void PrepareTypes(CodeGen.CSharpEmitter newEmitter, SchemaType.Context ctx)
        {
            newEmitter.SetRuntimeName(ExtensionRootClassName, "MaterialClearCoat");
        }        
    }
}
