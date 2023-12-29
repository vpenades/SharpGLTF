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
            newEmitter.SetRuntimeName("EXT_structural_metadata glTF Mesh Primitive extension", "ExtStructuralMetadataMeshPrimitive", Constants.CesiumNameSpace);
            newEmitter.SetRuntimeName("EXT_structural_metadata glTF extension", "EXTStructuralMetadataRoot", Constants.CesiumNameSpace);
            newEmitter.SetRuntimeName("Property Table in EXT_structural_metadata", "PropertyTable", Constants.CesiumNameSpace);
            newEmitter.SetRuntimeName("Schema in EXT_structural_metadata", "StructuralMetadataSchema", Constants.CesiumNameSpace);
            newEmitter.SetRuntimeName("Property Table Property in EXT_structural_metadata", "PropertyTableProperty", Constants.CesiumNameSpace);
            newEmitter.SetRuntimeName("Property Texture in EXT_structural_metadata", "PropertyTexture", Constants.CesiumNameSpace);
            newEmitter.SetRuntimeName("Property Texture Property in EXT_structural_metadata", "PropertyTextureProperty", Constants.CesiumNameSpace);
            newEmitter.SetRuntimeName("Property Attribute Property in EXT_structural_metadata", "PropertyAttributeProperty", Constants.CesiumNameSpace);
            newEmitter.SetRuntimeName("Class Property in EXT_structural_metadata", "ClassProperty", Constants.CesiumNameSpace);
            newEmitter.SetRuntimeName("Class in EXT_structural_metadata", "StructuralMetadataClass", Constants.CesiumNameSpace);
            newEmitter.SetRuntimeName("Enum Value in EXT_structural_metadata", "EnumValue", Constants.CesiumNameSpace);
            newEmitter.SetRuntimeName("Enum in EXT_structural_metadata", "StructuralMetadataEnum", Constants.CesiumNameSpace);
            newEmitter.SetRuntimeName("Property Attribute in EXT_structural_metadata", "PropertyAttribute", Constants.CesiumNameSpace);

            newEmitter.SetRuntimeName("BOOLEAN-ENUM-MAT2-MAT3-MAT4-SCALAR-STRING-VEC2-VEC3-VEC4", "ElementType", Constants.CesiumNameSpace);
            newEmitter.SetRuntimeName("FLOAT32-FLOAT64-INT16-INT32-INT64-INT8-UINT16-UINT32-UINT64-UINT8", "DataType", Constants.CesiumNameSpace);
            newEmitter.SetRuntimeName("INT16-INT32-INT64-INT8-UINT16-UINT32-UINT64-UINT8", "IntegerType", Constants.CesiumNameSpace);
            newEmitter.SetRuntimeName("UINT16-UINT32-UINT64-UINT8", "ArrayOffsetType", Constants.CesiumNameSpace);

            newEmitter.SetFieldToChildrenList(ctx, "EXT_structural_metadata glTF extension", "propertyTables");
            newEmitter.SetFieldToChildrenList(ctx, "EXT_structural_metadata glTF extension", "propertyAttributes");
            newEmitter.SetFieldToChildrenList(ctx, "EXT_structural_metadata glTF extension", "propertyTextures");

            newEmitter.SetFieldToChildrenDictionary(ctx, "Property Texture in EXT_structural_metadata", "properties");

            newEmitter.SetFieldToChildrenDictionary(ctx, "Class in EXT_structural_metadata", "properties");

            newEmitter.SetFieldToChildrenDictionary(ctx, "Property Table in EXT_structural_metadata", "properties");

            newEmitter.SetFieldToChildrenDictionary(ctx, "Property Attribute in EXT_structural_metadata", "properties");

            newEmitter.SetFieldToChildrenDictionary(ctx, "Schema in EXT_structural_metadata", "classes");
            newEmitter.SetFieldToChildrenDictionary(ctx, "Schema in EXT_structural_metadata", "enums");
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
