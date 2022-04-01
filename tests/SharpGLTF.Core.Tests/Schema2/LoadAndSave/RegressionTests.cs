using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using NUnit.Framework;

using SharpGLTF.Validation;

namespace SharpGLTF.Schema2.LoadAndSave
{
    internal class RegressionTests
    {
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
    }
}
