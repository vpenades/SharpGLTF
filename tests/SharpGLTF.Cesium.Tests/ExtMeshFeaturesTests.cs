using NUnit.Framework;
using SharpGLTF.Geometry;
using SharpGLTF.Geometry.VertexTypes;
using SharpGLTF.Materials;
using SharpGLTF.Scenes;
using SharpGLTF.Validation;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;

namespace SharpGLTF.Schema2.Tiles3D
{
    using VBTexture1 = VertexBuilder<VertexPosition, VertexTexture1, VertexEmpty>;

    [Category("Toolkit.Scenes")]
    public class ExtMeshFeaturesTests
    {
        [SetUp]
        public void SetUp()
        {
            Tiles3DExtensions.RegisterExtensions();
        }

        // Test files are from https://github.com/CesiumGS/3d-tiles-validator/tree/main/specs/data/gltfExtensions/meshFeatures
        [Test(Description = "Reads glTF's with EXT_Mesh_Features")]
        [TestCase(@"ValidFeatureIdAttributeDefault/ValidFeatureIdAttributeDefault.gltf", null)]
        [TestCase("FeatureIdAttributeAccessorNormalized.gltf", typeof(ModelException))]
        [TestCase("FeatureIdAttributeAccessorNotScalar.gltf", typeof(ModelException))]
        [TestCase("FeatureIdAttributeAttributeInvalidType.gltf", typeof(InvalidOperationException))]
        [TestCase("FeatureIdAttributeAttributeInvalidValue.gltf", typeof(ModelException))]
        [TestCase("FeatureIdAttributeFeatureCountInvalidType.gltf", typeof(InvalidOperationException))]
        [TestCase("FeatureIdAttributeFeatureCountInvalidValue.gltf", typeof(ModelException))]
        [TestCase("FeatureIdAttributeFeatureCountMismatch.gltf", typeof(ModelException))]
        [TestCase("FeatureIdAttributeFeatureCountMismatchForNullFeatureId.gltf", typeof(ModelException))]
        [TestCase("FeatureIdAttributeFeatureCountMissing.gltf", typeof(ModelException))]
        [TestCase("FeatureIdAttributeLabelInvalidType.gltf", typeof(ModelException))]
        [TestCase("FeatureIdAttributeLabelInvalidValue.gltf", typeof(ModelException))]
        [TestCase("FeatureIdAttributeNullFeatureIdInvalidType.gltf", typeof(InvalidOperationException))]
        [TestCase("FeatureIdAttributeNullFeatureIdInvalidValue.gltf", typeof(ModelException))]
        [TestCase("FeatureIdTextureFeatureCountMismatch.gltf", typeof(ModelException))]
        [TestCase("FeatureIdTextureSamplerInvalidFilterMode.gltf", typeof(ModelException))]
        [TestCase("FeatureIdTextureTextureChannelsInvalidElementType.gltf", typeof(InvalidOperationException))]
        [TestCase("FeatureIdTextureTextureChannelsInvalidType.gltf", typeof(SchemaException))]
        [TestCase("FeatureIdTextureTextureChannelsTooManyChannels.gltf", typeof(ModelException))]
        [TestCase("FeatureIdTextureTextureChannelsTooManyElements.gltf", typeof(ModelException))]
        [TestCase("FeatureIdTextureTextureImageDataInvalid.gltf", typeof(ModelException))]
        [TestCase("FeatureIdTextureTextureIndexInvalidType.gltf", typeof(InvalidOperationException))]
        [TestCase("FeatureIdTextureTextureIndexInvalidValue.gltf", typeof(LinkException))]
        [TestCase("FeatureIdTextureTextureInvalidType.gltf", typeof(SchemaException))]
        [TestCase("FeatureIdTextureTextureTexCoordInvalidType.gltf", typeof(InvalidOperationException))]
        [TestCase("FeatureIdTextureTextureTexCoordInvalidValue.gltf", typeof(ModelException))]
        [TestCase("ValidFeatureIdAttribute.gltf", null)]
        [TestCase("ValidFeatureIdAttributeWithByteStride.glb", null)]
        [TestCase("ValidFeatureIdAttributeWithLargerFeatureCount.gltf", null)]
        [TestCase("ValidFeatureIdAttributeWithNullFeatureId.gltf", null)]
        [TestCase("ValidFeatureIdTexture.glb", null)]
        [TestCase("ValidFeatureIdTexture.gltf", null)]
        [TestCase("ValidFeatureIdTextureUsingDefaultChannels.gltf", null)]
        public void ReadExtMeshFeaturesFiles(string file, Type exception = null)
        {
            var fileName = $"./testfixtures/meshFeatures/{file}";

            if (exception != null)
            {
                Assert.Throws(exception, delegate { ModelRoot.Load(fileName); });
            }
            else
            {
                var model = ModelRoot.Load(fileName);
                var meshFeaturesExtension = model.LogicalMeshes[0].Primitives[0].GetExtension<MeshExtMeshFeatures>();
                Assert.That(meshFeaturesExtension.FeatureIds, Is.Not.Null);
                Assert.That(meshFeaturesExtension.FeatureIds, Has.Count.GreaterThanOrEqualTo(1));
                var ctx = new ValidationResult(model, ValidationMode.Strict, true);
                model.ValidateContent(ctx.GetContext());
            }
        }

