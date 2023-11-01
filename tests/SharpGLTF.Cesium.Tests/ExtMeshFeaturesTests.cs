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
    public class ExtMeshFeaturesTests
    {
        [SetUp]
        public void SetUp()
        {
            CesiumExtensions.RegisterExtensions();
        }

        [Test(Description = "Creates a simple triangle with Cesium EXT_Mesh_Features")]
        public void AddExtMeshFeatures()
        {
            TestContext.CurrentContext.AttachGltfValidatorLinks();

            var material = MaterialBuilder.CreateDefault();

            var mesh = new MeshBuilder<VertexPosition>("mesh");

            var prim = mesh.UsePrimitive(material);
            prim.AddTriangle(new VertexPosition(-10, 0, 0), new VertexPosition(10, 0, 0), new VertexPosition(0, 10, 0));

            var scene = new SceneBuilder();

            scene.AddRigidMesh(mesh, Matrix4x4.Identity);

            var model = scene.ToGltf2();

            var featureIds = new List<FeatureID>() { new FeatureID(1) };
            model.LogicalMeshes[0].Primitives[0].SetFeatureIds(featureIds);

            var cesiumExtMeshFeaturesExtension = (MeshExtMeshFeatures)model.LogicalMeshes[0].Primitives[0].Extensions.FirstOrDefault();
            Assert.NotNull(cesiumExtMeshFeaturesExtension.FeatureIds);

            Assert.IsTrue(cesiumExtMeshFeaturesExtension.FeatureIds.Equals(featureIds));

            var ctx = new ValidationResult(model, ValidationMode.Strict, true);
            model.ValidateContent(ctx.GetContext());

            scene.AttachToCurrentTest("cesium_ext_mesh_features.glb");
            scene.AttachToCurrentTest("cesium_ext_mesh_features.gltf");
            scene.AttachToCurrentTest("cesium_ext_mesh_features.plotly");
        }

    }
}
