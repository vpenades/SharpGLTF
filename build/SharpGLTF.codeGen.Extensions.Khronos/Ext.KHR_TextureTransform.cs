using System;
using System.Collections.Generic;
using System.Text;

using SharpGLTF.SchemaReflection;

namespace SharpGLTF
{
    class TextureTransformExtension : SchemaProcessor
    {
        private static string SchemaUri => KhronosExtensions.KhronosExtensionPath("KHR_texture_transform", "textureInfo.KHR_texture_transform.schema.json");

        private const string ExtensionRootClassName = "KHR_texture_transform glTF TextureInfo Extension";

        public override IEnumerable<(string, SchemaType.Context)> ReadSchema()
        {
            var ctx = SchemaProcessing.LoadExtensionSchemaContext(SchemaUri);

            var tex = ctx.FindClass(ExtensionRootClassName);

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
            newEmitter.SetRuntimeName(ExtensionRootClassName, "TextureTransform");
        }
    }
}
