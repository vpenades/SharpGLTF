using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using NUnit.Framework;

namespace SharpGLTF.Runtime
{
    [Category("Core.Runtime")]
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

            Assert.IsFalse(result.Item2.TryGetTarget(out Schema2.ModelRoot model));
            Assert.Null(model);
        }

        private static (SceneTemplate, WeakReference<Schema2.ModelRoot>) LoadModelTemplate(string path)
        {
            var model = Schema2.ModelRoot.Load(path);

            var template = SceneTemplate.Create(model.DefaultScene, true);

            return (template, new WeakReference<Schema2.ModelRoot>(model));
        }

        [Test]
        public static void TestMeshDecoding()
        {
            var modelPath = TestFiles.GetSampleModelsPaths()
                                .FirstOrDefault(item => item.Contains("BrainStem.glb"));

            var model = Schema2.ModelRoot.Load(modelPath);

            var (center, radius) = model.DefaultScene.EvaluateBoundingSphere(1.0f);
            
            // precission needs to be fairly low because calculation results
            // in NetCore and NetFramework are amazingly different.
            Assert.AreEqual(-0.07429607f, center.X, 0.0001f);
            Assert.AreEqual( 0.8432209f, center.Y, 0.0001f);
            Assert.AreEqual(-0.04639983f, center.Z, 0.0001f);
            Assert.AreEqual( 2.528468f, radius, 0.0001f);
        }

    }
}
