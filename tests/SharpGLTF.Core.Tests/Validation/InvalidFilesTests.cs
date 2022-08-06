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
                var json = System.IO.File.ReadAllText(f + ".report.json");
                var report = GltfValidator.ValidationReport.Parse(json);

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
                var json = System.IO.File.ReadAllText(f + ".report.json");
                var report = GltfValidator.ValidationReport.Parse(json);

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
                .Where(item => item.EndsWith(".gltf"))
                .Where(item => !item.Contains("KHR_materials_variants"));

            foreach (var f in files)
            {
                if (f.EndsWith("invalid_image_data.gltf")) continue; // we're not checking images data (yet)
                if (f.EndsWith("png_eos.gltf")) continue; // we're not checking images data (yet)

                // https://github.com/KhronosGroup/glTF-Validator/issues/189
                if (f.EndsWith("node_weights_override.gltf")) continue;
                if (f.EndsWith("valid_with_tangent.gltf")) continue;

                // if (!f.EndsWith("out_of_range.gltf")) continue;

                var gltfJson = f.EndsWith(".gltf") ? System.IO.File.ReadAllText(f) : string.Empty;
                
                var json = System.IO.File.ReadAllText($"{f}.report.json");
                var report = GltfValidator.ValidationReport.Parse(json);

                var result = Schema2.ModelRoot.Validate(f);

                if (result.HasErrors != report.Issues.NumErrors > 0)
                {
                    TestContext.WriteLine($"Failed: {f}");
                    foreach (var e in report.Issues.Messages.Where(item => item.Severity == 0)) TestContext.WriteLine($"    {e.Text}");
                }

                Assert.AreEqual(report.Issues.NumErrors > 0, result.HasErrors, result.Errors.FirstOrDefault()?.Message);                                
            }
        }
    }
}
