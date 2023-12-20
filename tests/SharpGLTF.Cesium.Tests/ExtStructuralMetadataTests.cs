using NUnit.Framework;
using SharpGLTF.Geometry;
using SharpGLTF.Geometry.VertexTypes;
using SharpGLTF.Materials;
using SharpGLTF.Scenes;
using SharpGLTF.Schema2;
using SharpGLTF.Validation;
using System;
using System.Collections.Generic;
using System.Numerics;

namespace SharpGLTF.Cesium
{
    using VBTexture1 = VertexBuilder<VertexPosition, VertexTexture1, VertexEmpty>;

    [Category("Toolkit.Scenes")]
    public class ExtStructuralMetadataTests
    {
        [SetUp]
        public void SetUp()
        {
            CesiumExtensions.RegisterExtensions();
        }

        [Test(Description = "ext_structural_metadata with FeatureId Texture and Property Table")]
        // sample see https://github.com/CesiumGS/3d-tiles-samples/tree/main/glTF/EXT_structural_metadata/FeatureIdTextureAndPropertyTable
        public void FeatureIdTextureAndPropertytableTest()
        {
            TestContext.CurrentContext.AttachGltfValidatorLinks();

            var img0 = "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";
            var imageBytes0 = Convert.FromBase64String(img0);
            var imageBuilder0 = ImageBuilder.From(imageBytes0);

            var img1 = "iVBORw0KGgoAAAANSUhEUgAAABAAAAAQCAYAAAAf8/9hAAAAJElEQVR42mNgYmBgoAQzDLwBgwcwY8FDzIDBDRiR8KgBNDAAAOKBAKByX2jMAAAAAElFTkSuQmCC";
            var imageBytes1 = Convert.FromBase64String(img1);
            var imageBuilder1 = ImageBuilder.From(imageBytes1);

            var material = MaterialBuilder
                .CreateDefault()
                .WithMetallicRoughnessShader()
                .WithBaseColor(imageBuilder0, new Vector4(1, 1, 1, 1))
                .WithDoubleSide(true)
                .WithAlpha(Materials.AlphaMode.OPAQUE)
                .WithMetallicRoughness(0, 1)
                .WithSpecularFactor(imageBuilder1, 0);
            ;

            var mesh = VBTexture1.CreateCompatibleMesh("mesh");
            var prim = mesh.UsePrimitive(material);
            prim.AddTriangle(
                new VBTexture1(new VertexPosition(0, 0, 0), new Vector2(0, 1)),
                new VBTexture1(new VertexPosition(1, 0, 0), new Vector2(1, 1)),
                new VBTexture1(new VertexPosition(0, 1, 0), new Vector2(0, 0)));

            prim.AddTriangle(
                new VBTexture1(new VertexPosition(1, 0, 0), new Vector2(1, 1)),
                new VBTexture1(new VertexPosition(1, 1, 0), new Vector2(1, 0)),
                new VBTexture1(new VertexPosition(0, 1, 0), new Vector2(0, 0)));

            var scene = new SceneBuilder();
            scene.AddRigidMesh(mesh, Matrix4x4.Identity);
            var model = scene.ToGltf2();

            var schema = new StructuralMetadataSchema();
            schema.Id = "FeatureIdTextureAndPropertyTableSchema";

            var buildingComponentsClass = new StructuralMetadataClass();
            buildingComponentsClass.Name = "Building components";
            buildingComponentsClass.Properties.Add("component", new ClassProperty() { Name = "Component", Type = ElementType.STRING });
            buildingComponentsClass.Properties.Add("yearBuilt", new ClassProperty() { Name = "Year built", Type = ElementType.SCALAR, ComponentType = DataType.INT16 });
            schema.Classes.Add ("buildingComponents", buildingComponentsClass);

            var propertyTable = new PropertyTable();
            propertyTable.Name = "Example property table";
            propertyTable.Class = "buildingComponents";
            propertyTable.Count = 4;

            var componentProperty = model.GetPropertyTableProperty(new List<string>() { "Wall", "Door", "Roof", "Window" });
            var yearBuiltProperty = model.GetPropertyTableProperty(new List<Int16>() { 1960, 1996, 1985, 2002});
            propertyTable.Properties.Add("component", componentProperty);
            propertyTable.Properties.Add("yearBuilt", yearBuiltProperty);

            model.SetPropertyTable(propertyTable, schema);

            // Set the FeatureIds, pointing to the red channel of the texture
            var texture = new MeshExtMeshFeatureIDTexture(new List<int>() { 0 }, 1, 0);
            var featureIdTexture = new MeshExtMeshFeatureID(4, texture: texture, propertyTable: 0);
            var featureIds = new List<MeshExtMeshFeatureID>() { featureIdTexture };
            
            var primitive = model.LogicalMeshes[0].Primitives[0];
            primitive.SetFeatureIds(featureIds);

            var ctx = new ValidationResult(model, ValidationMode.Strict, true);
            model.AttachToCurrentTest("cesium_ext_structural_metadata_featureid_texture_and_property_table.glb");
            model.AttachToCurrentTest("cesium_ext_structural_metadata_featureid_texture_and_property_table.gltf");
            model.AttachToCurrentTest("cesium_ext_structural_metadata_featureid_texture_and_property_table.plotly");
        }


