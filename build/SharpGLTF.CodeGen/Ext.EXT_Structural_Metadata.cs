using SharpGLTF.CodeGen;
using SharpGLTF.SchemaReflection;
using System.Collections.Generic;

namespace SharpGLTF
{
    class ExtStructuralMetadataExtension : SchemaProcessor
    {
        public override string GetTargetProject() { return Constants.CesiumProjectDirectory; }

        const string ExtensionPropertyTexturePropertyName = "Property Texture Property in EXT_structural_metadata";

        private static string RootSchemaUri => Constants.CustomExtensionsPath("EXT_structural_metadata", "glTF.EXT_structural_metadata.schema.json");
        private static string MeshPrimitiveSchemaUri => Constants.CustomExtensionsPath("EXT_structural_metadata", "mesh.primitive.EXT_structural_metadata.schema.json");

        public override void PrepareTypes(CSharpEmitter newEmitter, SchemaType.Context ctx)
        {
            newEmitter.SetRuntimeName("EXT_structural_metadata glTF extension", "EXTStructuralMetaData");
            newEmitter.SetRuntimeName("Property Table in EXT_structural_metadata", "PropertyTable");
            newEmitter.SetRuntimeName("Schema in EXT_structural_metadata", "StructuralMetadataSchema");
            newEmitter.SetRuntimeName("Property Table Property in EXT_structural_metadata", "PropertyTableProperty");
            newEmitter.SetRuntimeName("Property Texture in EXT_structural_metadata", "PropertyTexture");
            newEmitter.SetRuntimeName("Property Texture Property in EXT_structural_metadata", "PropertyTextureProperty");
            newEmitter.SetRuntimeName("Property Attribute Property in EXT_structural_metadata", "PropertyAttributeProperty");
            newEmitter.SetRuntimeName("Class Property in EXT_structural_metadata", "ClassProperty");
            newEmitter.SetRuntimeName("Class in EXT_structural_metadata", "StructuralMetadataClass");
            newEmitter.SetRuntimeName("Enum Value in EXT_structural_metadata", "EnumValue");
            newEmitter.SetRuntimeName("Enum in EXT_structural_metadata", "StructuralMetadataEnum");

            newEmitter.SetRuntimeName("BOOLEAN-ENUM-MAT2-MAT3-MAT4-SCALAR-STRING-VEC2-VEC3-VEC4", "ElementType");
            newEmitter.SetRuntimeName("FLOAT32-FLOAT64-INT16-INT32-INT64-INT8-UINT16-UINT32-UINT64-UINT8", "DataType");
            newEmitter.SetRuntimeName("INT16-INT32-INT64-INT8-UINT16-UINT32-UINT64-UINT8", "IntegerType");
            newEmitter.SetRuntimeName("UINT16-UINT32-UINT64-UINT8", "StringOffsets");
        }

        public override IEnumerable<(string TargetFileName, SchemaType.Context Schema)> Process()
        {
            yield return ("Ext.CESIUM_ext_structural_metadata_root.g", ProcessRoot());
            yield return ("Ext.CESIUM_ext_structural_metadata_primitive.g", ProcessMeshPrimitive());
        }

        private static SchemaType.Context ProcessRoot()
        {
            var ctx = SchemaProcessing.LoadSchemaContext(RootSchemaUri);
            ctx.IgnoredByCodeEmitter("glTF Property");
            ctx.IgnoredByCodeEmitter("glTF Child of Root Property");
            ctx.IgnoredByCodeEmitter("Texture Info");
            var fld = ctx.FindClass(ExtensionPropertyTexturePropertyName).GetField("channels");

            // for now we simply remove the default value, it can be set
            // in the constructor or on demand when the APIs are Called.
            fld.RemoveDefaultValue();


            return ctx;
        }

        private static SchemaType.Context ProcessMeshPrimitive()
        {
            var ctx = SchemaProcessing.LoadSchemaContext(MeshPrimitiveSchemaUri);
            ctx.IgnoredByCodeEmitter("glTF Property");
            ctx.IgnoredByCodeEmitter("glTF Child of Root Property");
            ctx.IgnoredByCodeEmitter("Texture Info");
            return ctx;
        }


    }
}
