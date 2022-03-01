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

            var suzanne = ModelRoot.Load(path, ValidationMode.TryFix);

            path = suzanne.AttachToCurrentTest("suzanne.glb");

            Assert.Less(1024*1024, new System.IO.FileInfo(path).Length);
        }
    }
}
