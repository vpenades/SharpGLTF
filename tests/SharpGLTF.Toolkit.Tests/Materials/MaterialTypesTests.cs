using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using NUnit.Framework;

namespace SharpGLTF.Materials
{
    internal class MaterialTypesTests
    {
        [Test]
        public void CheckKnownPropertyEnum()
        {
            var knownPropertyValues = Enum.GetNames(typeof(KnownProperty));
            var schemaPropertyValues = Enum.GetNames(typeof(Schema2._MaterialParameterKey));            

            CollectionAssert.AreEqual(schemaPropertyValues, knownPropertyValues);
        }
    }
}
