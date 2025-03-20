using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using NUnit.Framework;

namespace SharpGLTF.Schema2.LoadAndSave
{
    /// <summary>
    /// Test cases for models found in <see href="https://github.com/KhronosGroup/glTF-Sample-Models"/> and more....
    /// </summary>
    [TestFixture]
    [AttachmentPathFormat("*/TestResults/LoadAndSave/?", true)]
    [Category("Model Load and Save")]
    public class LoadSampleTests
    {
        #region setup

        [OneTimeSetUp]
        public void Setup()
        {
            // TestFiles.DownloadReferenceModels();
        }
        
        #endregion

        #region helpers

        private static ModelRoot _LoadModel(string f, bool tryFix = false)
        {
            var settings = tryFix
                ? Validation.ValidationMode.TryFix
                : Validation.ValidationMode.Strict;

            ModelRoot model = null;

            var perf = System.Diagnostics.Stopwatch.StartNew();

            try
            {
                model = ModelRoot.Load(f, settings);                
            }
            catch (Exception ex)
            {
                TestContext.Progress.WriteLine($"Failed {f.ToShortDisplayPath()}");
                Assert.Fail(ex.Message);
            }

            Assert.That(model, Is.Not.Null);

            if (model == null) return null;

            var perf_load = perf.ElapsedMilliseconds;

            // do a model clone and compare it
            _AssertAreEqual(model, model.DeepClone());

            var perf_clone = perf.ElapsedMilliseconds;

            if (!f.Contains("Iridescence")) // the iridescence sample models declares using IOR but it's not actually used
            {
                var unsupportedExtensions = new[] { "MSFT_lod", "EXT_lights_image_based" };

                // check extensions used
                if (unsupportedExtensions.All(uex => !model.ExtensionsUsed.Contains(uex)))
                {
                    var detectedExtensions = model.GatherUsedExtensions().ToArray();
                    Assert.That(detectedExtensions, Is.EquivalentTo(model.ExtensionsUsed));
                }
            }

            // Save models
            model.AttachToCurrentTest(System.IO.Path.ChangeExtension(System.IO.Path.GetFileName(f), ".obj"));
            var perf_wavefront = perf.ElapsedMilliseconds;

            model.AttachToCurrentTest(System.IO.Path.ChangeExtension(System.IO.Path.GetFileName(f), ".glb"));
            var perf_glb = perf.ElapsedMilliseconds;

            TestContext.Progress.WriteLine($"processed {f.ToShortDisplayPath()} - Load:{perf_load}ms Clone:{perf_clone}ms S.obj:{perf_wavefront}ms S.glb:{perf_glb}ms");

            return model;
        }

        private static void _AssertAreEqual(ModelRoot a, ModelRoot b)
        {
            var aa = a.GetLogicalChildrenFlattened().ToList();
            var bb = b.GetLogicalChildrenFlattened().ToList();

            Assert.That(bb, Has.Count.EqualTo(aa.Count));

            Assert.That
                (
                aa.Select(item => item.GetType()),
                Is.EqualTo(bb.Select(item => item.GetType()))
                );
        }

        #endregion

        [TestCase("\\glTF\\")]
        // [TestCase("\\glTF-Draco\\")] // Not supported
        [TestCase("\\glTF-IBL\\")]
        [TestCase("\\glTF-Binary\\")]
        [TestCase("\\glTF-Embedded\\")]
        [TestCase("\\glTF-Quantized\\")]
        // [TestCase("\\glTF-Meshopt\\")] // not supported
        public void LoadModelsFromKhronosSamples(string section)
        {            
            TestContext.CurrentContext.AttachGltfValidatorLinks();

            #if !DEBUG
            Assert.Multiple( () => {
            #endif

                foreach (var f in TestFiles.GetSampleModelsPaths())
                {
                    if (f.Contains("SuzanneMorphSparse")) continue; // temporarily skipping due to empty BufferView issue                    
                    if (f.Contains("SunglassesKhronos")) continue; // KHR_materials_specular is declared but not used

                if (!f.Contains(section)) continue;

                    _LoadModel(f);
                }

            #if !DEBUG
            } );
            #endif
        }

        [Test]
        public void LoadModelsFromBabylonJs()
        {         
            TestContext.CurrentContext.AttachGltfValidatorLinks();

            foreach (var f in TestFiles.GetBabylonJSModelsPaths())
            {
                _LoadModel(f, true);
            }
        }        

