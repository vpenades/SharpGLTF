using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;

using NUnit.Framework;
using System.Runtime.InteropServices;

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
                TestContext.Out.Write($"{f}...");

                var result = Schema2.ModelRoot.Validate(f);

                Assert.That(result.HasErrors, Is.EqualTo(report.Issues.NumErrors > 0));
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
                
                if (report.Issues.Messages.Any(item => item.Code.Contains("GLB_CHUNK_TOO_BIG")) && report.Issues.NumErrors > 0)
                {
                    // System.Diagnostics.Debugger.Break();
                }

                TestContext.Progress.WriteLine($"{f}...");
                TestContext.Out.WriteLine($"{f}...");

                var result = Schema2.ModelRoot.Validate(f);

                Assert.That(result.HasErrors, Is.EqualTo(report.Issues.NumErrors > 0));
            }
        }

        [Test]
        public void CheckInvalidFiles()
        {
            var files = TestFiles
                .GetKhronosValidationPaths()
                .Where(item => item.EndsWith(".gltf"))
                .Where(item => !item.Contains("KHR_materials_variants"));

            Assert.Multiple(() =>
            {
                foreach (var f in files)
                {
                    var ff = new System.IO.FileInfo(f);

                    // malformed json needs to be checked at the serializer level.
                    if (f.EndsWith("custom_property.gltf")) continue;

                    // we're not checking images data (yet)
                    if (f.EndsWith("invalid_image_data.gltf")) continue;
                    if (f.EndsWith("png_eos.gltf")) continue;

                    //--------------- TODO

                    // for debug only 
                    // if (!f.EndsWith("primitive_generated_tangent_space.gltf")) continue;                    
                    
                    if (f.EndsWith("channel_duplicate_targets.gltf")) continue;
                    if (f.EndsWith("channel_node_matrix.gltf")) continue;
                    if (f.EndsWith("channel_node_no_morphs.gltf")) continue;
                    if (f.EndsWith("sampler_input_no_bounds.gltf")) continue;
                    if (f.EndsWith("sampler_too_few_cubic_frames.gltf")) continue;
                    if (f.EndsWith("invalid_input_values.gltf")) continue;
                    if (f.EndsWith("quaternion_cubic_non_unit_values.gltf")) continue;
                    if (f.EndsWith("quaternion_non_unit_values.gltf")) continue;
                    if (f.EndsWith("accessor_attribute_uint32.gltf")) continue;
                    if (f.EndsWith("invalid_attribute_escaped_name.gltf")) continue;
                    if (f.EndsWith("invalid_indexed_attribute.gltf")) continue;
                    if (f.EndsWith("morph_attribute_invalid_count.gltf")) continue;
                    if (f.EndsWith("morph_attribute_no_base.gltf")) continue;
                    if (f.EndsWith("morph_position_accessor_no_bounds.gltf")) continue;
                    if (f.EndsWith("morph_unequal_targets_count.gltf")) continue;
                    if (f.EndsWith("position_accessor_no_bounds.gltf")) continue;
                    if (f.EndsWith("primitive_generated_tangent_space.gltf")) continue;
                    if (f.EndsWith("unequal_joints.gltf")) continue;
                    if (f.EndsWith("colors_non_clamped.gltf")) continue;
                    if (f.EndsWith("no_joints_in_scene.gltf")) continue;
                    if (f.EndsWith("invalid_channel_target_node.gltf")) continue;
                    if (f.EndsWith("invalid_pointer_syntax.gltf")) continue;
                    if (f.EndsWith("missing_pointer.gltf")) continue;
                    if (f.EndsWith("no_tangent_space.gltf")) continue;
                    if (f.EndsWith("valid_with_tangent.gltf")) continue;
                    if (f.EndsWith("invalid_attribute.gltf")) continue;


                    var gltfJson = f.EndsWith(".gltf")
                        ? System.IO.File.ReadAllText(f)
                        : string.Empty;

                    // gltf analyzer
                    // var json = System.IO.File.ReadAllText($"{f}.report.json");
                    // var report = GltfValidator.ValidationReport.Parse(json);
                    var report = GltfValidator.ValidationReport.Validate(f); // using a newer validator will generate more up to date reports

                    // sharpGLTF analizer
                    var result = Schema2.ModelRoot.Validate(f);

                    string _getErrors()
                    {
                        var msg = ff.Directory.Name + "/" + ff.Name;

                        if (report.Issues.NumErrors > 0) msg += "\r\n  gltfValidator:";
                        foreach (var e in report.Issues.Messages.Where(item => item.Severity == 0)) msg += $"\r\n    {e.Text}";

                        if (result.HasErrors) msg += "\r\n  sharpGltf:";
                        foreach (var e in result.Errors)
                        {
                            var exx = e.InnerException ?? e;
                            msg += $"\r\n    {e.Message} {exx.Message} {exx.StackTrace}";
                        }
                        return msg;
                    }

                    Assert.That(result.HasErrors == report.Issues.NumErrors > 0, _getErrors());
                }
            });
        }
    }
}
