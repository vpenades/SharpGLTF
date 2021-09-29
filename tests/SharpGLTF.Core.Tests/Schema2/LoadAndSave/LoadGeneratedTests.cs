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

        [TestCase(true)]
        [TestCase(false)]
        public void LoadGeneratedModels(bool isNegativeCase)
        {
            TestContext.CurrentContext.AttachShowDirLink();

            var files = TestFiles.GetReferenceModelPaths(isNegativeCase);

            bool passed = true;

            foreach (var filePath in files)
            {
                // System.Diagnostics.Debug.Assert(!filePath.EndsWith("Buffer_Interleaved_03.gltf"));

                if (filePath.EndsWith("Compatibility_05.gltf")) continue; // contains a REQUIRED Material_QuantumRendering

                var gltfJson = filePath.EndsWith(".gltf") ? System.IO.File.ReadAllText(filePath) : string.Empty;

                var report = GltfValidator.ValidationReport.Validate(filePath);                

                if (report == null) continue; // ??

                if (report.HasUnsupportedExtensions) continue;

                try
                {
                    var model = ModelRoot.Load(filePath);

                    if (report.Severity == GltfValidator.Severity.Error)
                    {
                        TestContext.Error.WriteLine($"{filePath.ToShortDisplayPath()} 👎😦 Should not load!");
                        passed = false;
                    }
                    else
                    {
                        TestContext.WriteLine($"{filePath.ToShortDisplayPath()} 🙂👍");                        
                    }                    
                }
                catch (Exception ex)
                {
                    if (report.Severity != GltfValidator.Severity.Error)
                    {
                        TestContext.Error.WriteLine($"{filePath.ToShortDisplayPath()} 👎😦 Should load!");
                        TestContext.Error.WriteLine($"   ERROR: {ex.Message}");
                        passed = false;
                    }
                    else
                    {
                        TestContext.WriteLine($"{filePath.ToShortDisplayPath()} 🙂👍");
                        TestContext.WriteLine($"   Expected Exception: {ex.Message}");
                    }                    
                }

                /*
                if (ShouldLoad && !filePath.ToLower().Contains("compatibility"))
                {
                    var model = ModelRoot.Load(filePath);
                    model.AttachToCurrentTest(System.IO.Path.ChangeExtension(System.IO.Path.GetFileName(filePath), ".obj"));
                }*/
            }

            Assert.IsTrue(passed);
        }        
    }
}
