using System;
using System.Collections.Generic;
using System.Text;

using SharpGLTF.SchemaReflection;

namespace SharpGLTF
{
    class TextureDDSExtension : SchemaProcessor
    {
        private static string SchemaUri => KhronosExtensions.VendorExtensionPath("MSFT_texture_dds", "glTF.MSFT_texture_dds.schema.json");
        public override IEnumerable<(string, SchemaType.Context)> ReadSchema()
        {
            var ctx = SchemaProcessing.LoadExtensionSchemaContext(SchemaUri);

            yield return ("ext.MSFT.TextureDDS.g", ctx);
        }

        public override void PrepareTypes(CodeGen.CSharpEmitter newEmitter, SchemaType.Context ctx)
        {
            newEmitter.SetRuntimeName("MSFT_texture_dds extension", "TextureDDS");
        }
    }
}
