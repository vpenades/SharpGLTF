using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using NUnit.Framework;
using NUnit.Framework.Internal;

using SharpGLTF.Schema2;

namespace SharpGLTF
{
    internal class MainTests
    {
        [TestCase("glTF-Sample-Assets/Models/CesiumMan/glTF/CesiumMan.gltf")]
        [TestCase("glTF-Sample-Assets/Models/CesiumMan/glTF-Binary/CesiumMan.glb")]
        public void LoadTest(string modelPath)
        {
            var finfo = ResourceInfo.From(modelPath);

            // load Polly model
            var model = ModelRoot.Load(finfo.FilePath);

            Assert.That(model, Is.Not.Null);

        }
    }
}
