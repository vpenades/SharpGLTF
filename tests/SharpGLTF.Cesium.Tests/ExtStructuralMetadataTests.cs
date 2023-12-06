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

        [Test(Description ="First test with ext_structural_metadata")]
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

            var bytes = BinaryTable.GetBytes(new List<int>() { 100 });
            var bufferView = model.UseBufferView(bytes);

            var ext = model.UseExtension<EXTStructuralMetaDataRoot>();

            var propertyTableProperty = new PropertyTableProperty();
            propertyTableProperty.Values = bufferView.LogicalIndex;
            var propertyTable = new PropertyTable("propertyTable", 1);
            propertyTable.Properties["id1"] = propertyTableProperty;
            ext.PropertyTables.Add( propertyTable);

            var schema = new StructuralMetadataSchema();
            schema.Id = "schema_001";
            schema.Name = "schema 001";
            schema.Description = "an example schema";
            schema.Version = "3.5.1";
            var classes = new Dictionary<string, StructuralMetadataClass>();
            var treeClass = new StructuralMetadataClass();
            classes["tree"] = treeClass;
            treeClass.Name = "Tree";
            treeClass.Description = "Woody, perennial plant.";

            var speciesProperty = new ClassProperty();
            speciesProperty.Description = "Type of tree";
            speciesProperty.Type = ElementType.ENUM;
            speciesProperty.EnumType = "speciesEnum";
            speciesProperty.Required = true;

            treeClass.Properties.Add("species", speciesProperty);

            var ageProperty = new ClassProperty();
            ageProperty.Description = "The age of the tree, in years";
            ageProperty.Type = ElementType.SCALAR;
            ageProperty.ComponentType = DataType.UINT8;
            ageProperty.Required = true;

            treeClass.Properties.Add("age", ageProperty);

            var speciesEnum = new StructuralMetadataEnum();
            schema.Enums["speciesEnum"] = speciesEnum;
            speciesEnum.Name = "Species";
            speciesEnum.Description = "An example enum for tree species.";
            speciesEnum.Values.Add(new EnumValue() { Name = "Unpsecified", Value = 0 });
            speciesEnum.Values.Add(new EnumValue() { Name = "Oak", Value = 1 });
            speciesEnum.Values.Add(new EnumValue() { Name = "Pine", Value = 2 });
            speciesEnum.Values.Add(new EnumValue() { Name = "Maple", Value = 3 });

            schema.Classes = classes;
            ext.Schema = schema;

            var ctx = new ValidationResult(model, ValidationMode.Strict, true);
            model.AttachToCurrentTest("cesium_ext_structural_metadata_basic_triangle.glb");
            model.AttachToCurrentTest("cesium_ext_structural_metadata_basic_triangle.gltf");
            model.AttachToCurrentTest("cesium_ext_structural_metadata_basic_triangle.plotly");
        }
    }
}
