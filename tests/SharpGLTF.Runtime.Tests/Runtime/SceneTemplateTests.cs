using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;

using NUnit.Framework;

using Plotly;

namespace SharpGLTF.Runtime
{
    [Category("Core.Runtime")]
    [AttachmentPathFormat("*/TestResults/Runtime/?", true)]
    public class SceneTemplateTests
    {
        [Test]
        public void TestMemoryIsolation()
        {
            // this tests checks if, after creating a template from a scene,
            // there's any reference from the template to the source model
            // that prevents the source model to be garbage collected.

            (SceneTemplate, WeakReference<Schema2.ModelRoot>) scopedLoad()
            {
                var path = TestFiles.GetSampleModelsPaths()
                                .FirstOrDefault(item => item.Contains("BrainStem.glb"));

                var result = LoadModelTemplate(path);

                GC.Collect();
                GC.WaitForFullGCComplete();

                return result;
            }

            var result = scopedLoad();

            GC.Collect();
            GC.WaitForFullGCComplete();

            Assert.That(result.Item2.TryGetTarget(out Schema2.ModelRoot model), Is.False);
            Assert.That(model, Is.Null);
        }

        private static (SceneTemplate, WeakReference<Schema2.ModelRoot>) LoadModelTemplate(string path)
        {
            var model = Schema2.ModelRoot.Load(path);

            var options = new Runtime.RuntimeOptions { IsolateMemory = true };

            var template = SceneTemplate.Create(model.DefaultScene, options);

            return (template, new WeakReference<Schema2.ModelRoot>(model));
        }

        [Test]
        public static void TestMeshDecoding()
        {
            var modelPath = TestFiles.GetSampleModelsPaths()
                                .FirstOrDefault(item => item.Contains("BrainStem.glb"));

            var model = Schema2.ModelRoot.Load(modelPath);

            model.AttachToCurrentTest("reference.plotly");


            var scene = model.DefaultScene;
            
            var decodedMeshes = scene.LogicalParent.LogicalMeshes.Decode();
            var sceneTemplate = SceneTemplate.Create(scene);
            var sceneInstance = sceneTemplate.CreateInstance();

            var duration = sceneInstance.Armature.AnimationTracks[0].Duration;
            sceneInstance.Armature.SetAnimationFrame(0, duration/2);

            IEnumerable<(Vector3,Vector3,Vector3, int)> evaluateTriangles(DrawableInstance inst)
            {
                var mesh = decodedMeshes[inst.Template.LogicalMeshIndex];

                foreach(var prim in mesh.Primitives)
                {
                    foreach(var (idxA, idxB, idxC) in prim.TriangleIndices)
                    {
                        var posA = prim.GetPosition(idxA, inst.Transform);
                        var posB = prim.GetPosition(idxB, inst.Transform);
                        var posC = prim.GetPosition(idxC, inst.Transform);

                        yield return (posA, posB, posC, 0xb0b0b0);
                    }
                }
            }

            var worldTriangles = sceneInstance.SelectMany(item => evaluateTriangles(item));            

            var scenePlot = new PlotlyScene();
            scenePlot.AppendTriangles(worldTriangles, c=>c);

            AttachmentInfo
                .From("result.html")
                .WriteAllText(scenePlot.ToHtml());
        }

        [Test]
        public static void TestMeshDecodingBounds()
        {
            var modelPath = TestFiles.GetSampleModelsPaths()
                                .FirstOrDefault(item => item.Contains("BrainStem.glb"));

            var model = Schema2.ModelRoot.Load(modelPath);

            var (center, radius) = model.DefaultScene.EvaluateBoundingSphere(0.25f);           
            
            var sceneTemplate = SceneTemplate.Create(model.DefaultScene);
            var sceneInstance = sceneTemplate.CreateInstance();
            sceneInstance.Armature.SetAnimationFrame(0, 0.1f);

            var vertices = sceneInstance.GetWorldVertices(model.LogicalMeshes.Decode()).ToList();

            foreach(var p in vertices)
            {
                var d = (p - center).Length();
                Assert.That(d, Is.LessThanOrEqualTo(radius + 0.0001f));
            }
        }

        [Test]
        public static void TestAnimationPointer()
        {
            var modelPath = TestFiles.GetSampleModelsPaths()
                                .FirstOrDefault(item => item.Contains("AnimationPointerUVs.glb"));

            var model = Schema2.ModelRoot.Load(modelPath);            

            var sceneTemplate = SceneTemplate.Create(model.DefaultScene);
            var sceneInstance = sceneTemplate.CreateInstance();
            sceneInstance.Armature.SetAnimationFrame(0, 0.1f);            
        }
    }
}
