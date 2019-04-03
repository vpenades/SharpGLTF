using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

using NUnit.Framework;

namespace SharpGLTF
{
    [TestFixture]
    public class AssemblyAPITests
    {
        public class TestClass
        {
            public int PointerFunc(int x, out int y, ref int z) { y = 0; return x + 7; }

            public void ParamsFunc(int x, params string[] values) { }

            public void MultiArgsFunc(int x, int y=1, int z = 2) { }

            public int[][,][,,] MultiArray;

            public const string Alpha = "Alpha";

            public static readonly string Beta = "Beta";

            public delegate void MyDelegate(int x);

            public MyDelegate DelegateImpl;

            public enum Hello
            {
                World = 7
            }

            public event EventHandler EventX;

            public struct Structure
            {
                public int X;
                public int Y { get; }
                public int Z { get; set; }

                public System.Numerics.Vector3 Vector;
            }
        }

        [Test]
        public void DumpTestAPI()
        {
            TestContext.CurrentContext.AttachShowDirLink();

            var type = typeof(TestClass);

            var API = DumpAssemblyAPI.GetTypeSignature(type.GetTypeInfo()).OrderBy(item => item).ToArray();

            TestContext.CurrentContext.AttachText("TestAPI.txt", API);

            foreach (var l in API)
            {
                TestContext.WriteLine(l);
            }
        }


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
