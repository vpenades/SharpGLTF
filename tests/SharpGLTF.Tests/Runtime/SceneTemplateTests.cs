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

            var path = TestFiles.GetSampleModelsPaths()
                            .FirstOrDefault(item => item.Contains("CesiumMan.glb"));

            var result = LoadModelTemplate(path);            

            GC.Collect(0);
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
    }
}
