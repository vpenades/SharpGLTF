using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using NUnit.Framework;

namespace SharpGLTF
{
    [TestFixture]
    public class TestAssemblyAPI
    {
        [Test]
        public void DumpCurrentAPI()
        {
            var assembly = typeof(Schema2.ModelRoot).Assembly;

            var API = DumpAssemblyAPI.DumpAPI(assembly).ToList();

            foreach(var l in API)
            {
                TestContext.WriteLine(l);
            }

        }
    }
}