        [Test(Description = "ext_structural_metadata with simple property texture")]
        // sample see https://github.com/CesiumGS/3d-tiles-samples/tree/main/glTF/EXT_structural_metadata/SimplePropertyTexture
        public void SimplePropertyTextureTest()
        {
            TestContext.CurrentContext.AttachGltfValidatorLinks();

            // Bitmap of 16*16 pixels, containing FeatureID's (0, 1, 2, 3) in the red channel
            // var img0 = "AAABAAIAAQADAAIAAAAAAAAAAAAAAAAAAACAPwAAAAAAAAAAAAAAAAAAgD8AAAAAAACAPwAAgD8AAAAAAAAAAAAAAAAAAIA/AAAAAAAAAAAAAIA/AAAAAAAAAAAAAIA/AAAAAAAAAAAAAIA/AAAAAAAAgD8AAIA/AACAPwAAAAAAAAAAAACAPwAAAAA=";
            var img0 = "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";
            var imageBytes0 = Convert.FromBase64String(img0);
            var imageBuilder0 = ImageBuilder.From(imageBytes0);

            var img1 = "iVBORw0KGgoAAAANSUhEUgAAABAAAAAQCAYAAAAf8/9hAAABLUlEQVR42mVSSxbDIAh0GzUxKZrmCF3n/oerIx9pupgHIswAGtblE7bIKN0vqSOyXSOjPLAtktv9sCFxmcXj7EgsFj8zN00yYxrBZZJBRYk2LdC4WCDUfAdab7bpDm1lCyBW+7lpDnyNS34gcTQRltTPbAeEdFjcSQ0X9EOhGPYjhgLA7xh3kjxEEpMj1qQj7iAzAYoPELzYtuwK02M06WywAFDfX1MdJEoOtSZ7Allz1mYmWZDNL0pNF6ezu9jsQJUcNK7qzbWvMdSYQ8Jo7KKK8/uo4dxreHe0/HgF2/IqBen/za+Di69Sf8cZz5jmk+hcuhdd2tWLz8IE5MbFnRWT+yyU5vZJRtAOqlvq6MDeOrstu0UidsoO0Ak9xGwE+67+34salNEBSCxX7Bexg0rbq6TFvwAAAABJRU5ErkJggg==";
            var imageBytes1 = Convert.FromBase64String(img1);
            var imageBuilder1 = ImageBuilder.From(imageBytes1);

            var material = MaterialBuilder
                .CreateDefault()
                .WithMetallicRoughnessShader()
                .WithBaseColor(imageBuilder0, new Vector4(1, 1, 1, 1))
                .WithDoubleSide(true)
                .WithAlpha(Materials.AlphaMode.OPAQUE)
                .WithMetallicRoughness(0, 1);

            var mesh = VBTexture1.CreateCompatibleMesh("mesh");
            var prim = mesh.UsePrimitive(material);
            prim.AddTriangle(
                new VBTexture1(new VertexPosition(0, 0, 0), new Vector2(0, 1)),
                new VBTexture1(new VertexPosition(1, 0, 0), new Vector2(1, 1)),
                new VBTexture1(new VertexPosition(0, 1, 0), new Vector2(0, 0)));

            prim.AddTriangle(
                new VBTexture1(new VertexPosition(1, 0, 0), new Vector2(1, 1)),
                new VBTexture1(new VertexPosition(1, 1, 0), new Vector2(1, 0)),
                new VBTexture1(new VertexPosition(0, 1, 0), new Vector2(0, 0)));

            var scene = new SceneBuilder();
            scene.AddRigidMesh(mesh, Matrix4x4.Identity);
            var model = scene.ToGltf2();
            model.UseImage(imageBuilder1.Content);

            var schema = new StructuralMetadataSchema();

            schema.Id = "SimplePropertyTextureSchema";

            var exampleMetadataClass = new StructuralMetadataClass();
            exampleMetadataClass.Name = "Building properties";

            var insideTemperatureProperty = new ClassProperty();
            insideTemperatureProperty.Name = "Inside temperature";
            insideTemperatureProperty.Type = ElementType.SCALAR;
            insideTemperatureProperty.ComponentType = DataType.UINT8;

            var outsideTemperatureProperty = new ClassProperty();
            outsideTemperatureProperty.Name = "Outside temperature";
            outsideTemperatureProperty.Type = ElementType.SCALAR;
            outsideTemperatureProperty.ComponentType = DataType.UINT8;

            var insulationProperty = new ClassProperty();
            insulationProperty.Name = "Insulation Thickness";
            insulationProperty.Type = ElementType.SCALAR;
            insulationProperty.ComponentType = DataType.UINT8;
            insideTemperatureProperty.Normalized = true;

            exampleMetadataClass.Properties.Add("insideTemperature", insideTemperatureProperty);
            exampleMetadataClass.Properties.Add("outsideTemperature", outsideTemperatureProperty);
            exampleMetadataClass.Properties.Add("insulation", insulationProperty);

            schema.Classes.Add("buildingComponents", exampleMetadataClass);

            var buildingPropertyTexture = new PropertyTexture();
            buildingPropertyTexture.Class = "buildingComponents";

            var insideTemperatureTextureProperty = new PropertyTextureProperty();
            insideTemperatureTextureProperty._LogicalTextureIndex = 1;
            insideTemperatureTextureProperty.TextureCoordinate = 0;
            insideTemperatureTextureProperty.Channels = new List<int>() { 0 };

            buildingPropertyTexture.Properties.Add("insideTemperature", insideTemperatureTextureProperty);

            var outsideTemperatureTextureProperty = new PropertyTextureProperty();
            outsideTemperatureTextureProperty._LogicalTextureIndex = 1;
            outsideTemperatureTextureProperty.TextureCoordinate = 0;
            outsideTemperatureTextureProperty.Channels = new List<int>() { 1 };

            buildingPropertyTexture.Properties.Add("outsideTemperature", outsideTemperatureTextureProperty);

            var insulationTextureProperty = new PropertyTextureProperty();
            insulationTextureProperty._LogicalTextureIndex = 1;
            insulationTextureProperty.TextureCoordinate = 0;
            insulationTextureProperty.Channels = new List<int>() { 2 };

            buildingPropertyTexture.Properties.Add("insulation", insulationTextureProperty);

            model.SetPropertyTexture(buildingPropertyTexture, schema);
            // todo: set the textures on the primitive
            // model.LogicalMeshes[0].Primitives[0].SetPropertyTextures(new List<uint>() { 0 });

            var ctx = new ValidationResult(model, ValidationMode.Strict, true);
            model.AttachToCurrentTest("cesium_ext_structural_metadata_simple_property_texture.glb");
            model.AttachToCurrentTest("cesium_ext_structural_metadata_simple_property_texture.gltf");
            model.AttachToCurrentTest("cesium_ext_structural_metadata_simple_property_texture.plotly");
        }

