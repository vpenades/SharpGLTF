using SharpGLTF.CodeGen;
using SharpGLTF.SchemaReflection;
using System.Collections.Generic;

namespace SharpGLTF
{
    class CesiumPrimitiveOutlineExtension : SchemaProcessor
    {
        public override string GetTargetProject() { return CesiumExtensions.CesiumProjectDirectory; }

        private static string RootSchemaUri => CesiumExtensions.VendorExtensionPath("CESIUM_primitive_outline", "primitive.CESIUM_primitive_outline.schema.json");

        public override void PrepareTypes(CSharpEmitter newEmitter, SchemaType.Context ctx)
        {
            newEmitter.SetRuntimeName("CESIUM_primitive_outline glTF primitive extension", "CesiumPrimitiveOutline", CesiumExtensions.CesiumNameSpace);
        }        

        public override IEnumerable<(string TargetFileName, SchemaType.Context Schema)> ReadSchema()
        {
            yield return ("Ext.CESIUM_primitive_outline.g", ProcessRoot());
        }

        private static SchemaType.Context ProcessRoot()
        {
            var ctx = SchemaProcessing.LoadExtensionSchemaContext(RootSchemaUri);

            return ctx;
        }
    }
}
