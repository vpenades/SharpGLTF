using NUnit.Framework;
using SharpGLTF.Scenes;
using SharpGLTF.Schema2;
using SharpGLTF.Transforms;
using SharpGLTF.Validation;
using System.Collections.Generic;
using System.Numerics;
using System.Text.Json.Nodes;

namespace SharpGLTF.Cesium
{
    [Category("Toolkit.Scenes")]
    public class ExtInstanceFeaturesTests
    {
        [SetUp]
        public void SetUp()
        {
            CesiumExtensions.RegisterExtensions();
        }

        [Test(Description = "Creates a gpu_instancing glTF from a tree with Cesium EXT_Instance_Features")]
        public void AddExtGpuInstanceFeatures()
        {
            var settings = SceneBuilderSchema2Settings.WithGpuInstancing;
            settings.GpuMeshInstancingMinCount = 0;

            var modelRoot = ModelRoot.Load(ResourceInfo.From("tree.glb"));
            var meshBuilder = modelRoot.LogicalMeshes[0].ToMeshBuilder();
            var sceneBuilder = new SceneBuilder();
            var quaternion = Quaternion.CreateFromYawPitchRoll(0, 0, 0);
            var scale = Vector3.One;

            sceneBuilder.
                    AddRigidMesh(meshBuilder, new AffineTransform(scale, quaternion, new Vector3(-10, 0, 10))).
                    WithExtras(JsonNode.Parse("{\"_FEATURE_ID_0\":0}"));
            sceneBuilder.
                    AddRigidMesh(meshBuilder, new AffineTransform(scale, quaternion, new Vector3(0, 0, 0))).
                    WithExtras(JsonNode.Parse("{\"_FEATURE_ID_0\":1}"));


            var featureId0 = new MeshExtInstanceFeatureID(2, 0, 0, "Forests", 2);
            var featureId1 = new MeshExtInstanceFeatureID(9, propertyTable: 1, label: "Trees");

            var featureIds = new List<MeshExtInstanceFeatureID>() { featureId0, featureId1 };

            var model = sceneBuilder.ToGltf2(settings);
            model.LogicalNodes[0].SetFeatureIds(featureIds);

            var cesiumExtInstanceFeaturesExtension = model.LogicalNodes[0].GetExtension<MeshExtInstanceFeatures>();

            Assert.That(cesiumExtInstanceFeaturesExtension.FeatureIds, Is.Not.Null);
            Assert.That(cesiumExtInstanceFeaturesExtension.FeatureIds, Is.EqualTo(featureIds));
            Assert.That(cesiumExtInstanceFeaturesExtension.FeatureIds[0], Is.EqualTo(featureId0));
            Assert.That(cesiumExtInstanceFeaturesExtension.FeatureIds[1], Is.EqualTo(featureId1));

            var ctx = new ValidationResult(model, ValidationMode.Strict, true);
            model.ValidateContent(ctx.GetContext());

            model.AttachToCurrentTest("cesium_ext_instance_features.glb");
            model.AttachToCurrentTest("cesium_ext_instance_features.gltf");
            model.AttachToCurrentTest("cesium_ext_instance_features.plotly");
        }
    }
}
