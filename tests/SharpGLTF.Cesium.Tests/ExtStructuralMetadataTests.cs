using NUnit.Framework;
using SharpGLTF.Geometry;
using SharpGLTF.Geometry.VertexTypes;
using SharpGLTF.Materials;
using SharpGLTF.Scenes;
using SharpGLTF.Schema2;
using SharpGLTF.Validation;
using System.Collections.Generic;
using System.Numerics;

namespace SharpGLTF.Cesium
{
    [Category("Toolkit.Scenes")]
    public class ExtStructuralMetadataTests
    {
        [SetUp]
        public void SetUp()
        {
            CesiumExtensions.RegisterExtensions();
        }

        [Test(Description ="First test with ext_structural_metadata")]
        public void TriangleWithMetadataTest()
        {
            TestContext.CurrentContext.AttachGltfValidatorLinks();

            // Create a triangle with feature ID custom vertex attribute
            var featureId = 1;
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

            var featureIdAttribute = new MeshExtMeshFeatureID(1, 0);

            // Set the FeatureIds
            var featureIds = new List<MeshExtMeshFeatureID>() { featureIdAttribute };
            model.LogicalMeshes[0].Primitives[0].SetFeatureIds(featureIds);

            model.InitializeMetadataExtension("propertyTable", 1);

            // todo add metadata

            var ctx = new ValidationResult(model, ValidationMode.Strict, true);
            model.ValidateContent(ctx.GetContext());

            model.AttachToCurrentTest("cesium_ext_structural_metadata_basic_triangle.glb");
            model.AttachToCurrentTest("cesium_ext_structural_metadata_basic_triangle.gltf");
            model.AttachToCurrentTest("cesium_ext_structural_metadata_basic_triangle.plotly");

        }
    }
}
