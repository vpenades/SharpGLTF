using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using NUnit.Framework;

namespace SharpGLTF.Runtime
{
    [Category("Core.Runtime")]
    [AttachmentPathFormat("*/TestResults/Runtime/?", true)]
    internal class ExtensionTests
    {
        [Test]
        public void TestAnimatedVisibility()
        {
            var modelPath = TestFiles.GetSampleModelsPaths()
                                .FirstOrDefault(item => item.Contains("CubeVisibility.glb"));

            var model = Schema2.ModelRoot.Load(modelPath);

            var vinput = model.LogicalAccessors[3].AsScalarArray();
            var voutput = model.LogicalAccessors[4].AsIndexArray();
            var vtrack = vinput.Zip(voutput, (i,o) => (i,o));

            var sceneTemplate = SceneTemplate.Create(model.DefaultScene);
            var sceneInstance = sceneTemplate.CreateInstance();

            foreach(var (time,val) in vtrack)
            {
                sceneInstance.Armature.SetAnimationFrame(0, time);

                TestContext.Out.WriteLine($"Time:{time} Value:{val}");                

                foreach (var nodeInst in sceneInstance.Armature.LogicalNodes)
                {
                    TestContext.Out.WriteLine($"{nodeInst.Name} {nodeInst.IsVisible}");
                }                

                var drawables = sceneInstance.AsEnumerable().ToArray();

                TestContext.Out.WriteLine($"count: {drawables.Length}");
                TestContext.Out.WriteLine(string.Empty);

                Assert.That(drawables.Length, Is.EqualTo(val != 0 ? 2 : 1));
            }

            
        }
    }
}
