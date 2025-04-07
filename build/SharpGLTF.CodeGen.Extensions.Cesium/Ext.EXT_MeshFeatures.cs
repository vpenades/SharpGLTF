using SharpGLTF.CodeGen;
using SharpGLTF.SchemaReflection;
using System;
using System.Collections.Generic;

namespace SharpGLTF
{
    class ExtMeshFeaturesExtension : SchemaProcessor
    {
        public override string GetTargetProject() { return CesiumExtensions.CesiumProjectDirectory; }
    
        private static string RootSchemaUri => CesiumExtensions.CustomExtensionsPath("EXT_mesh_features", "mesh.primitive.EXT_mesh_features.schema.json");
    
        const string ExtensionFeatureIdTextureName = "Feature ID Texture in EXT_mesh_features";
    
        public override void PrepareTypes(CSharpEmitter newEmitter, SchemaType.Context ctx)
        {
            newEmitter.SetRuntimeName("EXT_mesh_features glTF Mesh Primitive extension", "MeshExtMeshFeatures", CesiumExtensions.CesiumNameSpace);
            newEmitter.SetRuntimeName("Feature ID in EXT_mesh_features", "MeshExtMeshFeatureID", CesiumExtensions.CesiumNameSpace);
            newEmitter.SetRuntimeName(ExtensionFeatureIdTextureName, "MeshExtMeshFeatureIDTexture", CesiumExtensions.CesiumNameSpace);

            newEmitter.SetFieldToChildrenList(ctx, "EXT_mesh_features glTF Mesh Primitive extension", "featureIds");
        }
    
        public override IEnumerable<(string TargetFileName, SchemaType.Context Schema)> ReadSchema()
        {
            yield return ("Ext.CESIUM_ext_mesh_features.g", ProcessNode());
        }
    
        private static SchemaType.Context ProcessNode()
        {
            var ctx = SchemaProcessing.LoadSchemaContext(RootSchemaUri);
            ctx.IgnoredByCodeEmitter("glTF Property");
            ctx.IgnoredByCodeEmitter("glTF Child of Root Property");
            ctx.IgnoredByCodeEmitter("Texture Info");
    
            var fld = ctx.FindClass(ExtensionFeatureIdTextureName)
                .GetField("channels");
    
            // for now we simply remove the default value, it can be set
            // in the constructor or on demand when the APIs are Called.
            fld.RemoveDefaultValue();                
    
            return ctx;
        }        
    }
}
