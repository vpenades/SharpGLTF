using System;
using System.Collections.Generic;
using System.Text;

using NUnit.Framework;

namespace glTF2Sharp.Schema2
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
        public void GeneratedModelsLoadTest()
        {
            foreach (var f in TestFiles.GetGeneratedFilePaths())
            {
                var model = _LoadModel(f);

                Assert.NotNull(model);
            }
        }

        
        [TestCase(0)]        
        [TestCase(6)]
        public void LoadCompatibilityModelTest(int idx)
        {
            var filePath = TestFiles.GetCompatibilityFilePath(idx);

            var model = _LoadModel(filePath);

            Assert.NotNull(model);
        }

        [TestCase(1)]
        [TestCase(2)]
        [TestCase(3)]
        [TestCase(4)]
        [TestCase(5)]
        public void LoadInvalidModelsTest(int idx)
        {
            var filePath = TestFiles.GetCompatibilityFilePath(idx);

            try
            {                
                ModelRoot.Load(filePath);
                Assert.Fail("Did not throw!");
            }
            catch(ModelException ex)
            {
                TestContext.WriteLine($"{filePath} threw {ex.Message}");
            }
                       
        }

        #endregion

        #region testing models of https://github.com/KhronosGroup/glTF-Sample-Models.git

        [Test]
        public void SampleModelsLoadTest()
        {
            foreach (var f in TestFiles.GetSampleFilePaths())
            {
                var root = _LoadModel(f);
                Assert.NotNull(root);

                // var fileName = System.IO.Path.GetFileNameWithoutExtension(f);

                // root.DefaultScene._DumpWavefrontToTest(f);
            }
        }

        [Test]
        public void MaterialSpecularGlossinessModelsLoadTest()
        {
            foreach (var f in TestFiles.GetFilePathsWithSpecularGlossinessPBR())
            {
                var root = _LoadModel(f);
                Assert.NotNull(root);
            }
        }

        #endregion

        #region test polly model

        [Test(Description ="Example of traversing the visual tree all the way to individual vertices and indices")]
        public void TestLoadPolly()
        {
            var model = _LoadModel(TestFiles.GetPollyFilePath());            

            var scene = model.DefaultScene;

            var pollyNode = scene.FindNode("Polly_Display");            

            var pollyPrimitive = pollyNode.Mesh.Primitives[0];

            var pollyIndices = pollyPrimitive.IndexAccessor.CastToIndicesAccessor();
            var pollyPositions = pollyPrimitive.VertexAccessors["POSITION"].CastToVector3Accessor();
            var pollyNormals = pollyPrimitive.VertexAccessors["NORMAL"].CastToVector3Accessor();

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

        #region model loading

        public static ModelRoot _LoadModel(string filePath)
        {
            try
            {
                TestContext.Progress.WriteLine($"Loading {filePath.ToShortDisplayPath()}");

                return ModelRoot.Load(filePath);
            }
            catch(ExtensionException eex)            
            {
                TestContext.WriteLine($"{filePath} ERROR: {eex.Message}");

                return null;
            }
        }

        #endregion
    }
}
