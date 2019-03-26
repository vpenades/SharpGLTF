using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using NUnit.Framework;

namespace SharpGLTF.Schema2.LoadAndSave
{
    /// <summary>
    /// Test cases for models found in <see href="https://github.com/KhronosGroup/glTF-Sample-Models"/>
    /// </summary>
    [TestFixture]
    public class LoadSampleTests
    {
        #region setup

        [OneTimeSetUp]
        public void Setup()
        {
            TestFiles.DownloadReferenceModels();
        }

        #endregion

        [TestCase("\\glTF\\")]
        // [TestCase("\\glTF-Draco\\")] // Not supported
        [TestCase("\\glTF-IBL\\")]
        [TestCase("\\glTF-Binary\\")]
        [TestCase("\\glTF-Embedded\\")]
        [TestCase("\\glTF-pbrSpecularGlossiness\\")]
        public void TestLoadSampleModels(string section)
        {
            TestContext.CurrentContext.AttachShowDirLink();
            TestContext.CurrentContext.AttachGltfValidatorLink();

            foreach (var f in TestFiles.GetSampleModelsPaths())
            {
                if (!f.Contains(section)) continue;

                var perf = System.Diagnostics.Stopwatch.StartNew();                

                var model = GltfUtils.LoadModel(f);
                Assert.NotNull(model);

                var perf_load = perf.ElapsedMilliseconds;

                // do a model clone and compare it
                _AssertAreEqual(model, model.DeepClone());

                var perf_clone= perf.ElapsedMilliseconds;

                // check extensions used
                if (!model.ExtensionsUsed.Contains("EXT_lights_image_based"))
                {
                    var detectedExtensions = model.RetrieveUsedExtensions().ToArray();
                    CollectionAssert.AreEquivalent(model.ExtensionsUsed, detectedExtensions);
                }
                
                // evaluate and save all the triangles to a Wavefront Object
                model.AttachToCurrentTest(System.IO.Path.ChangeExtension(System.IO.Path.GetFileName(f), ".obj"));
                var perf_wavefront = perf.ElapsedMilliseconds;

                model.AttachToCurrentTest(System.IO.Path.ChangeExtension(System.IO.Path.GetFileName(f), ".glb"));
                var perf_glb = perf.ElapsedMilliseconds;

                TestContext.Progress.WriteLine($"processed {f.ToShortDisplayPath()} - Load:{perf_load}ms Clone:{perf_clone}ms S.obj:{perf_wavefront}ms S.glb:{perf_glb}ms");
            }
        }

        private static void _AssertAreEqual(ModelRoot a, ModelRoot b)
        {
            var aa = a.GetLogicalChildrenFlattened().ToList();
            var bb = b.GetLogicalChildrenFlattened().ToList();

            Assert.AreEqual(aa.Count,bb.Count);

            CollectionAssert.AreEqual
                (
                aa.Select(item => item.GetType()),
                bb.Select(item => item.GetType())
                );
        }

        [TestCase("SpecGlossVsMetalRough.gltf")]
        [TestCase(@"UnlitTest\glTF-Binary\UnlitTest.glb")]
        public void TestLoadSpecialCaseModels(string filePath)
        {
            TestContext.CurrentContext.AttachShowDirLink();
            TestContext.CurrentContext.AttachGltfValidatorLink();

            var f = TestFiles.GetSampleModelsPaths()
                .FirstOrDefault(item => item.EndsWith(filePath));

            var model = GltfUtils.LoadModel(f);
            Assert.NotNull(model);

            // evaluate and save all the triangles to a Wavefront Object
            model.AttachToCurrentTest(System.IO.Path.ChangeExtension(System.IO.Path.GetFileName(f), ".obj"));
            model.AttachToCurrentTest(System.IO.Path.ChangeExtension(System.IO.Path.GetFileName(f), ".glb"));
            model.AttachToCurrentTest(System.IO.Path.ChangeExtension(System.IO.Path.GetFileName(f), ".gltf"));

            // do a model roundtrip
            var bytes = model.WriteGLB();
            var modelBis = ModelRoot.ParseGLB(bytes);

            // clone
            var cloned = model.DeepClone();
        }

        [Test]
        public void TestLoadUnlitModel()
        {
            var f = TestFiles.GetSampleModelsPaths()
                .FirstOrDefault(item => item.EndsWith(@"UnlitTest\glTF-Binary\UnlitTest.glb"));

            var model = GltfUtils.LoadModel(f);
            Assert.NotNull(model);

            Assert.IsTrue(model.LogicalMaterials[0].Unlit);

            // do a model roundtrip
            var modelBis = ModelRoot.ParseGLB(model.WriteGLB());
            Assert.NotNull(modelBis);

            Assert.IsTrue(modelBis.LogicalMaterials[0].Unlit);
        }

        [Test]
        public void TestLoadLightsModel()
        {
            var f = TestFiles.GetSchemaExtensionsModelsPaths()
                .FirstOrDefault(item => item.EndsWith("lights.gltf"));

            var model = GltfUtils.LoadModel(f);
            Assert.NotNull(model);

            Assert.AreEqual(3, model.LogicalPunctualLights.Count);

            Assert.AreEqual(1, model.DefaultScene.VisualChildren.ElementAt(0).PunctualLight.LogicalIndex);
            Assert.AreEqual(0, model.DefaultScene.VisualChildren.ElementAt(1).PunctualLight.LogicalIndex);
        }
    }
}
