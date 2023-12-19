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

        [Test(Description = "ext_structural_metadata with simple property texture")]
        // sample see https://github.com/CesiumGS/3d-tiles-samples/tree/main/glTF/EXT_structural_metadata/SimplePropertyTexture
        public void SimplePropertyTextureTest()
        {
            TestContext.CurrentContext.AttachGltfValidatorLinks();

            // Bitmap of 16*16 pixels, containing FeatureID's (0, 1, 2, 3) in the red channel
            var img0 = "iVBORw0KGgoAAAANSUhEUgAAABAAAAAQCAYAAAAf8/9hAAAAJElEQVR42mNgYmBgoAQzDLwBgwcwY8FDzIDBDRiR8KgBNDAAAOKBAKByX2jMAAAAAElFTkSuQmCC";
            var imageBytes = Convert.FromBase64String(img0);
            var imageBuilder = ImageBuilder.From(imageBytes);

            var material = MaterialBuilder
                .CreateDefault()
                .WithMetallicRoughnessShader()
                .WithBaseColor(imageBuilder, new Vector4(1, 1, 1, 1))
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

            // model.SaveGLTF(@"D:\dev\github.com\bertt\cesium_3dtiles_samples\samples\1.1\EXT_Structural_Metadata\SimplePropertyTexture\SimplePropertyTexture1.gltf");

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

            exampleMetadataClass.Properties.Add("inside_temperature", insideTemperatureProperty);
            exampleMetadataClass.Properties.Add("outside_temperature", outsideTemperatureProperty);
            exampleMetadataClass.Properties.Add("insulation_thickness", insulationProperty);

            // todo set propertytexture
            // var insideTemperaturePropertyTexture = new PropertyTexture();
            // insideTemperaturePropertyTexture.index = 1

            var propertyTextures = new List<PropertyTexture>() {};
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
