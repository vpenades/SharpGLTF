using System;
using System.Collections.Generic;
using System.Text;

using SharpGLTF.SchemaReflection;

namespace SharpGLTF
{
    class MeshGpuInstancingExtension : SchemaProcessor
    {
        private static string SchemaUri => KhronosExtensions.VendorExtensionPath("EXT_mesh_gpu_instancing", "glTF.EXT_mesh_gpu_instancing.schema.json");
        public override IEnumerable<(string, SchemaType.Context)> ReadSchema()
        {
            var ctx = SchemaProcessing.LoadExtensionSchemaContext(SchemaUri);

            yield return ("ext.MeshGpuInstancing.g", ctx);
        }

        public override void PrepareTypes(CodeGen.CSharpEmitter newEmitter, SchemaType.Context ctx)
        {
            newEmitter.SetRuntimeName("EXT_mesh_gpu_instancing glTF extension", "MeshGpuInstancing");
        }
    }
}
