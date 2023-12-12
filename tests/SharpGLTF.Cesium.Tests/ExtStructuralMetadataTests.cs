using NUnit.Framework;
using SharpGLTF.Geometry;
using SharpGLTF.Geometry.VertexTypes;
using SharpGLTF.Materials;
using SharpGLTF.Scenes;
using SharpGLTF.Schema2;
using SharpGLTF.Validation;
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

        [Test(Description = "ext_structural_metadata with pointcloud and custom attributes")]

        public void CreatePointCloudWithCustomAttributesTest()
        {
            var material = new MaterialBuilder("material1").WithUnlitShader();
            var mesh = new MeshBuilder<VertexPosition, VertexPointcloud, VertexEmpty>("mesh");
            var pointCloud = mesh.UsePrimitive(material, 1);

            for (var x = -10; x < 10; x++)
            {
                for (var y = -10; y < 10; y++)
                {
                    for (var z = -10; z < 10; z++)
                    {
                        var vt0 = VertexBuilder.GetVertexPointcloud(new Vector3(x, y, z), 199, 4);

                        pointCloud.AddPoint(vt0);
                    }
                }
            }
            var model = ModelRoot.CreateModel();
            model.CreateMeshes(mesh);

            // create a scene, a node, and assign the first mesh (the terrain)
            model.UseScene("Default")
                .CreateNode().WithMesh(model.LogicalMeshes[0]);

            var ctx = new ValidationResult(model, ValidationMode.Strict, true);
            model.AttachToCurrentTest("cesium_ext_structural_metadata_with_pointcloud_attributes.glb");
            model.AttachToCurrentTest("cesium_ext_structural_metadata_with_prointcloud_attributes.gltf");
            model.AttachToCurrentTest("cesium_ext_structural_metadata_with_prointcloud_attributes.plotly");
        }


        /// <summary>
        /// Sample see https://github.com/CesiumGS/3d-tiles-samples/blob/main/glTF/EXT_structural_metadata/PropertyAttributesPointCloud/PropertyAttributesPointCloudHouse.gltf
        /// </summary>
        [Test(Description = "ext_structural_metadata with PropertyAttributes")]
        public void TriangleWithPropertyAttributes()
        {
            // todo: create a model with custom vertex attributes "_INTENSITY" and "_CLASSIFICATION"
            var model = GetTriangleModel();
            var propertyAttribute = new Schema2.PropertyAttribute();
            propertyAttribute.Class = "exampleMetadataClass";
            var intensityProperty = new PropertyAttributeProperty();
            intensityProperty.Attribute = "_INTENSITY";
            var classificationProperty = new PropertyAttributeProperty();
            classificationProperty.Attribute = "_CLASSIFICATION";
            propertyAttribute.Properties["intensity"] = intensityProperty;
            propertyAttribute.Properties["classification"] = classificationProperty;

            model.SetPropertyAttribute(propertyAttribute);
            
            var ctx = new ValidationResult(model, ValidationMode.Strict, true);
            model.AttachToCurrentTest("cesium_ext_structural_metadata_with_property_attribute.glb");
            model.AttachToCurrentTest("cesium_ext_structural_metadata_with_property_attribute.gltf");
            model.AttachToCurrentTest("cesium_ext_structural_metadata_with_property_attribute.plotly");
        }

        [Test(Description ="First test with ext_structural_metadata")]
        public void TriangleWithMetadataTest()
        {
            var  model = GetTriangleModel();

            var schema = GetSampleSchema();

            var attribute = new List<int>() { 100 };
            var dict = new Dictionary<string, List<int>>();
            dict["age"] = attribute;
            model.SetPropertyTable(schema, dict);
            
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
