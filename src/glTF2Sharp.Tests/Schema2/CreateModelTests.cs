using System;
using System.Collections.Generic;
using System.Text;

using NUnit.Framework;

namespace glTF2Sharp.Schema2
{
    [TestFixture]
    public class CreateModelTests
    {
        [Test]
        public void CreateEmptyScene()
        {
            var root = ModelRoot.CreateNew();

            var scene = root.UseScene("Empty Scene");

            Assert.NotNull(scene);
            Assert.AreEqual("Empty Scene", scene.Name);
            Assert.AreEqual("Empty Scene", root.DefaultScene.Name);            
        }
    }
}
