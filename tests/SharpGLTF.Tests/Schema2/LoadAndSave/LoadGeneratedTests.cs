using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using NUnit.Framework;

namespace SharpGLTF.Schema2.LoadAndSave
{
    /// <summary>
    /// Test cases for models found in <see href="https://github.com/bghgary/glTF-Asset-Generator"/>
    /// </summary>
    [TestFixture]
    public class LoadGeneratedTests
    {
        #region setup

        [OneTimeSetUp]
        public void Setup()
        {
            TestFiles.DownloadReferenceModels();
        }

        #endregion        

        [Test]
        public void TestLoadReferenceModels()
        {
            TestContext.CurrentContext.AttachShowDirLink();

            var files = TestFiles.GetReferenceModelPaths();

            foreach (var f in files)
            {
                // var errors = _LoadNumErrorsForModel(f);
                // if (errors > 0) continue;

                try
                {
                    var model = ModelRoot.Load(f);
                    model.AttachToCurrentTest(System.IO.Path.ChangeExtension(System.IO.Path.GetFileName(f), ".obj"));
                }
                catch (IO.UnsupportedExtensionException eex)
                {
                    TestContext.WriteLine($"{f.ToShortDisplayPath()} ERROR: {eex.Message}");
                }
            }
        }        

        private static int _LoadNumErrorsForModel(string gltfPath)
        {
            var dir = System.IO.Path.GetDirectoryName(gltfPath);
            var fn = System.IO.Path.GetFileNameWithoutExtension(gltfPath);

            var jsonPath = System.IO.Path.Combine(dir, "ValidatorResults", System.IO.Path.ChangeExtension(fn, "json"));

            var content = System.IO.File.ReadAllText(jsonPath);
            var doc = Newtonsoft.Json.Linq.JObject.Parse(content);

            var token = doc.SelectToken("issues").SelectToken("numErrors");

            return (int)token;
        }
    }
}