        [Test(Description = "ext_structural_metadata with Multiple Feature IDs and Properties")]
        // sample see https://github.com/CesiumGS/3d-tiles-samples/tree/main/glTF/EXT_structural_metadata/MultipleFeatureIdsAndProperties
        public void MultipleFeatureIdsandPropertiesTest()
        {
            TestContext.CurrentContext.AttachGltfValidatorLinks();
            var material = MaterialBuilder.CreateDefault().WithDoubleSide(true);
            var mesh = new MeshBuilder<VertexPosition, VertexWithFeatureIds, VertexEmpty>("mesh");
            var prim = mesh.UsePrimitive(material);

            // first triangle has _feature_id_0 = 0 and _feature_id_1 = 1
            var vt0 = VertexBuilder.GetVertexWithFeatureIds(new Vector3(0, 0, 0), new Vector3(0, 0, 1), 0, 1);
            var vt1 = VertexBuilder.GetVertexWithFeatureIds(new Vector3(1, 0, 0), new Vector3(0, 0, 1), 0, 1);
            var vt2 = VertexBuilder.GetVertexWithFeatureIds(new Vector3(0, 1, 0), new Vector3(0, 0, 1), 0, 1);

            // second triangle has _feature_id_0 = 1 and _feature_id_1 = 0
            var vt3 = VertexBuilder.GetVertexWithFeatureIds(new Vector3(1, 1, 0), new Vector3(0, 0, 1), 1, 0);
            var vt4 = VertexBuilder.GetVertexWithFeatureIds(new Vector3(0, 0, 0), new Vector3(0, 0, 1), 1, 0);
            var vt5 = VertexBuilder.GetVertexWithFeatureIds(new Vector3(1, 0, 0), new Vector3(0, 0, 1), 1, 0);

            prim.AddTriangle(vt0, vt1, vt2);
            prim.AddTriangle(vt3, vt4, vt5);

            var scene = new SceneBuilder();
            scene.AddRigidMesh(mesh, Matrix4x4.Identity);

            var model = scene.ToGltf2();

            var featureId0 = new MeshExtMeshFeatureID(2, 0, 0);
            var featureId1 = new MeshExtMeshFeatureID(2, 1, 0);
            var featureIds = new List<MeshExtMeshFeatureID>() { featureId0, featureId1 };

            model.LogicalMeshes[0].Primitives[0].SetFeatureIds(featureIds);

            var schema = new StructuralMetadataSchema();
            schema.Id = "MultipleFeatureIdsAndPropertiesSchema";

            var exampleMetadataClass = new StructuralMetadataClass();
            exampleMetadataClass.Name = "Example metadata class";
            exampleMetadataClass.Description = "An example metadata class";

            var vector3Property = new ClassProperty();
            vector3Property.Name = "Example VEC3 FLOAT32 property";
            vector3Property.Description = "An example property, with type VEC3, with component type FLOAT32";
            vector3Property.Type = ElementType.VEC3;
            vector3Property.ComponentType = DataType.FLOAT32;

            exampleMetadataClass.Properties.Add("example_VEC3_FLOAT32", vector3Property);

            var stringProperty = new ClassProperty();
            stringProperty.Name = "Example STRING property";
            stringProperty.Description = "An example property, with type STRING";
            stringProperty.Type = ElementType.STRING;

            exampleMetadataClass.Properties.Add("example_STRING", stringProperty);

            schema.Classes.Add("exampleMetadataClass", exampleMetadataClass);

            var vector3List = new List<Vector3>() { 
                new Vector3(3, 3.0999999046325684f, 3.200000047683716f),
                new Vector3(103, 103.0999999046325684f, 103.200000047683716f)

            };

            var vector3PropertyTableProperty = model.GetPropertyTableProperty(vector3List);

            var examplePropertyTable = new PropertyTable("exampleMetadataClass", 2, "Example property table");

            examplePropertyTable.Properties.Add("example_VEC3_FLOAT32", vector3PropertyTableProperty);

            var stringList = new List<string>() { "Rain 🌧", "Thunder ⛈" };

            var stringPropertyTableProperty = model.GetPropertyTableProperty(stringList);

            examplePropertyTable.Properties.Add("example_STRING", stringPropertyTableProperty);

            model.SetPropertyTable(examplePropertyTable, schema);

            var ctx = new ValidationResult(model, ValidationMode.Strict, true);
            model.AttachToCurrentTest("cesium_ext_structural_metadata_featureid_attribute_and_property_table.glb");
            model.AttachToCurrentTest("cesium_ext_structural_metadata_featureid_attribute_and_property_table.gltf");
            model.AttachToCurrentTest("cesium_ext_structural_metadata_featureid_attribute_and_property_table.plotly");
        }

