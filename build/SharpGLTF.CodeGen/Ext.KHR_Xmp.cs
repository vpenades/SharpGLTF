using System;
using System.Collections.Generic;
using System.Text;

using SharpGLTF.SchemaReflection;

namespace SharpGLTF
{
    class XMPExtension : SchemaProcessor
    {
        private static string RootSchemaUri => Constants.KhronosExtensionPath("KHR_xmp", "glTF.KHR_xmp.schema.json");
        private static string NodeSchemaUri => Constants.KhronosExtensionPath("KHR_xmp", "node.KHR_xmp.schema.json");

        public override IEnumerable<(string, SchemaType.Context)> Process()
        {
            // Model extension

            var ctx = SchemaProcessing.LoadSchemaContext(RootSchemaUri);
            ctx.IgnoredByCodeEmitter("glTF Property");
            ctx.IgnoredByCodeEmitter("glTF Child of Root Property");

            /*
            var jdict = ctx.UseClass("JsonDictionary");
            var jlist = ctx.UseClass("JsonList");

            ctx.FindClass("KHR_xmp glTF extension")
                .GetField("@context")
                .SetDataType(jdict);

            ctx.FindClass("KHR_xmp glTF extension")
                .GetField("packets")
                .SetDataType(jlist);*/

            /*
            ctx.FindClass("KHR_xmp glTF extension")
                .GetField("@context")
                .SetDataType(typeof(Dictionary<string,Object>), true);
            */

            yield return ("ext.XMP.Model.g", ctx);

            // Node extension

            ctx = SchemaProcessing.LoadSchemaContext(NodeSchemaUri);
            ctx.IgnoredByCodeEmitter("glTF Property");
            ctx.IgnoredByCodeEmitter("glTF Child of Root Property");

            yield return ("ext.XMP.Node.g", ctx);
        }

        public override void PrepareTypes(CodeGen.CSharpEmitter newEmitter, SchemaType.Context ctx)
        {
            newEmitter.SetRuntimeName("KHR_xmp glTF extension", "XMPPacketsCollection");
            newEmitter.SetRuntimeName("KHR_xmp node extension", "XMPPacketReference");
        }
    }
}
