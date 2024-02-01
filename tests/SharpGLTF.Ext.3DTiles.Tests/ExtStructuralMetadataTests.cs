using NUnit.Framework;
using SharpGLTF.Geometry;
using SharpGLTF.Geometry.VertexTypes;
using SharpGLTF.Materials;
using SharpGLTF.Scenes;
using SharpGLTF.Validation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace SharpGLTF.Schema2.Tiles3D
{
    using VBTexture1 = VertexBuilder<VertexPosition, VertexTexture1, VertexEmpty>;

    [Category("Toolkit.Scenes")]
    public class ExtStructuralMetadataTests
    {
        [SetUp]
        public void SetUp()
        {
            Tiles3DExtensions.RegisterExtensions();
        }

        // Test files are from https://github.com/CesiumGS/3d-tiles-validator/tree/main/specs/data/gltfExtensions/structuralMetadata
        [Test(Description = "Reads glTF's with EXT_Structural_Metadata")]
        [TestCase("ExtensionInMeshPrimitiveWithoutTopLevelObject.gltf", typeof(ModelException))]
        [TestCase("PropertyAttributesClassPropertyArray.gltf", typeof(ModelException))]
        [TestCase("PropertyAttributesClassPropertyInvalidComponentType.gltf", typeof(ModelException))]
        [TestCase("PropertyAttributesClassPropertyInvalidEnumValueType.gltf", typeof(ModelException))]
        // Todo: Minmax [TestCase("PropertyAttributesClassPropertyMaxNotInRange.gltf", typeof(ModelException))]
        // Todo: Minmax [TestCase("PropertyAttributesClassPropertyMinNotInRange.gltf", typeof(ModelException))]
        [TestCase("PropertyAttributesClassPropertyString.gltf", typeof(ModelException))]
        [TestCase("PropertyAttributesMeshPrimitivePropertyAttributesInvalidElementType.gltf", typeof(InvalidOperationException))]
        [TestCase("PropertyAttributesMeshPrimitivePropertyAttributesInvalidElementValue.gltf", typeof(LinkException))]
        [TestCase("PropertyAttributesMeshPrimitivePropertyAttributesInvalidLength.gltf", typeof(SchemaException))]
        [TestCase("PropertyAttributesMeshPrimitivePropertyAttributesInvalidType.gltf", typeof(SchemaException))]
        [TestCase("PropertyAttributesPropertyAttributePropertyInvalidAttribute.gltf", typeof(ModelException))]
        // Todo: Minmax [TestCase("PropertyAttributesPropertyAttributePropertyMaxMismatch.gltf", typeof(ModelException))] 
        // Todo: Minmax [TestCase("PropertyAttributesPropertyAttributePropertyMaxNotInRange.gltf", typeof(ModelException))]
        // Todo: Minmax [TestCase("PropertyAttributesPropertyAttributePropertyMinMismatch.gltf", typeof(ModelException))]
        // Todo: Minmax [TestCase("PropertyAttributesPropertyAttributePropertyMinNotInRange.gltf", typeof(ModelException))]
        // todo minmax with texture [TestCase("PropertyTextureClassPropertyMaxNotInRange.gltf", typeof(ModelException))]
        // todo minmax with texture [TestCase("PropertyTextureClassPropertyMinNotInRange.gltf", typeof(ModelException))]
        // todo minmax with texture [TestCase("PropertyTextureClassPropertyWithOffsetScaleMinNotInRange.gltf", typeof(ModelException))]
        // todo minmax with texture [TestCase("PropertyTextureEnumsInvalidEnumValue.gltf", typeof(ModelException))]
        [TestCase("PropertyTextureInvalidPropertyTypeA.gltf", typeof(ModelException))]
        [TestCase("PropertyTextureInvalidPropertyTypeB.gltf", typeof(ModelException))]
        [TestCase("PropertyTextureMeshPrimitivePropertyTexturesInvalidElementType.gltf", typeof(InvalidOperationException))]
        [TestCase("PropertyTextureMeshPrimitivePropertyTexturesInvalidElementValue.gltf", typeof(LinkException))]
        [TestCase("PropertyTextureMeshPrimitivePropertyTexturesInvalidLength.gltf", typeof(SchemaException))]
        [TestCase("PropertyTextureMeshPrimitivePropertyTexturesInvalidType.gltf", typeof(SchemaException))]
        [TestCase("PropertyTextureMeshPrimitivePropertyTextureTexCoordInvalidValue.gltf", typeof(ModelException))]
        [TestCase("PropertyTexturePropertyChannelsSizeMismatch.gltf", typeof(ModelException))]
        [TestCase("PropertyTexturePropertyIndexInvalidType.gltf", typeof(InvalidOperationException))]
        [TestCase("PropertyTexturePropertyIndexInvalidValue.gltf", typeof(LinkException))]
        // todo minmax with texture [TestCase("PropertyTexturePropertyTexturePropertyMaxMismatch.gltf", typeof(ModelException))]
        // todo minmax with texture [TestCase("PropertyTexturePropertyTexturePropertyMaxNotInRange.gltf", typeof(ModelException))]
        // todo minmax with texture [TestCase("PropertyTexturePropertyTexturePropertyMinMismatch.gltf", typeof(ModelException))]
        // todo minmax with texture[TestCase("PropertyTexturePropertyTexturePropertyMinNotInRange.gltf", typeof(ModelException))]
        [TestCase("StructuralMetadataMissingSchema.gltf", typeof(ModelException))]
        [TestCase("StructuralMetadataSchemaAndSchemaUri.gltf", typeof(ModelException))]
        [TestCase("ValidMultipleClasses.gltf", null)]
        [TestCase("ValidPropertyAttributes.gltf", null)]
        [TestCase("ValidPropertyTexture.gltf", null)]
        [TestCase("ValidPropertyTextureEnums.gltf", null)]

        public void ReadExtStructuralMetadata(string file, Type exception = null)
        {
            var fileName = ResourceInfo.From($"structuralMetadata/{file}");

            if (exception != null)
            {
                Assert.Throws(exception, delegate { ModelRoot.Load(fileName); });
            }
            else
            {
                var model = ModelRoot.Load(fileName);
                var structuralMetadataExtension = model.GetExtension<EXTStructuralMetadataRoot>();
                var ctx = new ValidationResult(model, ValidationMode.Strict, true);
                model.ValidateContent(ctx.GetContext());
            }
        }

        /// <summary>
        /// In this test a single triangle is defined, it has attributes defined for all types with a noData value, 
        /// but the values are set to the noData value. In CesiumJS the triangle is rendered but the 
        /// attritutes are not shown (because noData).
        /// </summary>
        [Test(Description = "MetadataAndNullValuesAttributeSample")]
        public void MetadataNullValuesAttributeSample()
        {
            TestContext.CurrentContext.AttachGltfValidatorLinks();

            int featureId = 0;
            var material = MaterialBuilder.CreateDefault().WithDoubleSide(true);

            var mesh = new MeshBuilder<VertexPositionNormal, VertexWithFeatureId, VertexEmpty>("mesh");
            var prim = mesh.UsePrimitive(material);

            var vt0 = VertexBuilder.GetVertexWithFeatureId(new Vector3(0, 0, 0), new Vector3(0, 0, 1), featureId);
            var vt1 = VertexBuilder.GetVertexWithFeatureId(new Vector3(1, 0, 0), new Vector3(0, 0, 1), featureId);
            var vt2 = VertexBuilder.GetVertexWithFeatureId(new Vector3(0, 1, 0), new Vector3(0, 0, 1), featureId);

            prim.AddTriangle(vt0, vt1, vt2);
            var scene = new SceneBuilder();
            scene.AddRigidMesh(mesh, Matrix4x4.Identity);
            var model = scene.ToGltf2();

            var rootMetadata = model.UseStructuralMetadata();
            var schema = rootMetadata.UseEmbeddedSchema("schema_001");

            var schemaClass = schema.UseClassMetadata("triangles");

            var speciesEnum = schema.UseEnumMetadata("speciesEnum", ("Unspecified", 0), ("Oak", 1), ("Pine", 2), ("Maple", 3));
            speciesEnum.Name = "Species";
            speciesEnum.Description = "An example enum for tree species.";

            var descriptionProperty = schemaClass
                    .UseProperty("description")
                    .WithStringType();

            // for this property, the default value (byte.MaxValue) should be shown in the client when the actual value is 
            // equal to the noData value (byte.MinValue)
            var uint8Property = schemaClass
                .UseProperty("uint8")
                .WithUInt8Type(byte.MinValue, byte.MaxValue);

            var int8Property = schemaClass
                .UseProperty("int8")
                .WithInt8Type(sbyte.MinValue);

            var int16Property = schemaClass
                .UseProperty("int16")
                .WithInt16Type(short.MinValue);

            var uint16Property = schemaClass
                .UseProperty("uint16")
                .WithUInt16Type(ushort.MinValue);

            var int32Property = schemaClass
                .UseProperty("int32")
                .WithInt32Type(int.MinValue);

            var uint32Property = schemaClass
                .UseProperty("uint32")
                .WithUInt32Type(uint.MinValue);

            var int64Property = schemaClass
                .UseProperty("int64")
                .WithInt64Type(long.MinValue);

            var uint64Property = schemaClass
                .UseProperty("uint64")
                .WithUInt64Type(ulong.MinValue);

            // when using float.MinValue there is an error in the validator: ""The value has type FLOAT32 and must be in [-3.4028234663852886e+38,3.4028234663852886e+38], but is -3.4028235e+38"
            // And the noData value is shown in CesiumJS. Therefore we use -10.0f here.
            var float32Property = schemaClass
                .UseProperty("float32")
                .WithFloat32Type(-10.0f);

            var float64Property = schemaClass
                .UseProperty("float64")
                .WithFloat64Type(double.MinValue);

            var stringProperty = schemaClass
                .UseProperty("string")
                .WithStringType("noData", "-");

            var speciesProperty = schemaClass
                .UseProperty("species")
                .WithDescription("Type of tree.")
                .WithEnumeration(speciesEnum, "Unspecified")
                .WithRequired(false);

            var vector3Property = schemaClass
                .UseProperty("vector3")
                .WithVector3Type(new Vector3(-10.0f, -10.0f, -10.0f));

            var matrix4x4Property = schemaClass
                .UseProperty("matrix4x4")
                .WithMatrix4x4Type(Matrix4x4.Identity * -10);

            var propertyTable = schemaClass.AddPropertyTable(1);

            propertyTable
                .UseProperty(descriptionProperty)
                .SetValues("Description of the triangle");

            propertyTable
                .UseProperty(uint8Property)
                .SetValues(byte.MinValue);

            propertyTable
                .UseProperty(int8Property)
                .SetValues(sbyte.MinValue);

            propertyTable
                .UseProperty(int16Property)
                .SetValues(short.MinValue);

            propertyTable
                .UseProperty(uint16Property)
                .SetValues(ushort.MinValue);

            propertyTable
                .UseProperty(int32Property)
                .SetValues(int.MinValue);

            propertyTable
                .UseProperty(uint32Property)
                .SetValues(uint.MinValue);

            propertyTable
                .UseProperty(int64Property)
                .SetValues(long.MinValue);

            propertyTable
                .UseProperty(uint64Property)
                .SetValues(ulong.MinValue);

            propertyTable
                .UseProperty(float32Property)
                .SetValues(-10f);

            propertyTable
                .UseProperty(float64Property)
                .SetValues(double.MinValue);

            propertyTable
                .UseProperty(stringProperty)
                .SetValues("noData");

            propertyTable
                .UseProperty(speciesProperty)
                .SetValues((short)0);

            propertyTable
                .UseProperty(vector3Property)
                .SetValues(new Vector3(10.0f,10.0f,10.0f));

            var m4 = Matrix4x4.Identity;
            propertyTable
                .UseProperty(matrix4x4Property)
                .SetValues(m4);

            foreach (var primitive in model.LogicalMeshes[0].Primitives)
            {
                var featureIdAttribute = new FeatureIDBuilder(1, 0, propertyTable);
                primitive.AddMeshFeatureIds(featureIdAttribute);
            }

            // create files
            var ctx = new ValidationResult(model, ValidationMode.Strict, true);
            model.AttachToCurrentTest("cesium_ext_structural_minimal_metadata_sample.glb");
            model.AttachToCurrentTest("cesium_ext_structural_minimal_metadata_sample.gltf");
            model.AttachToCurrentTest("cesium_ext_structural_minimal_metadata_sample.plotly");
        }

        [Test(Description = "MinimalMetadataAttributeSample")]
        public void MinimalMetadataAttributeSample()
        {
            TestContext.CurrentContext.AttachGltfValidatorLinks();

            int featureId = 0;
            var material = MaterialBuilder.CreateDefault().WithDoubleSide(true);

            var mesh = new MeshBuilder<VertexPositionNormal, VertexWithFeatureId, VertexEmpty>("mesh");
            var prim = mesh.UsePrimitive(material);

            var vt0 = VertexBuilder.GetVertexWithFeatureId(new Vector3(0, 0, 0), new Vector3(0, 0, 1), featureId);
            var vt1 = VertexBuilder.GetVertexWithFeatureId(new Vector3(1, 0, 0), new Vector3(0, 0, 1), featureId);
            var vt2 = VertexBuilder.GetVertexWithFeatureId(new Vector3(0, 1, 0), new Vector3(0, 0, 1), featureId);

            prim.AddTriangle(vt0, vt1, vt2);
            var scene = new SceneBuilder();
            scene.AddRigidMesh(mesh, Matrix4x4.Identity);
            var model = scene.ToGltf2();

            var rootMetadata = model.UseStructuralMetadata();
            var schema = rootMetadata.UseEmbeddedSchema("schema_001");

            var schemaClass = schema.UseClassMetadata("triangles");

            var nameProperty = schemaClass
                .UseProperty("name")
                .WithStringType();

            var propertyTable = schemaClass.AddPropertyTable(1);

            propertyTable
                .UseProperty(nameProperty)
                .SetValues("this is featureId0");

            foreach (var primitive in model.LogicalMeshes[0].Primitives)
            {
                var featureIdAttribute = new FeatureIDBuilder(1, 0, propertyTable);
                primitive.AddMeshFeatureIds(featureIdAttribute);
            }

            // create files
            var ctx = new ValidationResult(model, ValidationMode.Strict, true);
            model.AttachToCurrentTest("cesium_ext_structural_minimal_metadata_sample.glb");
            model.AttachToCurrentTest("cesium_ext_structural_minimal_metadata_sample.gltf");
            model.AttachToCurrentTest("cesium_ext_structural_minimal_metadata_sample.plotly");
        }


        [Test(Description = "TestWith2PrimitivesAndMetadata")]
        public void MultiplePrimitivesAndMetadata()
        {
            TestContext.CurrentContext.AttachGltfValidatorLinks();

            int featureId = 0;
            var material = MaterialBuilder.CreateDefault().WithDoubleSide(true);

            var mesh = new MeshBuilder<VertexPositionNormal, VertexWithFeatureId, VertexEmpty>("mesh");
            var prim = mesh.UsePrimitive(material);

            var vt0 = VertexBuilder.GetVertexWithFeatureId(new Vector3(0, 0, 0), new Vector3(0, 0, 1), featureId);
            var vt1 = VertexBuilder.GetVertexWithFeatureId(new Vector3(1, 0, 0), new Vector3(0, 0, 1), featureId);
            var vt2 = VertexBuilder.GetVertexWithFeatureId(new Vector3(0, 1, 0), new Vector3(0, 0, 1), featureId);

            prim.AddTriangle(vt0, vt1, vt2);

            // featureId = 1 and 2 (other material)

            var material2 = new MaterialBuilder()
                .WithDoubleSide(true)
                .WithMetallicRoughnessShader()
                .WithChannelParam(KnownChannel.BaseColor, KnownProperty.RGBA, new Vector4(1, 0, 1, 1));

            var prim2 = mesh.UsePrimitive(material2);

            featureId = 1;
            var vt3 = VertexBuilder.GetVertexWithFeatureId(new Vector3(2, 0, 0), new Vector3(0, 0, 1), featureId);
            var vt4 = VertexBuilder.GetVertexWithFeatureId(new Vector3(3, 0, 0), new Vector3(0, 0, 1), featureId);
            var vt5 = VertexBuilder.GetVertexWithFeatureId(new Vector3(2, 1, 0), new Vector3(0, 0, 1), featureId);

            prim2.AddTriangle(vt3, vt4, vt5);

            featureId = 2;
            var vt6 = VertexBuilder.GetVertexWithFeatureId(new Vector3(4, 0, 0), new Vector3(0, 0, 1), featureId);
            var vt7 = VertexBuilder.GetVertexWithFeatureId(new Vector3(5, 0, 0), new Vector3(0, 0, 1), featureId);
            var vt8 = VertexBuilder.GetVertexWithFeatureId(new Vector3(4, 1, 0), new Vector3(0, 0, 1), featureId);

            prim2.AddTriangle(vt6, vt7, vt8);

            var scene = new SceneBuilder();
            scene.AddRigidMesh(mesh, Matrix4x4.Identity);
            var model = scene.ToGltf2();

            var rootMetadata = model.UseStructuralMetadata();
            var schema = rootMetadata.UseEmbeddedSchema("schema_001");
            schema.Name = "schema 001";
            schema.Description = "an example schema";
            schema.Version = "3.5.1";

            var trianglesClass = schema
                .UseClassMetadata("triangles")
                .WithName("Triangle");

            var nameProperty = trianglesClass
                .UseProperty("name")
                .WithStringType();


            var isTriangle = trianglesClass
                .UseProperty("IsTriangle")
                .WithBooleanType();

            var propertyTable = trianglesClass
                .AddPropertyTable(3, "PropertyTable");

            propertyTable
                .UseProperty(nameProperty)
                .SetValues("this is featureId0", "this is featureId1", "this is featureId2");

            propertyTable
                .UseProperty(isTriangle)
                .SetValues(false, true, false);


            foreach (var primitive in model.LogicalMeshes[0].Primitives)
            {
                var triangles = primitive.EvaluateTriangles().Count();
                var featureIdAttribute = new FeatureIDBuilder(triangles, 0, propertyTable);
                primitive.AddMeshFeatureIds(featureIdAttribute);
            }

            // create files
            var ctx = new ValidationResult(model, ValidationMode.Strict, true);
            model.AttachToCurrentTest("cesium_ext_structural_metadata_multiple_primitives.glb");
            model.AttachToCurrentTest("cesium_ext_structural_metadata_multiple_primitives.gltf");
            model.AttachToCurrentTest("cesium_ext_structural_metadata_multiple_primitives.plotly");
        }


        [Test(Description = "First test with ext_structural_metadata")]
        // This test creates a simple triangle (featureId = 0) with ext_structural_metadata (4 tree attributes like
        // species (Enumeration), age (Scalar), height (Scalar) and diameter (Scalar) and a property table.
        // following the structure described in https://github.com/CesiumGS/glTF/tree/proposal-EXT_structural_metadata/extensions/2.0/Vendor/EXT_structural_metadata
        public void TriangleWithMetadataTest()
        {
            TestContext.CurrentContext.AttachGltfValidatorLinks();
            var material = MaterialBuilder.CreateDefault().WithDoubleSide(true);

            var mesh = new MeshBuilder<VertexPositionNormal, VertexWithFeatureId, VertexEmpty>("mesh");
            var prim = mesh.UsePrimitive(material);

            var features = new List<int>() { 0, 1 };

            var vt0 = VertexBuilder.GetVertexWithFeatureId(new Vector3(0, 0, 0), new Vector3(0, 0, 1), features[0]);
            var vt1 = VertexBuilder.GetVertexWithFeatureId(new Vector3(1, 0, 0), new Vector3(0, 0, 1), features[0]);
            var vt2 = VertexBuilder.GetVertexWithFeatureId(new Vector3(0, 1, 0), new Vector3(0, 0, 1), features[0]);

            prim.AddTriangle(vt0, vt1, vt2);

            var vt3 = VertexBuilder.GetVertexWithFeatureId(new Vector3(2, 0, 0), new Vector3(0, 0, 1), features[1]);
            var vt4 = VertexBuilder.GetVertexWithFeatureId(new Vector3(3, 0, 0), new Vector3(0, 0, 1), features[1]);
            var vt5 = VertexBuilder.GetVertexWithFeatureId(new Vector3(2, 1, 0), new Vector3(0, 0, 1), features[1]);

            prim.AddTriangle(vt3, vt4, vt5);

            var scene = new SceneBuilder();
            scene.AddRigidMesh(mesh, Matrix4x4.Identity);
            var model = scene.ToGltf2();
           
            var rootMetadata = model.UseStructuralMetadata();
            var schema = rootMetadata.UseEmbeddedSchema("schema_001");
            schema.Name = "schema 001";
            schema.Description = "an example schema";
            schema.Version = "3.5.1";

            var speciesEnum = schema.UseEnumMetadata("speciesEnum", ("Unspecified", 0), ("Oak", 1), ("Pine", 2), ("Maple",3));
            speciesEnum.Name = "Species";
            speciesEnum.Description = "An example enum for tree species.";

            var treeClass = schema
                .UseClassMetadata("tree")
                .WithName("Tree")
                .WithDescription("Woody, perennial plant.");

            // species property
            var speciesProperty = treeClass
                .UseProperty("species")
                .WithDescription("Type of tree.")
                .WithEnumeration(speciesEnum)
                .WithRequired(true);

            // age property
            var ageProperty = treeClass
                .UseProperty("age")
                .WithDescription("The age of the tree, in years")
                .WithUInt32Type()
                .WithRequired(true);

            // Height property
            var heightProperty = treeClass
                .UseProperty("height")
                .WithDescription("Height of tree measured from ground level, in meters");
            heightProperty.WithFloat32Type();

            // Diameter property
            var diameterProperty = treeClass
                .UseProperty("diameter")
                .WithDescription("Diameter at trunk base, in meters.");
            diameterProperty.WithFloat32Type();

            var propertyTable = treeClass
                .AddPropertyTable(features.Count, "PropertyTable");

            propertyTable
                .UseProperty(ageProperty)
                .SetValues((uint)100, (uint)101);

            propertyTable
                .UseProperty(speciesProperty)
                .SetValues((short)0, (short)3);

            propertyTable.UseProperty(heightProperty)
                .SetValues(10.0f, 11f);

            propertyTable.UseProperty(diameterProperty)
                .SetValues(1.5f, 2f);

            // Set the FeatureIds
            var cnt = propertyTable.Count;
            var featureIdAttribute = new FeatureIDBuilder(2, 0, propertyTable);
            model.LogicalMeshes[0].Primitives[0].AddMeshFeatureIds(featureIdAttribute);

            // create files
            var ctx = new ValidationResult(model, ValidationMode.Strict, true);

            model.AttachToCurrentTest("cesium_ext_structural_metadata_basic_triangle.glb");
            model.AttachToCurrentTest("cesium_ext_structural_metadata_basic_triangle.gltf");
            model.AttachToCurrentTest("cesium_ext_structural_metadata_basic_triangle.plotly");
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
                .WithMetallicRoughness(imageBuilder1);

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

            var rootMetadata = model.UseStructuralMetadata();
            var schema = rootMetadata.UseEmbeddedSchema("FeatureIdTextureAndPropertyTableSchema");

            // define schema

            var buildingComponentsClass = schema
                .UseClassMetadata("buildingComponents")
                .WithName("Building components")
                .WithDescription("The components of a building.");

            var componentProp = buildingComponentsClass
                .UseProperty("component")
                .WithName("Component")
                .WithStringType();

            var yearProp = buildingComponentsClass
                .UseProperty("yearBuilt")
                .WithName("Year built")
                .WithInt16Type();            

            var propertyTable = buildingComponentsClass
                .AddPropertyTable(4, "Example property table");

            propertyTable
                .UseProperty(componentProp)
                .SetValues("Wall", "Door", "Roof", "Window");

            propertyTable
                .UseProperty(yearProp)
                .SetValues((short)1960, (short)1996, (short)1985, (short)2002);            

            // Set the FeatureIds, pointing to the red channel of the texture

            var featureId = new FeatureIDBuilder(4, null, propertyTable);            

            var primitive = model.LogicalMeshes[0].Primitives[0];
            primitive.AddMeshFeatureIds(featureId);

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
                .WithMetallicRoughness(0, 1)
                .WithMetallicRoughness(imageBuilder1);

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

            // --------------------------------------------------------------

            var rootMetadata = model.UseStructuralMetadata();
            var schema = rootMetadata.UseEmbeddedSchema("SimplePropertyTextureSchema");

            // define schema 

            var exampleMetadataClass = schema
                .UseClassMetadata("buildingComponents")
                .WithName("Building properties");

            exampleMetadataClass
                .UseProperty("insideTemperature")
                .WithName("Inside temperature")
                .WithUInt8Type();

            exampleMetadataClass
                .UseProperty("outsideTemperature")
                .WithName("Outside temperature")
                .WithUInt8Type();

            exampleMetadataClass
                .UseProperty("insulation")
                .WithName("Insulation Thickness")
                .WithUInt8Type()
                .WithNormalized(true);

            // define texture property

            var buildingPropertyTexture = exampleMetadataClass.AddPropertyTexture();    
            
            buildingPropertyTexture.CreateProperty("insideTemperature", model.LogicalTextures[1], new int[] {0});
            buildingPropertyTexture.CreateProperty("outsideTemperature", model.LogicalTextures[1], new int[] {1});
            buildingPropertyTexture.CreateProperty("insulation", model.LogicalTextures[1], new int[] {2});

            // assign to primitive

            var primitive = model.LogicalMeshes[0].Primitives[0];            
            primitive.AddPropertyTexture(buildingPropertyTexture);

            var ctx = new ValidationResult(model, ValidationMode.Strict, true);
            model.AttachToCurrentTest("cesium_ext_structural_metadata_simple_property_texture.glb");
            model.AttachToCurrentTest("cesium_ext_structural_metadata_simple_property_texture.gltf");
            model.AttachToCurrentTest("cesium_ext_structural_metadata_simple_property_texture.plotly");
        }

        [Test(Description = "ext_structural_metadata with Multiple Feature IDs and Properties")]
        // sample see https://github.com/CesiumGS/3d-tiles-samples/tree/main/glTF/EXT_structural_metadata/MultipleFeatureIdsAndProperties
        public void MultipleFeatureIdsAndPropertiesTest()
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

            // --------------------------------------------------------------

            var rootMetadata = model.UseStructuralMetadata();
            var schema = rootMetadata.UseEmbeddedSchema("MultipleFeatureIdsAndPropertiesSchema");

            // define schema

            var exampleMetadataClass = schema
                .UseClassMetadata("exampleMetadataClass")
                .WithName("Example metadata class")
                .WithDescription("An example metadata class");

            var vec3Property = exampleMetadataClass
                .UseProperty("example_VEC3_FLOAT32")
                .WithName("Example VEC3 FLOAT32 property")
                .WithDescription("An example property, with type VEC3, with component type FLOAT32")
                .WithVector3Type();

            var stringProperty = exampleMetadataClass
                .UseProperty("example_STRING")
                .WithName("Example STRING property")
                .WithDescription("An example property, with type STRING")
                .WithStringType();

            // define table

            var examplePropertyTable = exampleMetadataClass.AddPropertyTable(2, "Example property table");

            examplePropertyTable
                .UseProperty(vec3Property)
                .SetValues(new Vector3(3, 3.0999999046325684f, 3.200000047683716f), new Vector3(103, 103.0999999046325684f, 103.200000047683716f));

            examplePropertyTable
                .UseProperty(stringProperty)
                .SetValues("Rain 🌧", "Thunder ⛈");

            // assign to primitive

            var featureId0 = new FeatureIDBuilder(2, 0, examplePropertyTable);
            var featureId1 = new FeatureIDBuilder(2, 1, examplePropertyTable);

            model.LogicalMeshes[0].Primitives[0].AddMeshFeatureIds( featureId0, featureId1 );

            var ctx = new ValidationResult(model, ValidationMode.Strict, true);
            model.AttachToCurrentTest("cesium_ext_structural_metadata_featureid_attribute_and_property_table.glb");
            model.AttachToCurrentTest("cesium_ext_structural_metadata_featureid_attribute_and_property_table.gltf");
            model.AttachToCurrentTest("cesium_ext_structural_metadata_featureid_attribute_and_property_table.plotly");
        }

        // sample see https://github.com/CesiumGS/3d-tiles-samples/tree/main/glTF/EXT_structural_metadata/FeatureIdAttributeAndPropertyTable
        [Test(Description = "ext_structural_metadata with FeatureIdAttributeAndPropertyTable")]        
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

            // --------------------------------------------------------------

            var rootMetadata = model.UseStructuralMetadata();
            var schema = rootMetadata.UseEmbeddedSchema("FeatureIdAttributeAndPropertyTableSchema");

            // define schema

            var exampleMetadataClass = schema
                .UseClassMetadata("exampleMetadataClass")
                .WithName("Example metadata class")
                .WithDescription("An example metadata class");

            var vector3Property = exampleMetadataClass
                .UseProperty("example_VEC3_FLOAT32")
                .WithName("Example VEC3 FLOAT32 property")
                .WithDescription("An example property, with type VEC3, with component type FLOAT32")
                .WithVector3Type();

            var matrix4x4Property = exampleMetadataClass
                .UseProperty("example_MAT4_FLOAT32")
                .WithName("Example MAT4 FLOAT32 property")
                .WithDescription("An example property, with type MAT4, with component type FLOAT32")
                .WithMatrix4x4Type();

            // define table

            var examplePropertyTable = exampleMetadataClass.AddPropertyTable(1, "Example property table");

            examplePropertyTable
                .UseProperty(vector3Property)
                .SetValues(new Vector3(3, 3.0999999046325684f, 3.200000047683716f));

            examplePropertyTable
                .UseProperty(matrix4x4Property)
                .SetValues(Matrix4x4.Identity);

            // assign to primitive

            var featureId = new FeatureIDBuilder(1, 0, examplePropertyTable);

            model.LogicalMeshes[0].Primitives[0].AddMeshFeatureIds(featureId);

            var ctx = new ValidationResult(model, ValidationMode.Strict, true);
            model.AttachToCurrentTest("cesium_ext_structural_metadata_multiple_featureids_and_properties.glb");
            model.AttachToCurrentTest("cesium_ext_structural_metadata_multiple_featureids_and_properties.gltf");
            model.AttachToCurrentTest("cesium_ext_structural_metadata_multiple_featureids_and_properties.plotly");
        }

        // sample see https://github.com/CesiumGS/3d-tiles-samples/blob/main/glTF/EXT_structural_metadata/ComplexTypes/
        [Test(Description = "ext_structural_metadata with complex types")]        
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

            // --------------------------------------------------------------

            var rootMetadata = model.UseStructuralMetadata();
            var schema = rootMetadata.UseEmbeddedSchema("FeatureIdAttributeAndPropertyTableSchema");

            // define schema            

            var exampleMetadataClass = schema
                .UseClassMetadata("exampleMetadataClass")
                .WithName("Example metadata class A")
                .WithDescription("First example metadata class");

            // enums

            var exampleEnum = schema.UseEnumMetadata("exampleEnumType", ("ExampleEnumValueA", 0), ("ExampleEnumValueB", 1), ("ExampleEnumValueC", 2));

            //// class properties

            var uint8ArrayProperty = exampleMetadataClass
                .UseProperty("example_variable_length_ARRAY_normalized_UINT8")
                .WithName("Example variable-length ARRAY normalized INT8 property")
                .WithDescription("An example property, with type ARRAY, with component type UINT8, normalized, and variable length")
                .WithUInt8ArrayType()
                .WithNormalized(false);

            var fixedLengthBooleanProperty = exampleMetadataClass
                .UseProperty("example_fixed_length_ARRAY_BOOLEAN")
                .WithName("Example fixed-length ARRAY BOOLEAN property")
                .WithDescription("An example property, with type ARRAY, with component type BOOLEAN, and fixed length ")
                .WithBooleanArrayType(4)
                .WithNormalized(false);

            var variableLengthStringArrayProperty = exampleMetadataClass
                .UseProperty("example_variable_length_ARRAY_STRING")
                .WithName("Example variable-length ARRAY STRING property")
                .WithDescription("An example property, with type ARRAY, with component type STRING, and variable length")
                .WithStringArrayType();

            var fixed_length_ARRAY_ENUM = exampleMetadataClass
                .UseProperty("example_fixed_length_ARRAY_ENUM")
                .WithName("Example fixed-length ARRAY ENUM property")
                .WithDescription("An example property, with type ARRAY, with component type ENUM, and fixed length")
                .WithEnumArrayType(exampleEnum, 2);

            var examplePropertyTable = exampleMetadataClass.AddPropertyTable(1, "Example property table");

            var bytes = new List<List<byte>>();
            bytes.Add(new List<byte>() { 0, 1, 2, 3, 4, 5, 6, 7 });
            examplePropertyTable
                .UseProperty(uint8ArrayProperty)
                .SetArrayValues(bytes);

            var bools = new List<List<bool>>();
            bools.Add(new List<bool>() { true, false, true, false });
            examplePropertyTable
                .UseProperty(fixedLengthBooleanProperty)
                .SetArrayValues(bools);

            var strings = new List<List<string>>();
            strings.Add(["Example string 1", "Example string 2", "Example string 3"]);
            examplePropertyTable
                .UseProperty(variableLengthStringArrayProperty)
                .SetArrayValues(strings);

            // Fill property table with enum values
            var shorts = new List<List<short>>();
            shorts.Add([0, 1]);
            examplePropertyTable
                .UseProperty(fixed_length_ARRAY_ENUM)
                .SetArrayValues(shorts);

            // add to primitive            
            var featureId = new FeatureIDBuilder(1, 0, examplePropertyTable);

            model.LogicalMeshes[0].Primitives[0].AddMeshFeatureIds(featureId);

            var ctx = new ValidationResult(model, ValidationMode.Strict, true);
            model.AttachToCurrentTest("cesium_ext_structural_metadata_complex_types.glb");
            model.AttachToCurrentTest("cesium_ext_structural_metadata_complex_types.gltf");
            model.AttachToCurrentTest("cesium_ext_structural_metadata_complex_types.plotly");
        }

        // Sample see https://github.com/CesiumGS/3d-tiles-samples/blob/main/glTF/EXT_structural_metadata/MultipleClasses/
        [Test(Description = "ext_structural_metadata with multiple classes")]        
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

            var rootMetadata = model.UseStructuralMetadata();
            var schema = rootMetadata.UseEmbeddedSchema("MultipleClassesSchema");            

            // classes

            var classA = schema
                .UseClassMetadata("exampleMetadataClassA")
                .WithName("Example metadata class A")
                .WithDescription("First example metadata class");

            var classAp0 = classA.UseProperty("example_FLOAT32")
                .WithName("Example FLOAT32 property")
                .WithDescription("An example property, with component type FLOAT32")
                .WithFloat32Type();

            var classAp1 = classA.UseProperty("example_INT64")
                .WithName("Example INT64 property")
                .WithDescription("An example property, with component type INT64")
                .WithInt64Type();

            var classB = schema.UseClassMetadata("exampleMetadataClassB")
                .WithName("Example metadata class B")
                .WithDescription("Second example metadata class");

            var classBp0 = classB.UseProperty("example_UINT16")
                .WithName("Example UINT16 property")
                .WithDescription("An example property, with component type UINT16")
                .WithUInt16Type();

            var classBp1 = classB.UseProperty("example_FLOAT64")
                .WithName("Example FLOAT64 property")
                .WithDescription("An example property, with component type FLOAT64")
                .WithFloat64Type();

            // properties

            var firstPropertyTable = classA.AddPropertyTable(1, "First example property table");
            firstPropertyTable.UseProperty(classAp0).SetValues<float>(100);
            firstPropertyTable.UseProperty(classAp1).SetValues<long>(101);

            var secondPropertyTable = classB.AddPropertyTable(1, "Second example property table");
            secondPropertyTable.UseProperty(classBp0).SetValues<ushort>(102);
            secondPropertyTable.UseProperty(classBp1).SetValues<double>(103);

            // features

            // FeatureID 0: featureCount=1, attribute=0, propertyTable=0 
            var featureId0 = new FeatureIDBuilder(1, 0, firstPropertyTable);
            // FeatureID 1: featureCount=1, attribute=1, prorpertyTable=1
            var featureId1 = new FeatureIDBuilder(1, 1, secondPropertyTable);
            
            model.LogicalMeshes[0].Primitives[0].AddMeshFeatureIds(featureId0, featureId1);
            var ctx = new ValidationResult(model, ValidationMode.Strict, true);
            model.AttachToCurrentTest("cesium_ext_structural_metadata_multiple_classes.glb");
            model.AttachToCurrentTest("cesium_ext_structural_metadata_multiple_classes.gltf");
            model.AttachToCurrentTest("cesium_ext_structural_metadata_multiple_classes.plotly");
        }

        
        // Sample see https://github.com/CesiumGS/3d-tiles-samples/blob/main/glTF/EXT_structural_metadata/PropertyAttributesPointCloud/
        // Note in the sample an external json file (MetadataSchema.json) is used to define the schema, which is not supported
        // in this library yet.
        // This test uses the same schema but defines it in code instead.
        [Test(Description = "ext_structural_metadata with pointcloud and custom attributes")]
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

            // --------------------------------------------------------------

            var rootMetadata = model.UseStructuralMetadata();
            var schema = rootMetadata.UseEmbeddedSchema();

            var classA = schema
                .UseClassMetadata("exampleMetadataClass")
                .WithName("Example metadata class")
                .WithDescription("An example metadata class for property attributes");

            classA.UseProperty("intensity")
                .WithName("Example intensity property")
                .WithDescription("An example property for the intensity, with component type FLOAT32")
                .WithFloat32Type();

            var speciesEnum = schema.UseEnumMetadata("classificationEnumType", ("MediumVegetation", 0), ("Buildings", 1));

            classA
                .UseProperty("classification")
                .WithName("Example classification property")
                .WithDescription("An example property for the classification, with the classificationEnumType")
                .WithEnumeration(speciesEnum);


            var propertyAttribute = rootMetadata.AddPropertyAttribute(classA);
            var intensityAttribute = propertyAttribute.CreateProperty("intensity");
            intensityAttribute.Attribute = "_INTENSITY";
            var classificationAttribute = propertyAttribute.CreateProperty("classification");
            classificationAttribute.Attribute = "_CLASSIFICATION";

            var ctx = new ValidationResult(model, ValidationMode.Strict, true);

            foreach (var primitive in model.LogicalMeshes[0].Primitives)
            {
                primitive.AddPropertyAttribute(propertyAttribute);
            }

            model.AttachToCurrentTest("cesium_ext_structural_metadata_with_pointcloud_attributes.glb");
            model.AttachToCurrentTest("cesium_ext_structural_metadata_with_pointcloud_attributes.gltf");
            model.AttachToCurrentTest("cesium_ext_structural_metadata_with_pointcloud_attributes.plotly");
        }
    }
}
