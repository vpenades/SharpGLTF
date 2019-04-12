using System;
using System.Collections.Generic;
using System.Text;

using NUnit.Framework;

namespace SharpGLTF.Schema2.LoadAndSave
{
    [TestFixture]
    public class LoadInvalidTests
    {
        [Test]
        public void TestLoadInvalidJson()
        {
            var path = System.IO.Path.Combine(TestContext.CurrentContext.TestDirectory, "Assets", "Invalid_Json.gltf");

            Assert.Throws<Validation.SchemaException>(() => ModelRoot.Load(path));
        }
    }
}
