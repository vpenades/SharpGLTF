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
            
            Assert.AreEqual(0.3f, v1.CustomId);
        }

        [Test]
        public void TransferContentTest()
        {
            var v1 = new VertexColor1Texture1Custom1(Vector4.One, Vector2.One, 0.3f);
            var v2 = v1.ConvertToMaterial<VertexCustom2>();
            Assert.AreEqual(0.3f, v2.CustomId0);
        }

    }
}
