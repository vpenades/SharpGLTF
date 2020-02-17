using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;

using NUnit.Framework;

namespace SharpGLTF.Validation
{
    [Category("Invalid files")]
    public class InvalidFilesTests
    {
        [Test]
        public void CheckInvalidJsonFiles()
        {
            var files = TestFiles
                .GetKhronosValidationPaths()
                .Where(item => item.EndsWith(".gltf"))
                .Where(item => item.Contains("\\data\\json\\"));

            foreach (var f in files)
            {
                TestContext.Progress.WriteLine($"{f}...");
                TestContext.Write($"{f}...");

                var result = Schema2.ModelRoot.Validate(f);

                Assert.IsTrue(result.HasErrors);                
            }
        }

        [Test]
        public void CheckExceptionOnInvalidFiles()
        {
            var files = TestFiles
                .GetKhronosValidationPaths()
                .Where(item => item.EndsWith(".gltf"));

            foreach (var f in files)
            {
                TestContext.Progress.WriteLine($"{f}...");

                TestContext.Write($"{f}...");

                try
                {

                    var result = Schema2.ModelRoot.Validate(f);

                    TestContext.WriteLine($"{result.HasErrors}");
                }
                catch(Exception ex)
                {
                    TestContext.WriteLine("THROW!");
                }                
            }
        }        

    }
}
