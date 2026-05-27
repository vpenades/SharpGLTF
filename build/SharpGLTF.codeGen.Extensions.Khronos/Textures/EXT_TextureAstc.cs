using System;
using System.Collections.Generic;
using System.Text;

using SharpGLTF.SchemaReflection;

namespace SharpGLTF
{
    class TextureAstcExtension : SchemaProcessor
    {
        private static string SchemaUri => KhronosExtensions.VendorExtensionPath("EXT_texture_astc", "texture.EXT_texture_astc.schema.json");
        public override IEnumerable<(string, SchemaType.Context)> ReadSchema()
        {
            var ctx = SchemaProcessing.LoadExtensionSchemaContext(SchemaUri);            

            yield return ("ext.TextureASTC.g", ctx);
        }

        public override void PrepareTypes(CodeGen.CSharpEmitter newEmitter, SchemaType.Context ctx)
        {
            newEmitter.SetRuntimeName("EXT_texture_astc glTF Texture Extension", "TextureASTC");
        }        
    }
}
