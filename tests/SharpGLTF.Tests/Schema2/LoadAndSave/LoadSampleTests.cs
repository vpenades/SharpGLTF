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
    [Category("Model Load and Save")]
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
        public void LoadSampleModels(string section)
        {
            TestContext.CurrentContext.AttachShowDirLink();
            TestContext.CurrentContext.AttachGltfValidatorLinks();

            foreach (var f in TestFiles.GetSampleModelsPaths())
            {
                if (!f.Contains(section)) continue;

                _LoadModel(f);
            }
        }

        [Test]
        public void LoadBabylonJsModels()
        {
            TestContext.CurrentContext.AttachShowDirLink();
            TestContext.CurrentContext.AttachGltfValidatorLinks();

            foreach (var f in TestFiles.GetBabylonJSModelsPaths())
            {
                _LoadModel(f);
            }
        }

        private static void _LoadModel(string f)
        {
            var perf = System.Diagnostics.Stopwatch.StartNew();

            ModelRoot model = null;

            try
            {
                model = ModelRoot.Load(f);
                Assert.NotNull(model);
            }
            catch(Exception ex)
            {
                TestContext.Progress.WriteLine($"Failed {f.ToShortDisplayPath()}");

                Assert.Fail(ex.Message);
            }
            
            

            var perf_load = perf.ElapsedMilliseconds;

            // do a model clone and compare it
            _AssertAreEqual(model, model.DeepClone());

            var perf_clone = perf.ElapsedMilliseconds;

            var unsupportedExtensions = new[] { "MSFT_lod", "EXT_lights_image_based" };

            // check extensions used
            if (unsupportedExtensions.All(uex => !model.ExtensionsUsed.Contains(uex)))
            {
                var detectedExtensions = model.RetrieveUsedExtensions().ToArray();
                CollectionAssert.AreEquivalent(model.ExtensionsUsed, detectedExtensions);
            }

            // Save models
            model.AttachToCurrentTest(System.IO.Path.ChangeExtension(System.IO.Path.GetFileName(f), ".obj"));
            var perf_wavefront = perf.ElapsedMilliseconds;

            model.AttachToCurrentTest(System.IO.Path.ChangeExtension(System.IO.Path.GetFileName(f), ".glb"));
            var perf_glb = perf.ElapsedMilliseconds;

            TestContext.Progress.WriteLine($"processed {f.ToShortDisplayPath()} - Load:{perf_load}ms Clone:{perf_clone}ms S.obj:{perf_wavefront}ms S.glb:{perf_glb}ms");
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
        [TestCase(@"TextureTransformTest.gltf")]
        [TestCase(@"UnlitTest\glTF-Binary\UnlitTest.glb")]        
        public void LoadExtendedModels(string filePath)
        {
            TestContext.CurrentContext.AttachShowDirLink();
            TestContext.CurrentContext.AttachGltfValidatorLinks();

            filePath = TestFiles
                .GetSampleModelsPaths()
                .FirstOrDefault(item => item.EndsWith(filePath));

            var model = ModelRoot.Load(filePath);
            Assert.NotNull(model);

            // do a model clone and compare it
            _AssertAreEqual(model, model.DeepClone());

            // evaluate and save all the triangles to a Wavefront Object

            filePath = System.IO.Path.GetFileNameWithoutExtension(filePath);
            model.AttachToCurrentTest(filePath + "_wf.obj");
            model.AttachToCurrentTest(filePath + ".glb");
            model.AttachToCurrentTest(filePath + ".gltf");
        }

        [Test]
        public void LoadUnlitModel()
        {
            var f = TestFiles
                .GetSampleModelsPaths()
                .FirstOrDefault(item => item.EndsWith(@"UnlitTest\glTF-Binary\UnlitTest.glb"));

            var model = ModelRoot.Load(f);
            Assert.NotNull(model);

            Assert.IsTrue(model.LogicalMaterials[0].Unlit);

            // do a model roundtrip
            var modelBis = ModelRoot.ParseGLB(model.WriteGLB());
            Assert.NotNull(modelBis);

            Assert.IsTrue(modelBis.LogicalMaterials[0].Unlit);
        }

        [Test]
        public void LoadLightsModel()
        {
            var f = TestFiles
                .GetSchemaExtensionsModelsPaths()
                .FirstOrDefault(item => item.EndsWith("lights.gltf"));

            var model = ModelRoot.Load(f);
            Assert.NotNull(model);

            Assert.AreEqual(3, model.LogicalPunctualLights.Count);

            Assert.AreEqual(1, model.DefaultScene.VisualChildren.ElementAt(0).PunctualLight.LogicalIndex);
            Assert.AreEqual(0, model.DefaultScene.VisualChildren.ElementAt(1).PunctualLight.LogicalIndex);
        }

        [Test]
        public void LoadSparseModel()
        {
            var path = TestFiles
                .GetSampleModelsPaths()
                .FirstOrDefault(item => item.Contains("SimpleSparseAccessor.gltf"));

            var model = ModelRoot.Load(path);
            Assert.NotNull(model);

            var primitive = model.LogicalMeshes[0].Primitives[0];

            var accessor = primitive.GetVertexAccessor("POSITION");

            var basePositions = accessor._GetMemoryAccessor().AsVector3Array();

            var positions = accessor.AsVector3Array();
        }

        [Test]
        public void LoadMorphTargetModel()
        {
            TestContext.CurrentContext.AttachShowDirLink();

            var path = TestFiles
                .GetSampleModelsPaths()
                .FirstOrDefault(item => item.Contains("MorphPrimitivesTest.glb"));

            var model = ModelRoot.Load(path);
            Assert.NotNull(model);

            var triangles = model.DefaultScene
                .Triangulate<Geometry.VertexTypes.VertexPosition, Geometry.VertexTypes.VertexEmpty>(null, 0)
                .ToArray();

            model.AttachToCurrentTest(System.IO.Path.ChangeExtension(System.IO.Path.GetFileName(path), ".obj"));
            model.AttachToCurrentTest(System.IO.Path.ChangeExtension(System.IO.Path.GetFileName(path), ".glb"));
        }

        [TestCase("RiggedFigure.glb")]
        [TestCase("RiggedSimple.glb")]
        [TestCase("BoxAnimated.glb")]
        [TestCase("AnimatedMorphCube.glb")]
        [TestCase("AnimatedMorphSphere.glb")]
        [TestCase("CesiumMan.glb")]
        [TestCase("Monster.glb")]
        [TestCase("BrainStem.glb")]
        public void LoadAnimatedModels(string path)
        {
            TestContext.CurrentContext.AttachShowDirLink();

            path = TestFiles
                .GetSampleModelsPaths()
                .FirstOrDefault(item => item.Contains(path));

            var model = ModelRoot.Load(path);
            Assert.NotNull(model);

            path = System.IO.Path.GetFileNameWithoutExtension(path);
            model.AttachToCurrentTest(path + ".glb");

            var triangles = model.DefaultScene
                .Triangulate<Geometry.VertexTypes.VertexPosition, Geometry.VertexTypes.VertexEmpty>(null, 0)
                .ToArray();            

            var anim = model.LogicalAnimations[0];

            var duration = anim.Duration;

            for(int i=0; i < 10; ++i)
            {
                var t = duration * i / 10;
                int tt = (int)(t * 1000.0f);

                model.AttachToCurrentTest($"{path} at {tt}.obj",anim, t);
            }            
        }

        [Test]
        public void LoadAnimatedMorphCube()
        {
            TestContext.CurrentContext.AttachShowDirLink();

            var path = TestFiles
                .GetSampleModelsPaths()
                .FirstOrDefault(item => item.Contains("AnimatedMorphCube.glb"));

            var model = ModelRoot.Load(path);
            Assert.NotNull(model);

            path = System.IO.Path.GetFileNameWithoutExtension(path);
            model.AttachToCurrentTest(path + ".glb");

            var triangles = model.DefaultScene
                .Triangulate<Geometry.VertexTypes.VertexPosition, Geometry.VertexTypes.VertexEmpty>(null, 0)
                .ToArray();
        }
    }
}
