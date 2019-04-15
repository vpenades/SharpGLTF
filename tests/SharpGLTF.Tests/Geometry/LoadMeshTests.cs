using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using NUnit.Framework;

namespace SharpGLTF.Geometry
{
    [TestFixture]
    public class LoadMeshTests
    {
        #region setup

        [OneTimeSetUp]
        public void Setup()
        {
            TestFiles.DownloadReferenceModels();
        }

        #endregion

        [Test]
        public void LoadModels()
        {
            foreach (var f in TestFiles.GetSampleModelsPaths())
            {
                var root = Schema2.ModelRoot.Load(f);
                Assert.NotNull(root);
            }
        }

        [Test]
        public void LoadBrokenFile()
        {
            var f = TestFiles.GetSampleModelsPaths().First(item => item.EndsWith(".gltf"));

            var json = System.IO.File.ReadAllText(f);

            // break the file
            json = json.Substring(0, json.Length - 40);

            Assert.Throws<Validation.SchemaException>(() => Schema2.ModelRoot.ParseGLTF(json, new Schema2.ReadSettings()));
        }
    }
}
