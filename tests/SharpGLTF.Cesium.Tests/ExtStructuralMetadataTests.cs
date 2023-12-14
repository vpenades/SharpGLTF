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
    [Category("Toolkit.Scenes")]
    public class ExtStructuralMetadataTests
    {
        [SetUp]
        public void SetUp()
        {
            CesiumExtensions.RegisterExtensions();
        }

        [Test(Description = "ext_structural_metadata with multiple classes")]
        // Sample see https://github.com/CesiumGS/3d-tiles-samples/blob/main/glTF/EXT_structural_metadata/MultipleClasses/MultipleClasses.gltf
        public void MultipleClassesTest()
        {
            var material = MaterialBuilder.CreateDefault().WithDoubleSide(true);

            var mesh = new MeshBuilder<VertexPositionNormal, VertexWithFeatureIds, VertexEmpty>("mesh");
            var prim = mesh.UsePrimitive(material);

            // All the vertices in the triangle have the same feature ID
            var vt0 = VertexBuilder.GetVertexWithFeatureIds(new Vector3(-10, 0, 0), new Vector3(0, 0, 1), 0, 100);
            var vt1 = VertexBuilder.GetVertexWithFeatureIds(new Vector3(10, 0, 0), new Vector3(0, 0, 1), 0, 100);
            var vt2 = VertexBuilder.GetVertexWithFeatureIds(new Vector3(0, 10, 0), new Vector3(0, 0, 1), 0, 100);

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
            exampleEnum.Values.Add(new EnumValue() {  Name= "ExampleEnumValueA", Value = 0 });
            exampleEnum.Values.Add(new EnumValue() { Name = "ExampleEnumValueB", Value = 1 });
            exampleEnum.Values.Add(new EnumValue() { Name = "ExampleEnumValueC", Value = 2 });

            schema.Enums.Add("exampleEnumType", exampleEnum);

            // Todo: create the property tables (First example property table, Second example property table)

            
            // model.SetPropertyAttribute(propertyAttribute, schema);


            // todo: add metadata
            var ctx = new ValidationResult(model, ValidationMode.Strict, true);
            model.AttachToCurrentTest("cesium_ext_structural_metadata_multiple_classes.glb");
            model.AttachToCurrentTest("cesium_ext_structural_metadata_multiple_classes.gltf");
            model.AttachToCurrentTest("cesium_ext_structural_metadata_multiple_classes.plotly");
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

        [Test(Description = "ext_structural_metadata with pointcloud and custom attributes")]
        // Sample see https://github.com/CesiumGS/3d-tiles-samples/blob/main/glTF/EXT_structural_metadata/PropertyAttributesPointCloud/PropertyAttributesPointCloudHouse.gltf

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
            model.SetPropertyAttribute(propertyAttribute, schemaUri );
            var ctx = new ValidationResult(model, ValidationMode.Strict, true);
            model.AttachToCurrentTest("cesium_ext_structural_metadata_with_pointcloud_attributes.glb");
            model.AttachToCurrentTest("cesium_ext_structural_metadata_with_pointcloud_attributes.gltf");
            model.AttachToCurrentTest("cesium_ext_structural_metadata_with_pointcloud_attributes.plotly");
        }


        [Test(Description = "First test with ext_structural_metadata")]
        public void TriangleWithMetadataTest()
        {
            var model = GetTriangleModel();

            var schema = GetSampleSchema();

            var attribute = new List<int>() { 100 };
            var dict = new Dictionary<string, List<int>>();
            dict["age"] = attribute;
            model.SetPropertyTable(dict, schema: schema);

            // create files
            var ctx = new ValidationResult(model, ValidationMode.Strict, true);
            model.AttachToCurrentTest("cesium_ext_structural_metadata_basic_triangle.glb");
            model.AttachToCurrentTest("cesium_ext_structural_metadata_basic_triangle.gltf");
            model.AttachToCurrentTest("cesium_ext_structural_metadata_basic_triangle.plotly");
        }

        private static ModelRoot GetTriangleModel()
        {
            TestContext.CurrentContext.AttachGltfValidatorLinks();

            var material = MaterialBuilder.CreateDefault().WithDoubleSide(true);

            var mesh = new MeshBuilder<VertexPosition>("mesh");
            var prim = mesh.UsePrimitive(material);

            prim.AddTriangle(new VertexPosition(-10, 0, 0), new VertexPosition(10, 0, 0), new VertexPosition(0, 10, 0));

            var scene = new SceneBuilder();
            scene.AddRigidMesh(mesh, Matrix4x4.Identity);
            var model = scene.ToGltf2();
            return model;
        }

        private static StructuralMetadataSchema GetSampleSchema()
        {
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
            return schema;
        }
    }
}
