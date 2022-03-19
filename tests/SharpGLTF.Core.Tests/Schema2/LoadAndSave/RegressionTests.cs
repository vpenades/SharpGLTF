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
            var path = TestFiles.GetSampleModelsPaths().First(item => item.EndsWith("Suzanne.gltf"));

            var suzanne1 = ModelRoot.Load(path, ValidationMode.TryFix);

            path = suzanne1.AttachToCurrentTest("suzanne.glb");

            var suzanne2 = ModelRoot.Load(path);

            Assert.AreEqual(suzanne1.LogicalMeshes.Count, suzanne2.LogicalMeshes.Count);

            Assert.Less( new System.IO.FileInfo(path).Length, 1024*512);
        }
    }
}
