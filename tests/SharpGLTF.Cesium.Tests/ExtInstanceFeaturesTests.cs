using NUnit.Framework;
using SharpGLTF.Geometry;
using SharpGLTF.Geometry.VertexTypes;
using SharpGLTF.Materials;
using SharpGLTF.Scenes;
using SharpGLTF.Schema2;
using SharpGLTF.Validation;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

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

        [Test(Description = "Creates a simple triangle with Cesium EXT_Instance_Features")]
        public void AddExtInstanceFeatures()
        {
            TestContext.CurrentContext.AttachGltfValidatorLinks();

            // todo: create a test with ext_mesh_gpu_instancing

            var material = MaterialBuilder.CreateDefault();

            var mesh = new MeshBuilder<VertexPosition>("mesh");

            var prim = mesh.UsePrimitive(material);
            prim.AddTriangle(new VertexPosition(-10, 0, 0), new VertexPosition(10, 0, 0), new VertexPosition(0, 10, 0));

            var scene = new SceneBuilder();

            scene.AddRigidMesh(mesh, Matrix4x4.Identity);

            var model = scene.ToGltf2();

            // following the sample at https://github.com/CesiumGS/glTF/tree/3d-tiles-next/extensions/2.0/Vendor/EXT_instance_features#feature-id-by-gpu-instance
            var featureId0 = new MeshExtInstanceFeatureID(2, 0, 0, "Forests",2);
            var featureId1 = new MeshExtInstanceFeatureID(9, propertyTable: 1, label: "Trees");

            var featureIds = new List<MeshExtInstanceFeatureID>() { featureId0, featureId1 };
            model.LogicalNodes[0].SetFeatureIds(featureIds);

            var cesiumExtInstanceFeaturesExtension = (MeshExtInstanceFeatures)model.LogicalNodes[0].Extensions.FirstOrDefault();
            Assert.NotNull(cesiumExtInstanceFeaturesExtension.FeatureIds);

            Assert.IsTrue(cesiumExtInstanceFeaturesExtension.FeatureIds.Equals(featureIds));
            Assert.IsTrue(cesiumExtInstanceFeaturesExtension.FeatureIds[0].Equals(featureId0));
            Assert.IsTrue(cesiumExtInstanceFeaturesExtension.FeatureIds[1].Equals(featureId1));

            var ctx = new ValidationResult(model, ValidationMode.Strict, true);
            model.ValidateContent(ctx.GetContext());

            scene.AttachToCurrentTest("cesium_ext_instance_features.glb");
            scene.AttachToCurrentTest("cesium_ext_instance_features.gltf");
            scene.AttachToCurrentTest("cesium_ext_instance_features.plotly");
        }
    }
}
