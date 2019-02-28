using System;
using System.Collections.Generic;
using System.Text;

using NUnit.Framework;

namespace SharpGLTF.Schema2.LoadAndSave
{
    [TestFixture]
    public class LoadModelTests
    {
        #region setup

        [OneTimeSetUp]
        public void Setup()
        {
            TestFiles.CheckoutDataDirectories();
        }

        #endregion

        #region testing models of https://github.com/bghgary/glTF-Asset-Generator.git

        [Test]
        public void TestLoadGeneratedModels()
        {
            foreach (var f in TestFiles.GetGeneratedFilePaths())
            {
                var model = GltfUtils.LoadModel(f);

                Assert.NotNull(model);
            }
        }
        
        [TestCase(0)]        
        [TestCase(6)]
        public void TestLoadCompatibleModels(int idx)
        {
            var filePath = TestFiles.GetCompatibilityFilePath(idx);

            var model = GltfUtils.LoadModel(filePath);

            Assert.NotNull(model);
        }

        [TestCase(1)]
        [TestCase(2)]
        [TestCase(3)]
        [TestCase(4)]
        [TestCase(5)]
        public void TestLoadInvalidModels(int idx)
        {
            var filePath = TestFiles.GetCompatibilityFilePath(idx);

            try
            {                
                ModelRoot.Load(filePath);
                Assert.Fail("Did not throw!");
            }
            catch(IO.ModelException ex)
            {
                TestContext.WriteLine($"{filePath} threw {ex.Message}");
            }
                       
        }

        #endregion

        #region testing models of https://github.com/KhronosGroup/glTF-Sample-Models.git

        [Test]
        public void TestLoadSampleModels()
        {
            TestContext.CurrentContext.AttachShowDirLink();

            foreach (var f in TestFiles.GetSampleFilePaths())
            {
                var model = GltfUtils.LoadModel(f);
                Assert.NotNull(model);

                model.AttachToCurrentTest(System.IO.Path.ChangeExtension(System.IO.Path.GetFileName(f), ".obj"));
            }
        }

        [Test]
        public void TestLoadSampleModelsWithMaterialSpecularGlossiness()
        {
            foreach (var f in TestFiles.GetFilePathsWithSpecularGlossinessPBR())
            {
                var root = GltfUtils.LoadModel(f);
                Assert.NotNull(root);
            }
        }

        #endregion

        #region testing polly model

        [Test(Description ="Example of traversing the visual tree all the way to individual vertices and indices")]
        public void TestLoadPolly()
        {
            TestContext.CurrentContext.AttachShowDirLink();
            
            // load Polly model
            var model = GltfUtils.LoadModel( TestFiles.GetPollyFilePath() );

            Assert.NotNull(model);            

            // Save as GLB, and also evaluate all triangles and save as Wavefront OBJ            
            model.AttachToCurrentTest("polly_out.glb");
            model.AttachToCurrentTest("polly_out.obj");

            // hierarchically browse some elements of the model:

            var scene = model.DefaultScene;

            var pollyNode = scene.FindNode("Polly_Display");            

            var pollyPrimitive = pollyNode.Mesh.Primitives[0];

            var pollyIndices = pollyPrimitive.GetIndices();
            var pollyPositions = pollyPrimitive.GetVertices("POSITION").AsVector3Array();
            var pollyNormals = pollyPrimitive.GetVertices("NORMAL").AsVector3Array();

            for (int i=0; i < pollyIndices.Count; i+=3)
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

        #endregion        
    }
}
