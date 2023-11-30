using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

using NUnit.Framework;

namespace SharpGLTF.Geometry.VertexTypes
{
    [Category("Toolkit")]
    public class CustomVertexTests
    {
        [Test]
        public void CreateCustomVertexTest()
        {
            var v2 = new VertexCustom2(0.3f, Vector4.One);
            var v1 = new VertexColor1Texture1Custom1(v2);
            
            Assert.That(v1.CustomId, Is.EqualTo(0.3f));
        }

        [Test]
        public void TransferContentTest()
        {
            var v1 = new VertexColor1Texture1Custom1(Vector4.One, Vector2.One, 0.3f);
            var v2 = v1.ConvertToMaterial<VertexCustom2>();
            Assert.That(v2.CustomId0, Is.EqualTo(0.3f));
        }

    }
}
