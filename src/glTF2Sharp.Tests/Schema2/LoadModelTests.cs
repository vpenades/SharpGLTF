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
                TestContext.Progress.WriteLine($"Loading {f}...");

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
                TestContext.Progress.WriteLine($"Loading {f}...");

                var root = _LoadModel(f);
                Assert.NotNull(root);
            }
        }

        #endregion

        #region model loading

        static ModelRoot _LoadModel(string filePath)
        {
            try
            {
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