        [Test(Description = "ext_structural_metadata with FeatureIdAttributeAndPropertyTable")]
        // sample see https://github.com/CesiumGS/3d-tiles-samples/tree/main/glTF/EXT_structural_metadata/FeatureIdAttributeAndPropertyTable
        public void FeatureIdAndPropertyTableTest()
        {
            TestContext.CurrentContext.AttachGltfValidatorLinks();
            var material = MaterialBuilder.CreateDefault().WithDoubleSide(true);
            var mesh = new MeshBuilder<VertexPosition, VertexWithFeatureId, VertexEmpty>("mesh");
            var prim = mesh.UsePrimitive(material);

            // All the vertices in the triangle have the same feature ID
            var vt0 = VertexBuilder.GetVertexWithFeatureId(new Vector3(-1, 0, 0), new Vector3(0, 0, 1), 0);
            var vt1 = VertexBuilder.GetVertexWithFeatureId(new Vector3(1, 0, 0), new Vector3(0, 0, 1), 0);
            var vt2 = VertexBuilder.GetVertexWithFeatureId(new Vector3(0, 1, 0), new Vector3(0, 0, 1), 0);

            prim.AddTriangle(vt0, vt1, vt2);

            var scene = new SceneBuilder();
            scene.AddRigidMesh(mesh, Matrix4x4.Identity);

            var model = scene.ToGltf2();

            var featureId = new MeshExtMeshFeatureID(1, 0, 0);
            model.LogicalMeshes[0].Primitives[0].SetFeatureId(featureId);

            var schema = new StructuralMetadataSchema();
            schema.Id = "FeatureIdAttributeAndPropertyTableSchema";

            var exampleMetadataClass = new StructuralMetadataClass();
            exampleMetadataClass.Name = "Example metadata class";
            exampleMetadataClass.Description = "An example metadata class";

            var vector3Property = new ClassProperty();
            vector3Property.Name = "Example VEC3 FLOAT32 property";
            vector3Property.Description = "An example property, with type VEC3, with component type FLOAT32";
            vector3Property.Type = ElementType.VEC3;
            vector3Property.ComponentType = DataType.FLOAT32;

            exampleMetadataClass.Properties.Add("example_VEC3_FLOAT32", vector3Property);

            var matrix4x4Property = new ClassProperty();
            matrix4x4Property.Name = "Example MAT4 FLOAT32 property";
            matrix4x4Property.Description = "An example property, with type MAT4, with component type FLOAT32";
            matrix4x4Property.Type = ElementType.MAT4;
            matrix4x4Property.ComponentType = DataType.FLOAT32;

            exampleMetadataClass.Properties.Add("example_MAT4_FLOAT32", matrix4x4Property);

            schema.Classes.Add("exampleMetadataClass", exampleMetadataClass);

            var vector3List = new List<Vector3>() { new Vector3(3, 3.0999999046325684f, 3.200000047683716f) };

            var vector3PropertyTableProperty = model.GetPropertyTableProperty(vector3List);

            var examplePropertyTable = new PropertyTable("exampleMetadataClass", 1, "Example property table");

            examplePropertyTable.Properties.Add("example_VEC3_FLOAT32", vector3PropertyTableProperty);

            var matrix4x4List = new List<Matrix4x4>() { Matrix4x4.Identity };

            var matrix4x4PropertyTableProperty = model.GetPropertyTableProperty(matrix4x4List);

            examplePropertyTable.Properties.Add("example_MAT4_FLOAT32", matrix4x4PropertyTableProperty);
            
            model.SetPropertyTable(examplePropertyTable, schema);

            var ctx = new ValidationResult(model, ValidationMode.Strict, true);
            model.AttachToCurrentTest("cesium_ext_structural_metadata_multiple_featureids_and_properties.glb");
            model.AttachToCurrentTest("cesium_ext_structural_metadata_multiple_featureids_and_properties.gltf");
            model.AttachToCurrentTest("cesium_ext_structural_metadata_multiple_featureids_and_properties.plotly");
        }

