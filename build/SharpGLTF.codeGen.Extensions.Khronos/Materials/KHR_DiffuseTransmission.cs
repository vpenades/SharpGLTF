using System;
using System.Collections.Generic;
using System.Text;

using SharpGLTF.SchemaReflection;

namespace SharpGLTF
{
    class DiffuseTransmissionExtension : SchemaProcessor
    {
        // it seems KHR_materials_diffuse_transmission has
        // file naming issues and it's subject to change anytime.
        // https://github.com/KhronosGroup/glTF/issues/2482

        private static string SchemaUri => KhronosExtensions.KhronosExtensionPath("KHR_materials_diffuse_transmission", "glTF.KHR_materials_diffuse_transmission.schema.json");

        private const string ExtensionRootClassName = "KHR_materials_diffuse_transmission glTF extension"; // not correctly named?

        public override IEnumerable<(string, SchemaType.Context)> ReadSchema()
        {
            var ctx = SchemaProcessing.LoadExtensionSchemaContext(SchemaUri);

            ctx.FindClass(ExtensionRootClassName)
                .GetField("diffuseTransmissionColorFactor")
                .SetDataType(typeof(System.Numerics.Vector3), true)
                .SetDefaultValue("Vector3.One")
                .SetItemsRange(0);

            yield return("ext.DiffuseTransmission.g", ctx);
        }

        public override void PrepareTypes(CodeGen.CSharpEmitter newEmitter, SchemaType.Context ctx)
        {
            newEmitter.SetRuntimeName(ExtensionRootClassName, "MaterialDiffuseTransmission");
        }
    }
}
