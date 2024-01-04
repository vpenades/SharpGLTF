using NUnit.Framework;
using SharpGLTF.Scenes;
using SharpGLTF.Transforms;
using SharpGLTF.Validation;
using System.Numerics;
using System.Text.Json.Nodes;

namespace SharpGLTF.Schema2.Tiles3D
{
    [Category("Toolkit.Scenes")]
    public class ExtInstanceFeaturesTests
    {
        [SetUp]
        public void SetUp()
        {
            Tiles3DExtensions.RegisterExtensions();
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


            var featureId0 = new FeatureIDBuilder(2, 0, label: "Forests");
            var featureId1 = new FeatureIDBuilder(9, label: "Trees");            

            var model = sceneBuilder.ToGltf2(settings);
            model.LogicalNodes[0].AddInstanceFeatureIds(featureId0, featureId1);

            var cesiumExtInstanceFeaturesExtension = model.LogicalNodes[0].GetExtension<MeshExtInstanceFeatures>();

            Assert.That(cesiumExtInstanceFeaturesExtension.FeatureIds, Is.Not.Null);
            
            Assert.That(featureId0.Equals(cesiumExtInstanceFeaturesExtension.FeatureIds[0])); 
            Assert.That(featureId1.Equals(cesiumExtInstanceFeaturesExtension.FeatureIds[1]));

            var ctx = new ValidationResult(model, ValidationMode.Strict, true);
            model.ValidateContent(ctx.GetContext());

            model.AttachToCurrentTest("cesium_ext_instance_features.glb");
            model.AttachToCurrentTest("cesium_ext_instance_features.gltf");
            model.AttachToCurrentTest("cesium_ext_instance_features.plotly");
        }
    }
}
