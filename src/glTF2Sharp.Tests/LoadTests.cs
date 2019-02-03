using System;
using System.Collections.Generic;
using System.Text;

using NUnit.Framework;

namespace glTF2Sharp
{
    [TestFixture]
    public class LoadTests
    {
        #region setup

        [OneTimeSetUp]
        public void Setup()
        {
            TestFiles.CheckDataDirectories();
        }

        #endregion

        #region testing models of https://github.com/bghgary/glTF-Asset-Generator.git

        [Test]
        public void GeneratedModelsLoadTest()
        {
            foreach (var f in TestFiles.GetGeneratedFilePaths())
            {
                var model = _LoadModel(f);

                // Assert.NotNull(model);
            }
        }

        
        [TestCase(0)]
        [TestCase(1)]
        [TestCase(2)]
        [TestCase(3)]
        [TestCase(4)]
        [TestCase(5)]
        public void LoadCompatibilityModelTest(int idx)
        {
            var filePath = TestFiles.GetCompatibilityFilePath(idx);

            var model = _LoadModel(filePath);

            // Assert.NotNull(model);
        }

        #endregion

        #region testing models of https://github.com/KhronosGroup/glTF-Sample-Models.git

        [Test]
        public void SampleModelsLoadTest()
        {
            foreach (var f in TestFiles.GetSampleFilePaths())
            {
                var root = _LoadModel(f);
                // Assert.NotNull(root);

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

        #region model loading

        static Schema2.ModelRoot _LoadModel(string filePath)
        {
            try
            {
                return Schema2.ModelRoot.Load(filePath);
            }
            catch(Schema2.ModelException mex)            
            {
                TestContext.WriteLine($"{filePath} ERROR: {mex.Message}");

                return null;
            }
        }

        #endregion
    }
}
