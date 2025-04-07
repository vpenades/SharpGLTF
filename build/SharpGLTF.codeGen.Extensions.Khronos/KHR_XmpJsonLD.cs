using System;
using System.Collections.Generic;
using System.Text;

using SharpGLTF.SchemaReflection;

namespace SharpGLTF
{
    class XmpJsonLdExtension : SchemaProcessor
    {
        private static string RootSchemaUri => KhronosExtensions.KhronosExtensionPath("KHR_xmp_json_ld", "glTF.KHR_xmp_json_ld.schema.json");

        private static string PropSchemaUri => KhronosExtensions.KhronosExtensionPath("KHR_xmp_json_ld", "KHR_xmp_json_ld.schema.json");

        private const string ExtensionRootClassName = "KHR_xmp_json_ld glTF Document Extension";
        private const string ExtensionPropClassName = "KHR_xmp_json_ld glTF Extension";

        public override IEnumerable<(string, SchemaType.Context)> ReadSchema()
        {
            yield return ("ext.XmpJsonLinkedData.Root.g", ProcessModel());
            yield return ("ext.XmpJsonLinkedData.Prop.g", ProcessProps());
        }

        private static SchemaType.Context ProcessModel()
        {
            var ctx = SchemaProcessing.LoadExtensionSchemaContext(RootSchemaUri);

            var packets = ctx.FindClass(ExtensionRootClassName).GetField("packets");

            return ctx;
        }

        private static SchemaType.Context ProcessProps()
        {
            var ctx = SchemaProcessing.LoadExtensionSchemaContext(PropSchemaUri);

            return ctx;
        }

        public override void PrepareTypes(CodeGen.CSharpEmitter newEmitter, SchemaType.Context ctx)
        {
            newEmitter.SetRuntimeName(ExtensionRootClassName, "XmpPackets");
            newEmitter.SetRuntimeName(ExtensionPropClassName, "XmpPacketReference");
        }
    }
}
