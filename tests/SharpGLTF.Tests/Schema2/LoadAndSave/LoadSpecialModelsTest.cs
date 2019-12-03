using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using NUnit.Framework;

namespace SharpGLTF.Schema2.LoadAndSave
{
    /// <summary>
    /// Test cases for models found in <see href="https://github.com/KhronosGroup/glTF-Blender-Exporter"/>
    /// </summary>
    [TestFixture]
    [Category("Model Load and Save")]
    public class LoadSpecialModelsTest
    {
        #region setup

        [OneTimeSetUp]
        public void Setup()
        {
            // TestFiles.DownloadReferenceModels();
        }

        #endregion

        public void LoadWithCustomImageLoader()
        {
            TestContext.CurrentContext.AttachShowDirLink();

            

            // load Polly model
            var model = ModelRoot.Load(TestFiles.GetPollyFileModelPath());
        }

        [Test(Description = "Example of traversing the visual tree all the way to individual vertices and indices")]
        public void LoadPollyModel()
        {
            TestContext.CurrentContext.AttachShowDirLink();

            // load Polly model
            var model = ModelRoot.Load(TestFiles.GetPollyFileModelPath());

            Assert.NotNull(model);

            var triangles = model.DefaultScene
                .EvaluateTriangles<Geometry.VertexTypes.VertexPosition, Geometry.VertexTypes.VertexTexture1>(model.LogicalAnimations[0], 0.5f)
                .ToList();

            // Save as GLB, and also evaluate all triangles and save as Wavefront OBJ            
            model.AttachToCurrentTest("polly_out.glb");
            model.AttachToCurrentTest("polly_out.obj");

            // hierarchically browse some elements of the model:

            var scene = model.DefaultScene;

            var pollyNode = scene.FindNode(n => n.Name == "Polly_Display");

            var pollyPrimitive = pollyNode.Mesh.Primitives[0];

            var pollyIndices = pollyPrimitive.GetIndices();
            var pollyPositions = pollyPrimitive.GetVertices("POSITION").AsVector3Array();
            var pollyNormals = pollyPrimitive.GetVertices("NORMAL").AsVector3Array();

            for (int i = 0; i < pollyIndices.Count; i += 3)
            {
                var a = (int)pollyIndices[i + 0];
                var b = (int)pollyIndices[i + 1];
                var c = (int)pollyIndices[i + 2];

                var ap = pollyPositions[a];
                var bp = pollyPositions[b];
                var cp = pollyPositions[c];

                var an = pollyNormals[a];
                var bn = pollyNormals[b];
                var cn = pollyNormals[c];

                TestContext.WriteLine($"Triangle {ap} {an} {bp} {bn} {cp} {cn}");
            }
        }

        // [Test]
        public void LoadUniVRM()
        {
            TestContext.CurrentContext.AttachShowDirLink();

            var path = TestFiles.GetUniVRMModelPath();
            
            var model = ModelRoot.Load(path);
            Assert.NotNull(model);

            model.AttachToCurrentTest("AliceModel.glb");
        }

        // [Test]
        public void LoadShrekshaoModel()
        {
            TestContext.CurrentContext.AttachShowDirLink();

            var path = "Assets\\SpecialCases\\shrekshao.glb";

            var model = ModelRoot.Load(path);
            Assert.NotNull(model);
        }

        // these models show normal mapping but lack tangents, which are expected to be
        // generated at runtime; These tests generate the tangents and check them against the baseline.
        [TestCase("NormalTangentTest.glb")]
        [TestCase("NormalTangentMirrorTest.glb")]
        public void LoadGeneratedTangetsTest(string fileName)
        {
            TestContext.CurrentContext.AttachShowDirLink();

            var path = TestFiles.GetSampleModelsPaths().FirstOrDefault(item => item.EndsWith(fileName));

            var model = ModelRoot.Load(path);

            var mesh = model.DefaultScene
                .EvaluateTriangles<Geometry.VertexTypes.VertexPositionNormalTangent, Geometry.VertexTypes.VertexTexture1>()
                .ToMeshBuilder( m => m.ToMaterialBuilder() );            

            var editableScene = new Scenes.SceneBuilder();
            editableScene.AddRigidMesh(mesh, System.Numerics.Matrix4x4.Identity);

            model.AttachToCurrentTest("original.glb");
            editableScene.ToSchema2().AttachToCurrentTest("WithTangents.glb");
        }
    }
}