        [Test(Description = "ext_structural_metadata with complex types")]
        // sample see https://github.com/CesiumGS/3d-tiles-samples/blob/main/glTF/EXT_structural_metadata/ComplexTypes/
        public void ComplexTypesTest()
        {
            TestContext.CurrentContext.AttachGltfValidatorLinks();
            var material = MaterialBuilder.CreateDefault().WithDoubleSide(true);
            var mesh = new MeshBuilder<VertexPosition, VertexWithFeatureId, VertexEmpty>("mesh");
            var prim = mesh.UsePrimitive(material);

            // All the vertices in the triangle have the same feature ID
            var vt0 = VertexBuilder.GetVertexWithFeatureId(new Vector3(-1, 0, 0), new Vector3(0, 0, 1), 0);
            var vt1 = VertexBuilder.GetVertexWithFeatureId(new Vector3(1, 0, 0), new Vector3(0, 0, 1), 0);
            var vt2 = VertexBuilder.GetVertexWithFeatureId(new Vector3(0, 1, 0), new Vector3(0, 0, 1), 0);

            prim.AddTriangle(vt0, vt1, vt2);

            var scene = new SceneBuilder();
            scene.AddRigidMesh(mesh, Matrix4x4.Identity);

            var model = scene.ToGltf2();

            var featureId = new MeshExtMeshFeatureID(1, 0, 0);
            model.LogicalMeshes[0].Primitives[0].SetFeatureId(featureId);

            var schema = new StructuralMetadataSchema();

            var exampleMetadataClass = new StructuralMetadataClass();
            exampleMetadataClass.Name = "Example metadata class A";
            exampleMetadataClass.Description = "First example metadata class";

            // class properties
            
            var uint8ArrayProperty = new ClassProperty();
            uint8ArrayProperty.Name = "Example variable-length ARRAY normalized INT8 property";
            uint8ArrayProperty.Description = "An example property, with type ARRAY, with component type UINT8, normalized, and variable length";
            uint8ArrayProperty.Type = ElementType.SCALAR;
            uint8ArrayProperty.ComponentType = DataType.UINT8;
            uint8ArrayProperty.Normalized = false;
            uint8ArrayProperty.Array = true;

            exampleMetadataClass.Properties.Add("example_variable_length_ARRAY_normalized_UINT8", uint8ArrayProperty);

            var fixedLengthBooleanProperty = new ClassProperty();
            fixedLengthBooleanProperty.Name = "Example fixed-length ARRAY BOOLEAN property";
            fixedLengthBooleanProperty.Description = "An example property, with type ARRAY, with component type BOOLEAN, and fixed length ";
            fixedLengthBooleanProperty.Type = ElementType.BOOLEAN;
            fixedLengthBooleanProperty.Array = true;
            fixedLengthBooleanProperty.Count = 4;

            exampleMetadataClass.Properties.Add("example_fixed_length_ARRAY_BOOLEAN", fixedLengthBooleanProperty);

            var variableLengthStringArrayProperty = new ClassProperty();
            variableLengthStringArrayProperty.Name = "Example variable-length ARRAY STRING property";
            variableLengthStringArrayProperty.Description = "An example property, with type ARRAY, with component type STRING, and variable length";
            variableLengthStringArrayProperty.Type = ElementType.STRING;
            variableLengthStringArrayProperty.Array = true;
            exampleMetadataClass.Properties.Add("example_variable_length_ARRAY_STRING", variableLengthStringArrayProperty);

            var fixed_length_ARRAY_ENUM = new ClassProperty();
            fixed_length_ARRAY_ENUM.Name = "Example fixed-length ARRAY ENUM property";
            fixed_length_ARRAY_ENUM.Description = "An example property, with type ARRAY, with component type ENUM, and fixed length";
            fixed_length_ARRAY_ENUM.Type = ElementType.ENUM;
            fixed_length_ARRAY_ENUM.Array = true;
            fixed_length_ARRAY_ENUM.Count = 2;
            fixed_length_ARRAY_ENUM.EnumType = "exampleEnumType";

            exampleMetadataClass.Properties.Add("example_fixed_length_ARRAY_ENUM", fixed_length_ARRAY_ENUM);

            schema.Classes.Add("exampleMetadataClass", exampleMetadataClass);

            // enums

            var exampleEnum = new StructuralMetadataEnum();
            exampleEnum.Values.Add(new EnumValue() { Name = "ExampleEnumValueA", Value = 0 });
            exampleEnum.Values.Add(new EnumValue() { Name = "ExampleEnumValueB", Value = 1 });
            exampleEnum.Values.Add(new EnumValue() { Name = "ExampleEnumValueC", Value = 2 });

            schema.Enums.Add("exampleEnumType", exampleEnum);

            // property tables

            var examplePropertyTable = new PropertyTable("exampleMetadataClass", 1, "Example property table");
            var list2 = new List<List<byte>>() {
                new() { 0, 1, 2, 3, 4, 5, 6, 7 }
            };

            var property = model.GetArrayPropertyTableProperty(list2);
            examplePropertyTable.Properties.Add("example_variable_length_ARRAY_normalized_UINT8", property);

            var booleansList = new List<List<bool>>()
            {
                new() { true, false, true, false }
            };
            var propertyBooleansList = model.GetArrayPropertyTableProperty(booleansList, false);
            examplePropertyTable.Properties.Add("example_fixed_length_ARRAY_BOOLEAN", propertyBooleansList);

            var stringsList = new List<List<string>>()
            {
                new() { "Example string 1", "Example string 2", "Example string 3" }
            };

            var propertyStringsList = model.GetArrayPropertyTableProperty(stringsList);
            examplePropertyTable.Properties.Add("example_variable_length_ARRAY_STRING", propertyStringsList);

            var enumsList = new List<List<int>>()
            {
                new() { 0, 1 }
            };

            var enumsProperty = model.GetArrayPropertyTableProperty(enumsList, false);
            examplePropertyTable.Properties.Add("example_fixed_length_ARRAY_ENUM", enumsProperty);

            model.SetPropertyTable(examplePropertyTable, schema);

            var ctx = new ValidationResult(model, ValidationMode.Strict, true);
            model.AttachToCurrentTest("cesium_ext_structural_metadata_complex_types.glb");
            model.AttachToCurrentTest("cesium_ext_structural_metadata_complex_types.gltf");
            model.AttachToCurrentTest("cesium_ext_structural_metadata_complex_types.plotly");
        }

