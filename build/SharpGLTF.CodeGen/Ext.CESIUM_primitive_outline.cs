using SharpGLTF.CodeGen;
using SharpGLTF.SchemaReflection;
using System.Collections.Generic;

namespace SharpGLTF
{
    class CesiumPrimitiveOutlineExtension : SchemaProcessor
    {
        public override string GetTargetProject() { return Constants.CesiumProjectDirectory; }

        private static string RootSchemaUri => Constants.VendorExtensionPath("CESIUM_primitive_outline", "primitive.CESIUM_primitive_outline.schema.json");

        public override void PrepareTypes(CSharpEmitter newEmitter, SchemaType.Context ctx)
        {
            newEmitter.SetRuntimeName("CESIUM_primitive_outline glTF primitive extension", "CesiumPrimitiveOutline");
        }        

        public override IEnumerable<(string TargetFileName, SchemaType.Context Schema)> Process()
        {
            yield return ("Ext.CESIUM_primitive_outline.g", ProcessRoot());
        }

        private static SchemaType.Context ProcessRoot()
        {
            var ctx = SchemaProcessing.LoadSchemaContext(RootSchemaUri);
            ctx.IgnoredByCodeEmitter("glTF Property");
            ctx.IgnoredByCodeEmitter("glTF Child of Root Property");

            return ctx;
        }
    }
}
