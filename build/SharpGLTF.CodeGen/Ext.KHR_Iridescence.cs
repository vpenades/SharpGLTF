using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using SharpGLTF.SchemaReflection;

namespace SharpGLTF
{
    class IridescenceExtension : SchemaProcessor
    {
        private static string SchemaUri => Constants.KhronosExtensionPath("KHR_materials_iridescence", "material.KHR_materials_iridescence.schema.json");

        private const string ExtensionRootClassName = "KHR_materials_iridescence glTF Material Extension";

        public override IEnumerable<(string, SchemaType.Context)> Process()
        {
            var ctx = SchemaProcessing.LoadSchemaContext(SchemaUri);
            ctx.IgnoredByCodeEmitter("glTF Property");
            ctx.IgnoredByCodeEmitter("glTF Child of Root Property");
            ctx.IgnoredByCodeEmitter("Texture Info");
            ctx.IgnoredByCodeEmitter("Material Normal Texture Info");            

            yield return ("ext.Iridescence.g", ctx);
        }

        public override void PrepareTypes(CodeGen.CSharpEmitter newEmitter, SchemaType.Context ctx)
        {
            newEmitter.SetRuntimeName(ExtensionRootClassName, "MaterialIridescence");
        }
    }
}