        [TestCase("TeapotsGalore.gltf")]
        [TestCase("GrassFieldInstanced.glb")]
        [TestCase("InstanceTest.glb")]
        public void LoadModelsWithGpuMeshInstancingExtension(string fileFilter)
        {            
            TestContext.CurrentContext.AttachGltfValidatorLinks();

            var f = TestFiles.GetMeshIntancingModelPaths().FirstOrDefault(item => item.Contains(fileFilter));
                        
            var model = _LoadModel(f, false);

            var ff = System.IO.Path.GetFileNameWithoutExtension(f);

            model.AttachToCurrentTest($"{ff}.loaded.glb");

            // perform roundtrip

            var roundtripDefault = model.DefaultScene
                .ToSceneBuilder()                                       // glTF to SceneBuilder
                .ToGltf2(Scenes.SceneBuilderSchema2Settings.Default);   // SceneBuilder to glTF

            var roundtripInstanced = model.DefaultScene
                .ToSceneBuilder()                                               // glTF to SceneBuilder
                .ToGltf2(Scenes.SceneBuilderSchema2Settings.WithGpuInstancing); // SceneBuilder to glTF

            // compare bounding spheres

            var modelBounds = Runtime.MeshDecoder.EvaluateBoundingBox(model.DefaultScene);
            var rtripDefBounds = Runtime.MeshDecoder.EvaluateBoundingBox(roundtripDefault.DefaultScene);
            var rtripGpuBounds = Runtime.MeshDecoder.EvaluateBoundingBox(roundtripInstanced.DefaultScene);

            Assert.That(rtripDefBounds, Is.EqualTo(modelBounds));
            Assert.That(rtripGpuBounds, Is.EqualTo(modelBounds));

            // save results

            roundtripDefault.AttachToCurrentTest($"{ff}.roundtrip.default.glb");
            roundtripInstanced.AttachToCurrentTest($"{ff}.roundtrip.instancing.glb");            
        }

        [TestCase("AnimationPointerUVs.gltf")]
        [TestCase("IridescenceMetallicSpheres.gltf")]
        [TestCase("SpecGlossVsMetalRough.gltf")]
        [TestCase(@"TextureTransformTest.gltf")]
        [TestCase(@"UnlitTest\glTF-Binary\UnlitTest.glb")]                                                
        [TestCase(@"glTF-Quantized\Avocado.gltf")]
        [TestCase(@"glTF-Quantized\AnimatedMorphCube.gltf")]
        [TestCase(@"glTF-Quantized\AnimatedMorphCube.gltf")]
        [TestCase(@"glTF-Quantized\Duck.gltf")]
        [TestCase(@"glTF-Quantized\Lantern.gltf")]
        [TestCase(@"MosquitoInAmber.glb")]        
        public void LoadModelsWithExtensions(string filePath)
        {            
            TestContext.CurrentContext.AttachGltfValidatorLinks();

            filePath = TestFiles
                .GetSampleModelsPaths()
                .FirstOrDefault(item => item.EndsWith(filePath));

            _LoadModel(filePath);
        }

        [Test]
        public void LoadModelWithUnlitMaterial()
        {
            var f = TestFiles
                .GetSampleModelsPaths()
                .FirstOrDefault(item => item.EndsWith(@"UnlitTest\glTF-Binary\UnlitTest.glb"));

            var model = ModelRoot.Load(f);
            Assert.That(model, Is.Not.Null);

            Assert.That(model.LogicalMaterials[0].Unlit, Is.True);

            // do a model roundtrip
            var modelBis = ModelRoot.ParseGLB(model.WriteGLB());
            Assert.That(modelBis, Is.Not.Null);

            Assert.That(modelBis.LogicalMaterials[0].Unlit, Is.True);
        }

        [Test]
        public void LoadModelWithLights()
        {
            var f = TestFiles
                .GetSchemaExtensionsModelsPaths()
                .FirstOrDefault(item => item.EndsWith("lights.gltf"));

            var model = ModelRoot.Load(f);
            Assert.That(model, Is.Not.Null);

            Assert.That(model.LogicalPunctualLights, Has.Count.EqualTo(3));

            Assert.That(model.DefaultScene.VisualChildren.ElementAt(0).PunctualLight.LogicalIndex, Is.EqualTo(1));
            Assert.That(model.DefaultScene.VisualChildren.ElementAt(1).PunctualLight.LogicalIndex, Is.EqualTo(0));
        }

        [TestCase("SimpleSparseAccessor.gltf")]
        // [TestCase("SuzanneMorphSparse.gltf")] requires supporting empty BufferView
        public void LoadModelWithSparseAccessor(string fileName)
        {
            var path = TestFiles
                .GetSampleModelsPaths()
                .FirstOrDefault(item => item.Contains(fileName));

            var model = ModelRoot.Load(path);
            Assert.That(model, Is.Not.Null);

            var primitive = model.LogicalMeshes[0].Primitives[0];

            var accessor = primitive.GetVertexAccessor("POSITION");

            var basePositions = accessor._GetMemoryAccessor().AsVector3Array();

            var positions = accessor.AsVector3Array();
        }

        [Test]
        public void LoadModelWithMorphTargets()
        {
            var path = TestFiles
                .GetSampleModelsPaths()
                .FirstOrDefault(item => item.Contains("MorphPrimitivesTest.glb"));

            var model = ModelRoot.Load(path);
            Assert.That(model, Is.Not.Null);

            var triangles = model.DefaultScene
                .EvaluateTriangles<Geometry.VertexTypes.VertexPosition, Geometry.VertexTypes.VertexEmpty>(null, null, 0)
                .ToArray();

            model.AttachToCurrentTest(System.IO.Path.ChangeExtension(System.IO.Path.GetFileName(path), ".obj"));
            model.AttachToCurrentTest(System.IO.Path.ChangeExtension(System.IO.Path.GetFileName(path), ".glb"));
        }

