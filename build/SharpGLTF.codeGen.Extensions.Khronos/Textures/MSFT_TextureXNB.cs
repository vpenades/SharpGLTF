using System;
using System.Collections.Generic;
using System.Text;

using SharpGLTF.SchemaReflection;

namespace SharpGLTF
{
    class TextureXNBExtension : SchemaProcessor
    {
        private static string SchemaUri => System.IO.Path.Combine(AppContext.BaseDirectory, "Schemas", "glTF.MSFT_texture_xnb.schema.json");
        public override IEnumerable<(string, SchemaType.Context)> ReadSchema()
        {
            var ctx = SchemaProcessing.LoadExtensionSchemaContext(SchemaUri);

            yield return ("ext.MSFT.TextureXNB.g", ctx);
        }

        public override void PrepareTypes(CodeGen.CSharpEmitter newEmitter, SchemaType.Context ctx)
        {
            newEmitter.SetRuntimeName("MSFT_texture_xnb extension", "TextureXNB");
        }
    }
}
