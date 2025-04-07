using System;
using System.Collections.Generic;
using System.Text;

using SharpGLTF.SchemaReflection;

namespace SharpGLTF
{
    class TextureWebpExtension : SchemaProcessor
    {
        private static string SchemaUri => KhronosExtensions.VendorExtensionPath("EXT_texture_webp", "glTF.EXT_texture_webp.schema.json");
        public override IEnumerable<(string, SchemaType.Context)> ReadSchema()
        {
            var ctx = SchemaProcessing.LoadExtensionSchemaContext(SchemaUri);            

            yield return ("ext.TextureWEBP.g", ctx);
        }

        public override void PrepareTypes(CodeGen.CSharpEmitter newEmitter, SchemaType.Context ctx)
        {
            newEmitter.SetRuntimeName("EXT_texture_webp glTF extension", "TextureWEBP");
        }        
    }
}