        [Test(Description = "Test for settting the FeatureIds with vertex attributes. See sample https://github.com/CesiumGS/3d-tiles-samples/blob/main/glTF/EXT_mesh_features/FeatureIdAttribute")]
        // In the sample html code, there is a shader that uses the feature ID to color the triangle
        public void FeaturesIdAttributeTest()
        {
            TestContext.CurrentContext.AttachGltfValidatorLinks();

            // Create a triangle with feature ID custom vertex attribute
            var featureId = 2;
            var material = MaterialBuilder.CreateDefault().WithDoubleSide(true);

            var mesh = new MeshBuilder<VertexPositionNormal, VertexWithFeatureId, VertexEmpty>("mesh");
            var prim = mesh.UsePrimitive(material);

            // All the vertices in the triangle have the same feature ID
            var vt0 = VertexBuilder.GetVertexWithFeatureId(new Vector3(-10, 0, 0), new Vector3(0, 0, 1), featureId);
            var vt1 = VertexBuilder.GetVertexWithFeatureId(new Vector3(10, 0, 0), new Vector3(0, 0, 1), featureId);
            var vt2 = VertexBuilder.GetVertexWithFeatureId(new Vector3(0, 10, 0), new Vector3(0, 0, 1), featureId);

            prim.AddTriangle(vt0, vt1, vt2);
            var scene = new SceneBuilder();
            scene.AddRigidMesh(mesh, Matrix4x4.Identity);
            var model = scene.ToGltf2();

            // Set the FeatureIds
            var featureIdAttribute = new FeatureIDBuilder(1, 0);
            model.LogicalMeshes[0].Primitives[0].AddMeshFeatureIds(featureIdAttribute);

            // Validate the FeatureIds
            var meshFeaturesExtension = (MeshExtMeshFeatures)model.LogicalMeshes[0].Primitives[0].Extensions.FirstOrDefault();
            Assert.That(meshFeaturesExtension.FeatureIds, Is.Not.Null);

            // Check there should be a custom vertex attribute with name _FEATURE_ID_{attribute}
            var attribute = meshFeaturesExtension.FeatureIds[0].Attribute;
            Assert.That(attribute == 0);
            var primitive = model.LogicalMeshes[0].Primitives[0];
            var featureIdVertexAccessor = primitive.GetVertexAccessor($"_FEATURE_ID_{attribute}");
            Assert.That(featureIdVertexAccessor, Is.Not.Null);
            var items = featureIdVertexAccessor.AsScalarArray();
            Assert.That(items, Is.EqualTo(new List<int> { featureId, featureId, featureId }));

            var ctx = new ValidationResult(model, ValidationMode.Strict, true);

            model.ValidateContent(ctx.GetContext());
            scene.AttachToCurrentTest("cesium_ext_mesh_features_feature_id_attribute.glb");
            scene.AttachToCurrentTest("cesium_ext_mesh_features_feature_id_attribute.gltf");
            scene.AttachToCurrentTest("cesium_ext_mesh_features_feature_id_attribute.plotly");
        }

        [Test(Description = "Test for settting the FeatureIds with a texture. See sample https://github.com/CesiumGS/3d-tiles-samples/blob/main/glTF/EXT_mesh_features/FeatureIdTexture")]
        // In the sample html code, there is a shader that uses the feature'ID from the texture to color the 2 triangles
        public void FeaturesIdTextureTest()
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

            // Set the FeatureIds, pointing to the red channel of the texture            
            var featureId = new FeatureIDBuilder(4, model.LogicalTextures[0]);

            var primitive = model.LogicalMeshes[0].Primitives[0];
            primitive.AddMeshFeatureIds(featureId);

            var meshFeaturesExtension = (MeshExtMeshFeatures)primitive.Extensions.FirstOrDefault();
            Assert.That(meshFeaturesExtension.FeatureIds, Is.Not.Null);

            var firstFeatureId = meshFeaturesExtension.FeatureIds[0];
            var texture = firstFeatureId.GetTexture();
            var texCoord = texture.TextureCoordinate;
            var textureIdVertexAccessor = primitive.GetVertexAccessor($"TEXCOORD_{texCoord}");
            Assert.That(textureIdVertexAccessor, Is.Not.Null);
            Assert.That(textureIdVertexAccessor.AsVector2Array(), Has.Count.EqualTo(4));

            var ctx = new ValidationResult(model, ValidationMode.Strict, true);

            model.ValidateContent(ctx.GetContext());
            scene.AttachToCurrentTest("cesium_ext_mesh_features_feature_id_texture.glb");
            scene.AttachToCurrentTest("cesium_ext_mesh_features_feature_id_texture.gltf");
            scene.AttachToCurrentTest("cesium_ext_mesh_features_feature_id_texture.plotly");
        }
    }
}
