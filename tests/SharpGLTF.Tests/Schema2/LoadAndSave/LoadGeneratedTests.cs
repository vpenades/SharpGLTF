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
    [Category("Model Load and Save")]
    public class LoadGeneratedTests
    {
        #region setup

        [OneTimeSetUp]
        public void Setup()
        {
            // TestFiles.DownloadReferenceModels();
        }

        #endregion        

        [Test]
        public void LoadPositiveModels()
        {
            TestContext.CurrentContext.AttachShowDirLink();

            var files = TestFiles.GetReferenceModelPaths();

            bool passed = true;

            foreach (var f in files)
            {
                try
                {
                    var model = ModelRoot.Load(f.Item1);

                    if (!f.ShouldLoad)
                    {
                        TestContext.Error.WriteLine($"{f.Path.ToShortDisplayPath()} 👎😦 Should not load!");
                        passed = false;
                    }
                    else
                    {
                        TestContext.WriteLine($"{f.Path.ToShortDisplayPath()} 🙂👍");                        
                    }                    
                }
                catch (Exception ex)
                {
                    if (f.ShouldLoad)
                    {
                        TestContext.Error.WriteLine($"{f.Path.ToShortDisplayPath()} 👎😦 Should load!");
                        TestContext.Error.WriteLine($"   ERROR: {ex.Message}");
                        passed = false;
                    }
                    else
                    {
                        TestContext.WriteLine($"{f.Path.ToShortDisplayPath()} 🙂👍");
                        TestContext.WriteLine($"   Exception: {ex.Message}");
                    }                    
                }

                if (f.ShouldLoad && !f.Path.ToLower().Contains("compatibility"))
                {
                    var model = ModelRoot.Load(f.Path);
                    model.AttachToCurrentTest(System.IO.Path.ChangeExtension(System.IO.Path.GetFileName(f.Path), ".obj"));
                }
            }

            Assert.IsTrue(passed);
        }

        [Test]
        public void LoadNegativeModels()
        {
            TestContext.CurrentContext.AttachShowDirLink();

            var files = TestFiles.GetReferenceModelPaths(true);

            bool passed = true;

            foreach (var f in files)
            {
                try
                {
                    var model = ModelRoot.Load(f.Path);

                    if (!f.ShouldLoad)
                    {
                        TestContext.Error.WriteLine($"{f.Path.ToShortDisplayPath()} 👎😦 Should not load!");
                        passed = false;
                    }
                    else
                    {
                        TestContext.WriteLine($"{f.Path.ToShortDisplayPath()} 🙂👍");
                    }
                }
                catch (Exception ex)
                {
                    if (f.ShouldLoad)
                    {
                        TestContext.Error.WriteLine($"{f.Path.ToShortDisplayPath()} 👎😦 Should load!");
                        TestContext.Error.WriteLine($"   ERROR: {ex.Message}");
                        passed = false;
                    }
                    else
                    {
                        TestContext.WriteLine($"{f.Path.ToShortDisplayPath()} 🙂👍");
                        TestContext.WriteLine($"   Exception: {ex.Message}");
                    }
                }

                if (f.ShouldLoad && !f.Path.ToLower().Contains("compatibility"))
                {
                    var model = ModelRoot.Load(f.Path);
                    model.AttachToCurrentTest(System.IO.Path.ChangeExtension(System.IO.Path.GetFileName(f.Path), ".obj"));
                }
            }

            Assert.IsTrue(passed);
        }
    }
}
