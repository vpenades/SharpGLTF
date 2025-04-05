using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using NUnit.Framework;

using SharpGLTF.Schema2;

namespace SharpGLTF.Reflection
{
    internal class ReflectionTests
    {
        [TestCase("Avocado")]
        [TestCase("AnimationPointerUVs")]
        public void ReflectionDump(string modelName)
        {
            var mpath = TestFiles.GetSampleModelsPaths().FirstOrDefault(item => item.Contains(modelName));

            var model = ModelRoot.Load(mpath, Validation.ValidationMode.TryFix);

            _DumpReflectionItem(string.Empty, string.Empty, model);
        }

        private static void _DumpReflectionItem(string indent, string name, Object value)
        {
            if (value == null) return;

            TestContext.Out.Write($"{indent}{name}:");

            switch(value)
            {
                case IReflectionObject reflectionObject:
                    TestContext.Out.WriteLine(string.Empty);
                    indent += "    ";
                    foreach (var field in reflectionObject.GetFields())
                    {                        
                        _DumpReflectionItem(indent, field.Name, field.Value);
                    }
                    break;

                case IConvertible convertible:
                    TestContext.Out.WriteLine($"{value}");
                    break;

                case IEnumerable enumerable:
                    TestContext.Out.WriteLine(string.Empty);
                    indent += "    ";
                    foreach (var item in enumerable)
                    {
                        TestContext.Out.WriteLine($"{indent}{value}");
                    }
                    break;

                default:
                    TestContext.Out.WriteLine($"{value}");
                    break;
            }            
        }

        [TestCase("Avocado", "/materials/0/alphaCutoff", "0.5")]
        [TestCase("Avocado", "/nodes/0/rotation", "{X:0 Y:1 Z:0 W:0}")]
        [TestCase("AnimationPointerUVs", "/materials/61/extensions/KHR_materials_specular/specularTexture/extensions/KHR_texture_transform/offset", "<-0.2388889, 0.2388889>")]
        [TestCase("AnimationPointerUVs", "/materials/25/extensions/KHR_materials_anisotropy/anisotropyTexture/extensions/KHR_texture_transform/offset", "<-0.2388889, 0.2388889>")]
        [TestCase("AnimationPointerUVs", "/materials/1/extensions/KHR_materials_diffuse_transmission/diffuseTransmissionTexture/extensions/KHR_texture_transform/offset", "<-0.2388889, 0.2388889>")]
        public void ReflectionPointerPathTest(string modelName, string pointerPath, string expectedValue)
        {
            var mpath = TestFiles.GetSampleModelsPaths()
                .Where(item => !item.Contains("Quantized"))
                .FirstOrDefault(item => item.Contains(modelName));

            var model = ModelRoot.Load(mpath, Validation.ValidationMode.TryFix);

            var field = Reflection.FieldInfo.From(model, pointerPath);

            var result = FormattableString.Invariant($"{field.Value}");

            Assert.That(result, Is.EqualTo(expectedValue));
        }
    }
}
