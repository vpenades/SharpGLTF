using SharpGLTF.CodeGen;
using SharpGLTF.SchemaReflection;
using System;
using System.Collections.Generic;

namespace SharpGLTF
{
    class ExtMeshFeaturesExtension : SchemaProcessor
    {
        public override string GetTargetProject() { return Constants.CesiumProjectDirectory; }

        private static string RootSchemaUri => Constants.CustomExtensionsPath("EXT_mesh_features", "mesh.primitive.EXT_mesh_features.schema.json");
        public override void PrepareTypes(CSharpEmitter newEmitter, SchemaType.Context ctx)
        {
            newEmitter.SetRuntimeName("EXT_mesh_features glTF Mesh Primitive extension", "MeshExtMeshFeatures");
            newEmitter.SetRuntimeName("Feature ID in EXT_mesh_features", "FeatureID");

            // Todo Trying to fix the code generation for the FeatureIDTexture channel property

            // Schema: from featureIdTexture.schema.json
            //    "channels": {
            //        "type": "array",
            //    "items": {
            //            "type": "integer",
            //        "minimum": 0
            //    },
            //    "minItems": 1,
            //    "description": "Texture channels containing feature IDs, identified by index. Feature IDs may be packed into multiple channels if a single channel does not have sufficient bit depth to represent all feature ID values. The values are packed in little-endian order.",
            //    "default": [
            //        0
            //    ]
            //},


            // code generated
            //                    private static readonly Int32[] _channelsDefault = [
            //  0
            //];
            //        private const int _channelsMinItems = 1;
            //        private List<Int32> _channels = _channelsDefault;

            // should be?
            //private static readonly Int32[] _channelsDefault = new Int32[1] { 0 };
            //private const int _channelsMinItems = 1;
            //private Int32[] _channels = _channelsDefault;

            // attempts

            //var FeatureIdTextureClass = ctx.FindClass("Feature ID Texture in EXT_mesh_features")
            //    .GetField("channels")
            //    .SetDefaultValue("new List<Int32>()");
            // BT does not work :-( .SetDefaultValue("List<Int32>")
        }

        public override IEnumerable<(string TargetFileName, SchemaType.Context Schema)> Process()
        {
            yield return ("Ext.CESIUM_ext_mesh_features.g", ProcessRoot());
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