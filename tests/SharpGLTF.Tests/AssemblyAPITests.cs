using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using NUnit.Framework;

namespace SharpGLTF
{
    [TestFixture]
    public class AssemblyAPITests
    {
        [Test]
        public void DumpCoreAPI()
        {
            TestContext.CurrentContext.AttachShowDirLink();

            var assembly = typeof(Schema2.ModelRoot).Assembly;

            var API = DumpAssemblyAPI.GetAssemblySignature(assembly).OrderBy(item => item).ToArray();

            TestContext.CurrentContext.AttachText("CoreAPI.txt", API);

            foreach(var l in API)
            {
                TestContext.WriteLine(l);
            }
        }

        [Test]
        public void DumpToolkitAPI()
        {
            TestContext.CurrentContext.AttachShowDirLink();

            var assembly = typeof(Schema2.Schema2Toolkit).Assembly;

            var API = DumpAssemblyAPI.GetAssemblySignature(assembly).OrderBy(item => item).ToArray();

            TestContext.CurrentContext.AttachText("ToolkitAPI.txt", API);

            foreach (var l in API)
            {
                TestContext.WriteLine(l);
            }
        }
    }
}
