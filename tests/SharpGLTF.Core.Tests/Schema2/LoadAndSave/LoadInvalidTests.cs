using System;
using System.Collections.Generic;
using System.Text;

using NUnit.Framework;

namespace SharpGLTF.Schema2.LoadAndSave
{
    [TestFixture]
    [AttachmentPathFormat("*/TestResults/LoadAndSave/?", true)]
    [Category("Model Load and Save")]
    public class LoadInvalidTests
    {
        [Test]
        public void LoadInvalidJsonModel()
        {
            var path = ResourceInfo.From("Invalid_Json.gltf").FilePath;

            Assert.Throws<Validation.SchemaException>(() => ModelRoot.Load(path));

            var validation = ModelRoot.Validate(path);

            Assert.That(validation.HasErrors, Is.True);
        }
    }
}