        [Test(Description = "ext_structural_metadata with multiple classes")]
        // Sample see https://github.com/CesiumGS/3d-tiles-samples/blob/main/glTF/EXT_structural_metadata/MultipleClasses/
        public void MultipleClassesTest()
        {
            var material = MaterialBuilder.CreateDefault().WithDoubleSide(true);

            var mesh = new MeshBuilder<VertexPositionNormal, VertexWithFeatureIds, VertexEmpty>("mesh");
            var prim = mesh.UsePrimitive(material);

            // All the vertices in the triangle have the same feature ID
            var vt0 = VertexBuilder.GetVertexWithFeatureIds(new Vector3(-10, 0, 0), new Vector3(0, 0, 1), 0, 0);
            var vt1 = VertexBuilder.GetVertexWithFeatureIds(new Vector3(10, 0, 0), new Vector3(0, 0, 1), 0, 0);
            var vt2 = VertexBuilder.GetVertexWithFeatureIds(new Vector3(0, 10, 0), new Vector3(0, 0, 1), 0, 0);

            prim.AddTriangle(vt0, vt1, vt2);
            var scene = new SceneBuilder();
            scene.AddRigidMesh(mesh, Matrix4x4.Identity);
            var model = scene.ToGltf2();

            // FeatureID 0: featureCount=1, attribute=0, porpertyTable=0 
            var featureId0Attribute = new MeshExtMeshFeatureID(1, 0, 0);
            // FeatureID 1: featureCount=1, attribute=1, porpertyTable=1
            var featureId1Attribute = new MeshExtMeshFeatureID(1, 1, 1);

            // Set the FeatureIds
            var featureIds = new List<MeshExtMeshFeatureID>() { featureId0Attribute, featureId1Attribute };
            model.LogicalMeshes[0].Primitives[0].SetFeatureIds(featureIds);

            var schema = new StructuralMetadataSchema();
            schema.Id = "MultipleClassesSchema";

            var classes = new Dictionary<string, StructuralMetadataClass>();
            classes["exampleMetadataClassA"] = GetExampleClassA();
            classes["exampleMetadataClassB"] = GetExampleClassB();

            schema.Classes = classes;
            var exampleEnum = new StructuralMetadataEnum();
            exampleEnum.Values.Add(new EnumValue() { Name = "ExampleEnumValueA", Value = 0 });
            exampleEnum.Values.Add(new EnumValue() { Name = "ExampleEnumValueB", Value = 1 });
            exampleEnum.Values.Add(new EnumValue() { Name = "ExampleEnumValueC", Value = 2 });

            schema.Enums.Add("exampleEnumType", exampleEnum);

            var firstPropertyTable = GetFirstPropertyTable(model);
            var secondPropertyTable = GetSecondPropertyTable(model);

            var propertyTables = new List<PropertyTable>() { firstPropertyTable, secondPropertyTable };
            model.SetPropertyTables(propertyTables, schema);

            var ctx = new ValidationResult(model, ValidationMode.Strict, true);
            model.AttachToCurrentTest("cesium_ext_structural_metadata_multiple_classes.glb");
            model.AttachToCurrentTest("cesium_ext_structural_metadata_multiple_classes.gltf");
            model.AttachToCurrentTest("cesium_ext_structural_metadata_multiple_classes.plotly");
        }