        [TestCase("AnimationPointerUVs.glb")]
        [TestCase("RiggedFigure.glb")]
        [TestCase("RiggedFigure.glb")]
        [TestCase("RiggedSimple.glb")]
        [TestCase("BoxAnimated.glb")]
        [TestCase("AnimatedMorphCube.glb")]        
        [TestCase("CesiumMan.glb")]        
        [TestCase("BrainStem.glb")]
        [TestCase("Fox.glb")]
        public void LoadModelsWithAnimations(string path)
        {
            path = TestFiles
                .GetSampleModelsPaths()
                .FirstOrDefault(item => item.Contains(path));

            var model = ModelRoot.Load(path);
            Assert.That(model, Is.Not.Null);

            path = System.IO.Path.GetFileNameWithoutExtension(path);
            model.AttachToCurrentTest(path + ".glb");

            var triangles = model.DefaultScene
                .EvaluateTriangles<Geometry.VertexTypes.VertexPosition, Geometry.VertexTypes.VertexEmpty>()
                .ToArray();            

            var anim = model.LogicalAnimations[0];

            var duration = anim.Duration;

            for(int i=0; i < 10; ++i)
            {
                var t = duration * i / 10;
                int tt = (int)(t * 1000.0f);

                model.AttachToCurrentTest($"{path} at {tt}.obj", anim, t);
            }            
        }

        [Test]
        public void LoadAnimatedMorphCube()
        {
            var path = TestFiles
                .GetSampleModelsPaths()
                .FirstOrDefault(item => item.Contains("AnimatedMorphCube.glb"));

            var model = ModelRoot.Load(path);
            Assert.That(model, Is.Not.Null);

            var anim = model.LogicalAnimations[0];
            var node = model.LogicalNodes[0];

            var acc_master = node.Mesh.Primitives[0].GetVertexAccessor("POSITION");
            var acc_morph0 = node.Mesh.Primitives[0].GetMorphTargetAccessors(0)["POSITION"];
            var acc_morph1 = node.Mesh.Primitives[0].GetMorphTargetAccessors(1)["POSITION"];

            var pos_master = acc_master.AsVector3Array();
            var pos_morph0 = acc_morph0.AsVector3Array();
            var pos_morph1 = acc_morph1.AsVector3Array();

            // pos_master

            var instance = Runtime.SceneTemplate
                .Create(model.DefaultScene)
                .CreateInstance();

            var pvrt = node.Mesh.Primitives[0].GetVertexColumns();

            for (float t = 0; t < 5; t+=0.25f)
            {
                instance.Armature.SetAnimationFrame(anim.LogicalIndex, t);

                var nodexform = instance.GetDrawableInstance(0).Transform;

                TestContext.WriteLine($"Animation at {t}");

                var curves = node.GetCurveSamplers(anim);

                if (t < anim.Duration)
                {
                    var mw = curves.GetMorphingSampler<float[]>()
                        .CreateCurveSampler()
                        .GetPoint(t);            
                    
                    TestContext.WriteLine($"    Morph Weights: {mw[0]} {mw[1]}");
                }

                var msw = curves.GetMorphingSampler<Transforms.SparseWeight8>()
                    .CreateCurveSampler()
                    .GetPoint(t);

                TestContext.WriteLine($"    Morph Sparse : {msw.Weight0} {msw.Weight1}");

                var triangles = model.DefaultScene
                    .EvaluateTriangles<Geometry.VertexTypes.VertexPosition, Geometry.VertexTypes.VertexEmpty>(null, anim, t)
                    .ToList();

                var vertices = triangles
                    .SelectMany(item => new[] { item.A.Position, item.B.Position, item.C.Position })
                    .Distinct()
                    .ToList();

                foreach (var v in vertices) TestContext.WriteLine($"{v}");

                TestContext.WriteLine();
            }


        }

        [Test]
        public void LoadMultiUVTexture()
        {
            var path = TestFiles
                .GetSampleModelsPaths()
                .FirstOrDefault(item => item.Contains("TextureTransformMultiTest.glb"));

            var model = ModelRoot.Load(path);
            Assert.That(model, Is.Not.Null);

            var materials = model.LogicalMaterials;

            var normalTest0Mat = materials.FirstOrDefault(item => item.Name == "NormalTest0Mat");
            var normalTest0Mat_normal = normalTest0Mat.FindChannel("Normal").Value;
            var normalTest0Mat_normal_xform = normalTest0Mat_normal.TextureTransform;            

            Assert.That(normalTest0Mat_normal_xform, Is.Not.Null);
        }

        [Test]
        public void FindDependencyFiles()
        {
            TestContext.CurrentContext.AttachGltfValidatorLinks();

            foreach (var f in TestFiles.GetBabylonJSModelsPaths())
            {
                TestContext.WriteLine(f);

                var dependencies = ModelRoot.GetSatellitePaths(f);

                foreach(var d in dependencies)
                {
                    TestContext.WriteLine($"    {d}");
                }

                TestContext.WriteLine();
            }
        }
    }
}
