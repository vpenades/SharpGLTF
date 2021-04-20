using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;

using NUnit.Framework;

namespace SharpGLTF.Validation
{
    [Category("glTF-Validator Files")]
    public class InvalidFilesTests
    {
        [Test]
        public void CheckInvalidJsonFiles()
        {
            var files = TestFiles
                .GetKhronosValidationPaths()
                .Where(item => item.EndsWith(".gltf"))
                .Where(item => item.Contains("\\data\\json\\"));

            foreach (var f in files)
            {
                var report = ValidationReport.Load(f + ".report.json");

                TestContext.Progress.WriteLine($"{f}...");
                TestContext.Write($"{f}...");

                var result = Schema2.ModelRoot.Validate(f);

                Assert.IsTrue(result.HasErrors == report.Issues.NumErrors > 0);
            }
        }

        [Test]
        public void CheckInvalidBinaryFiles()
        {
            var files = TestFiles
                .GetKhronosValidationPaths()
                .Where(item => item.EndsWith(".glb"));          

            foreach (var f in files)
            {
                var report = ValidationReport.Load(f + ".report.json");

                TestContext.Progress.WriteLine($"{f}...");
                TestContext.WriteLine($"{f}...");

                var result = Schema2.ModelRoot.Validate(f);

                Assert.IsTrue(result.HasErrors == report.Issues.NumErrors > 0);
            }
        }

        [Test]
        public void CheckInvalidFiles()
        {
            var files = TestFiles
                .GetKhronosValidationPaths()
                .Where(item => item.EndsWith(".gltf"));

            foreach (var f in files)
            {
                // System.Diagnostics.Debug.Assert(!f.EndsWith("invalid_uri_scheme.gltf"));

                var gltfJson = f.EndsWith(".gltf") ? System.IO.File.ReadAllText(f) : string.Empty;

                var report = ValidationReport.Load($"{f}.report.json");
                
                var result = Schema2.ModelRoot.Validate(f);

                if (result.HasErrors != report.Issues.NumErrors > 0)
                {
                    TestContext.WriteLine($"Failed: {f}");
                    foreach (var e in report.Issues.Messages.Where(item => item.Severity == 0)) TestContext.WriteLine($"    {e.Message}");
                }

                Assert.AreEqual(report.Issues.NumErrors > 0, result.HasErrors);                                
            }
        }
    }
}
