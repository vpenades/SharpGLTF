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

            // Add feature ID extension
            // See sample https://github.com/CesiumGS/glTF/tree/proposal-EXT_mesh_features/extensions/2.0/Vendor/EXT_mesh_features#feature-id-by-vertex

            // Method 1: Feature ID by attribute
            var featureIdAttribute = new MeshExtMeshFeatureID(2, 0);

            // Method 2: Feature ID by Texture coordinates
            var texture = new MeshExtMeshFeatureIDTexture(new List<int>() { 0 }, 0, 0);
            var featureIdTexture = new MeshExtMeshFeatureID(3, nullFeatureId: 0, texture: texture);

            // Method 3: Feature ID with property table
            var featureIdPropertyTable = new MeshExtMeshFeatureID(3, nullFeatureId: 0, texture: texture,propertyTable:1, label:"classification");

            // Set the FeatureIds
            var featureIds = new List<MeshExtMeshFeatureID>() { featureIdAttribute, featureIdTexture, featureIdPropertyTable };
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

            // Todo check: Add check for Texture?
            var ctx = new ValidationResult(model, ValidationMode.Strict, true);

            model.ValidateContent(ctx.GetContext());
            scene.AttachToCurrentTest("cesium_ext_mesh_features.glb");
            scene.AttachToCurrentTest("cesium_ext_mesh_features.gltf");
            scene.AttachToCurrentTest("cesium_ext_mesh_features.plotly");
        }


        private static VertexBuilder<VertexPositionNormal, VertexWithFeatureId, VertexEmpty> GetVertexBuilderWithFeatureId(Vector3 position, Vector3 normal, int featureid)
        {
            var vp0 = new VertexPositionNormal(position, normal);
            var vb0 = new VertexBuilder<VertexPositionNormal, VertexWithFeatureId, VertexEmpty>(vp0, featureid);
            return vb0;
        }


    }
}