        [Test(Description = "ext_structural_metadata with pointcloud and custom attributes")]
        // Sample see https://github.com/CesiumGS/3d-tiles-samples/blob/main/glTF/EXT_structural_metadata/PropertyAttributesPointCloud/

        public void CreatePointCloudWithCustomAttributesTest()
        {
            var material = new MaterialBuilder("material1").WithUnlitShader();
            var mesh = new MeshBuilder<VertexPosition, VertexPointcloud, VertexEmpty>("mesh");
            var pointCloud = mesh.UsePrimitive(material, 1);
            var redColor = new Vector4(1f, 0f, 0f, 1f);
            var rand = new Random();
            for (var x = -10; x < 10; x++)
            {
                for (var y = -10; y < 10; y++)
                {
                    for (var z = -10; z < 10; z++)
                    {
                        // intensity values is based on x-axis values
                        // classification of points is 0 or 1 (random)
                        var vt0 = VertexBuilder.GetVertexPointcloud(new Vector3(x, y, z), redColor, x, rand.Next(0, 2));

                        pointCloud.AddPoint(vt0);
                    }
                }
            }
            var model = ModelRoot.CreateModel();
            model.CreateMeshes(mesh);

            // create a scene, a node, and assign the first mesh (the terrain)
            model.UseScene("Default")
                .CreateNode().WithMesh(model.LogicalMeshes[0]);

            var propertyAttribute = new Schema2.PropertyAttribute();
            propertyAttribute.Class = "exampleMetadataClass";
            var intensityProperty = new PropertyAttributeProperty();
            intensityProperty.Attribute = "_INTENSITY";
            var classificationProperty = new PropertyAttributeProperty();
            classificationProperty.Attribute = "_CLASSIFICATION";
            propertyAttribute.Properties["intensity"] = intensityProperty;
            propertyAttribute.Properties["classification"] = classificationProperty;

            var schemaUri = new Uri("MetadataSchema.json", UriKind.Relative);
            model.SetPropertyAttribute(propertyAttribute, schemaUri);
            var ctx = new ValidationResult(model, ValidationMode.Strict, true);
            model.AttachToCurrentTest("cesium_ext_structural_metadata_with_pointcloud_attributes.glb");
            model.AttachToCurrentTest("cesium_ext_structural_metadata_with_pointcloud_attributes.gltf");
            model.AttachToCurrentTest("cesium_ext_structural_metadata_with_pointcloud_attributes.plotly");
        }


