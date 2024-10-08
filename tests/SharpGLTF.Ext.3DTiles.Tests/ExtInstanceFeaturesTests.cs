﻿using NUnit.Framework;
using SharpGLTF.Scenes;
using SharpGLTF.Transforms;
using SharpGLTF.Validation;
using System;
using System.IO;
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

        // Test files are from https://github.com/CesiumGS/3d-tiles-validator/tree/main/specs/data/gltfExtensions/instanceFeatures

        [Test(Description = "Reads glTF's with EXT_Instance_Features")]
        [TestCase(@"InstanceFeaturesFeatureIdAttributeInvalidValue.gltf", typeof(ModelException))]
        [TestCase(@"InstanceFeaturesWithoutMeshGpuInstancing.gltf", typeof(SchemaException))]
        [TestCase(@"ValidInstanceFeatures.gltf", null)]
        public void ReadExtInstanceFeatures(string file, Type exception = null)
        {
            var fileName = ResourceInfo.From($"instanceFeatures/{file}");

            if (exception != null)
            {
                Assert.Throws(exception, delegate { ModelRoot.Load(fileName); });
            }
            else
            {
                var model = ModelRoot.Load(fileName);
                var instanceFeaturesExtension = model.LogicalNodes[0].GetExtension<MeshExtInstanceFeatures>();
                Assert.That(instanceFeaturesExtension.FeatureIds, Is.Not.Null);
                Assert.That(instanceFeaturesExtension.FeatureIds, Has.Count.GreaterThanOrEqualTo(1));
                var ctx = new ValidationResult(model, ValidationMode.Strict, true);
                model.ValidateContent(ctx.GetContext());
            }
        }


        [Test(Description = "Creates a gpu_instancing glTF from a tree with Cesium EXT_Instance_Features")]
        // Sample model structure is from https://github.com/CesiumGS/glTF/tree/3d-tiles-next/extensions/2.0/Vendor/EXT_instance_features
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
