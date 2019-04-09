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

            bool passed = true;

            foreach (var f in files)
            {
                try
                {
                    var model = ModelRoot.Load(f.Item1);

                    if (!f.Item2)
                    {
                        TestContext.Error.WriteLine($"{f.Item1.ToShortDisplayPath()} 👎😦 Should not load!");
                        passed = false;
                    }
                    else
                    {
                        TestContext.WriteLine($"{f.Item1.ToShortDisplayPath()} 🙂👍");                        
                    }                    
                }
                catch (Exception ex)
                {
                    if (f.Item2)
                    {
                        TestContext.Error.WriteLine($"{f.Item1.ToShortDisplayPath()} 👎😦 Should load!");
                        TestContext.Error.WriteLine($"   ERROR: {ex.Message}");
                        passed = false;
                    }
                    else
                    {
                        TestContext.WriteLine($"{f.Item1.ToShortDisplayPath()} 🙂👍");
                        TestContext.WriteLine($"   Exception: {ex.Message}");
                    }                    
                }

                if (f.Item2 && !f.Item1.ToLower().Contains("compatibility"))
                {
                    var model = ModelRoot.Load(f.Item1);
                    model.AttachToCurrentTest(System.IO.Path.ChangeExtension(System.IO.Path.GetFileName(f.Item1), ".obj"));
                }
            }

            Assert.IsTrue(passed);
        }
    }
}
