using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

using NUnit.Framework;

namespace SharpGLTF
{
    [TestFixture]
    [Category("API Validation")]
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

        [Test(Description = "proof of concept to dump the whole public API of an assembly")]
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


        [Test(Description ="Checks if we have introduced a breaking change between the current and previous API")]
        public void DumpCoreAPI()
        {
            TestContext.CurrentContext.AttachShowDirLink();

            var assembly = typeof(Schema2.ModelRoot).Assembly;

            var API = DumpAssemblyAPI.GetAssemblySignature(assembly).OrderBy(item => item).ToArray();

            TestContext.CurrentContext.AttachText($"API.Core.{Schema2.Asset.AssemblyInformationalVersion}.txt", API);

            _CheckBackwardsCompatibility("API.Core.1.0.0-alpha0011.txt", API);
        }

        [Test(Description = "Checks if we have introduced a breaking change between the current and previous API")]
        public void DumpToolkitAPI()
        {
            TestContext.CurrentContext.AttachShowDirLink();

            var assembly = typeof(Schema2.Schema2Toolkit).Assembly;

            var API = DumpAssemblyAPI.GetAssemblySignature(assembly).OrderBy(item => item).ToArray();

            TestContext.CurrentContext.AttachText($"API.Toolkit.{Schema2.Asset.AssemblyInformationalVersion}.txt", API);

            _CheckBackwardsCompatibility("API.Toolkit.1.0.0-alpha0011.txt", API);
        }

        private static void _CheckBackwardsCompatibility(string referenceAPIFile, string[] newLines)
        {
            referenceAPIFile = System.IO.Path.Combine(TestContext.CurrentContext.WorkDirectory, "Assets", referenceAPIFile);

            var refLines = System.IO.File.ReadAllLines(referenceAPIFile);

            bool backwardsCompatible = true;

            foreach (var l in refLines)
            {
                if (!newLines.Contains(l))
                {
                    TestContext.WriteLine($"Missing:  {l}");
                    backwardsCompatible = false;
                }                
            }

            Warn.If(!backwardsCompatible, "Current API is not backwards compatible");
        }        
    }
}
