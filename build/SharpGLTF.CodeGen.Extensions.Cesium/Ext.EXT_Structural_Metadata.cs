using SharpGLTF.CodeGen;
using SharpGLTF.SchemaReflection;
using System.Collections.Generic;

namespace SharpGLTF
{
    class ExtStructuralMetadataExtension : SchemaProcessor
    {
        public override string GetTargetProject() { return CesiumExtensions.CesiumProjectDirectory; }

        const string ExtensionPropertyTexturePropertyName = "Property Texture Property in EXT_structural_metadata";

        private static string RootSchemaUri => CesiumExtensions.CustomExtensionsPath("EXT_structural_metadata", "glTF.EXT_structural_metadata.schema.json");
        private static string MeshPrimitiveSchemaUri => CesiumExtensions.CustomExtensionsPath("EXT_structural_metadata", "mesh.primitive.EXT_structural_metadata.schema.json");

        public override IEnumerable<(string TargetFileName, SchemaType.Context Schema)> ReadSchema()
        {
            yield return ("Ext.CESIUM_ext_structural_metadata_root.g", ProcessRoot());
            yield return ("Ext.CESIUM_ext_structural_metadata_primitive.g", ProcessMeshPrimitive());
        }

        private static SchemaType.Context ProcessRoot()
        {
            var ctx = SchemaProcessing.LoadExtensionSchemaContext(RootSchemaUri);

            // for now we simply remove the default value, it can be set
            // in the constructor or on demand when the APIs are Called.
            var fld = ctx.FindClass(ExtensionPropertyTexturePropertyName).GetField("channels");
            fld.RemoveDefaultValue();

            return ctx;
        }

        private static SchemaType.Context ProcessMeshPrimitive()
        {
            var ctx = SchemaProcessing.LoadExtensionSchemaContext(MeshPrimitiveSchemaUri);
            return ctx;
        }



        public override void PrepareTypes(CSharpEmitter newEmitter, SchemaType.Context ctx)
        {
            newEmitter.SetRuntimeName("EXT_structural_metadata glTF Mesh Primitive extension", "ExtStructuralMetadataMeshPrimitive", CesiumExtensions.CesiumNameSpace);
            newEmitter.SetRuntimeName("EXT_structural_metadata glTF extension", "EXTStructuralMetadataRoot", CesiumExtensions.CesiumNameSpace);
            newEmitter.SetRuntimeName("Property Table in EXT_structural_metadata", "PropertyTable", CesiumExtensions.CesiumNameSpace);
            newEmitter.SetRuntimeName("Schema in EXT_structural_metadata", "StructuralMetadataSchema", CesiumExtensions.CesiumNameSpace);
            newEmitter.SetRuntimeName("Property Table Property in EXT_structural_metadata", "PropertyTableProperty", CesiumExtensions.CesiumNameSpace);
            newEmitter.SetRuntimeName("Property Texture in EXT_structural_metadata", "PropertyTexture", CesiumExtensions.CesiumNameSpace);
            newEmitter.SetRuntimeName("Property Texture Property in EXT_structural_metadata", "PropertyTextureProperty", CesiumExtensions.CesiumNameSpace);
            newEmitter.SetRuntimeName("Property Attribute Property in EXT_structural_metadata", "PropertyAttributeProperty", CesiumExtensions.CesiumNameSpace);
            newEmitter.SetRuntimeName("Class Property in EXT_structural_metadata", "StructuralMetadataClassProperty", CesiumExtensions.CesiumNameSpace);
            newEmitter.SetRuntimeName("Class in EXT_structural_metadata", "StructuralMetadataClass", CesiumExtensions.CesiumNameSpace);
            newEmitter.SetRuntimeName("Enum Value in EXT_structural_metadata", "StructuralMetadataEnumValue", CesiumExtensions.CesiumNameSpace);
            newEmitter.SetRuntimeName("Enum in EXT_structural_metadata", "StructuralMetadataEnum", CesiumExtensions.CesiumNameSpace);
            newEmitter.SetRuntimeName("Property Attribute in EXT_structural_metadata", "PropertyAttribute", CesiumExtensions.CesiumNameSpace);

            newEmitter.SetRuntimeName("BOOLEAN-ENUM-MAT2-MAT3-MAT4-SCALAR-STRING-VEC2-VEC3-VEC4", "ElementType", CesiumExtensions.CesiumNameSpace);
            newEmitter.SetRuntimeName("FLOAT32-FLOAT64-INT16-INT32-INT64-INT8-UINT16-UINT32-UINT64-UINT8", "DataType", CesiumExtensions.CesiumNameSpace);
            newEmitter.SetRuntimeName("INT16-INT32-INT64-INT8-UINT16-UINT32-UINT64-UINT8", "IntegerType", CesiumExtensions.CesiumNameSpace);
            newEmitter.SetRuntimeName("UINT16-UINT32-UINT64-UINT8", "ArrayOffsetType", CesiumExtensions.CesiumNameSpace);

            newEmitter.SetFieldToChildrenList(ctx, "EXT_structural_metadata glTF extension", "propertyTables");
            newEmitter.SetFieldToChildrenList(ctx, "EXT_structural_metadata glTF extension", "propertyAttributes");
            newEmitter.SetFieldToChildrenList(ctx, "EXT_structural_metadata glTF extension", "propertyTextures");

            newEmitter.SetFieldToChildrenDictionary(ctx, "Property Attribute in EXT_structural_metadata", "properties");
            newEmitter.SetFieldToChildrenDictionary(ctx, "Property Texture in EXT_structural_metadata", "properties");
            newEmitter.SetFieldToChildrenDictionary(ctx, "Property Table in EXT_structural_metadata", "properties");


            newEmitter.SetFieldToChildrenDictionary(ctx, "Class in EXT_structural_metadata", "properties");
            newEmitter.SetFieldToChildrenDictionary(ctx, "Schema in EXT_structural_metadata", "classes");

            newEmitter.SetFieldToChildrenList(ctx, "Enum in EXT_structural_metadata", "values");
            newEmitter.SetFieldToChildrenDictionary(ctx, "Schema in EXT_structural_metadata", "enums");
        }

        


    }
}
