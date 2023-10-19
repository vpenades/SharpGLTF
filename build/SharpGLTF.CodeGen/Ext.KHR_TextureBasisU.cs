using System;
using System.Collections.Generic;
using System.Text;

using SharpGLTF.SchemaReflection;

namespace SharpGLTF
{
    class TextureKtx2Extension : SchemaProcessor
    {
        private static string SchemaUri => Constants.KhronosExtensionPath("KHR_texture_basisu", "texture.KHR_texture_basisu.schema.json");

        private const string ExtensionRootClassName = "KHR_texture_basisu glTF Texture Extension";

        public override IEnumerable<(string, SchemaType.Context)> Process()
        {
            var ctx = SchemaProcessing.LoadSchemaContext(SchemaUri);
            ctx.IgnoredByCodeEmitter("glTF Property");

            yield return ("ext.TextureKTX2.g", ctx);
        }

        public override void PrepareTypes(CodeGen.CSharpEmitter newEmitter, SchemaType.Context ctx)
        {
            newEmitter.SetRuntimeName(ExtensionRootClassName, "TextureKTX2");
        }
    }
}
