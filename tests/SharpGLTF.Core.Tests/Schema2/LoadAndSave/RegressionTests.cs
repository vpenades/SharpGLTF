using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using NUnit.Framework;

using SharpGLTF.Validation;

namespace SharpGLTF.Schema2.LoadAndSave
{
    [AttachmentPathFormat("*/TestResults/LoadAndSave/?", true)]
    [ResourcePathFormat("*/Assets/SpecialCases")]
    internal class RegressionTests
    {
        [Test]
        public void LoadWithRelativeAndAbsolutePath()
        {
            // store current directory

            var cdir = Environment.CurrentDirectory;

            var modelPath = ResourceInfo.From("RelativePaths.gltf");

            // absolute path            

            var model1 = ModelRoot.Load(modelPath, Validation.ValidationMode.TryFix);
            Assert.NotNull(model1);
            Assert.AreEqual(4, model1.LogicalImages.Count);

            TestContext.WriteLine(string.Join("   ", ModelRoot.GetSatellitePaths(modelPath)));

            // local path            

            Environment.CurrentDirectory = System.IO.Path.GetDirectoryName(modelPath);
            var modelFile = System.IO.Path.GetFileName(modelPath);

            var model2 = ModelRoot.Load(modelFile, Validation.ValidationMode.TryFix);
            Assert.NotNull(model2);
            Assert.AreEqual(4, model2.LogicalImages.Count);

            TestContext.WriteLine(string.Join("   ", ModelRoot.GetSatellitePaths(modelPath)));

            // relative path:

            modelFile = System.IO.Path.Combine(System.IO.Path.GetFileName(Environment.CurrentDirectory), modelFile);
            Environment.CurrentDirectory = System.IO.Path.GetDirectoryName(Environment.CurrentDirectory);

            var model3 = ModelRoot.Load(modelFile, Validation.ValidationMode.TryFix);
            Assert.NotNull(model3);
            Assert.AreEqual(4, model3.LogicalImages.Count);

            TestContext.WriteLine(string.Join("   ", ModelRoot.GetSatellitePaths(modelPath)));

            // restore current directory

            Environment.CurrentDirectory = cdir;
        }

        [Test]
        public void LoadSuzanneTest()
        {
            var path1 = TestFiles.GetSampleModelsPaths().First(item => item.EndsWith("Suzanne.gltf"));

            var suzanne1 = ModelRoot.Load(path1, ValidationMode.TryFix);
            var suzanne1Mem = suzanne1.LogicalBuffers.Sum(item => item.Content.Length);

            // direct save-load

            var path2 = suzanne1
                .AttachToCurrentTest("suzanne2.glb");            

            var suzanne2 = ModelRoot.Load(path2);
            var suzanne2Mem = suzanne1.LogicalBuffers.Sum(item => item.Content.Length);

            Assert.AreEqual(suzanne1Mem, suzanne2Mem);
            Assert.AreEqual(suzanne1.LogicalMeshes.Count, suzanne2.LogicalMeshes.Count);

            // scenebuilder roundtrip

            var path3 = Scenes.SceneBuilder
                .CreateFrom(suzanne1.DefaultScene)
                .ToGltf2()
                .AttachToCurrentTest("suzanne.glb");

            var suzanne3 = ModelRoot.Load(path3);
            var suzanne3Mem = suzanne1.LogicalBuffers.Sum(item => item.Content.Length);

            Assert.AreEqual(suzanne1Mem, suzanne3Mem);
            Assert.AreEqual(suzanne1.LogicalMeshes.Count, suzanne3.LogicalMeshes.Count);
        }

        [Test]
        public void LoadBinaryWithLimitedStream()
        {
            var path1 = TestFiles.GetSampleModelsPaths().First(item => item.EndsWith("BrainStem.glb"));

            var bytes = System.IO.File.ReadAllBytes(path1);
            using(var ls = new ReadOnlyTestStream(bytes))
            {
                var model = ModelRoot.ReadGLB(ls);
                Assert.NotNull(model);
            }
        }
    }
}
