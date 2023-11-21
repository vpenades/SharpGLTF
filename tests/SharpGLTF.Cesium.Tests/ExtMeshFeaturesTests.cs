using NUnit.Framework;
using SharpGLTF.Geometry;
using SharpGLTF.Geometry.VertexTypes;
using SharpGLTF.Materials;
using SharpGLTF.Scenes;
using SharpGLTF.Schema2;
using SharpGLTF.Validation;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;

namespace SharpGLTF.Cesium
{
    using VBTexture1 = VertexBuilder<VertexPosition, VertexTexture1, VertexEmpty>;

    [Category("Toolkit.Scenes")]
    public class ExtMeshFeaturesTests
    {
        [SetUp]
        public void SetUp()
        {
            CesiumExtensions.RegisterExtensions();
        }

        [Test(Description = "Creates a simple triangle with Cesium EXT_Mesh_Features with Id = 100")]
        public void FeaturesIdAttributeTest()
        {
            // see sample https://github.com/CesiumGS/3d-tiles-samples/tree/main/glTF/EXT_mesh_features/FeatureIdAttribute

            TestContext.CurrentContext.AttachGltfValidatorLinks();

            // Create a triangle with feature ID custom vertex attribute
            var featureId = 100;
            var material = MaterialBuilder.CreateDefault();

            var mesh = new MeshBuilder<VertexPositionNormal, VertexWithFeatureId, VertexEmpty>("mesh");
            var prim = mesh.UsePrimitive(material);

            // All the vertices in the triangle have the same feature ID
            var vt0 = GetVertexBuilderWithFeatureId(new Vector3(-10, 0, 0), new Vector3(0, 0, 1), featureId);
            var vt1 = GetVertexBuilderWithFeatureId(new Vector3(10, 0, 0), new Vector3(0, 0, 1), featureId);
            var vt2 = GetVertexBuilderWithFeatureId(new Vector3(0, 10, 0), new Vector3(0, 0, 1), featureId);

            prim.AddTriangle(vt0, vt1, vt2);
            var scene = new SceneBuilder();
            scene.AddRigidMesh(mesh, Matrix4x4.Identity);
            var model = scene.ToGltf2();

            var featureIdAttribute = new MeshExtMeshFeatureID(2, 0, label: "my_label");

            // Set the FeatureIds
            var featureIds = new List<MeshExtMeshFeatureID>() { featureIdAttribute };
            model.LogicalMeshes[0].Primitives[0].SetFeatureIds(featureIds);

            // Validate the FeatureIds
            var cesiumExtMeshFeaturesExtension = (MeshExtMeshFeatures)model.LogicalMeshes[0].Primitives[0].Extensions.FirstOrDefault();
            Assert.NotNull(cesiumExtMeshFeaturesExtension.FeatureIds);

            Assert.IsTrue(cesiumExtMeshFeaturesExtension.FeatureIds.Equals(featureIds));
            
            // Check there should be a custom vertex attribute with name _FEATURE_ID_{attribute}
            var attribute = cesiumExtMeshFeaturesExtension.FeatureIds[0].Attribute;
            Assert.IsTrue(attribute == 0);
            var primitive = model.LogicalMeshes[0].Primitives[0];
            var featureIdVertexAccessor = primitive.GetVertexAccessor($"_FEATURE_ID_{attribute}");
            Assert.NotNull(featureIdVertexAccessor);
            var items = featureIdVertexAccessor.AsScalarArray();
            Assert.AreEqual(items, new List<int> { featureId, featureId, featureId });

            var ctx = new ValidationResult(model, ValidationMode.Strict, true);

            model.ValidateContent(ctx.GetContext());
            scene.AttachToCurrentTest("cesium_ext_mesh_features_feature_id_attribute.glb");
            scene.AttachToCurrentTest("cesium_ext_mesh_features_feature_id_attribute.gltf");
            scene.AttachToCurrentTest("cesium_ext_mesh_features_feature_id_attribute.plotly");
        }

        [Test]
        public void FeaturesIdTextureTest()
        {
            var img0 = "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";
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

            // todo: add featureIdTexture
            // Method 2: Feature ID by Texture coordinates
            // var texture = new MeshExtMeshFeatureIDTexture(new List<int>(0) { }, 0, 0);
            // var featureIdTexture = new MeshExtMeshFeatureID(3, nullFeatureId: 0, texture: texture);
        }

        private static VertexBuilder<VertexPositionNormal, VertexWithFeatureId, VertexEmpty> GetVertexBuilderWithFeatureId(Vector3 position, Vector3 normal, int featureid)
        {
            var vp0 = new VertexPositionNormal(position, normal);
            var vb0 = new VertexBuilder<VertexPositionNormal, VertexWithFeatureId, VertexEmpty>(vp0, featureid);
            return vb0;
        }
    }
}
