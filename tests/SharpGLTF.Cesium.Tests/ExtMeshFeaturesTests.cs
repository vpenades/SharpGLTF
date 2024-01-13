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
    public class ExtMeshFeaturesTests
    {
        [SetUp]
        public void SetUp()
        {
            Tiles3DExtensions.RegisterExtensions();
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
            var cesiumExtMeshFeaturesExtension = (MeshExtMeshFeatures)model.LogicalMeshes[0].Primitives[0].Extensions.FirstOrDefault();
            Assert.That(cesiumExtMeshFeaturesExtension.FeatureIds, Is.Not.Null);

            // Check there should be a custom vertex attribute with name _FEATURE_ID_{attribute}
            var attribute = cesiumExtMeshFeaturesExtension.FeatureIds[0].Attribute;
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

            var cesiumExtMeshFeaturesExtension = (MeshExtMeshFeatures)primitive.Extensions.FirstOrDefault();
            Assert.That(cesiumExtMeshFeaturesExtension.FeatureIds, Is.Not.Null);

            var firstFeatureId = cesiumExtMeshFeaturesExtension.FeatureIds[0];
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
