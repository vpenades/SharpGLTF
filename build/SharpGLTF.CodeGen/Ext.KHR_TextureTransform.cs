using System;
using System.Collections.Generic;
using System.Text;

using SharpGLTF.SchemaReflection;

namespace SharpGLTF
{
    class TextureTransformExtension : SchemaProcessor
    {
        private static string SchemaUri => Constants.KhronosExtensionPath("KHR_texture_transform", "KHR_texture_transform.textureInfo.schema.json");

        public override IEnumerable<(string, SchemaType.Context)> Process()
        {
            var ctx = SchemaProcessing.LoadSchemaContext(SchemaUri);
            ctx.IgnoredByCodeEmitter("glTF Property");
            ctx.IgnoredByCodeEmitter("glTF Child of Root Property");

            var tex = ctx.FindClass("KHR_texture_transform textureInfo extension");

            tex.GetField("offset")
                .SetDataType(typeof(System.Numerics.Vector2), true)
                .SetDefaultValue("Vector2.Zero")
                .SetItemsRange(0);

            tex.GetField("scale")
                .SetDataType(typeof(System.Numerics.Vector2), true)
                .SetDefaultValue("Vector2.One")
                .SetItemsRange(0);

            yield return ("ext.TextureTransform.g", ctx);
        }

        public override void PrepareTypes(CodeGen.CSharpEmitter newEmitter, SchemaType.Context ctx)
        {
            newEmitter.SetRuntimeName("KHR_texture_transform textureInfo extension", "TextureTransform");
        }
    }
}