        private static PropertyTable GetFirstPropertyTable(ModelRoot model)
        {
            var firstPropertyTable = new PropertyTable("exampleMetadataClassA", 1, "First example property table");
            var float32Property = model.GetPropertyTableProperty(new List<float>() { 100 });
            firstPropertyTable.Properties.Add("example_FLOAT32", float32Property);
            var int64Property = model.GetPropertyTableProperty(new List<long>() { 101 });
            firstPropertyTable.Properties.Add("example_INT64", int64Property);
            return firstPropertyTable;
        }

        private static PropertyTable GetSecondPropertyTable(ModelRoot model)
        {
            var secondPropertyTable = new PropertyTable("exampleMetadataClassB", 1, "First example property table");
            var uint16Property = model.GetPropertyTableProperty(new List<ushort>() { 102 });
            secondPropertyTable.Properties.Add("example_UINT16", uint16Property);
            var float64Property = model.GetPropertyTableProperty(new List<double>() { 103 });
            secondPropertyTable.Properties.Add("example_FLOAT64", float64Property);
            return secondPropertyTable;
        }

        private static StructuralMetadataClass GetExampleClassB()
        {
            var classB = new StructuralMetadataClass();
            classB.Name = "Example metadata class B";
            classB.Description = "Second example metadata class";

            var uint16Property = new ClassProperty();
            uint16Property.Name = "Example UINT16 property";
            uint16Property.Description = "An example property, with component type UINT16";
            uint16Property.Type = ElementType.SCALAR;
            uint16Property.ComponentType = DataType.UINT16;

            classB.Properties.Add("example_UINT16", uint16Property);

            var float64Property = new ClassProperty();
            float64Property.Name = "Example FLOAT64 property";
            float64Property.Description = "An example property, with component type FLOAT64";
            float64Property.Type = ElementType.SCALAR;
            float64Property.ComponentType = DataType.FLOAT64;

            classB.Properties.Add("example_FLOAT64", float64Property);
            return classB;
        }


        private static StructuralMetadataClass GetExampleClassA()
        {
            var classA = new StructuralMetadataClass();
            classA.Name = "Example metadata class A";
            classA.Description = "First example metadata class";

            var float32Property = new ClassProperty();
            float32Property.Name = "Example FLOAT32 property";
            float32Property.Description = "An example property, with component type FLOAT32";
            float32Property.Type = ElementType.SCALAR;
            float32Property.ComponentType = DataType.FLOAT32;

            classA.Properties.Add("example_FLOAT32", float32Property);

            var int64Property = new ClassProperty();
            int64Property.Name = "Example INT64 property";
            int64Property.Description = "An example property, with component type INT64";
            int64Property.Type = ElementType.SCALAR;
            int64Property.ComponentType = DataType.INT64;

            classA.Properties.Add("example_INT64", int64Property);
            return classA;
        }

        [Test(Description = "First test with ext_structural_metadata")]
        public void TriangleWithMetadataTest()
        {
            TestContext.CurrentContext.AttachGltfValidatorLinks();
            var material = MaterialBuilder.CreateDefault().WithDoubleSide(true);
            var mesh = new MeshBuilder<VertexPosition>("mesh");
            var prim = mesh.UsePrimitive(material);

            prim.AddTriangle(new VertexPosition(-10, 0, 0), new VertexPosition(10, 0, 0), new VertexPosition(0, 10, 0));

            var scene = new SceneBuilder();
            scene.AddRigidMesh(mesh, Matrix4x4.Identity);
            var model = scene.ToGltf2();
            
            var schema = new StructuralMetadataSchema();
            schema.Id = "schema_001";
            schema.Name = "schema 001";
            schema.Description = "an example schema";
            schema.Version = "3.5.1";
            var classes = new Dictionary<string, StructuralMetadataClass>();
            var treeClass = new StructuralMetadataClass();
            treeClass.Name = "Tree";
            treeClass.Description = "Woody, perennial plant.";
            classes["tree"] = treeClass;
            var ageProperty = new ClassProperty();
            ageProperty.Description = "The age of the tree, in years";
            ageProperty.Type = ElementType.SCALAR;
            ageProperty.ComponentType = DataType.UINT32;
            ageProperty.Required = true;

            treeClass.Properties.Add("age", ageProperty);

            schema.Classes = classes;

            var propertyTable = new PropertyTable("tree", 1, "PropertyTable");
            var agePropertyTableProperty = model.GetPropertyTableProperty(new List<int>() { 100 });
            propertyTable.Properties.Add("age", agePropertyTableProperty);

            model.SetPropertyTable(propertyTable, schema);

            // create files
            var ctx = new ValidationResult(model, ValidationMode.Strict, true);
            model.AttachToCurrentTest("cesium_ext_structural_metadata_basic_triangle.glb");
            model.AttachToCurrentTest("cesium_ext_structural_metadata_basic_triangle.gltf");
            model.AttachToCurrentTest("cesium_ext_structural_metadata_basic_triangle.plotly");
        }
    }
}
