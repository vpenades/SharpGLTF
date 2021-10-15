using System;
using System.Collections.Generic;

using SharpGLTF.SchemaReflection;

namespace SharpGLTF
{
    class VolumeExtension : SchemaProcessor
    {
        private static string SchemaUri => Constants.KhronosExtensionPath("KHR_materials_volume", "glTF.KHR_materials_volume.schema.json");

        public override IEnumerable<(string, SchemaType.Context)> Process()
        {
            var ctx = SchemaProcessing.LoadSchemaContext(SchemaUri);
            ctx.IgnoredByCodeEmitter("glTF Property");
            ctx.IgnoredByCodeEmitter("glTF Child of Root Property");
            ctx.IgnoredByCodeEmitter("Texture Info");

            var cls = ctx.FindClass("KHR_materials_volume glTF extension");

            cls.GetField("attenuationColor")
                .SetDataType(typeof(System.Numerics.Vector3), true)
                .SetDefaultValue("Vector3.One")
                .SetItemsRange(0);

            yield return ("ext.MaterialsVolume.g", ctx);
        }

        public override void PrepareTypes(CodeGen.CSharpEmitter newEmitter, SchemaType.Context ctx)
        {
            newEmitter.SetRuntimeName("KHR_materials_volume glTF extension", "MaterialVolume");
        }
    }
}
